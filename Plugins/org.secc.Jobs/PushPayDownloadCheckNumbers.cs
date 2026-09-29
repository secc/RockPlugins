// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Jobs;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Jobs
{
    [DefinedValueField( Rock.SystemGuid.DefinedType.FINANCIAL_SOURCE_TYPE, "Transaction Source", "The transaction source for PushPay transactions.", true )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.FINANCIAL_CURRENCY_TYPE, "Currency Type", "The currency type source for PushPay check transactions.", true )]
    [AttributeField( Rock.SystemGuid.EntityType.FINANCIAL_TRANSACTION, "Check Number Attribute", "The check number finacial transaction attribute." )]
    [SlidingDateRangeField( "Date Range", "The date range of transactions to include", true, "Previous|24|Hour||" )]
    public class PushPayDownloadCheckNumbers : RockJob
    {
        /// <summary>
        /// One HttpClient for the whole run; a new one per call exhausts sockets.
        /// </summary>
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds( 30 ) };

        /// <summary>
        /// Failed calls (timeouts, 5xx) on one gift before the job stops trying that gift. It is
        /// counted as an error and retried on the next run.
        /// </summary>
        private const int MaxErrorsPerGift = 3;

        /// <summary>
        /// Gifts in a row that end in errors, with no real answer from Pushpay in between, before
        /// the run gives up. A single bad gift can't stop the run, but an outage stops it after a
        /// few gifts instead of costing every remaining gift 19 merchants x the 30 second timeout.
        /// </summary>
        private const int MaxConsecutiveFailedGifts = 5;

        /// <summary>
        /// Longest the job waits for a token the Pushpay DLL still considers valid to reach its
        /// stored expiry, after Pushpay has already rejected it.
        /// </summary>
        private static readonly TimeSpan MaxTokenExpiryWait = TimeSpan.FromMinutes( 2 );

        // OAuth tokens are issued per Pushpay ACCOUNT, not per merchant - our 19 merchants all
        // share one account and therefore one token.
        private readonly Dictionary<int, string> _accountTokens = new Dictionary<int, string>();
        private readonly Dictionary<int, DateTime?> _accountTokenExpires = new Dictionary<int, DateTime?>();

        private MethodInfo _accessTokenMethodInfo = null;

        // Pushpay's documented API limits (https://pushpay.io/docs/guidance/rate_limiting): 10 a second,
        // 60 a minute, 500 an hour, 5,000 a day. Calls are paced under the per-minute limit, and a run
        // stops at a call budget below the hourly limit so "Pushpay Downloads", which uses the same
        // account, keeps headroom. A backfill simply continues on the next scheduled run.
        private TimeSpan _minCallInterval = TimeSpan.FromMilliseconds( 1100 );
        private int _maxCallsPerRun = 400;

        /// <summary>
        /// A 429 is retried after the retry-after wait Pushpay sends, plus an increasing backoff, as
        /// Pushpay's guidance asks. The run stops after this many retries in a row, or if Pushpay asks
        /// for a longer wait than <see cref="MaxRateLimitWait"/>.
        /// </summary>
        private const int MaxRateLimitRetries = 3;
        private static readonly TimeSpan MaxRateLimitWait = TimeSpan.FromMinutes( 5 );
        private static readonly TimeSpan DefaultRetryAfter = TimeSpan.FromSeconds( 60 );

        private const string DefaultApiUrl = "https://api.pushpay.com/";

        private readonly Stopwatch _sinceLastCall = new Stopwatch();
        private int _callCount = 0;
        private int _forbiddenCount = 0;
        private readonly Dictionary<int, string> _accountApiUrls = new Dictionary<int, string>();

        public override void Execute()
        {
            var rockContext = new RockContext();

            // Load all of the attributes
            var transactionSource = DefinedValueCache.Get( GetAttributeValue( "TransactionSource" ).AsGuid() );
            var currencyType = DefinedValueCache.Get( GetAttributeValue( "CurrencyType" ).AsGuid() );
            var checkNumberAttribute = AttributeCache.Get( GetAttributeValue( "CheckNumberAttribute" ).AsGuid() );
            DateRange dateRange = Rock.Web.UI.Controls.SlidingDateRangePicker.CalculateDateRangeFromDelimitedValues( GetAttributeValue( "DateRange" ) ?? "-1||" );

            FinancialTransactionService financialTransactionService = new FinancialTransactionService( rockContext );
            AttributeValueService attributeValueService = new AttributeValueService( rockContext );

            // Fetch any transactions that don't have check numbers
            var candidates = financialTransactionService.Queryable()
                                        .Where( ft => ft.SourceTypeValueId == transactionSource.Id
                                                      && ft.FinancialPaymentDetail.CurrencyTypeValueId == currencyType.Id
                                                      && ft.CreatedDateTime >= dateRange.Start
                                                      && ft.CreatedDateTime <= dateRange.End )
                                        .GroupJoin( attributeValueService.Queryable(),
                                            ft => new { EntityId = ( int? ) ft.Id, AttributeId = checkNumberAttribute.Id },
                                            av => new { av.EntityId, AttributeId = av.AttributeId },
                                            ( ft, av ) => new { Transaction = ft, CheckNumberAttributes = av } )
                                        // A blank attribute value is still "no check number" - there
                                        // are rows with an empty Value that the old Count() == 0
                                        // predicate treated as already done.
                                        .Where( ft => ft.CheckNumberAttributes.Where( av => av.Value != null && av.Value != "" ).Count() == 0 )
                                        // Newest first, so a long backfill fills in the current
                                        // statement quarter before the old gifts.
                                        .OrderByDescending( ft => ft.Transaction.CreatedDateTime )
                                        // Only the Id and the Pushpay payment token are needed; each
                                        // match is loaded and saved in its own short-lived context.
                                        .Select( ft => new { ft.Transaction.Id, ft.Transaction.ForeignKey } )
                                        .ToList();

            // Without a Pushpay payment token there is nothing to look up. These can never succeed,
            // so they are reported but not counted as errors (that would flag every run).
            int noPaymentToken = candidates.Count( c => c.ForeignKey.IsNullOrWhiteSpace() );
            var checkTransactions = candidates.Where( c => c.ForeignKey.IsNotNullOrWhiteSpace() ).ToList();

            int updates = 0;
            int pending = 0;
            int notFound = 0;
            int deleted = 0;
            int errors = 0;
            int processed = 0;
            int consecutiveFailedGifts = 0;
            var errorsByStatus = new Dictionary<string, int>();
            string stopReason = null;
            bool stoppedAtCallBudget = false;

            if ( checkTransactions.Count > 0 )
            {
                // First setup our PushPay Merchant data. Tokens are fetched per account, lazily.
                DataSet merchants = DbService.GetDataSet( "select AccountId, MerchantKey from _com_pushPay_RockRMS_Merchant", CommandType.Text, null );

                List<MerchantData> merchantDataList = new List<MerchantData>();

                foreach ( DataRow merchantRow in merchants.Tables[0].Rows )
                {
                    MerchantData data = new MerchantData();
                    data.AccountId = merchantRow["AccountId"].ToString().AsInteger();
                    data.MerchantKey = merchantRow["MerchantKey"].ToString();

                    merchantDataList.Add( data );
                }

                foreach ( var transaction in checkTransactions )
                {
                    bool resolved = false;
                    bool gotAnswer = false;
                    int giftErrors = 0;
                    string lastErrorStatus = null;

                    for ( int i = 0; i < merchantDataList.Count; i++ )
                    {
                        // Fetch the payment information from PushPay
                        PaymentResult paymentResult = FetchPayment( merchantDataList[i], transaction.ForeignKey );

                        if ( paymentResult.Outcome == CallOutcome.CallBudgetExhausted )
                        {
                            // Expected during a backfill, so this does not turn the run into a Warning.
                            stoppedAtCallBudget = true;
                            stopReason = string.Format( "Stopped after {0} Pushpay calls, this run's limit (Pushpay allows 500 an hour, shared with Pushpay Downloads). The next scheduled run will pick up where this one stopped.", _callCount );
                            break;
                        }

                        if ( paymentResult.Outcome == CallOutcome.RateLimited )
                        {
                            stopReason = "Stopped early: Pushpay kept returning HTTP 429 (rate limited) after waiting as it asked. The next scheduled run will pick up where this one stopped.";
                            break;
                        }

                        if ( paymentResult.Outcome == CallOutcome.Unauthorized )
                        {
                            stopReason = string.Format( "Stopped early at {0}: Pushpay rejected the access token even after requesting a new one. Check that the Pushpay account is still authorized in Rock. The next scheduled run will pick up where this one stopped.", RockDateTime.Now.ToString( "HH:mm" ) );
                            break;
                        }

                        if ( paymentResult.Outcome == CallOutcome.Error )
                        {
                            lastErrorStatus = paymentResult.StatusLabel ?? "Unknown";
                            giftErrors++;

                            if ( giftErrors >= MaxErrorsPerGift )
                            {
                                // Give up on this gift for now; the next run retries it.
                                break;
                            }

                            continue;
                        }

                        // A real answer from Pushpay (found, pending or not found).
                        gotAnswer = true;

                        if ( paymentResult.Outcome == CallOutcome.Found )
                        {
                            if ( SaveCheckNumber( transaction.Id, checkNumberAttribute.Key, paymentResult.CheckNumber ) )
                            {
                                updates++;
                            }
                            else
                            {
                                deleted++;
                            }

                            resolved = true;
                            PromoteMerchant( merchantDataList, i );
                            break;
                        }

                        if ( paymentResult.Outcome == CallOutcome.Pending )
                        {
                            // PushPay knows the payment, it just isn't deposited yet. Not an error.
                            pending++;
                            resolved = true;
                            PromoteMerchant( merchantDataList, i );
                            break;
                        }

                        // CallOutcome.NotFound - this payment belongs to another merchant, keep looking.
                    }

                    if ( stopReason.IsNotNullOrWhiteSpace() )
                    {
                        // This transaction was not resolved, so it isn't counted as processed -
                        // the next run picks it up.
                        break;
                    }

                    if ( !resolved )
                    {
                        if ( lastErrorStatus != null )
                        {
                            // One entry per failed gift, so the status breakdown adds up to Errors.
                            errors++;
                            AddErrorStatus( errorsByStatus, lastErrorStatus );
                        }
                        else
                        {
                            notFound++;
                        }
                    }

                    processed++;

                    // Only gifts where Pushpay never answered count toward the outage check; any
                    // real answer shows Pushpay is up and resets the streak.
                    if ( gotAnswer )
                    {
                        consecutiveFailedGifts = 0;
                    }
                    else if ( lastErrorStatus != null )
                    {
                        consecutiveFailedGifts++;

                        if ( consecutiveFailedGifts >= MaxConsecutiveFailedGifts )
                        {
                            stopReason = string.Format( "Stopped early: {0} gifts in a row got only errors from Pushpay (last: {1}), so Pushpay may be unavailable. The next scheduled run will pick up where this one stopped.", consecutiveFailedGifts, lastErrorStatus );
                            break;
                        }
                    }
                }
            }

            string result = string.Format( "Updated {0}. Pending {1} (no check number yet). Not found {2}. Errors {3}", updates, pending, notFound, errors );

            if ( errorsByStatus.Count > 0 )
            {
                var breakdown = errorsByStatus.OrderByDescending( e => e.Value ).Select( e => string.Format( "{0} x {1}", e.Key, e.Value ) );
                result += " [by status: " + string.Join( ", ", breakdown ) + "]";
            }

            result += string.Format( ". Processed {0} of {1}.", processed, checkTransactions.Count );

            if ( deleted > 0 )
            {
                result += string.Format( " {0} deleted during the run, so nothing was saved for them.", deleted );
            }

            if ( noPaymentToken > 0 )
            {
                result += string.Format( " Skipped {0} with no Pushpay payment token.", noPaymentToken );
            }

            if ( _forbiddenCount > 0 )
            {
                // Pushpay does not document whether another merchant's payment returns 403 or 404, so a
                // 403 is treated as "not at this merchant". Report it, so a real permission problem shows.
                result += string.Format( " Pushpay answered 403 on {0} of {1} calls (treated as not at that merchant).", _forbiddenCount, _callCount );
            }

            if ( stopReason.IsNotNullOrWhiteSpace() )
            {
                result += " " + stopReason;
            }

            Result = result;

            if ( errors > 0 || ( stopReason.IsNotNullOrWhiteSpace() && !stoppedAtCallBudget ) )
            {
                // RockJobListener uses this.Result as the status message for a warning exception,
                // so the job reports Warning with the summary instead of plain success.
                throw new RockJobWarningException( Result );
            }
        }

        /// <summary>
        /// Writes the check number to one transaction in its own context, so a long run doesn't
        /// accumulate every transaction and its attributes in a single change tracker.
        /// </summary>
        private static bool SaveCheckNumber( int transactionId, string attributeKey, string checkNumber )
        {
            using ( var rockContext = new RockContext() )
            {
                var transaction = new FinancialTransactionService( rockContext ).Get( transactionId );
                if ( transaction == null )
                {
                    // Deleted since the candidate list was built.
                    return false;
                }

                transaction.LoadAttributes( rockContext );
                transaction.SetAttributeValue( attributeKey, checkNumber );
                transaction.SaveAttributeValues( rockContext );
                return true;
            }
        }

        private static void AddErrorStatus( Dictionary<string, int> errorsByStatus, string statusLabel )
        {
            if ( statusLabel.IsNullOrWhiteSpace() )
            {
                statusLabel = "Unknown";
            }

            int count;
            errorsByStatus.TryGetValue( statusLabel, out count );
            errorsByStatus[statusLabel] = count + 1;
        }

        /// <summary>
        /// Moves the merchant that just answered to the front. Nothing in Rock records which
        /// merchant owns a payment, so trying the last one that worked turns the typical cost from
        /// 19 calls per transaction into one or two.
        /// </summary>
        private static void PromoteMerchant( List<MerchantData> merchantDataList, int index )
        {
            if ( index > 0 )
            {
                MerchantData merchantData = merchantDataList[index];
                merchantDataList.RemoveAt( index );
                merchantDataList.Insert( 0, merchantData );
            }
        }

        /// <summary>
        /// Gets the OAuth token for a PushPay account, re-requesting it once it has actually
        /// expired. The token is minted by PushPay's own "Pushpay Downloads" job (~05:15, 60 minute
        /// life) and this job starts at 05:52, so it goes stale mid-run; the old code fetched it
        /// once and then silently failed every call after ~06:15.
        /// </summary>
        private string FetchAccessToken( int accountId, bool forceRefresh )
        {
            string cachedToken;
            DateTime? tokenExpires;
            _accountTokens.TryGetValue( accountId, out cachedToken );
            _accountTokenExpires.TryGetValue( accountId, out tokenExpires );

            // TokenExpires is stored in Rock server local time, so compare against RockDateTime.Now
            // (Rock's configured timezone) rather than DateTime.Now or DateTime.UtcNow.
            if ( !forceRefresh && cachedToken.IsNotNullOrWhiteSpace() && tokenExpires.HasValue && RockDateTime.Now < tokenExpires.Value )
            {
                return cachedToken;
            }

            string oAuthToken = InvokeGetAccessToken( accountId );

            // The DLL only renews a token once its stored expiry has passed. If Pushpay rejected the
            // token a little early (clock skew), the DLL hands back the same one; wait out the few
            // remaining seconds and ask again rather than ending the run.
            if ( forceRefresh && oAuthToken == cachedToken )
            {
                DateTime? storedExpires = GetTokenExpires( accountId );
                if ( storedExpires.HasValue )
                {
                    TimeSpan wait = storedExpires.Value - RockDateTime.Now + TimeSpan.FromSeconds( 5 );
                    if ( wait > TimeSpan.Zero && wait <= MaxTokenExpiryWait )
                    {
                        Thread.Sleep( wait );
                        oAuthToken = InvokeGetAccessToken( accountId );
                    }
                }
            }

            _accountTokens[accountId] = oAuthToken;
            _accountTokenExpires[accountId] = GetTokenExpires( accountId );

            return oAuthToken;
        }

        private string InvokeGetAccessToken( int accountId )
        {
            if ( _accessTokenMethodInfo == null )
            {
                Assembly assembly = Assembly.LoadFrom( System.Web.Hosting.HostingEnvironment.MapPath( "~/bin/com.pushpay.RockRMS.dll" ) );
                Type pushpayApiType = assembly.GetType( "com.pushpay.RockRMS.PushpayApi" );
                _accessTokenMethodInfo = pushpayApiType.GetMethod( "GetAccessToken" );
            }

            return Convert.ToString( _accessTokenMethodInfo.Invoke( null, new object[] { accountId } ) );
        }

        /// <summary>
        /// Reads the stored expiry for a PushPay account. Deliberately selects only TokenExpires -
        /// the AccessToken / RefreshToken columns are never read or logged by this job.
        /// </summary>
        private static DateTime? GetTokenExpires( int accountId )
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add( "@Id", accountId );

            object tokenExpires = DbService.ExecuteScalar(
                "SELECT TokenExpires FROM _com_pushPay_RockRMS_Account WHERE Id = @Id",
                CommandType.Text,
                parameters );

            if ( tokenExpires == null || tokenExpires == DBNull.Value )
            {
                return null;
            }

            return tokenExpires as DateTime?;
        }

        /// <summary>
        /// Calls PushPay for one payment against one merchant. A 401 gets one forced token refresh
        /// and one retry; a 429 waits as Pushpay asks and retries (see <see cref="MaxRateLimitRetries"/>);
        /// anything thrown (HttpClient timeouts included) becomes an Error outcome so a single bad call
        /// can't end the run the way task.Wait() used to.
        /// </summary>
        private PaymentResult FetchPayment( MerchantData merchantData, string paymentToken )
        {
            int attempt = 0;
            int rateLimitRetries = 0;

            while ( true )
            {
                if ( _callCount >= _maxCallsPerRun )
                {
                    return new PaymentResult { Outcome = CallOutcome.CallBudgetExhausted };
                }

                PaymentResult paymentResult;

                try
                {
                    string oAuthToken = FetchAccessToken( merchantData.AccountId, attempt > 0 );
                    WaitForCallSlot();
                    _callCount++;
                    paymentResult = GetPayment( GetApiUrl( merchantData.AccountId ), oAuthToken, merchantData.MerchantKey, paymentToken ).GetAwaiter().GetResult();
                }
                catch ( Exception ex )
                {
                    return new PaymentResult { Outcome = CallOutcome.Error, StatusLabel = ex.GetBaseException().GetType().Name };
                }

                if ( paymentResult.Outcome == CallOutcome.RateLimited )
                {
                    // Pushpay's guidance: wait the retry-after value plus an increasing backoff (1s, 2s, 4s).
                    rateLimitRetries++;
                    TimeSpan wait = ( paymentResult.RetryAfter ?? DefaultRetryAfter ) + TimeSpan.FromSeconds( Math.Pow( 2, rateLimitRetries - 1 ) );
                    if ( rateLimitRetries > MaxRateLimitRetries || wait > MaxRateLimitWait )
                    {
                        return paymentResult;
                    }

                    Thread.Sleep( wait );
                    continue;
                }

                if ( paymentResult.Outcome == CallOutcome.Unauthorized && attempt == 0 )
                {
                    // The token expired mid-run; get a fresh one and retry this one call.
                    attempt++;
                    continue;
                }

                if ( paymentResult.StatusLabel == "403" )
                {
                    _forbiddenCount++;
                }

                return paymentResult;
            }
        }

        /// <summary>
        /// Keeps calls at least <see cref="_minCallInterval"/> apart, under Pushpay's per-minute limit.
        /// </summary>
        private void WaitForCallSlot()
        {
            if ( _sinceLastCall.IsRunning )
            {
                TimeSpan remaining = _minCallInterval - _sinceLastCall.Elapsed;
                if ( remaining > TimeSpan.Zero )
                {
                    Thread.Sleep( remaining );
                }
            }

            _sinceLastCall.Restart();
        }

        /// <summary>
        /// The API base URL stored on the Pushpay account (e.g. https://api.pushpay.com/), so the job
        /// follows the account's configuration, sandbox included, instead of a hard-coded host.
        /// </summary>
        private string GetApiUrl( int accountId )
        {
            string apiUrl;
            if ( !_accountApiUrls.TryGetValue( accountId, out apiUrl ) )
            {
                var parameters = new Dictionary<string, object>();
                parameters.Add( "@Id", accountId );

                apiUrl = Convert.ToString( DbService.ExecuteScalar(
                    "SELECT ApiUrl FROM _com_pushPay_RockRMS_Account WHERE Id = @Id",
                    CommandType.Text,
                    parameters ) );

                if ( apiUrl.IsNullOrWhiteSpace() )
                {
                    apiUrl = DefaultApiUrl;
                }

                _accountApiUrls[accountId] = apiUrl;
            }

            return apiUrl;
        }


        /// <summary>
        ///	Get Payment
        /// </summary>
        /// <remarks>
        ///	Get a payment details from a payment token
        /// </remarks>
        private static async Task<PaymentResult> GetPayment( string apiUrl, string oAuthToken, string merchantKey, string paymentToken )
        {
            string baseUrl = apiUrl.TrimEnd( '/' );
            if ( !baseUrl.EndsWith( "/v1", StringComparison.OrdinalIgnoreCase ) )
            {
                baseUrl += "/v1";
            }

            var requestUrl = string.Format( "{0}/merchant/{1}/payment/{2}", baseUrl, merchantKey, paymentToken );

            // The bearer token rides on the request, not on DefaultRequestHeaders - the HttpClient
            // is shared and its default headers must not be mutated per call.
            using ( var request = new HttpRequestMessage( HttpMethod.Get, requestUrl ) )
            {
                request.Headers.Accept.Add( new MediaTypeWithQualityHeaderValue( "application/json" ) );
                request.Headers.Authorization = new AuthenticationHeaderValue( "Bearer", oAuthToken );

                using ( var httpResponse = await _httpClient.SendAsync( request ) )
                {
                    int statusCode = ( int ) httpResponse.StatusCode;

                    if ( statusCode == 401 )
                    {
                        return new PaymentResult { Outcome = CallOutcome.Unauthorized, StatusLabel = "401" };
                    }

                    if ( statusCode == 429 )
                    {
                        TimeSpan? retryAfter = null;
                        var retryAfterHeader = httpResponse.Headers.RetryAfter;
                        if ( retryAfterHeader != null )
                        {
                            if ( retryAfterHeader.Delta.HasValue )
                            {
                                retryAfter = retryAfterHeader.Delta.Value;
                            }
                            else if ( retryAfterHeader.Date.HasValue )
                            {
                                retryAfter = retryAfterHeader.Date.Value - DateTimeOffset.UtcNow;
                            }

                            if ( retryAfter < TimeSpan.Zero )
                            {
                                retryAfter = TimeSpan.Zero;
                            }
                        }

                        return new PaymentResult { Outcome = CallOutcome.RateLimited, StatusLabel = "429", RetryAfter = retryAfter };
                    }

                    if ( statusCode == 404 || statusCode == 403 )
                    {
                        // This merchant doesn't know the payment; the caller tries the next one. Pushpay
                        // doesn't document which of the two it returns for another merchant's payment.
                        return new PaymentResult { Outcome = CallOutcome.NotFound, StatusLabel = statusCode.ToString() };
                    }

                    if ( !httpResponse.IsSuccessStatusCode )
                    {
                        return new PaymentResult { Outcome = CallOutcome.Error, StatusLabel = statusCode.ToString() };
                    }

                    var content = await httpResponse.Content.ReadAsStringAsync();

                    var response = JsonConvert.DeserializeObject<Response>(
                        content, new JsonSerializerSettings
                        {
                            ContractResolver = new CamelCasePropertyNamesContractResolver()
                        } );

                    string checkNumber = response?.DepositedCheck?.CheckNumber;

                    if ( checkNumber.IsNotNullOrWhiteSpace() )
                    {
                        return new PaymentResult { Outcome = CallOutcome.Found, CheckNumber = checkNumber };
                    }

                    return new PaymentResult { Outcome = CallOutcome.Pending };
                }
            }
        }

        private enum CallOutcome
        {
            Found,
            Pending,
            NotFound,
            Unauthorized,
            RateLimited,
            CallBudgetExhausted,
            Error
        }

        private class PaymentResult
        {
            public CallOutcome Outcome { get; set; }
            public string CheckNumber { get; set; }
            public string StatusLabel { get; set; }
            public TimeSpan? RetryAfter { get; set; }
        }
    }

    class MerchantData
    {
        public int AccountId { get; set; }
        public string MerchantKey { get; set; }
    }

    public class Response
    {
        public string PaymentMethodType { get; set; }
        public string Source { get; set; }
        public DepositedCheck DepositedCheck { get; set; }
        [JsonIgnore]
        public string Error { get; set; }
    }

    public class DepositedCheck
    {
        public string RoutingNumber { get; set; }
        public string Reference { get; set; }
        public string BankName { get; set; }
        public string CheckNumber { get; set; }
    }
}

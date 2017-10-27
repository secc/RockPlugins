using Rock.Attribute;
using Rock.Financial;
using Rock.Model;
using Rock.Web.Cache;
using Rock;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using PayPal.PayPalAPIInterfaceService.Model;
using PayPal.PayPalAPIInterfaceService;
using Rock.Security;
using Rock.Web;

namespace org.secc.PayPalExpress
{
    /// <summary>
    /// Paypal Express Payment Gateway
    /// </summary>
    [Description("PayPal Express Gateway")]
    [Export(typeof(GatewayComponent))]
    [ExportMetadata("ComponentName", "PayPal Express Gateway")]

    [EncryptedTextField("PayPal API Username", "Username for authenticating to the PayPal API", true, "", "PayPal Settings", 1)]
    [EncryptedTextField("PayPal API Password", "Password for authenticating to the PayPal API", true, "", "PayPal Settings", 2)]
    [EncryptedTextField("PayPal API Signature", "Signature for authenticating to the PayPal API", true, "", "PayPal Settings", 3)]
    [CustomDropdownListField("PayPal Environment", "Sandbox for test, Live for production", "Live,Sandbox", true, "Sandbox", "PayPal Settings", 4)]
    [TextField("PayPal URL", "Base URL for the PayPal Redirect.", true, "https://www.sandbox.paypal.com/cgi-bin/webscr?cmd=", "PayPal Urls", 0)]
    [LinkedPage("Return Page", "Page for handling the return request from PayPal.  More documentation is available here: https://developer.paypal.com/docs/classic/express-checkout/integration-guide/ECGettingStarted/#setting-up-the-express-checkout-transaction", true, "1615E090-1889-42FF-AB18-5F7BE9F24498", "PayPal Urls", 1)]
    [LinkedPage("Cancel Page", "Page for handling a PayPal cancellation.  More documentation is available here: https://developer.paypal.com/docs/classic/express-checkout/integration-guide/ECGettingStarted/#setting-up-the-express-checkout-transaction", true, "1615E090-1889-42FF-AB18-5F7BE9F24498", "PayPal Urls", 2)]
    [TextField("PayPal Brand Name", "(Optional) A label that overrides the business name in the PayPal account on the PayPal hosted checkout pages.", false, "", "PayPal Look & Feel", 1)]
    [TextField("PayPal Logo Image", "(Optional) A URL to your logo image. Use a valid graphics format, such as .gif, .jpg, or.png.Limit the image to 190 pixels wide by 60 pixels high.PayPal crops images that are larger.PayPal places your logo image at the top of the cart review area.", false, "", "PayPal Look & Feel", 1)]
    public class Gateway : RedirectGatewayComponent
    {

        public const string CURRENCY_TYPE_PAYPAL = "2D6FC5FA-A49F-4D20-BCDF-2F0D7E67AD86";
        
        #region Gateway Component Implementation

        /// <summary>
        /// Gets the supported payment schedules.
        /// </summary>
        /// <value>
        /// The supported payment schedules.
        /// </value>
        public override List<DefinedValueCache> SupportedPaymentSchedules
        {
            get
            {
                var values = new List<DefinedValueCache>();
                values.Add(DefinedValueCache.Read(Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_ONE_TIME));
                return values;
            }
        }

        private String mRedirectUrl = "";
        public override string RedirectUrl
        {
            get
            {
                return mRedirectUrl;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the gateway requires the name on card for CC processing
        /// </summary>
        /// <param name="financialGateway">The financial gateway.</param>
        /// <returns></returns>
        /// <value>
        ///   <c>true</c> if [name on card required]; otherwise, <c>false</c>.
        /// </value>
        public override bool PromptForNameOnCard(FinancialGateway financialGateway)
        {
            return false;
        }

        /// <summary>
        /// Prompts the name of for bank account.
        /// </summary>
        /// <param name="financialGateway">The financial gateway.</param>
        /// <returns></returns>
        public override bool PromptForBankAccountName(FinancialGateway financialGateway)
        {
            return false;
        }

        /// <summary>
        /// Gets a value indicating whether [address required].
        /// </summary>
        /// <param name="financialGateway">The financial gateway.</param>
        /// <returns></returns>
        /// <value>
        ///   <c>true</c> if [address required]; otherwise, <c>false</c>.
        /// </value>
        public override bool PromptForBillingAddress(FinancialGateway financialGateway)
        {
            return false;
        }

        /// <summary>
        /// Charges the specified payment info.
        /// </summary>
        /// <param name="financialGateway">The financial gateway.</param>
        /// <param name="paymentInfo">The payment info.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override FinancialTransaction Charge(FinancialGateway financialGateway, PaymentInfo paymentInfo, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!(paymentInfo is PayPalExpress.PayPalPaymentInfo))
            {
                errorMessage = "PaymentInfo object must be of type PayPalPaymentInfo in order to charge a PayPal Express transaction.";
                return null;
            }
            PayPalPaymentInfo payPalPaymentInfo = (PayPalPaymentInfo)paymentInfo;

            // Create the DoExpressCheckoutPaymentResponseType object
            DoExpressCheckoutPaymentResponseType responseDoExpressCheckoutPaymentResponseType = new DoExpressCheckoutPaymentResponseType();

            try
            {
                // Create the DoExpressCheckoutPaymentReq object
                DoExpressCheckoutPaymentReq doExpressCheckoutPayment = new DoExpressCheckoutPaymentReq();

                DoExpressCheckoutPaymentRequestDetailsType doExpressCheckoutPaymentRequestDetails = new DoExpressCheckoutPaymentRequestDetailsType();

                // The timestamped token value that was returned in the
                // `SetExpressCheckout` response and passed in the
                // `GetExpressCheckoutDetails` request.
                doExpressCheckoutPaymentRequestDetails.Token = payPalPaymentInfo.Token;

                // Unique paypal buyer account identification number as returned in
                // `GetExpressCheckoutDetails` Response
                doExpressCheckoutPaymentRequestDetails.PayerID = payPalPaymentInfo.PayerId;

                // # Payment Information
                // list of information about the payment
                List<PaymentDetailsType> paymentDetailsList = new List<PaymentDetailsType>();

                // information about the first payment
                PaymentDetailsType paymentDetails = new PaymentDetailsType();
               
                BasicAmountType orderTotal = new BasicAmountType(CurrencyCodeType.USD, paymentInfo.Amount.ToString());
                paymentDetails.OrderTotal = orderTotal;

                // We are actually capturing this payment now.
                paymentDetails.PaymentAction = PaymentActionCodeType.SALE;

                // Unique identifier for the merchant. For parallel payments, this field
                // is required and must contain the Payer Id or the email address of the
                // merchant.
                String apiUsername = Encryption.DecryptString(financialGateway.GetAttributeValue("PayPalAPIUsername"));
                SellerDetailsType sellerDetails = new SellerDetailsType();
                sellerDetails.PayPalAccountID = apiUsername;
            
                paymentDetailsList.Add(paymentDetails);
                doExpressCheckoutPaymentRequestDetails.PaymentDetails = paymentDetailsList;

                DoExpressCheckoutPaymentRequestType doExpressCheckoutPaymentRequest = new DoExpressCheckoutPaymentRequestType(doExpressCheckoutPaymentRequestDetails);
                doExpressCheckoutPayment.DoExpressCheckoutPaymentRequest = doExpressCheckoutPaymentRequest;

                // Create the service wrapper object to make the API call
                PayPalAPIInterfaceServiceService service = new PayPalAPIInterfaceServiceService(GetCredentials(financialGateway));

                // # API call
                // Invoke the DoExpressCheckoutPayment method in service wrapper object
                responseDoExpressCheckoutPaymentResponseType = service.DoExpressCheckoutPayment(doExpressCheckoutPayment);

                if (responseDoExpressCheckoutPaymentResponseType != null)
                {
                    // # Success values
                    if (responseDoExpressCheckoutPaymentResponseType.Ack.ToString().Trim().ToUpper().Equals("SUCCESS"))
                    {
                        // Transaction identification number of the transaction that was
                        // created.
                        // This field is only returned after a successful transaction
                        // for DoExpressCheckout has occurred.
                        if (responseDoExpressCheckoutPaymentResponseType.DoExpressCheckoutPaymentResponseDetails.PaymentInfo != null)
                        {
                            IEnumerator<PaymentInfoType> paymentInfoIterator = responseDoExpressCheckoutPaymentResponseType.DoExpressCheckoutPaymentResponseDetails.PaymentInfo.GetEnumerator();
                            while (paymentInfoIterator.MoveNext())
                            {
                                PaymentInfoType ppPaymentInfo = paymentInfoIterator.Current;

                                var transaction = new FinancialTransaction();
                                transaction.TransactionCode = ppPaymentInfo.TransactionID;
                                return transaction;
                            }
                        }
                    }
                    // # Error Values
                    else
                    {
                        List<ErrorType> errorMessages = responseDoExpressCheckoutPaymentResponseType.Errors;
                        foreach (ErrorType error in errorMessages)
                        {
                            errorMessage += "API Error Message : " + error.LongMessage + "\n";
                        }
                    }
                }
            }
            // # Exception log    
            catch (System.Exception ex)
            {
                errorMessage += "Error Message : " + ex.Message;
            }

            return null;
        }

        public PaymentInfo GetPaymentInfo(FinancialGateway financialGateway, String token, out string errorMessage)
        {
            errorMessage = string.Empty;
            GetExpressCheckoutDetailsResponseType response = GetExpressCheckoutDetailsResponse(financialGateway, token, out errorMessage);
            if (response == null)
            {
                return null;
            }
            
            var details = response.GetExpressCheckoutDetailsResponseDetails;
            var paymentDetails = details.PaymentDetails.FirstOrDefault();
            PayPalPaymentInfo paymentInfo = new PayPalPaymentInfo();
            paymentInfo.Amount = paymentDetails.OrderTotal.value.AsDecimal();
            paymentInfo.Token = token;
            paymentInfo.PayerId = details.PayerInfo.PayerID;
            paymentInfo.City = details.PayerInfo.Address.CityName;
            paymentInfo.State = details.PayerInfo.Address.StateOrProvince;
            paymentInfo.Street1 = details.PayerInfo.Address.Street1;
            paymentInfo.Street2 = details.PayerInfo.Address.Street2;
            paymentInfo.PostalCode = details.PayerInfo.Address.PostalCode;
            paymentInfo.Country = details.PayerInfo.Address.CountryName;
            paymentInfo.Phone = details.PayerInfo.ContactPhone;
            paymentInfo.Email = details.PayerInfo.Payer;
            paymentInfo.FirstName = details.PayerInfo.PayerName.FirstName;
            paymentInfo.LastName = details.PayerInfo.PayerName.LastName;
            return paymentInfo;
        }

        public List<GatewayAccountItem> GetSelectedAccounts(FinancialGateway financialGateway, String token, out string errorMessage)
        {
            errorMessage = string.Empty;
            GetExpressCheckoutDetailsResponseType response = GetExpressCheckoutDetailsResponse(financialGateway, token, out errorMessage);
            if (response == null)
            {
                return null;
            }
            var details = response.GetExpressCheckoutDetailsResponseDetails;
            var paymentDetails = details.PaymentDetails.FirstOrDefault();
            List<GatewayAccountItem> accounts = new List<GatewayAccountItem>();
            foreach(PaymentDetailsItemType item in paymentDetails.PaymentDetailsItem) { 
                GatewayAccountItem account = new GatewayAccountItem();
                account.Id = item.Number.AsInteger();
                account.Name = item.Name;
                account.PublicName = item.Description;
                account.Amount = item.Amount.value.AsDecimal();
                accounts.Add(account);
            }
            return accounts;
        }



        /// <summary>
        /// Credits the specified transaction.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="amount">The amount.</param>
        /// <param name="comment">The comment.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override FinancialTransaction Credit(FinancialTransaction transaction, decimal amount, string comment, out string errorMessage)
        {

            errorMessage = string.Empty;

            if (transaction == null ||
                string.IsNullOrWhiteSpace(transaction.TransactionCode) ||
                transaction.FinancialGateway == null)
            {
                errorMessage = "Invalid original transaction, transaction code, or gateway.";
                return null;
            }

            // Create the RefundTransactionResponseType object
            RefundTransactionResponseType responseRefundTransactionResponseType = new RefundTransactionResponseType();

            try
            {
                // Create the RefundTransactionReq object
                RefundTransactionReq refundTransaction = new RefundTransactionReq();
                RefundTransactionRequestType refundTransactionRequest = new RefundTransactionRequestType();

                refundTransactionRequest.TransactionID = transaction.TransactionCode;
                refundTransactionRequest.RefundType = RefundType.FULL;

                // Set the amount
                refundTransactionRequest.Amount = new BasicAmountType(CurrencyCodeType.USD, amount.ToString());

                refundTransaction.RefundTransactionRequest = refundTransactionRequest;
                
                // Create the service wrapper object to make the API call
                PayPalAPIInterfaceServiceService service = new PayPalAPIInterfaceServiceService(GetCredentials(transaction.FinancialGateway));

                // # API call
                // Invoke the RefundTransaction method in service wrapper object
                responseRefundTransactionResponseType = service.RefundTransaction(refundTransaction);

                if (responseRefundTransactionResponseType != null)
                {


                    // # Success values
                    if (responseRefundTransactionResponseType.Ack.ToString().Trim().ToUpper().Equals("SUCCESS"))
                    {
                        var refundFinancialTransaction = new FinancialTransaction();
                        refundFinancialTransaction.TransactionCode = responseRefundTransactionResponseType.RefundTransactionID;
                        return refundFinancialTransaction;
                    }
                    // # Error Values
                    else
                    {
                        List<ErrorType> errorMessages = responseRefundTransactionResponseType.Errors;
                        foreach (ErrorType error in errorMessages)
                        {
                            errorMessage = string.Format("[{0}] {1}", responseRefundTransactionResponseType.Ack, error.LongMessage);
                        }
                    }
                }
                else
                {
                    errorMessage = "Invalid transaction response from the financial gateway";
                }
            }
            catch (System.Exception ex)
            {
                // Log the exception message       
                errorMessage = ex.Message;
            }
            return null;
        }

        /// <summary>
        /// Adds the scheduled payment.
        /// </summary>
        /// <param name="financialGateway">The financial gateway.</param>
        /// <param name="schedule">The schedule.</param>
        /// <param name="paymentInfo">The payment info.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override FinancialScheduledTransaction AddScheduledPayment(FinancialGateway financialGateway, PaymentSchedule schedule, PaymentInfo paymentInfo, out string errorMessage)
        {
            // Just do nothing here.  It's not supported!
            errorMessage = "";
            return null;
        }

        /// <summary>
        /// Reactivates the scheduled payment.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override bool ReactivateScheduledPayment(FinancialScheduledTransaction transaction, out string errorMessage)
        {
            // Just do nothing here.  It's not supported!
            errorMessage = "";
            return false;
        }

        /// <summary>
        /// Updates the scheduled payment.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="paymentInfo">The payment info.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override bool UpdateScheduledPayment(FinancialScheduledTransaction transaction, PaymentInfo paymentInfo, out string errorMessage)
        {
            // Just do nothing here.  It's not supported!
            errorMessage = "";
            return false;
        }

        /// <summary>
        /// Cancels the scheduled payment.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override bool CancelScheduledPayment(FinancialScheduledTransaction transaction, out string errorMessage)
        {
            // Just do nothing here.  It's not supported!
            errorMessage = "";
            return false;
        }

        /// <summary>
        /// Gets the scheduled payment status.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override bool GetScheduledPaymentStatus(FinancialScheduledTransaction transaction, out string errorMessage)
        {
            // Just do nothing here.  It's not supported!
            errorMessage = "";
            return false;
        }

        /// <summary>
        /// Gets the payments that have been processed for any scheduled transactions
        /// </summary>
        /// <param name="financialGateway">The financial gateway.</param>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override List<Payment> GetPayments(FinancialGateway financialGateway, DateTime startDate, DateTime endDate, out string errorMessage)
        {
            // Just do nothing here.  It's not supported!
            errorMessage = "";
            return new List<Payment>();
            ;
        }

        /// <summary>
        /// Gets an optional reference identifier needed to process future transaction from saved account.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override string GetReferenceNumber(FinancialTransaction transaction, out string errorMessage)
        {
            errorMessage = string.Empty;
            return string.Empty;
        }

        /// <summary>
        /// Gets an optional reference identifier needed to process future transaction from saved account.
        /// </summary>
        /// <param name="scheduledTransaction">The scheduled transaction.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override string GetReferenceNumber(FinancialScheduledTransaction scheduledTransaction, out string errorMessage)
        {
            errorMessage = string.Empty;
            return string.Empty;
        }

        public override void PreRedirect(FinancialGateway financialGateway, PaymentInfo paymentInfo, List<GatewayAccountItem> selectedAccounts, out string errorMessage)
        {
            // Create request object
            SetExpressCheckoutRequestType request = populateRequestObject(financialGateway, paymentInfo, selectedAccounts);

            // Invoke the API
            SetExpressCheckoutReq wrapper = new SetExpressCheckoutReq();
            wrapper.SetExpressCheckoutRequest = request;


            // Create the PayPalAPIInterfaceServiceService service object to make the API call
            PayPalAPIInterfaceServiceService service = new PayPalAPIInterfaceServiceService(GetCredentials(financialGateway));

            // # API call 
            // Invoke the SetExpressCheckout method in service wrapper object  
            SetExpressCheckoutResponseType setECResponse = service.SetExpressCheckout(wrapper);

            // Check for API return status
            String url = financialGateway.GetAttributeValue("PayPalURL")
                    + "_express-checkout&token=" + setECResponse.Token;
            mRedirectUrl = url;
            errorMessage = string.Empty;
            return;
        }

        private GetExpressCheckoutDetailsResponseType GetExpressCheckoutDetailsResponse(FinancialGateway financialGateway, String token, out string errorMessage)
        {
            errorMessage = string.Empty;
            

            // Create the GetExpressCheckoutDetailsResponseType object
            GetExpressCheckoutDetailsResponseType responseGetExpressCheckoutDetailsResponseType = new GetExpressCheckoutDetailsResponseType();

            try
            {
                // Create the GetExpressCheckoutDetailsReq object
                GetExpressCheckoutDetailsReq getExpressCheckoutDetails = new GetExpressCheckoutDetailsReq();

                // A timestamped token, the value of which was returned by `SetExpressCheckout` response
                GetExpressCheckoutDetailsRequestType getExpressCheckoutDetailsRequest = new GetExpressCheckoutDetailsRequestType(token);
                getExpressCheckoutDetails.GetExpressCheckoutDetailsRequest = getExpressCheckoutDetailsRequest;

                // Create the service wrapper object to make the API call
                PayPalAPIInterfaceServiceService service = new PayPalAPIInterfaceServiceService(GetCredentials(financialGateway));

                // # API call
                // Invoke the GetExpressCheckoutDetails method in service wrapper object
                responseGetExpressCheckoutDetailsResponseType = service.GetExpressCheckoutDetails(getExpressCheckoutDetails);

                if (responseGetExpressCheckoutDetailsResponseType != null)
                {
                    // # Success values
                    if (responseGetExpressCheckoutDetailsResponseType.Ack.ToString().Trim().ToUpper().Equals("SUCCESS"))
                    {
                        return responseGetExpressCheckoutDetailsResponseType;
                    }
                    // # Error Values
                    else
                    {
                        List<ErrorType> errorMessages = responseGetExpressCheckoutDetailsResponseType.Errors;
                        foreach (ErrorType error in errorMessages)
                        {
                            errorMessage += "API Error Message : " + error.LongMessage + "\n";
                        }
                    }
                }
            }
            // # Exception log    
            catch (System.Exception ex)
            {
                errorMessage += "Error Message : " + ex.Message;
            }
            return null;
        }

        private SetExpressCheckoutRequestType populateRequestObject(FinancialGateway financialGateway, PaymentInfo paymentInfo, List<GatewayAccountItem> selectedAccounts)
        {
            SetExpressCheckoutRequestType request = new SetExpressCheckoutRequestType();
            SetExpressCheckoutRequestDetailsType ecDetails = new SetExpressCheckoutRequestDetailsType();
            String host = GlobalAttributesCache.Value("PublicApplicationRoot");
            int lastSlash = host.LastIndexOf('/');
            host = (lastSlash > -1) ? host.Substring(0, lastSlash) : host;
            if (financialGateway.GetAttributeValue("ReturnPage") != string.Empty)
            {
                PageReference pageReference = new PageReference(financialGateway.GetAttributeValue("ReturnPage"));
                ecDetails.ReturnURL = host + pageReference.BuildUrl();
            }
            if (financialGateway.GetAttributeValue("CancelPage") != string.Empty)
            {
                PageReference pageReference = new PageReference(financialGateway.GetAttributeValue("CancelPage"));
                ecDetails.CancelURL = host + pageReference.BuildUrl();
            }
            /*
            // (Optional) Email address of the buyer as entered during checkout. PayPal uses this value to pre-fill the PayPal membership sign-up portion on the PayPal pages.
            if (buyerEmail.Value != string.Empty)
            {
                
            }*/
            ecDetails.BuyerEmail = paymentInfo.Email;

            /* Populate payment requestDetails. 
             * SetExpressCheckout allows parallel payments of upto 10 payments. 
             * This samples shows just one payment.
             */
            PaymentDetailsType paymentDetails = new PaymentDetailsType();
            ecDetails.PaymentDetails.Add(paymentDetails);
            // (Required) Total cost of the transaction to the buyer. If shipping cost and tax charges are known, include them in this value. If not, this value should be the current sub-total of the order. If the transaction includes one or more one-time purchases, this field must be equal to the sum of the purchases. Set this field to 0 if the transaction does not include a one-time purchase such as when you set up a billing agreement for a recurring payment that is not immediately charged. When the field is set to 0, purchase-specific fields are ignored.
            double orderTotal = 0.0;
            // Sum of cost of all items in this order. For digital goods, this field is required.
            double itemTotal = Convert.ToDouble(paymentInfo.Amount);
            CurrencyCodeType currency = CurrencyCodeType.USD;
            
            //(Optional) Description of items the buyer is purchasing.
            paymentDetails.OrderDescription = "Contribution";

            // We do a authorization then complete the sale in the "Charge" phase
            paymentDetails.PaymentAction = PaymentActionCodeType.AUTHORIZATION;
            foreach( GatewayAccountItem item in selectedAccounts )
            {
                if (item.Amount > 0) { 
                    PaymentDetailsItemType itemDetails = new PaymentDetailsItemType();

                    itemDetails.Name = item.Name;
                    itemDetails.Amount = new BasicAmountType(currency, item.Amount.ToString());
                    itemDetails.Quantity = 1;
                    itemDetails.Description = item.PublicName;
                    itemDetails.Number = item.Id.ToString();
                    paymentDetails.PaymentDetailsItem.Add(itemDetails);
                }
            }

            orderTotal += itemTotal;
            paymentDetails.ItemTotal = new BasicAmountType(currency, itemTotal.ToString());
            paymentDetails.OrderTotal = new BasicAmountType(currency, orderTotal.ToString());

            //(Optional) Your URL for receiving Instant Payment Notification (IPN) 
            //about this transaction. If you do not specify this value in the request, 
            //the notification URL from your Merchant Profile is used, if one exists.
            //Important:
            //The notify URL applies only to DoExpressCheckoutPayment. 
            //This value is ignored when set in SetExpressCheckout or GetExpressCheckoutDetails.
            //Character length and limitations: 2,048 single-byte alphanumeric characters
            paymentDetails.NotifyURL = "";
            // ipnNotificationUrl.Value.Trim();

            //(Optional) Locale of pages displayed by PayPal during Express Checkout.           
            /*if (localeCode.SelectedIndex != 0)
            {
                ecDetails.LocaleCode = localeCode.SelectedValue;
            }

            // (Optional) Name of the Custom Payment Page Style for payment pages associated with this button or link. It corresponds to the HTML variable page_style for customizing payment pages. It is the same name as the Page Style Name you chose to add or edit the page style in your PayPal Account profile.
            if (pageStyle.Value != string.Empty)
            {
                ecDetails.PageStyle = pageStyle.Value;
            }*/
            //(Optional) URL for the image you want to appear at the top left of the payment page. The image has a maximum size of 750 pixels wide by 90 pixels high. PayPal recommends that you provide an image that is stored on a secure (https) server. If you do not specify an image, the business name displays.
            if (financialGateway.GetAttributeValue("PayPalLogoImage") != string.Empty)
            {
                ecDetails.cppHeaderImage = financialGateway.GetAttributeValue("PayPalLogoImage");
            }
            /*// (Optional) Sets the border color around the header of the payment page. The border is a 2-pixel perimeter around the header space, which is 750 pixels wide by 90 pixels high. By default, the color is black.
            if (cppheaderbordercolor.Value != string.Empty)
            {
                ecDetails.cppHeaderBorderColor = cppheaderbordercolor.Value;
            }
            // (Optional) Sets the background color for the header of the payment page. By default, the color is white.
            if (cppheaderbackcolor.Value != string.Empty)
            {
                ecDetails.cppHeaderBackColor = cppheaderbackcolor.Value;
            }
            // (Optional) Sets the background color for the payment page. By default, the color is white.
            if (cpppayflowcolor.Value != string.Empty)
            {
                ecDetails.cppPayflowColor = cpppayflowcolor.Value;
            }
            // (Optional) A label that overrides the business name in the PayPal account on the PayPal hosted checkout pages.
            */
            if (financialGateway.GetAttributeValue("PayPalBrandName") != string.Empty)
            {
                ecDetails.BrandName = financialGateway.GetAttributeValue("PayPalBrandName");
            }

            request.SetExpressCheckoutRequestDetails = ecDetails;

            return request;
        }


        #endregion

        private Dictionary<string, string> GetCredentials(FinancialGateway financialGateway)
        {
            financialGateway.LoadAttributes();
            Dictionary<string, string> config = new Dictionary<string, string>();

            String apiUsername = Encryption.DecryptString(financialGateway.GetAttributeValue("PayPalAPIUsername"));
            String apiPassword = Encryption.DecryptString(financialGateway.GetAttributeValue("PayPalAPIPassword"));
            String apiSignature = Encryption.DecryptString(financialGateway.GetAttributeValue("PayPalAPISignature"));

            config.Add("mode", financialGateway.GetAttributeValue("PayPalEnvironment"));
            config.Add("account1.apiUsername", apiUsername);
            config.Add("account1.apiPassword", apiPassword);
            config.Add("account1.apiSignature", apiSignature);

            return config;
        }
    }
}

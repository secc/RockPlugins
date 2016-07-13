using System;
using System.Collections.Generic;
using System.Linq;

using System.Data;
using Rock.Attribute;
using Quartz;
using Rock.Model;
using Rock.Data;
using RestSharp;
using Rock;

using org.secc.PersonMatch;
using Rock.Security;
using org.secc.PayPalReporting.Model;
using Rock.Web.Cache;

namespace org.secc.ServiceReef
{
    [EncryptedTextField("PayPal API Username", "Username for authenticating to the PayPal API", true, "", "PayPal API")]
    [EncryptedTextField("PayPal API Password", "Password for authenticating to the PayPal API", true, "", "PayPal API")]
    [EncryptedTextField("PayPal API Signature", "Signature for authenticating to the PayPal API", true, "", "PayPal API")]

    [EncryptedTextField("Service Reef API Key", "Key for authenticating to the ServiceReef API", true, "", "ServiceReef API")]
    [EncryptedTextField("Service Reef API Secret", "Secret for authenticating to the ServiceReef API", true, "", "ServiceReef API")]
    [TextField("Service Reef API URL", "Service Reef API URL.", true, "", "ServiceReef API")]
    [SlidingDateRangeField("Date Range", "The range of dates to import.", true, "Previous|2|Day||")]
    [AccountField("Account", "Financial account to use for the parent for all transactions imported (Sub-accounts will be created for each event).", true)]
    [FinancialGatewayField("Financial Gateway", "The financial gateway to use for these transactions.", true)]
    [DefinedValueField(Rock.SystemGuid.DefinedType.FINANCIAL_SOURCE_TYPE, "Transaction Source", "Transaction source for all Service Reef payments.", true, false, "9a3e36fa-634e-45e4-9244-d3d21646dba4")]
    [DefinedValueField(Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS, "Connection Status", "The connection status to use for newly created people.", true)]
    [DefinedValueField(Rock.SystemGuid.DefinedType.FINANCIAL_ACCOUNT_TYPE, "ServiceReef Account Type", "Account type for creating sub-accounts for each Service Reef Trip.", true, false, "51DC439B-2931-47CE-8FA8-C6DA1451B633")]
    [DisallowConcurrentExecution]
    public class ImportData : IJob
    {



        /// <summary>Process all transactions (payments) from Service Reef.</summary>
        /// <param name="message">The message that is returned depending on the result.</param>
        /// <param name="state">The state of the process.</param>
        /// <returns><see cref="WorkerResultStatus"/></returns>
        public void Execute(IJobExecutionContext context)
        {
            RockContext dbContext = new RockContext();
            FinancialBatchService financialBatchService = new FinancialBatchService(dbContext);
            PersonService personService = new PersonService(dbContext);
            PersonAliasService personAliasService = new PersonAliasService(dbContext);
            FinancialAccountService financialAccountService = new FinancialAccountService(dbContext);
            FinancialAccountService accountService = new FinancialAccountService(dbContext);
            FinancialTransactionService financialTransactionService = new FinancialTransactionService(dbContext);
            FinancialGatewayService financialGatewayService = new FinancialGatewayService(dbContext);
            DefinedValueService definedValueService = new DefinedValueService(dbContext);
            DefinedTypeService definedTypeService = new DefinedTypeService(dbContext);
            TransactionService transactionService = new TransactionService(new PayPalReporting.Data.PayPalReportingContext());

            // Get the datamap for loading attributes
            JobDataMap dataMap = context.JobDetail.JobDataMap;

            String warnings = string.Empty;
            
            FinancialBatch batch = null;
            Double totalAmount = 0;
            var total = 1;
            var processed = 0;
            try
            {
                DateRange dateRange = Rock.Web.UI.Controls.SlidingDateRangePicker.CalculateDateRangeFromDelimitedValues( dataMap.GetString( "DateRange" ) ?? "-1||" );
 
                String SRApiKey = Encryption.DecryptString(dataMap.GetString("ServiceReefAPIKey"));
                String SRApiSecret = Encryption.DecryptString(dataMap.GetString("ServiceReefAPISecret"));
                String SRApiUrl = dataMap.GetString("ServiceReefAPIURL");
                DefinedValueCache transactionSource = DefinedValueCache.Read(dataMap.GetString("TransactionSource").AsGuid(), dbContext);
                DefinedValueCache connectionStatus = DefinedValueCache.Read(dataMap.GetString("ConnectionStatus").AsGuid(), dbContext);
                DefinedValueCache contribution = DefinedValueCache.Read(Rock.SystemGuid.DefinedValue.TRANSACTION_TYPE_CONTRIBUTION);

                // Setup some lookups
                DefinedTypeCache creditCards = DefinedTypeCache.Read(Rock.SystemGuid.DefinedType.FINANCIAL_CREDIT_CARD_TYPE.AsGuid(), dbContext);
                DefinedTypeCache tenderType = DefinedTypeCache.Read(Rock.SystemGuid.DefinedType.FINANCIAL_CURRENCY_TYPE.AsGuid(), dbContext);
                FinancialAccount specialFund = accountService.Get(dataMap.GetString("Account").AsGuid());
                FinancialGateway gateway = financialGatewayService.Get(dataMap.GetString("FinancialGateway").AsGuid());
                List<FinancialAccount> trips = financialAccountService.Queryable().Where(fa => fa.ParentAccountId == specialFund.Id).OrderBy(fa => fa.Order).ToList();

                // Get the trips
                DefinedValueCache serviceReefAccountType = DefinedValueCache.Read(dataMap.Get("ServiceReefAccountType").ToString().AsGuid());

                // Setup the ServiceReef API Client
                var client = new RestClient(SRApiUrl);
                client.Authenticator = new HMACAuthenticator(SRApiKey, SRApiSecret);

                // Get all payments from ServiceReef
                var request = new RestRequest("v1/payments", Method.GET);
                request.AddParameter("pageSize", 100);
                request.AddParameter("startDate", dateRange.Start );
                request.AddParameter("endDate", dateRange.End );
                request.AddParameter("page", 1);

                while (total > processed)
                {
                    var response = client.Execute<Contracts.Payments>(request);
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new Exception("ServiceReef API Response: " + response.StatusDescription + " Content Length: " + response.ContentLength);

                    }
                    if (response.Data != null && response.Data.PageInfo != null)
                    {
                        total = response.Data.PageInfo.TotalRecords;
                        foreach (Contracts.Payments.Result result in response.Data.Results)
                        {
                            // Process the transaction
                            if (result.PaymentProcessorTransactionId != null) {
                                if (result.FirstName == null || result.LastName == null)
                                {
                                    warnings += "Mising Firstname/Lastname for ServiceReef transaction Id: " + result.TransactionId + Environment.NewLine;
                                    processed++;
                                    continue;
                                }
                                FinancialAccount trip = null;
                                // Make sure we have a sub-account to go with this transaction
                                if (result.EventId > 0)
                                {
                                    trip = trips.Where(t => t.GlCode == result.EventCode && t.Url == result.EventUrl).FirstOrDefault();
                                }
                                if (trip == null)
                                {
                                    if (result.EventCode == null)
                                    {
                                        warnings += "Event Code is missing on the Service Reef Trip for ServiceReef transaction Id: " + result.TransactionId + Environment.NewLine;
                                        processed++;
                                        continue;
                                    }

                                    // Create the trip subaccount
                                    FinancialAccount tripFA = new FinancialAccount();
                                    tripFA.Name = specialFund.Name + ": " + result.EventName;
                                    // Name is limited to 50
                                    if ( tripFA.Name.Length > 50 )
                                    {
                                        tripFA.Name = tripFA.Name.Substring( 0, 50 );
                                    }
                                    tripFA.Description = "Service Reef Event.  Name: " + result.EventName + " ID: " + result.EventId;
                                    tripFA.GlCode = result.EventCode;
                                    tripFA.Url = result.EventUrl;
                                    tripFA.PublicName = result.EventCode + ": " + result.EventName;
                                    // Public Name is limited to 50
                                    if (tripFA.PublicName.Length > 50) {
                                        tripFA.PublicName = tripFA.PublicName.Substring(0, 50);
                                    }
                                    tripFA.IsTaxDeductible = true;
                                    tripFA.IsPublic = false;
                                    tripFA.ParentAccountId = specialFund.Id;
                                    tripFA.Order = specialFund.Order+1;
                                    tripFA.AccountTypeValueId = serviceReefAccountType.Id;
                                    // Figure out what order it should be;
                                    foreach (FinancialAccount tmpTrip in trips)
                                    {
                                        if (tmpTrip.Name.CompareTo(tripFA.Name) < 0)
                                        {
                                            tripFA.Order++;
                                        }
                                    }

                                    financialAccountService.Add(tripFA);

                                    // Now save the trip
                                    dbContext.SaveChanges();
                                    // Increment all the rest of the Orders
                                    financialAccountService.Queryable().Where(fa => fa.Order >= tripFA.Order && fa.Id != tripFA.Id).ToList().ForEach(c => c.Order++);
                                    dbContext.SaveChanges();
                                    trips = financialAccountService.Queryable().Where(fa => fa.ParentAccountId == specialFund.Id).OrderBy(fa => fa.Order).ToList();
                                    trip = tripFA;
                                }

                                FinancialTransaction tran = financialTransactionService.Queryable().Where(tx => tx.TransactionCode == result.PaymentProcessorTransactionId).FirstOrDefault();

                                // We haven't processed this before so get busy!
                                if (tran == null)
                                {
                                    tran = new FinancialTransaction();
                                    tran.FinancialPaymentDetail = new FinancialPaymentDetail();
                                    if (result.Type == "CreditCard")
                                    {
                                        tran.FinancialPaymentDetail.CurrencyTypeValueId = tenderType.DefinedValues.Where(t => t.Value == "Credit Card").FirstOrDefault().Id;
                                    } else
                                    {
                                        tran.TransactionTypeValueId = tenderType.DefinedValues.Where(t => t.Value == "Credit Card").FirstOrDefault().Id;
                                    }

                                    Person person = null;
                                    // Find the person this transaction belongs to
                                    // 1. First start by determining whether this was a person
                                    //    paying their application fee or contributing to themselves
                                    //    because then we can just use their member info
                                    if (result.UserId > 0 &&
                                        result.DonatedToUserId == result.UserId && 
                                        result.DonatedToFirstName == result.FirstName &&
                                        result.DonatedToLastName == result.LastName)
                                    {
                                        var memberRequest = new RestRequest("v1/members/{userId}", Method.GET);
                                        memberRequest.AddUrlSegment("userId", result.UserId.ToString());
                                        var memberResult = client.Execute<Contracts.Member>(memberRequest);
                                        if (memberResult.Data != null && memberResult.Data.ArenaId > 0)
                                        {
                                            try
                                            {
                                                PersonAlias personAlias = personAliasService.Get(memberResult.Data.ArenaId);
                                                if (personAlias == null)
                                                {
                                                    throw new Exception("PersonAlias not found: " + memberResult.Data.ArenaId);
                                                }
                                                person = personAlias.Person;
                                            } catch (Exception e)
                                            {
                                                warnings += "Loading the person failed transaction id " + result.TransactionId  + " for " + result.FirstName + " " + result.LastName + " with the following error: " + e.Message + Environment.NewLine;
                                                processed++;
                                                continue;
                                            }
                                        }
                                    }
                                    // 2. If we didn't get a person match via their Alias Id
                                    //    then just use the standard person match logic
                                    if (person == null) {

                                        String street1 = null;
                                        String postalCode = null;
                                        if (result.Address != null)
                                        {
                                            street1 = result.Address.Address1;
                                            postalCode = result.Address.Zip;
                                        }
                                        List<Person> matches = personService.GetByMatch(result.FirstName.Trim(), result.LastName.Trim(), null, result.Email, null, street1, postalCode).ToList();
                                        
                                        if (matches.Count > 1)
                                        {
                                            // Find the oldest member record in the list
                                            person = matches.Where(p => p.ConnectionStatusValue.Value == "Member").OrderBy(p => p.Id).FirstOrDefault();
                                            if (person == null)
                                            {
                                                // Find the oldest attendee record in the list
                                                person = matches.Where(p => p.ConnectionStatusValue.Value == "Attendee").OrderBy(p => p.Id).FirstOrDefault();
                                                if (person == null)
                                                {
                                                    person = matches.OrderBy(p => p.Id).First();
                                                }
                                            }
                                        } else if (matches.Count == 1) {
                                            person = matches.First();
                                        }
                                        else
                                        {
                                            // Create the person
                                            person = new Person();
                                            person.FirstName = result.FirstName.Trim();
                                            person.LastName = result.LastName.Trim();
                                            person.Email = result.Email.Trim();
                                            Group family = PersonService.SaveNewPerson(person, dbContext);
                                            GroupLocation location = new GroupLocation();
                                            location.GroupLocationTypeValueId = DefinedValueCache.Read(Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME).Id;
                                            location.Location = new Location()
                                            {
                                                Street1 = result.Address.Address1,
                                                Street2 = result.Address.Address2,
                                                City = result.Address.City,
                                                State = result.Address.State,
                                                PostalCode = result.Address.Zip,
                                            };
                                            family.CampusId = CampusCache.All().FirstOrDefault().Id;
                                            family.GroupLocations.Add(location);
                                            dbContext.SaveChanges();
                                        }
                                        
                                    }
                                    
                                    // Get details about the transaction from our PayPal report table
                                    Transaction tx = transactionService.Get(result.PaymentProcessorTransactionId);
                                    if (tx != null)
                                    {
                                        if (tx.TenderType.Contains("ACH")) { 
                                            result.Type = "ACH";
                                            result.Method = null;
                                        } else
                                        {
                                            result.Type = "Credit Card";
                                            result.Method = tx.TenderType;
                                        }
                                    }
                                    else
                                    {
                                        // Defaults
                                        result.Type = "Credit Card";
                                        result.Method = "Visa";

                                        warnings += "Unable to find transaction in _org_secc_PaypalReporting_Transaction table: " + result.TransactionId + Environment.NewLine;

                                    }
                                    
                                    // If we don't have a batch, create one
                                    if (batch == null)
                                    {
                                        batch = new FinancialBatch();
                                        batch.BatchStartDateTime = result.Date;
                                        batch.BatchEndDateTime = DateTime.Now;
                                        batch.Name = "Service Reef Payments";
                                        batch.Status = BatchStatus.Open;
                                        financialBatchService.Add(batch);
                                        dbContext.SaveChanges();
                                    }
                                    
                                    // Complete the FinancialTransaction
                                    tran.AuthorizedPersonAliasId = person.PrimaryAliasId;
                                    tran.BatchId = batch.Id;
                                    tran.Summary = "F" + specialFund.Id + ":$" + result.Amount.ToString();
                                    tran.TransactionDateTime = result.Date;
                                    tran.FinancialGatewayId = gateway.Id;
                                    
                                    FinancialTransactionDetail financialTransactionDetail = new FinancialTransactionDetail();
                                    financialTransactionDetail.AccountId = trip.Id;
                                    financialTransactionDetail.Amount = result.Amount.ToString().AsDecimal();
                                    tran.TransactionDetails.Add(financialTransactionDetail);
                                    tran.TransactionTypeValueId = contribution.Id;

                                    tran.FinancialPaymentDetail = new FinancialPaymentDetail();
                                    tran.FinancialPaymentDetail.CurrencyTypeValueId = tenderType.DefinedValues.Where(type => type.Value.ToLower() == result.Type.ToLower()).FirstOrDefault().Id;
                                    if (result.Method != null)
                                    {
                                        tran.FinancialPaymentDetail.CreditCardTypeValueId = creditCards.DefinedValues.Where(card => card.Value.ToLower() == result.Method.ToLower()).FirstOrDefault().Id;
                                    }
                                    tran.TransactionCode = result.PaymentProcessorTransactionId;
                                    tran.SourceTypeValueId = transactionSource.Id;

                                    financialTransactionService.Add(tran);
                                    dbContext.SaveChanges();
                                    
                                    totalAmount += result.Amount;
                                }
                            }
                            processed++;
                        }
                    }
                    else
                    {
                        total = 0;
                    }
                    // Update the page number for the next request
                    var pageParam = request.Parameters.Where(p => p.Name == "page").FirstOrDefault();
                    pageParam.Value = (int)pageParam.Value + 1;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("ServiceReef Job Failed", ex);
            } finally
            {
                if (batch != null && totalAmount > 0)
                {
                    batch.ControlAmount = (Decimal)totalAmount;
                }
                dbContext.SaveChanges();

            }
            if (warnings.Length > 0)
            {
                throw new Exception(warnings);
            }
            context.Result = "Successfully imported " + processed + " transactions.";
        }
    }
}

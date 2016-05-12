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
    [DecimalField("Start Date Offset", "The number of hours to subtract from the start date (Defaults to now - 48 hours).", true, 48)]
    [DecimalField("End Date Offset", "The number of hours to subtract from the end date (Defauts to now - 8 hours).", true, 8)]

    [AttributeField("2C1CB26B-AB22-42D0-8164-AEDEE0DAE667", "ServiceReef Trip Attribute", "Attribute for a Financial Transaction to use for assigning the transaction to a ServiceReef trip.", true)]

    [AccountField("Account", "Financial account to use for all transactions imported.", true)]
    [FinancialGatewayField("Financial Gateway", "The financial gateway to use for these transactions.", true)]
    [DefinedValueField(Rock.SystemGuid.DefinedType.FINANCIAL_SOURCE_TYPE, "Transaction Source", "Transaction source for all Service Reef payments.", true, false, "9a3e36fa-634e-45e4-9244-d3d21646dba4")]
    [DefinedValueField(Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS, "Connection Status", "The connection status to use for newly created people.", true)]
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
            AttributeService attributeService = new AttributeService(dbContext);
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
                String SRApiKey = Encryption.DecryptString(dataMap.GetString("ServiceReefAPIKey"));
                String SRApiSecret = Encryption.DecryptString(dataMap.GetString("ServiceReefAPISecret"));
                String SRApiUrl = dataMap.GetString("ServiceReefAPIURL");
                Double StartDateOffset = dataMap.GetString("StartDateOffset").AsDouble();
                Double EndDateOffset = dataMap.GetString("EndDateOffset").AsDouble();
                DefinedValueCache transactionSource = DefinedValueCache.Read(dataMap.GetString("TransactionSource").AsGuid(), dbContext);
                DefinedValueCache connectionStatus = DefinedValueCache.Read(dataMap.GetString("ConnectionStatus").AsGuid(), dbContext);
                DefinedValueCache contribution = DefinedValueCache.Read(Rock.SystemGuid.DefinedValue.TRANSACTION_TYPE_CONTRIBUTION);

                // Setup some lookups
                DefinedTypeCache creditCards = DefinedTypeCache.Read(Rock.SystemGuid.DefinedType.FINANCIAL_CREDIT_CARD_TYPE.AsGuid(), dbContext);
                DefinedTypeCache tenderType = DefinedTypeCache.Read(Rock.SystemGuid.DefinedType.FINANCIAL_CURRENCY_TYPE.AsGuid(), dbContext);
                FinancialAccount specialFund = accountService.Get(dataMap.GetString("Account").AsGuid());
                FinancialGateway gateway = financialGatewayService.Get(dataMap.GetString("FinancialGateway").AsGuid());

                // Setup the ServiceReef API Client
                var client = new RestClient(SRApiUrl);
                client.Authenticator = new HMACAuthenticator(SRApiKey, SRApiSecret);

                // Get all payments from ServiceReef
                var request = new RestRequest("v1/payments", Method.GET);
                request.AddParameter("pageSize", 100);
                request.AddParameter("startDate", DateTime.Now.AddHours(0 - StartDateOffset).ToString());
                request.AddParameter("endDate", DateTime.Now.AddHours(0 - EndDateOffset).ToString());
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

                                Rock.Model.Attribute tripAttribute = attributeService.Get(dataMap.Get("ServiceReefTripAttribute").ToString().AsGuid());
                                DefinedType tripDT = definedTypeService.Get(tripAttribute.AttributeQualifiers.Where(aq => aq.Key == "definedtype").FirstOrDefault().Value.AsInteger());

                                List<DefinedValueCache> trips = DefinedTypeCache.Read(tripDT.Guid, dbContext).DefinedValues ;

                                DefinedValueCache trip = null;
                                // Make sure we have a project to go with this transaction
                                if (result.EventId > 0)
                                {
                                    trip = trips.Where(t => t.AttributeValues.Where(av => av.Key == "ServiceReefTripId" && av.Value.Value.AsInteger() == result.EventId).Any()).FirstOrDefault();
                                    //ProjectCollection pc = new ProjectCollection();
                                    //pc.Load(1);
                                    //project = pc.Where(p => p.Name.Contains(result.EventCode)).LastOrDefault();
                                }
                                if (trip == null)
                                {
                                    if (result.EventCode == null)
                                    {
                                        warnings += "Event Code (" + result.EventCode + ") with matching Project Name in Arena missing for ServiceReef transaction Id: " + result.TransactionId + Environment.NewLine;
                                        processed++;
                                        continue;
                                    }

                                    // Create the trip defined value
                                    DefinedValue tripDV = new DefinedValue();
                                    tripDV.Value = result.EventName;
                                    tripDV.Description = result.EventName + " - " + result.EventUrl;
                                    tripDV.DefinedTypeId = tripDT.Id;
                                    definedValueService.Add(tripDV);

                                    // Now save the trip
                                    dbContext.SaveChanges();
                                    
                                    // Save the attributes;
                                    tripDV.LoadAttributes();
                                    tripDV.AttributeValues["ServiceReefTripId"] = new AttributeValueCache() { Value = result.EventId.ToString() };
                                    tripDV.AttributeValues["GLCode"] = new AttributeValueCache() { Value = result.EventCode };

                                    tripDV.SaveAttributeValues();

                                    // Now load all the defined values of that type and sort them
                                    tripDT = definedTypeService.Get(tripAttribute.AttributeQualifiers.Where(aq => aq.Key == "definedtype").FirstOrDefault().Value.AsInteger());
                                    Rock.Web.UI.Controls.SortProperty property = new Rock.Web.UI.Controls.SortProperty();
                                    List<DefinedValue> sortedTrips = tripDT.DefinedValues.AsQueryable().OrderBy(dv => dv.Value).ToList();
                                    int i = 0;
                                    Boolean changed = false;
                                    foreach(DefinedValue sortedTrip in sortedTrips)
                                    {
                                        if (sortedTrip.Order != i)
                                        {
                                            sortedTrip.Order = i;
                                            changed = true;
                                        }
                                        i++;
                                    }
                                    if (changed)
                                    {
                                        dbContext.SaveChanges();
                                    }

                                    // Flush the defined types
                                    DefinedTypeCache.Flush(tripDT.Id);

                                    // Read the Defined Value from cache
                                    trip = DefinedValueCache.Read(tripDV.Guid);
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
                                        List<Person> matches = personService.GetByMatch(result.FirstName, result.LastName, null, result.Email, null, street1, postalCode).ToList();
                                        
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
                                            person.FirstName = result.FirstName;
                                            person.LastName = result.LastName;
                                            person.Email = result.Email;
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
                                    financialTransactionDetail.AccountId = specialFund.Id;
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
                                    tran.LoadAttributes();
                                    tran.AttributeValues["ServiceReefTrip"] = new AttributeValueCache() { Value = trip.Guid.ToString() };
                                    tran.SaveAttributeValues();
                                    
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

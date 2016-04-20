using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.paypal.sdk.services;
using com.paypal.soap.api;
using com.paypal.sdk.profiles;

namespace org.secc.PayPalReporting.Services
{
    class PayPalSvc
    {
        public static IAPIProfile CreateProfile(string userName, string password, string key, PayPalEnvironment environment)
        {
            IAPIProfile Profile = ProfileFactory.createSignatureAPIProfile();
            Profile.APIUsername = userName;
            Profile.APIPassword = password;
            Profile.APISignature = key;
            Profile.Environment = environment.ToString().ToLower();

            return Profile;
        }

        public static TransactionSearchResponseType TransactionSearchByID(IAPIProfile profile, string txID, DateTime startDate, DateTime endDate)
        {
            CallerServices caller = new CallerServices();
            caller.APIProfile = profile;

            TransactionSearchRequestType txSearchReq = new TransactionSearchRequestType();
            txSearchReq.TransactionID = txID;
            txSearchReq.StartDate = startDate.Date;
            txSearchReq.EndDate = endDate.Date.AddDays(1).AddSeconds(-1);
            txSearchReq.EndDateSpecified = true;
            txSearchReq.TransactionID = txID;

            TransactionSearchResponseType txSearchResp = (TransactionSearchResponseType)caller.Call("TransactionSearch", txSearchReq);
                       
            return txSearchResp;
        }


    }

    public enum PayPalEnvironment
    {
        Sandbox,
        Live
    }
}

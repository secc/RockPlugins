using org.secc.Purchasing.DataLayer.Accounting;
using System;
using System.Configuration;
using System.Linq;
namespace org.secc.Purchasing.DataLayer
{
    public class ContextHelper
    {
        //Shelby Connection String Name
        //private static string SHELBY_CSN = "Shelby";
        
        public static PurchasingContext GetDBContext()
        {
            var context = new PurchasingContext(new Rock.Data.RockContext().Database.Connection.ConnectionString);
            return context;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static ShelbyContext GetShelbyContext()
        {
            return new ShelbyContext(ConfigurationManager.ConnectionStrings["ShelbyContext"].ConnectionString);
        }
    }
}

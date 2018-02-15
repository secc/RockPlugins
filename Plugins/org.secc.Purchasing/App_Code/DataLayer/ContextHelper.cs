// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
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

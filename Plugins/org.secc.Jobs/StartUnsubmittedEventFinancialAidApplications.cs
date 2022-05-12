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
//<description>
// A registration workflow will only be triggered when the registration is 
// originally created. If a registrant goes back later and updates a registration
// at a later time to include a financial aid request, the workflow will not be 
// triggered to submit the financial aid request. This job checks to see if all
// registrations in the selected categories that have a financial aid discount code
// associated with them have been sent through the appropriate workflow process.
//</description>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;
namespace org.secc.Jobs
{
    [CategoryField( "Registration Categories", "Event Categories to search for unsubmitted Financial Aid Applications.", true,
        "Rock.Model.RegistrationTemplate", IsRequired = true )]
    [IntegerField( "Delay", "The about of time (in minutes) to wait after a registration has been updated to trigger the Financial Aid Workflow.", false, 30 )]
    [TextField( "Financial Aid Discount Code Prefix", "The prefix for financial aid discount codes.", true, "FINAID-", Key = "FinancialAidPrefix" )]

    [DisallowConcurrentExecution]
    public class StartUnsubmittedEventFinancialAidApplications : IJob
    {
        List<CategoryCache> registrationCategories;
        string financialAidPrefix = "";
        public void Execute( IJobExecutionContext context )
        {
            var dataMap = context.JobDetail.JobDataMap;

            registrationCategories = new List<CategoryCache>();
            foreach ( var categoryGuid in dataMap.GetString( "RegistrationCategories" ).SplitDelimitedValues())
            {
                var category = CategoryCache.Get( categoryGuid.AsGuid() );
                if(category != null)
                {
                    registrationCategories.Add( category );
                }
            }

            if(registrationCategories.Count == 0)
            {
                throw new Exception( "Registration Categories are not set." );
            }

            var registrationDelay = RockDateTime.Now.AddMinutes( - dataMap.GetIntegerFromString( "Delay" ) );
            financialAidPrefix = dataMap.GetString( "FinancialAidPrefix" );
            foreach ( var category in registrationCategories )
            {
               
            }
        }
    }
}

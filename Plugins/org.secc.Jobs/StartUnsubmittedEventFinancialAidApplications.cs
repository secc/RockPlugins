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
using System.Data.Entity;
using System.Linq;

using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
namespace org.secc.Jobs
{
    [CategoryField( "Registration Categories", "Event Categories to search for unsubmitted Financial Aid Applications.", true,
        "Rock.Model.RegistrationTemplate", IsRequired = true )]
    [IntegerField( "Delay", "The about of time (in minutes) to wait after a registration has been updated to trigger the Financial Aid Workflow.", false, 30 )]
    [TextField( "Financial Aid Discount Code Prefix", "The prefix for financial aid discount codes.", true, "FINAID-", Key = "FinancialAidPrefix" )]

    [DisallowConcurrentExecution]
    public class StartUnsubmittedEventFinancialAidApplications : IJob
    {
        public class FinancialAidApplication
        {
            public int WorkflowId { get; set; }
            public string DiscountCode { get; set; }
            public Guid RegistrationGuid { get; set; }

        }

        List<CategoryCache> registrationCategories;
        string financialAidPrefix = "";
        public void Execute( IJobExecutionContext context )
        {
            var dataMap = context.JobDetail.JobDataMap;

            registrationCategories = new List<CategoryCache>();
            foreach ( var categoryGuid in dataMap.GetString( "RegistrationCategories" ).SplitDelimitedValues() )
            {
                var category = CategoryCache.Get( categoryGuid.AsGuid() );
                if ( category != null )
                {
                    registrationCategories.Add( category );
                }
            }

            if ( registrationCategories.Count == 0 )
            {
                throw new Exception( "Registration Categories are not set." );
            }

            var registrationDelay = RockDateTime.Now.AddMinutes( -dataMap.GetIntegerFromString( "Delay" ) );
            financialAidPrefix = dataMap.GetString( "FinancialAidPrefix" );
            foreach ( var c in registrationCategories )
            {
                var unsubmittedFinAidApps = GetFinancialAidApps( registrationDelay, c.Id );
            }
        }

        public List<Registration> GetFinancialAidApps( DateTime delay, int categoryId )
        {
            var rockContext = new RockContext();
            var registrationService = new RegistrationService( rockContext );
            var attributeValueService = new AttributeValueService( rockContext );

            var workflowEntityType = EntityTypeCache.Get( typeof( Workflow ) );
            var workflowTypeIds = new List<string> { "139", "144" };
            var attributeValues = attributeValueService.Queryable()
                .Where( v => v.Attribute.EntityTypeId == workflowEntityType.Id )
                .Where( v => v.Attribute.EntityTypeQualifierColumn == "WorkflowTypeId" )
                .Where( v => workflowTypeIds.Contains( v.Attribute.EntityTypeQualifierValue ) )
                .Where( v => v.Attribute.Key == "DiscountCode" || v.Attribute.Key == "RegistrationTemplate" )
                .GroupBy( v => v.EntityId )
                .Select( v => new
                {
                    EntityId = v.Key,
                    DiscountCode = v.Where( v1 => v1.Attribute.Key == "DiscountCode" && v1.Value != "" ).Select( v1 => v1.Value ).FirstOrDefault(),
                    RegistrationTemplateGuid = v.Where( v1 => v1.Attribute.Key == "RegistrationTemplate" && v1.Value != "" ).Select( v1 => v1.Value ).FirstOrDefault()
                } )
                .Where(r => r.RegistrationTemplateGuid != null)
                .ToList()
                .Select( v => new FinancialAidApplication
                {
                    WorkflowId = v.EntityId.Value,
                    DiscountCode = v.DiscountCode,
                    RegistrationGuid = v.RegistrationTemplateGuid.AsGuid()
                } )
                .ToList();


            return registrationService.Queryable()
                .Include( "RegistrationInstance.RegistrationTemplate" )
                .Where( r => r.RegistrationInstance.RegistrationTemplate.CategoryId == categoryId )
                .Where( r => r.RegistrationInstance.RegistrationTemplate.IsActive )
                .Where( r => r.RegistrationInstance.IsActive )
                .Where( r => r.ModifiedDateTime < delay )
                .Where( r => r.DiscountCode.StartsWith( financialAidPrefix ) )
                .GroupJoin( attributeValues,
                    r => new { TemplateGuid = r.RegistrationInstance.RegistrationTemplate.Guid, DiscountCode = r.DiscountCode },
                    v => new { TemplateGuid = v.RegistrationGuid, DiscountCode = v.DiscountCode },
                    ( r, v ) => new { Registration = r, Values = v.DefaultIfEmpty() } )
                .Where( r => r.Values.Count() == 0)
                .Select( r => r.Registration ).ToList();


        }

    }

}

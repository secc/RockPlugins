using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Quartz;
using Rock;
using Rock.Data;
using Rock.Model;

using Rock.Web.Cache;


namespace org.secc.Jobs.Rock13
{
    [DisallowConcurrentExecution]
    [DisplayName("Clean up duplicate phone number types")]

    public class PhoneNumberCleanup : IJob
    {
        string _phoneTypeOtherGuid = "f5e13eeb-a98e-4bf8-a60f-14e4dc0ae662";
        string _phoneTypeOther2Guid = "48f77164-a742-45fb-beec-7d91c81442de";

        public void Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            var rockContext = new RockContext();

            var phoneService = new PhoneNumberService( rockContext );
            var mobilePhoneType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE );
            var other1PhoneType = DefinedValueCache.Get( _phoneTypeOtherGuid );
            var other2PhoneType = DefinedValueCache.Get( _phoneTypeOther2Guid );


            var duplicatePhoneTypes = phoneService.Queryable()
                .Where(p => p.NumberTypeValueId == mobilePhoneType.Id)
                .GroupBy( p => new { p.PersonId, p.NumberTypeValueId } )
                .Select( p => new
                {
                    p.Key.PersonId,
                    PhoneCount = p.Count()
                } )
                .Where( p => p.PhoneCount > 1 )
                .ToList();

            foreach (var duplicatePhoneType in duplicatePhoneTypes)
            {
                using (var phoneContext = new RockContext())
                {
                    var personPhones = new PhoneNumberService( phoneContext ).Queryable()
                        .Where( p => p.PersonId == duplicatePhoneType.PersonId )
                        .Where( p => p.NumberTypeValueId == mobilePhoneType.Id )
                        .OrderByDescending( p => p.IsMessagingEnabled )
                        .ThenByDescending( p => p.ModifiedDateTime )
                        .ToList();

                    personPhones[1].NumberTypeValueId = other1PhoneType.Id;

                    if(duplicatePhoneType.PhoneCount > 2)
                    {
                        personPhones[2].NumberTypeValueId = other2PhoneType.Id;
                    }

                    phoneContext.SaveChanges();
                }
            }


        }
    }
}

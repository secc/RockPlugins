using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using org.secc.Purchasing.DataLayer;
using Rock.Model;
using Rock;


namespace org.secc.Purchasing.Helpers
{
    public class Person
    {
        #region Public
        public static DefinedValue GetMyMinistryLookup( int personID, String ministryAttribute )
        { 
            DefinedValue MinistryLookup = null;
            PersonAliasService personAliasService = new PersonAliasService( new Rock.Data.RockContext() );
            var person = personAliasService.Get( personID ).Person;
            person.LoadAttributes();
            Guid? pa = person.GetAttributeValue( ministryAttribute ).AsGuidOrNull();
                DefinedValueService definedValueService = new DefinedValueService( new Rock.Data.RockContext() );

            if (pa.HasValue)
                MinistryLookup = definedValueService.Get(pa.Value);
            else
                MinistryLookup = new DefinedValue();

            return MinistryLookup;
        }
        
        public static List<int> GetPersonIDByAttributeValue(int attributeID, int intValue)
        {
            return GetPersonIDByAttributeValue(attributeID, intValue, null, null, null);
        }


        public static List<int> GetPersonIDByAttributeValue(int attributeID, int? intValue, string varcharValue, DateTime? datetimeValue, decimal? decimalValue)
        {
            List<int> PersonIDList = null;
            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                PersonIDList = Context.GetPersonIDsByAttributeValue(attribute_id: attributeID,
                    int_value: intValue,
                    varchar_value: varcharValue,
                    datetime_value: datetimeValue,
                    decimal_value: decimalValue,
                    organizationID: 1).Select(pa => pa.person_id).ToList();

            }

            return PersonIDList;
        }
        #endregion



    }
}

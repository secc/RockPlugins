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

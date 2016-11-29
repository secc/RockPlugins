using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

using Rock;
using Rock.Model;

namespace org.secc.Purchasing.DataLayer
{
    public class StaffMemberData 
    {

        /// <summary>
        /// Get datatable containing all staff members.
        /// </summary>
        /// <param name="ministryAID">The ministry Attribute ID.</param>
        /// <param name="positionAID">The position Attribute ID.</param>
        /// <returns>Data table containing staff member data</returns>
        public DataTable GetStaffMembersDT(int ministryAID, int positionAID)
        {
            return GetStaffMembersDT(ministryAID: ministryAID, positionAID: positionAID, personID: 0, ministryID: null, name1: String.Empty, name2: string.Empty);
        }

        /// <summary>
        /// Get Staff members by Ministry Area
        /// </summary>
        /// <param name="ministryAID">The Ministry Attribute ID.</param>
        /// <param name="positionAID">The position Attribute ID.</param>
        /// <param name="ministryID">The ministry DefinedType ID.</param>
        /// <returns></returns>
        public DataTable GetStaffMembersDTByMinistryID(int ministryAID, int positionAID, Guid? ministryID)
        {
            Rock.Data.RockContext context = new Rock.Data.RockContext();
            AttributeValueService attributeValueService = new AttributeValueService(context);
            AttributeService attributeService = new AttributeService(context);
            Rock.Model.Attribute ministryAttribute = attributeService.Get(ministryAID);
            Rock.Model.Attribute positionAttribute = attributeService.Get(positionAID);

            var matchingAttributes = attributeValueService.GetByAttributeId(ministryAttribute.Id).Where(av => av.Value == ministryID.ToString());

            DataTable results = new DataTable();
            results.Columns.Add("PersonAliasId");
            results.Columns.Add("Name");
            results.Columns.Add("Ministry Area");
            results.Columns.Add("Position");
            foreach (AttributeValue attributeValue in matchingAttributes)
            {
                PersonService personService = new PersonService(context);

                Person person = personService.Get(attributeValue.EntityId.Value);
                person.LoadAttributes();
                if (person.RecordStatusValue.Value != "Inactive" &&
                    person.AttributeValues["StaffMember"].Value.AsBoolean())
                {
                    DataRow dr = results.NewRow();
                    dr["PersonAliasId"] = person.PrimaryAliasId;
                    dr["Name"] = person.FullName;
                    if (person.AttributeValues[ministryAttribute.Key] != null)
                    {
                        dr["Ministry Area"] = person.AttributeValues[ministryAttribute.Key].Value;
                    }
                    else
                    {
                        dr["Ministry Area"] = "N/A";
                    }
                    if (person.AttributeValues[positionAttribute.Key] != null)
                    {
                        dr["Position"] = person.AttributeValues[positionAttribute.Key].Value;
                    }
                    else
                    {
                        dr["Position"] = "N/A";
                    }
                    results.Rows.Add(dr);

                }


            }
            return results;
        }

        /// <summary>
        /// Gets the staff member by personID
        /// </summary>
        /// <param name="ministryAID">The Ministry Attribute ID.</param>
        /// <param name="positionAID">The position Attribute ID.</param>
        /// <param name="personID">The staff member's PersonID.</param>
        /// <returns>Data table containing staff member data</returns>
        public DataTable GetStaffMembersDTByPersonID(int ministryAID, int positionAID, int personID)
        {
            return GetStaffMembersDT(
                    ministryAID: ministryAID,
                    positionAID: positionAID,
                    personID: personID,
                    ministryID: null,
                    name1: String.Empty,
                    name2: String.Empty
                );
        }

        /// <summary>
        /// Gets list of staff members by full or partial match.
        /// </summary>
        /// <param name="ministryAID">The Ministry Attribute ID.</param>
        /// <param name="positionAID">The position Attribute ID.</param>
        /// <param name="name1">First word for name match</param>
        /// <param name="name2">Second word for name match</param>
        /// <returns>Data table of staff members who's name is a full or partial match.</returns>
        public DataTable GetStaffMembersDTByName(int ministryAID, int positionAID, string name1, string name2)
        {
            return GetStaffMembersDT(
                    ministryAID: ministryAID,
                    positionAID: positionAID,
                    name1: name1,
                    name2: name2,
                    personID: 0,
                    ministryID: null
                );
        }

        /// <summary>
        /// Gets a list of staff members by full or partial name and ministry 
        /// </summary>
        /// <param name="ministryAID">The Ministry Attribute ID.</param>
        /// <param name="positionAID">The position Attribute ID.</param>
        /// <param name="name1">First word for name match</param>
        /// <param name="name2">Second word for name match</param>
        /// <param name="ministryID">The ministry DefinedType ID.</param>
        /// <returns>Data table of staff members</returns>
        public DataTable GetStaffMembersDTByNameMinistry(int ministryAID, int positionAID, string name1, string name2, Guid? ministryID)
        {
            return GetStaffMembersDT(
                    ministryAID: ministryAID,
                    positionAID: positionAID,
                    name1: name1,
                    name2: name2,
                    personID: 0,
                    ministryID: ministryID
                );
        }

        /// <summary>
        /// Datatable of selected staff members
        /// </summary>
        /// <param name="ministryAID">The Ministry Attribute ID.</param>
        /// <param name="positionAID">The position Attribute ID.</param>
        /// <param name="personID">The staff member's PersonID.</param>
        /// <param name="ministryID">The ministry DefinedType ID.</param>
        /// <param name="name1">First word for name match</param>
        /// <param name="name2">Second word for name match</param>
        /// <returns>Datatable of staff members</returns>
        public DataTable GetStaffMembersDT(int ministryAID, int positionAID, int personID, Guid? ministryID, string name1, string name2)
        {
            AttributeService attributeService = new AttributeService(new Rock.Data.RockContext());
            Rock.Model.Attribute ministryAttribute = attributeService.Get(ministryAID);
            Rock.Model.Attribute positionAttribute = attributeService.Get(positionAID);

            PersonService personService = new PersonService(new Rock.Data.RockContext());
            var people = personService.GetByFullName(name1 + ' ' + name2, true);

            DataTable results = new DataTable();
            results.Columns.Add("PersonAliasId");
            results.Columns.Add("Name");
            results.Columns.Add("Ministry Area");
            results.Columns.Add("Position");
            foreach (Person person in people)
            {
                person.LoadAttributes();
                if (person.RecordStatusValue.Value != "Inactive" &&
                    (!ministryID.HasValue && !string.IsNullOrEmpty(person.AttributeValues[ministryAttribute.Key].Value) ||
                        (ministryID.HasValue &&
                    (person.AttributeValues[ministryAttribute.Key] != null &&
                    person.AttributeValues[ministryAttribute.Key].Value == ministryID.Value.ToString()))))
                    {

                    DataRow dr = results.NewRow();
                    dr["PersonAliasId"] = person.PrimaryAliasId;
                    dr["Name"] = person.FullName;
                    if (person.AttributeValues[ministryAttribute.Key] != null)
                    {
                        dr["Ministry Area"] = person.AttributeValues[ministryAttribute.Key].Value;
                    }
                    else
                    {
                        dr["Ministry Area"] = "N/A";
                    }
                    if (person.AttributeValues[positionAttribute.Key] != null)
                    {
                        dr["Position"] = person.AttributeValues[positionAttribute.Key].Value;
                    }
                    else
                    {
                        dr["Position"] = "N/A";
                    }
                    results.Rows.Add(dr);

                }
            }
            return results;
        }
    }
}

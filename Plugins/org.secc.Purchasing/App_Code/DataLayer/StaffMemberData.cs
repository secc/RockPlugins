using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

using Rock;

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
            return GetStaffMembersDT(ministryAID: ministryAID, positionAID: positionAID, personID: 0, ministryID: 0, name1: String.Empty, name2: string.Empty);
        }

        /// <summary>
        /// Get Staff members by Ministry Area
        /// </summary>
        /// <param name="ministryAID">The Ministry Attribute ID.</param>
        /// <param name="positionAID">The position Attribute ID.</param>
        /// <param name="ministryID">The ministry DefinedType ID.</param>
        /// <returns></returns>
        public DataTable GetStaffMembersDTByMinistryID(int ministryAID, int positionAID, int ministryID)
        {
            return GetStaffMembersDT(
                    ministryAID: ministryAID,
                    positionAID: positionAID,
                    ministryID: ministryID,
                    personID:0,
                    name1: string.Empty,
                    name2: string.Empty
                );
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
                    ministryID: 0,
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
                    ministryID: 0
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
        public DataTable GetStaffMembersDTByNameMinistry(int ministryAID, int positionAID, string name1, string name2, int ministryID)
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
        public DataTable GetStaffMembersDT(int ministryAID, int positionAID, int personID, int ministryID, string name1, string name2)
        {
            ArrayList paramList = new ArrayList();

            //paramList.Add(new SqlParameter("@OrganizationID", 1));
            paramList.Add(new SqlParameter("@MinistryAttributeID", ministryAID));
            paramList.Add(new SqlParameter("@PositionAttributeID", positionAID));

            if (ministryID > 0)
                paramList.Add(new SqlParameter("@MinistryLUID", ministryID));
            else
                paramList.Add(new SqlParameter("@MinistryLUID", DBNull.Value));

            if (personID > 0)
            {
                paramList.Add(new SqlParameter("@PersonID", personID));
            }
            else
            {
                paramList.Add(new SqlParameter("@PersonID", DBNull.Value));
            }

            if (!String.IsNullOrEmpty(name1))
            {
                paramList.Add(new SqlParameter("@Name1", name1));
            }
            else
            {
                paramList.Add(new SqlParameter("@Name1", DBNull.Value));
            }

            if (!String.IsNullOrEmpty(name2))
            {
                paramList.Add(new SqlParameter("@Name2", name2));
            }
            else
            {
                paramList.Add(new SqlParameter("@Name2", DBNull.Value));
            }

            //return base.ExecuteDataTable("dbo.cust_secc_sp_get_personStaffList", paramList);
            return null;
        }
    }
}

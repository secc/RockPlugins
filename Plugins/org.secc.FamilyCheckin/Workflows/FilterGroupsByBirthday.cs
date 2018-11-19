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
// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Web.Cache;
using Rock.Workflow;
using Rock.Workflow.Action.CheckIn;

namespace org.secc.FamilyCheckin
{
    /// <summary>
    /// Removes (or excludes) the groups for each selected family member that are not specific to their age.
    /// </summary>
    [ActionCategory( "SECC > Check-In" )]
    [Description( "Removes (or excludes) the groups for each selected family member that are not specific to their birthday. Also removes/excludes children who have a graduation year." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Filter Groups By Birthday" )]

    [BooleanField( "Remove", "Select 'Yes' if groups should be be removed.  Select 'No' if they should just be marked as excluded.", true, "", 0 )]
    [AttributeField( Rock.SystemGuid.EntityType.GROUP, "Group Birthday Range Attribute", "Select the attribute used to define the inclusive birthday range of the group", true, false, "43511B8F-71D9-423A-85BF-D1CD08C1998E", order: 2 )]
    [AttributeField( Rock.SystemGuid.EntityType.GROUP, "Group Filter GradeSchool Attribute", "Attribute which describes if children with grades should be removed." )]
    public class FilterGroupsByBirthday : CheckInActionComponent
    {
        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The workflow action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override bool Execute( RockContext rockContext, Rock.Model.WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            var checkInState = GetCheckInState( entity, out errorMessages );
            if ( checkInState == null )
            {
                throw new Exception( "Check-In state lost: Filter Groups By Birthday" );
            }

            var family = checkInState.CheckIn.CurrentFamily;
            if ( family != null )
            {
                var remove = GetAttributeValue( action, "Remove" ).AsBoolean();
                bool ageRequired = checkInState.CheckInType == null || checkInState.CheckInType.AgeRequired;

                // get the admin-selected attribute key instead of using a hardcoded key
                var birthdayRangeAttributeKey = string.Empty;
                var birthdayRangeAttributeGuid = GetAttributeValue( action, "GroupBirthdayRangeAttribute" ).AsGuid();
                if ( birthdayRangeAttributeGuid != Guid.Empty )
                {
                    birthdayRangeAttributeKey = AttributeCache.Get( birthdayRangeAttributeGuid ).Key;
                }

                // log a warning if the attribute is missing or invalid
                if ( string.IsNullOrWhiteSpace( birthdayRangeAttributeKey ) )
                {

                    throw new Exception( "Workflow attribute not set: Filter Groups By Birthday | Birthday Range Attribute" );
                }

                var filterGradeSchoolGuid = GetAttributeValue( action, "GroupFilterGradeSchoolAttribute" ).AsGuid();
                if ( filterGradeSchoolGuid == Guid.Empty )
                {
                    throw new Exception( "Workflow attribute not set: Filter Groups By Birthday | Filter GradeSchool Students" );
                }
                string filterGradeSchoolKey = AttributeCache.Get( filterGradeSchoolGuid ).Key;
                if ( string.IsNullOrWhiteSpace( filterGradeSchoolKey ) )
                {
                    throw new Exception( "Workflow attribute not set: Filter Groups By Birthday | Filter GradeSchool Students" );
                }

                foreach ( var person in family.People )
                {

                    if ( person.Person.BirthDate == null && !ageRequired )
                    {
                        continue;
                    }

                    foreach ( var groupType in person.GroupTypes.ToList() )
                    {
                        foreach ( var group in groupType.Groups.ToList() )
                        {
                            if ( person.Person.BirthDate == null )
                            {
                                if ( remove )
                                {
                                    groupType.Groups.Remove( group );
                                }
                                else
                                {
                                    group.ExcludedByFilter = true;
                                }
                                continue;
                            }

                            var birthdayRange = group.Group.GetAttributeValue( birthdayRangeAttributeKey );
                            if ( string.IsNullOrWhiteSpace( birthdayRange ) )
                            {
                                continue;
                            }
                            var dateRangePair = birthdayRange.Split( new char[] { ',' }, StringSplitOptions.None );
                            DateTime? minDate = null;
                            DateTime? maxDate = null;

                            if ( dateRangePair.Length == 2 )
                            {
                                minDate = dateRangePair[0].AsDateTime();
                                maxDate = dateRangePair[1].AsDateTime();
                            }

                            if ( minDate != null )
                            {
                                if ( person.Person.BirthDate < minDate )
                                {
                                    if ( remove )
                                    {
                                        groupType.Groups.Remove( group );
                                    }
                                    else
                                    {
                                        group.ExcludedByFilter = true;
                                    }
                                    continue;
                                }
                            }

                            if ( maxDate != null )
                            {

                                if ( person.Person.BirthDate > maxDate )
                                {
                                    if ( remove )
                                    {
                                        groupType.Groups.Remove( group );
                                    }
                                    else
                                    {
                                        group.ExcludedByFilter = true;
                                    }
                                    continue;
                                }
                            }

                            //Filter kids out who are in school
                            if ( group.Group.GetAttributeValue( filterGradeSchoolKey ).AsBoolean() )
                            {
                                if ( person.Person.GradeOffset != null && person.Person.GradeOffset < 13 && person.Person.GradeOffset >= 0 )
                                {
                                    if ( remove )
                                    {
                                        groupType.Groups.Remove( group );
                                    }
                                    else
                                    {
                                        group.ExcludedByFilter = true;
                                    }
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }
    }
}
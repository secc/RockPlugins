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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Caching;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;
using Rock.Workflow.Action.CheckIn;

namespace org.secc.FamilyCheckin
{
    /// <summary>
    /// Removes or excludes checkin groups that require the member to be in a particular group
    /// </summary>
    [ActionCategory( "SECC > Check-In" )]
    [Description( "Removes or excludes checkin groups that require the member to be in a dataview" )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Filter Groups By DataView" )]
    [AttributeField( Rock.SystemGuid.EntityType.GROUP, "DataView Group Attribute", "Select the attribute used to filter by DataView.", true, false )]
    [BooleanField( "Remove", "Select 'Yes' if groups should be be removed.  Select 'No' if they should just be marked as excluded.", true, order: 5 )]
    public class FilterGroupsByDataView : CheckInActionComponent
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
                return false;
            }

            ObjectCache cache = Rock.Web.Cache.RockMemoryCache.Default;

            var dataViewAttributeKey = string.Empty;
            var dataViewAttributeGuid = GetAttributeValue( action, "DataViewGroupAttribute" ).AsGuid();
            if ( dataViewAttributeGuid != Guid.Empty )
            {
                dataViewAttributeKey = AttributeCache.Read( dataViewAttributeGuid, rockContext ).Key;
            }

            var dataViewService = new DataViewService( rockContext );

            var family = checkInState.CheckIn.CurrentFamily;
            if ( family != null )
            {
                var remove = GetAttributeValue( action, "Remove" ).AsBoolean();

                foreach ( var person in family.People )
                {
                    foreach ( var groupType in person.GroupTypes.ToList() )
                    {
                        foreach ( var group in groupType.Groups.ToList() )
                        {
                            if ( group.ExcludedByFilter == true )
                            {
                                continue;
                            }

                            var approvedPeopleGuid = group.Group.GetAttributeValue( dataViewAttributeKey );
                            if ( string.IsNullOrWhiteSpace( approvedPeopleGuid ) )
                            {
                                continue;
                            }

                            //Get approved people dataview from cache or from db
                            var approvedPeopleList = cache[approvedPeopleGuid] as List<int>;

                            if ( approvedPeopleList == null )
                            {
                                DataView approvedPeople = dataViewService.Get( approvedPeopleGuid.AsGuid() );

                                if ( approvedPeople == null )
                                {
                                    continue;
                                }

                                var errors = new List<string>();
                                var approvedPeopleQry = approvedPeople.GetQuery( null, 30, out errors );
                                if ( approvedPeopleQry != null )
                                {
                                    approvedPeopleList = approvedPeopleQry.Select( e => e.Id ).ToList();
                                    var cachePolicy = new CacheItemPolicy();
                                    cachePolicy.AbsoluteExpiration = DateTimeOffset.Now.AddMinutes( 10 );
                                    cache.Set( approvedPeopleGuid, approvedPeopleList, cachePolicy );
                                }
                            }

                            if ( approvedPeopleList != null && !approvedPeopleList.Contains( person.Person.Id ) )
                            {
                                if ( remove )
                                {
                                    groupType.Groups.Remove( group );
                                }
                                else
                                {
                                    group.ExcludedByFilter = true;
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
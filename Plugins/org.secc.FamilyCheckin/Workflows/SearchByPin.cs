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
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
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
using System.Data.Entity;
using System.Linq;
using Rock;
using Rock.Workflow;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Attribute;
using Rock.Workflow.Action.CheckIn;

namespace org.secc.FamilyCheckin
{
    /// <summary>
    /// Finds family members in a given family
    /// </summary>
    [ActionCategory( "SECC > Check-In" )]
    [Description( "Serches for people by PIN." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Search By PIN" )]
    [BooleanField( "Always Search", "Should we search even if a family has been found?", false )]
    [BooleanField("Clear Families", "Should we clear families if we find a PIN", false)]
    public class SearchByPIN : CheckInActionComponent
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
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            var checkInState = GetCheckInState( entity, out errorMessages );
            if ( checkInState != null )
            {
                if ( GetActionAttributeValue( action, "AlwaysSearch" ).AsBoolean() || !checkInState.CheckIn.Families.Any() )
                {

                    var searchValue = checkInState.CheckIn.SearchValue.Trim();

                    long n;
                    bool isNumeric = long.TryParse( searchValue, out n );

                    if ( isNumeric )
                    {
                        UserLoginService userLogin = new UserLoginService( rockContext );
                        var user = userLogin.GetByUserName( searchValue );
                        if ( user != null )
                        {
                            //Short PINs can be confused for phone numbers. Clear families if we have selected.
                            if (GetActionAttributeValue(action, "ClearFamilies" ).AsBoolean() )
                            {
                                checkInState.CheckIn.Families.Clear();
                            }

                            var memberService = new GroupMemberService( rockContext );
                            var families = user.Person.GetFamilies();
                            foreach ( var group in families )
                            {
                                var family = checkInState.CheckIn.Families.Where( f => f.Group.Id == group.Id ).FirstOrDefault();
                                if ( family == null )
                                {
                                    family = new CheckInFamily();
                                    family.Group = group.Clone( false );
                                    family.Group.LoadAttributes( rockContext );
                                    family.Caption = group.ToString();
                                    family.SubCaption = memberService.GetFirstNames( group.Id ).ToList().AsDelimited( ", " );
                                    checkInState.CheckIn.Families.Add( family );
                                }
                            }
                        }
                    }

                }
                return true;
            }

            return false;
        }
    }
}
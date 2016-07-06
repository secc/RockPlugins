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
    [Description( "Searches and loads person by PIN (does not load family)." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Load Person By PIN" )]
    [BooleanField( "Search By Phone", "Should we also allow searching by phone number? This will only return true if a person is found." )]
    [IntegerField( "Minimum Phone Length", "The minimum number of digits for a phone number.", false, 7 )]
    public class LoadPersonByPIN : CheckInActionComponent
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
                var searchValue = checkInState.CheckIn.SearchValue.Trim();

                long n;
                bool isNumeric = long.TryParse( searchValue, out n );

                if ( isNumeric )
                {
                    UserLoginService userLogin = new UserLoginService( rockContext );
                    var user = userLogin.GetByUserName( searchValue );
                    if ( user != null )
                    {
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
                                family.SubCaption = "";
                                checkInState.CheckIn.Families.Add( family );
                            }
                            var person = new CheckInPerson();
                            person.Person = user.Person;
                            person.Selected = true;
                            family.People = new List<CheckInPerson>() { person };
                            family.Selected = true;
                        }

                    }
                    else if ( GetAttributeValue( action, "SearchByPhone" ).AsBoolean() )
                    {
                        //Look for person by phone number
                        if ( searchValue.Length < ( GetAttributeValue( action, "MinimumPhoneLength" ).AsIntegerOrNull() ?? 7) )
                        {
                            return false;
                        }
                        PhoneNumberService phoneNumberService = new PhoneNumberService( rockContext );
                        var phoneNumbers = phoneNumberService.GetBySearchterm( searchValue ).DistinctBy( pn => pn.Number ).DistinctBy( pn => pn.PersonId ).ToList();
                        if ( phoneNumbers.Count() != 1 )
                        {
                            return false;
                        }
                        var phoneNumber = phoneNumbers.FirstOrDefault();
                        var person = phoneNumber.Person;
                        var family = person.GetFamilies().FirstOrDefault();

                        var cFamily = new CheckInFamily();
                        cFamily.Group = family.Clone( false );
                        cFamily.Group.LoadAttributes( rockContext );
                        cFamily.Caption = family.ToString();
                        cFamily.SubCaption = "";
                        cFamily.Selected = true;
                        checkInState.CheckIn.Families.Add( cFamily );

                        var cPerson = new CheckInPerson();
                        cPerson.Person = person;
                        cPerson.Selected = true;
                        cFamily.People = new List<CheckInPerson>() { cPerson };
                    }
                }
                return true;
            }

            return false;
        }
    }
}
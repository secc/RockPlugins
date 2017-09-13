using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Workflow;
using Rock.Workflow.Action.CheckIn;

namespace org.secc.FamilyCheckin
{
    /// <summary>
    /// Removes (or excludes) the groups for each selected family member if the person's ability level does not match the groups.
    /// </summary>
    [ActionCategory( "SECC > Check-In" )]
    [Description( "Removes (or excludes) the groups for each selected family member if the person's ability level does not match the groups." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Filter Groups By Ability" )]
    [BooleanField( "Remove", "Select 'Yes' if groups should be be removed.  Select 'No' if they should just be marked as excluded.", true )]
    public class FilterGroupsByAbility : CheckInActionComponent
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
            var family = checkInState.CheckIn.CurrentFamily;
            if ( family != null )
            {
                var remove = GetAttributeValue( action, "Remove" ).AsBoolean();

                foreach ( var person in family.People )
                {
                    FilterGroups( person, rockContext, remove );
                }
            }

            return true;
        }

        private void FilterGroups( CheckInPerson person, RockContext rockContext, bool remove )
        {
            if ( person.Person.Attributes == null )
            {
                person.Person.LoadAttributes( rockContext );
            }

            string personAbilityLevel = person.Person.GetAttributeValue( "AbilityLevel" );
            if ( !string.IsNullOrWhiteSpace( personAbilityLevel ) )
            {
                foreach ( var groupType in person.GroupTypes.ToList() )
                {
                    foreach ( var group in groupType.Groups.ToList() )
                    {
                        var groupAttribute = group.Group.GetAttributeValue( "AbilityLevel" );
                        if ( !string.IsNullOrWhiteSpace( groupAttribute ) &&
                            groupAttribute.AsGuid() != personAbilityLevel.AsGuid() )
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
    }
}
// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
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
using System.Data.Entity;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;

namespace org.secc.Workflow.Registrations
{
    /// <summary>
    /// Synchronizes placement group assignments back to registration group member
    /// attributes. When a person is added to (or removed from) a placement group,
    /// this action finds their corresponding registration group member record and
    /// updates the specified attribute with the placement group name.
    ///
    /// <para>This is designed for camp registration scenarios where placement
    /// information (bus, dorm room, small group, activity, etc.) needs to be stored
    /// on the registration group member record for check-in label printing.</para>
    ///
    /// <para><strong>Matching logic:</strong> The placement group is linked to a
    /// RegistrationInstance via the RelatedEntity table. The person's
    /// RegistrationRegistrant record for that instance provides the GroupMemberId
    /// in the registration group. This naturally scopes to the correct camp even
    /// when a person is registered for multiple camps (e.g., adult leaders at two
    /// camps, or HS campers who are also minor leaders at children's camp).</para>
    ///
    /// <para><strong>Removal logic:</strong> When "Is Removal" is true, the action
    /// checks if the person is still in another placement group of the same
    /// GroupType for the same registration instance. If so, it updates the
    /// attribute to that group's name. If not, it clears the attribute. This
    /// prevents accidental data loss when a person is removed from a duplicate
    /// placement after being correctly placed in the right group.</para>
    ///
    /// <para><strong>Workflow Setup:</strong></para>
    /// <para>1. Create a workflow type triggered by "Group Member Added to Group"
    ///    on the placement group type(s). Add this action with "Is Removal" = false.</para>
    /// <para>2. Create a second workflow type triggered by "Group Member Removed
    ///    from Group" on the same group type(s). Add this action with "Is Removal"
    ///    = true.</para>
    /// <para>3. Each workflow should have a Person attribute and a Group attribute
    ///    populated by the trigger.</para>
    /// <para>4. Create one pair of workflows per placement type (bus, dorm, etc.)
    ///    or reuse the same workflow if the attribute key is the same.</para>
    /// </summary>
    [ActionCategory( "SECC > Registrations" )]
    [Description( "Updates a group member attribute on the registration group based on placement group membership. " +
        "Handles both additions and removals, including resolution when a person is in multiple placement groups of the same type." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Update Registration Group With Placement Group" )]

    [WorkflowAttribute( "Person",
        "The person whose registration group member record should be updated.",
        true, "", "", 0, "Person",
        new string[] { "Rock.Field.Types.PersonFieldType" } )]

    [WorkflowAttribute( "Placement Group",
        "The placement group the person was added to or removed from.",
        true, "", "", 1, "PlacementGroup",
        new string[] { "Rock.Field.Types.GroupFieldType" } )]

    [TextField( "Group Member Attribute Key",
        "The attribute key on the registration group member to update with the placement group name.",
        true, "", "", 2, "GroupMemberAttributeKey" )]

    [BooleanField( "Is Removal",
        "Set to true when this action is triggered by a group member removal. " +
        "The action will check if the person is still in another placement group of the same type " +
        "and update the attribute accordingly, or clear it if not.",
        false, "", 3, "IsRemoval" )]

    public class UpdateRegistrationGroupWithPlacementGroup : ActionComponent
    {
        /// <summary>
        /// Executes the action to update the registration group member's attribute
        /// with the placement group information.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        public override bool Execute( RockContext rockContext, WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            // 1. Get the person from the workflow attribute.
            var person = GetPersonFromAttribute( rockContext, action, errorMessages );
            if ( person == null )
            {
                errorMessages.ForEach( m => action.AddLogEntry( m, true ) );
                return false;
            }

            // 2. Get the placement group from the workflow attribute.
            Group placementGroup = GetPlacementGroupFromAttribute( rockContext, action, errorMessages );
            if ( placementGroup == null )
            {
                errorMessages.ForEach( m => action.AddLogEntry( m, true ) );
                return false;
            }

            // 3. Read configuration values.
            string attributeKey = GetAttributeValue( action, "GroupMemberAttributeKey" ).Replace( " ", "" );
            bool isRemoval = GetAttributeValue( action, "IsRemoval" ).AsBoolean();

            if ( string.IsNullOrWhiteSpace( attributeKey ) )
            {
                errorMessages.Add( "The group member attribute key is required." );
                errorMessages.ForEach( m => action.AddLogEntry( m, true ) );
                return false;
            }

            // 4. Find registration instances linked to this placement group.
            //    This checks both instance-level and template-level placement configurations.
            var registrationInstanceIds = GetLinkedRegistrationInstanceIds( rockContext, placementGroup.Id );
            if ( !registrationInstanceIds.Any() )
            {
                action.AddLogEntry( string.Format(
                    "No registration instances found linked to placement group '{0}' (Id: {1}).",
                    placementGroup.Name, placementGroup.Id ) );
                return true;
            }

            // 5. Get all PersonAliasIds for this person (to match across aliases).
            var personAliasIds = new PersonAliasService( rockContext )
                .Queryable().AsNoTracking()
                .Where( pa => pa.PersonId == person.Id )
                .Select( pa => pa.Id )
                .ToList();

            // 6. Find RegistrationRegistrant records that link this person to a
            //    GroupMember in the registration group for the matching instances.
            //    RegistrationRegistrant.GroupMemberId is the key that directly
            //    identifies the correct registration group member record.
            var registrationGroupMemberIds = new RegistrationRegistrantService( rockContext )
                .Queryable().AsNoTracking()
                .Where( rr =>
                    rr.PersonAliasId.HasValue
                    && personAliasIds.Contains( rr.PersonAliasId.Value )
                    && rr.GroupMemberId.HasValue
                    && rr.Registration != null
                    && registrationInstanceIds.Contains( rr.Registration.RegistrationInstanceId ) )
                .Select( rr => rr.GroupMemberId.Value )
                .Distinct()
                .ToList();

            if ( !registrationGroupMemberIds.Any() )
            {
                action.AddLogEntry( string.Format(
                    "No registrant records with linked group members found for '{0}' in the matching registration instances.",
                    person.FullName ) );
                return true;
            }

            // 7. Determine the attribute value to set.
            string valueToSet;
            if ( isRemoval )
            {
                // On removal, check if the person is still in another placement
                // group of the same GroupType. If so, use that group's name as
                // the value. Otherwise, clear it.
                valueToSet = FindReplacementPlacementGroupName(
                    rockContext, person, placementGroup, registrationInstanceIds );
            }
            else
            {
                valueToSet = placementGroup.Name;
            }

            // 8. Load the registration group member records and update the attribute.
            var groupMembers = new GroupMemberService( rockContext )
                .Queryable()
                .Where( gm => registrationGroupMemberIds.Contains( gm.Id ) )
                .ToList();

            int updatedCount = 0;
            foreach ( var groupMember in groupMembers )
            {
                groupMember.LoadAttributes( rockContext );
                if ( groupMember.Attributes.ContainsKey( attributeKey ) )
                {
                    var attribute = groupMember.Attributes[attributeKey];
                    Rock.Attribute.Helper.SaveAttributeValue( groupMember, attribute, valueToSet, rockContext );
                    updatedCount++;

                    string logAction = isRemoval
                        ? ( string.IsNullOrEmpty( valueToSet )
                            ? "Cleared"
                            : string.Format( "Updated to '{0}' (replacement found)", valueToSet ) )
                        : string.Format( "Set to '{0}'", valueToSet );

                    action.AddLogEntry( string.Format(
                        "{0} attribute '{1}' for {2} on group member #{3}.",
                        logAction, attributeKey, person.FullName, groupMember.Id ) );
                }
                else
                {
                    action.AddLogEntry( string.Format(
                        "The attribute '{0}' does not exist on group member #{1}.",
                        attributeKey, groupMember.Id ), true );
                }
            }

            if ( updatedCount == 0 )
            {
                action.AddLogEntry( "No group member attributes were updated." );
            }

            errorMessages.ForEach( m => action.AddLogEntry( m, true ) );
            return true;
        }

        /// <summary>
        /// Gets the person from the workflow attribute.
        /// </summary>
        private Rock.Model.Person GetPersonFromAttribute( RockContext rockContext, WorkflowAction action, List<string> errorMessages )
        {
            Guid personAttributeGuid = GetAttributeValue( action, "Person" ).AsGuid();
            if ( personAttributeGuid.IsEmpty() )
            {
                errorMessages.Add( "The Person attribute is not configured." );
                return null;
            }

            var personAliasGuid = action.GetWorkflowAttributeValue( personAttributeGuid ).AsGuidOrNull();
            if ( !personAliasGuid.HasValue )
            {
                errorMessages.Add( "The Person attribute does not contain a valid value." );
                return null;
            }

            var person = new PersonAliasService( rockContext )
                .Queryable().AsNoTracking()
                .Where( pa => pa.Guid == personAliasGuid.Value )
                .Select( pa => pa.Person )
                .FirstOrDefault();

            if ( person == null )
            {
                errorMessages.Add( "The person could not be found." );
            }

            return person;
        }

        /// <summary>
        /// Gets the placement group from the workflow attribute.
        /// </summary>
        private Group GetPlacementGroupFromAttribute( RockContext rockContext, WorkflowAction action, List<string> errorMessages )
        {
            Guid groupAttributeGuid = GetAttributeValue( action, "PlacementGroup" ).AsGuid();
            if ( groupAttributeGuid.IsEmpty() )
            {
                errorMessages.Add( "The Placement Group attribute is not configured." );
                return null;
            }

            var groupGuid = action.GetWorkflowAttributeValue( groupAttributeGuid ).AsGuidOrNull();
            if ( !groupGuid.HasValue )
            {
                errorMessages.Add( "The Placement Group attribute does not contain a valid value." );
                return null;
            }

            var group = new GroupService( rockContext ).Get( groupGuid.Value );
            if ( group == null )
            {
                errorMessages.Add( "The placement group could not be found." );
            }

            return group;
        }

        /// <summary>
        /// Finds all registration instance IDs linked to the given placement group.
        /// Checks both instance-level placements (PLACEMENT purpose key) and
        /// template-level shared placements (PLACEMENT-TEMPLATE purpose key).
        /// </summary>
        private List<int> GetLinkedRegistrationInstanceIds( RockContext rockContext, int placementGroupId )
        {
            int groupEntityTypeId = EntityTypeCache.Get( typeof( Group ) ).Id;
            int registrationInstanceEntityTypeId = EntityTypeCache.Get( typeof( RegistrationInstance ) ).Id;
            int templatePlacementEntityTypeId = EntityTypeCache.Get( typeof( RegistrationTemplatePlacement ) ).Id;

            var relatedEntityService = new RelatedEntityService( rockContext );

            // Instance-level: RegistrationInstance -> Group with PurposeKey "PLACEMENT"
            var instanceIds = relatedEntityService.Queryable().AsNoTracking()
                .Where( re =>
                    re.SourceEntityTypeId == registrationInstanceEntityTypeId
                    && re.TargetEntityTypeId == groupEntityTypeId
                    && re.TargetEntityId == placementGroupId
                    && re.PurposeKey == RelatedEntityPurposeKey.RegistrationInstanceGroupPlacement )
                .Select( re => re.SourceEntityId )
                .ToList();

            // Template-level: RegistrationTemplatePlacement -> Group with PurposeKey "PLACEMENT-TEMPLATE"
            // If found, resolve to all active registration instances for those templates.
            var templatePlacementIds = relatedEntityService.Queryable().AsNoTracking()
                .Where( re =>
                    re.SourceEntityTypeId == templatePlacementEntityTypeId
                    && re.TargetEntityTypeId == groupEntityTypeId
                    && re.TargetEntityId == placementGroupId
                    && re.PurposeKey == RelatedEntityPurposeKey.RegistrationTemplateGroupPlacementTemplate )
                .Select( re => re.SourceEntityId )
                .ToList();

            if ( templatePlacementIds.Any() )
            {
                var templateIds = new RegistrationTemplatePlacementService( rockContext )
                    .Queryable().AsNoTracking()
                    .Where( rtp => templatePlacementIds.Contains( rtp.Id ) )
                    .Select( rtp => rtp.RegistrationTemplateId )
                    .ToList();

                var additionalInstanceIds = new RegistrationInstanceService( rockContext )
                    .Queryable().AsNoTracking()
                    .Where( ri => templateIds.Contains( ri.RegistrationTemplateId ) && ri.IsActive )
                    .Select( ri => ri.Id )
                    .ToList();

                instanceIds = instanceIds.Union( additionalInstanceIds ).Distinct().ToList();
            }

            return instanceIds;
        }

        /// <summary>
        /// When a person is removed from a placement group, checks if they are still
        /// a member of another placement group of the same GroupType for the same
        /// registration instance(s). Returns that group's name as a replacement value,
        /// or an empty string if no replacement exists.
        /// 
        /// This handles the scenario where a person was accidentally placed in the
        /// wrong group and then the correct group, and the wrong group is subsequently
        /// removed — the correct group's value is preserved.
        /// </summary>
        private string FindReplacementPlacementGroupName(
            RockContext rockContext,
            Rock.Model.Person person,
            Group removedPlacementGroup,
            List<int> registrationInstanceIds )
        {
            int groupEntityTypeId = EntityTypeCache.Get( typeof( Group ) ).Id;
            int registrationInstanceEntityTypeId = EntityTypeCache.Get( typeof( RegistrationInstance ) ).Id;

            // Find all placement group IDs linked to the same registration instances.
            var allPlacementGroupIds = new RelatedEntityService( rockContext )
                .Queryable().AsNoTracking()
                .Where( re =>
                    re.SourceEntityTypeId == registrationInstanceEntityTypeId
                    && registrationInstanceIds.Contains( re.SourceEntityId )
                    && re.TargetEntityTypeId == groupEntityTypeId
                    && re.PurposeKey == RelatedEntityPurposeKey.RegistrationInstanceGroupPlacement )
                .Select( re => re.TargetEntityId )
                .Distinct()
                .ToList();

            // Find a group of the same GroupType (excluding the removed group) where
            // the person is still an active member.
            var replacementGroup = new GroupService( rockContext )
                .Queryable().AsNoTracking()
                .Where( g =>
                    allPlacementGroupIds.Contains( g.Id )
                    && g.Id != removedPlacementGroup.Id
                    && g.GroupTypeId == removedPlacementGroup.GroupTypeId
                    && g.Members.Any( m =>
                        m.PersonId == person.Id
                        && m.GroupMemberStatus != GroupMemberStatus.Inactive ) )
                .FirstOrDefault();

            return replacementGroup?.Name ?? string.Empty;
        }
    }
}

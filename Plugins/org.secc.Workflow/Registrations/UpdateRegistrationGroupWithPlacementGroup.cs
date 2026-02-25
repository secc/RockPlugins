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
    /// updates the specified attribute.
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
    /// <para><strong>Attribute Key and Value:</strong> Both the attribute key and
    /// value can be set dynamically via workflow attributes. This supports scenarios
    /// where multiple placement types share the same GroupType (e.g., all activity
    /// placement groups use one GroupType, but "Archery - Day 2 1:00PM" should set
    /// the "Archery" attribute to "Day 2 1:00PM"). A prior workflow step can parse
    /// the group name to determine the correct key and value.</para>
    ///
    /// <para><strong>Group Name Prefix:</strong> When multiple placement types share
    /// the same GroupType (e.g., activities), set the Group Name Prefix to scope
    /// both the default value and replacement logic to groups matching that prefix.
    /// For example, with prefix "Archery" and group name "Archery - Day 2 1:00PM":
    /// the default value becomes "Day 2 1:00PM" (prefix and separator stripped),
    /// and removal replacement only considers other "Archery" groups — not Zipline
    /// or other activity groups.</para>
    ///
    /// <para><strong>Removal logic:</strong> When "Is Removal" is true and no
    /// explicit Attribute Value is provided, the action checks if the person is
    /// still in another placement group of the same GroupType (and matching the
    /// Group Name Prefix, if set) for the same registration instance. If so, it
    /// updates the attribute with that group's value. If not, it clears the
    /// attribute. When an explicit Attribute Value IS provided (even if empty),
    /// it is used directly — the prior workflow step is responsible for any
    /// replacement logic.</para>
    ///
    /// <para><strong>Workflow Setup (simple — bus, housing, small group):</strong></para>
    /// <para>1. Create a workflow type triggered by "Group Member Added to Group"
    ///    on the placement group type. Set a fixed Attribute Key and leave
    ///    Attribute Value and Group Name Prefix blank (defaults to the group
    ///    name).</para>
    /// <para>2. Create a matching removal workflow with "Is Removal" = true.</para>
    ///
    /// <para><strong>Workflow Setup (activities — shared group type):</strong></para>
    /// <para>1. Create a workflow triggered by "Group Member Added to Group" on the
    ///    activity placement group type.</para>
    /// <para>2. Add a prior step to parse the activity name from the group name
    ///    (e.g., "Archery" from "Archery - Day 2 1:00PM") and store it in a
    ///    workflow attribute.</para>
    /// <para>3. Add this action, pointing Attribute Key and Group Name Prefix to
    ///    that workflow attribute. Leave Attribute Value blank — the prefix will
    ///    be stripped automatically.</para>
    /// <para>4. For the removal workflow, use the same setup. The built-in
    ///    replacement logic will only consider groups matching the prefix.</para>
    /// </summary>
    [ActionCategory( "SECC > Registrations" )]
    [Description( "Updates a group member attribute on the registration group based on placement group membership. " +
        "Both the attribute key and value can be set dynamically via workflow attributes to support activities " +
        "and other scenarios where the group name needs to be parsed." )]
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

    [WorkflowTextOrAttribute( "Attribute Key", "Attribute Key Attribute",
        "The attribute key on the registration group member to update. " +
        "Can be a fixed text value or a workflow attribute set by a prior step " +
        "(e.g., for activities where the key is parsed from the group name). <span class='tip tip-lava'></span>",
        true, "", "", 2, "GroupMemberAttributeKey" )]

    [WorkflowTextOrAttribute( "Attribute Value", "Attribute Value Attribute",
        "The value to set on the group member attribute. If left blank, defaults to the placement group name " +
        "(with the Group Name Prefix and separator stripped, if set). " +
        "Use a workflow attribute to provide a fully custom value. " +
        "When provided, the built-in removal replacement logic is skipped. <span class='tip tip-lava'></span>",
        false, "", "", 3, "AttributeValue" )]

    [WorkflowTextOrAttribute( "Group Name Prefix", "Group Name Prefix Attribute",
        "Optional. When placement groups of different types share the same GroupType (e.g., activities), " +
        "set this to the activity name (e.g., 'Archery'). This does two things: " +
        "(1) the default value strips the prefix and separator (e.g., 'Archery - Day 2 1:00PM' becomes 'Day 2 1:00PM'), and " +
        "(2) removal replacement logic only considers groups whose names start with this prefix. " +
        "The separator ' - ' is used by default. Leave blank for placement types that have their own GroupType. <span class='tip tip-lava'></span>",
        false, "", "", 4, "GroupNamePrefix" )]

    [BooleanField( "Is Removal",
        "Set to true when this action is triggered by a group member removal. " +
        "When no explicit Attribute Value is provided, the action will check if the person is still in " +
        "another placement group of the same type (filtered by Group Name Prefix if set) " +
        "and update the attribute accordingly, or clear it if not. " +
        "When an explicit Attribute Value IS provided, it is used directly.",
        false, "", 5, "IsRemoval" )]

    public class UpdateRegistrationGroupWithPlacementGroup : ActionComponent
    {
        /// <summary>
        /// The separator used between the group name prefix and the value portion
        /// of a placement group name (e.g., "Archery - Day 2 1:00PM").
        /// </summary>
        private const string GroupNameSeparator = " - ";

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

            // 3. Read the attribute key (supports text, Lava, or workflow attribute).
            string attributeKey = ResolveTextOrAttribute( action, "GroupMemberAttributeKey" );
            if ( !string.IsNullOrWhiteSpace( attributeKey ) )
            {
                attributeKey = attributeKey.Replace( " ", "" );
            }

            if ( string.IsNullOrWhiteSpace( attributeKey ) )
            {
                errorMessages.Add( "The group member attribute key is required." );
                errorMessages.ForEach( m => action.AddLogEntry( m, true ) );
                return false;
            }

            // 4. Read the optional explicit attribute value.
            //    If the action's AttributeValue field is configured (non-empty config),
            //    use the resolved value directly — even if it resolves to empty string.
            //    If the field is not configured at all, fall back to default behavior.
            string attributeValueConfig = GetAttributeValue( action, "AttributeValue" );
            bool hasExplicitValue = !string.IsNullOrWhiteSpace( attributeValueConfig );
            string explicitValue = null;
            if ( hasExplicitValue )
            {
                explicitValue = ResolveTextOrAttribute( action, "AttributeValue" ) ?? string.Empty;
            }

            // 5. Read the optional group name prefix for filtering and value extraction.
            string groupNamePrefix = ResolveTextOrAttribute( action, "GroupNamePrefix" );
            bool hasGroupNamePrefix = !string.IsNullOrWhiteSpace( groupNamePrefix );

            bool isRemoval = GetAttributeValue( action, "IsRemoval" ).AsBoolean();

            // 6. Find registration instances linked to this placement group.
            //    This checks both instance-level and template-level placement configurations.
            var registrationInstanceIds = GetLinkedRegistrationInstanceIds( rockContext, placementGroup.Id );
            if ( !registrationInstanceIds.Any() )
            {
                action.AddLogEntry( string.Format(
                    "No registration instances found linked to placement group '{0}' (Id: {1}).",
                    placementGroup.Name, placementGroup.Id ) );
                return true;
            }

            // 7. Get all PersonAliasIds for this person (to match across aliases).
            var personAliasIds = new PersonAliasService( rockContext )
                .Queryable().AsNoTracking()
                .Where( pa => pa.PersonId == person.Id )
                .Select( pa => pa.Id )
                .ToList();

            // 8. Find RegistrationRegistrant records that link this person to a
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

            // 9. Determine the attribute value to set.
            string valueToSet;
            if ( hasExplicitValue )
            {
                // An explicit value was provided (possibly by a prior workflow step
                // that parsed the group name). Use it directly — the caller is
                // responsible for any replacement logic on removal.
                valueToSet = explicitValue;
            }
            else if ( isRemoval )
            {
                // No explicit value and this is a removal. Check if the person is
                // still in another placement group of the same GroupType (and matching
                // the group name prefix, if set). If so, use that group's name
                // (with prefix stripped). Otherwise, clear it.
                valueToSet = FindReplacementPlacementGroupValue(
                    rockContext, person, placementGroup, registrationInstanceIds, groupNamePrefix );
            }
            else
            {
                // No explicit value and this is an addition. Use the placement
                // group name, stripping the prefix and separator if configured.
                valueToSet = StripGroupNamePrefix( placementGroup.Name, groupNamePrefix );
            }

            // 10. Load the registration group member records and update the attribute.
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
        /// Strips the group name prefix and separator from a placement group name.
        /// For example, with prefix "Archery" and name "Archery - Day 2 1:00PM",
        /// returns "Day 2 1:00PM". If no prefix is set or the name doesn't start
        /// with the prefix, the full name is returned.
        /// </summary>
        private string StripGroupNamePrefix( string groupName, string prefix )
        {
            if ( string.IsNullOrWhiteSpace( prefix ) || string.IsNullOrWhiteSpace( groupName ) )
            {
                return groupName ?? string.Empty;
            }

            string prefixWithSeparator = prefix + GroupNameSeparator;
            if ( groupName.StartsWith( prefixWithSeparator, StringComparison.OrdinalIgnoreCase ) )
            {
                return groupName.Substring( prefixWithSeparator.Length ).Trim();
            }

            // If the name equals just the prefix with no separator/suffix, return empty.
            if ( groupName.Equals( prefix, StringComparison.OrdinalIgnoreCase ) )
            {
                return string.Empty;
            }

            // Prefix doesn't match the expected pattern — return the full name.
            return groupName;
        }

        /// <summary>
        /// Resolves a WorkflowTextOrAttribute value. If the configured value is a
        /// valid Guid, it is treated as a workflow attribute reference and the
        /// attribute's value is returned. Otherwise, the text is resolved as Lava
        /// using the action's merge fields.
        /// </summary>
        private string ResolveTextOrAttribute( WorkflowAction action, string configKey )
        {
            string rawValue = GetAttributeValue( action, configKey );
            if ( string.IsNullOrWhiteSpace( rawValue ) )
            {
                return rawValue;
            }

            Guid guid = rawValue.AsGuid();
            if ( !guid.IsEmpty() )
            {
                // Value is a workflow attribute Guid — resolve to the attribute's value.
                return action.GetWorkflowAttributeValue( guid );
            }
            else
            {
                // Value is literal text — resolve any Lava merge fields.
                return rawValue.ResolveMergeFields( GetMergeFields( action ) );
            }
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
        /// a member of another placement group of the same GroupType (and optionally
        /// matching a group name prefix) for the same registration instance(s).
        /// Returns the replacement group's value (with prefix stripped if applicable),
        /// or an empty string if no replacement exists.
        ///
        /// When <paramref name="groupNamePrefix"/> is set, only groups whose names
        /// start with the prefix are considered as replacements. This prevents an
        /// Archery removal from picking up a Zipline group when both share the same
        /// Activity group type.
        /// </summary>
        private string FindReplacementPlacementGroupValue(
            RockContext rockContext,
            Rock.Model.Person person,
            Group removedPlacementGroup,
            List<int> registrationInstanceIds,
            string groupNamePrefix )
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

            // Build the query for replacement groups: same GroupType, different group,
            // person is still an active member.
            var replacementQuery = new GroupService( rockContext )
                .Queryable().AsNoTracking()
                .Where( g =>
                    allPlacementGroupIds.Contains( g.Id )
                    && g.Id != removedPlacementGroup.Id
                    && g.GroupTypeId == removedPlacementGroup.GroupTypeId
                    && g.Members.Any( m =>
                        m.PersonId == person.Id
                        && m.GroupMemberStatus != GroupMemberStatus.Inactive ) );

            // If a group name prefix is set, only consider groups matching it.
            // This prevents cross-activity contamination (e.g., Archery removal
            // picking up a Zipline group).
            bool hasPrefix = !string.IsNullOrWhiteSpace( groupNamePrefix );
            if ( hasPrefix )
            {
                string prefixWithSeparator = groupNamePrefix + GroupNameSeparator;
                replacementQuery = replacementQuery.Where( g =>
                    g.Name.StartsWith( prefixWithSeparator )
                    || g.Name == groupNamePrefix );
            }

            var replacementGroup = replacementQuery.FirstOrDefault();

            if ( replacementGroup == null )
            {
                return string.Empty;
            }

            // Strip the prefix from the replacement group's name to get the value.
            return StripGroupNamePrefix( replacementGroup.Name, groupNamePrefix );
        }
    }
}

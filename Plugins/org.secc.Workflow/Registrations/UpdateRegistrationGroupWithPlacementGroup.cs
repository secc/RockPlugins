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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.Entity;
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
    /// <para><strong>Lava Merge Fields:</strong> The Attribute Key, Attribute Value,
    /// and Group Name Prefix fields all support Lava with two custom merge fields:</para>
    /// <para>• <c>PlacementGroupName</c> — Always the name of the placement group
    ///   that triggered the workflow (the group being added to or removed from).</para>
    /// <para>• <c>GroupName</c> — The "effective" group name: on add, this equals
    ///   PlacementGroupName; on removal, this is the replacement group name (or
    ///   empty if no replacement exists). Use this in the Attribute Value field so
    ///   the same Lava template works for both add and removal scenarios.</para>
    ///
    /// <para><strong>Group Name Prefix:</strong> When multiple placement types share
    /// the same GroupType (e.g., activities), set the Group Name Prefix to scope
    /// replacement logic to groups matching that prefix. For example, with prefix
    /// "Archery", removal replacement only considers other "Archery" groups — not
    /// Zipline or other activity groups. The prefix is also stripped from the default
    /// value when no explicit Attribute Value is configured.</para>
    ///
    /// <para><strong>Removal logic:</strong> When "Is Removal" is true, the action
    /// finds a replacement placement group of the same GroupType (and matching the
    /// Group Name Prefix, if set) for the same registration instance. The
    /// replacement group name is exposed as the <c>GroupName</c> merge field. If no
    /// replacement exists, <c>GroupName</c> is empty. When no explicit Attribute
    /// Value is configured, the default value is <c>GroupName</c> with the prefix
    /// stripped (or empty to clear the attribute).</para>
    ///
    /// <para><strong>Workflow Setup (simple — bus, small group):</strong></para>
    /// <para>Create an add workflow and a removal workflow on the placement group
    /// type. Set a fixed Attribute Key (e.g., "BusAssignment") and leave Attribute
    /// Value and Group Name Prefix blank. The full group name is used as the value
    /// and replacement works automatically.</para>
    ///
    /// <para><strong>Workflow Setup (housing — two attributes from one group):</strong></para>
    /// <para>Use two instances of this action in each workflow. For group name
    /// "Cedar Room 4":</para>
    /// <para>• Action 1: Key = "Dorm", Value = <c>{{ GroupName | Split:' Room ' | First }}</c></para>
    /// <para>• Action 2: Key = "Room", Value = <c>{{ GroupName | Split:' Room ' | Last }}</c></para>
    /// <para>On removal, <c>GroupName</c> automatically resolves to the replacement
    /// group name (or empty), so the same Lava works for both actions.</para>
    ///
    /// <para><strong>Workflow Setup (activities — shared group type):</strong></para>
    /// <para>For group name "Archery - Day 2 1:00PM":</para>
    /// <para>• Attribute Key = <c>{{ PlacementGroupName | Split:' - ' | First }}</c>
    ///   (resolves to "Archery")</para>
    /// <para>• Group Name Prefix = <c>{{ PlacementGroupName | Split:' - ' | First }}</c>
    ///   (same Lava; filters replacement to only other Archery groups)</para>
    /// <para>• Leave Attribute Value blank — the prefix is stripped automatically,
    ///   yielding "Day 2 1:00PM".</para>
    /// </summary>
    [ActionCategory( "SECC > Registrations" )]
    [Description( "Updates a group member attribute on the registration group based on placement group membership. " +
        "Both the attribute key and value can be set dynamically via Lava (with PlacementGroupName and GroupName " +
        "merge fields) or workflow attributes to support housing, activities, and other scenarios where the " +
        "group name needs to be parsed." )]
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
        "Can be a fixed text value, a workflow attribute, or Lava referencing {{ PlacementGroupName }} " +
        "(e.g., <code>{{ PlacementGroupName | Split:' - ' | First }}</code> for activities). <span class='tip tip-lava'></span>",
        true, "", "", 2, "GroupMemberAttributeKey" )]

    [WorkflowTextOrAttribute( "Attribute Value", "Attribute Value Attribute",
        "The value to set on the group member attribute. If left blank, defaults to the group name " +
        "(with the Group Name Prefix and separator stripped, if set). " +
        "Supports Lava with {{ GroupName }} (effective group — placement on add, replacement on removal) " +
        "and {{ PlacementGroupName }} (always the triggering group). " +
        "For housing: <code>{{ GroupName | Split:' Room ' | First }}</code>. <span class='tip tip-lava'></span>",
        false, "", "", 3, "AttributeValue" )]

    [WorkflowTextOrAttribute( "Group Name Prefix", "Group Name Prefix Attribute",
        "Optional. When placement groups of different types share the same GroupType (e.g., activities), " +
        "set this to scope replacement logic and strip the prefix from the default value. " +
        "Supports Lava: <code>{{ PlacementGroupName | Split:' - ' | First }}</code>. " +
        "The separator ' - ' is used for stripping. Leave blank for placement types with their own GroupType. <span class='tip tip-lava'></span>",
        false, "", "", 4, "GroupNamePrefix" )]

    [BooleanField( "Is Removal",
        "Set to true when this action is triggered by a group member removal. " +
        "The action will look for a replacement placement group of the same type (filtered by Group Name Prefix). " +
        "The replacement group name is available as {{ GroupName }} in the Attribute Value Lava template. " +
        "If no replacement exists, {{ GroupName }} is empty and the attribute will be cleared.",
        false, "", 5, "IsRemoval" )]

    [WorkflowTextOrAttribute( "Workflow Group Attribute Key", "Workflow Group Attribute Key Attribute",
        "Optional fallback for registrants not directly placed in the registration group (e.g., leaders " +
        "who go through a connections process). Specify the attribute key on the WorkflowType referenced " +
        "by the registration instance's RegistrationWorkflowTypeId that contains the registration group. " +
        "When the initial registration group member lookup returns no results, the action reads this " +
        "attribute from the WorkflowType to identify the registration group and find the person's " +
        "group member record within it by matching PersonId.",
        false, "", "", 6, "WorkflowGroupAttributeKey" )]

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

            bool isRemoval = GetAttributeValue( action, "IsRemoval" ).AsBoolean();

            // 3. Build custom merge fields so that Lava in the Attribute Key, Value,
            //    and Group Name Prefix fields can reference the placement group name.
            //    PlacementGroupName is always the group that triggered the workflow.
            //    GroupName will be set in step 9 to the effective group name.
            var customMergeFields = new Dictionary<string, object>
            {
                { "PlacementGroupName", placementGroup.Name }
            };

            // 4. Resolve the attribute key (supports text, Lava, or workflow attribute).
            //    Lava can reference {{ PlacementGroupName }} to derive the key from the
            //    group name (e.g., {{ PlacementGroupName | Split:' - ' | First }} for activities).
            string attributeKey = ResolveTextOrAttribute( action, "GroupMemberAttributeKey", customMergeFields );
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

            // 5. Resolve the optional group name prefix for replacement filtering.
            //    Lava can reference {{ PlacementGroupName }} here too.
            string groupNamePrefix = ResolveTextOrAttribute( action, "GroupNamePrefix", customMergeFields );
            if ( !string.IsNullOrWhiteSpace( groupNamePrefix ) )
            {
                groupNamePrefix = groupNamePrefix.Trim();
            }

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
                // 8b. Fallback: check the WorkflowType configured on the registration
                //     instance for a group attribute (e.g., leaders placed via connections).
                string workflowGroupAttrKey = ResolveTextOrAttribute( action, "WorkflowGroupAttributeKey", customMergeFields );
                if ( !string.IsNullOrWhiteSpace( workflowGroupAttrKey ) )
                {
                    registrationGroupMemberIds = GetGroupMemberIdsFromRegistrationWorkflowType(
                        rockContext, registrationInstanceIds, workflowGroupAttrKey, person.Id );

                    if ( registrationGroupMemberIds.Any() )
                    {
                        action.AddLogEntry( string.Format(
                            "Found {0} group member(s) for '{1}' via registration WorkflowType attribute '{2}'.",
                            registrationGroupMemberIds.Count, person.FullName, workflowGroupAttrKey ) );
                    }
                }

                if ( !registrationGroupMemberIds.Any() )
                {
                    action.AddLogEntry( string.Format(
                        "No registrant records with linked group members found for '{0}' in the matching registration instances.",
                        person.FullName ) );
                    return true;
                }
            }

            // 9. Determine the effective group name and add it to merge fields.
            //    On add: GroupName = placement group name.
            //    On removal: find replacement group of the same GroupType (and
            //    matching prefix); GroupName = replacement name or empty.
            string effectiveGroupName;
            bool replacementFound = false;
            if ( isRemoval )
            {
                var replacementGroup = FindReplacementPlacementGroup(
                    rockContext, person, placementGroup, registrationInstanceIds, groupNamePrefix );
                replacementFound = replacementGroup != null;
                effectiveGroupName = replacementFound ? replacementGroup.Name : string.Empty;
            }
            else
            {
                effectiveGroupName = placementGroup.Name;
            }

            customMergeFields["GroupName"] = effectiveGroupName;

            // 10. Determine the attribute value to set.
            //     If an explicit Attribute Value is configured, resolve it with merge
            //     fields (Lava can use {{ GroupName }}, {{ PlacementGroupName }}).
            //     Otherwise, use the effective group name with prefix stripped as the default.
            string attributeValueConfig = GetAttributeValue( action, "AttributeValue" );
            bool hasExplicitValue = !string.IsNullOrWhiteSpace( attributeValueConfig );

            string valueToSet;
            if ( hasExplicitValue )
            {
                valueToSet = ResolveTextOrAttribute( action, "AttributeValue", customMergeFields ) ?? string.Empty;
            }
            else
            {
                // Default: use the effective group name, stripping the prefix if set.
                valueToSet = StripGroupNamePrefix( effectiveGroupName, groupNamePrefix );
            }

            // 11. Load the registration group member records and update the attribute.
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
                        ? ( replacementFound
                            ? string.Format( "Updated to '{0}' (replacement found)", valueToSet )
                            : "Cleared" )
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
        /// using the action's merge fields plus any additional merge fields provided.
        /// </summary>
        private string ResolveTextOrAttribute( WorkflowAction action, string configKey, Dictionary<string, object> additionalMergeFields = null )
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
                var mergeFields = GetMergeFields( action );
                if ( additionalMergeFields != null )
                {
                    foreach ( var kvp in additionalMergeFields )
                    {
                        mergeFields[kvp.Key] = kvp.Value;
                    }
                }

                return rawValue.ResolveMergeFields( mergeFields );
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
        /// When a person is removed from a placement group, finds a replacement
        /// placement group of the same GroupType (and optionally matching a group
        /// name prefix) for the same registration instance(s) where the person is
        /// still an active member.
        ///
        /// Returns the replacement Group, or null if no replacement exists.
        ///
        /// When <paramref name="groupNamePrefix"/> is set, only groups whose names
        /// start with the prefix are considered. This prevents an Archery removal
        /// from picking up a Zipline group when both share the same GroupType.
        /// </summary>
        private Group FindReplacementPlacementGroup(
            RockContext rockContext,
            Rock.Model.Person person,
            Group removedPlacementGroup,
            List<int> registrationInstanceIds,
            string groupNamePrefix )
        {
            int groupEntityTypeId = EntityTypeCache.Get( typeof( Group ) ).Id;
            int registrationInstanceEntityTypeId = EntityTypeCache.Get( typeof( RegistrationInstance ) ).Id;
            int templatePlacementEntityTypeId = EntityTypeCache.Get( typeof( RegistrationTemplatePlacement ) ).Id;

            var relatedEntityService = new RelatedEntityService( rockContext );

            // Instance-level: placement groups linked directly to the registration instances.
            var allPlacementGroupIds = relatedEntityService.Queryable().AsNoTracking()
                .Where( re =>
                    re.SourceEntityTypeId == registrationInstanceEntityTypeId
                    && registrationInstanceIds.Contains( re.SourceEntityId )
                    && re.TargetEntityTypeId == groupEntityTypeId
                    && re.PurposeKey == RelatedEntityPurposeKey.RegistrationInstanceGroupPlacement )
                .Select( re => re.TargetEntityId )
                .Distinct()
                .ToList();

            // Template-level: resolve the registration instances back to their template
            // placements, then find all groups linked at the template level.
            var templateIds = new RegistrationInstanceService( rockContext )
                .Queryable().AsNoTracking()
                .Where( ri => registrationInstanceIds.Contains( ri.Id ) )
                .Select( ri => ri.RegistrationTemplateId )
                .Distinct()
                .ToList();

            var templatePlacementIds = new RegistrationTemplatePlacementService( rockContext )
                .Queryable().AsNoTracking()
                .Where( rtp => templateIds.Contains( rtp.RegistrationTemplateId ) )
                .Select( rtp => rtp.Id )
                .ToList();

            if ( templatePlacementIds.Any() )
            {
                var templatePlacementGroupIds = relatedEntityService.Queryable().AsNoTracking()
                    .Where( re =>
                        re.SourceEntityTypeId == templatePlacementEntityTypeId
                        && templatePlacementIds.Contains( re.SourceEntityId )
                        && re.TargetEntityTypeId == groupEntityTypeId
                        && re.PurposeKey == RelatedEntityPurposeKey.RegistrationTemplateGroupPlacementTemplate )
                    .Select( re => re.TargetEntityId )
                    .Distinct()
                    .ToList();

                allPlacementGroupIds = allPlacementGroupIds
                    .Union( templatePlacementGroupIds )
                    .Distinct()
                    .ToList();
            }

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
            if ( !string.IsNullOrWhiteSpace( groupNamePrefix ) )
            {
                string prefixWithSeparator = groupNamePrefix + GroupNameSeparator;
                replacementQuery = replacementQuery.Where( g =>
                    g.Name.StartsWith( prefixWithSeparator )
                    || g.Name == groupNamePrefix );
            }

            return replacementQuery.FirstOrDefault();
        }

        /// <summary>
        /// Fallback for registrants not directly placed in the registration group
        /// (e.g., leaders who go through a connections process). Looks up the
        /// workflow attribute definition (by key) for the WorkflowType configured
        /// on each linked registration instance (via RegistrationWorkflowTypeId)
        /// and reads its DefaultValue to get the target registration group, then
        /// finds the person's GroupMember record in that group by matching PersonId.
        /// </summary>
        private List<int> GetGroupMemberIdsFromRegistrationWorkflowType(
            RockContext rockContext,
            List<int> registrationInstanceIds,
            string workflowGroupAttributeKey,
            int personId )
        {
            // Get the RegistrationWorkflowTypeIds from the linked registration instances.
            var workflowTypeIds = new RegistrationInstanceService( rockContext )
                .Queryable().AsNoTracking()
                .Where( ri =>
                    registrationInstanceIds.Contains( ri.Id )
                    && ri.RegistrationWorkflowTypeId.HasValue )
                .Select( ri => ri.RegistrationWorkflowTypeId.Value )
                .Distinct()
                .ToList();

            if ( !workflowTypeIds.Any() )
            {
                return new List<int>();
            }

            // Workflow attributes are stored as Attribute records where:
            //   EntityTypeId = Workflow entity type
            //   EntityTypeQualifierColumn = "WorkflowTypeId"
            //   EntityTypeQualifierValue = the WorkflowType's Id
            // The group is stored in the Attribute's DefaultValue.
            int workflowEntityTypeId = EntityTypeCache.Get( typeof( Rock.Model.Workflow ) ).Id;
            var workflowTypeIdStrings = workflowTypeIds.Select( id => id.ToString() ).ToList();

            var defaultValues = new AttributeService( rockContext )
                .Queryable().AsNoTracking()
                .Where( a =>
                    a.EntityTypeId == workflowEntityTypeId
                    && a.EntityTypeQualifierColumn == "WorkflowTypeId"
                    && workflowTypeIdStrings.Contains( a.EntityTypeQualifierValue )
                    && a.Key == workflowGroupAttributeKey )
                .Select( a => a.DefaultValue )
                .ToList();

            var groupMemberIds = new List<int>();
            var groupMemberService = new GroupMemberService( rockContext );

            foreach ( var groupValue in defaultValues )
            {
                if ( string.IsNullOrWhiteSpace( groupValue ) )
                {
                    continue;
                }

                // The default value may be a Group Guid (Group field type)
                // or an integer Group Id.
                int? groupId = null;
                var groupGuid = groupValue.AsGuidOrNull();
                if ( groupGuid.HasValue )
                {
                    groupId = new GroupService( rockContext )
                        .Queryable().AsNoTracking()
                        .Where( g => g.Guid == groupGuid.Value )
                        .Select( g => g.Id )
                        .Cast<int?>()
                        .FirstOrDefault();
                }
                else
                {
                    groupId = groupValue.AsIntegerOrNull();
                }

                if ( !groupId.HasValue )
                {
                    continue;
                }

                // Find this person's group member record in the target group.
                var memberIds = groupMemberService
                    .Queryable().AsNoTracking()
                    .Where( gm =>
                        gm.GroupId == groupId.Value
                        && gm.PersonId == personId )
                    .Select( gm => gm.Id )
                    .ToList();

                groupMemberIds.AddRange( memberIds );
            }

            return groupMemberIds.Distinct().ToList();
        }
    }
}

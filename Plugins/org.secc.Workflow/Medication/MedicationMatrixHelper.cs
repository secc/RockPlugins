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
using System.Collections.Generic;
using System.Linq;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Workflow.Medication
{
    /// <summary>
    /// Helpers for ROCK-8501 master → snapshot medication active-state sync.
    /// Called by <see cref="PropagateMedicationActiveState"/>.
    /// </summary>
    public static class MedicationMatrixHelper
    {
        // Matrix row attribute keys (on the medication matrix template).
        public const string MedicationActiveKey = "MedicationActive";
        public const string MedicationKey = "Medication";
        public const string InstructionsKey = "Instructions";
        public const string ScheduleKey = "Schedule";

        /// <summary>
        /// True when a Person-entity attribute value references this matrix's Guid.
        /// Per-event snapshots are referenced by GroupMember-entity attributes instead.
        /// </summary>
        public static bool IsMasterMatrix( AttributeMatrix matrix, RockContext rockContext )
        {
            if ( matrix == null )
            {
                return false;
            }

            var personEntityId = EntityTypeCache.GetId<Rock.Model.Person>();
            if ( !personEntityId.HasValue )
            {
                return false;
            }

            var matrixGuidStr = matrix.Guid.ToString();
            return new AttributeValueService( rockContext ).Queryable()
                .Any( av => av.Value == matrixGuidStr
                    && av.Attribute.EntityTypeId == personEntityId.Value );
        }

        /// <summary>
        /// Writes the master item's new MedicationActive value to every snapshot
        /// matrix item the same Person has that matches by Medication + Instructions + Schedule.
        /// Snapshot writes don't re-trigger the workflow because <see cref="IsMasterMatrix"/>
        /// returns false for them — automatic loop prevention.
        /// </summary>
        public static void PropagateActiveStateToSnapshots(
            AttributeMatrixItem masterItem,
            bool active,
            RockContext rockContext )
        {
            if ( masterItem == null || masterItem.AttributeMatrix == null )
            {
                return;
            }

            masterItem.LoadAttributes( rockContext );
            var medication = ( masterItem.GetAttributeValue( MedicationKey ) ?? string.Empty ).Trim();
            var instructions = ( masterItem.GetAttributeValue( InstructionsKey ) ?? string.Empty ).Trim();
            var schedule = ( masterItem.GetAttributeValue( ScheduleKey ) ?? string.Empty ).Trim();

            var masterMatrixGuidStr = masterItem.AttributeMatrix.Guid.ToString();

            var personEntityId = EntityTypeCache.GetId<Rock.Model.Person>();
            var groupMemberEntityId = EntityTypeCache.GetId<Rock.Model.GroupMember>();
            if ( !personEntityId.HasValue || !groupMemberEntityId.HasValue )
            {
                return;
            }

            var attributeValueService = new AttributeValueService( rockContext );

            // Find the Person who owns this matrix.
            var personId = attributeValueService.Queryable()
                .Where( av => av.Value == masterMatrixGuidStr
                    && av.Attribute.EntityTypeId == personEntityId.Value )
                .Select( av => av.EntityId )
                .FirstOrDefault();

            if ( !personId.HasValue )
            {
                return;
            }

            // Active group memberships → potential snapshot owners.
            var groupMemberIds = new GroupMemberService( rockContext ).Queryable()
                .Where( gm => gm.PersonId == personId.Value
                    && gm.GroupMemberStatus == GroupMemberStatus.Active )
                .Select( gm => gm.Id )
                .ToList();

            if ( !groupMemberIds.Any() )
            {
                return;
            }

            // Snapshot matrix Guids referenced by those group members.
            var snapshotMatrixGuidStrings = attributeValueService.Queryable()
                .Where( av => av.Attribute.EntityTypeId == groupMemberEntityId.Value
                    && av.EntityId.HasValue
                    && groupMemberIds.Contains( av.EntityId.Value )
                    && av.Value != null && av.Value != "" )
                .Select( av => av.Value )
                .Distinct()
                .ToList();

            var snapshotMatrixGuids = snapshotMatrixGuidStrings
                .Select( s => s.AsGuidOrNull() )
                .Where( g => g.HasValue && g.Value != System.Guid.Empty )
                .Select( g => g.Value )
                .ToList();

            if ( !snapshotMatrixGuids.Any() )
            {
                return;
            }

            var attributeMatrixService = new AttributeMatrixService( rockContext );
            var snapshotMatrixIds = attributeMatrixService.Queryable()
                .Where( am => snapshotMatrixGuids.Contains( am.Guid ) )
                .Select( am => am.Id )
                .ToList();

            if ( !snapshotMatrixIds.Any() )
            {
                return;
            }

            var attributeMatrixItemService = new AttributeMatrixItemService( rockContext );
            var snapshotItems = attributeMatrixItemService.Queryable()
                .Where( ami => snapshotMatrixIds.Contains( ami.AttributeMatrixId ) )
                .ToList();

            // Match by exact-string composite key. Schedule comparison is order-sensitive
            // ("A,B" != "B,A"); snapshots are copied from the master so this is fine in
            // practice but would miss matches if either side is hand-edited and re-ordered.
            var itemsToUpdate = new List<AttributeMatrixItem>();
            foreach ( var item in snapshotItems )
            {
                item.LoadAttributes( rockContext );

                var snapMed = ( item.GetAttributeValue( MedicationKey ) ?? string.Empty ).Trim();
                var snapIns = ( item.GetAttributeValue( InstructionsKey ) ?? string.Empty ).Trim();
                var snapSch = ( item.GetAttributeValue( ScheduleKey ) ?? string.Empty ).Trim();

                if ( snapMed == medication && snapIns == instructions && snapSch == schedule )
                {
                    var currentActive = item.GetAttributeValue( MedicationActiveKey ).AsBoolean( true );
                    if ( currentActive != active )
                    {
                        item.SetAttributeValue( MedicationActiveKey, active.ToString() );
                        itemsToUpdate.Add( item );
                    }
                }
            }

            foreach ( var item in itemsToUpdate )
            {
                item.SaveAttributeValues( rockContext );
            }
        }
    }
}

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
    /// Shared helpers for the master → snapshot medication active-state sync feature
    /// that backs ROCK-8501. The ActionComponent <see cref="PropagateMedicationActiveState"/>
    /// is the only caller today; the helper is split out so the matrix-classification
    /// and propagation logic stay readable.
    /// </summary>
    public static class MedicationMatrixHelper
    {
        /// <summary>Attribute key used for the per-row active flag on the medication matrix template.</summary>
        public const string MedicationActiveKey = "MedicationActive";

        /// <summary>Attribute key for the medication name on each matrix row.</summary>
        public const string MedicationKey = "Medication";

        /// <summary>Attribute key for the instructions text on each matrix row.</summary>
        public const string InstructionsKey = "Instructions";

        /// <summary>
        /// Attribute key for the comma-delimited Schedule defined-value-guid list on each matrix row.
        /// </summary>
        public const string ScheduleKey = "Schedule";

        /// <summary>
        /// A matrix is a Person "master" when at least one Person-entity attribute value
        /// references its Guid. Snapshot matrices are referenced by GroupMember-entity
        /// attribute values instead. This is the inverse of <c>IsSnapshotMatrix</c> used
        /// in the camp dispense block.
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
        /// Given a freshly-modified Person master matrix item, write its new
        /// MedicationActive value to every snapshot matrix item belonging to
        /// the same Person that matches by composite key (Medication +
        /// Instructions + Schedule).
        ///
        /// Snapshots are GroupMember-entity matrix copies created at camp
        /// registration time. Because the trigger only fires for master saves
        /// (see <see cref="IsMasterMatrix"/>), the snapshot AV writes this
        /// method performs do NOT re-trigger the workflow — natural loop
        /// prevention without a recursion guard flag.
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

            // The PersonId is the EntityId on the Person AV that holds this matrix's Guid.
            var personId = attributeValueService.Queryable()
                .Where( av => av.Value == masterMatrixGuidStr
                    && av.Attribute.EntityTypeId == personEntityId.Value )
                .Select( av => av.EntityId )
                .FirstOrDefault();

            if ( !personId.HasValue )
            {
                return;
            }

            // Active GroupMember ids for this person — the entities that may carry per-event snapshot matrices.
            var groupMemberIds = new GroupMemberService( rockContext ).Queryable()
                .Where( gm => gm.PersonId == personId.Value
                    && gm.GroupMemberStatus == GroupMemberStatus.Active )
                .Select( gm => gm.Id )
                .ToList();

            if ( !groupMemberIds.Any() )
            {
                return;
            }

            // The snapshot matrices these GroupMembers reference (via any GroupMember-entity attribute).
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

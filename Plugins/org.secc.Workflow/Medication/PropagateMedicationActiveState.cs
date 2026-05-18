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
using System.ComponentModel;
using System.ComponentModel.Composition;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;

namespace org.secc.Workflow.Medication
{
    /// <summary>
    /// ROCK-8501: fires on AttributeValue PostSave for MedicationActive and propagates
    /// the new value from the Person master matrix to all matching snapshot matrices.
    /// Wired up by <c>Migrations/001_MedicationActiveSyncTrigger.cs</c>.
    ///
    /// Kill switch: <c>UPDATE WorkflowTrigger SET IsActive = 0 WHERE [Guid] = 'CC5A8B9C-1A2B-4C3D-9E5F-6A7B8C9D0E1F'</c>
    /// </summary>
    [ActionCategory( "SECC > Medication" )]
    [Description( "Propagates MedicationActive changes from a Person master medication matrix to all matching snapshot matrix items." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Propagate Medication Active State" )]
    public class PropagateMedicationActiveState : ActionComponent
    {
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            var av = entity as AttributeValue;
            if ( av == null )
            {
                return true;
            }

            // Defense in depth: trigger qualifier should already narrow firings.
            var attribute = AttributeCache.Get( av.AttributeId );
            if ( attribute == null || attribute.Key != MedicationMatrixHelper.MedicationActiveKey )
            {
                return true;
            }

            var matrixItemEntityTypeId = EntityTypeCache.GetId<Rock.Model.AttributeMatrixItem>();
            if ( !matrixItemEntityTypeId.HasValue || attribute.EntityTypeId != matrixItemEntityTypeId.Value )
            {
                return true;
            }

            var ami = new AttributeMatrixItemService( rockContext ).Get( av.EntityId ?? 0 );
            if ( ami == null || ami.AttributeMatrix == null )
            {
                return true;
            }

            // Skip snapshots — propagating those would loop.
            if ( !MedicationMatrixHelper.IsMasterMatrix( ami.AttributeMatrix, rockContext ) )
            {
                return true;
            }

            MedicationMatrixHelper.PropagateActiveStateToSnapshots(
                ami,
                av.Value.AsBoolean( true ),
                rockContext );

            return true;
        }
    }
}

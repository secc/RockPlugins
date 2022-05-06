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
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using org.secc.Imaging.AI;
using Rock.Data;
using Rock.Model;

namespace RockWeb.Plugins.org_secc.Imaging
{
    /// <summary>
    /// OAuth login prompts user for login credentials.
    /// </summary>
    [DisplayName( "Update Person Image" )]
    [Category( "SECC > Imaging" )]
    [Description( "Uses sql to update person images from binary files cropping the images with AI" )]

    public partial class UpdatePersonImage : Rock.Web.UI.RockBlock
    {
        #region Base Control Methods



        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );


        }

        #endregion

        protected void btnRun_Click( object sender, EventArgs e )
        {
            FaceCrop face = new FaceCrop();

            var sql = ceQuery.Text;

            RockContext rockContext = new RockContext();
            var personService = new PersonService( rockContext );
            var binaryFileService = new BinaryFileService( rockContext );
            var rows = rockContext.Database.SqlQuery<ResponseData>( sql );
            foreach ( var row in rows )
            {
                var person = personService.GetNoTracking( row.PersonId );
                var binaryFile = binaryFileService.GetNoTracking( row.BinaryFileId );
                if ( person != null && binaryFile != null )
                {
                    var task = Task.Run( async () => await face.UpdatePhoto( person, binaryFile ) );
                }
            }

        }

        class ResponseData
        {
            public int PersonId { get; set; }
            public int BinaryFileId { get; set; }
        }

    }
}

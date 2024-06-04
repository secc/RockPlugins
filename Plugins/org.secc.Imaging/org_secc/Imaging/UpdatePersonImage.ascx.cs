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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using org.secc.Imaging.AI;
using Rock;
using Rock.Attribute;
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
    [IntegerField("Execution Delay","How many milliseconds to delay between each execution to stay under transaction per second limit.",false,0)]

    public partial class UpdatePersonImage : Rock.Web.UI.RockBlock
    {
        protected static class AttributeKeys
        {
            internal const string ExecutionDelay = "ExecutionDelay";
        }

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

            var templateIds = rtpTemplatePicker.SelectedIds.ToList();
            var overwritePhotos = cbOverwritePhotos.Checked;

            int? delay = GetAttributeValue( "ExecutionDelay" ).AsIntegerOrNull();

            RockContext rockContext = new RockContext();
            var personService = new PersonService( rockContext );
            var binaryFileService = new BinaryFileService( rockContext );
            var registrationRegistrantService = new RegistrationRegistrantService( rockContext );
            var attributeValueService = new AttributeValueService( rockContext );
            var attributeService = new AttributeService( rockContext );

            //Create a list of registrants from all selected registration templates
            var registrants = registrationRegistrantService.Queryable()
                .Where( rr => overwritePhotos
                                ? ( templateIds.Contains( ( int ) rr.Registration.RegistrationInstance.RegistrationTemplateId ) && ( rr.PersonAlias.Person.AgeClassification == AgeClassification.Adult || rr.PersonAlias.Person.Email.IndexOf("secc.org") == -1 ) )
                                : ( templateIds.Contains( ( int ) rr.Registration.RegistrationInstance.RegistrationTemplateId ) && !rr.PersonAlias.Person.PhotoId.HasValue ) )
                .Select( rr => new
                {
                    rrId = rr.Id,
                    pId = rr.PersonAlias.Person.Id
                } )
                .ToList();

            //Create a list of attributes belonging to the selected registration templates w/ key like 'photo'
            var attributeIds = attributeService.Queryable().AsEnumerable()
                .Where( a => {
                    int intETQV;
                    bool isValidIntETQV = int.TryParse( a.EntityTypeQualifierValue, out intETQV );
                    return isValidIntETQV && templateIds.Contains( intETQV ) && a.Key.ToLower().Contains( "photo" );
                    } )
                .Select( a => a.Id )
                .ToList();

            //Create a list of registrant Ids & binary file Guids
            var rrIdsList = registrants.Select( rr => rr.rrId ).ToList();
            var attributeValues = attributeValueService.Queryable()
                .Where( av => attributeIds.Contains( av.AttributeId ) && rrIdsList.Contains( ( int ) av.EntityId ) )
                .Select( av => new
                {
                    rrId = av.EntityId, 
                    bfGuid = av.Value
                } )
                .ToList();

            //Create the list of binary files
            var bfGuids = attributeValues.Select( av => av.bfGuid.AsGuid() ).ToList();
            var binaryFiles = binaryFileService.Queryable()
                .Where( bf => bfGuids.Contains( bf.Guid ) )
                .Select( bf => new
                {
                    bfId = bf.Id,
                    bfGuid = bf.Guid
                } )
                .ToList();

            //Join the lists to connect PersonId & BinaryFileId
            var rows = ( from rr in registrants
                                join av in attributeValues on rr.rrId equals ( int ) av.rrId
                                join bf in binaryFiles on av.bfGuid.AsGuid() equals bf.bfGuid
                                select new
                                {
                                    PersonId = rr.pId,
                                    BinaryFileId = bf.bfId
                                } ).ToList();

            //Iterate over the rows to call UpdatePhoto
            foreach ( var row in rows )
            {
                var person = personService.GetNoTracking( row.PersonId );
                var binaryFile = binaryFileService.GetNoTracking( row.BinaryFileId );
                if ( person != null && binaryFile != null)
                {
                    var task = Task.Run( async () => await face.UpdatePhoto( person, binaryFile ) );
                    if (!delay.IsNullOrZero())
                    {
                        Thread.Sleep( delay.Value );
                    }
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

// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
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
using System;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using Rock.Attribute;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using Rock.Security;
using System.Web.UI.WebControls;
using iTextSharp.text.pdf;
using System.Text.RegularExpressions;
using System.IO;
using System.Web;
using System.Collections.Generic;
using Rock.Web.Cache;
using ListItem = System.Web.UI.WebControls.ListItem;
using DotLiquid;
using Document = iTextSharp.text.Document;

namespace RockWeb.Plugins.org_secc.Finance
{
    [DisplayName( "Contribution Statement List" )]
    [Category( "SECC > Finance" )]
    [Description( "Shows a list of all contribution statements." )]

    [LinkedPage("Detail Page")]
    [BinaryFileTypeField]
    [CustomDropdownListField( "Document Type", "The document type for contribution statements.", "SELECT Guid as Value,Name as Text FROM DocumentType", Key = "DocumentType" )]
    [CustomEnhancedListField("Print & Mail Dataviews", "Any dataviews which indicate people/businesses for who statements will be mailed.", "SELECT Guid as Value,Name as Text FROM DataView", Key="PrintAndMail")]
    public partial class ContributionStatementList : RockBlock, ICustomGridColumns
    {
        private BinaryFileType binaryFileType = null;
        
        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            Guid binaryFileTypeGuid = Guid.NewGuid();
            if ( Guid.TryParse( GetAttributeValue( "BinaryFileType" ), out binaryFileTypeGuid ) )
            {
                var service = new BinaryFileTypeService( new RockContext() );
                binaryFileType = service.Get( binaryFileTypeGuid );
            }

            BindFilter();
            fBinaryFile.ApplyFilterClick += fBinaryFile_ApplyFilterClick;

            gBinaryFile.DataKeyNames = new string[] { "Id" };
            gBinaryFile.Actions.ShowAdd = true;
            gBinaryFile.Actions.AddClick += gBinaryFile_Add;
            gBinaryFile.GridRebind += gBinaryFile_GridRebind;
            gBinaryFile.RowItemText = binaryFileType != null ? binaryFileType.Name : "Binary File";

            // Block Security and special attributes (RockPage takes care of View)
            bool canAddEditDelete = IsUserAuthorized( Authorization.EDIT );
            gBinaryFile.Actions.ShowAdd = canAddEditDelete;
            gBinaryFile.IsDeleteEnabled = canAddEditDelete;
        }

        public void ExportPdfs_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            rockContext.Database.CommandTimeout = 180;
            
            var files = GetBinaryFiles().Select( d => d.BinaryFile );

            PdfImportedPage importedPage;

            var outputStream = new MemoryStream();

            Document sourceDocument = new Document();
            PdfCopy pdfCopyProvider = new PdfCopy( sourceDocument, outputStream );

            //output file Open  
            sourceDocument.Open();


            Regex regex = new Regex( @"/Type\s*/Page[^s]" );

            foreach ( var file in files )
            {
                if (file.MimeType == "application/pdf")
                {
                    using ( StreamReader sr = new StreamReader( file.ContentStream ) )
                    {
                        MatchCollection matches = regex.Matches( sr.ReadToEnd() );
                        int pages = matches.Count;

                        PdfReader reader = new PdfReader( file.ContentStream );
                        //Add pages in new file  
                        for ( int i = 1; i <= pages; i++ )
                        {
                            importedPage = pdfCopyProvider.GetImportedPage( reader, i );
                            pdfCopyProvider.AddPage( importedPage );
                        }
                        reader.Close();
                    }
                }
            }
            // Finish up the output
            sourceDocument.Close();

            this.Page.EnableViewState = false;
            this.Page.Response.Clear();
            Response.ContentType = "application/pdf";
            Response.AddHeader( "content-disposition", "attachment;filename=GivingStatementExport.pdf" );

            this.Page.Response.Charset = string.Empty;
            this.Page.Response.BinaryWrite( outputStream.ToArray() );
            this.Page.Response.Flush();
            this.Page.Response.End();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                BindGrid();
            }

            base.OnLoad( e );
        }

        #endregion

        #region Grid Events (main grid)

        /// <summary>
        /// Handles the ApplyFilterClick event of the fBinaryFile control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void fBinaryFile_ApplyFilterClick( object sender, EventArgs e )
        {
            fBinaryFile.SaveUserPreference( "File Name", tbName.Text );
            fBinaryFile.SaveUserPreference( "Person", ppPerson.SelectedValue.ToString() );
            fBinaryFile.SaveUserPreference( "Statement Delivery Preference", cbDeliveryPreference.SelectedValues.JoinStrings( "" ) );

            BindGrid();
        }

        /// <summary>
        /// Handles the Add event of the gBinaryFile control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void gBinaryFile_Add( object sender, EventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", "BinaryFileId", 0, "BinaryFileTypeId", binaryFileType != null ? binaryFileType.Id : 0 );
        }

        /// <summary>
        /// Handles the Edit event of the gBinaryFile control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gBinaryFile_Edit( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", "BinaryFileId", e.RowKeyId );
        }

        /// <summary>
        /// Handles the Delete event of the gBinaryFile control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gBinaryFile_Delete( object sender, RowEventArgs e )
        {
            var rockContext = new RockContext();
            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
            BinaryFile binaryFile = binaryFileService.Get( e.RowKeyId );

            if ( binaryFile != null )
            {
                string errorMessage;
                if ( !binaryFileService.CanDelete( binaryFile, out errorMessage ) )
                {
                    mdGridWarning.Show( errorMessage, ModalAlertType.Information );
                    return;
                }

                Guid guid = binaryFile.Guid;
                bool clearDeviceCache = binaryFile.BinaryFileType.Guid.Equals( Rock.SystemGuid.BinaryFiletype.CHECKIN_LABEL.AsGuid() );

                binaryFileService.Delete( binaryFile );
                rockContext.SaveChanges();

                if ( clearDeviceCache )
                {
                    Rock.CheckIn.KioskDevice.Clear();
                    Rock.CheckIn.KioskLabel.Remove( guid );
                }
            }

            BindGrid();
        }

        /// <summary>
        /// Handles the GridRebind event of the gBinaryFile control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void gBinaryFile_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Binds the filter.
        /// </summary>
        private void BindFilter()
        {
            if ( !Page.IsPostBack )
            {
                // Set the default filter value for file name
                tbName.Text = fBinaryFile.GetUserPreference( "File Name" );

                // Set the filter value for Person
                var personId = fBinaryFile.GetUserPreference( "Person" ).AsIntegerOrNull();
                if ( personId.HasValue )
                {
                    var person = new PersonService( new RockContext() ).Get( personId.Value );
                    ppPerson.SetValue( person );
                }
                else
                {
                    ppPerson.SetValue( null );
                }

                // Set up the filter for statement delivery preference
                var statementDeliveryPreference = new List<ListItem>();
                statementDeliveryPreference.Add( new ListItem( "Electronic" ) );
                statementDeliveryPreference.Add( new ListItem( "Print & Mail" ) );
                cbDeliveryPreference.DataSource = statementDeliveryPreference;
                cbDeliveryPreference.DataBind();

                cbDeliveryPreference.SetValues( fBinaryFile.GetUserPreference( "Statement Delivery Preference" ).SplitDelimitedValues( false ) );
            }            
        }

        private List<DocumentData> GetBinaryFiles()
        {
            Guid binaryFileTypeGuid = binaryFileType != null ? binaryFileType.Guid : Guid.NewGuid();
            RockContext context = new RockContext();
            var binaryFileService = new BinaryFileService( context );
            var attributeService = new AttributeService( context );
            var attributeValueService = new AttributeValueService( context );
            var personAliasService = new PersonAliasService( context );
            var personService = new PersonService( context );
            var documentService = new DocumentService( context );

            // If the document type is not set
            if ( string.IsNullOrWhiteSpace(GetAttributeValue( "DocumentType" ) ) )
            {
                return null;
            }

            int documentTypeId = DocumentTypeCache.Get( GetAttributeValue( "DocumentType" ).AsGuid() ).Id;

            var documentQuery = documentService.Queryable( "BinaryFile" ).Where( d => d.DocumentTypeId == documentTypeId );

            // Add any query filters here
            string name = fBinaryFile.GetUserPreference( "File Name" );
            if ( !string.IsNullOrWhiteSpace( name ) )
            {
                documentQuery = documentQuery.Where( d => d.BinaryFile.FileName.Contains( name ) );
            }

            var personQuery = personService.Queryable( true );

            // Filter for a specific Person
            if ( ppPerson.SelectedValue.HasValue && ppPerson.SelectedValue.Value > 0 ) {
                string givingId = personService.Queryable().Where(a => a.Id == ppPerson.SelectedValue ).Select(p => p.GivingId ).FirstOrDefault();
                personQuery = personQuery.Where( p => p.GivingId == givingId );
            }

            var documents = documentQuery
                    .Join( personQuery,
                        obj => obj.EntityId,
                        p => p.Id,
                        ( obj, p ) => new { Document = obj, Person = p } )
                    .GroupBy(obj => obj.Document.BinaryFile.Id);


            List<DocumentData> list = documents.Select(d => new DocumentData() { BinaryFile = d.FirstOrDefault().Document.BinaryFile, People = d.Select( p => p.Person ).ToList() }).ToList();


            List<Guid?> dataviews = GetAttributeValue( "PrintAndMail" ).SplitDelimitedValues().AsGuidOrNullList();
            if ( list.Count() > 0 )
            {
                if ( dataviews != null && dataviews.Count > 0 )
                {
                    var dataViewService = new DataViewService( context );
                    foreach ( var dataviewguid in dataviews )
                    {
                        List<string> errorMessages = new List<string>();
                        list.FirstOrDefault().MailPersonIds.AddRange( dataViewService.Get( dataviewguid.Value ).GetQuery( null, null, out errorMessages ).OfType<Rock.Model.Person>().Select( p => p.Id ).ToList() );
                    }
                }
            }

            // Apply the Statement Delivery Preference filter
            if ( cbDeliveryPreference.SelectedValue == "Electronic" )
            {
                list = list.Where( l => !l.MailPersonIds.Intersect( l.People.Select( p => p.Id ) ).Any() ).ToList();
            }
            else if ( cbDeliveryPreference.SelectedValue == "Print & Mail" )
            {
                list = list.Where( l => l.MailPersonIds.Intersect( l.People.Select( p => p.Id ) ).Any() ).ToList();
            }


            var sortProperty = gBinaryFile.SortProperty;
            // Sort by Person Name
            if ( sortProperty != null && sortProperty.Property == "PersonNames" )
            {
                if ( sortProperty.Direction == SortDirection.Ascending )
                {
                    list = list.OrderBy( l => l.People.FirstOrDefault().LastName ).ToList();
                }
                else
                {
                    list = list.OrderByDescending( l => l.People.FirstOrDefault().LastName ).ToList();
                }
            }
            // Sort by Person Name
            else if ( sortProperty != null && sortProperty.Property == "GivingId" )
            {
                if ( sortProperty.Direction == SortDirection.Ascending )
                {
                    list = list.OrderBy( l => l.People.FirstOrDefault().GivingId ).ToList();
                }
                else
                {
                    list = list.OrderByDescending( l => l.People.FirstOrDefault().GivingId ).ToList();
                }
            }
            // Sort by Delivery Preference
            else if ( sortProperty != null && sortProperty.Property == "StatementDelivery" )
            {
                if ( sortProperty.Direction == SortDirection.Ascending )
                {
                    list = list.OrderBy( l => l.StatementDelivery ).ToList();
                }
                else
                {
                    list = list.OrderByDescending( l => l.StatementDelivery ).ToList();
                }
            }
            // Sort by LastModified
            else if ( sortProperty != null && sortProperty.Property == "ModifiedDateTime" )
            {
                if ( sortProperty.Direction == SortDirection.Ascending )
                {
                    list = list.OrderBy( l => l.BinaryFile.ModifiedDateTime ).ToList();
                }
                else
                {
                    list = list.OrderByDescending( l => l.BinaryFile.ModifiedDateTime ).ToList();
                }
            }
            // Sort by Giving Id
            else
            {
                if ( sortProperty == null || sortProperty.Direction == SortDirection.Ascending )
                {
                    list = list.OrderBy( d => d.BinaryFile.FileName ).ToList();
                }
                else
                {
                    list = list.OrderByDescending( d => d.BinaryFile.FileName ).ToList();

                }
            }

            return list;
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            gBinaryFile.DataSource = GetBinaryFiles();
            gBinaryFile.DataBind();
        }

        #endregion

        public class DocumentData : ILiquidizable
        {
            private static List<int> mailPersonIds = new List<int>();

            public BinaryFile BinaryFile { get; set; }
            public List<Person> People { get; set; }


            public int Id
            {
                get
                {
                    return BinaryFile.Id;
                }
            }

            public string GivingId
            {
                get
                {
                    return People.First().GivingId;
                }
            }

            public List<int> MailPersonIds
            {
                get { return mailPersonIds; }
            }

            public string StatementDelivery
            {
                get
                {
                    return People.Select( p => p.Id ).Intersect( MailPersonIds ).Any() ? "Print & Mail" : "Electronic";
                }
            }

            public string PersonNames
            {
                get
                {
                    return People.OrderByDescending( pa => pa.Age ).Select( pa => pa.FullName ).JoinStrings( ", " );
                }
            }

            public object ToLiquid()
            {
                return this;
            }
        }
    }
}
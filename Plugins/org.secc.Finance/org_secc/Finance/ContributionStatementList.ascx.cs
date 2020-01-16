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
using iTextSharp.text;
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
            /*
            RockContext rockContext = new RockContext();
            rockContext.Database.CommandTimeout = 180;

            var fileIds = rockContext.Database.SqlQuery<int>( @"
                select Id from (
	                select distinct bf.Id, bf.FileName, av.Value from person p 
		                inner join attributevalue av on p.GivingId = av.Value and av.AttributeId = 86048
		                inner join BinaryFile bf on bf.Id = av.EntityId
		                where p.id in (
		                select EntityId from DataViewPersistedValue where DataViewId = 2699
		                union
		                select EntityId from DataViewPersistedValue where DataViewId = 2697
                 )
                ) tmp where
                 tmp.filename like '%2019 Annual%';" ).ToList();
               

            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
            var files = binaryFileService.Queryable().Where( bf => fileIds.Contains( bf.Id ) ).ToList().OrderBy( bf => bf.FileName.Right( bf.FileName.Length - bf.FileName.IndexOf( " - " ) ) );
            */

            var files = GetBinaryFiles();
            PdfImportedPage importedPage;

            var outputStream = new MemoryStream();

            Document sourceDocument = new Document();
            PdfCopy pdfCopyProvider = new PdfCopy( sourceDocument, outputStream );

            //output file Open  
            sourceDocument.Open();


            Regex regex = new Regex( @"/Type\s*/
            Page[^s]" );

            foreach ( var file in files )
            {
                if (file.BinaryFile.MimeType == "application/pdf")
                {
                    using ( StreamReader sr = new StreamReader( file.BinaryFile.ContentStream ) )
                    {
                        MatchCollection matches = regex.Matches( sr.ReadToEnd() );
                        int pages = matches.Count;

                        PdfReader reader = new PdfReader( file.BinaryFile.ContentStream );
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
                tbName.Text = fBinaryFile.GetUserPreference( "File Name" );
                
                var statementDeliveryPreference = new List<ListItem>();
                statementDeliveryPreference.Add( new ListItem( "Electronic" ) );
                statementDeliveryPreference.Add( new ListItem( "Print & Mail" ) );
                cbDeliveryPreference.DataSource = statementDeliveryPreference;
                cbDeliveryPreference.DataBind();

                cbDeliveryPreference.SetValues( fBinaryFile.GetUserPreference( "Statement Delivery Preference" ).SplitDelimitedValues( false ) );
            }            
        }

        private List<BinaryFileWrapper> GetBinaryFiles()
        {
            Guid binaryFileTypeGuid = binaryFileType != null ? binaryFileType.Guid : Guid.NewGuid();
            RockContext context = new RockContext();
            var binaryFileService = new BinaryFileService( context );
            var attributeService = new AttributeService( context );
            var attributeValueService = new AttributeValueService( context );
            var personAliasService = new PersonAliasService( context );

            string binaryFileId = binaryFileType != null ? binaryFileType.Id.ToString() : "0";
            var attributeIds = attributeService.Queryable()
                .Where( a => a.EntityTypeQualifierColumn == "BinaryFileTypeId" && a.EntityTypeQualifierValue == binaryFileId )
                .Select( a => a.Id ).ToList();

            var queryable = binaryFileService.Queryable().Where( f => f.BinaryFileType.Guid == binaryFileTypeGuid )
                .GroupJoin( attributeValueService.Queryable(),
                    obj => obj.Id,
                    av => av.EntityId.Value,
                    ( obj, avs ) => new BinaryFileWrapper() { BinaryFile = obj, AttributeValues = avs.Where( av => attributeIds.Contains( av.AttributeId ) ) } );

            var sortProperty = gBinaryFile.SortProperty;
            string name = fBinaryFile.GetUserPreference( "File Name" );
            if ( !string.IsNullOrWhiteSpace( name ) )
            {
                queryable = queryable.Where( f => f.BinaryFile.FileName.Contains( name ) );
            }

            if ( sortProperty != null && sortProperty.Property != "PersonNames" && sortProperty.Property != "PersonNames" )
            {
                queryable = queryable.Sort( sortProperty );
            }
            else
            {
                queryable = queryable.OrderBy( d => d.BinaryFile.FileName );
            }

            var list = queryable.ToList();

            // Set all the PersonIds;
            foreach ( var document in list )
            {
                document.PersonIds.AddRange(
                    document.AttributeValues.Where( a => a.Attribute.Key == "PersonIds" ).Where( v => !string.IsNullOrWhiteSpace( v.Value ) ).Select( v => v.Value ).FirstOrDefault().SplitDelimitedValues().AsIntegerList()
                );
            }

            if ( sortProperty != null && sortProperty.Property == "PersonNames" )
            {
                if ( sortProperty.Direction == SortDirection.Ascending )
                {
                    list = list.OrderBy( l => l.Persons.FirstOrDefault().LastName ).ToList();
                }
                else
                {
                    list = list.OrderByDescending( l => l.Persons.FirstOrDefault().LastName ).ToList();
                }
            }

            List<Guid?> dataviews = GetAttributeValue( "PrintAndMail" ).SplitDelimitedValues().AsGuidOrNullList();
            if ( dataviews != null && dataviews.Count > 0 )
            {
                var dataViewService = new DataViewService( context );
                foreach ( var dataviewguid in dataviews )
                {
                    List<string> errorMessages = new List<string>();
                    list.FirstOrDefault().MailPersonIds.AddRange( dataViewService.Get( dataviewguid.Value ).GetQuery( null, null, out errorMessages ).OfType<Rock.Model.Person>().Select( p => p.Id ).ToList() );
                }
            }

            if ( sortProperty != null && sortProperty.Property == "PersonNames" )
            {
                if ( sortProperty.Direction == SortDirection.Ascending )
                {
                    list = list.OrderBy( l => l.Persons.FirstOrDefault().LastName ).ToList();
                }
                else
                {
                    list = list.OrderByDescending( l => l.Persons.FirstOrDefault().LastName ).ToList();
                }
            }

            if ( fBinaryFile.GetUserPreference( "Statement Delivery Preference" ) == "Electronic" )
            {
                list = list.Where( l => !l.MailPersonIds.Contains( l.Persons.Select( p => p.Id ).FirstOrDefault() ) ).ToList();
            }
            else if ( fBinaryFile.GetUserPreference( "Statement Delivery Preference" ) == "Print & Mail" )
            {
                list = list.Where( l => l.MailPersonIds.Contains( l.Persons.Select( p => p.Id ).FirstOrDefault() ) ).ToList();
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

        public class PersonData
        {
            public int Id { get; set; }
            public int AliasPersonId { get; set; }
            public string NickName { get; set; }
            public string LastName { get; set; }
            public string FullName {
                get
                {
                    return NickName + " " + LastName;
                }
            }
            public DateTime? BirthDate { get; set; }


            public int? Age
            {
                get
                {
                    return Person.GetAge( this.BirthDate );
                }
            }

        }

        public class BinaryFileWrapper : ILiquidizable
        {
            private static List<int> personIds = new List<int>();
            private static List<int> mailPersonIds = new List<int>();
            private static List<PersonData> people;

            public int Id {
                get {
                    return BinaryFile.Id;
                }
            }

            public string GivingId {
                get
                {
                    return AttributeValues.Where( a => a.Attribute.Key == "GivingId" ).Select( v => v.Value ).FirstOrDefault();
                }
            }

            public BinaryFile BinaryFile { get; set; }

            public List<int> PersonIds
            {
                get { return personIds; }
            }

            public List<int> MailPersonIds
            {
                get { return mailPersonIds; }
            }

            public String StatementDelivery
            {
                get
                {
                    return People.Select( p => p.Id ).Intersect( MailPersonIds ).Any()?"Print & Mail":"Electronic";
                }
            }

            public IEnumerable<AttributeValue> AttributeValues { get; set; }

            private List<PersonData> AllPeople
            {
                get { return people; }
            }

            private List<PersonData> People
            {
                get
                {
                    if ( people == null || personIds.Distinct().Except( people.Select( p => p.AliasPersonId ) ).Any() )
                    {
                        RockContext context = new RockContext();
                        PersonAliasService personAliasService = new PersonAliasService( context );
                        people = new List<PersonData>();
                        for ( int i = 0; i < personIds.Count; i += 10000 )
                        {
                            var smallPersonIds = personIds.GetRange( i, Math.Min( 10000, personIds.Count - i ) );

                            var peopleQuery = personAliasService.Queryable( "Person" ).Where( pa => smallPersonIds.Contains( pa.AliasPersonId.Value ) )
                                .Select( pa => new PersonData() { Id = pa.Person.Id, AliasPersonId = pa.AliasPersonId.Value, NickName = pa.Person.NickName, LastName = pa.Person.LastName, BirthDate = pa.Person.BirthDate } );
                            people.AddRange(peopleQuery.ToList());
                        }
                    }
                    if ( people != null )
                    {
                        var strings = AttributeValues.Where( a => a.Attribute.Key == "PersonIds" ).Select( v => v.Value.Split( '|' ) ).FirstOrDefault();
                        if (strings != null)
                        {
                            return people.Where( pa => strings.AsIntegerList().Contains( pa.AliasPersonId ) ).DistinctBy(pa => pa.Id).ToList();
                        }
                    }
                    return new List<PersonData>();
                }
            }

            public List<PersonData> Persons
            {
                get
                {
                    return People.OrderByDescending( pa => pa.Age ).ToList();
                }
            }

            public string PersonNames
            {
                get
                {
                    var tmp = AttributeValues.Where( a => a.Attribute.Key == "PersonIds" );
                    return People.OrderByDescending(pa => pa.Age).Select( pa => pa.FullName ).JoinStrings( ", " );
                }
            }

            public object ToLiquid()
            {
                return this;
            }
        }
    }
}
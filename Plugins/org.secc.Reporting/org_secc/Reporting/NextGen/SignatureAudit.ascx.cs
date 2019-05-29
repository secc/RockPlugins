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
using System;
using System.ComponentModel;
using System.Web.UI;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using System.Collections.Generic;
using Rock;
using Rock.Web.Cache;
using Rock.Data;
using System.Linq;

namespace RockWeb.Blocks.Reporting.NextGen
{
    /// <summary>
    /// Block for auditing signature documents
    /// </summary>
    [DisplayName( "Signature Audit" )]
    [Category( "SECC > Reporting > NextGen" )]
    [Description( "Block for auditing signature documents" )]

    public partial class SignatureAudit : RockBlock
    {
        #region Control Methods

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            gData.GridRebind += GData_GridRebind;
        }

        private void GData_GridRebind( object sender, GridRebindEventArgs e )
        {
            BindGrid();
        }

        private void BindGrid()
        {
            RockContext rockContext = new RockContext();
            GroupMemberService groupMemberService = new GroupMemberService( rockContext );
            SignatureDocumentService signatureDocumentService = new SignatureDocumentService( rockContext );
            var documentQry = signatureDocumentService.Queryable();
            var groupId = gpGroup.SelectedValueAsId() ?? 0;
            var minorDocumentTemplateIds = cblMinorDocuments.SelectedValuesAsInt;
            var adultDocumentTemplateIds = cblAdultDocuments.SelectedValuesAsInt;
            var members = groupMemberService.Queryable().Where( gm => gm.GroupId == groupId )
                .Select( gm => new
                {
                    Person = gm.Person,
                    MinorDocuments = documentQry.Where( d =>
                        minorDocumentTemplateIds.Contains( d.SignatureDocumentTemplateId ) &&
                        gm.Person.Aliases.Select( pa => pa.Id ).Contains( d.AppliesToPersonAliasId ?? 0 ) ).ToList(),
                    AdultDocuments = documentQry.Where( d =>
                        minorDocumentTemplateIds.Contains( d.SignatureDocumentTemplateId ) &&
                        gm.Person.Aliases.Select( pa => pa.Id ).Contains( d.AppliesToPersonAliasId ?? 0 ) ).ToList()
                } )
                .ToList();

            var signatureMembers = new List<SignatureMemberData>();

            var endDate = dpEndDate.Text.AsDateTime();
            if ( endDate == null )
            {
                endDate = Rock.RockDateTime.Today;
            }

            foreach ( var member in members )
            {
                var person = member.Person;
                var signatureMember = new SignatureMemberData
                {
                    Id = person.Id,
                    Person = person,
                    Warning = false,
                    WarningText = ""
                };

                if ( !person.BirthDate.HasValue )
                {
                    signatureMember.WarningText += "[No Set Birthdate]";
                    signatureMember.Warning = true;
                }
                //For Adults Only
                else if ( person.Age > 18 )
                {
                    foreach ( var document in member.AdultDocuments )
                    {
                        if ( document.AssignedToPersonAliasId == document.AppliesToPersonAliasId )
                        {
                            signatureMember.HasValidDocument = true;
                        }
                    }
                }
                //Minors
                else if ( person.Age < 18 )
                {
                    foreach ( var document in member.MinorDocuments )
                    {
                        if ( document.AssignedToPersonAliasId != document.AppliesToPersonAliasId )
                        {
                            signatureMember.HasValidDocument = true;
                        }
                        else
                        {
                            signatureMember.WarningText += " Self signed document detected.";
                        }
                    }

                    //Test to see if they will turn 18 during trip
                    var futureDate = person.BirthDate.Value.AddYears( 18 );
                    if ( futureDate < endDate.Value )
                    {
                        signatureMember.Warning = true;
                        signatureMember.WarningText += " Minor will be turning 18 before end of event. [" + futureDate.ToString( "MM/dd/yyyy" ) + "]";
                    }
                }

                if ( !signatureMember.HasValidDocument )
                {
                    signatureMember.Warning = true;
                    signatureMember.WarningText += " No Valid Document.";
                }

                signatureMembers.Add( signatureMember );
            }

            SortProperty sortProperty = gData.SortProperty;

            if ( sortProperty != null )
            {
                gData.DataSource = signatureMembers.AsQueryable().Sort( sortProperty ).ToList();
            }
            else
            {
                gData.DataSource = signatureMembers.OrderByDescending( a => a.Warning ).ToList();
            }

            gData.DataBind();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {

            if ( !Page.IsPostBack )
            {
                BindSttingControlls();
                BindGrid();
            }
        }
        #endregion



        protected void btnSettings_Click( object sender, EventArgs e )
        {
            BindSttingControlls();
            mdSettings.Show();
        }

        private void BindSttingControlls()
        {
            dpEndDate.Text = GetBlockUserPreference( "dpEndDate" );

            RockContext rockContext = new RockContext();
            SignatureDocumentTemplateService signatureDocumentTemplateService = new SignatureDocumentTemplateService( rockContext );

            var templates = signatureDocumentTemplateService.Queryable().ToList();
            cblMinorDocuments.DataSource = templates;
            cblAdultDocuments.DataSource = templates;
            cblMinorDocuments.DataBind();
            cblAdultDocuments.DataBind();

            var minorValues = GetBlockUserPreference( "cblMinorDocuments" );
            if ( minorValues.IsNotNullOrWhiteSpace() )
            {
                cblMinorDocuments.SetValues( minorValues.Split( ',' ) );
            }

            var adultValues = GetBlockUserPreference( "cblAdultDocuments" );
            if ( adultValues.IsNotNullOrWhiteSpace() )
            {
                cblAdultDocuments.SetValues( adultValues.Split( ',' ) );
            }

            gpGroup.SetValue( GetBlockUserPreference( "gpGroup" ).AsInteger() );

        }

        protected void mdSettings_SaveClick( object sender, EventArgs e )
        {
            SetBlockUserPreference( "dpEndDate", dpEndDate.Text );
            SetBlockUserPreference( "cblMinorDocuments", string.Join( ",", cblMinorDocuments.SelectedValues ) );
            SetBlockUserPreference( "cblAdultDocuments", string.Join( ",", cblAdultDocuments.SelectedValues ) );
            SetBlockUserPreference( "gpGroup", gpGroup.SelectedValue );
            mdSettings.Hide();
            BindGrid();
        }

        protected void gData_RowDataBound( object sender, System.Web.UI.WebControls.GridViewRowEventArgs e )
        {
            var item = e.Row.DataItem;
            if ( item is SignatureMemberData )
            {
                var signatureData = item as SignatureMemberData;
                if ( signatureData.Warning )
                {
                    e.Row.BackColor = System.Drawing.Color.LightCoral;
                }
            }
        }

        protected void gData_RowSelected( object sender, RowEventArgs e )
        {
            Response.Redirect( "/Person/" + e.RowKeyId.ToString() + "/Releases" );
        }
    class SignatureMemberData
    {
        public int Id { get; set; }
        public Person Person { get; set; }
        public string AgeAtEnd { get; set; }
        public bool Warning { get; set; }
        public string WarningText { get; set; }
        public bool HasValidDocument { get; set; }
    }

}
}
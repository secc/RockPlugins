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
using System;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Attribute;
using System.Collections.Generic;
using Rock;
using Rock.Web.Cache;

namespace RockWeb.Plugins.org_secc.Reporting
{
    /// <summary>
    /// Fiancial aid profile for a family.
    /// </summary>
    [DisplayName( "Financial Aid - Family Profile" )]
    [Category( "SECC > Reporting" )]
    [Description( "A block to display the financial aid information for an entire family." )]
    [WorkflowTypeField( "Workflow Type", "The workflow type for financial aid." )]
    
    public partial class FinancialAidFamilyProfile : RockBlock
    {
        #region Fields

        // used for private variables

        #endregion

        #region Properties

        // used for public / protected properties

        #endregion

        #region Base Control Methods

        //  overrides of the base RockBlock methods (i.e. OnInit, OnLoad)

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
                      
            if ( !Page.IsPostBack )
            {
                BindGrid();
            }
        }

        protected void BindGrid()
        {
            RockContext rockContext = new RockContext();
            PersonAliasService personAliasService = new PersonAliasService( rockContext );
            WorkflowService workflowService = new WorkflowService( rockContext );
            AttributeService attributeService = new AttributeService( rockContext );
            AttributeValueService attributeValueService = new AttributeValueService( rockContext );
            RegistrationRegistrantService registrationRegistrantService = new RegistrationRegistrantService( rockContext );

            var attributes = attributeService.Queryable().Where( a => a.EntityTypeQualifierValue == "139" && a.EntityTypeQualifierColumn == "WorkflowTypeId" );
            var attributeIds = attributes.Select( a => a.Id );

            // Get all the family members
            var familyMembers = personAliasService.Get( PageParameter( "PersonAliasId" ).AsInteger() ).Person.GetFamilyMembers( true );

            var familyAliasIds = familyMembers.SelectMany( fm => fm.Person.Aliases.Select( pa => pa.Id ) ).ToList();

           var discountCodes = registrationRegistrantService.Queryable().Where( rr => rr.PersonAliasId.HasValue && familyAliasIds.Contains( rr.PersonAliasId.Value ) )
                                .Where( rr => rr.Registration.DiscountCode != null && rr.Registration.DiscountCode != "" )
                                .ToDictionary( x => x.Registration.DiscountCode, x => x.Registration.DiscountAmount );

            var qry = workflowService.Queryable().Where( w => w.WorkflowTypeId == 139 )
                .GroupJoin( attributeValueService.Queryable().Where( av => attributeIds.Contains( av.AttributeId ) ),
                    w => w.Id,
                    av => av.EntityId,
                    ( w, av ) => new { Workflow = w, AttributeValues = av } )
                .Where( obj => obj.AttributeValues.Any(av => av.Attribute.Key == "DiscountCode" && discountCodes.Keys.Contains( av.Value ) ) )
                .ToList()
                .Select(obj => new {
                    Id = obj.Workflow.Id,
                    FirstName = obj.AttributeValues.Where( av => av.Attribute.Key == "StudentFirstName" ).Select( av => av.Value ).FirstOrDefault(),
                    LastName = obj.AttributeValues.Where( av => av.Attribute.Key == "StudentLastName" ).Select( av => av.Value ).FirstOrDefault(),
                    Campus = obj.AttributeValues.Where( av => av.Attribute.Key == "Campus" ).Select( av => CampusCache.Get(av.Value.AsGuid()) ).FirstOrDefault(),
                    ApplicationYear = obj.AttributeValues.Where( av => av.Attribute.Key == "ApplicationYear" ).Select( av => av.Value ).DefaultIfEmpty( attributes.Where( a => a.Key == "ApplicationYear" ).Select( a => a.DefaultValue ).FirstOrDefault() ).FirstOrDefault(),
                    DiscountCode = obj.AttributeValues.Where( av => av.Attribute.Key == "DiscountCode" ).Select( av => av.Value ).FirstOrDefault(),
                    DiscountAmount = discountCodes[obj.AttributeValues.Where( av => av.Attribute.Key == "DiscountCode" ).Select( av => av.Value ).FirstOrDefault()],
                    Event = obj.AttributeValues.Where( av => av.Attribute.Key == "EventStudentisAttending" ).Select( av => av.Value ).FirstOrDefault(),
                    Status = obj.Workflow.Status
                } );

            gFamilyProfile.DataSource = qry.ToList();
            gFamilyProfile.DataBind();
        }

        #endregion

        #region Events

        // handlers called by the controls on your block

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {

        }

        #endregion

    }
}
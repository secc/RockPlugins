using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Web.UI;
using System.Web.UI.WebControls;
using Amazon.S3.Model.Internal.MarshallTransformations;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using OpenXmlPowerTools;
using org.secc.SystemsMonitor;
using org.secc.SystemsMonitor.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.SystemsMonitor
{
    [DisplayName( "Monitor Test Detail" )]
    [Category( "SECC > Systems Monitor" )]
    [Description( "Displays the details of a monitor for editing." )]

    public partial class MonitorTestDetail : RockBlock
    {
        private List<string> _attributeExclusion = new List<string>
        {
            "Order","Active"
        };

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

        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );
            var monitorTest = GetMonitorTest();
            if ( ViewState["EntityTypeId"] != null && ViewState["EntityTypeId"] is int? )
            {
                monitorTest.EntityTypeId = ViewState["EntityTypeId"] as int?;
            }
            monitorTest.LoadAttributes();
            Rock.Attribute.Helper.AddEditControls( monitorTest, phAttributes, false, "", exclude: _attributeExclusion );

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
                BindDropDown();
                SystemTest monitorTest = GetMonitorTest();
                if ( monitorTest != null && monitorTest.Id != 0 )
                {
                    ViewState["EntityTypeId"] = monitorTest.EntityTypeId;
                    BindAlarmDropDown( monitorTest.EntityTypeId );
                    ddlComponent.Visible = false;
                    ltName.Text = monitorTest.Name;
                    tbName.Text = monitorTest.Name;
                    tbInterval.Text = monitorTest.RunIntervalMinutes.ToString();
                    ddlAlarmCondition.SetValue( Convert.ToInt32( monitorTest.AlarmCondition ).ToString() );
                    monitorTest.LoadAttributes();
                    Rock.Attribute.Helper.AddEditControls( monitorTest, phAttributes, true, "", exclude: _attributeExclusion );
                }
                else
                {
                    ddlComponent.Required = true;
                }
            }
        }

        private SystemTest GetMonitorTest()
        {
            return GetMonitorTest( new SystemTestService( new RockContext() ) );
        }

        private SystemTest GetMonitorTest( SystemTestService monitorTestService )
        {
            var testId = PageParameter( "MonitorTestId" ).AsInteger();
            var monitorTest = monitorTestService.Get( testId );
            if ( monitorTest == null )
            {
                monitorTest = new SystemTest();
            }
            return monitorTest;
        }

        private void BindDropDown()
        {
            var monitorComponents = SystemTestContainer.Instance.Dictionary.Select( i => i.Value.Value as SystemTestComponent ).ToList();
            ddlComponent.DataSource = monitorComponents;
            ddlComponent.DataBind();

            ddlComponent.Items.Insert( 0, new ListItem( "", "" ) );
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

        #region Methods

        #endregion

        protected void ddlComponent_SelectedIndexChanged( object sender, EventArgs e )
        {
            SystemTest monitorTest = new SystemTest
            {
                EntityTypeId = ddlComponent.SelectedValueAsId()
            };
            monitorTest.LoadAttributes();

            ViewState["EntityTypeId"] = monitorTest.EntityTypeId;
            BindAlarmDropDown( monitorTest.EntityTypeId );


            Rock.Attribute.Helper.AddEditControls( monitorTest, phAttributes, true, "", exclude: _attributeExclusion );
        }

        private void BindAlarmDropDown( int? entityTypeId )
        {
            var component = ( SystemTestComponent ) SystemTestContainer.Instance.Dictionary
               .Where( c => c.Value.Value.EntityType.Id == entityTypeId )
               .Select( c => c.Value.Value )
               .FirstOrDefault();

            if ( component != null )
            {
                var alertConditions = new Dictionary<int, string>();
                foreach ( var condition in component.SupportedAlarmConditions )
                {
                    alertConditions.Add( Convert.ToInt32( condition ), condition.ToString().SplitCase() );
                }
                ddlAlarmCondition.DataSource = alertConditions;
                ddlAlarmCondition.DataBind();
            }
            else
            {
                ddlAlarmCondition.DataSource = new Dictionary<int, string>();
                ddlAlarmCondition.DataBind();
            }
        }

        protected void btnSave_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            SystemTestService monitorTestService = new SystemTestService( rockContext );
            var monitorTest = GetMonitorTest( monitorTestService );
            var entityTypeId = ddlComponent.SelectedValueAsId();

            if ( monitorTest.Id == 0 )
            {
                if ( entityTypeId.HasValue )
                {
                    monitorTest.EntityTypeId = entityTypeId;
                    monitorTestService.Add( monitorTest );
                }
                else
                {
                    return;
                }
            }

            monitorTest.Name = tbName.Text;
            monitorTest.RunIntervalMinutes = tbInterval.Text.AsIntegerOrNull();
            monitorTest.AlarmCondition = ddlAlarmCondition.SelectedValueAsEnum<AlarmCondition>();
            rockContext.SaveChanges();

            monitorTest.LoadAttributes();
            Rock.Attribute.Helper.GetEditValues( phAttributes, monitorTest );
            monitorTest.SaveAttributeValues();

            NavigateToParentPage();
        }

        protected void lbCancel_Click( object sender, EventArgs e )
        {
            NavigateToParentPage();
        }
    }
}
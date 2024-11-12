using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using Rock.Data;
using System.Data.Entity;


namespace RockWeb.Plugins.org_secc.Reporting
{

    [DisplayName("Decision Analytics")]
    [Category("SECC > Reporting")]
    [Description("Reports of people who made a next step decision.")]

    [WorkflowTypeField("Decision Guide Workflow",
        Description = "The workflow form that the decision guide uses to record a person's decision.",
        AllowMultiple = false,
        IsRequired = true,
        DefaultValue = "57F2615F-133D-4FC0-8024-181CA3E596E2",
        Key = AttributeKeys.DecisionWorkflowKey,
        Order = 0)]
    [TextField("Decision Type Key",
        Description = "The key for the Decision Type attribute.",
        IsRequired = true,
        DefaultValue = "WhatDecision",
        Key = AttributeKeys.DecisionTypeAttributeKey,
        Order = 1)]
    [TextField("Campus Key",
        Description = "Attribute Key of the campus where the decision was made.",
        IsRequired = true,
        DefaultValue ="Campus",
        Key = AttributeKeys.CampusAttributeKey,
        Order = 2)]
    [TextField("Event Key",
        Description = "The key of the Event attribute.",
        IsRequired = true,
        DefaultValue = "Event",
        Key = AttributeKeys.EventAttributeKey,
        Order = 3 )]


    public partial class DecisionAnalytics : RockBlock
    {

        public class AttributeKeys
        {
            public const string DecisionWorkflowKey = "DecisionGuideWorkflow";
            public const string DecisionTypeAttributeKey = "DecisionTypeAttributeKey";
            public const string CampusAttributeKey = "CampusAttributeKey";
            public const string EventAttributeKey = "EventAttributeKey";
        }

        private int workflowTypeId;
        private string decisionTypeAttributeKey;
        private string campusAttributeKey;
        public string eventAttributeKey;

        #region Base Control Methods
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            dvpConnectionStatus.DefinedTypeId = DefinedTypeCache.GetId( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS.AsGuid() );
            dvpBaptismType.DefinedTypeId = DefinedTypeCache.GetId( Guid.Parse( "75FEF4A0-90E2-46AC-9A83-76672F0FB402" ) );
            
        }
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            workflowTypeId = WorkflowTypeCache.Get( GetAttributeValue( AttributeKeys.DecisionWorkflowKey ).AsGuid() ).Id;
            decisionTypeAttributeKey = GetAttributeValue( AttributeKeys.DecisionTypeAttributeKey );
            campusAttributeKey = GetAttributeValue( AttributeKeys.CampusAttributeKey );
            eventAttributeKey = GetAttributeValue( AttributeKeys.EventAttributeKey );

            if(!IsPostBack)
            {
                LoadBoundFields();
            }
        }

        #endregion

        #region Events
        protected void btnApply_Click( object sender, EventArgs e )
        {
            
        }

        protected void gResults_RowSelected( object sender, RowEventArgs e )
        {

        }

        protected void gResults_RowDataBound( object sender, GridViewRowEventArgs e )
        {

        }
        #endregion

        #region Methods
        private void LoadBoundFields()
        {
            LoadGradeRanges();
            LoadDecisionTypes();
            LoadEventTypes();
        }


        private void LoadDecisionTypes()
        {
            ddlDecisionType.Items.Clear();
            var decisionTypes = new List<string>();
            using (var rockContext = new RockContext())
            {
                var workflowETID = EntityTypeCache.GetId( typeof( Workflow) );
                var workflowTypeIdStr = workflowTypeId.ToString();

                var values = new AttributeService( rockContext ).Queryable()
                    .Include( a => a.AttributeQualifiers )
                    .Where( a => a.EntityTypeId == workflowETID )
                    .Where( a => a.EntityTypeQualifierColumn == "WorkflowTypeId" )
                    .Where( a => a.EntityTypeQualifierValue == workflowTypeIdStr )
                    .Where( a => a.Key == decisionTypeAttributeKey )
                    .FirstOrDefault()
                    .AttributeQualifiers.Where( q => q.Key == "values" )
                    .Select( q => q.Value )
                    .FirstOrDefault();

                decisionTypes.AddRange( values.SplitDelimitedValues(false));                
            }

            foreach (var value in decisionTypes)
            {
                ddlDecisionType.Items.Add( new ListItem( value, value ) );
            }
            ddlDecisionType.Items.Insert( 0, new ListItem( "", "" ) );
        }

        private void LoadEventTypes()
        {
            ddlEventType.Items.Clear();
            var eventTypes = new List<string>();

            using (var rockContext = new RockContext())
            {
                var workflowETID = EntityTypeCache.GetId( typeof( Workflow ) );
                var workflowTypeIdStr = workflowTypeId.ToString();

                var values = new AttributeService( rockContext ).Queryable()
                    .Include( a => a.AttributeQualifiers )
                    .Where( a => a.EntityTypeId == workflowETID )
                    .Where( a => a.EntityTypeQualifierColumn == "WorkflowTypeId" )
                    .Where( a => a.EntityTypeQualifierValue == workflowTypeIdStr )
                    .Where( a => a.Key == eventAttributeKey )
                    .FirstOrDefault()
                    .AttributeQualifiers.Where( q => q.Key == "values" )
                    .Select( q => q.Value )
                    .FirstOrDefault();

                eventTypes.AddRange( values.SplitDelimitedValues( false ) );
            }

            foreach (var value in eventTypes)
            {
                ddlEventType.Items.Add( new ListItem( value, value ) );
            }
            ddlEventType.Items.Insert( 0, new ListItem( "", "" ) );
        }

        private void LoadGradeRanges()
        {
            ddlLowerGrade.Items.Clear();
            ddlUpperGrade.Items.Clear();

            var rockContext = new RockContext();
            var dvservice = new DefinedValueService( rockContext );
            var avService = new AttributeValueService( rockContext );

            var definedTypeId = DefinedTypeCache.GetId( Rock.SystemGuid.DefinedType.SCHOOL_GRADES.AsGuid() );
            var definedValueETID = EntityTypeCache.Get( typeof( DefinedValue ) ).Id;
            var definedTypeIdStr = definedTypeId.ToString();

            var avQry = avService.Queryable().AsNoTracking()
                .Where( av => av.Attribute.EntityTypeId == definedValueETID )
                .Where( av => av.Attribute.EntityTypeQualifierColumn == "DefinedTypeId" )
                .Where( av => av.Attribute.EntityTypeQualifierValue == definedTypeIdStr )
                .Where( av => av.Attribute.Key == "Abbreviation" );

            var grades = dvservice.Queryable().AsNoTracking()
                .Where( dv => dv.DefinedTypeId == definedTypeId )
                .Where( dv => dv.IsActive )
                .Join( avQry, dv => dv.Id, av => av.EntityId,
                    ( d, av ) => new { d.Id, d.Description, d.Order, Abbreviation = av.Value } )
                .OrderBy( d => d.Order )
                .ToList();


            foreach (var grade in grades)
            {
                ddlLowerGrade.Items.Add( new ListItem( grade.Abbreviation, grade.Id.ToString() ) );
                ddlUpperGrade.Items.Add( new ListItem( grade.Abbreviation, grade.Id.ToString() ) );

            }
            ddlLowerGrade.Items.Insert( 0, new ListItem( "", "" ) );
            ddlUpperGrade.Items.Insert( 0, new ListItem( "", "" ) );
                                                                                                                                           
        }

        private void UpdateDataset()
        {

        }


        #endregion



    }
}
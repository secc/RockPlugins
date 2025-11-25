using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Linq.Dynamic;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using org.secc.Reporting.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;


namespace RockWeb.Plugins.org_secc.Reporting
{

    [DisplayName( "Decision Analytics" )]
    [Category( "SECC > Reporting" )]
    [Description( "Reports of people who made a next step decision." )]

    [WorkflowTypeField( "Decision Guide Workflow",
        Description = "The workflow form that the decision guide uses to record a person's decision.",
        AllowMultiple = false,
        IsRequired = true,
        DefaultValue = "57F2615F-133D-4FC0-8024-181CA3E596E2",
        Key = AttributeKeys.DecisionWorkflowKey,
        Order = 0 )]
    [TextField( "Decision Type Key",
        Description = "The key for the Decision Type attribute.",
        IsRequired = true,
        DefaultValue = "WhatDecision",
        Key = AttributeKeys.DecisionTypeAttributeKey,
        Order = 1 )]
    [TextField( "Campus Key",
        Description = "Attribute Key of the campus where the decision was made.",
        IsRequired = true,
        DefaultValue = "Campus",
        Key = AttributeKeys.CampusAttributeKey,
        Order = 2 )]
    [TextField( "Event Key",
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

        private List<DecisionReportItem> decisions = null;
        private int workflowTypeId;
        private string decisionTypeAttributeKey;
        private string campusAttributeKey;
        public string eventAttributeKey;

        public List<DecisionReportItem> Decisions
        {
            get
            {
                if (decisions != null)
                {
                    return decisions;
                }
                else
                {
                    var json = ViewState[$"{this.BlockId}_Decisions"] as string;
                    if (json.IsNotNullOrWhiteSpace())
                    {
                        decisions = JsonConvert.DeserializeObject<List<DecisionReportItem>>( json );
                    }
                }

                return decisions;
            }
            set
            {
                decisions = value;
                ViewState[$"{this.BlockId}_Decisions"] = JsonConvert.SerializeObject( decisions );

            }

        }

        #region Base Control Methods
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            dvpConnectionStatus.DefinedTypeId = DefinedTypeCache.GetId( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS.AsGuid() );
            dvpBaptismType.DefinedTypeId = DefinedTypeCache.GetId( Guid.Parse( "75FEF4A0-90E2-46AC-9A83-76672F0FB402" ) );
            gResults.Actions.ShowAdd = false;
            gResults.Actions.ShowBulkUpdate = true;
            gResults.Actions.ShowCommunicate = true;
            gResults.Actions.ShowMergePerson = false;
            gResults.ExportFilename = "Decisons";
            gResults.PersonIdField = "PersonId";
            gResults.EntityIdField = "PersonId";
            gResults.GridRebind += gResults_GridRebind;

            lbClearFilters.CausesValidation = false;
            lbClearFilters.Click += lbClearFilters_Click;


        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            workflowTypeId = WorkflowTypeCache.Get( GetAttributeValue( AttributeKeys.DecisionWorkflowKey ).AsGuid() ).Id;
            decisionTypeAttributeKey = GetAttributeValue( AttributeKeys.DecisionTypeAttributeKey );
            campusAttributeKey = GetAttributeValue( AttributeKeys.CampusAttributeKey );
            eventAttributeKey = GetAttributeValue( AttributeKeys.EventAttributeKey );

            if (!IsPostBack)
            {
                LoadBoundFields();
            }

            UpdateBaptismTypeVisibility();
        }

        #endregion

        #region Events
        protected void btnApply_Click( object sender, EventArgs e )
        {
            UpdateDataset();
        }

        protected void gResults_RowSelected( object sender, RowEventArgs e )
        {
            if (e.Row.RowType != DataControlRowType.DataRow)
            {
                return;
            }

            var id = e.RowKeyValues["Id"] as int?;

            var recordType = e.RowKeyValues["RecordType"] as string;

            if (id.HasValue)
            {
                LoadDetailModal( recordType, id.Value );
            }
        }

        protected void gResults_RowDataBound( object sender, GridViewRowEventArgs e )
        {

        }

        private void gResults_GridRebind( object sender, GridRebindEventArgs e )
        {
            UpdateDataset();
        }

        private void lbClearFilters_Click( object sender, EventArgs e )
        {
            ClearFilters();
        }

        protected void ddlDecisionType_SelectedIndexChanged( object sender, EventArgs e )
        {
            UpdateBaptismTypeVisibility();
        }
        #endregion

        #region Methods

        private void ClearFilters()
        {
            drpDecisionDate.LowerValue = null;
            drpDecisionDate.UpperValue = null;
            ppDecisions.SetValue( null );
            cblGender.SelectedValue = null;
            nreAgeRange.LowerValue = null;
            nreAgeRange.UpperValue = null;
            ddlLowerGrade.SelectedIndex = 0;
            ddlUpperGrade.SelectedIndex = 0;

            dvpConnectionStatus.SetValues( new List<int>() );
            pkFamilyCampus.SetValue( (int?) null );
            pkDecisionCampus.SetValue( (int?) null );
            ddlDecisionType.SelectedIndex = 0;
            ddlEventType.SelectedIndex = 0;
            dvpBaptismType.SelectedValue = null;
            cblServing.SelectedValue = null;
            cblInterestArea.SelectedValue = null;

            UpdateBaptismTypeVisibility();
        }

        private void LoadBoundFields()
        {
            LoadGradeRanges();
            LoadDecisionTypes();
            LoadEventTypes();
        }

        private void UpdateBaptismTypeVisibility()
        {
            var selectedDecisionType = ddlDecisionType.SelectedValue;
            var showBaptismType = selectedDecisionType.IsNotNullOrWhiteSpace()
                && selectedDecisionType.Equals( "Baptism", StringComparison.InvariantCultureIgnoreCase );

            dvpBaptismType.Visible = showBaptismType;

            if (!showBaptismType)
            {
                dvpBaptismType.SelectedValue = null;
            }
        }

        private void LoadDecisionTypes()
        {
            ddlDecisionType.Items.Clear();
            var decisionTypes = new List<string>();
            using (var rockContext = new RockContext())
            {
                var workflowETID = EntityTypeCache.GetId( typeof( Workflow ) );
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

                decisionTypes.AddRange( values.SplitDelimitedValues( false ) );
            }

            foreach (var value in decisionTypes)
            {
                ddlDecisionType.Items.Add( new ListItem( value, value ) );
            }
            ddlDecisionType.Items.Insert( 0, new ListItem( "", "" ) );
        }

        private void LoadDetailModal( string recordType, int id )
        {
            var decision = Decisions.Where( d => d.Id == id && d.RecordType == recordType )
                .FirstOrDefault();

            if (decision == null)
            {
                return;
            }

            pnlParentInfo.Visible = false;
            pnlPersonInfo.Visible = false;

            mdPersonInfo.Title = decision.FullName;
            lAddress.Text = decision.FullAddressHtml;


            if (decision.Age < 18)
            {
                lParentName.Text = decision.ParentGuardianName;
                lParentPhone.Text = decision.ParentPhone;
                lParentEmail.Text = decision.Email;
                pnlParentInfo.Visible = true;
            }
            else
            {
                lMobilePhone.Text = decision.MobilePhone;
                lEmail.Text = decision.Email;
                pnlPersonInfo.Visible = true;
            }

            if (decision.BaptismDate.HasValue)
            {
                lBaptism.Text = $"{decision.BaptismDate.ToShortDateString()} {decision.BaptismTypeValue}";
            }
            else
            {
                lBaptism.Text = "<span class=\"label label-danger\">No Baptism Info</span>";
            }

            if (decision.StatementOfFaithSignedDate.HasValue)
            {
                lStatementOfFaith.Text = decision.StatementOfFaithSignedDate.Value.ToShortDateString();
            }
            else
            {
                lStatementOfFaith.Text = "<span class=\"label label-danger\">Not Signed</span>";
            }

            if (decision.MembershipDate.HasValue)
            {
                lMembershipDate.Text = decision.MembershipDate.Value.ToShortDateString();
            }
            else
            {
                lMembershipDate.Text = "<span class=\"label label-danger\">No Member Info</span>";
            }

            if (decision.MembershipClassDate.HasValue)
            {
                lMembershipClass.Text = decision.MembershipClassDate.Value.ToShortDateString();
            }
            else
            {
                lMembershipClass.Text = "<span class=\"label label-danger\">Has Not Attended</span>";
            }

            if ( !string.IsNullOrEmpty(decision.Serving) )
            {
                lServing.Text = decision.Serving;
            }
            else
            {
                lServing.Text = "<span class=\"label label-danger\">No Serving Info</span>";
            }

            if ( !string.IsNullOrEmpty(decision.InterestArea) )
            {
                lInterestArea.Text = decision.InterestArea;
            }
            else
            {
                lInterestArea.Text = "<span class=\"label label-danger\">No Serving Area of Interest Info</span>";
            }

            mdPersonInfo.Show();

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

            var decisionQry = new DecisionReportItemService( new RockContext() ).Queryable().AsNoTracking();

            var startDate = drpDecisionDate.LowerValue.Value.Date;
            var endDate = drpDecisionDate.UpperValue.Value.Date.Add(new TimeSpan(23, 59, 59));
            if (drpDecisionDate.LowerValue.HasValue)
            {
                decisionQry = decisionQry.Where( q => q.FormDate >= startDate );
            }

            if (drpDecisionDate.UpperValue.HasValue)
            {
                decisionQry = decisionQry.Where( q => q.FormDate <= endDate );
            }

            if (ppDecisions.SelectedValue.HasValue)
            {
                decisionQry = decisionQry.Where( q => q.PersonId == ppDecisions.PersonId.Value );
            }

            if (cblGender.SelectedValues.Any())
            {
                var selectedGenders = cblGender.SelectedValues;
                decisionQry = decisionQry.Where( q => selectedGenders.Contains( q.Gender ) );
            }

            if (nreAgeRange.LowerValue.HasValue)
            {
                decisionQry = decisionQry.Where( q => q.Age >= nreAgeRange.LowerValue.Value );
            }


            if (nreAgeRange.UpperValue.HasValue)
            {
                decisionQry = decisionQry.Where( q => q.Age <= nreAgeRange.UpperValue.Value );
            }


            // grade min and maxes are flipped due to how graduation years work the lower the grade
            // the higher the graduation year.
            if (ddlLowerGrade.SelectedValueAsInt().HasValue)
            {
                var maxGraduationYear = RockDateTime.CurrentGraduationYear +
                    DefinedValueCache.Get( ddlLowerGrade.SelectedValueAsInt().Value ).Value.AsInteger();

                decisionQry = decisionQry.Where( q => q.GraduationYear <= maxGraduationYear );
            }

            if (ddlUpperGrade.SelectedValueAsInt().HasValue)
            {
                var minGraduationYear = RockDateTime.CurrentGraduationYear +
                    DefinedValueCache.Get( ddlUpperGrade.SelectedValueAsInt().Value ).Value.AsInteger();

                decisionQry = decisionQry.Where( q => q.GraduationYear >= minGraduationYear );
            }

            if (dvpConnectionStatus.SelectedDefinedValuesId.Any())
            {
                var selectedConnectionStatuses = dvpConnectionStatus.SelectedDefinedValuesId;

                decisionQry = decisionQry.Where( q => selectedConnectionStatuses.Contains( q.ConnectionStatusValueId ?? -1 ) );
            }

            if (pkFamilyCampus.SelectedCampusId.HasValue)
            {
                decisionQry = decisionQry.Where( q => q.FamilyCampusId == pkFamilyCampus.SelectedCampusId.Value );
            }

            if (pkDecisionCampus.SelectedCampusId.HasValue)
            {
                decisionQry = decisionQry.Where( q => q.DecisionCampusId == pkDecisionCampus.SelectedCampusId.Value );
            }

            if (ddlDecisionType.SelectedValue.IsNotNullOrWhiteSpace())
            {
                decisionQry = decisionQry.Where( q => q.DecisionType.Equals( ddlDecisionType.SelectedValue, StringComparison.InvariantCultureIgnoreCase ) );

            }

            if (ddlEventType.SelectedValue.IsNotNullOrWhiteSpace())
            {
                decisionQry = decisionQry.Where( q => q.EventName.Equals( ddlEventType.SelectedValue, StringComparison.InvariantCultureIgnoreCase ) );
            }

            if (dvpBaptismType.SelectedValuesAsInt.Any())
            {
                var baptismTypeIds = dvpBaptismType.SelectedValuesAsInt;
                decisionQry = decisionQry.Where( q => baptismTypeIds.Contains( q.BaptismTypeValueId ?? -1 ) );
            }

            if ( cblServing.SelectedValues.Any() )
            {
                var selectedServing = cblServing.SelectedValues;
                decisionQry = decisionQry.Where( q => selectedServing.Contains( q.Serving ) );
            }

            if ( cblInterestArea.SelectedValues.Any() )
            {
                var selectedInterestAreas = cblInterestArea.SelectedValues;
                decisionQry = decisionQry.Where( q => !string.IsNullOrEmpty( q.InterestArea ) && selectedInterestAreas.Any( val => q.InterestArea.Contains( val ) ) );
            }

            Decisions = decisionQry.ToList()
                .GroupBy( d => new { d.PersonId, d.DecisionType, FormDate = d.FormDate.ToShortDateString() } )
                .Select( d => d.OrderBy( d1 => d1.Id ).FirstOrDefault() )
                .ToList();

            gResults.DataSource = Decisions;
            gResults.DataBind();

            pnlGridResults.Visible = Decisions.Any();
            pnlUpdateMessage.Visible = !Decisions.Any();

        }


        #endregion



    }
}
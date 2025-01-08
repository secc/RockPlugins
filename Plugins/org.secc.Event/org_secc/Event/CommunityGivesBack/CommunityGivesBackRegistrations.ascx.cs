using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using System.Data.Entity;



namespace RockWeb.Plugins.org_secc.CommunityGivesBack
{
    [DisplayName("Community Gives Back Registrations List")]
    [Category("SECC > Community Gives Back")]
    [Description("Registration List for Community Gives Back by School")]


    [DefinedTypeField( "Community Gives Back Schools",
            Description = "Defined Type that contains the list of Community Gives Back Schools.",
            IsRequired = true,
            Key = AttributeKeys.SchoolDefinedType,
            Order = 0 )]

    [WorkflowTypeField( "Registration Workflow Type",
            Description = "Community Gives Back Workflow Type",
            IsRequired = true,
            AllowMultiple = false,
            Order = 1,
            Key = AttributeKeys.RegistrationWorkflow )]
    [LinkedPage( "School List Page",
        Description = "Page that contains the list of schools for the Community Gives Back program",
        IsRequired = true,
        Key = AttributeKeys.SchoolListPage,
        Order = 2 )]
    public partial class CommunityGivesBackRegistrations : RockBlock
    {
        public class AttributeKeys
        {
            public const string RegistrationWorkflow = "RegistrationWorkflow";
            public const string SchoolListPage = "SchoolListPage";
            public const string SchoolDefinedType = "SchoolDefinedType";

        }

        #region Base Control Methods
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            gSponsorships.GridRebind += gSponsorships_GridRebind;
            gSponsorships.AllowPaging = true;
            gSponsorships.AllowSorting = true;
            gSponsorships.Actions.ShowAdd = false;
            gSponsorships.Actions.ShowBulkUpdate = false;
            gSponsorships.Actions.ShowMergeTemplate = false;
            gSponsorships.Actions.ShowMergePerson = false;
            gSponsorships.Actions.ShowExcelExport = true;
        }



        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            nbError.Visible = false;
            if(!IsPostBack)
            {
                LoadRegistrations();
            }
        }
        #endregion

        #region Events

        private void gSponsorships_GridRebind( object sender, GridRebindEventArgs e )
        {
            LoadRegistrations();
        }

        #endregion

        #region Methods

        private void BindRegistrationList(List<RegistrationSummary> registrations)
        {
            gSponsorships.DataSource = registrations;
            gSponsorships.DataBind();
            pnlRegistrations.Visible = true;
        }

        private int GetRegistrationCount(Guid schoolguid)
        {
            var workflowType = WorkflowTypeCache.Get( GetAttributeValue( AttributeKeys.RegistrationWorkflow ).AsGuid() );
            var workflowEntityType = EntityTypeCache.Get( typeof( Workflow ) );
            using (var rockContext = new RockContext())
            {
                var workflowService = new WorkflowService( rockContext );
                var workflowTypeStr = workflowType.Id.ToString();

                var attributeValueQry = new AttributeValueService( rockContext ).Queryable().AsNoTracking()
                    .Where( v => v.Attribute.EntityTypeId == workflowEntityType.Id )
                    .Where( v => v.Attribute.EntityTypeQualifierValue == workflowTypeStr );

                var students = workflowService.Queryable().AsNoTracking()
                    .Where( w => w.WorkflowTypeId == workflowType.Id )
                    .Join( attributeValueQry, w => w.Id, v => v.EntityId,
                        ( w, v ) => new { w.Id, v.Attribute.Key, v.Value } )
                    .GroupBy( w => w.Id )
                    .Select( w => new
                    {
                        Id = w.Key,
                        SchoolGuid = w.Where( w1 => w1.Key == "School" ).Select( w1 => w1.Value ).FirstOrDefault(),
                        StudentsSponsored = w.Where( w1 => w1.Key == "StudentstoSponsor" ).Select( w1 => w1.Value ).FirstOrDefault()
                    } )
                    .ToList()
                    .Where( w => w.SchoolGuid.AsGuid() == schoolguid )
                    .Select( w => w.StudentsSponsored.AsInteger() )
                    .Sum();

                return students;
            }

        }

        private List<RegistrationSummary> GetRegistrationSummary(Guid schoolguid)
        {
            var workflowType = WorkflowTypeCache.Get( GetAttributeValue( AttributeKeys.RegistrationWorkflow ).AsGuid() );
            var workflowEntityType = EntityTypeCache.Get( typeof( Workflow ) );

            using (var rockContext = new RockContext())
            {
                var workflowService = new WorkflowService( rockContext );
                var workflowTypeStr = workflowType.Id.ToString();

                var attributeValueQry = new AttributeValueService( rockContext ).Queryable().AsNoTracking()
                    .Where( v => v.Attribute.EntityTypeId == workflowEntityType.Id )
                    .Where( v => v.Attribute.EntityTypeQualifierValue == workflowTypeStr );

                var students = workflowService.Queryable().AsNoTracking()
                    .Where( s => s.WorkflowTypeId == workflowType.Id )
                    .Join( attributeValueQry, w => w.Id, v => v.EntityId,
                        ( w, v ) => new { w.Id, v.Attribute.Key, v.Value } )
                    .GroupBy( s => s.Id )
                    .Select( s => new
                    {
                        Id = s.Key,
                        FirstName = s.Where( s1 => s1.Key == "FirstName" ).Select( s1 => s1.Value ).FirstOrDefault(),
                        LastName = s.Where( s1 => s1.Key == "LastName" ).Select( s1 => s1.Value ).FirstOrDefault(),
                        Email = s.Where( s1 => s1.Key == "Email" ).Select( s1 => s1.Value ).FirstOrDefault(),
                        MobilePhone = s.Where( s1 => s1.Key == "MobilePhone" ).Select( s1 => s1.Value ).FirstOrDefault(),
                        SchoolGuid = s.Where( s1 => s1.Key == "School" ).Select( s1 => s1.Value ).FirstOrDefault(),
                        Sponsored = s.Where( s1 => s1.Key == "StudentstoSponsor" ).Select( s1 => s1.Value ).FirstOrDefault(),
                        SponsorSiblingGroup = s.Where( s1 => s1.Key == "SponsorSiblingGroup" ).Select( s1 => s1.Value ).FirstOrDefault()
                    } )
                    .ToList()
                    .Where( s => s.SchoolGuid.AsGuid() == schoolguid )
                    .Select( s => new RegistrationSummary
                    {
                        Id = s.Id,
                        FirstName = s.FirstName,
                        LastName = s.LastName,
                        Email = s.Email,
                        MobilePhone = s.MobilePhone,
                        SchoolGuid = s.SchoolGuid.AsGuid(),
                        Sponsorships = s.Sponsored.AsInteger(),
                        SponsorSiblingGroup = s.SponsorSiblingGroup.AsBoolean()
                    } )
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .ToList();
                return students;
            }
        }

        private SchoolSummary GetSchoolSummary(Guid schoolGuid)
        {
            using (var rockContext = new RockContext())
            {
                var definedValueService = new DefinedValueService( rockContext );
                var definedType = DefinedTypeCache.Get( GetAttributeValue( AttributeKeys.SchoolDefinedType ).AsGuid() );

                var school = definedValueService.Queryable().AsNoTracking()
                    .Where( s => s.DefinedTypeId == definedType.Id )
                    .Where( s => s.Guid == schoolGuid )
                    .FirstOrDefault();

                if(school == null)
                {
                    return null;
                }
                school.LoadAttributes( rockContext );
                var summary = new SchoolSummary()
                {
                    Id = school.Id,
                    Name = school.Value,
                    TeacherName = school.GetAttributeValue( "ResourceTeacherName" ),
                    TeacherEmail = school.GetAttributeValue( "ResourceTeacherEmail" ),
                    TotalSponsorships = school.GetAttributeValue( "SponsorshipsAvailable" ).AsInteger(),
                    IsActive = school.IsActive,
                    Sponsored = GetRegistrationCount(school.Guid)
                };


                return summary;
            }
        }

        private void LoadRegistrations()
        {
            var schoolGuid = PageParameter( "School" ).AsGuid();
            if(schoolGuid == Guid.Empty)
            {
                nbError.Visible = true;
                return;
            }
            var school = GetSchoolSummary( schoolGuid );

            if(school == null)
            {
                nbError.Visible = true;
                return;
            }

            LoadSchoolSummary(school);
            var registrations = GetRegistrationSummary( schoolGuid );
            BindRegistrationList( registrations );

        }

        private void LoadSchoolSummary(SchoolSummary summary)
        {
            lSchoolName.Text = summary.Name;
            lTeacherName.Text = summary.TeacherName;
            lTeacherEmail.Text = summary.TeacherEmail;
            lTotalSponsorships.Text = summary.TotalSponsorships.ToString();
            lSponsoredCount.Text = summary.Sponsored.ToString();

            if(summary.IsActive)
            {
                hlStatus.Text = "Active";
                hlStatus.LabelType = LabelType.Success;
            }
            else
            {
                hlStatus.Text = "Inactive";
                hlStatus.LabelType = LabelType.Danger;
            }
            pnlSchoolDetail.Visible = true;

        }



        #endregion

        #region Helper Class
        protected class SchoolSummary
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string TeacherName { get; set; }
            public string TeacherEmail { get; set; }
            public int TotalSponsorships { get; set; }
            public int Sponsored { get; set; }
            public bool IsActive { get; set; }
        }

        protected class RegistrationSummary
        {
            public int Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public Guid SchoolGuid { get; set; }
            public string Email { get; set; }
            public string MobilePhone { get; set; }
            public int Sponsorships { get; set; }
            public bool SponsorSiblingGroup { get; set; }
            public string SponsorName
            {
                get
                {
                    return $"{FirstName} {LastName}";
                }
            }
            public string SponsorNameReversed
            {
                get
                {
                    return $"{LastName}, {FirstName}";
                }
            }
            
        }
        #endregion
    }
}
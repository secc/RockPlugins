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
    [DisplayName( "Community Gives Back School List" )]
    [Category( "SECC > Community Gives Back" )]
    [Description( "School List for Community Gives Back" )]

    [DefinedTypeField( "Community Gives Back Schools",
            Description = "Defined Type that contains the list of Community Gives Back Schools.",
            IsRequired = true,
            Key = AttributeKeys.SchoolDefinedType,
            Order = 0 )]
    [WorkflowTypeField( "Registration Workflow Type",
            Description = "Community Gives Back Workflow Type",
            IsRequired = true,
            AllowMultiple = false,
            Order = 4,
            Key = AttributeKeys.RegistrationWorkflow )]
    public partial class SchoolList : RockBlock
    {
        public class AttributeKeys
        {
            public const string SchoolDefinedType = "SchoolDefinedType";
            public const string RegistrationWorkflow = "RegistrationWorkflow";
        }

        private List<SchoolDataItem> _schools = null;

        protected List<SchoolDataItem> Schools
        {
            get
            {
                if(_schools == null)
                {
                    _schools = ViewState[BlockId + "_SchoolList"] as List<SchoolDataItem>;
                }
                return _schools;
            }
            set
            {
                _schools = value;
                ViewState[BlockId + "_SchoolList"] = _schools;
            }
        }

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            gSchoolList.GridRebind += gSchoolList_GridRebind;
            this.BlockUpdated += SchoolList_BlockUpdated;
        }


        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            BuildSchoolList();
            if(!IsPostBack)
            {
                LoadSchoolGrid();
            }


        }

        private void gSchoolList_GridRebind( object sender, GridRebindEventArgs e )
        {
            LoadSchoolGrid();
        }


        private void SchoolList_BlockUpdated( object sender, EventArgs e )
        {
            Schools = null;
            BuildSchoolList();
            LoadSchoolGrid();
        }

        private void BuildSchoolList()
        {
            if(Schools != null)
            {
                return;
            }


            var rockContext = new RockContext();

            var definedType = DefinedTypeCache.Get( GetAttributeValue( AttributeKeys.SchoolDefinedType ).AsGuid(), rockContext );
            var definedValueEntityTypeId = EntityTypeCache.Get( typeof( DefinedValue ) ).Id;
            var workflowEntityTypeId = EntityTypeCache.Get( typeof( Workflow ) ).Id;
            var workflowType = WorkflowTypeCache.Get( GetAttributeValue( AttributeKeys.RegistrationWorkflow ).AsGuid(), rockContext );

            if(definedType == null || workflowType == null)
            {
                return;
            }


            var attributeValueService = new AttributeValueService( rockContext );
            var definedValueService = new DefinedValueService( rockContext );
            var workflowService = new WorkflowService( rockContext );

            var workflowAVQry = attributeValueService.Queryable().AsNoTracking()
                .Where( v => v.Attribute.EntityTypeId == workflowEntityTypeId );

            var signups = workflowService.Queryable().AsNoTracking()
                .Where( w => w.WorkflowTypeId == workflowType.Id )
                .Join( workflowAVQry, w => w.Id, v => v.EntityId,
                    ( w, v ) => new { WorkflowId = w.Id, AttributeKey = v.Attribute.Key, Value = v.Value } )
                .GroupBy( w => w.WorkflowId )
                .Select( w => new
                {
                    WorkflowId = w.Key,
                    SchoolGuid = w.Where( w1 => w1.AttributeKey == "School" ).Select( w1 => w1.Value ).FirstOrDefault(),
                    Sponsorships = w.Where( w1 => w1.AttributeKey == "StudentsToSponsor" ).Select( w1 => w1.Value ).FirstOrDefault()
                } )
                .ToList()
                .GroupBy( s => s.SchoolGuid )
                .Select( s => new { SchoolGuid = s.Key, Sponsored = s.Sum( s1 => s1.Sponsorships.AsInteger() ) } )
                .ToList();

            var definedValueAVQuery = attributeValueService.Queryable().AsNoTracking()
                .Where( v => v.Attribute.EntityTypeId == definedValueEntityTypeId );

            Schools = definedValueService.Queryable().AsNoTracking()
                .Where( v => v.DefinedTypeId == definedType.Id )
                .Join( definedValueAVQuery, v => v.Id, av => av.EntityId,
                    ( v, av ) => new
                    {
                        Id = v.Id,
                        Name = v.Value,
                        Guid = v.Guid,
                        IsActive = v.IsActive,
                        Key = av.Attribute.Key,
                        Value = av.Value,

                    } )
                .GroupBy( v => v.Id )
                .Select( v => new
                {
                    Id = v.Key,
                    Name = v.Select( v1 => v1.Name ).FirstOrDefault(),
                    Guid = v.Select( v1 => v1.Guid ).FirstOrDefault(),
                    IsActive = v.Select( v1 => v1.IsActive ).FirstOrDefault(),
                    TeacherName = v.Where( v1 => v1.Key == "ResourceTeacherName" ).Select( v1 => v1.Value ).FirstOrDefault(),
                    TeacherEmail = v.Where( v1 => v1.Key == "ResourceTeacherEmail" ).Select( v1 => v1.Value ).FirstOrDefault(),
                    TotalSponsorships = v.Where( v1 => v1.Key == "SponsorshipsAvailable" ).Select( v1 => v1.Value ).FirstOrDefault()
                } )
                .ToList()
                .Select( s => new SchoolDataItem
                {
                    Id = s.Id,
                    Name = s.Name,
                    IsActive = s.IsActive,
                    TeacherName = s.TeacherName,
                    TeacherEmail = s.TeacherEmail,
                    TotalSponsorships = s.TotalSponsorships.AsInteger(),
                    Sponsored = signups.Where( s1 => s1.SchoolGuid == s.Guid.ToString() ).Select( s1 => s1.Sponsored ).FirstOrDefault()

                } ).ToList();


            



        }

        public void LoadSchoolGrid()
        {
            if(Schools == null)
            {
                pnlSchoolList.Visible = false;
                return;
            }
            gSchoolList.DataSource = Schools;
            gSchoolList.DataBind();

            pnlSchoolList.Visible = true;
        }


        [Serializable]
        public class SchoolDataItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool IsActive { get; set; }
            public string TeacherName { get; set; }
            public string TeacherEmail { get; set; }
            public int TotalSponsorships { get; set; }
            public int Sponsored { get; set; }
            public int AvailableSponsorships
            {
                get
                {
                    return TotalSponsorships - Sponsored;
                }
            }
        }
    }
}
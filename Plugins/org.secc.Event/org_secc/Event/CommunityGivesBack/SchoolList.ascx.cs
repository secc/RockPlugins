using DocumentFormat.OpenXml.Drawing.Diagrams;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI.WebControls;


namespace RockWeb.Plugins.org_secc.CommunityGivesBack
{
    [DisplayName("Community Gives Back School List")]
    [Category("SECC > Community Gives Back")]
    [Description("School List for Community Gives Back")]

    [DefinedTypeField("Community Gives Back Schools",
            Description = "Defined Type that contains the list of Community Gives Back Schools.",
            IsRequired = true,
            Key = AttributeKeys.SchoolDefinedType,
            Order = 0)]
    [WorkflowTypeField("Registration Workflow Type",
            Description = "Community Gives Back Workflow Type",
            IsRequired = true,
            AllowMultiple = false,
            Order = 1,
            Key = AttributeKeys.RegistrationWorkflow)]
    [LinkedPage("School Registration Page",
        Description = "Page that contains a list of the sponsorship registrations.",
        IsRequired = false,
        Key = AttributeKeys.RegistrationListPage,
        Order = 2)]
    public partial class SchoolList : RockBlock
    {
        public class AttributeKeys
        {
            public const string SchoolDefinedType = "SchoolDefinedType";
            public const string RegistrationWorkflow = "RegistrationWorkflow";
            public const string RegistrationListPage = "RegistrationListPage";
        }

        #region Fields

        private List<SchoolDataItem> _schools = null;
        private int? _selectedSchoolId = null;

        #endregion

        #region Properties
        protected List<SchoolDataItem> Schools
        {
            get
            {
                if (_schools == null)
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

        protected int? SelectedSchoolId
        {
            get
            {
                if (_selectedSchoolId == null)
                {
                    _selectedSchoolId = ViewState[BlockId + "_SelectedSchoolId"] as int?;
                }
                return _selectedSchoolId;
            }
            set
            {
                _selectedSchoolId = value;
                ViewState[BlockId + "_SelectedSchoolId"] = _selectedSchoolId;
            }
        }

        protected string SchoolSortExpression
        {
            get
            {
                return ViewState[$"{this.BlockId}_SchoolSortExpression"].ToString();
            }
            set
            {
                ViewState[$"{this.BlockId}_SchoolSortExpression"] = value;
            }
        }

        protected SortDirection SchoolSortDirection
        {
            get
            {
                var directionInt = ViewState[$"{this.BlockId}_SchoolSortDirection"].ToString().AsIntegerOrNull();
                if(directionInt.HasValue)
                {
                    return (SortDirection)directionInt.Value;
                }
                return SortDirection.Ascending;
         
            }
            set
            {
                ViewState[$"{this.BlockId}_SchoolSortDirection"] = (int)value;
            }
        }
        #endregion 

        #region Base Control Methods
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            this.BlockUpdated += SchoolList_BlockUpdated;
            rFilter.ApplyFilterClick += rFilter_ApplyFilterClick;
            gSchoolList.GridRebind += gSchoolList_GridRebind;
            gSchoolList.Sorting += gSchoolList_Sorting;
            gSchoolList.Actions.ShowMergePerson = false;
            gSchoolList.Actions.ShowExcelExport = true;
            gSchoolList.Actions.ShowAdd = true;
            gSchoolList.Actions.ShowMergeTemplate = false;
            gSchoolList.RowItemText = "School";

            gSchoolList.Actions.AddClick += gSchoolList_AddClick;
            gSchoolList.RowSelected += gSchoolList_RowSelected;
            mdlSchoolEdit.SaveClick += mdlSchoolEdit_SaveClick;

        }



        private void gSchoolList_Sorting(object sender, GridViewSortEventArgs e)
        {
            if(e.SortExpression == SchoolSortExpression)
            {
                if(SchoolSortDirection == SortDirection.Ascending)
                {
                    SchoolSortDirection = SortDirection.Descending;
                }
                else
                {
                    SchoolSortDirection = SortDirection.Ascending;
                }
            }
            else
            {
                SchoolSortExpression = e.SortExpression;
                SchoolSortDirection = SortDirection.Ascending;
            }

            LoadSchoolGrid();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!IsPostBack)
            {
                SelectedSchoolId = null;
                Schools = null;
                SetFilter();
                BuildSchoolList();
                LoadSchoolGrid();
            }

        }
        #endregion

        #region Events
        private void gSchoolList_GridRebind(object sender, GridRebindEventArgs e)
        {
            LoadSchoolGrid();
        }

        private void gSchoolList_AddClick(object sender, EventArgs e)
        {
            ClearSchoolModal();
            mdlSchoolEdit.Show();
        }

        protected void gSchoolListItem_Edit(object sender, RowEventArgs e)
        {
            LoadSchoolForEdit((int)e.RowKeyValue);
        }

        private void gSchoolList_RowSelected(object sender, RowEventArgs e)
        {
            var schoolId = (int)e.RowKeyValue;
            var schoolGuid = DefinedValueCache.Get(schoolId).Guid;
            var pageParams = new Dictionary<string, string>();
            pageParams.Add("School", schoolGuid.ToString());
            NavigateToLinkedPage(AttributeKeys.RegistrationListPage, pageParams);
        }

        private void mdlSchoolEdit_SaveClick(object sender, EventArgs e)
        {
            if (SaveSchoolRecord())
            {
                mdlSchoolEdit.Hide();
                BuildSchoolList();
                LoadSchoolGrid();
            }
        }


        private void rFilter_ApplyFilterClick(object sender, EventArgs e)
        {
            rFilter.SaveUserPreference("Campaign", "Campaign", string.Join(";", cblCampaign.SelectedValues));

            Schools = null;
            BuildSchoolList();
            LoadSchoolGrid();
        }

        protected void rFilter_ClearFilterClick(object sender, EventArgs e)
        {
            rFilter.DeleteUserPreferences();
            SetFilter();
        }

        protected void rFilter_DisplayFilterValue(object sender, GridFilter.DisplayFilterValueArgs e)
        {
            if (e.Key == "Campaign")
            {
                var campaignValues = new List<string>();
                foreach (var v in e.Value.Split(';'))
                {
                    campaignValues.Add(v);
                }

                e.Value = string.Join(", ", campaignValues);
            }
        }

        private void SchoolList_BlockUpdated(object sender, EventArgs e)
        {
            Schools = null;
            SetFilter();
            BuildSchoolList();
            LoadSchoolGrid();
        }

        #endregion

        #region Methods
        private void BuildSchoolList()
        {
            if (Schools != null)
            {
                return;
            }

            var rockContext = new RockContext();

            var definedType = DefinedTypeCache.Get(GetAttributeValue(AttributeKeys.SchoolDefinedType).AsGuid(), rockContext);
            var definedValueEntityTypeId = EntityTypeCache.Get(typeof(DefinedValue)).Id;
            var workflowEntityTypeId = EntityTypeCache.Get(typeof(Workflow)).Id;
            var workflowType = WorkflowTypeCache.Get(GetAttributeValue(AttributeKeys.RegistrationWorkflow).AsGuid(), rockContext);

            if (definedType == null || workflowType == null)
            {
                return;
            }


            var attributeValueService = new AttributeValueService(rockContext);
            var definedValueService = new DefinedValueService(rockContext);
            var workflowService = new WorkflowService(rockContext);

            var workflowAVQry = attributeValueService.Queryable().AsNoTracking()
                .Where(v => v.Attribute.EntityTypeId == workflowEntityTypeId);

            var signups = workflowService.Queryable().AsNoTracking()
                .Where(w => w.WorkflowTypeId == workflowType.Id)
                .Join(workflowAVQry, w => w.Id, v => v.EntityId,
                    (w, v) => new { WorkflowId = w.Id, AttributeKey = v.Attribute.Key, Value = v.Value })
                .GroupBy(w => w.WorkflowId)
                .Select(w => new
                {
                    WorkflowId = w.Key,
                    SchoolGuid = w.Where(w1 => w1.AttributeKey == "School").Select(w1 => w1.Value).FirstOrDefault(),
                    Sponsorships = w.Where(w1 => w1.AttributeKey == "StudentsToSponsor").Select(w1 => w1.Value).FirstOrDefault()
                })
                .ToList()
                .GroupBy(s => s.SchoolGuid)
                .Select(s => new { SchoolGuid = s.Key, Sponsored = s.Sum(s1 => s1.Sponsorships.AsInteger()) })
                .ToList();

            var definedValueAVQuery = attributeValueService.Queryable().AsNoTracking()
                .Where(v => v.Attribute.EntityTypeId == definedValueEntityTypeId);

            Schools = definedValueService.Queryable().AsNoTracking()
                .Where(v => v.DefinedTypeId == definedType.Id)
                .Join(definedValueAVQuery, v => v.Id, av => av.EntityId,
                    (v, av) => new
                    {
                        Id = v.Id,
                        Name = v.Value,
                        Guid = v.Guid,
                        IsActive = v.IsActive,
                        Key = av.Attribute.Key,
                        Value = av.Value,

                    })
                .GroupBy(v => v.Id)
                .Select(v => new
                {
                    Id = v.Key,
                    Name = v.Select(v1 => v1.Name).FirstOrDefault(),
                    Guid = v.Select(v1 => v1.Guid).FirstOrDefault(),
                    IsActive = v.Select(v1 => v1.IsActive).FirstOrDefault(),
                    TeacherName = v.Where(v1 => v1.Key == "ResourceTeacherName").Select(v1 => v1.Value).FirstOrDefault(),
                    TeacherEmail = v.Where(v1 => v1.Key == "ResourceTeacherEmail").Select(v1 => v1.Value).FirstOrDefault(),
                    TotalSponsorships = v.Where(v1 => v1.Key == "SponsorshipsAvailable").Select(v1 => v1.Value).FirstOrDefault(),
                    Campaign = v.Where(v1 => v1.Key == "Year").Select(v1 => v1.Value).FirstOrDefault()
                })
                .ToList()
                .Select(s => new SchoolDataItem
                {
                    Id = s.Id,
                    Name = s.Name,
                    IsActive = s.IsActive,
                    TeacherName = s.TeacherName,
                    TeacherEmail = s.TeacherEmail,
                    TotalSponsorships = s.TotalSponsorships.AsInteger(),
                    Campaign = s.Campaign,
                    Sponsored = signups.Where(s1 => s1.SchoolGuid == s.Guid.ToString()).Select(s1 => s1.Sponsored).FirstOrDefault()

                }).ToList();

            var campaigns = cblCampaign.SelectedValues.ToList();
            if(campaigns.Any())
            {
                Schools = Schools.Where(s => campaigns.Contains(s.Campaign)).ToList();
            }

            SchoolSortDirection = SortDirection.Ascending;
            SchoolSortExpression = "Name";

        }

        private void ClearSchoolModal()
        {
            SelectedSchoolId = null;
            tbCampaign.Text = string.Empty;
            tbSchoolName.Text = string.Empty;
            tbTeacherName.Text = string.Empty;
            tbTeacherEmail.Text = string.Empty;
            tbSponsorships.Text = string.Empty;
            cbActive.Checked = true;
        }

        private void LoadSchoolForEdit(int id)
        {
            ClearSchoolModal();
            using (var rockContext = new RockContext())
            {
                var school = new DefinedValueService(rockContext).Get(id);
                if (school == null)
                {
                    SelectedSchoolId = null;
                    return;
                }
                SelectedSchoolId = school.Id;
                school.LoadAttributes(rockContext);
                tbCampaign.Text = school.GetAttributeValue("Year");
                tbSchoolName.Text = school.Value;
                tbTeacherName.Text = school.GetAttributeValue("ResourceTeacherName");
                tbTeacherEmail.Text = school.GetAttributeValue("ResourceTeacherEmail");
                tbSponsorships.Text = school.GetAttributeValue("SponsorshipsAvailable").AsInteger().ToString();
                cbActive.Checked = school.IsActive;
            }

            mdlSchoolEdit.Show();
        }

        private void LoadSchoolGrid()
        {
            if (Schools == null)
            {
                pnlSchoolList.Visible = false;
                return;
            }
            var schoolQry = Schools.AsQueryable();
            switch (SchoolSortExpression)
            {
                case "Campaign":
                    if (SchoolSortDirection == SortDirection.Ascending)
                    {
                        schoolQry = schoolQry.OrderBy(s => s.Campaign)
                            .ThenBy(s => s.Name);
                    }
                    else
                    {
                        schoolQry = schoolQry.OrderByDescending(s => s.Campaign)
                            .ThenBy(s => s.Name);
                    }
                    break;
                case "Name":
                    if (SchoolSortDirection == SortDirection.Ascending)
                    {
                        schoolQry = schoolQry.OrderBy(s => s.Name)
                            .ThenBy(s => s.Campaign);
                    }
                    else
                    {
                        schoolQry = schoolQry.OrderByDescending(s => s.Name)
                            .ThenBy(s => s.Campaign);
                    }
                    break;
                case "Teacher":
                    if(SchoolSortDirection == SortDirection.Ascending)
                    {
                        schoolQry = schoolQry.OrderBy(s => s.TeacherName);
                    }
                    else
                    {
                        schoolQry = schoolQry.OrderByDescending(s => s.TeacherName);
                    }
                    break;
                case "TotalSponsorships":
                    if(SchoolSortDirection == SortDirection.Ascending)
                    {
                        schoolQry = schoolQry.OrderBy(s => s.TotalSponsorships);
                    }
                    else
                    {
                        schoolQry = schoolQry.OrderByDescending(s => s.TotalSponsorships);
                    }
                    break;
                case "Sponsored":
                    if(SchoolSortDirection == SortDirection.Ascending)
                    {
                        schoolQry = schoolQry.OrderBy(s => s.Sponsored);
                    }
                    else
                    {
                        schoolQry = schoolQry.OrderByDescending(s => s.Sponsored);
                    }
                    break;
                case "AvailableSponsorships":
                    if(SchoolSortDirection == SortDirection.Ascending)
                    {
                        schoolQry = schoolQry.OrderBy(s => s.AvailableSponsorships);
                    }
                    else
                    {
                        schoolQry = schoolQry.OrderByDescending(s => s.AvailableSponsorships);
                    }
                    break;
                default:
                    schoolQry = schoolQry.OrderBy(s => s.Name)
                        .ThenBy(s => s.Campaign);
                    break;
            }
            gSchoolList.DataSource = schoolQry.ToList();
            gSchoolList.DataBind();

            pnlSchoolList.Visible = true;
        }

        private bool SaveSchoolRecord()
        {
            var rockContext = new RockContext();
            var definedValueService = new DefinedValueService(rockContext);

            var school = new DefinedValue();
            if (SelectedSchoolId.HasValue && SelectedSchoolId > 0)
            {
                school = definedValueService.Get(SelectedSchoolId.Value);
            }
            else
            {
                school.DefinedTypeId = DefinedTypeCache.GetId(GetAttributeValue(AttributeKeys.SchoolDefinedType).AsGuid()).Value;
                school.Guid = Guid.NewGuid();
                definedValueService.Add(school);
            }

            school.Value = tbSchoolName.Text.Trim();
            school.IsActive = cbActive.Checked;

            rockContext.SaveChanges();

            school.LoadAttributes(rockContext);
            school.SetAttributeValue("ResourceTeacherName", tbTeacherName.Text.Trim());
            school.SetAttributeValue("ResourceTeacherEmail", tbTeacherEmail.Text.Trim());
            school.SetAttributeValue("SponsorshipsAvailable", tbSponsorships.Text.AsInteger());
            school.SetAttributeValue("Year", tbCampaign.Text.Trim());

            school.SaveAttributeValues(rockContext);
            rockContext.SaveChanges();


            return school.Id > 0;

        }

        private void SetFilter()
        {
            var rockContext = new RockContext();
            var definedValueEntityId = EntityTypeCache.Get(typeof(DefinedValue)).Id;
            var definedType = DefinedTypeCache.Get(GetAttributeValue(AttributeKeys.SchoolDefinedType).AsGuid());

            var definedTypeIdAsString = definedType.Id.ToString();

            var campaigns = new AttributeValueService(rockContext)
                .Queryable().AsNoTracking()
                .Where(v => v.Attribute.EntityTypeId == definedValueEntityId)
                .Where(v => v.Attribute.EntityTypeQualifierColumn == "DefinedTypeId")
                .Where(v => v.Attribute.EntityTypeQualifierValue == definedTypeIdAsString)
                .Where(v => v.Attribute.Key == "Year")
                .Where(v => v.Value != "")
                .Select(v => v.Value)
                .Distinct()
                .OrderBy(v => v)
                .ToList();

            cblCampaign.Items.Clear();

            foreach (var c in campaigns)
            {
                cblCampaign.Items.Add(new ListItem(c, c));
            }

            var campaignValue = rFilter.GetUserPreference("Campaign");
            if(!string.IsNullOrWhiteSpace(campaignValue))
            {
                cblCampaign.SetValues(campaignValue.Split(';').ToList());
            }


        }
        #endregion

        #region Helper Class
        [Serializable]
        public class SchoolDataItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool IsActive { get; set; }
            public string TeacherName { get; set; }
            public string TeacherEmail { get; set; }
            public int TotalSponsorships { get; set; }
            public string Campaign { get; set; }
            public int Sponsored { get; set; }
            public int AvailableSponsorships
            {
                get
                {
                    return TotalSponsorships - Sponsored;
                }
            }
        }
        #endregion



    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using Lucene.Net.Support;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Lava;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;


namespace RockWeb.Plugins.org_secc.GroupManager
{

    [DisplayName( "Camp Allergies" )]
    [Category( "SECC > Camp" )]
    [Description( "Campers in Group who have Allergies." )]
    [GroupField( "Parent Group", "The pareent group of the Camp Small Group List", true, Key = AttributeKey.ParentGroup )]
    [CodeEditorField( "Allergy Content Lava", "Allergy Lava", CodeEditorMode.Lava, CodeEditorTheme.Rock, 300, true, Key = AttributeKey.AllergyLava )]
    public partial class CampGroupAllergyReport : RockBlock
    {

        public class AttributeKey
        {
            public const string AllergyLava = "AllergyLava";
            public const string ParentGroup = "ParentGroup";
        }

        int? _groupId;
        string _campSmallGroupGuid = "2936a009-2552-448e-9c3c-17d9cc0f8742";

        private int? GroupId
        {
            get
            {
                if (!_groupId.HasValue)
                {
                    _groupId = ViewState[$"{this.BlockId}_GroupId"] as int?;
                }
                return _groupId;

            }
            set
            {
                _groupId = value;
                ViewState[$"{this.BlockId}_GroupId"] = _groupId;

            }
        }


        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            nbMain.Title = "";
            nbMain.Text = "";
            nbMain.Visible = false;
            if (!IsPostBack)
            {
                LoadSmallGroup();
            }
        }

        private void LoadSmallGroup()
        {
            using (var rockContext = new RockContext())
            {
                var groupService = new GroupService( rockContext );
                var groupMemberService = new GroupMemberService( rockContext );
                var parentGroupGuid = GetAttributeValue( AttributeKey.ParentGroup ).AsGuid();

                var parentGroup = groupService.Get( parentGroupGuid );

                if (parentGroup == null)
                {
                    nbMain.Title = "Parent Group Settring Required";
                    nbMain.Text = "Parent Group is required to pull allergy report. Contact IT Help.";
                    nbMain.NotificationBoxType = NotificationBoxType.Danger;
                    nbMain.Visible = true;
                    return;
                }

                var smallGroupIds = groupService.GetAllDescendentGroupIds( parentGroup.Id, true );

                var leaderGroupId = groupMemberService.Queryable().AsNoTracking()
                    .Join( smallGroupIds, m => m.GroupId, g => g, ( m, g ) => m )
                    .Where( m => m.GroupRole.IsLeader )
                    .Where( m => m.GroupMemberStatus == GroupMemberStatus.Active )
                    .Where( m => m.PersonId == CurrentPerson.Id )
                    .Select( m => m.GroupId )
                    .FirstOrDefault();


                if (leaderGroupId <= 0)
                {
                    nbMain.Title = "No Groups Found";
                    nbMain.Visible = true;
                    return;
                }

                var sqlParam = new SqlParameter( "@GroupId", leaderGroupId );
                var allergies = rockContext.Database.SqlQuery<CampGroupMemberAllergies>( "dbo._org_secc_CampManager_GetGroupMemberAllergies @GroupId",
                    new SqlParameter[] { sqlParam } )
                    .ToList();

                var lavaMergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, CurrentPerson );
                lavaMergeFields.Add( "AllergyList", allergies );

                lResults.Text = GetAttributeValue( AttributeKey.AllergyLava ).ResolveMergeFields( lavaMergeFields );

                pnlResults.Visible = true;



            }
        }

        public class CampGroupMemberAllergies : ILiquidizable
        {
            [LavaIgnore]
            public object this[object key]
            {
                get
                {
                    switch (key.ToStringSafe())
                    {
                        case "Id":
                            return Id;
                        case "LastName":
                            return LastName;
                        case "FirstName":
                            return NickName;
                        case "NickName":
                            return NickName;
                        case "GroupName":
                            return GroupName;
                        case "GroupRole":
                            return GroupRole;
                        case "Eggs":
                            return Eggs.AsBoolean();
                        case "Fish":
                            return Fish.AsBoolean();
                        case "MilkAndDairy":
                            return MilkAndDairy.AsBoolean();
                        case "Peanuts":
                            return Peanuts.AsBoolean();
                        case "TreeNuts":
                            return TreeNuts.AsBoolean();
                        case "Shellfish":
                            return Shellfish.AsBoolean();
                        case "Soy":
                            return Soy.AsBoolean();
                        case "WheatAndGluten":
                            return WheatGluten.AsBoolean();
                        case "Other":
                            return Other;
                        case "AllAllergies":
                            return AllAllergies;
                        default:
                            return string.Empty;

                    }
                }
            }

            public int Id { get; set; }
            public string LastName { get; set; }
            public string NickName { get; set; }
            public string GroupName { get; set; }
            public string GroupRole { get; set; }
            public string Eggs { get; set; }
            public string Fish { get; set; }
            [Column( "Milk and Dairy" )]
            public string MilkAndDairy { get; set; }
            public string Peanuts { get; set; }
            [Column( "Tree Nuts" )]
            public string TreeNuts { get; set; }
            public string Shellfish { get; set; }
            public string Soy { get; set; }
            [Column( "Wheat and Gluten" )]
            public string WheatGluten { get; set; }
            public string Other { get; set; }
            public string AllAllergies
            {
                get
                {
                    StringBuilder sb = new StringBuilder();

                    if (Eggs.AsBoolean())
                    {
                        sb.Append( "Eggs, " );
                    }
                    if (Fish.AsBoolean())
                    {
                        sb.Append( "Fish, " );
                    }
                    if (MilkAndDairy.AsBoolean())
                    {
                        sb.Append( "Milk & Dairy, " );
                    }
                    if (Peanuts.AsBoolean())
                    {
                        sb.Append( "Peanuts, " );
                    }
                    if (TreeNuts.AsBoolean())
                    {
                        sb.Append( "Tree Nuts, " );
                    }
                    if (Shellfish.AsBoolean())
                    {
                        sb.Append( "Shellfish, " );
                    }
                    if (Shellfish.AsBoolean())
                    {
                        sb.Append( "Soy, " );
                    }
                    if (Shellfish.AsBoolean())
                    {
                        sb.Append( "Wheat & Gluten, " );
                    }
                    sb.Append( $"{Other}, " );

                    return sb.ToString().ReplaceLastOccurrence( ",", string.Empty ).Trim();
                }
            }

            [LavaIgnore]
            public List<string> AvailableKeys
            {
                get
                {
                    return new List<string> {
                        "Id",
                        "LastName",
                        "FirstName",
                        "NickName",
                        "GroupName",
                        "GroupRole",
                        "Eggs",
                        "Fish",
                        "MilkAndDairy",
                        "Peanuts",
                        "TreeNuts",
                        "Shellfish",
                        "Soy",
                        "WheatAndGluten",
                        "Other",
                        "AllAllergies"
                    };
                }
            }

            public bool ContainsKey( object key )
            {
                var keys = AvailableKeys;
                return keys.Contains( key.ToStringSafe() );
            }

            public object ToLiquid()
            {
                return this;
            }

        }
    }
}
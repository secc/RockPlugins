using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using System;
using System.ComponentModel;
using System.Web.UI;

namespace RockWeb.Plugins.org_secc.GroupManager
{

    [DisplayName( "Group Detail Button Inject" )]
    [Category( "SECC > Groups" )]
    [Description( "Adds custom buttons to the Group Detail Block on this page" )]
    [ContextAware( typeof( Group ) )]

    [BooleanField( "Enforce Publish Group Security",
        "Should user security be enforced with the Publish Group button",
        IsRequired = false,
        DefaultBooleanValue = true,
        Order = 0,
        Key = AttributeKey.PublishGroupUseSecurity )]

    [LinkedPage( "Publish Group Detail Page",
        Description = "Publish Group Detail Page",
        IsRequired = false,
        Order = 1,
        Key = AttributeKey.PublishGroupDetailPage )]

    [BooleanField( "Enforce Quality Check Security",
        "Should user security be enforced with the Quality Check button.",
        IsRequired = false,
        DefaultBooleanValue = true,
        Order = 2,
        Key = AttributeKey.QualityCheckGroupUseSecurity )]

    [LinkedPage("Quality Check Detail Page",
        Description = "Quality Check Page",
        IsRequired = false,
        Order = 3,
        Key = AttributeKey.QualityCheckDetailPage)]

    public partial class GroupDetailButtonInject : ContextEntityBlock
    {
        private static class AttributeKey
        {
            public const string PublishGroupUseSecurity = "PublishGroupUseSecurity";
            public const string QualityCheckGroupUseSecurity = "QualityCheckGroupUseSecurity";
            public const string PublishGroupDetailPage = "PublishGroupPage";
            public const string QualityCheckDetailPage = "QualityCheckPage";
        }

        public Group SelectedGroup { get; set; }
        public bool ShowPublishGroup { get; set; } = false;
        public bool ShowQualityCheck { get; set; } = false;
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            int? groupId = PageParameter( "GroupId" ).AsIntegerOrNull();

            if ( groupId.HasValue )
            {
                BindButtons( groupId.Value );
            }

        }

        private void BindButtons( int groupId )
        {
            var rockContext = new RockContext();
            SelectedGroup = new GroupService( rockContext )
                .Get( groupId );

            if ( SelectedGroup == null )
            {
                return;
            }

            var enforcePublishGroupSecurity = GetAttributeValue( AttributeKey.PublishGroupUseSecurity ).AsBoolean();

            if ( !enforcePublishGroupSecurity )
            {
                ShowPublishGroup = true;
            }
            else
            {
                ShowPublishGroup = SelectedGroup.IsAuthorized( Authorization.EDIT, CurrentPerson );
            }

            var enforceQualityCheckGroupSecurity = GetAttributeValue( AttributeKey.QualityCheckGroupUseSecurity ).AsBoolean();

            if(!enforceQualityCheckGroupSecurity)
            {
                ShowQualityCheck = true;
            }
            else
            {
                ShowQualityCheck = SelectedGroup.IsAuthorized( Authorization.EDIT, CurrentPerson );
            }

            var publishPageGuid = GetAttributeValue( AttributeKey.PublishGroupDetailPage ).AsGuid();
            var publishPage = PageCache.Get( publishPageGuid );

            var qualityCheckPageGuid = GetAttributeValue(AttributeKey.QualityCheckDetailPage).AsGuid();
            var qualityCheckPage = PageCache.Get( qualityCheckPageGuid );

            var sbScript = new System.Text.StringBuilder();
            if(ShowQualityCheck && qualityCheckPage != null)
            {
                sbScript.Append( $"$('[id*=\"hlMap\"]').parent().append('<a href=\"/page/{qualityCheckPage.Id}?GroupId={groupId}\" class=\"btn btn-sm btn-square btn-default\" " );
                sbScript.Append( $"title=\"Quality Check\" height=\"500px\"><i class=\"fa fa-badge-check\"></i></a> '); \n" );
            }

            if(ShowPublishGroup && publishPage != null)
            {
                sbScript.Append( $"$('[id*=\"hlMap\"]').parent().append('<a href=\"/page/{publishPage.Id}?GroupId={groupId}\" class=\"btn btn-sm btn-square btn-default\" ");
                sbScript.Append( $"title=\"Publish Group\" height=\"500px\"><i class=\"fa fa-globe\"></i></a>');" );
            }

            if ( sbScript.ToString().IsNotNullOrWhiteSpace() )
            {
                ScriptManager.RegisterStartupScript( upMain, upMain.GetType(), "ButtonInject" + RockDateTime.Now.Ticks.ToString(), sbScript.ToString(), true );
            }
        }
    }
}
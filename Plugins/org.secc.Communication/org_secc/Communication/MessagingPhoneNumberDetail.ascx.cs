using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using org.secc.Communication;
using org.secc.Communication.Messaging.Model;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.Communication
{

    [DisplayName( "Messaging Phone Number Detail" )]
    [Category( "SECC > Communication" )]
    [Description( "Messaging Phone Number Detail" )]
    public partial class MessagingPhoneNumberDetail : RockBlock
    {
        #region Fields
        MessagingPhoneNumber currentPhoneNumber = null;
        #endregion

        #region Properties
        private MessagingPhoneNumber CurrentPhoneNumber
        {
            get
            {
                if ( currentPhoneNumber == null )
                {
                    currentPhoneNumber = LoadPhoneNumber();
                }
                return currentPhoneNumber;
            }
            set
            {
                currentPhoneNumber = value;
            }
        }

        protected string PhoneIdDrawerItem
        {
            get
            {
                return CurrentPhoneNumber == null ? string.Empty : CurrentPhoneNumber.Id.ToString();
            }

        }

        protected string CreatedByDrawerItem
        {
            get
            {
                if ( CurrentPhoneNumber == null )
                {
                    return string.Empty;
                }

                return GetAuditLink( CurrentPhoneNumber.CreatedBy, CurrentPhoneNumber.CreatedOnDateTime );

            }
        }

        protected string ModifiedByDrawerItem
        {
            get
            {
                if ( CurrentPhoneNumber == null )
                {
                    return string.Empty;
                }

                return GetAuditLink( CurrentPhoneNumber.ModifiedBy, CurrentPhoneNumber.ModifiedOnDateTime );
            }
        }

        #endregion

        #region Base Control Methods
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            this.BlockUpdated += Block_BlockUpdated;

            btnEditPhone.Click += btnEditPhone_Click;
            btnSavePhone.Click += btnSavePhone_Click;
            btnCancelPhone.Click += btnCancelPhone_Click;
            lbChangeOwner.Click += lbChangeOwner_Click;
            lbDeleteOwner.Click += lbDeleteOwner_Click;
            lbSetOwnerPerson.Click += lbSetOwnerPerson_Click;
            lbSetOwnerGroup.Click += lbSetOwnerGroup_Click;
            lbUpdateOwnerPerson.Click += lbUpdateOwnerPerson_Click;
            lbUpdateOwnerGroup.Click += lbUpdateOwnerGroup_Click;
            lbCancelOwnerPerson.Click += lbCancelOwnerPerson_Click;
            lbCancelOwnerGroup.Click += lbCancelOwnerGroup_Click;
            this.AddConfigurationUpdateTrigger( upPhoneDetail );
        }



        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            NotificationBoxClear();
            LoadPanelDrawerScript();
            if ( !Page.IsPostBack )
            {
                if ( PageParameter( "IsSave" ).AsBoolean() )
                {
                    NotificationBoxShowSaveAlert();
                }
                ShowDetail();
            }
        }



        #endregion

        #region Events
        private void Block_BlockUpdated( object sender, EventArgs e )
        {
            ShowDetail();
        }

        private void btnCancelPhone_Click( object sender, EventArgs e )
        {
            if ( hfMessagingPhoneId.Value.AsGuid() == Guid.Empty )
            {
                RedirectToParentPage();
                return;
            }

            ShowDetailView();
        }

        private void btnEditPhone_Click( object sender, EventArgs e )
        {
            ShowDetailEdit();
        }

        private void btnSavePhone_Click( object sender, EventArgs e )
        {
            var phoneId = hfMessagingPhoneId.Value.Trim();

            MessagingPhoneNumber phone = null;
            var client = new MessagingClient();

            if ( phoneId == Guid.Empty.ToString() )
            {
                phone = new MessagingPhoneNumber();
                phone.CreatedBy = new MessagingPerson( CurrentPerson );
                phone.Sid = ddlPhoneNumber.SelectedValue;
                phone.Number = client.GetTwilioNumbers().FirstOrDefault( t => t.Sid.Equals( phone.Sid ) ).PhoneNumber;
            }
            else
            {
                phone = client.GetPhoneNumber( phoneId );
            }

            phone.Name = tbPhoneNumberName.Text.Trim();
            phone.Description = tbDescription.Text.Trim();
            phone.IsActive = switchActive.Checked;
            phone.ModifiedBy = new MessagingPerson( CurrentPerson );

            if ( hfOwner.Value.IsNullOrWhiteSpace() )
            {
                phone.OwnedBy = null;
            }
            else if ( hfOwner.Value.Split( "|".ToCharArray() )[0].AsInteger() == EntityTypeCache.Get( typeof( Group ) ).Id )
            {
                var messagingGroup = new MessagingGroup( hfOwner.Value.Split( "|".ToCharArray() )[1].AsGuid() );
                phone.OwnedBy = new MessagingOwner();
                phone.OwnedBy.OwnerGroup = messagingGroup;
            }
            else if ( hfOwner.Value.Split( "|".ToCharArray() )[0].AsInteger() == EntityTypeCache.Get( typeof( PersonAlias ) ).Id )
            {
                var messagingPerson = new MessagingPerson( hfOwner.Value.Split( "|".ToCharArray() )[1].AsGuid() );
                phone.OwnedBy = new MessagingOwner();
                phone.OwnedBy.OwnerPerson = messagingPerson;
            }

            if ( phone.Id.IsEmpty() )
            {
                phone = client.AddPhoneNumber( phone );
                hfMessagingPhoneId.Value = phone.Id.ToString();
                var queryParams = new Dictionary<string, string>();
                queryParams.Add( "MessagingNumber", phone.Id.ToString() );
                queryParams.Add( "IsSave", bool.TrueString );

                NavigateToCurrentPage( queryParams );
                return;
            }
            else
            {
                phone = client.UpdatePhoneNumber( phone );
            }
            CurrentPhoneNumber = phone;


            ShowDetailView();
            NotificationBoxShowSaveAlert();
        }

        private void lbChangeOwner_Click( object sender, EventArgs e )
        {
            LoadChangeOwnerPanel();
        }

        private void lbDeleteOwner_Click( object sender, EventArgs e )
        {
            hfOwner.Value = string.Empty;
            lOwnerNameEdit.Text = "(none)";
            lblOwner.Text = "Not Set";

        }

        private void lbSetOwnerGroup_Click( object sender, EventArgs e )
        {
            gpSetOwner.SetValue( null );
            pnlOwner.Visible = false;
            pnlOwnerGroup.Visible = true;
            pnlOwnerPerson.Visible = false;

            if ( hfOwner.Value.IsNullOrWhiteSpace() )
            {
                return;
            }

            var entityTypeId = hfOwner.Value.Split( "|".ToCharArray() )[0].AsInteger();
            var groupGuid = hfOwner.Value.Split( "|".ToCharArray() )[1].AsGuid();

            if ( entityTypeId == EntityTypeCache.Get( typeof( Group ) ).Id )
            {
                var group = new GroupService( new RockContext() ).Get( groupGuid );
                gpSetOwner.SetValue( group );
            }
        }

        private void lbSetOwnerPerson_Click( object sender, EventArgs e )
        {
            ppSetOwner.SetValue( null );
            pnlOwner.Visible = false;
            pnlOwnerGroup.Visible = false;
            pnlOwnerPerson.Visible = true;

            if ( hfOwner.Value.IsNullOrWhiteSpace() )
            {
                return;
            }

            var entityTypeId = hfOwner.Value.Split( "|".ToCharArray() )[0].AsInteger();
            var personAliasGuid = hfOwner.Value.Split( "|".ToCharArray() )[1].AsGuid();

            if ( entityTypeId == EntityTypeCache.Get( typeof( PersonAlias ) ).Id )
            {
                var person = new PersonAliasService( new RockContext() ).Get( personAliasGuid ).Person;
                ppSetOwner.SetValue( person );
            }
        }

        private void lbCancelOwnerGroup_Click( object sender, EventArgs e )
        {
            pnlOwner.Visible = true;
            pnlOwnerPerson.Visible = false;
            pnlOwnerGroup.Visible = false;
        }

        private void lbCancelOwnerPerson_Click( object sender, EventArgs e )
        {
            pnlOwner.Visible = true;
            pnlOwnerPerson.Visible = false;
            pnlOwnerGroup.Visible = false;
        }

        private void lbUpdateOwnerGroup_Click( object sender, EventArgs e )
        {
            pnlOwner.Visible = true;
            pnlOwnerPerson.Visible = false;
            pnlOwnerGroup.Visible = false;

            if ( gpSetOwner.GroupId.HasValue )
            {
                var groupEntityType = EntityTypeCache.Get( typeof( Group ) );
                var group = new GroupService( new RockContext() ).Get( gpSetOwner.GroupId.Value );
                hfOwner.Value = $"{groupEntityType.Id}|{group.Guid}";
                lblOwner.Text = $"{group.Name}";
                lOwnerNameEdit.Text = $"{group.Name}";
            }

        }

        private void lbUpdateOwnerPerson_Click( object sender, EventArgs e )
        {
            pnlOwner.Visible = true;
            pnlOwnerPerson.Visible = false;
            pnlOwnerGroup.Visible = false;

            if ( ppSetOwner.PersonAliasId.HasValue )
            {
                var personAliasEntityType = EntityTypeCache.Get( typeof( PersonAlias ) );
                var personAlias = new PersonAliasService( new RockContext() ).Get( ppSetOwner.PersonAliasId.Value );
                hfOwner.Value = $"{personAliasEntityType.Id}|{personAlias.Guid}";
                lblOwner.Text = $"{personAlias.Person.FullName}";
                lOwnerNameEdit.Text = $"{personAlias.Person.FullName}";
            }
        }

        #endregion

        #region Methods

        private void ClearDetailPanel()
        {
            tbPhoneNumberName.Text = string.Empty;
            lPhoneNumber.Text = string.Empty;
            hlStatus.Text = string.Empty;
            hlStatus.Visible = false;
            tbDescription.Text = string.Empty;
            switchActive.Checked = false;
        }

        private string GetAuditLink( MessagingPerson p, DateTime? actionDateUtc )
        {
            var stringBuilder = new StringBuilder();
            var internalUrl = GlobalAttributesCache.Value( "InternalApplicationRoot" );

            if ( p != null )
            {
                var rockPerson = new PersonAliasService( new Rock.Data.RockContext() ).Get( p.AliasGuid ).Person;
                stringBuilder.AppendFormat( "<a href='{0}Person/{1}'>{2}</a>", internalUrl, rockPerson.Id, rockPerson.FullName );
            }

            if ( actionDateUtc.HasValue )
            {
                var localTime = actionDateUtc.Value.ToLocalTime();
                stringBuilder.AppendFormat( " <small class='js-date-rollover' data-toggle='tooltip' data-placement='top' title='{0}'>({1})</small>",
                    localTime.ToString(), localTime.ToRelativeDateString() );
            }
            return stringBuilder.ToString();
        }

        private void LoadChangeOwnerPanel()
        {
            var currentOwnerValue = hfOwner.Value;

            if ( currentOwnerValue.IsNotNullOrWhiteSpace() )
            {
                var entityTypeId = currentOwnerValue.Split( "|".ToCharArray() )[0].AsInteger();
                var entityGuid = currentOwnerValue.Split( "|".ToCharArray() )[1].AsGuid();

                var rockContext = new RockContext();
                if ( entityTypeId == EntityTypeCache.Get( typeof( PersonAlias ) ).Id )
                {
                    var ownerPerson = new PersonAliasService( rockContext ).Get( entityGuid );
                    lblOwner.Text = ownerPerson == null ? string.Empty : ownerPerson.Person.FullName;
                }
                else if ( entityTypeId == EntityTypeCache.Get( typeof( Group ) ).Id )
                {
                    var ownerGroup = new GroupService( rockContext ).Get( entityGuid );
                    lblOwner.Text = ownerGroup == null ? string.Empty : ownerGroup.Name;
                }
                lbDeleteOwner.Visible = true;
            }
            else
            {
                lbDeleteOwner.Visible = false;
                lblOwner.Text = "Not Set";
            }


            pnlOwner.Visible = true;
            pnlOwnerGroup.Visible = false;
            pnlOwnerPerson.Visible = false;
            mdlAssignOwner.Show();
        }

        private void LoadPanelDrawerScript()
        {
            string script = @"
$('.js-drawerpull').on('click', function () {
    $( this ).closest( '.panel-drawer' ).toggleClass( 'open' );
    $( this ).siblings( '.drawer-content' ).slideToggle();

    $expanded = $(this).children('input.filter-expanded');
    $expanded.val($expanded.val() == 'True' ? 'False' : 'True');

    var icon = $( this ).find( 'i' );
    var iconOpenClass = icon.attr( 'data-icon-open' ) || 'fa fa-chevron-up';
    var iconCloseClass = icon.attr( 'data-icon-closed' ) || 'fa fa-chevron-down';

    if ($( this ).closest( '.panel-drawer' ).hasClass( 'open' )) {
        icon.attr( 'class', iconOpenClass );
    }
    else {
        icon.attr( 'class', iconCloseClass );
    }
});

$('.js-date-rollover').tooltip();
";
            ScriptManager.RegisterStartupScript( pnlDetails, typeof( Panel ), "MessagingPhoneAuditPanel" + DateTime.Now.Ticks, script, true );
        }

        private MessagingPhoneNumber LoadPhoneNumber()
        {
            var phoneId = hfMessagingPhoneId.Value;

            if ( phoneId.IsNullOrWhiteSpace() )
            {
                return null;
            }

            var client = new MessagingClient();
            return client.GetPhoneNumber( phoneId );
        }

        private void LoadTwilioNumbers()
        {
            var client = new MessagingClient();

            var phoneSids = client.GetPhoneNumbers()
                .Select( p => p.Sid ).ToList();

            var twilioNumbers = client.GetTwilioNumbers()
                .Where( t => !phoneSids.Contains( t.Sid ) )
                .OrderBy( t => t.FriendlyName )
                .ToList();

            ddlPhoneNumber.Items.Clear();
            foreach ( var tn in twilioNumbers )
            {
                var formattedNumber = string.Empty;
                if ( tn.PhoneNumber.StartsWith( "1" ) )
                {
                    formattedNumber = PhoneNumber.FormattedNumber( "1", tn.PhoneNumber.Substring( 1 ) );
                }
                else
                {
                    formattedNumber = tn.PhoneNumber;
                }

                var listItem = new ListItem( $"{formattedNumber} - {tn.FriendlyName}", tn.Sid );
                ddlPhoneNumber.Items.Add( listItem );
            }
            ddlPhoneNumber.Items.Insert( 0, new ListItem( "", "" ) );
        }

        private void NotificationBoxClear()
        {
            nbPhoneNotification.Title = string.Empty;
            nbPhoneNotification.Text = string.Empty;
            nbPhoneNotification.Visible = false;
        }

        private void NotificationBoxSet( string title, string text, NotificationBoxType boxType )
        {
            nbPhoneNotification.Title = title;
            nbPhoneNotification.Text = text;
            nbPhoneNotification.NotificationBoxType = boxType;
            nbPhoneNotification.Visible = title.IsNotNullOrWhiteSpace() || text.IsNotNullOrWhiteSpace();
        }

        private void NotificationBoxShowSaveAlert()
        {
            NotificationBoxSet( "<i class=\"fas fa-save\"></i> Phone Number Saved",
                "Phone Number Saved Successfully.",
                 NotificationBoxType.Success );
        }

        private void RedirectToParentPage()
        {
            NavigateToParentPage();
        }

        private void ShowDetail()
        {
            var phoneId = PageParameter( "MessagingNumber" ).AsGuid();
            hfMessagingPhoneId.Value = phoneId.ToString();

            if ( phoneId.Equals( Guid.Empty ) )
            {
                ShowDetailEdit();
            }
            else
            {
                ShowDetailView();
            }
            pnlDetails.Visible = true;
        }

        private void ShowDetailEdit()
        {
            pnlViewDetails.Visible = false;
            pnlEditDetails.Visible = true;
            pnlAuditDrawer.Visible = false;
            if ( CurrentPhoneNumber == null )
            {

                lTitle.Text = "New Phone Number";
                hlStatus.Text = "New";
                hlStatus.LabelType = LabelType.Info;
                hlStatus.Visible = true;
                ClearDetailPanel();
                LoadTwilioNumbers();

                ddlPhoneNumber.Visible = true;
                lPhoneNumber.Visible = false;
            }
            else
            {
                ClearDetailPanel();
                lTitle.Text = $"Phone Number - {CurrentPhoneNumber.Name}";
                hlStatus.Text = CurrentPhoneNumber.IsActive ? "Active" : "Inactive";
                hlStatus.LabelType = CurrentPhoneNumber.IsActive ? LabelType.Success : LabelType.Default;
                hlStatus.Visible = true;

                tbPhoneNumberName.Text = CurrentPhoneNumber.Name;
                tbDescription.Text = CurrentPhoneNumber.Description;
                ddlPhoneNumber.Visible = false;
                lPhoneNumber.Visible = true;
                lPhoneNumber.Text = CurrentPhoneNumber.NumberFormatted;
                switchActive.Checked = CurrentPhoneNumber.IsActive;

                if ( currentPhoneNumber.OwnedBy != null && currentPhoneNumber.OwnedBy.OwnerGroup != null )
                {
                    var group = new GroupService( new RockContext() ).Get( currentPhoneNumber.OwnedBy.OwnerGroup.GroupGuid );
                    lOwnerNameEdit.Text = group != null ? group.Name : "(none)";
                    hfOwner.Value = $"{EntityTypeCache.Get( typeof( Group ) ).Id}|{currentPhoneNumber.OwnedBy.OwnerGroup.GroupGuid.ToString()}";
                }
                else if ( currentPhoneNumber.OwnedBy != null && currentPhoneNumber.OwnedBy.OwnerPerson != null )
                {
                    var person = new PersonAliasService( new RockContext() ).Get( currentPhoneNumber.OwnedBy.OwnerPerson.AliasGuid ).Person;
                    lOwnerNameEdit.Text = person != null ? person.FullName : "(none)";
                    hfOwner.Value = $"{EntityTypeCache.Get( typeof( PersonAlias ) ).Id}|{currentPhoneNumber.OwnedBy.OwnerPerson.AliasGuid.ToString()}";
                }
                else
                {
                    lOwnerNameEdit.Text = "(none)";
                    hfOwner.Value = string.Empty;
                }

            }
        }

        private void ShowDetailView()
        {
            pnlViewDetails.Visible = true;
            pnlEditDetails.Visible = false;
            pnlAuditDrawer.Visible = true;
            pnlViewOwner.Visible = false;


            lTitle.Text = $"Phone Number - {CurrentPhoneNumber.Name}";
            lPhoneNumberView.Text = CurrentPhoneNumber.NumberFormatted;
            lDescription.Text = CurrentPhoneNumber.Description;

            if ( CurrentPhoneNumber.OwnedBy != null )
            {
                pnlViewOwner.Visible = true;
                if ( CurrentPhoneNumber.OwnedBy.OwnerGroup != null )
                {
                    var group = new GroupService( new RockContext() ).Get( CurrentPhoneNumber.OwnedBy.OwnerGroup.GroupGuid );
                    lViewOwner.Text = group != null ? group.Name : String.Empty;
                }
                else if ( CurrentPhoneNumber.OwnedBy.OwnerPerson != null )
                {
                    var person = new PersonAliasService( new RockContext() ).Get( CurrentPhoneNumber.OwnedBy.OwnerPerson.AliasGuid )
                        .Person;
                    lViewOwner.Text = person != null ? person.FullName : String.Empty;
                }

            }
            hlStatus.Text = CurrentPhoneNumber.IsActive ? "Active" : "Inactive";
            hlStatus.LabelType = CurrentPhoneNumber.IsActive ? LabelType.Success : LabelType.Default;
            hlStatus.Visible = true;


        }


        #endregion
    }
}
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

using org.secc.Communication;
using org.secc.Communication.Messaging.Model;
using Rock;
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
                if(CurrentPhoneNumber == null)
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
                if(CurrentPhoneNumber == null)
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
            this.AddConfigurationUpdateTrigger( upPhoneDetail );




        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            NotificationBoxClear();
            LoadPanelDrawerScript();
            if ( !Page.IsPostBack )
            {
                if(PageParameter("IsSave").AsBoolean())
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
            if ( hfMessagingPhoneId.Value.AsGuid() == Guid.Empty)
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
            phone.IsActive = cbActive.Checked;
            phone.ModifiedBy = new MessagingPerson( CurrentPerson );

            if(phone.Id.IsEmpty())
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


        #endregion

        #region Methods

        private void ClearDetailPanel()
        {
            tbPhoneNumberName.Text = string.Empty;
            lPhoneNumber.Text = string.Empty;
            hlStatus.Text = string.Empty;
            hlStatus.Visible = false;
            tbDescription.Text = string.Empty;
            cbActive.Checked = false;
        }

        private string GetAuditLink(MessagingPerson p, DateTime? actionDateUtc)
        {
            var stringBuilder = new StringBuilder();
            var internalUrl = GlobalAttributesCache.Value( "InternalApplicationRoot" );

            if(p != null)
            {
                var rockPerson = new PersonAliasService( new Rock.Data.RockContext() ).Get( p.AliasGuid ).Person;
                stringBuilder.AppendFormat( "<a href='{0}Person/{1}'>{2}</a>", internalUrl,  rockPerson.Id, rockPerson.FullName );
            }

            if(actionDateUtc.HasValue)
            {
                var localTime = actionDateUtc.Value.ToLocalTime();
                stringBuilder.AppendFormat( " <small class='js-date-rollover' data-toggle='tooltip' data-placement='top' title='{0}'>({1})</small>",
                    localTime.ToString(), localTime.ToRelativeDateString() );
            }
            return stringBuilder.ToString();
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
                if(tn.PhoneNumber.StartsWith("1"))
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

        private void NotificationBoxSet(string title, string text, NotificationBoxType boxType)
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
                hlStatus.LabelType = CurrentPhoneNumber.IsActive ? LabelType.Success : LabelType.Warning;
                hlStatus.Visible = true;

                tbPhoneNumberName.Text = CurrentPhoneNumber.Name;
                tbDescription.Text = CurrentPhoneNumber.Description;
                ddlPhoneNumber.Visible = false;
                lPhoneNumber.Visible = true;
                lPhoneNumber.Text = CurrentPhoneNumber.NumberFormatted;
                cbActive.Checked = CurrentPhoneNumber.IsActive;

            }


        }

        private void ShowDetailView()
        {
            pnlViewDetails.Visible = true;
            pnlEditDetails.Visible = false;
            pnlAuditDrawer.Visible = true;

            lTitle.Text = $"Phone Number - {CurrentPhoneNumber.Name}";
            lPhoneNumberView.Text = CurrentPhoneNumber.NumberFormatted;
            lDescription.Text = CurrentPhoneNumber.Description;
            hlStatus.Text = CurrentPhoneNumber.IsActive ? "Active" : "Inactive";
            hlStatus.LabelType = CurrentPhoneNumber.IsActive ? LabelType.Success : LabelType.Warning;
            hlStatus.Visible = true;


        }


        #endregion
    }
}
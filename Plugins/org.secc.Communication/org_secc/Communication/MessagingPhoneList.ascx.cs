using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using org.secc.Communication;
using org.secc.Communication.Messaging.Model;
using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;


namespace RockWeb.Plugins.org_secc.Communication
{
    [DisplayName( "Messaging Phone Numbers" )]
    [Category( "SECC > Communication" )]
    [Description( "List of active phone numbers from the SECC Messaging Service." )]

    [LinkedPage( "Detail Page",
        Description = "The Phone Number detail Page.",
        IsRequired = false,
        Order = 0,
        Key = AttributeKeys.DetailPage )]
    public partial class MessagingPhoneList : RockBlock
    {
        public static class AttributeKeys
        {
            public const string DetailPage = "DetailPage";
        }

        List<TwilioNumber> twilioNumbers = new List<TwilioNumber>();

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            gPhoneNumbers.Actions.ShowAdd = UserCanEdit;
            gPhoneNumbers.IsDeleteEnabled = UserCanEdit;
            gPhoneNumbers.Actions.AddClick += gPhoneNumbers_AddClick;
            gPhoneNumbers.GridRebind += GPhoneNumbers_GridRebind;
            mdlAddTwilioNumber.SaveClick += MdlAddTwilioNumber_SaveClick;
            gPhoneNumbers.Columns[gPhoneNumbers.Columns.Count - 1].Visible = UserCanEdit;
        }



        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            SetNotificationBox( string.Empty, string.Empty );
            if ( !IsPostBack )
            {
                LoadTwilioNumbers();
                LoadPhoneNumberList();
            }
        }

        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );
            twilioNumbers = ViewState[$"{this.BlockId}_TwilioNumbers"] as List<TwilioNumber>;
        }

        protected override object SaveViewState()
        {
            ViewState[$"{this.BlockId}_TwilioNumbers"] = twilioNumbers;
            return base.SaveViewState();
        }


        private void gPhoneNumbers_AddClick( object sender, EventArgs e )
        {
            NavigateToLinkedPage( AttributeKeys.DetailPage );

            //ClearAddModal();
            //BindTwilioNumberList();
            //hPhonenumberId.Value = string.Empty;
            //ddlTwilioNumbers.Visible = true;
            //lPhone.Visible = false;
            //pnlMdlHistory.Visible = false;

            //mdlAddTwilioNumber.SaveButtonText = "Save";
            //mdlAddTwilioNumber.SaveButtonCausesValidation = true;
            //mdlAddTwilioNumber.CancelLinkVisible = true;

            //mdlAddTwilioNumber.Title = "Add Phone Number";
            //mdlAddTwilioNumber.Show();


        }

        private void GPhoneNumbers_GridRebind( object sender, Rock.Web.UI.Controls.GridRebindEventArgs e )
        {
            LoadPhoneNumberList();
        }

        protected void gPhoneNumbers_RowSelected( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            if ( !UserCanEdit )
            {
                return;
            }

            var phoneId = e.RowKeyValue.ToString();
            var queryStringValues = new Dictionary<string, string>();
            queryStringValues.Add( "MessagingNumber", phoneId );
            NavigateToLinkedPage( AttributeKeys.DetailPage, queryStringValues );
        }

        protected void gPhoneNumber_Delete( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            if ( !UserCanEdit )
            {
                return;
            }
            var client = new MessagingClient();
            var key = e.RowKeyValue.ToString();
            var phoneNumber = client.GetPhoneNumber( key );

            if ( phoneNumber.ActiveKeywordCount > 0 )
            {
                SetNotificationBox( "Can not delete phone number.", "This phone number has active keywords, and can not be deleted." );
                nbNotifications.NotificationBoxType = NotificationBoxType.Danger;
                nbNotifications.Visible = true;
                return;
            }

            client.DeletePhoneNumber( phoneNumber.Id.ToString() );
            LoadPhoneNumberList();

        }

        private void MdlAddTwilioNumber_SaveClick( object sender, EventArgs e )
        {
            var messagingClient = new MessagingClient();
            bool isNew = true;
            MessagingPhoneNumber number = null;
            if ( hPhonenumberId.Value.IsNotNullOrWhiteSpace() )
            {
                isNew = false;

                number = messagingClient.GetPhoneNumber( hPhonenumberId.Value );
            }
            else
            {
                number = new MessagingPhoneNumber();
                number.Sid = ddlTwilioNumbers.SelectedValue;
                number.Number = twilioNumbers.FirstOrDefault( t => t.Sid.Equals( number.Sid ) ).NumberRaw;
                number.CreatedBy = new MessagingPerson( CurrentPerson );
            }
            number.Name = tbName.Text.Trim();
            number.IsActive = cbActive.Checked;
            number.ModifiedBy = new MessagingPerson( CurrentPerson );

            if ( isNew )
            {
                messagingClient.AddPhoneNumber( number );
            }
            else
            {
                messagingClient.UpdatePhoneNumber( number );
            }
            mdlAddTwilioNumber.Hide();

            LoadPhoneNumberList();


        }

        protected void viewKeywords_Click( object sender, RowEventArgs e )
        {
            var phoneId = e.RowKeyValue.ToString();
            var queryStrings = new Dictionary<string, string>
            {
                { "MessagingNumber", phoneId }
            };
            NavigateToLinkedPage( AttributeKeys.DetailPage, queryStrings );
        }

        private void BindTwilioNumberList()
        {
            ddlTwilioNumbers.Items.Clear();

            var messagingClient = new MessagingClient();
            var numbers = messagingClient.GetPhoneNumbers();

            var availableTwilioNumbers = twilioNumbers
                .Where( t => !numbers.Select( n => n.Sid ).Contains( t.Sid ) )
                .OrderBy( t => t.Name )
                .ToList();

            ddlTwilioNumbers.DataSource = availableTwilioNumbers;
            ddlTwilioNumbers.DataValueField = "Sid";
            ddlTwilioNumbers.DataTextField = "DisplayName";
            ddlTwilioNumbers.DataBind();

        }

        private void ClearAddModal()
        {
            hPhonenumberId.Value = String.Empty;
            ddlTwilioNumbers.Items.Clear();
            lPhone.Text = String.Empty;
            tbName.Text = String.Empty;
            cbActive.Checked = false;

        }


        private void LoadTwilioNumbers()
        {
            twilioNumbers.Clear();

            var messagingClient = new MessagingClient();
            var twilioResults = messagingClient.GetTwilioNumbers();


            foreach ( var item in twilioResults )
            {
                var number = new TwilioNumber
                {
                    Sid = item.Sid,
                    Name = item.FriendlyName,
                    Number = PhoneNumber.CleanNumber( item.PhoneNumber ),
                    NumberRaw= item.PhoneNumber
                };
                twilioNumbers.Add( number );
            }
        }

        private void LoadPhoneEditModel( string key )
        {
            if ( key.IsNullOrWhiteSpace() )
            {
                return;
            }
            ClearAddModal();
            var phoneId = key;
            var client = new MessagingClient();
            var phonenumber = client.GetPhoneNumber( key );

            ddlTwilioNumbers.Visible = false;
            lPhone.Visible = true;
            hPhonenumberId.Value = phonenumber.Id.ToString();
            lPhone.Text = Rock.Model.PhoneNumber.FormattedNumber( "1", phonenumber.Number.Replace( "+1", String.Empty ), false );
            tbName.Text = phonenumber.Name;
            cbActive.Checked = phonenumber.IsActive;
            pnlMdlHistory.Visible = true;

            if ( phonenumber.CreatedBy == null )
            {
                lCreatedBy.Text = "(unknown) " + ( phonenumber.CreatedOnDateTime.HasValue ? phonenumber.CreatedOnDateTime?.ToShortDateTimeString() : String.Empty );
            }
            else
            {
                lCreatedBy.Text = phonenumber.CreatedBy.ToString() + " " + ( phonenumber.CreatedOnDateTime.HasValue ? phonenumber.CreatedOnDateTime?.ToLocalTime().ToShortDateTimeString() : String.Empty );
            }

            if( phonenumber.ModifiedBy == null)
            {
                lModifiedBy.Text = "(unknown) " + ( phonenumber.CreatedOnDateTime.HasValue ? phonenumber.CreatedOnDateTime?.ToShortDateTimeString() : string.Empty );
            }
            else
            {
                lModifiedBy.Text = phonenumber.ModifiedBy.ToString() + " " + ( phonenumber.ModifiedOnDateTime.HasValue ? phonenumber.ModifiedOnDateTime?.ToLocalTime().ToShortDateTimeString() : String.Empty );
            }

            mdlAddTwilioNumber.Title = "Edit Phone Number";
            mdlAddTwilioNumber.Show();

        }

        private void LoadPhoneNumberList()
        {
            var messagingClient = new MessagingClient();
            var phoneNumbers = messagingClient.GetPhoneNumbers();
            gPhoneNumbers.DataSource = phoneNumbers;
            gPhoneNumbers.DataBind();
        }

        public void SetNotificationBox( string title, string message )
        {
            if ( message.IsNullOrWhiteSpace() && title.IsNullOrWhiteSpace() )
            {
                nbNotifications.Visible = false;
            }
            nbNotifications.Title = title;
            nbNotifications.Text = message;
        }

        [Serializable]
        public class TwilioNumber
        {
            public string Sid { get; set; }
            public string Name { get; set; }
            public string Number { get; set; }
            public string NumberRaw { get; set; }
            public string DisplayName
            {
                get
                {
                    return $"{Name} - {Number}";
                }
            }
        }



    }


}
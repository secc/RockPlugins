using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using MassTransit.Initializers;
using org.secc.Communication;
using org.secc.Communication.Messaging.Model;
using Rock;
using Rock.Model;
using Rock.Web.UI;


namespace RockWeb.Plugins.org_secc.Communication
{
    [DisplayName( "Messaging Phone Numbers" )]
    [Category( "SECC > Communication" )]
    [Description( "List of active phone numbers from the SECC Messaging Service." )]
    public partial class MessagingPhoneList : RockBlock
    {
        List<TwilioNumber> twilioNumbers = new List<TwilioNumber>();

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            gPhoneNumbers.Actions.ShowAdd = IsUserAuthorized( Rock.Security.Authorization.EDIT );
            gPhoneNumbers.Actions.AddClick += Actions_AddClick;
            mdlAddTwilioNumber.SaveClick += MdlAddTwilioNumber_SaveClick;
        }


        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

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


        private void Actions_AddClick( object sender, EventArgs e )
        {
            BindTwilioNumberList();
            hPhonenumberId.Value = string.Empty;
            lPhone.Visible = false;
            mdlAddTwilioNumber.SaveButtonText = "Save";
            mdlAddTwilioNumber.SaveButtonCausesValidation = true;
            mdlAddTwilioNumber.CancelLinkVisible = true;

            mdlAddTwilioNumber.Show();


        }

        private void MdlAddTwilioNumber_SaveClick( object sender, EventArgs e )
        {
            var messagingClient = new MessagingClient();
            bool isNew = true;
            MessagingPhoneNumber number = null;
            if ( hPhonenumberId.Value.IsNotNullOrWhiteSpace() )
            {
                isNew = false;

                number = Task.Run( async () => await messagingClient.GetPhoneNumber( hPhonenumberId.Value ) ).Result;
            }
            else
            {
                number = new MessagingPhoneNumber();
                number.Sid = ddlTwilioNumbers.SelectedValue;
                number.Number = twilioNumbers.FirstOrDefault( t => t.Sid.Equals( number.Sid ) ).Number;
            }
            number.Name = tbName.Text.Trim();
            number.IsActive = cbActive.Checked;

            if ( isNew )
            {
                Task.Run( async () => await messagingClient.AddPhoneNumber( number ) );
            }
            else
            {
                Task.Run( async () => await messagingClient.UpdatePhoneNumber( number ) );
            }
            mdlAddTwilioNumber.Hide();
            LoadPhoneNumberList();


        }

        private void BindTwilioNumberList()
        {
            ddlTwilioNumbers.Items.Clear();

            var messagingClient = new MessagingClient();
            var numbers = Task.Run( async () => await messagingClient.GetPhoneNumbers() ).Result;

            var availableTwilioNumbers = twilioNumbers
                .Where( t => !numbers.Select( n => n.Sid ).Contains( t.Sid ) )
                .OrderBy( t => t.Name )
                .ToList();

            ddlTwilioNumbers.DataSource = availableTwilioNumbers;
            ddlTwilioNumbers.DataValueField = "Sid";
            ddlTwilioNumbers.DataTextField = "DisplayName";
            ddlTwilioNumbers.DataBind();





        }

        private void LoadTwilioNumbers()
        {
            twilioNumbers.Clear();

            var messagingClient = new MessagingClient();
            var twilioResults = Task.Run( async () => await messagingClient.GetTwilioNumbers() ).Result;


            foreach ( var item in twilioResults )
            {
                var number = new TwilioNumber
                {
                    Sid = item.Sid,
                    Name = item.FriendlyName,
                    Number = PhoneNumber.CleanNumber( item.PhoneNumber )
                };
                twilioNumbers.Add( number );
            }
        }

        private void LoadPhoneNumberList()
        {
            var messagingClient = new MessagingClient();
            var phoneNumbers = Task.Run( async () => await messagingClient.GetPhoneNumbers() ).Result;
            gPhoneNumbers.DataSource = phoneNumbers;
            gPhoneNumbers.DataBind();
        }

        [Serializable]
        public class TwilioNumber
        {
            public string Sid { get; set; }
            public string Name { get; set; }
            public string Number { get; set; }
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
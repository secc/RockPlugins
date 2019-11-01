// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
using System;
using System.ComponentModel;
using Rock;
using Rock.Model;
using System.Web.UI;
using Rock.Web.Cache;
using Rock.Data;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using org.secc.ChangeManager.Model;
using Rock.Attribute;
using org.secc.RecurringCommunications.Model;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.RecurringCommunications
{
    [DisplayName( "Recurring Communications Detail" )]
    [Category( "SECC > Communication" )]
    [Description( "Block for the creation and editing of recurring communications." )]
    public partial class RecurringCommunicationsDetail : Rock.Web.UI.RockBlock
    {
        internal class PageParameterKey
        {
            public static string RecurringCommunicationId = "RecurringCommunicationId";
        }

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            var personEntityId = EntityTypeCache.Get( typeof( Person ) ).Id;
            dvpDataview.EntityTypeId = personEntityId;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                rblCommunicationType.BindToEnum<CommunicationFocus>();
                ddlPhoneNumber.BindToDefinedType( DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.COMMUNICATION_SMS_FROM ), true );
                DisplayDetails();
            }
        }

        private void DisplayDetails()
        {
            RecurringCommunication recurringCommunication = GetRecurringCommunication();


            tbName.Text = recurringCommunication.Name;
            dvpDataview.SetValue( recurringCommunication.DataView );
            if ( recurringCommunication.Schedule != null )
            {
                sbScheduleBuilder.iCalendarContent = recurringCommunication.Schedule.iCalendarContent;
                DisplayScheduleDetails();
            }
            rblCommunicationType.SetValue( recurringCommunication.CommunicationType.ConvertToInt() );
            UpdateCommunicationTypeUI( recurringCommunication.CommunicationType );

            tbFromName.Text = recurringCommunication.FromName;
            tbFromEmail.Text = recurringCommunication.FromEmail;
            tbSubject.Text = recurringCommunication.Subject;
            ceEmailBody.Text = recurringCommunication.EmailBody;

            ddlPhoneNumber.SelectedValue = recurringCommunication.PhoneNumberValueId.ToString();
            tbSMSBody.Text = recurringCommunication.SMSBody;
        }

        private void DisplayScheduleDetails()
        {
            var iCal = sbScheduleBuilder.iCalendarContent;
            if ( iCal.IsNullOrWhiteSpace() )
            {
                lScheduleDescription.Visible = false;
                return;
            }

            lScheduleDescription.Visible = true;
            var schedule = new Schedule();
            schedule.iCalendarContent = sbScheduleBuilder.iCalendarContent;
            lScheduleDescription.Text = schedule.ToFriendlyScheduleText();
        }

        private void UpdateCommunicationTypeUI( CommunicationType communicationType )
        {
            switch ( communicationType )
            {
                case CommunicationType.RecipientPreference:
                    DisplaySMS( true );
                    DisplayEMail( true );
                    break;
                case CommunicationType.Email:
                    DisplayEMail( true );
                    DisplaySMS( false );
                    break;
                case CommunicationType.SMS:
                    DisplaySMS( true );
                    DisplayEMail( false );
                    break;
                default:
                    break;
            }
        }

        private void DisplayEMail( bool shouldDisplay )
        {
            pnlEmail.Visible = shouldDisplay;
            tbFromEmail.Required = shouldDisplay;
            tbFromName.Required = shouldDisplay;
            tbSubject.Required = shouldDisplay;
            ceEmailBody.Required = shouldDisplay;
        }

        private void DisplaySMS( bool shouldDisplay )
        {
            pnlSMS.Visible = shouldDisplay;
            ddlPhoneNumber.Required = shouldDisplay;
            tbSMSBody.Required = shouldDisplay;
        }

        private RecurringCommunication GetRecurringCommunication()
        {
            return GetRecurringCommunication( new RecurringCommunicationService( new RockContext() ) );
        }

        private RecurringCommunication GetRecurringCommunication( RecurringCommunicationService recurringCommunicationService )
        {
            var rucurringCommunicationId = PageParameter( PageParameterKey.RecurringCommunicationId ).AsInteger();
            var recurringCommunication = recurringCommunicationService.Get( rucurringCommunicationId );
            if ( recurringCommunication == null )
            {
                recurringCommunication = new RecurringCommunication
                {
                    CommunicationType = CommunicationType.Email,
                    EmailBody = "{{ 'Global' | Attribute:'EmailHeader' }}\n<br>\n<br>\n{{ 'Global' | Attribute:'EmailFooter' }}",
                };
            }
            return recurringCommunication;
        }

        protected void btnCancel_Click( object sender, EventArgs e )
        {
            NavigateToParentPage();
        }

        protected void btnSave_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();
            RecurringCommunicationService recurringCommunicationService = new RecurringCommunicationService( rockContext );
            var recurringCommunication = GetRecurringCommunication( recurringCommunicationService );

            if ( recurringCommunication.Schedule == null )
            {
                recurringCommunication.Schedule = new Schedule();
                ScheduleService scheduleService = new ScheduleService( rockContext );
                scheduleService.Add( recurringCommunication.Schedule );
            }
            recurringCommunication.Name = tbName.Text;
            recurringCommunication.DataViewId = dvpDataview.SelectedValue.AsInteger();
            recurringCommunication.Schedule.iCalendarContent = sbScheduleBuilder.iCalendarContent;
            recurringCommunication.CommunicationType = rblCommunicationType.SelectedValueAsEnum<CommunicationType>();
            recurringCommunication.FromName = tbFromName.Text;
            recurringCommunication.FromEmail = tbFromEmail.Text;
            recurringCommunication.Subject = tbSubject.Text;
            recurringCommunication.EmailBody = ceEmailBody.Text;
            recurringCommunication.PhoneNumberValueId = ddlPhoneNumber.SelectedValue.AsIntegerOrNull();
            recurringCommunication.SMSBody = tbSMSBody.Text;

            if ( recurringCommunication.Id == 0 )
            {
                recurringCommunicationService.Add( recurringCommunication );
            }
            rockContext.SaveChanges();
            NavigateToParentPage();
        }

        protected void rblCommunicationType_SelectedIndexChanged( object sender, EventArgs e )
        {
            UpdateCommunicationTypeUI( rblCommunicationType.SelectedValueAsEnum<CommunicationType>() );
        }

        protected void sbScheduleBuilder_SaveSchedule( object sender, EventArgs e )
        {
            DisplayScheduleDetails();
        }
    }

    public enum CommunicationFocus
    {
        RecipientPreference = 0,
        Email = 1,
        SMS = 2,
    }
}
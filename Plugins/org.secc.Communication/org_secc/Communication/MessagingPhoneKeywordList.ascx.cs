using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Linq.Dynamic;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using org.secc.Communication;
using org.secc.Communication.Messaging;
using org.secc.Communication.Messaging.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;


namespace RockWeb.Plugins.org_secc.Communication
{
    [DisplayName( "Messaging Phone Number Keywords" )]
    [Category( "SECC > Communication" )]
    [Description( "List of keywords that are associated with a messaging phone number." )]

    [BooleanField( "Show Filter",
        Description = "Show Keyword Filter options",
        DefaultBooleanValue = true,
        Order = 2,
        Key = AttributeKeys.ShowFilter )]
    [BooleanField("Enforce Response Limit",
        Description = "Enforce the character limit for SMS Responses.",
        DefaultBooleanValue = true,
        Order = 3,
        Key = AttributeKeys.EnforceResponseLimit)]
    public partial class MessagingPhoneKeywordList : RockBlock
    {
        public static class AttributeKeys
        {
            public const string ShowFilter = "ShowFilter";
            public const string EnforceResponseLimit = "EnforceResponseLimit";
            public const string QS_MessagingNumber = "MessagingNumber";
        }

        #region Base Control Methods
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            this.BlockUpdated += MessagingPhoneKeywordList_BlockUpdated;
            gKeywords.ItemType = "Keywords";
            gKeywords.EmptyDataText = "No Keywords Found";
            gKeywords.Actions.ShowAdd = UserCanEdit;
            gKeywords.Actions.ShowBulkUpdate = false;
            gKeywords.Actions.ShowMergePerson = false;
            gKeywords.Actions.ShowCommunicate = false;
            gKeywords.Actions.ShowMergeTemplate = false;
            gKeywords.Actions.ShowExcelExport = false;
            gKeywords.GridRebind += gKeywords_GridRebind;
            gKeywords.GridReorder += gKeywords_GridReorder;
            gKeywords.Actions.AddClick += gKeywords_AddClick;
            gKeywords.RowSelected += gKeywords_RowSelected;
            gKeywords.RowDataBound += GKeywords_RowDataBound;
            gfKeywords.ApplyFilterClick += gfKeywords_ApplyFilterClick;
            gfKeywords.ClearFilterClick += gfKeywords_ClearFilterClick;
            gfKeywords.DisplayFilterValue += gfKeywords_DisplayFilterValue;
            lbKeywordCancel.Click += lbKeywordCancel_Click;
            lbKeywordSave.Click += lbKeywordSave_Click;

            if(!GetAttributeValue(AttributeKeys.EnforceResponseLimit).AsBoolean())
            {
                tbResponseMessage.MaxLength = 0;
            }


            gKeywords.Columns[gKeywords.Columns.Count - 1].Visible = UserCanEdit;

        }





        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            NotificationBoxClear();

            if ( !IsPostBack )
            {
                hfPhoneNumberId.Value = PageParameter( AttributeKeys.QS_MessagingNumber );
                if ( hfPhoneNumberId.Value.IsNotNullOrWhiteSpace() )
                {
                    BindFilter();
                    LoadPhoneNumberKeywords();
                    pnlContent.Visible = true;
                }
                else
                {
                    pnlContent.Visible = false;
                }
            }
        }
        #endregion

        #region Events

        private void MessagingPhoneKeywordList_BlockUpdated( object sender, EventArgs e )
        {
            if ( hfPhoneNumberId.Value.IsNotNullOrWhiteSpace() )
            {
                BindFilter();
                LoadPhoneNumberKeywords();
                pnlContent.Visible = true;
            }
            else
            {
                pnlContent.Visible = false;
            }
        }

        private void gKeywords_AddClick( object sender, EventArgs e )
        {
            KeywordFormLoad( string.Empty );
        }

        protected void gKeywords_DeleteClick( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            if ( !UserCanEdit )
            {
                return;
            }
            var keywordId = e.RowKeyValue.ToString();
            var client = new MessagingClient();
            client.DeleteKeyword( hfPhoneNumberId.Value, keywordId );
            LoadPhoneNumberKeywords();
            NotificationBoxSetContent(
                "<i class='fas fa-trash'></i> Keyword Deleted",
                "Keyword has been successfully deleted.", NotificationBoxType.Success );

        }

        private void gKeywords_GridRebind( object sender, GridRebindEventArgs e )
        {
            LoadPhoneNumberKeywords();
        }

        private void gKeywords_GridReorder( object sender, GridReorderEventArgs e )
        {
            var reorderItem = new KeywordReorderItem
            {
                KeywordId = e.DataKey.ToString().AsGuid(),
                OldIndex = e.OldIndex,
                NewIndex = e.NewIndex
            };
            var client = new MessagingClient();
            client.ReorderKeyword( reorderItem, hfPhoneNumberId.Value );
            LoadPhoneNumberKeywords();


        }

        private void GKeywords_RowDataBound( object sender, System.Web.UI.WebControls.GridViewRowEventArgs e )
        {
            if ( e.Row.RowType != DataControlRowType.DataRow )
            {
                return;
            }

            var item = e.Row.DataItem as KeywordSummary;

            Literal lStatus = e.Row.FindControl( "lStatus" ) as Literal;

            var labelText = String.Empty;
            var cssClass = string.Empty;

            switch ( item.Status )
            {
                case KeywordStatus.Inactive:
                    labelText = "Inactive";
                    cssClass = "label label-default";
                    break;
                case KeywordStatus.Active:
                    labelText = "Active";
                    cssClass = "label label-success";
                    break;
                case KeywordStatus.PendingApproval:
                    labelText = "Pending Approval";
                    cssClass = "label label-warning";
                    break;
            }
            lStatus.Text = $"<span class='{cssClass}'>{labelText}</span>";

            var phrases = string.Join( "<br />", item.PhrasesToMatch );
            Literal lPhrases = e.Row.FindControl( "lKeywordPhrases" ) as Literal;
            lPhrases.Text = phrases;

        }

        private void gKeywords_RowSelected( object sender, RowEventArgs e )
        {
            var keywordId = e.RowKeyValue.ToString();
            KeywordFormLoad( keywordId );
        }

        private void gfKeywords_ApplyFilterClick( object sender, EventArgs e )
        {
            gfKeywords.SaveUserPreference( "Keyword", tbKeywordSearch.Text.Trim() );
            gfKeywords.SaveUserPreference( "Status", cblStatus.SelectedValues.AsDelimited( "," ) );
            LoadPhoneNumberKeywords();
        }

        private void gfKeywords_ClearFilterClick( object sender, EventArgs e )
        {
            gfKeywords.DeleteUserPreferences();
            tbKeywordSearch.Text = string.Empty;

            foreach ( ListItem li in cblStatus.Items )
            {
                li.Selected = false;
            }

            BindFilter();
        }


        private void gfKeywords_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            switch ( e.Key )
            {
                case "Status":
                    var statusValues = e.Value.SplitDelimitedValues().ToList();
                    var displayValue = string.Empty;
                    foreach ( var value in statusValues )
                    {
                        displayValue += cblStatus.Items.FindByValue( value ).Text + ", ";
                    }

                    if ( displayValue.IsNotNullOrWhiteSpace() )
                    {
                        e.Value = displayValue.Substring( 0, displayValue.LastIndexOf( "," ) ).Trim();
                    }
                    break;
            }
        }

        private void lbKeywordCancel_Click( object sender, EventArgs e )
        {
            KeywordFormClear();
            LoadPhoneNumberKeywords();
            pnlKeywordGrid.Visible = true;
            pnlKeywordEdit.Visible = false;
        }

        private void lbKeywordSave_Click( object sender, EventArgs e )
        {
            if ( !KeywordDateRangeIsValid() )
            {
                NotificationBoxSetContent( "Please correct the following:", "Start Date must be before End Date.", NotificationBoxType.Validation );
                return;
            }

            bool isNew = false;
            Keyword keyword = null;
            if ( hfKeywordId.Value.IsNotNullOrWhiteSpace() )
            {
                keyword = LoadKeyword( hfKeywordId.Value );
            }
            else
            {
                keyword = new Keyword();
                keyword.CreatedBy = new MessagingPerson( CurrentPerson );
                keyword.CreatedOnDateTime = RockDateTime.Now.ToUniversalTime();
                isNew = true;
            }

            keyword.Name = tbName.Text.Trim();
            keyword.Description = tbDescription.Text.Trim();

            if ( dpStart.SelectedDate.HasValue )
            {
                keyword.StartDate = dpStart.SelectedDate.Value.ToUniversalTime();
            }
            else
            {
                keyword.StartDate = dpStart.SelectedDate;
            }

            if ( dpEnd.SelectedDate.HasValue )
            {
                var ts = new TimeSpan( 23, 59, 59 );
                keyword.EndDate = dpEnd.SelectedDate.Value.Add( ts ).ToUniversalTime();
            }
            else
            {
                keyword.EndDate = dpEnd.SelectedDate;
            }

            if ( ppContact.PersonId.HasValue )
            {
                var contactPerson = new PersonService( new RockContext() ).Get( ppContact.PersonId.Value );
                keyword.ContactPerson = new MessagingPerson( contactPerson );
            }
            else
            {
                keyword.ContactPerson = null;
            }

            keyword.ModifiedBy = new MessagingPerson( CurrentPerson );
            keyword.ModifiedOnDateTime = RockDateTime.Now.ToUniversalTime();

            keyword.IsActive = switchActive.Checked;

            var wordsToMatch = new List<string>();
            var listItems = JsonConvert.DeserializeObject<List<ListItems.KeyValuePair>>( listPhrasesToMatch.Value );
            keyword.PhrasesToMatch = listItems.Select( l => l.Value ).ToList();
            keyword.ResponseMessage = tbResponseMessage.Text.Trim();

            var client = new MessagingClient();
            if ( isNew )
            {
                client.AddKeyword( hfPhoneNumberId.Value, keyword );
            }
            else
            {
                client.UpdateKeyword( hfPhoneNumberId.Value, keyword );
            }


            pnlKeywordGrid.Visible = true;
            pnlKeywordEdit.Visible = false;
            LoadPhoneNumberKeywords();
        }


        #endregion

        #region Methods

        private void BindFilter()
        {
            var showFilter = GetAttributeValue( AttributeKeys.ShowFilter ).AsBoolean();
            gfKeywords.Visible = showFilter;

            if ( !showFilter )
                return;

            tbKeywordSearch.Text = gfKeywords.GetUserPreference( "Keyword" );

            var selectedStatus = gfKeywords.GetUserPreference( "Status" );
            if ( selectedStatus.IsNotNullOrWhiteSpace() )
            {
                foreach ( var status in selectedStatus.SplitDelimitedValues() )
                {
                    var item = cblStatus.Items.FindByValue( status );
                    item.Selected = true;
                }
            }

        }

        private Person GetContactPerson( MessagingPerson contact )
        {
            if ( contact == null )
            {
                return null;
            }

            var rockContext = new RockContext();
            return new PersonAliasService( rockContext )
                .Queryable().AsNoTracking()
                .Where( a => a.Guid == contact.AliasGuid )
                .Select( a => a.Person )
                .FirstOrDefault();
        }

        private void KeywordFormClear()
        {
            hfKeywordId.Value = string.Empty;
            tbName.Text = string.Empty;
            tbDescription.Text = string.Empty;
            dpStart.SelectedDate = null;
            dpEnd.SelectedDate = null;
            ppContact.SetValue( null );
            switchActive.Checked = false;
            listPhrasesToMatch.Value = string.Empty;
            tbResponseMessage.Text = string.Empty;
        }

        private Keyword LoadKeyword( string id )
        {
            var client = new MessagingClient();
            var keyword = client.GetKeyword( hfPhoneNumberId.Value, id );
            return keyword;
        }

        private void KeywordFormLoad( string keywordId )
        {
            Keyword keyword = null;
            if ( keywordId.IsNotNullOrWhiteSpace() )
            {
                keyword = LoadKeyword( keywordId );
            }
            KeywordFormClear();
            if ( keyword != null )
            {
                hfKeywordId.Value = keyword.Id.ToString();
                tbName.Text = keyword.Name;
                tbDescription.Text = keyword.Description;
                dpStart.SelectedDate = keyword.StartDate.HasValue ? keyword.StartDate.Value.ToLocalTime() : keyword.StartDate;
                dpEnd.SelectedDate = keyword.EndDate.HasValue ? keyword.EndDate.Value.ToLocalTime() : keyword.EndDate;
                switchActive.Checked = keyword.IsActive;
                ppContact.SetValue( GetContactPerson( keyword.ContactPerson ) );


                var listItems = new List<ListItems.KeyValuePair>();
                foreach ( var phrase in keyword.PhrasesToMatch )
                {
                    listItems.Add( new ListItems.KeyValuePair { Value = phrase } );
                }

                listPhrasesToMatch.Value = JsonConvert.SerializeObject( listItems );
                tbResponseMessage.Text = keyword.ResponseMessage;
            }

            pnlKeywordEdit.Visible = true;
            pnlKeywordGrid.Visible = false;

        }

        private void LoadPhoneNumberKeywords()
        {
            var phoneGuid = hfPhoneNumberId.Value;
            var client = new MessagingClient();
            var phoneNumber = client.GetPhoneNumber( phoneGuid );

            if ( phoneNumber == null )
            {
                return;
            }


            List<KeywordSummary> keywords = new List<KeywordSummary>();
            if ( phoneNumber.Keywords != null )
            {
                foreach ( var item in phoneNumber.Keywords )
                {
                    keywords.Add( new KeywordSummary
                    {
                        KeywordId = item.Id,
                        PhoneNumberId = item.PhoneNumberId,
                        Name = item.Name,
                        Description = item.Description,
                        ResponseMessage = item.ResponseMessage,
                        StartDate = item.StartDate.HasValue ? item.StartDate.Value.ToLocalTime() : item.StartDate,
                        EndDate = item.EndDate.HasValue ? item.EndDate.Value.ToLocalTime() : item.EndDate,
                        Order = item.Order ?? 0,
                        CreatedOn = item.CreatedOnDateTime,
                        ModifiedOn = item.ModifiedOnDateTime,
                        IsActive = item.IsActive,
                        PhrasesToMatch = item.PhrasesToMatch
                    } );

                }
            }

            var keywordQry = keywords.AsQueryable();


            if ( GetAttributeValue( AttributeKeys.ShowFilter ).AsBoolean() )
            {
                if ( tbKeywordSearch.Text.IsNotNullOrWhiteSpace() )
                {
                    keywordQry = keywordQry.Where( k => k.PhrasesToMatch.Contains( tbKeywordSearch.Text.Trim() ) );
                }

                var selectedStatuses = new List<KeywordStatus>();
                foreach ( ListItem li in cblStatus.Items )
                {
                    if ( li.Selected )
                    {
                        selectedStatuses.Add( li.Value.ConvertToEnum<KeywordStatus>() );
                    }
                }

                if ( selectedStatuses.Count > 0 )
                {
                    keywordQry = keywordQry.Where( k => selectedStatuses.Contains( k.Status ) );
                }
            }

            gKeywords.DataSource = keywordQry.OrderBy( k => k.Order ).ToList();
            gKeywords.DataBind();


        }

        private bool KeywordDateRangeIsValid()
        {

            if ( !dpStart.SelectedDate.HasValue || !dpEnd.SelectedDate.HasValue )
            {
                return true;
            }

            if ( dpStart.SelectedDate.Value <= dpEnd.SelectedDate.Value )
            {
                return true;
            }

            return false;
        }



        private void NotificationBoxClear()
        {
            NotificationBoxSetContent( String.Empty, String.Empty, NotificationBoxType.Info );
        }

        private void NotificationBoxSetContent( string title, string message, NotificationBoxType boxType )
        {
            nbNotifications.Title = title;
            nbNotifications.Text = message;
            nbNotifications.NotificationBoxType = boxType;

            nbNotifications.Visible = title.IsNotNullOrWhiteSpace() || message.IsNotNullOrWhiteSpace();
        }
        #endregion

        protected class KeywordSummary
        {
            KeywordStatus? _status = null;

            public Guid KeywordId { get; set; }
            public Guid PhoneNumberId { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string MessageToMatch { get; set; }
            public string ResponseMessage { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public DateTime? CreatedOn { get; set; }
            public DateTime? ModifiedOn { get; set; }
            public bool IsActive { get; set; }
            public int PhraseCount
            {
                get
                {
                    return PhrasesToMatch.Count;
                }
            }
            public List<string> PhrasesToMatch { get; set; } = new List<string>();
            public int Order { get; set; }


            public KeywordStatus Status
            {
                get
                {
                    if ( _status.HasValue )
                    {
                        return _status.Value;
                    }

                    _status = GetStatus();

                    return _status.Value;
                }
            }

            private KeywordStatus GetStatus()
            {
                var status = KeywordStatus.Active;
                if ( !IsActive )
                {
                    status = KeywordStatus.Inactive;
                }
                else if ( StartDate.HasValue && StartDate.Value > RockDateTime.Now )
                {
                    status = KeywordStatus.Inactive;
                }
                else if ( EndDate.HasValue && EndDate.Value < RockDateTime.Now )
                {
                    status = KeywordStatus.Inactive;
                }

                return status;
            }

        }

        public enum KeywordStatus
        {
            Inactive = 0,
            Active = 1,
            PendingApproval = 2

        }




    }
}
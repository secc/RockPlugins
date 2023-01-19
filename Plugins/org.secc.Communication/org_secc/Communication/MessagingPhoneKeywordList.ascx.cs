using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using org.secc.Communication;
using org.secc.Communication.Messaging;
using org.secc.Communication.Messaging.Model;
using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using System.Linq.Dynamic;
using EntityFramework.Utilities;
using Rock.Jobs;

namespace RockWeb.Plugins.org_secc.Communication
{
    [DisplayName( "Messaging Phone Number Keywords" )]
    [Category( "SECC > Communication" )]
    [Description( "List of keywords that are associated with a messaging phone number." )]

    [BooleanField( "Show Inactive Keywords",
        Description = "Should inactive keywords be displayed",
        DefaultBooleanValue = true,
        Order = 1,
        Key = AttributeKeys.IncludeInactive )]
    [BooleanField( "Show Filter",
        Description = "Show Keyword Filter options",
        DefaultBooleanValue = true,
        Order = 2,
        Key = AttributeKeys.ShowFilter )]
    public partial class MessagingPhoneKeywordList : RockBlock
    {
        public static class AttributeKeys
        {
            public const string IncludeInactive = "IncludeInactive";
            public const string ShowFilter = "ShowFilter";
            public const string QS_MessagingNumber = "MessagingNumber";
        }

        #region Base Control Methods
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            gKeywords.DataKeyNames = new string[] { "KeywordId" };
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

            mdlEditKeyword.SaveClick += mdlEditKeyword_SaveClick;

        }



        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            NotificationBoxClear();

            if ( !IsPostBack )
            {
                hfPhoneNumberId.Value = PageParameter( AttributeKeys.QS_MessagingNumber );
                LoadPhoneNumberKeywords();
            }
        }
        #endregion

        #region Events

        private void gKeywords_AddClick( object sender, EventArgs e )
        {
            ClearKeywordModal();
            mdlEditKeyword.Title = "<i class='fas fa-comments'></i> Add Keyword";
            hfKeyword.Value = String.Empty;
            mdlEditKeyword.Show();
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


        }

        private void gKeywords_RowSelected( object sender, RowEventArgs e )
        {
            ClearKeywordModal();
            mdlEditKeyword.Title = "<i class='fas fa-comments'></i> Edit Keyword";
            var keyword = LoadKeyword( e.Row.DataItem.ToString() );
            

            if(keyword == null)
            {
                return;
            }

            hfKeyword.Value = keyword.Id.ToString();
            tbWord.Text = keyword.MessageToMatch;
            tbResponse.Text = keyword.ResponseMessage;
            dtpStartDate.SelectedDateTime = keyword.StartDate;
            dtpEndDate.SelectedDateTime = keyword.EndDate;
            cbActive.Checked = keyword.IsActive;

            pnlStatus.Visible = true;
            lCreatedOn.Text = ( keyword.CreatedBy != null ? keyword.CreatedBy.ToString() : "(unknown) " )
                + ( keyword.StartDate.HasValue ? keyword.StartDate.Value.ToShortDateTimeString() : String.Empty );
            lModifiedOn.Text = ( keyword.ModifiedBy != null ? keyword.ModifiedBy.ToString() : "(unknown) " )
                + ( keyword.ModifiedBy.HasValue ? keyword.ModifiedBy.Value.ToShortDateTimeString() : String.Empty );

            mdlEditKeyword.Show();
        }

        private void mdlEditKeyword_SaveClick( object sender, EventArgs e )
        {
            var phoneGuid = hfPhoneNumberId.Value.AsGuid();
            var keywordId = hfKeyword.Value;

            Keyword k = null;
            if( keywordId.IsNullOrWhiteSpace())
            {
                k = new Keyword();
            }
            else
            {
                k = LoadKeyword( keywordId );
            }
            pnlStatus.Visible = false;
        }
        #endregion

        #region Methods

        private void ClearKeywordModal()
        {
            mdlEditKeyword.Title = String.Empty;
            hfKeyword.Value = String.Empty;
            tbWord.Text = String.Empty;
            tbResponse.Text = String.Empty;
            dtpStartDate.SelectedDateTime = null;
            dtpEndDate.SelectedDateTime = null;
            cbActive.Checked = false;
        }

        private Keyword LoadKeyword(string id)
        {
            var client = new MessagingClient();
            var keyword = client.GetKeyword( hfPhoneNumberId.Value, id );
            return keyword;
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
            foreach ( var item in phoneNumber.Keywords )
            {
                keywords.Add( new KeywordSummary
                {
                    KeywordId = item.Value.Id,
                    PhoneNumberId = item.Value.PhoneNumberId,
                    MessageToMatch = item.Value.MessageToMatch,
                    ResponseMessage = item.Value.ResponseMessage,
                    StartDate = item.Value.StartDate,
                    EndDate = item.Value.EndDate,
                    Order = item.Key,
                    CreatedOn = item.Value.CreatedOnDateTime,
                    ModifiedOn = item.Value.ModifiedOnDateTime

                } );
            }

            gKeywords.DataSource = keywords.OrderBy( k => k.Order ).ToList();
            gKeywords.DataBind();   

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
            public Guid KeywordId { get; set; }
            public Guid PhoneNumberId { get; set; }
            public string MessageToMatch { get; set; }
            public string ResponseMessage { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public DateTime? CreatedOn { get; set; }
            public DateTime? ModifiedOn { get; set; }
            public string Status { get; set; }
            public int Order { get; set; }

        }
    }
}
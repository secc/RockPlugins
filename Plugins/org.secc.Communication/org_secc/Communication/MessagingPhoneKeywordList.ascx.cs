using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Dynamic;
using System.Web.UI.WebControls;
using Lucene.Net.Analysis.Miscellaneous;
using Newtonsoft.Json;
using org.secc.Communication;
using org.secc.Communication.Messaging;
using org.secc.Communication.Messaging.Model;
using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;


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
            lbKeywordCancel.Click += lbKeywordCancel_Click;
            lbKeywordSave.Click += lbKeywordSave_Click;


            gKeywords.Columns[gKeywords.Columns.Count - 1].Visible = UserCanEdit;

        }

        private void GKeywords_RowDataBound( object sender, System.Web.UI.WebControls.GridViewRowEventArgs e )
        {
            if ( e.Row.RowType != DataControlRowType.DataRow )
            {
                return;
            }

            var item = e.Row.DataItem as KeywordSummary;

            Literal lStatus = e.Row.FindControl( "lStatus" ) as Literal;

            lStatus.Text = item.Status;


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

        private void gKeywords_RowSelected( object sender, RowEventArgs e )
        {
            var keywordId = e.RowKeyValue.ToString();
            KeywordFormLoad( keywordId );
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
                keyword.EndDate = dpEnd.SelectedDate.Value.Add(ts).ToUniversalTime();
            }
            else
            {
                keyword.EndDate = dpEnd.SelectedDate;
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

        private void KeywordFormClear()
        {
            hfKeywordId.Value = string.Empty;
            tbName.Text = string.Empty;
            tbDescription.Text = string.Empty;
            dpStart.SelectedDate = null;
            dpEnd.SelectedDate = null;
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
                        KeywordId = item.Value.Id,
                        PhoneNumberId = item.Value.PhoneNumberId,
                        Name = item.Value.Name,
                        Description = item.Value.Description,
                        ResponseMessage = item.Value.ResponseMessage,
                        StartDate = item.Value.StartDate.HasValue ? item.Value.StartDate.Value.ToLocalTime() : item.Value.StartDate,
                        EndDate = item.Value.EndDate.HasValue ? item.Value.EndDate.Value.ToLocalTime() : item.Value.EndDate,
                        Order = item.Key,
                        CreatedOn = item.Value.CreatedOnDateTime,
                        ModifiedOn = item.Value.ModifiedOnDateTime,
                        PhraseCount = item.Value.PhrasesToMatch != null ? item.Value.PhrasesToMatch.Count() : 0,
                        Status = item.Value.IsActive ? "<span class='label label-success'>Active</span>" :
                            "<span class='label label-warning'>Inactive</span>"
                    } );
                    ;
                }
            }

            gKeywords.DataSource = keywords.OrderBy( k => k.Order ).ToList();
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
            public int PhraseCount { get; set; }
            public string Status { get; set; }
            public int Order { get; set; }

        }




    }
}
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
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using org.secc.SmsCapture.Model;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.SmsCapture
{
    [DisplayName( "SMS Capture Inbox" )]
    [Category( "SECC > Communication" )]
    [Description( "Papercut-style inbox for SMS messages captured by the SMS Capture transport. DEV/testing only." )]
    public partial class SmsCaptureInbox : RockBlock
    {
        #region Filter Keys

        private static class FilterKey
        {
            public const string DateRange = "Date Range";
            public const string ToNumber = "To Number";
            public const string Body = "Body";
        }

        #endregion

        #region Base Control Methods

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            gMessages.DataKeyNames = new string[] { "Id" };
            gMessages.Actions.ShowAdd = false;
            gMessages.IsDeleteEnabled = true;
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                BindFilter();
                BindGrid();
            }
        }

        #endregion

        #region Events

        protected void gfFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            gfFilter.SaveUserPreference( FilterKey.DateRange, drpDates.DelimitedValues );
            gfFilter.SaveUserPreference( FilterKey.ToNumber, tbToNumber.Text );
            gfFilter.SaveUserPreference( FilterKey.Body, tbBody.Text );
            BindGrid();
        }

        protected void gfFilter_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            switch ( e.Key )
            {
                case FilterKey.DateRange:
                    e.Value = DateRangePicker.FormatDelimitedValues( e.Value );
                    break;
                case FilterKey.ToNumber:
                case FilterKey.Body:
                    break;
                default:
                    e.Value = string.Empty;
                    break;
            }
        }

        protected void gfFilter_ClearFilterClick( object sender, EventArgs e )
        {
            gfFilter.DeleteUserPreferences();
            BindFilter();
            BindGrid();
        }

        protected void gMessages_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        protected void gMessages_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            var row = e.Row.DataItem as CapturedSmsRow;
            if ( row == null )
            {
                return;
            }

            var lPerson = e.Row.FindControl( "lPerson" ) as Literal;
            if ( lPerson != null && row.PersonId.HasValue )
            {
                lPerson.Text = string.Format( "<a href='{0}'>{1}</a>",
                    ResolveRockUrl( "~/Person/" + row.PersonId.Value ),
                    HttpUtility.HtmlEncode( row.PersonName ) );
            }

            var lCommunication = e.Row.FindControl( "lCommunication" ) as Literal;
            if ( lCommunication != null && row.CommunicationId.HasValue )
            {
                lCommunication.Text = string.Format( "<a href='{0}'>#{1}</a>",
                    ResolveRockUrl( "~/Communication/" + row.CommunicationId.Value ),
                    row.CommunicationId.Value );
            }
        }

        protected void gMessages_RowSelected( object sender, RowEventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                var capture = new CapturedSmsService( rockContext ).Get( e.RowKeyId );
                if ( capture == null )
                {
                    return;
                }

                lDetailFrom.Text = HttpUtility.HtmlEncode( capture.FromNumber );
                lDetailTo.Text = HttpUtility.HtmlEncode( capture.ToNumber );
                lDetailCaptured.Text = capture.CreatedDateTime.HasValue ? capture.CreatedDateTime.Value.ToString( "g" ) : string.Empty;
                lDetailSource.Text = HttpUtility.HtmlEncode( capture.Source );

                if ( capture.RecipientPersonAlias != null )
                {
                    lDetailPerson.Text = string.Format( "<a href='{0}'>{1}</a>",
                        ResolveRockUrl( "~/Person/" + capture.RecipientPersonAlias.PersonId ),
                        HttpUtility.HtmlEncode( capture.RecipientPersonAlias.Person.FullName ) );
                }
                else
                {
                    lDetailPerson.Text = string.Empty;
                }

                if ( capture.CommunicationId.HasValue )
                {
                    lDetailCommunication.Text = string.Format( "<a href='{0}'>#{1}</a>",
                        ResolveRockUrl( "~/Communication/" + capture.CommunicationId.Value ),
                        capture.CommunicationId.Value );
                }
                else
                {
                    lDetailCommunication.Text = string.Empty;
                }

                lDetailBody.Text = string.Format( "<pre style='white-space: pre-wrap;'>{0}</pre>", HttpUtility.HtmlEncode( capture.Body ) );

                var attachmentLinks = new List<string>();
                foreach ( var binaryFileId in ( capture.AttachmentBinaryFileIds ?? string.Empty ).SplitDelimitedValues().AsIntegerList() )
                {
                    attachmentLinks.Add( string.Format( "<a href='{0}' target='_blank'>File {1}</a>",
                        ResolveRockUrl( "~/GetFile.ashx?id=" + binaryFileId ),
                        binaryFileId ) );
                }

                lDetailAttachments.Text = attachmentLinks.Any() ? string.Join( "<br />", attachmentLinks ) : "<em>None</em>";
            }

            mdDetail.Show();
        }

        protected void gMessages_Delete( object sender, RowEventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                var capturedSmsService = new CapturedSmsService( rockContext );
                var capture = capturedSmsService.Get( e.RowKeyId );
                if ( capture != null )
                {
                    capturedSmsService.Delete( capture );
                    rockContext.SaveChanges();
                }
            }

            BindGrid();
        }

        protected void btnClearAll_Click( object sender, EventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                rockContext.Database.ExecuteSqlCommand( "DELETE FROM [dbo].[_org_secc_SmsCapture_CapturedSms]" );
            }

            BindGrid();
        }

        #endregion

        #region Methods

        private void BindFilter()
        {
            drpDates.DelimitedValues = gfFilter.GetUserPreference( FilterKey.DateRange );
            tbToNumber.Text = gfFilter.GetUserPreference( FilterKey.ToNumber );
            tbBody.Text = gfFilter.GetUserPreference( FilterKey.Body );
        }

        private void BindGrid()
        {
            using ( var rockContext = new RockContext() )
            {
                var qry = new CapturedSmsService( rockContext ).Queryable().AsNoTracking();

                var dateRange = DateRangePicker.CalculateDateRangeFromDelimitedValues( gfFilter.GetUserPreference( FilterKey.DateRange ) );
                if ( dateRange.Start.HasValue )
                {
                    qry = qry.Where( c => c.CreatedDateTime >= dateRange.Start.Value );
                }

                if ( dateRange.End.HasValue )
                {
                    qry = qry.Where( c => c.CreatedDateTime < dateRange.End.Value );
                }

                var toNumber = gfFilter.GetUserPreference( FilterKey.ToNumber );
                if ( toNumber.IsNotNullOrWhiteSpace() )
                {
                    qry = qry.Where( c => c.ToNumber.Contains( toNumber ) );
                }

                var body = gfFilter.GetUserPreference( FilterKey.Body );
                if ( body.IsNotNullOrWhiteSpace() )
                {
                    qry = qry.Where( c => c.Body.Contains( body ) );
                }

                var sortProperty = gMessages.SortProperty;
                if ( sortProperty != null )
                {
                    qry = qry.Sort( sortProperty );
                }
                else
                {
                    qry = qry.OrderByDescending( c => c.Id );
                }

                gMessages.DataSource = qry
                    .Select( c => new
                    {
                        c.Id,
                        c.CreatedDateTime,
                        c.FromNumber,
                        c.ToNumber,
                        c.Body,
                        c.Source,
                        c.CommunicationId,
                        PersonId = ( int? ) c.RecipientPersonAlias.PersonId,
                        PersonNickName = c.RecipientPersonAlias.Person.NickName,
                        PersonLastName = c.RecipientPersonAlias.Person.LastName
                    } )
                    .ToList()
                    .Select( c => new CapturedSmsRow
                    {
                        Id = c.Id,
                        CreatedDateTime = c.CreatedDateTime,
                        FromNumber = c.FromNumber,
                        ToNumber = c.ToNumber,
                        BodyPreview = c.Body.Truncate( 80 ),
                        Source = c.Source,
                        CommunicationId = c.CommunicationId,
                        PersonId = c.PersonId,
                        PersonName = string.Format( "{0} {1}", c.PersonNickName, c.PersonLastName ).Trim()
                    } )
                    .ToList();
                gMessages.DataBind();
            }
        }

        #endregion

        #region Helper Classes

        private class CapturedSmsRow
        {
            public int Id { get; set; }
            public DateTime? CreatedDateTime { get; set; }
            public string FromNumber { get; set; }
            public string ToNumber { get; set; }
            public string BodyPreview { get; set; }
            public string Source { get; set; }
            public int? CommunicationId { get; set; }
            public int? PersonId { get; set; }
            public string PersonName { get; set; }
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using org.secc.GroupManager.Exceptions;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace org.secc.GroupManager
{
    [LinkedPage( "Home Page", "Home page to send users to if they are not allowed to see this page.", false )]
    [GroupTypeField( "Filter Group Type", "Group Type which contains small group filtering information.", false )]
    [BooleanField( "Leaders Only", "Should leaders only be able to view this page?" )]
    public class GroupManagerBlock : RockBlock
    {
        public Group CurrentGroup { get; set; }
        public GroupMember CurrentGroupMember { get; set; }
        public List<string> CurrentGroupFilters { get; set; }
        public bool AllowLoadTheme = true;
        protected override void OnInit( EventArgs e )
        {
            RockContext rockContext = new RockContext();

            base.OnInit( e );
            //Reject non user
            if ( CurrentUser == null )
            {
                NavigateToHomePage();
                return;
            }

            LoadSession( rockContext );

            GroupType FilterGroupType = new GroupTypeService( rockContext ).Get( GetAttributeValue( "FilterGroupType" ).AsGuid() );
            if ( FilterGroupType != null
                && CurrentGroup.GroupType.ParentGroupTypes.Select( gt => gt.Id ).Contains( FilterGroupType.Id ) )
            {
                FilterGroupType.LoadAttributes();
                CurrentGroup.LoadAttributes();
                CurrentGroupFilters = CurrentGroup.GetAttributeValue(
                                FilterGroupType.GetAttributeValue( "FilterAttribute" )
                    )
                    .Split( new char[','], StringSplitOptions.RemoveEmptyEntries )
                    .ToList();
            }
            else
            {
                CurrentGroupFilters = new List<string>();
            }

        }

        private void LoadSession( RockContext rockContext )
        {
            int groupId = PageParameter( "GroupId" ).AsInteger();
            if ( groupId == 0 && Session["CurrentGroupManagerGroup"] != null )
            {
                groupId = ( int ) Session["CurrentGroupManagerGroup"];
            }

            CurrentGroup = new GroupService( rockContext ).Get( groupId );
            if ( CurrentGroup != null )
            {
                Session["CurrentGroupManagerGroup"] = groupId;
                CurrentGroupMember = CurrentGroup.Members
                    .Where( gm => gm.PersonId == CurrentUser.PersonId )
                    .FirstOrDefault();
                SetThemeCookie();
            }
            else
            {
                CurrentGroupMember = null;
                ClearThemeCookie();
            }

            if ( GetAttributeValue( "LeadersOnly" ).AsBoolean()
               && (
                    CurrentGroupMember == null
                    || !CurrentGroupMember.GroupRole.IsLeader )
               )
            {
                this.Visible = false;
                NavigateToHomePage();
            }
        }

        public void ClearThemeCookie()
        {
            if ( HttpContext.Current.Request.UrlReferrer != null
                && HttpContext.Current.Request.UrlReferrer.ToString() == Request.RawUrl )
            {
                return;
            }
            var httpContext = HttpContext.Current;
            if ( httpContext != null )
            {
                var request = httpContext.Request;
                if ( request != null )
                {
                    string cookieName = "Rock:Site:" + PageCache.Layout.SiteId.ToString() + ":theme";
                    HttpCookie cookie = request.Cookies[cookieName];
                    if ( cookie != null )
                    {
                        httpContext.Response.Cookies[cookieName].Expires = DateTime.Now.AddDays( -1 );
                        ReloadPage();
                    }
                }
            }
        }

        private void SetThemeCookie()
        {
            if ( !AllowLoadTheme || ( HttpContext.Current.Request.UrlReferrer != null
                && HttpContext.Current.Request.UrlReferrer.ToString() == Request.RawUrl ) )
            {
                return;
            }
            // Load custom theme
            CurrentGroup.LoadAttributes();
            if ( CurrentGroup.Attributes.ContainsKey( "Theme" ) )
            {
                var httpContext = HttpContext.Current;
                if ( httpContext != null )
                {
                    var request = httpContext.Request;
                    if ( request != null )
                    {
                        string cookieName = "Rock:Site:" + PageCache.Layout.SiteId.ToString() + ":theme";
                        HttpCookie cookie = request.Cookies[cookieName];
                        string theme = CurrentGroup.GetAttributeValue( "Theme" );
                        if ( !string.IsNullOrWhiteSpace( theme ) )
                        {
                            // Don't allow switching to an invalid theme
                            if ( System.IO.Directory.Exists( httpContext.Server.MapPath( "~/Themes/" + theme ) ) )
                            {
                                if ( cookie == null )
                                {
                                    cookie = new HttpCookie( cookieName, theme );
                                    httpContext.Response.SetCookie( cookie );
                                    ReloadPage();
                                }
                                else if ( theme != cookie.Value )
                                {
                                    cookie.Value = theme;
                                    httpContext.Response.SetCookie( cookie );
                                    ReloadPage();
                                }
                            }
                        }
                        else if ( cookie != null )
                        {
                            httpContext.Response.Cookies[cookieName].Expires = DateTime.Now.AddDays( -1 );
                            ReloadPage();
                        }
                    }
                }
            }
        }

        private void ReloadPage()
        {

            Response.Redirect( Request.RawUrl );

            Response.End();
        }

        protected virtual void NavigateToHomePage()
        {
            NavigateToLinkedPage( "HomePage" );
        }

        protected List<GroupMember> GetFilterdMembers( string Filter )
        {
            if ( !CurrentGroupFilters.Contains( Filter ) )
            {
                throw new NonExistantFilter( "The current group is not filterable by selected filter: " + Filter );
            }

            List<GroupMember> filteredMembers = new List<GroupMember>() { CurrentGroupMember };

            using ( RockContext rockContext = new RockContext() )
            {
                CurrentGroupMember.LoadAttributes( rockContext );
                var filterValue = CurrentGroupMember.GetAttributeValue( Filter );
                foreach ( var groupMember in CurrentGroup.Members )
                {
                    //This can be slow and may need upgraded
                    groupMember.LoadAttributes( rockContext );
                    if ( groupMember.GetAttributeValue( Filter ) == filterValue )
                    {
                        filteredMembers.Add( groupMember );
                    }
                }
            }
            return filteredMembers;
        }


    }
}

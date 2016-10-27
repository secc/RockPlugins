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
    [GroupTypeField( "GroupType", "If the current group is not of this group type. The page redirect home.", false )]
    [BooleanField( "Leaders Only", "Should leaders only be able to view this page?" )]
    public class GroupManagerBlock : RockBlock
    {
        public Group CurrentGroup { get; set; }
        public GroupMember CurrentGroupMember { get; set; }
        public List<string> CurrentGroupFilters { get; set; }
        public bool AllowLoadTheme = true;
        public List<string> CurrentGroupFilterValues
        {
            get
            {
                return GetCurrentGroupFilterValues();
            }
        }

        /// <summary>
        /// The current group members, filtered if applicable
        /// </summary>
        public List<GroupMember> CurrentGroupMembers
        {
            get
            {
                return GetFilteredMembers();
            }
        }

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

            GroupType FilterGroupType = new GroupTypeService( rockContext )
                .Get( GlobalAttributesCache.Read().GetValue( "FilterGroupType" ).AsGuid() );

            //If this group type inherits Filter Group
            if ( CurrentGroup != null && FilterGroupType != null
                && CurrentGroup.GroupType.InheritedGroupTypeId == FilterGroupType.Id )
            {
                CurrentGroup.GroupType.LoadAttributes();
                CurrentGroupFilters = CurrentGroup.GroupType.GetAttributeValue( "FilterAttribute" )
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
                if ( GetAttributeValue( "LeadersOnly" ).AsBoolean() && !CurrentGroupMember.GroupRole.IsLeader )
                {
                    NavigateToHomePage();
                    return;
                }
                SetThemeCookie();
            }
            else
            {
                CurrentGroupMember = null;
                NavigateToHomePage();
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
            CurrentGroup.GroupType.LoadAttributes();
            if ( CurrentGroup.GroupType.Attributes.ContainsKey( "Theme" ) )
            {
                var httpContext = HttpContext.Current;
                if ( httpContext != null )
                {
                    var request = httpContext.Request;
                    if ( request != null )
                    {
                        string cookieName = "Rock:Site:" + PageCache.Layout.SiteId.ToString() + ":theme";
                        HttpCookie cookie = request.Cookies[cookieName];
                        string theme = CurrentGroup.GroupType.GetAttributeValue( "Theme" );
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

        protected List<GroupMember> GetFilteredMembers()
        {
            if ( CurrentGroupFilters.Any() )
            {
                CurrentGroupMember.LoadAttributes();
                var filterAttribute = CurrentGroupFilters.FirstOrDefault();
                var filter = CurrentGroupMember.GetAttributeValue( filterAttribute );
                return GetFilteredMembers( filter );
            }
            else
            {
                return CurrentGroup.Members.ToList();
            }
        }

        public List<GroupMember> GetFilteredMembers( string filter )
        {
            List<GroupMember> members = new List<GroupMember>();
            if ( CurrentGroupFilters.Any() )
            {
                var filterAttribute = CurrentGroupFilters.FirstOrDefault();
                foreach ( var member in CurrentGroup.Members )
                {
                    member.LoadAttributes();
                    if ( member.GetAttributeValue( filterAttribute ) == filter )
                    {
                        members.Add( member );
                    }
                }
            }
            return members;
        }

        private List<string> GetCurrentGroupFilterValues()
        {
            var filterValues = new List<string>();
            if ( CurrentGroupFilters.Any() )
            {
                var filterAttribute = CurrentGroupFilters.FirstOrDefault();
                foreach ( var member in CurrentGroup.Members )
                {
                    member.LoadAttributes();
                    var filterValue = member.GetAttributeValue( filterAttribute );
                    if ( filterAttribute != null && !filterValues.Contains( filterValue ) )
                    {
                        filterValues.Add( filterValue );
                    }
                }
            }
            return filterValues;
        }
    }
}

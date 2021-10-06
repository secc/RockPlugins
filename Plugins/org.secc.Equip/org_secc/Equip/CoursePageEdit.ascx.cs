
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web.UI;
using org.secc.Equip.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web;
using org.secc.Equip;
using Rock.Web.UI.Controls;
using Rock.Web.UI;
using System.Web.UI.WebControls;

namespace RockWeb.Plugins.org.secc.Equip
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Course Page Edit" )]
    [Category( "SECC > Equip" )]
    [Description( "Block for editing course pages" )]
    public partial class CoursePageEdit : Rock.Web.UI.RockBlock
    {
        private List<Control> controls;

        public Guid? EntityGuid
        {
            get
            {
                if ( ViewState["EntityGuid"] != null && ViewState["EntityGuid"] is Guid )
                {
                    return ( Guid ) ViewState["EntityGuid"];
                }
                return null;
            }
            set
            {
                ViewState["EntityGuid"] = value;
            }
        }

        public int? PageComponentId
        {
            get
            {
                if ( ViewState["PageComponentId"] != null && ViewState["PageComponentId"] is int )
                {
                    return ( int ) ViewState["PageComponentId"];
                }
                return null;
            }
            set
            {
                ViewState["PageComponentId"] = value;
            }
        }

        #region PageParameterKeys

        /// <summary>
        /// Keys to use for Page Parameters
        /// </summary>
        protected static class PageParameterKey
        {
            internal const string ChapterId = "ChapterId";
            internal const string CoursePageId = "CoursePageId";
        }

        #endregion PageParameterKeys




        #region Base Control Methods

        //  overrides of the base RockBlock methods (i.e. OnInit, OnLoad)

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );
            BuildFields();
        }

        private void BuildFields()
        {
            var coursePage = GetCoursePage();
            CoursePageComponent coursePageComponent = GetCoursePageComponent( coursePage );

            if ( coursePageComponent == null )
            {
                return;
            }

            ltTitle.Text = coursePageComponent.Name;

            controls = coursePageComponent.DisplayConfiguration( phEdit );

            int i = 0;
            foreach ( var control in controls )
            {
                // make sure each qualifier control has a unique/predictable ID to help avoid viewstate issues
                var controlTypeName = control.GetType().Name;
                var oldControlId = control.ID ?? string.Empty;
                control.ID = string.Format( "qualifier_{0}_{1}", control.ID, i++ );

                // if this is a RockControl with a required field validator, make sure RequiredFieldValidator.ControlToValidate gets updated with the new control id
                if ( control is IRockControl )
                {
                    var rockControl = ( IRockControl ) control;
                    if ( rockControl.RequiredFieldValidator != null )
                    {
                        if ( rockControl.RequiredFieldValidator.ControlToValidate == oldControlId )
                        {
                            rockControl.RequiredFieldValidator.ControlToValidate = control.ID;
                        }
                    }
                }
            }
        }

        private CoursePageComponent GetCoursePageComponent( CoursePage coursePage )
        {
            if ( coursePage == null )
            {
                return null;
            }

            var component = ( CoursePageComponent ) CoursePageContainer.Instance.Dictionary
                .Where( c => c.Value.Value.EntityType.Id == coursePage.EntityTypeId )
                .Select( c => c.Value.Value )
                .FirstOrDefault();
            if ( component != null )
            {
                PageComponentId = component.TypeId;
                return component;
            }
            return null;
        }

        private CoursePage GetCoursePage()
        {
            RockContext rockContext = new RockContext();
            CoursePageService coursePageService = new CoursePageService( rockContext );
            return GetCoursePage( coursePageService );
        }

        private CoursePage GetCoursePage( CoursePageService coursePageService )
        {
            var coursePageId = PageParameter( PageParameterKey.CoursePageId ).AsIntegerOrNull();

            if ( !coursePageId.HasValue ) //if there is no page parameter for the page the user doesn't want it
            {
                return null;
            }

            var coursePage = coursePageService.Get( coursePageId.Value );
            if ( coursePage == null )
            {
                var chapterId = PageParameter( PageParameterKey.ChapterId ).AsInteger();
                var coursePages = coursePageService
                    .Queryable()
                    .Where( p => p.ChapterId == chapterId )
                    .OrderBy( cc => cc.Order ).ToList();

                int order = 0;
                if ( coursePages.Any() )
                {
                    order = coursePages.Last().Order + 1;
                }

                if ( !EntityGuid.HasValue )
                {
                    EntityGuid = Guid.NewGuid();
                }

                var pageCourseComponent = GetCoursePageComponent( coursePage );

                coursePage = new CoursePage
                {
                    ChapterId = chapterId,
                    EntityTypeId = PageComponentId ?? 0,
                    Guid = EntityGuid.Value,
                    Order = order,
                    Configuration = string.Empty
                };
            }
            return coursePage;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );


            if ( !Page.IsPostBack )
            {

                var coursePage = GetCoursePage();

                if ( coursePage == null )
                {
                    this.Visible = false;
                    return;
                }

                pdAuditDetails.SetEntity( coursePage, "/" );

                var component = GetCoursePageComponent( coursePage );

                if ( component == null )
                {
                    pnlEdit.Visible = false;
                    BindRepeater();
                }
                else
                {
                    pnlNew.Visible = false;
                    BuildFields();
                    component.ConfigureControls( coursePage, controls );
                    tbName.Text = coursePage.Name;
                }

            }
        }

        private void BindRepeater()
        {
            var pageComponents = CoursePageContainer.Instance.Dictionary.Select( i => i.Value.Value as CoursePageComponent ).OrderBy( c => c.Order );
            rComponents.DataSource = pageComponents;
            rComponents.DataBind();
        }

        protected void btnComponent_Click( object sender, EventArgs e )
        {
            var chapterId = PageParameter( PageParameterKey.ChapterId );
            var btn = sender as LinkButton;
            var hf = btn.Parent.FindControl( "hfComponentId" ) as HiddenField;
            var componentId = hf.Value;
            PageComponentId = componentId.AsIntegerOrNull();
            var coursePage = GetCoursePage();
            var component = GetCoursePageComponent( coursePage );
            pnlNew.Visible = false;
            pnlEdit.Visible = true;
            BuildFields();
            component.ConfigureControls( coursePage, controls );
            tbName.Text = coursePage.Name;
        }

        protected void rComponents_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            if ( e.Item.DataItem is CoursePageComponent )
            {
                var component = e.Item.DataItem as CoursePageComponent;
                var btnComponent = e.Item.FindControl( "btnComponent" ) as LinkButton;
                btnComponent.Text = string.Format( @"<br><i class=""{0} fa-5x""></i><br><br>", component.Icon );
            }
        }
        #endregion

        protected void Block_BlockUpdated( object sender, EventArgs e )
        { }

        protected void btnSavePage_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            CoursePageService coursePageService = new CoursePageService( rockContext );

            var coursePage = GetCoursePage( coursePageService );
            var pageCourseComponent = GetCoursePageComponent( coursePage );
            if ( coursePage.Id == 0 )
            {
                var chapterId = PageParameter( PageParameterKey.ChapterId ).AsInteger();
                var coursePages = coursePageService
                    .Queryable()
                    .Where( p => p.ChapterId == chapterId )
                    .OrderBy( cc => cc.Order ).ToList();

                int order = 0;
                if ( coursePages.Any() )
                {
                    order = coursePages.Last().Order + 1;
                }

                if ( !EntityGuid.HasValue )
                {
                    EntityGuid = Guid.NewGuid();
                }

                coursePage = new CoursePage
                {
                    ChapterId = chapterId,
                    EntityTypeId = pageCourseComponent.TypeId,
                    Guid = EntityGuid.Value,
                    Order = order
                };
                coursePageService.Add( coursePage );
            }
            coursePage.Name = tbName.Text;

            pageCourseComponent.ConfigureCoursePage( coursePage, controls );
            rockContext.SaveChanges();

            var queryString = new Dictionary<string, string> {
                    { PageParameterKey.ChapterId, coursePage.ChapterId.ToString()}
            };

            ClientNavigate( queryString );
        }

        private void ClientNavigate( Dictionary<string, string> queryString )
        {
            var pageRef = new PageReference( PageCache.Id, 0, queryString );
            var script = string.Format( "window.location='{0}';", pageRef.BuildUrl() );
            ScriptManager.RegisterStartupScript( upnlContent, upnlContent.GetType(), "clientRedirect", script, true );
        }

        protected void btnCancelPage_Click( object sender, EventArgs e )
        {
            var coursePage = GetCoursePage();
            var queryString = new Dictionary<string, string> {
                    { PageParameterKey.ChapterId, coursePage.ChapterId.ToString()}
            };

            ClientNavigate( queryString );
        }
    }
}
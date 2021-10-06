
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using org.secc.Equip.Model;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace org.secc.Equip.Controls
{
    /// <summary>
    /// 
    /// </summary>
    public class CoursePicker : ItemPicker
    {

        public bool ShowInactive { get; set; } = true;


        protected override void OnInit( EventArgs e )
        {
            ItemRestUrlExtraParams = "?getCategorizedItems=true&showUnnamedEntityItems=true&showCategoriesThatHaveNoChildren=false";
            ItemRestUrlExtraParams += "&entityTypeId=" + EntityTypeCache.Get( typeof( org.secc.Equip.Model.Course ) ).Id;
            ItemRestUrlExtraParams += "&includeInactiveItems=" + ShowInactive + "&lazyLoad=false";
            this.IconCssClass = "fa fa-cogs";
            base.OnInit( e );
        }

        public void SetValue( Course course )
        {
            if ( course != null )
            {
                ItemId = course.Id.ToString();

                string parentCategoryIds = string.Empty;
                var parentCategory = course.CategoryId.HasValue ? CategoryCache.Get( course.CategoryId.Value ) : null;
                while ( parentCategory != null )
                {
                    parentCategoryIds = parentCategory.Id + "," + parentCategoryIds;
                    parentCategory = parentCategory.ParentCategory;
                }

                InitialItemParentIds = parentCategoryIds.TrimEnd( new[] { ',' } );
                ItemName = course.Name;
            }
            else
            {
                ItemId = Rock.Constants.None.IdValue;
                ItemName = Rock.Constants.None.TextHtml;
            }
        }

        public void SetValues( IEnumerable<Course> courses )
        {
            var courseList = courses.ToList();

            if ( courseList.Any() )
            {
                var ids = new List<string>();
                var names = new List<string>();
                var parentCategoryIds = string.Empty;

                foreach ( var course in courseList )
                {
                    if ( course != null )
                    {
                        ids.Add( course.Id.ToString() );
                        names.Add( course.Name );
                        CategoryCache parentCategory = null;
                        if ( course.CategoryId.HasValue )
                        {
                            parentCategory = CategoryCache.Get( course.CategoryId.Value );
                        }

                        if ( parentCategory != null )
                        {
                            // We need to get all of the categories the selected course is nested in order to expand them
                            parentCategoryIds += string.Join( ",", GetCategoryAncestorIdList( parentCategory.Id ) ) + ",";
                        }
                    }
                }

                InitialItemParentIds = parentCategoryIds.TrimEnd( new[] { ',' } );
                ItemIds = ids;
                ItemNames = names;
            }
            else
            {
                ItemId = Rock.Constants.None.IdValue;
                ItemName = Rock.Constants.None.TextHtml;
            }
        }

        /// <summary>
        /// Sets the value on select.
        /// </summary>
        protected override void SetValueOnSelect()
        {
            var course = new CourseService( new RockContext() ).Get( int.Parse( ItemId ) );
            SetValue( course );
        }

        /// <summary>
        /// Sets the values on select.
        /// </summary>
        protected override void SetValuesOnSelect()
        {
            var courses = new CourseService( new RockContext() ).Queryable().Where( g => ItemIds.Contains( g.Id.ToString() ) );
            this.SetValues( courses );
        }

        /// <summary>
        /// Gets the item rest URL.
        /// </summary>
        /// <value>
        /// The item rest URL.
        /// </value>
        public override string ItemRestUrl
        {
            get { return "~/api/Categories/GetChildren/"; }
        }

        private List<int> GetCategoryAncestorIdList( int childCategoryId )
        {
            CategoryService categoryService = new CategoryService( new RockContext() );
            var parentCategories = categoryService.GetAllAncestors( childCategoryId );

            List<int> parentCategoryIds = parentCategories.Select( p => p.Id ).ToList();

            return parentCategoryIds;
        }
    }
}
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
using System.ComponentModel;
using System.Linq;
using org.secc.Rise.Model;
using org.secc.Rise.Utilities;
using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.Rise
{
    [DisplayName( "Personal Rise Report" )]
    [Category( "SECC > Rise" )]
    [Description( "Block for showing completed courses in the Rise LMS" )]

    [CategoryField( "Categories",
        Description = "Categories to show courses from.",
        AllowMultiple = true,
        EntityType = typeof( Course ),
        IsRequired = false,
        Order = 0,
        Key = AttributeKeys.Categories )]

    [BooleanField( "Enrolled Only",
        Description = "Show only courses the person is enrolled in.",
        Order = 1,
        Key = AttributeKeys.EnrolledOnly )]

    public partial class PersonRiseReport : PersonBlock
    {

        class AttributeKeys
        {
            internal const string Categories = "Categories";
            internal const string EnrolledOnly = "EnrolledOnly";
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            var categoryGuids = GetAttributeValue( AttributeKeys.Categories ).SplitDelimitedValues();
            var categories = categoryGuids.Select( c => CategoryCache.Get( c.AsGuid() ) ).ToList();


            lBlockName.Text = this.BlockName;
            if ( Person != null )
            {
                var courses = EnrollmentHelper.GetPersonCourses( Person, categories, GetAttributeValue( AttributeKeys.EnrolledOnly ).AsBoolean() );
                foreach ( var item in courses )
                {
                    phCourses.Controls.Add( new RockLiteral
                    {
                        Label = item.Course.Name,
                        Text = item.Experiences.Where( ex => ex.Result.WasSuccess ).Any() ? "Complete" : "Incomplete"
                    } );
                }
            }

        }
    }
}

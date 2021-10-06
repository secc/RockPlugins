
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using org.secc.Equip.Model;
using Rock.Data;
using Rock.Extension;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace org.secc.Equip
{
    public abstract class CoursePageComponent : Component
    {
        /// <summary>Gets the name.</summary>
        /// <value>The name.</value>
        public abstract string Name { get; }

        /// <summary>Gets the icon.</summary>
        /// <value>The icon.</value>
        public abstract string Icon { get; }

        /// <summary>Gets a value indicating whether [allow documentation mode].</summary>
        /// <value>
        ///   <c>true</c> if [allow documentation mode]; otherwise, <c>false</c>.</value>
        public virtual bool AllowDocumentationMode => true;

        /// <summary>Displays the course page.</summary>
        /// <param name="coursePage">The course page.</param>
        /// <param name="parentControl">The parent control.</param>
        /// <returns></returns>
        public virtual List<Control> DisplayCoursePage( CoursePage coursePage, Control parentControl )
        {
            var controls = new List<Control>();
            var literal = new RockLiteral
            {
                Text = coursePage.Configuration
            };
            parentControl.Controls.Add( literal );

            controls.Add( literal );
            return controls;
        }

        /// <summary>Scores the course.</summary>
        /// <param name="controls">The controls.</param>
        /// <param name="coursePageRecord">The course page record.</param>
        public virtual void ScoreCourse( List<Control> controls, CoursePageRecord coursePageRecord )
        {
            coursePageRecord.Passed = true;
            coursePageRecord.CompletionDateTime = Rock.RockDateTime.Now;
        }

        /// <summary>Displays the configuration.</summary>
        /// <param name="parentControl">The parent control.</param>
        /// <returns></returns>
        public virtual List<Control> DisplayConfiguration( Control parentControl )
        {
            var controls = new List<Control>();
            var textbox = new RockTextBox { Label = "Test" };
            parentControl.Controls.Add( textbox );

            controls.Add( textbox );
            return controls;
        }

        /// <summary>Configures the controls.</summary>
        /// <param name="coursePage">The course page.</param>
        /// <param name="controls">The controls.</param>
        public virtual void ConfigureControls( CoursePage coursePage, List<Control> controls )
        {
            if ( controls.Count > 0 )
            {
                var textbox = ( RockTextBox ) controls[0];
                textbox.Text = coursePage.Configuration;
            }
        }

        /// <summary>Configures the course page.</summary>
        /// <param name="coursePage">The course page.</param>
        /// <param name="controls">The controls.</param>
        public virtual void ConfigureCoursePage( CoursePage coursePage, List<Control> controls )
        {
            if ( controls.Count > 0 )
            {
                coursePage.Configuration = ( ( RockTextBox ) controls[0] ).Text;
            }
        }
    }
}
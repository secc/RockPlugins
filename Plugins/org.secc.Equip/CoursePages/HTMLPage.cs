using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using org.secc.Equip.Model;
using Rock.Web.UI.Controls;

namespace org.secc.Equip.CoursePages
{
    [Export( typeof( CoursePageComponent ) )]
    [ExportMetadata( "ComponentName", "HTML Page" )]
    [Description( "Teach with the written word. Use HTML for text and images." )]
    public class HTMLPage : CoursePageComponent
    {
        public override string Name => "HTML Page";

        public override string Icon => "fa fa-file-code";

        public override int Order => 1;

        public override List<Control> DisplayConfiguration( Control parentControl )
        {
            var controls = new List<Control>();
            var htmlEditor = new HtmlEditor
            {
                Label = "Lesson HTML",
                Help = "The text to enter for use in the lesson.",
                Toolbar = HtmlEditor.ToolbarConfig.Full,
                Height = 400,
                ID = "tbLessonText",
                ValidateRequestMode = ValidateRequestMode.Disabled

            };
            parentControl.Controls.Add( htmlEditor );

            controls.Add( htmlEditor );
            return controls;
        }

        public override void ConfigureControls( CoursePage coursePage, List<Control> controls )
        {
            if ( controls.Count > 0 )
            {
                var codeEditor = ( HtmlEditor ) controls[0];
                codeEditor.Text = coursePage.Configuration;
            }
        }

        public override void ConfigureCoursePage( CoursePage coursePage, List<Control> controls )
        {
            if ( controls.Count > 0 )
            {
                coursePage.Configuration = ( ( HtmlEditor ) controls[0] ).Text;
            }
        }

    }
}

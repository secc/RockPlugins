using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using org.secc.Equip.Model;
using Rock;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace org.secc.Equip.CoursePages
{
    [Export( typeof( CoursePageComponent ) )]
    [ExportMetadata( "ComponentName", "Vimeo Video Page" )]
    [Description( "Page for playing videos from vimeo." )]
    public class VimeoPage : CoursePageComponent
    {
        public override string Name => "Vimeo Video Page";

        public override string Icon => "fa fa-vimeo";

        public override int Order => 10;
        public override List<Control> DisplayConfiguration( Control parentControl )
        {
            var controls = new List<Control>();

            RockTextBox embedCode = new RockTextBox
            {
                Label = "Vimeo Embed Code",
                Help = "The HTML provided by Vimeo for embeding the video",
                ID = "tbEmbed",
                ValidateRequestMode = ValidateRequestMode.Disabled
            };
            parentControl.Controls.Add( embedCode );
            controls.Add( embedCode );

            RockTextBox score = new RockTextBox
            {
                Label = "Watch Percent Required",
                Help = "The percent of the video the user is required to watch to complete the page.",
                ID = "tbScore"
            };
            parentControl.Controls.Add( score );
            controls.Add( score );

            return controls;
        }

        public override void ConfigureControls( CoursePage coursePage, List<Control> controls )
        {
            if ( controls.Count > 0 )
            {
                RockTextBox embedCode = ( RockTextBox ) controls[0];
                embedCode.Text = coursePage.Configuration;
            }
            if ( controls.Count > 1 )
            {
                RockTextBox percentRequired = ( RockTextBox ) controls[1];
                percentRequired.Text = coursePage.PassingScore.ToString();
            }
        }

        public override void ConfigureCoursePage( CoursePage coursePage, List<Control> controls )
        {
            if ( controls.Count > 0 )
            {
                RockTextBox embedCode = ( RockTextBox ) controls[0];
                coursePage.Configuration = embedCode.Text;
            }
            if ( controls.Count > 1 )
            {
                RockTextBox percentRequired = ( RockTextBox ) controls[1];
                coursePage.PassingScore = percentRequired.Text.AsInteger();
            }
        }

        public override List<Control> DisplayCoursePage( CoursePage coursePage, Control parentControl )
        {
            var controls = new List<Control>();

            HiddenField hfScore = new HiddenField
            {
                ID = "hfScore",
                Value = coursePage.PassingScore.ToString()
            };
            parentControl.Controls.Add( hfScore );
            controls.Add( hfScore );

            //Update the html to make it pleasent for mobile
            coursePage.Configuration = coursePage.Configuration.Replace( "iframe", "iframe class='embed-item'" );

            Literal literal = new Literal
            {
                Text = "<div id='vimeoVideoContainer' class='embed-responsive embed-responsive-16by9'>" + coursePage.Configuration + "</div>"
            };
            controls.Add( literal );
            parentControl.Controls.Add( literal );

            var page = parentControl.Page;
            ScriptManager.RegisterClientScriptInclude( page, page.GetType(), "VimeoAPI", "https://player.vimeo.com/api/player.js" );
            ScriptManager.RegisterClientScriptInclude( page, page.GetType(), "VimeoHelper", "/Plugins/org_secc/Equip/VimeoHelper.js" );
            ScriptManager.RegisterStartupScript( page, page.GetType(), "VimeoSetup", "VimeoSetup();", true );

            return controls;
        }
    }
}

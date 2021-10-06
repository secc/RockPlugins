using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    [ExportMetadata( "ComponentName", "YouTube Video Page" )]
    [Description( "Page for playing videos from YouTube." )]
    public class YouTubePage : CoursePageComponent
    {
        public override string Name => "YouTube Video Page";

        public override string Icon => "fa fa-youtube";

        public override int Order => 10;
        public override List<Control> DisplayConfiguration( Control parentControl )
        {
            var controls = new List<Control>();

            RockTextBox embedCode = new RockTextBox
            {
                Label = "YouTube Embed Code",
                Help = "The HTML provided by YouTube for embeding the video",
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

            var regex = @"((?:https?:)?\/\/)?((?:www|m)\.)?((?:youtube\.com|youtu.be))(\/(?:[\w\-]+\?v=|embed\/|v\/)?)([\w\-]+)";

            var rx = new Regex( regex );
            MatchCollection matches = rx.Matches( coursePage.Configuration );
            if ( matches.Count > 0 )
            {
                var match = matches[0];
                // need this to make the javascript work
                coursePage.Configuration = coursePage.Configuration.Replace( match.Value, match.Value + "?enablejsapi=1" );
            }

            //Update the html to make it pleasent for mobile
            coursePage.Configuration = coursePage.Configuration.Replace( "iframe", "iframe class='embed-item'" );

            Literal literal = new Literal
            {
                Text = "<div id='youtubeVideoContainer' class='embed-responsive embed-responsive-16by9'>" + coursePage.Configuration + "</div>"
            };
            controls.Add( literal );
            parentControl.Controls.Add( literal );

            var page = parentControl.Page;
            ScriptManager.RegisterClientScriptInclude( page, page.GetType(), "YouTubeHelper", "/Plugins/org_secc/Equip/YouTubeHelper.js" );
            ScriptManager.RegisterStartupScript( page, page.GetType(), "YouTubeSetup", "YouTubeSetup();", true );

            return controls;
        }
    }
}

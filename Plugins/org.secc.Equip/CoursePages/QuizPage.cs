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
    [ExportMetadata( "ComponentName", "Quiz Page" )]
    [Description( "Page for creating quizes to test the user." )]
    public class QuizPage : CoursePageComponent
    {
        public override string Name => "Quiz Page";

        public override string Icon => "fa fa-question-circle";

        public override int Order => 2;

        public override List<Control> DisplayConfiguration( Control parentControl )
        {
            var controls = new List<Control>();
            HiddenField hiddenField = new HiddenField
            {
                ID = "hfConfiguration",
                ValidateRequestMode = ValidateRequestMode.Disabled
            };
            parentControl.Controls.Add( hiddenField );
            controls.Add( hiddenField );

            RockTextBox score = new RockTextBox
            {
                Label = "Passing Score",
                Help = "Number of questions that must be answered correctly to pass. Leave blank to ignore score.",
                ID = "tbScore"
            };
            parentControl.Controls.Add( score );
            controls.Add( score );

            Literal literal = new Literal
            {
                Text = "<h3>Quiz Builder</h3>" +
                "Press \"Add Question\" to create a new question. \"Add Answer\" allows you to add a new answer." +
                " Indicate the correct answer with the radio buttons." +
                "<div class='panel-group QuizContent' class='panel-group' id='accordion-quizeditor'></div>" +
                "<a class='btn btn-primary' id='btnAddQuestion'><i class='fa fa-plus'></i> Add Question</a>",
                ID = "ltBuilder"

            };
            parentControl.Controls.Add( literal );
            controls.Add( literal );

            var page = parentControl.Page;
            ScriptManager.RegisterClientScriptInclude( page, page.GetType(), "QuizEdit", "/Plugins/org_secc/Equip/QuizEditor.js" );

            return controls;
        }

        public override void ConfigureControls( CoursePage coursePage, List<Control> controls )
        {
            var configuration = JsonConvert.DeserializeObject<QuizPageConfiguration>( coursePage.Configuration );
            if ( configuration == null )
            {
                configuration = new QuizPageConfiguration();
            }
            if ( controls.Count > 0 )
            {
                var hiddenField = ( HiddenField ) controls[0];
                hiddenField.Value = configuration.QuizConfiguration;
                ScriptManager.RegisterStartupScript( hiddenField.Page, hiddenField.Page.GetType(), "RenderQuizEditor", "RenderQuizEditor();", true );
            }
            if ( controls.Count > 1 )
            {
                RockTextBox score = ( RockTextBox ) controls[1];
                score.Text = coursePage.PassingScore.ToString();
            }
        }

        public override void ConfigureCoursePage( CoursePage coursePage, List<Control> controls )
        {
            var configuration = new QuizPageConfiguration();
            if ( controls.Count > 0 )
            {
                var hiddenField = ( HiddenField ) controls[0];
                configuration.QuizConfiguration = hiddenField.Value;
            }
            if ( controls.Count > 1 )
            {
                RockTextBox score = ( RockTextBox ) controls[1];
                coursePage.PassingScore = score.Text.AsInteger();
            }
            coursePage.Configuration = JsonConvert.SerializeObject( configuration );
        }

        public override List<Control> DisplayCoursePage( CoursePage coursePage, Control parentControl )
        {
            var configuration = JsonConvert.DeserializeObject<QuizPageConfiguration>( coursePage.Configuration );
            var controls = new List<Control>();

            HiddenField hfScore = new HiddenField
            {
                ID = "hfScore",
                Value = configuration.QuizConfiguration
            };
            parentControl.Controls.Add( hfScore );
            controls.Add( hfScore );

            HiddenField hfConfiguration = new HiddenField
            {
                ID = "hfConfiguration",
                Value = configuration.QuizConfiguration
            };
            parentControl.Controls.Add( hfConfiguration );
            controls.Add( hfConfiguration );

            Literal literal = new Literal
            {
                Text = "<div class='QuizContent'></div>"
            };
            controls.Add( literal );
            parentControl.Controls.Add( literal );

            var page = parentControl.Page;
            ScriptManager.RegisterClientScriptInclude( page, page.GetType(), "QuizDisplay", "/Plugins/org_secc/Equip/QuizDisplay.js" );
            ScriptManager.RegisterStartupScript( page, page.GetType(), "RenderQuizDisplay", "RenderQuizDisplay();", true );
            return controls;
        }

        public override void ScoreCourse( List<Control> controls, CoursePageRecord coursePageRecord )
        {
            coursePageRecord.Score = 0;
            if ( controls.Count > 0 && controls[0] is HiddenField )
            {
                coursePageRecord.Score = ( ( HiddenField ) controls[0] ).Value.AsInteger();
            }

            if ( coursePageRecord.Score >= coursePageRecord.CoursePage.PassingScore )
            {
                coursePageRecord.Passed = true;
            }
            else
            {
                coursePageRecord.Passed = false;
            }
        }

        public class QuizPageConfiguration
        {
            public string QuizConfiguration { get; set; }
        }

    }
}

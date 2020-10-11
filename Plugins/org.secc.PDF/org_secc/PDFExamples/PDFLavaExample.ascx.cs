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
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.ComponentModel;

using Rock;
using Rock.Web.UI;
using System.Web.UI.HtmlControls;
using Rock.Web.UI.Controls;
using Rock.Data;
using Rock.Model;
using org.secc.PDF;
using Rock.Attribute;
using Rock.Web.Cache;

namespace RockWeb.Plugins.org_secc.PDFExamples
{
    [DisplayName( "PDF Lava Example" )]
    [Category( "CMS" )]
    [Description( "Example " )]

    //Settings
    [WorkflowTypeField( "Workflow Type", "The workflow type to activate to merge pdf" )]
    [TextField( "Workflow Activity", "The name of the workflow activity to run on selection.", false, "" )]
    public partial class PDFLavaExample : RockBlock
    {
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                ceLava.Text = @"
<?xml version='1.0' encoding='UTF-8'?>
<!DOCTYPE html
PUBLIC '-//W3C//DTD XHTML 1.0 Strict//EN'>
<html xmlns='http://www.w3.org/1999/xhtml' xml:lang='en' lang='en'>
<head>
<title> Strict DTD XHTML Example </title>
<style type='text/css'>
	body{
		background-color:black;
		color:white;
		width:100%;
		height:100%;
        font-family:'Times New Roman', Times, serif
	}
</style>
</head>
<body >
<div id='content' style='font-family:'Times New Roman', Times, serif'>
<ul>
   <li><p>Welcome {{CurrentPerson.FullName}}</p></li>
   <li><p>Aliquam tincidunt mauris eu risus.</p></li>
   <li><a href='http:\\www.google.com'>Test</a></li>
    <li><a href='Download/test.html'>Test 2</a></li>
</ul>
<br/>
<img src='../img/Tulips.jpg' alt='test1'/>
<p>Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Vestibulum tortor quam, feugiat vitae, ultricies eget, tempor sit amet, ante. Donec eu libero sit amet quam egestas semper. Aenean ultricies mi vitae est. Mauris placerat eleifend leo.</p>
</div>
</body>
</html>
";
            }
        }


        protected void bntMerge_Click( object sender, EventArgs e )
        {
            PDFWorkflowObject pdfWorkflowObject = new PDFWorkflowObject();

            pdfWorkflowObject.LavaInput = ceLava.Text;

            pdfWorkflowObject.MergeObjects = new Dictionary<string, object>();
            if ( cbCurrentPerson.Checked )
            {
                pdfWorkflowObject.MergeObjects.Add( "CurrentPerson", CurrentPerson );
            }
            if ( cbGlobal.Checked )
            {
                pdfWorkflowObject.MergeObjects.Add( "GlobalAttributes", GlobalAttributesCache.Get() );
            }


            Guid workflowTypeGuid = Guid.NewGuid();
            if ( Guid.TryParse( GetAttributeValue( "WorkflowType" ), out workflowTypeGuid ) )
            {
                var workflowRockContext = new RockContext();
                var workflowTypeService = new WorkflowTypeService( workflowRockContext );
                var workflowType = workflowTypeService.Get( workflowTypeGuid );
                if ( workflowType != null )
                {
                    var workflow = Workflow.Activate( WorkflowTypeCache.Get( workflowType.Id ), "PDFLavaWorkflow" );

                    List<string> workflowErrors;
                    var workflowService = new WorkflowService( workflowRockContext );
                    var workflowActivity = GetAttributeValue( "WorkflowActivity" );
                    var activityType = workflowType.ActivityTypes.Where( a => a.Name == workflowActivity ).FirstOrDefault();
                    if ( activityType != null )
                    {
                        WorkflowActivity.Activate( WorkflowActivityTypeCache.Get( activityType.Id ), workflow, workflowRockContext );
                        if ( workflowService.Process( workflow, pdfWorkflowObject, out workflowErrors ) )
                        {
                            //success
                        }
                    }
                }
            }

            RockContext rockContext = new RockContext();
            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
            pdfWorkflowObject.PDF.FileName = "LavaGeneratedPDF.pdf";
            binaryFileService.Add( pdfWorkflowObject.PDF );
            rockContext.SaveChanges();

            Response.Redirect( pdfWorkflowObject.PDF.Path );
        }

        private Dictionary<string, object> GetMergeFields()
        {
            var rawMergeFields = kvlMerge.Value;
            var splitMergeFields = rawMergeFields.Split( '|' );
            Dictionary<string, object> mergeFields = new Dictionary<string, object>();
            foreach ( string split in splitMergeFields )
            {
                if ( !string.IsNullOrWhiteSpace( split ) )
                {
                    var keyValue = split.Split( '^' );
                    mergeFields.Add( keyValue[0], keyValue[1] );
                }
            }
            return mergeFields;
        }
    }
}
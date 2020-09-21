using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FFmpeg.NET;
using FFmpeg.NET.Events;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;

namespace org.secc.Workflow.media
{
    [ExportMetadata( "ComponentName", "FFmpeg" )]
    [ActionCategory( "SECC > Media" )]
    [Description( "Runs FFmpeg commands." )]
    [Export( typeof( ActionComponent ) )]
    [WorkflowTextOrAttribute( "File", "File Attribute", "The source file to use with FFmpeg.", true, "", "", 1, "File", new string[] { "Rock.Field.Types.TextFieldType" } )]
    [TextField( "Command", "The FFmpeg command to run (parameters).  The {{file}} and {{outputPath}} merge fields are available here. <span class='tip tip-lava'></span>", true, "", "", 2, "Command" )]
    [TextField( "FFmpeg Executable", "The FFmpeg executable location.", true, "C:\\Program Files\\ImageMagick-7.0.10-Q16-HDRI\\ffmpeg.exe", "", 3, "FFmpegExecutable" )]
    [WorkflowAttribute( "Output Path", "An optional output path where the image montages should be stored.  If the attribute is empty a temporary directory will be created and the workflow attribute will be set to that directory.", true, "", "", 4, "OutputPath", new string[] { "Rock.Field.Types.TextFieldType" } )]

    public class FFmpeg : ActionComponent
    {
        StringBuilder output = new StringBuilder();

        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            var file = GetAttributeValue( action, "File", true );
            var command = GetAttributeValue( action, "Command" );
            var executable = GetAttributeValue( action, "FFmpegExecutable" );

            var outputPath = GetAttributeValue( action, "OutputPath", true );
            if ( string.IsNullOrWhiteSpace(outputPath) )
            {
                outputPath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() ) + Path.DirectorySeparatorChar;
            } 
            else
            {
                outputPath = outputPath.TrimEnd( Path.DirectorySeparatorChar ) + Path.DirectorySeparatorChar;
            }

            Directory.CreateDirectory( outputPath );

            var mergeFields = GetMergeFields( action );
            mergeFields.Add( "file", file );
            mergeFields.Add( "outputPath", outputPath );
            
            Task.Run( async () => await StartConverting( command.ResolveMergeFields( mergeFields ), executable ) ).Wait();
            action.AddLogEntry( output.ToString() );

            // Now store the attribute
            var targetAttribute = AttributeCache.Get( GetActionAttributeValue( action, "OutputPath" ).AsGuid(), rockContext );
            if ( targetAttribute.EntityTypeId == new Rock.Model.Workflow().TypeId )
            {
                action.Activity.Workflow.SetAttributeValue( targetAttribute.Key, outputPath );
            }
            else if ( targetAttribute.EntityTypeId == new WorkflowActivity().TypeId )
            {
                action.Activity.SetAttributeValue( targetAttribute.Key, outputPath );
            }

            return true;
        }

        public async Task StartConverting( string command, string executable )
        {


            var ffmpeg = new Engine( executable );

            ffmpeg.Progress += OnProgress;
            ffmpeg.Data += OnData;
            ffmpeg.Error += OnError;
            ffmpeg.Complete += OnComplete;
            await ffmpeg.ExecuteAsync( command );
        }

        private void OnProgress( object sender, ConversionProgressEventArgs e )
        {
            output.AppendFormat( "Bitrate: {0}\n", e.Bitrate );
            output.AppendFormat( "Fps: {0}\n", e.Fps );
            output.AppendFormat( "Frame: {0}\n", e.Frame );
            output.AppendFormat( "ProcessedDuration: {0}\n", e.ProcessedDuration );
            output.AppendFormat( "Size: {0} kb\n", e.SizeKb );
            output.AppendFormat( "TotalDuration: {0}\n", e.TotalDuration );
        }

        private void OnData( object sender, ConversionDataEventArgs e )
        {
            output.AppendFormat( "{0}\n", e.Data );
        }

        private void OnComplete( object sender, ConversionCompleteEventArgs e )
        {
            output.AppendLine( "Done!\n" );
        }

        private void OnError( object sender, ConversionErrorEventArgs e )
        {
            output.AppendFormat( "Error: {0}\n{1}\n", e.Exception.ExitCode, e.Exception.InnerException );
        }
    }
}

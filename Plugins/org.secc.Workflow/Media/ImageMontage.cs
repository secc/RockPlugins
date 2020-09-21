using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using ImageMagick;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;

namespace org.secc.Workflow.media
{
    [ExportMetadata( "ComponentName", "Image Montage" )]
    [ActionCategory( "SECC > Media" )]
    [Description( "Creates one or more JPG montages of image tiles.  Each montage file contain up to width*height images and will be sequentially numbered on output." )]
    [Export( typeof( ActionComponent ) )]
    [WorkflowTextOrAttribute( "Image Columns", "Image Columns Attribute", "How many images columns should be generated in each montage.", true, "5", "", 1, "Wide" )]
    [WorkflowTextOrAttribute( "Image Rows", "Image Rows Attribute", "How many images rows should be generated in each montage.", true, "4", "", 2, "High" )]
    [WorkflowTextOrAttribute( "Filename Prefix", "Filename Prefix Attribute", "What is the prefix name for each montage.", true, "montage", "", 3, "Filename" )]
    [WorkflowTextOrAttribute( "Input Path", "Input Path Attribute", "The input path where the source images are saved.", true, "", "", 4, "InputPath", new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowTextOrAttribute( "Output Path", "Output Path Attribute", "The output path where the image montages should be stored.", true, "", "", 5, "OutputPath", new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowAttribute( "Output", "The output attribute where the a CSV string containing each image montage will be saved.", true, "", "", 6, "Output", new string[] { "Rock.Field.Types.TextFieldType" } )]

    public class ImageMontage : ActionComponent
    {

        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            int wide = GetAttributeValue( action, "Wide", true ).AsInteger();
            int high = GetAttributeValue( action, "High", true ).AsInteger();
            string filename = GetAttributeValue( action, "Filename", true );
            string inputPath = GetAttributeValue( action, "InputPath", true );
            var outputPath = GetAttributeValue( action, "OutputPath", true ).TrimEnd( Path.DirectorySeparatorChar ) + Path.DirectorySeparatorChar;
            Directory.CreateDirectory( outputPath );

            errorMessages = new List<string>();
            var outputFiles = new List<string>();

            using ( var images = new MagickImageCollection() )
            {
                int i = 1;
                int j = 0;
                foreach ( string file in Directory.EnumerateFiles( inputPath, "*" ) )
                {
                    images.Add( new MagickImage( file ) );
                    if ( i % ( wide * high) == 0)
                    {
                        outputFiles.Add( OutputMontage( filename, outputPath, images, j) );
                        j++;
                    }
                    i++;
                }

                if (images.Count > 0)
                {
                    outputFiles.Add( OutputMontage( filename, outputPath, images, j ) );
                }


            }

            // Now store the attribute
            var targetAttribute = AttributeCache.Get( GetActionAttributeValue( action, "Output" ).AsGuid(), rockContext );
            if ( targetAttribute.EntityTypeId == new Rock.Model.Workflow().TypeId )
            {
                action.Activity.Workflow.SetAttributeValue( targetAttribute.Key, outputFiles.JoinStrings(",") );
            }
            else if ( targetAttribute.EntityTypeId == new WorkflowActivity().TypeId )
            {
                action.Activity.SetAttributeValue( targetAttribute.Key, outputFiles.JoinStrings( "," ) );
            }

            return true;
        }

        private string OutputMontage( string filename, string outputPath, MagickImageCollection images, int j )
        {
            // Keep the original image's width/height in the Montage
            var settings = new MontageSettings();
            settings.Geometry = new MagickGeometry( images[0].Width, images[0].Height );
            string outputfile = string.Format( "{0}{1}_{2}.jpg", outputPath, filename, j );

            // Create a montage from the images
            using ( var result = images.Montage( settings ) )
            {
                // Save the result
                result.Write( outputfile );
            }
            images.Clear();
            return outputfile;
        }
    }
}

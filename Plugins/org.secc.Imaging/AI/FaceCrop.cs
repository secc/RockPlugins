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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using org.secc.DevLib.Components;
using org.secc.Imaging.Components;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Imaging.AI
{
    public class FaceCrop
    {
        private IFaceClient faceClient;

        public FaceCrop()
        {
            /*This class isn't terribly well organized, and probbly needs brokend down into
             * additional helper classes. I just needed something that would crop a few thousand
             * camper's faces really quick and organize them accordingly.
             * --Mark
             */

            var settings = SettingsComponent.GetComponent<MicrosoftFaceSettings>();
            faceClient = Authenticate( settings.GetAttributeValue( "Endpoint" ), settings.GetAttributeValue( "SubscriptionKey" ) );
        }

        public async Task<bool> UpdatePhoto( Rock.Model.Person person, BinaryFile binaryFile )
        {
            // Microsoft face doesn't do well with jumbo images
            // Send them a scaled image instead
            double scaleFactor = 1.0;
            bool isLargeImage = false;

            if ( binaryFile.Width.HasValue && ( binaryFile.Width.Value > 2000 || binaryFile.Height.Value > 2000 ) )
            {
                isLargeImage = true;
                scaleFactor = Math.Min( 2000.0 / binaryFile.Width.Value, 2000.0 / binaryFile.Height.Value );
            }
            //for debugging
            var url = GlobalAttributesCache.Get().GetValue( "PublicApplicationRoot" )
                .EnsureTrailingForwardslash()
                + "GetImage.ashx?Guid=" + binaryFile.Guid.ToString();

            if ( isLargeImage )
            {
                url = url + "&w=" + ( ( int ) ( binaryFile.Width.Value * scaleFactor ) ).ToString();
            }

            var detectedFace = await DetectFace( url );
            if ( detectedFace == null )
            {
                return false;
            }

            var stream = CropDetectedFace( detectedFace, binaryFile.ContentStream, scaleFactor );

            UpdatePersonPhoto( person, stream );

            stream.Dispose();


            return true;
        }

        private async Task<DetectedFace> DetectFace( string imageUrl )
        {
            var detectedFaces = await faceClient.Face.DetectWithUrlAsync( imageUrl,
                    returnFaceAttributes: new List<FaceAttributeType> { FaceAttributeType.HeadPose },
                    // We specify detection model 1 because we are retrieving attributes.
                    detectionModel: DetectionModel.Detection01,
                    recognitionModel: RecognitionModel.Recognition04 );
            return detectedFaces.FirstOrDefault();
        }

        private MemoryStream CropDetectedFace( DetectedFace detectedFace, Stream imageStream, double scaleFactor )
        {
            var left = detectedFace.FaceRectangle.Left;
            var width = detectedFace.FaceRectangle.Width;
            var right = left + width;
            var top = detectedFace.FaceRectangle.Top;
            var height = detectedFace.FaceRectangle.Height;
            var bottom = top + height;

            // Adjust numbers for scaled image
            if ( scaleFactor != 1.0 )
            {
                left = ( int ) ( left / scaleFactor );
                width = ( int ) ( width / scaleFactor );
                right = ( int ) ( right / scaleFactor );
                top = ( int ) ( top / scaleFactor );
                height = ( int ) ( height / scaleFactor );
                bottom = ( int ) ( bottom / scaleFactor );
            }

            var roll = ( float ) Math.Round( detectedFace.FaceAttributes.HeadPose.Roll, 2 );

            using ( var src = Image.FromStream( imageStream ) as Bitmap )
            {
                // Rotate the source image first
                Bitmap rotatedSrc = RotateImage( src, -roll );

                // Rotate the face rectangle coordinates
                PointF center = new PointF( src.Width / 2f, src.Height / 2f );

                // Define the four corners of the face rectangle
                PointF[] faceRectPoints =
                {
                    new PointF(left, top),
                    new PointF(right, top),
                    new PointF(right, bottom),
                    new PointF(left, bottom)
                };

                // Rotate each point
                for ( int i = 0; i < faceRectPoints.Length; i++ )
                {
                    faceRectPoints[i] = RotatePoint( faceRectPoints[i], center, -roll );
                }

                // Calculate the bounding rectangle of the rotated face rectangle
                float minX = faceRectPoints.Min( p => p.X );
                float maxX = faceRectPoints.Max( p => p.X );
                float minY = faceRectPoints.Min( p => p.Y );
                float maxY = faceRectPoints.Max( p => p.Y );

                int newLeft = ( int ) minX;
                int newTop = ( int ) minY;
                int newWidth = ( int ) ( maxX - minX );
                int newHeight = ( int ) ( maxY - minY );

                // Expand the crop rectangle around the face
                int maxDist = new List<int>
                    {
                        newWidth / 2,
                        newLeft,
                        newTop,
                        rotatedSrc.Width - (newLeft + newWidth),
                        rotatedSrc.Height - (newTop + newHeight)
                    }.Min();

                Rectangle cropRect = new Rectangle( newLeft - maxDist, newTop - maxDist, newWidth + ( maxDist * 2 ), newHeight + ( maxDist * 2 ) );

                // Ensure cropRect is within bounds
                cropRect.Intersect( new Rectangle( 0, 0, rotatedSrc.Width, rotatedSrc.Height ) );

                // Create target bitmap
                Bitmap target = new Bitmap( 500, 500 );

                using ( Graphics g = Graphics.FromImage( target ) )
                {
                    g.DrawImage( rotatedSrc, new Rectangle( 0, 0, target.Width, target.Height ), cropRect, GraphicsUnit.Pixel );
                }

                // Save the target bitmap
                ImageCodecInfo myImageCodecInfo;
                System.Drawing.Imaging.Encoder myEncoder;
                EncoderParameter myEncoderParameter;
                EncoderParameters myEncoderParameters;

                myEncoder = System.Drawing.Imaging.Encoder.Quality;
                myImageCodecInfo = GetEncoderInfo( "image/jpeg" );

                myEncoderParameters = new EncoderParameters( 1 );
                myEncoderParameter = new EncoderParameter( myEncoder, 95L );
                myEncoderParameters.Param[0] = myEncoderParameter;

                var stream = new MemoryStream();

                target.Save( stream, myImageCodecInfo, myEncoderParameters );

                return stream;
            }
        }

        private Bitmap RotateImage( Bitmap src, float angle )
        {
            // Create a new empty bitmap to hold rotated image.
            Bitmap result = new Bitmap( src.Width, src.Height );
            result.SetResolution( src.HorizontalResolution, src.VerticalResolution );

            // Make a graphics object from the empty bitmap.
            using ( Graphics g = Graphics.FromImage( result ) )
            {
                // Move the origin to the center of the image.
                g.TranslateTransform( ( float ) src.Width / 2, ( float ) src.Height / 2 );

                // Rotate.
                g.RotateTransform( angle );

                // Move the image back.
                g.TranslateTransform( -( float ) src.Width / 2, -( float ) src.Height / 2 );

                // Draw the passed in image onto the graphics object.
                g.DrawImage( src, new Point( 0, 0 ) );
            }

            return result;
        }

        private PointF RotatePoint( PointF point, PointF pivot, float angle )
        {
            float radian = angle * ( float ) ( Math.PI / 180 );
            float cosTheta = ( float ) Math.Cos( radian );
            float sinTheta = ( float ) Math.Sin( radian );
            return new PointF
            (
                cosTheta * ( point.X - pivot.X ) - sinTheta * ( point.Y - pivot.Y ) + pivot.X,
                sinTheta * ( point.X - pivot.X ) + cosTheta * ( point.Y - pivot.Y ) + pivot.Y
            );
        }

        private void UpdatePersonPhoto( Rock.Model.Person person, MemoryStream photoData )
        {
            RockContext rockContext = new RockContext();
            PersonService personService = new PersonService( rockContext );

            person = personService.Get( person.Id );


            var binaryFileType = new BinaryFileTypeService( rockContext ).GetNoTracking( Rock.SystemGuid.BinaryFiletype.PERSON_IMAGE.AsGuid() );

            var fileName = person.FullName.RemoveSpaces().MakeValidFileName();
            if ( fileName.IsNullOrWhiteSpace() )
            {
                fileName = "PersonPhoto";
            }

            var binaryFile = new BinaryFile()
            {
                FileName = fileName,
                MimeType = "image/jpeg",
                BinaryFileTypeId = binaryFileType.Id,
                IsTemporary = false
            };

            binaryFile.SetStorageEntityTypeId( binaryFileType.StorageEntityTypeId );

            byte[] photoDataBytes = photoData.ToArray();
            binaryFile.FileSize = photoDataBytes.Length;
            binaryFile.ContentStream = new MemoryStream( photoDataBytes );

            var binaryFileService = new BinaryFileService( rockContext );
            binaryFileService.Add( binaryFile );
            rockContext.SaveChanges();

            person.PhotoId = binaryFile.Id;

            rockContext.SaveChanges();
        }


        private static IFaceClient Authenticate( string endpoint, string key )
        {
            return new FaceClient( new ApiKeyServiceClientCredentials( key ) ) { Endpoint = endpoint };
        }

        private static ImageCodecInfo GetEncoderInfo( String mimeType )
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for ( j = 0; j < encoders.Length; ++j )
            {
                if ( encoders[j].MimeType == mimeType )
                    return encoders[j];
            }
            return null;
        }

    }
}

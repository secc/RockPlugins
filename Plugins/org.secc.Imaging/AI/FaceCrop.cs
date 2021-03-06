﻿// <copyright>
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
            //Microsoft face doesn't do well with jumbo images
            //Send them a half size image instead
            bool isLargeImage = false;
            if ( binaryFile.Width.HasValue && binaryFile.Width.Value > 2000 )
            {
                isLargeImage = true;
            }
            //for debugging
            var url = GlobalAttributesCache.Get().GetValue( "PublicApplicationRoot" )
                .EnsureTrailingForwardslash()
                + "/GetImage.ashx?Guid=" + binaryFile.Guid.ToString();

            if ( isLargeImage )
            {
                url = url + "&w=" + ( binaryFile.Width.Value / 2 ).ToString();
            }

            var detectedFace = await DetectFace( url );
            if ( detectedFace == null )
            {
                return false;
            }

            var stream = CropDetectedFace( detectedFace, binaryFile.ContentStream, isLargeImage );

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

        private MemoryStream CropDetectedFace( DetectedFace detectedFace, Stream imageStream, bool isLargeImage )
        {
            var left = detectedFace.FaceRectangle.Left;
            var width = detectedFace.FaceRectangle.Width;
            var right = left + width;
            var top = detectedFace.FaceRectangle.Top;
            var height = detectedFace.FaceRectangle.Height;
            var bottom = top + height;

            //Adjust numbers for half sized image
            if ( isLargeImage )
            {
                left = left * 2;
                width = width * 2;
                right = right * 2;
                top = top * 2;
                height = height * 2;
                bottom = bottom * 2;
            }

            var roll = Math.Round( detectedFace.FaceAttributes.HeadPose.Roll, 2 );

            Bitmap src = Image.FromStream( imageStream ) as Bitmap;

            var maxDist = ( new List<int>
                    { width / 2,
                        left,
                        top,
                        src.Width - right,
                        src.Height - bottom
                    } )
            .Min();

            Rectangle cropRect = new Rectangle( left - maxDist, top - maxDist, width + ( maxDist * 2 ), height + ( maxDist * 2 ) );

            Bitmap target = new Bitmap( 500, 500 );

            using ( Graphics g = Graphics.FromImage( target ) )
            {
                if ( roll < -15 || roll > 15 )
                {
                    g.TranslateTransform( target.Width / 2, target.Height / 2 );
                    g.RotateTransform( ( float ) roll * -1 );
                    g.TranslateTransform( -( target.Width / 2 ), -( target.Height / 2 ) );
                }

                g.DrawImage( src, new Rectangle( 0, 0, target.Width, target.Height ), cropRect, GraphicsUnit.Pixel );

            }

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

            imageStream.Dispose();

            return stream;
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

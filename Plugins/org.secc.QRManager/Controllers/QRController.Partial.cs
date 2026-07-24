using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Web.Http;
using QRCoder;
using Rock.Rest;
using Rock.Rest.Filters;

namespace org.secc.QRManager.Rest.Controllers
{
    public partial class QRController : ApiControllerBase
    {

        [Authenticate]
        [HttpGet]
        [System.Web.Http.Route( "api/qr/{code}" )]
        public HttpResponseMessage GetEntityGuid( string code )
        {
            // Only accept codes matching the known caller formats (MCR + 12 hex AccessKey, PFP-prefixed hyphenated alternate id).
            // This blocks arbitrary content from being rendered into a QR image by this anonymous endpoint (URLs need : / . none allowed).
            if ( string.IsNullOrEmpty( code ) || !Regex.IsMatch( code, "^[A-Za-z0-9-]{1,64}$" ) )
            {
                return Request.CreateErrorResponse( HttpStatusCode.BadRequest, "Invalid code" );
            }

            byte[] buffer;
            using ( Bitmap qr = GenerateQR( code ) )
            {
                if ( qr == null )
                {
                    throw new Exception( "Code Invalid" );
                }
                using ( MemoryStream stream = new MemoryStream() )
                {
                    qr.Save( stream, ImageFormat.Png );
                    buffer = stream.ToArray();
                }
            }

            var response = Request.CreateResponse( HttpStatusCode.OK );
            response.Content = new StreamContent( new MemoryStream( buffer ) );

            response.Content.Headers.ContentType = new MediaTypeHeaderValue( "image/png" );
            response.Content.Headers.ContentLength = buffer.Length;

            return response;
        }

        private static Bitmap GenerateQR( string code )
        {
            using ( QRCodeGenerator qrGenerator = new QRCodeGenerator() )
            using ( QRCodeData qrCodeData = qrGenerator.CreateQrCode( code, QRCodeGenerator.ECCLevel.Q ) )
            using ( QRCode qrCode = new QRCode( qrCodeData ) )
            {
                return qrCode.GetGraphic( 20 );
            }
        }
    }
}

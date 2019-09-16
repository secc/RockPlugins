using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using QRCoder;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest;
using Rock.Rest.Filters;
using Rock.Web.Cache;

namespace org.secc.QRManager.Rest.Controllers
{
    public partial class QRController : ApiControllerBase
    {

        [Authenticate]
        [HttpGet]
        [System.Web.Http.Route( "api/qr/{code}" )]
        public HttpResponseMessage GetEntityGuid( string code )
        {
            RockContext rockContext = new RockContext();
            PersonSearchKeyService personSearchKeyService = new PersonSearchKeyService( rockContext );
            var key = personSearchKeyService.Queryable().Where( k => k.SearchValue == code );
            if ( !key.Any() )
            {
                throw new Exception( "Invalid Key" );
            }

            var qr = GenerateQR( code );
            if ( qr == null )
            {
                throw new Exception( "Code Invalid" );
            }
            MemoryStream stream = new MemoryStream();
            qr.Save( stream, ImageFormat.Png );
            var buffer = stream.ToArray();
            var response = Request.CreateResponse( HttpStatusCode.OK );
            response.Content = new StreamContent( new MemoryStream( buffer ) );

            response.Content.Headers.ContentType = new MediaTypeHeaderValue( "image/png" );
            response.Content.Headers.ContentLength = stream.Length;

            return response;
        }

        private static Bitmap GenerateQR( string code )
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode( code, QRCodeGenerator.ECCLevel.Q );
            QRCode qrCode = new QRCode( qrCodeData );
            return qrCode.GetGraphic( 20 );
        }
    }
}

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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest;
using Rock.Rest.Filters;
using Rock.Web.Cache;

namespace org.secc.Imaging.Rest
{
    public partial class ImagingController : ApiControllerBase
    {

        [HttpGet]
        [System.Web.Http.Route( "api/imaging/test" )]
        public string test( [FromBody] Dictionary<string, string> data )
        {
            return "test";
        }

        [HttpPost]
        [System.Web.Http.Route( "api/imaging/generateimage" )]
        public HttpResponseMessage GenerateImage( [FromBody] Dictionary<string, string> data )
        {
            var content = data["Content"];
            int width;
            if ( !int.TryParse( data["Width"], out width ) )
            {
                width = 1920;
            };

            var image = HtmlToImage.GenerateImage( UnicodeEncode( content ), "png", width );

            var result = new HttpResponseMessage( HttpStatusCode.OK )
            {
                Content = new ByteArrayContent( image )
            };
            result.Content.Headers.ContentDisposition =
                new System.Net.Http.Headers.ContentDispositionHeaderValue( "attachment" )
                {
                    FileName = "image.png"
                };
            result.Content.Headers.ContentType =
                new MediaTypeHeaderValue( "image/png" );

            return result;
        }

        public string UnicodeEncode( string text )
        {
            char[] chars = text.ToCharArray();
            StringBuilder result = new StringBuilder( text.Length + ( int ) ( text.Length * 0.1 ) );

            foreach ( char c in chars )
            {
                int value = Convert.ToInt32( c );
                if ( value > 127 )
                    result.AppendFormat( "&#{0};", value );
                else
                    result.Append( c );
            }

            return result.ToString();
        }


    }
}

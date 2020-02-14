using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NReco.ImageGenerator;
using Rock;
using Rock.Web.Cache;

namespace org.secc.Imaging
{
    public class HtmlToImage
    {
        public static byte[] GenerateImage( string html, string format = "png", int? width = null, int? height = null )
        {
            var htmlToImageConv = new HtmlToImageConverter();

            if ( format == "png" )
            {
                htmlToImageConv.CustomArgs = "--transparent";
            }
            if ( width.HasValue )
            {
                htmlToImageConv.Width = width.Value;
            }

            if ( height.HasValue )
            {
                htmlToImageConv.Height = height.Value;
            }


            var image = htmlToImageConv.GenerateImage( html, format == "jpg" ? ImageFormat.Jpeg : ImageFormat.Png );

            return image;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ghostscript.NET.Rasterizer;
using Rock.Data;
using Rock.Model;

namespace org.secc.ConnectionCards.Utilities
{
    public static class ConnectionCardsUtilties
    {
        public static BinaryFile ConvertPDFToImage( BinaryFile inputFile )
        {
            int desired_x_dpi = 96;
            int desired_y_dpi = 96;

            using ( GhostscriptRasterizer rasterizer = new GhostscriptRasterizer() )
            {

                using ( MemoryStream ms = new MemoryStream( inputFile.DatabaseData.Content ) )
                {
                    rasterizer.Open( ms );
                    for ( int pageNumber = 1; pageNumber <= rasterizer.PageCount; pageNumber++ )
                    {
                        string filename = "ImageConvertedPDF.png";

                        Image img = rasterizer.GetPage( desired_x_dpi, desired_y_dpi, pageNumber );
                        using ( MemoryStream m = new MemoryStream() )
                        {
                            img.Save( m, ImageFormat.Png );
                            var data = m.ToArray();
                            var databaseData = new BinaryFileData()
                            {
                                Content = data
                            };
                            var outputFile = new BinaryFile()
                            {
                                FileName = filename,
                                MimeType = "image/png",
                                DatabaseData = databaseData,
                            };
                            return outputFile;
                        }
                    }
                }

            }
            return new BinaryFile();
        }

        public static BinaryFile RotateImage( BinaryFile inputFile, RotateFlipType rotateFlipType, RockContext rockContext )
        {
            using ( var inMS = new MemoryStream( inputFile.DatabaseData.Content ) )
            {
                Image image = Image.FromStream( inMS );
                image.RotateFlip( rotateFlipType );
                using ( var outMS = new MemoryStream() )
                {
                    image.Save( outMS, ImageFormat.Png );
                    inputFile.DatabaseData.Content = outMS.ToArray();
                    rockContext.SaveChanges();
                    return inputFile;
                }
            }
        }


        public static List<BinaryFile> ChopImage( BinaryFile inputFile, int cols, int rows, RockContext rockContext )
        {
            using ( MemoryStream ms = new MemoryStream( inputFile.DatabaseData.Content ) )
            {
                List<BinaryFile> output = new List<BinaryFile>();
                Image originalImage = Image.FromStream( ms );
                Bitmap sourceBitmap = new Bitmap( originalImage );
                int width = originalImage.Width;
                int height = originalImage.Height;
                int elementWidth = width / cols;
                int elementHeight = height / rows;
                for ( var x = 0; x < width; x += elementWidth )
                {
                    for ( var y = 0; y < height; y += elementHeight )
                    {
                        using ( MemoryStream outMS = new MemoryStream() )
                        {
                            var clone = sourceBitmap.Clone( new Rectangle( x + 4, y + 4, elementWidth - 4, elementHeight - 4 ), sourceBitmap.PixelFormat );
                            clone = Crop( clone );
                            clone.Save( outMS, ImageFormat.Png );
                            var data = outMS.ToArray();
                            var databaseData = new BinaryFileData()
                            {
                                Content = data
                            };
                            var element = new BinaryFile()
                            {
                                BinaryFileTypeId = inputFile.BinaryFileTypeId,
                                FileName = "9 Connection Card",
                                MimeType = "image/png",
                                DatabaseData = databaseData
                            };
                            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
                            binaryFileService.Add( element );
                            output.Add( element );
                        }
                    }
                }
                rockContext.SaveChanges();
                return output;
            }
        }

        public static Bitmap Crop( Bitmap bmp )
        {
            int w = bmp.Width;
            int h = bmp.Height;

            Func<int, bool> allWhiteRow = row =>
            {
                long total = 0;
                var divisor = 0;
                for ( int i = 0; i < w; ++i )
                {
                    total += bmp.GetPixel( i, row ).R;
                    divisor++;
                }
                if ( total / divisor > 235 )
                {
                    return true;
                }
                return false;
            };

            Func<int, bool> allWhiteColumn = col =>
            {
                long total = 0;
                var divisor = 0;
                for ( int i = 0; i < h; ++i )
                {
                    total += bmp.GetPixel( col, i ).R;
                    divisor++;
                }
                if ( total / divisor > 235 )
                {
                    return true;
                }
                return false;
            };

            int topmost = 0;
            for ( int row = 0; row < h; ++row )
            {
                if ( allWhiteRow( row ) )
                    topmost = row;
                else
                    break;
            }

            int bottommost = 0;
            for ( int row = h - 1; row >= 0; --row )
            {
                if ( allWhiteRow( row ) )
                    bottommost = row;
                else
                    break;
            }

            int leftmost = 0, rightmost = 0;
            for ( int col = 0; col < w; ++col )
            {
                if ( allWhiteColumn( col ) )
                    leftmost = col;
                else
                    break;
            }

            for ( int col = w - 1; col >= 0; --col )
            {
                if ( allWhiteColumn( col ) )
                    rightmost = col;
                else
                    break;
            }

            if ( rightmost == 0 )
                rightmost = w; // As reached left
            if ( bottommost == 0 )
                bottommost = h; // As reached top.

            int croppedWidth = rightmost - leftmost;
            int croppedHeight = bottommost - topmost;

            if ( croppedWidth == 0 ) // No border on left or right
            {
                leftmost = 0;
                croppedWidth = w;
            }

            if ( croppedHeight == 0 ) // No border on top or bottom
            {
                topmost = 0;
                croppedHeight = h;
            }

            try
            {
                var target = new Bitmap( croppedWidth, croppedHeight );
                using ( Graphics g = Graphics.FromImage( target ) )
                {
                    g.DrawImage( bmp,
                      new RectangleF( 0, 0, croppedWidth, croppedHeight ),
                      new RectangleF( leftmost, topmost, croppedWidth, croppedHeight ),
                      GraphicsUnit.Pixel );
                }
                return target;
            }
            catch ( Exception ex )
            {
                throw new Exception(
                  string.Format( "Values are topmost={0} btm={1} left={2} right={3} croppedWidth={4} croppedHeight={5}", topmost, bottommost, leftmost, rightmost, croppedWidth, croppedHeight ),
                  ex );
            }
        }

    }
}

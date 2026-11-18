// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
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
using Ghostscript.NET.Rasterizer;
using org.secc.DevLib.Extensions;
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

            // ROCK-9041: Copy the content into memory through a fresh provider stream (disposed) so the
            // caller can delete the source BinaryFile without a provider handle still open on it.
            byte[] pdfBytes = inputFile.ReadContentBytes( "Connection card sheet PDF" );

            using ( GhostscriptRasterizer rasterizer = new GhostscriptRasterizer() )
            using ( MemoryStream pdfStream = new MemoryStream( pdfBytes ) )
            {
                rasterizer.Open( pdfStream );
                if ( rasterizer.PageCount > 0 )
                {
                    string filename = "ImageConvertedPDF.png";

                    using ( Image img = rasterizer.GetPage( desired_x_dpi, desired_y_dpi, 1 ) )
                    using ( MemoryStream m = new MemoryStream() )
                    {
                        img.Save( m, ImageFormat.Png );
                        var data = m.ToArray();

                        // ROCK-9041: Assign ContentStream (not DatabaseData) so whichever storage
                        // provider the file type uses receives the content on save.
                        var outputFile = new BinaryFile()
                        {
                            FileName = filename,
                            MimeType = "image/png",
                            FileSize = data.Length,
                            ContentStream = new MemoryStream( data ),
                        };
                        return outputFile;
                    }
                }
            }

            // No pages: return null so the caller's null check handles it. An empty BinaryFile (no MimeType)
            // made Rock's save hook throw a NullReferenceException.
            return null;
        }

        public static BinaryFile RotateImage( BinaryFile inputFile, RotateFlipType rotateFlipType, RockContext rockContext )
        {
            // ROCK-9041: Read through a fresh provider stream (disposed) and dispose the Image; the old code
            // left both open.
            byte[] imageBytes = inputFile.ReadContentBytes( "Connection card sheet image" );

            using ( MemoryStream inMS = new MemoryStream( imageBytes ) )
            using ( Image image = Image.FromStream( inMS ) )
            using ( var outMS = new MemoryStream() )
            {
                image.RotateFlip( rotateFlipType );
                image.Save( outMS, ImageFormat.Png );

                // Copy the PNG out so the stream handed to the provider outlives this using block.
                var data = outMS.ToArray();
                inputFile.FileSize = data.Length;
                inputFile.ContentStream = new MemoryStream( data );
                rockContext.SaveChanges();
                return inputFile;
            }
        }


        public static List<BinaryFile> ChopImage( BinaryFile inputFile, int cols, int rows, RockContext rockContext )
        {
            // ROCK-9041: Read through the storage provider so this works for any provider,
            // not just Database (DatabaseData is null for files stored elsewhere).
            byte[] imageBytes = inputFile.ReadContentBytes( "Connection card sheet image" );

            using ( MemoryStream ms = new MemoryStream( imageBytes ) )
            using ( Image originalImage = Image.FromStream( ms ) )
            using ( Bitmap sourceBitmap = new Bitmap( originalImage ) )
            {
                // Fail loudly on a bad grid before anything is saved, so the caller keeps the source scan.
                // The 4px inset below needs each cell to be more than 4px in both directions.
                if ( cols < 1 || rows < 1 )
                {
                    throw new InvalidOperationException( string.Format( "Rows and Columns must both be at least 1 (got {0} rows, {1} columns).", rows, cols ) );
                }

                int elementWidth = sourceBitmap.Width / cols;
                int elementHeight = sourceBitmap.Height / rows;
                if ( elementWidth <= 4 || elementHeight <= 4 )
                {
                    throw new InvalidOperationException( string.Format( "A {0} x {1} grid is too fine for this {2} x {3} pixel sheet.", rows, cols, sourceBitmap.Height, sourceBitmap.Width ) );
                }

                List<BinaryFile> output = new List<BinaryFile>();

                // Exactly cols x rows cells. Stepping x/y by the element size until the edge added an extra
                // partial column/row when the size wasn't evenly divisible, and its rectangle ran past the
                // bitmap (Clone throws OutOfMemoryException). Integer division keeps every cell inside the
                // bitmap. The 4px inset trims scan borders between cards.
                for ( var col = 0; col < cols; col++ )
                {
                    for ( var row = 0; row < rows; row++ )
                    {
                        var cell = new Rectangle( col * elementWidth + 4, row * elementHeight + 4, elementWidth - 4, elementHeight - 4 );

                        using ( MemoryStream outMS = new MemoryStream() )
                        using ( Bitmap clone = sourceBitmap.Clone( cell, sourceBitmap.PixelFormat ) )
                        using ( Bitmap cropped = Crop( clone ) )
                        {
                            cropped.Save( outMS, ImageFormat.Png );
                            var data = outMS.ToArray();
                            var element = new BinaryFile()
                            {
                                BinaryFileTypeId = inputFile.BinaryFileTypeId,
                                FileName = "Connection Card",
                                MimeType = "image/png",
                                FileSize = data.Length,
                                ContentStream = new MemoryStream( data )
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

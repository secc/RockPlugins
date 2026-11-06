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
using System.IO;

using org.secc.LinkList.Services;
using org.secc.LinkList.SystemGuids;
using org.secc.LinkList.Utility;
using org.secc.LinkList.ViewModels;

using Xunit;

namespace org.secc.LinkList.Tests
{
    /// <summary>
    /// ROCK-9362: uploaded link files. The frontend mirror (linkListFiles.ts) pins the
    /// same allow-list / cap / type guid in tests/linkListFiles.spec.ts.
    /// </summary>
    public class LinkListFilesTests
    {
        private static readonly Guid ListGuid = Guid.Parse( "0b6f5a1e-2c3d-4e5f-8a9b-1c2d3e4f5a6b" );
        private static readonly Guid OtherListGuid = Guid.Parse( "1c7a6b2f-3d4e-4f60-9b0c-2d3e4f5a6b7c" );
        private static readonly Guid FileGuid = Guid.Parse( "7d8e9fa0-1b2c-4d3e-9f40-5a6b7c8d9e0f" );

        private static readonly byte[] PdfHeader = { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37 };
        private static readonly byte[] JpegHeader = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };
        private static readonly byte[] PngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] HtmlHeader = { 0x3C, 0x68, 0x74, 0x6D, 0x6C, 0x3E, 0x3C, 0x73 }; // "<html><s"

        [Fact]
        public void Constants_Match_Frontend_Mirror()
        {
            Assert.Equal( 50 * 1024 * 1024, LinkListFiles.MaxFileSizeBytes );
            Assert.Equal( "D3C59EA0-0CD7-42CD-817F-2937C31F5A86", LinkListGuids.BinaryFileTypeLinkListFile );
        }

        // ---- URL shape ----

        [Fact]
        public void BuildFileUrl_Is_Relative_And_Round_Trips()
        {
            var url = LinkListFiles.BuildFileUrl( ListGuid, FileGuid );
            Assert.Equal( $"/api/secc/linklist/{ListGuid:D}/file/{FileGuid:D}", url );
            Assert.Equal( FileGuid, LinkListFiles.TryParseFileGuid( url, ListGuid ) );
        }

        [Fact]
        public void TryParseFileGuid_Accepts_Uppercase_And_Surrounding_Whitespace()
        {
            var url = $"  /api/secc/linklist/{ListGuid.ToString().ToUpperInvariant()}/file/{FileGuid.ToString().ToUpperInvariant()} ";
            Assert.Equal( FileGuid, LinkListFiles.TryParseFileGuid( url, ListGuid ) );
        }

        [Fact]
        public void TryParseFileGuid_Ignores_Another_Lists_File_Url()
        {
            // Pasted into a different list it stays an ordinary link, gated by its owner.
            var url = LinkListFiles.BuildFileUrl( OtherListGuid, FileGuid );
            Assert.Null( LinkListFiles.TryParseFileGuid( url, ListGuid ) );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        [InlineData( "https://example.org/flyer.pdf" )]
        [InlineData( "/GetFile.ashx?guid=7d8e9fa0-1b2c-4d3e-9f40-5a6b7c8d9e0f" )]
        [InlineData( "https://rock.example.org/api/secc/linklist/0b6f5a1e-2c3d-4e5f-8a9b-1c2d3e4f5a6b/file/7d8e9fa0-1b2c-4d3e-9f40-5a6b7c8d9e0f" )]
        [InlineData( "/api/secc/linklist/0b6f5a1e-2c3d-4e5f-8a9b-1c2d3e4f5a6b/file/7d8e9fa0-1b2c-4d3e-9f40-5a6b7c8d9e0f?x=1" )]
        [InlineData( "/api/secc/linklist/0b6f5a1e-2c3d-4e5f-8a9b-1c2d3e4f5a6b/file/not-a-guid" )]
        public void TryParseFileGuid_Rejects_Other_Shapes( string url )
        {
            Assert.Null( LinkListFiles.TryParseFileGuid( url, ListGuid ) );
        }

        // ---- ResolveFileGuid (bag -> file) ----

        [Fact]
        public void ResolveFileGuid_Prefers_Explicit_FileGuid()
        {
            var bag = new LinkItemBag { ItemType = "link", FileGuid = FileGuid.ToString(), Url = "https://ignored.example" };
            Assert.Equal( FileGuid, LinkListService.ResolveFileGuid( bag, ListGuid ) );
        }

        [Fact]
        public void ResolveFileGuid_Falls_Back_To_This_Lists_File_Url()
        {
            var bag = new LinkItemBag { ItemType = "link", Url = LinkListFiles.BuildFileUrl( ListGuid, FileGuid ) };
            Assert.Equal( FileGuid, LinkListService.ResolveFileGuid( bag, ListGuid ) );
        }

        [Theory]
        [InlineData( "section" )]
        [InlineData( "separator" )]
        public void ResolveFileGuid_Ignores_Non_Link_Rows( string itemType )
        {
            var bag = new LinkItemBag { ItemType = itemType, FileGuid = FileGuid.ToString() };
            Assert.Null( LinkListService.ResolveFileGuid( bag, ListGuid ) );
        }

        [Fact]
        public void ResolveFileGuid_Treats_Unknown_Type_As_Link_Like_Persist_Does()
        {
            var bag = new LinkItemBag { ItemType = "bogus", FileGuid = FileGuid.ToString() };
            Assert.Equal( FileGuid, LinkListService.ResolveFileGuid( bag, ListGuid ) );
        }

        [Fact]
        public void ReferencedFileGuids_Is_Distinct_And_Skips_Plain_Links()
        {
            var rows = new[]
            {
                new LinkItemBag { ItemType = "link", FileGuid = FileGuid.ToString() },
                new LinkItemBag { ItemType = "link", Url = LinkListFiles.BuildFileUrl( ListGuid, FileGuid ) },
                new LinkItemBag { ItemType = "link", Url = "https://example.org" },
                null
            };
            var result = LinkListService.ReferencedFileGuids( rows, ListGuid );
            Assert.Single( result );
            Assert.Contains( FileGuid, result );
        }

        // ---- metadata ----

        [Theory]
        [InlineData( "flyer.pdf", "application/pdf", "application/pdf" )]
        [InlineData( "Flyer.PDF", "application/pdf", "application/pdf" )]
        [InlineData( "photo.jpg", "image/jpeg", "image/jpeg" )]
        [InlineData( "photo.jpeg", "image/pjpeg", "image/jpeg" )]
        [InlineData( "logo.png", "image/png", "image/png" )]
        [InlineData( "logo.png", "", "image/png" )]
        [InlineData( "flyer.pdf", "application/pdf; charset=binary", "application/pdf" )]
        public void ValidateMetadata_Accepts_Allowed_Types( string name, string mime, string expectedContentType )
        {
            Assert.Null( LinkListFiles.ValidateMetadata( name, mime, 1024 ) );
            Assert.Equal( expectedContentType, LinkListFiles.GetContentType( name ) );
        }

        [Theory]
        [InlineData( "evil.svg", "image/svg+xml" )]
        [InlineData( "page.html", "text/html" )]
        [InlineData( "doc.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" )]
        [InlineData( "flyer.pdf.exe", "application/octet-stream" )]
        [InlineData( "noext", "application/pdf" )]
        [InlineData( "", "application/pdf" )]
        public void ValidateMetadata_Rejects_Disallowed_Extensions( string name, string mime )
        {
            Assert.Contains( "not an allowed file type", LinkListFiles.ValidateMetadata( name, mime, 1024 ) );
            Assert.Null( LinkListFiles.GetContentType( name ) );
        }

        [Theory]
        [InlineData( "flyer.pdf", "text/html" )]
        [InlineData( "photo.jpg", "image/png" )]
        [InlineData( "logo.png", "image/svg+xml" )]
        public void ValidateMetadata_Rejects_Mime_Mismatch( string name, string mime )
        {
            Assert.Contains( "does not look like", LinkListFiles.ValidateMetadata( name, mime, 1024 ) );
        }

        [Fact]
        public void ValidateMetadata_Enforces_Size_Cap()
        {
            Assert.Null( LinkListFiles.ValidateMetadata( "max.pdf", "application/pdf", LinkListFiles.MaxFileSizeBytes ) );
            Assert.Contains( "50 MB limit", LinkListFiles.ValidateMetadata( "big.pdf", "application/pdf", LinkListFiles.MaxFileSizeBytes + 1L ) );
            Assert.Contains( "empty", LinkListFiles.ValidateMetadata( "empty.pdf", "application/pdf", 0 ) );
        }

        // ---- file names ----

        [Theory]
        [InlineData( "flyer.pdf", "flyer.pdf" )]
        [InlineData( @"C:\Users\me\Desktop\flyer.pdf", "flyer.pdf" )]
        [InlineData( "../../etc/flyer.pdf", "flyer.pdf" )]
        [InlineData( "fly<er>|?.pdf", "fly_er___.pdf" )]
        [InlineData( "  spaced name.pdf  ", "spaced name.pdf" )]
        [InlineData( "trailing.pdf. . ", "trailing.pdf" )]
        [InlineData( "tab\tname.png", "tab_name.png" )]
        [InlineData( "", "" )]
        [InlineData( null, "" )]
        public void CleanFileName_Strips_Paths_And_Unsafe_Characters( string input, string expected )
        {
            Assert.Equal( expected, LinkListFiles.CleanFileName( input ) );
        }

        [Fact]
        public void CleanFileName_Caps_Length_And_Keeps_Extension()
        {
            var cleaned = LinkListFiles.CleanFileName( new string( 'a', 500 ) + ".pdf" );
            Assert.Equal( LinkListFiles.MaxFileNameLength, cleaned.Length );
            Assert.EndsWith( ".pdf", cleaned );
            Assert.Equal( "application/pdf", LinkListFiles.GetContentType( cleaned ) );
        }

        [Theory]
        [InlineData( "a<b>.pdf", "pdf" )]
        [InlineData( "a|b.PNG", "png" )]
        [InlineData( "dir.v2/noext", "" )]
        [InlineData( "trailingdot.", "" )]
        [InlineData( ".pdf", "pdf" )]
        public void GetExtension_Handles_Characters_That_Path_Rejects( string name, string expected )
        {
            // .NET Framework's Path.GetExtension throws on '<' or '|'; ours must not.
            Assert.Equal( expected, LinkListFiles.GetExtension( name ) );
        }

        // ---- magic bytes ----

        [Fact]
        public void MatchesSignature_Accepts_Real_Headers()
        {
            Assert.True( LinkListFiles.MatchesSignature( "a.pdf", PdfHeader ) );
            Assert.True( LinkListFiles.MatchesSignature( "a.jpg", JpegHeader ) );
            Assert.True( LinkListFiles.MatchesSignature( "a.jpeg", JpegHeader ) );
            Assert.True( LinkListFiles.MatchesSignature( "a.png", PngHeader ) );
        }

        [Fact]
        public void MatchesSignature_Rejects_Renamed_Or_Truncated_Files()
        {
            Assert.False( LinkListFiles.MatchesSignature( "a.pdf", HtmlHeader ) );
            Assert.False( LinkListFiles.MatchesSignature( "a.pdf", PngHeader ) );
            Assert.False( LinkListFiles.MatchesSignature( "a.png", JpegHeader ) );
            Assert.False( LinkListFiles.MatchesSignature( "a.png", new byte[] { 0x89, 0x50 } ) );
            Assert.False( LinkListFiles.MatchesSignature( "a.pdf", new byte[0] ) );
            Assert.False( LinkListFiles.MatchesSignature( "a.pdf", null ) );
            Assert.False( LinkListFiles.MatchesSignature( "a.svg", PdfHeader ) );
        }

        [Fact]
        public void ReadSignature_Reads_Across_Short_Reads_And_Short_Files()
        {
            using ( var stream = new OneByteStream( PngHeader ) )
            {
                Assert.Equal( PngHeader, LinkListFiles.ReadSignature( stream ) );
            }
            using ( var stream = new MemoryStream( new byte[] { 0x25, 0x50 } ) )
            {
                Assert.Equal( new byte[] { 0x25, 0x50 }, LinkListFiles.ReadSignature( stream ) );
            }
            Assert.Null( LinkListFiles.ReadSignature( null ) );
        }

        /// <summary>Returns at most one byte per Read, like a slow network stream.</summary>
        private sealed class OneByteStream : MemoryStream
        {
            public OneByteStream( byte[] buffer ) : base( buffer ) { }

            public override int Read( byte[] buffer, int offset, int count )
            {
                return base.Read( buffer, offset, Math.Min( 1, count ) );
            }
        }
    }
}

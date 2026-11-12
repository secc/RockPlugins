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
using System.IO;
using System.Text.RegularExpressions;

namespace org.secc.LinkList.Utility
{
    /// <summary>
    /// ROCK-9362: pure rules for uploaded link files (PDF / JPG / PNG) - the allow-list,
    /// size cap, magic-byte check, and the URL shape a file link is stored under. No
    /// RockContext, so it is unit tested directly. The frontend mirrors the allow-list
    /// and size cap in linkListFiles.ts for early feedback only; these are the rules
    /// that are enforced.
    ///
    /// A file link's URL attribute holds the RELATIVE path
    /// <c>/api/secc/linklist/{listGuid}/file/{fileGuid}</c>. Relative so a database
    /// copied between environments keeps working; the web component prefixes its
    /// base-url for external embeds. The list guid in the path is what the file
    /// endpoint gates on, so a file is exactly as public as the list that owns it.
    /// </summary>
    public static class LinkListFiles
    {
        /// <summary>
        /// 50 MB. Sized from production (2026-11): the PDFs already linked from Link Lists
        /// via GetFile.ashx top out at 40.7 MB (full study guides; next 29.3, 19.9, 15.2),
        /// while the PDF p99 across content file types is ~12 MB. Under IIS's 100 MB
        /// request cap. Also written to the BinaryFileType (migration 013).
        /// </summary>
        public const int MaxFileSizeBytes = 50 * 1024 * 1024;

        /// <summary>Extension (lowercase, no dot) -> the only Content-Type the file is ever served as.</summary>
        private static readonly Dictionary<string, string> ContentTypeByExtension =
            new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase )
            {
                ["pdf"] = "application/pdf",
                ["jpg"] = "image/jpeg",
                ["jpeg"] = "image/jpeg",
                ["png"] = "image/png"
            };

        /// <summary>
        /// MIME types a browser may report for an allowed extension at upload time.
        /// image/pjpeg is old IE's progressive-JPEG type, which Rock's uploader also
        /// remaps; application/x-pdf is seen from some older clients.
        /// </summary>
        private static readonly Dictionary<string, string[]> AcceptedMimeTypesByContentType =
            new Dictionary<string, string[]>( StringComparer.OrdinalIgnoreCase )
            {
                ["application/pdf"] = new[] { "application/pdf", "application/x-pdf" },
                ["image/jpeg"] = new[] { "image/jpeg", "image/pjpeg", "image/jpg" },
                ["image/png"] = new[] { "image/png", "image/x-png" }
            };

        /// <summary>Human list for error messages.</summary>
        public const string AllowedTypesDescription = "PDF, JPG, or PNG";

        /// <summary>Number of leading bytes <see cref="MatchesSignature"/> needs.</summary>
        public const int SignatureLength = 8;

        private static readonly Regex FileUrlPattern = new Regex(
            @"^/api/secc/linklist/(?<list>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})/file/(?<file>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})$",
            RegexOptions.CultureInvariant );

        /// <summary>The relative URL a file link is stored (and rendered) under.</summary>
        public static string BuildFileUrl( Guid listGuid, Guid fileGuid )
        {
            return $"/api/secc/linklist/{listGuid:D}/file/{fileGuid:D}";
        }

        /// <summary>
        /// The file guid in <paramref name="url"/> when it is a file URL for THIS list,
        /// else null. A file URL that names a different list is deliberately not
        /// claimed: pasted into another list it stays an ordinary link, still gated by
        /// the list that owns the file.
        /// </summary>
        public static Guid? TryParseFileGuid( string url, Guid listGuid )
        {
            if ( string.IsNullOrWhiteSpace( url ) )
            {
                return null;
            }

            var match = FileUrlPattern.Match( url.Trim() );
            if ( !match.Success || Guid.Parse( match.Groups["list"].Value ) != listGuid )
            {
                return null;
            }
            return Guid.Parse( match.Groups["file"].Value );
        }

        /// <summary>
        /// Slack allowed on top of <see cref="MaxFileSizeBytes"/> for the multipart
        /// envelope when the upload endpoint pre-checks the request's Content-Length.
        /// </summary>
        public const int MultipartOverheadBytes = 64 * 1024;

        /// <summary>Longest file name kept (BinaryFile.FileName is nvarchar(255)).</summary>
        public const int MaxFileNameLength = 200;

        private static readonly Regex UnsafeFileNameChars = new Regex( @"[<>:""/\\|?*\x00-\x1F\x7F]", RegexOptions.CultureInvariant );

        /// <summary>
        /// A safe display/storage name for an uploaded file: the last path segment only
        /// (old browsers send a full client path), without the characters Rock's own
        /// uploader rejects or control characters, trailing dots/spaces trimmed, and
        /// capped at <see cref="MaxFileNameLength"/> while keeping the extension.
        /// Empty when nothing usable is left.
        /// </summary>
        public static string CleanFileName( string fileName )
        {
            if ( string.IsNullOrWhiteSpace( fileName ) )
            {
                return string.Empty;
            }

            var name = fileName.Replace( '\\', '/' );
            var slash = name.LastIndexOf( '/' );
            if ( slash >= 0 )
            {
                name = name.Substring( slash + 1 );
            }

            name = UnsafeFileNameChars.Replace( name, "_" ).Trim().TrimEnd( '.', ' ' );
            if ( name.Length > MaxFileNameLength )
            {
                var ext = DottedExtension( name );
                if ( ext.Length >= MaxFileNameLength )
                {
                    ext = string.Empty;
                }
                name = name.Substring( 0, MaxFileNameLength - ext.Length ).TrimEnd( '.', ' ' ) + ext;
            }
            return name;
        }

        /// <summary>Lowercase extension without the dot, or empty.</summary>
        public static string GetExtension( string fileName )
        {
            if ( string.IsNullOrWhiteSpace( fileName ) )
            {
                return string.Empty;
            }
            // Not Path.GetExtension: on .NET Framework it throws on characters such as
            // '<' or '|', and this runs on client-supplied names.
            var ext = DottedExtension( fileName.Trim() );
            return ext.Length == 0 ? string.Empty : ext.Substring( 1 ).ToLowerInvariant();
        }

        /// <summary>".pdf"-style extension of the last path segment, or empty.</summary>
        private static string DottedExtension( string name )
        {
            var segmentStart = Math.Max( name.LastIndexOf( '/' ), name.LastIndexOf( '\\' ) ) + 1;
            var dot = name.LastIndexOf( '.' );
            return dot >= segmentStart && dot < name.Length - 1 ? name.Substring( dot ) : string.Empty;
        }

        /// <summary>
        /// The Content-Type to serve <paramref name="fileName"/> as, decided by the
        /// extension allow-list (never the stored, client-reported MIME type), or null
        /// when the extension is not allowed.
        /// </summary>
        public static string GetContentType( string fileName )
        {
            return ContentTypeByExtension.TryGetValue( GetExtension( fileName ), out var contentType )
                ? contentType
                : null;
        }

        /// <summary>
        /// Validates an uploaded file's metadata: allowed extension, a reported MIME
        /// type consistent with it, and the size cap. Returns an error message, or
        /// null when the file is acceptable. A blank reported MIME type is tolerated
        /// (some clients send none); the magic-byte check still applies.
        /// </summary>
        public static string ValidateMetadata( string fileName, string mimeType, long? fileSize )
        {
            var contentType = GetContentType( fileName );
            if ( contentType == null )
            {
                return $"'{fileName}' is not an allowed file type. Upload a {AllowedTypesDescription} file.";
            }

            if ( !string.IsNullOrWhiteSpace( mimeType ) )
            {
                var reported = mimeType.Split( ';' )[0].Trim();
                if ( Array.IndexOf( AcceptedMimeTypesByContentType[contentType], reported.ToLowerInvariant() ) < 0 )
                {
                    return $"'{fileName}' does not look like a {AllowedTypesDescription} file.";
                }
            }

            if ( fileSize.HasValue && fileSize.Value > MaxFileSizeBytes )
            {
                return $"'{fileName}' is larger than the {MaxFileSizeBytes / ( 1024 * 1024 )} MB limit.";
            }
            if ( fileSize.HasValue && fileSize.Value <= 0 )
            {
                return $"'{fileName}' is empty.";
            }

            return null;
        }

        /// <summary>
        /// True when <paramref name="header"/> (the file's first bytes) carries the
        /// signature of the type its extension claims: "%PDF-" for PDF, FF D8 FF for
        /// JPEG, the 8-byte PNG signature for PNG. Stops a renamed HTML/SVG/script from
        /// being stored as a "PDF" (it is also always served with nosniff and the
        /// allow-listed Content-Type).
        /// </summary>
        public static bool MatchesSignature( string fileName, byte[] header )
        {
            if ( header == null )
            {
                return false;
            }

            switch ( GetContentType( fileName ) )
            {
                case "application/pdf":
                    return StartsWith( header, 0x25, 0x50, 0x44, 0x46, 0x2D );
                case "image/jpeg":
                    return StartsWith( header, 0xFF, 0xD8, 0xFF );
                case "image/png":
                    return StartsWith( header, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A );
                default:
                    return false;
            }
        }

        /// <summary>
        /// Reads up to <see cref="SignatureLength"/> bytes from the start of
        /// <paramref name="stream"/>. Loops because a network stream (Azure blob) may
        /// return fewer bytes than asked per Read. Does not dispose the stream.
        /// </summary>
        public static byte[] ReadSignature( Stream stream )
        {
            if ( stream == null )
            {
                return null;
            }

            var buffer = new byte[SignatureLength];
            var total = 0;
            while ( total < buffer.Length )
            {
                var read = stream.Read( buffer, total, buffer.Length - total );
                if ( read <= 0 )
                {
                    break;
                }
                total += read;
            }

            if ( total == buffer.Length )
            {
                return buffer;
            }
            var shortBuffer = new byte[total];
            Array.Copy( buffer, shortBuffer, total );
            return shortBuffer;
        }

        private static bool StartsWith( byte[] header, params byte[] signature )
        {
            if ( header.Length < signature.Length )
            {
                return false;
            }
            for ( var i = 0; i < signature.Length; i++ )
            {
                if ( header[i] != signature[i] )
                {
                    return false;
                }
            }
            return true;
        }
    }
}

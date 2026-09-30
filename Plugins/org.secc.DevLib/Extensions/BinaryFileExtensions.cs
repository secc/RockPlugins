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
using Rock;
using Rock.Model;

namespace org.secc.DevLib.Extensions
{
    public static class BinaryFileExtensions
    {
        /// <summary>
        /// Reads a binary file's content into a byte array, for any storage provider.
        /// </summary>
        /// <remarks>
        /// ROCK-9041: Stored files are read through a fresh storage provider stream that is disposed here.
        /// Disposing <see cref="BinaryFile.ContentStream"/> instead is unsafe: the entity caches that stream
        /// and only re-fetches it when CanSeek is false, and Azure's blob stream still reports CanSeek after
        /// Dispose, so any later read of the same tracked BinaryFile would get a disposed stream back.
        /// Unsaved files (Id == 0) are read from their in-memory ContentStream, which the caller owns and
        /// which is left open. Whether a file is unsaved is decided by Id, not by StorageProvider: calling
        /// SetStorageEntityTypeId on a new file sets StorageProvider before the content is stored.
        /// Because stored files are always read from the provider, save first if you have assigned a new
        /// ContentStream to a tracked file; otherwise this returns the previously stored bytes.
        /// Exceptions thrown by the storage provider itself (IO, Azure, ...) are not caught here.
        /// </remarks>
        /// <param name="binaryFile">The binary file.</param>
        /// <param name="description">A short name for the file used in error messages, e.g. "PDF Template".</param>
        /// <returns>The file content. Never null or empty.</returns>
        /// <exception cref="InvalidOperationException">The file is null or has no content.</exception>
        public static byte[] ReadContentBytes( this BinaryFile binaryFile, string description )
        {
            if ( binaryFile == null )
            {
                throw new InvalidOperationException( description + " binary file was not found." );
            }

            byte[] bytes;
            if ( binaryFile.Id != 0 && binaryFile.StorageProvider != null )
            {
                using ( var stream = binaryFile.StorageProvider.GetContentStream( binaryFile ) )
                {
                    bytes = stream?.ReadBytesToEnd();
                }
            }
            else
            {
                bytes = binaryFile.ContentStream?.ReadBytesToEnd();
            }

            if ( bytes == null || bytes.Length == 0 )
            {
                throw new InvalidOperationException( string.Format( "{0} has no content or its storage is unavailable. Guid: {1}", description, binaryFile.Guid ) );
            }

            return bytes;
        }
    }
}

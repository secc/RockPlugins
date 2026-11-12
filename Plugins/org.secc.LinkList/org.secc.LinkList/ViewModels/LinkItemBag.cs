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
namespace org.secc.LinkList.ViewModels
{
    public class LinkItemBag
    {
        public string Guid { get; set; }

        public string MatrixItemGuid { get; set; }

        public string ItemType { get; set; }

        public string Text { get; set; }

        public string Url { get; set; }

        public string Target { get; set; }

        public int IndentLevel { get; set; }

        public bool IsSectionCollapsed { get; set; }

        /// <summary>WS9: optional secondary line under a link's text. Plain text (escaped), link rows only.</summary>
        public string Subtitle { get; set; }

        /// <summary>WS9: optional blurb under a section heading. Plain text (escaped), section rows only.</summary>
        public string Description { get; set; }

        /// <summary>WS10: when true, this link is the list's featured button (max one; hoisted to the top at render time).</summary>
        public bool IsFeatured { get; set; }

        /// <summary>
        /// ROCK-9362: BinaryFile GUID when this link points at an uploaded file (PDF/JPG/PNG)
        /// instead of a typed URL. On save the server derives <see cref="Url"/> from it
        /// (<c>/api/secc/linklist/{list}/file/{file}</c>); on read it is parsed back out of
        /// the stored URL. Link rows only.
        /// </summary>
        public string FileGuid { get; set; }

        /// <summary>ROCK-9362: display name of the uploaded file (read-only; ignored on save).</summary>
        public string FileName { get; set; }

        public int Order { get; set; }
    }
}

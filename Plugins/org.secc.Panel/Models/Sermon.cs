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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.Panel.Models
{
    class Sermon
    {
        public int id { get; set; }
        public string title { get; set; }
        public string slug { get; set; }
        public string description { get; set; }
        public string speaker { get; set; }
        public int duration { get; set; }
        public int kitclub_id { get; set; }
        public long vimeo_id { get; set; }
        public string vimeo_sd_url { get; set; }
        public string vimeo_live_url { get; set; }
        public string audio_link { get; set; }
        public string audio_size { get; set; }
        public int series { get; set; }
        public int campus { get; set; }
        public int podcast_views { get; set; }
        public long date { get; set; }
        public int notes_downloaded { get; set; }
        public int questions_downloaded { get; set; }
        public bool deleted { get; set; }
        public override string ToString()
        {
            return this.title;
        }
    }
}

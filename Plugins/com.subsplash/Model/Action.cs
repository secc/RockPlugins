// <copyright>
// MIT License
//
// Copyright( c) 2020 Subsplash

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software. 

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// </copyright>
//
using Newtonsoft.Json;

namespace com.subsplash.Model
{
    public class Action
    {
        [JsonProperty( "handlerType" )]
        public string HandlerType { get; set; }

        [JsonProperty( "url" )]
        public string Url { get; set; }

        [JsonProperty( "contentUrl" )]
        public string ContentUrl { get; set; }

        [JsonProperty( "selectedIndex" )]
        public int? SelectedIndex { get; set; }

        [JsonProperty( "style" )]
        public string Style { get; set; }

        [JsonProperty( "showBrowserControls" )]
        public bool? ShowBrowserControls { get; set; }

        [JsonProperty( "address" )]
        public string Address { get; set; }

        [JsonProperty( "subject" )]
        public string Subject { get; set; }

        [JsonProperty( "body" )]
        public string Body { get; set; }

        [JsonProperty( "number" )]
        public string Number { get; set; }

        [JsonProperty( "json" )]
        public string Json { get; set; }
    }
}

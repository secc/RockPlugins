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
using System;
using System.ComponentModel;
using Rock;
using Rock.Model;
using Rock.Security;
using System.Web.UI;
using Rock.Web.Cache;
using Rock.Web.UI;
using System.Web;
using Rock.Attribute;
using Rock.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Rock.Web.UI.Controls;
using Rock.Lava;

namespace RockWeb.Plugins.org_secc.CMS
{
    [DisplayName( "Twitter Lava" )]
    [Category( "SECC > CMS" )]
    [Description( "Header Parallax Title" )]

    [TextField(
        "OAuth Consumer Key",
        Key = AttributeKeys.OAuthConsumerKey
        )]

    [TextField(
        "OAuth Consumer Secret",
        Key = AttributeKeys.OAuthConsumerSecret
        )]

    [TextField(
        "Screen Name",
        Key = AttributeKeys.ScreenName
        )]

    [IntegerField(
        "Number of Tweets to Pull",
        Key = AttributeKeys.TweetCount,
        DefaultValue = "5"
        )]

    [CodeEditorField(
        "Lava Template",
       key: AttributeKeys.LavaTemplate,
        mode: CodeEditorMode.Lava,
        height: 600
        )]

    public partial class TwitterLava : Rock.Web.UI.RockBlock
    {
        internal static class AttributeKeys
        {
            internal const string OAuthConsumerKey = "OAuthConsumerKey";
            internal const string OAuthConsumerSecret = "OAuthConsumerSecret";
            internal const string ScreenName = "ScreenName";
            internal const string LavaTemplate = "LavaTemplate";
            internal const string TweetCount = "TweetCount";
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                ltText.Text = GetTwitter();
            }
        }

        private string GetTwitter()
        {
            // You need to set your own keys and screen name
            var oAuthConsumerKey = GetAttributeValue( AttributeKeys.OAuthConsumerKey );
            var oAuthConsumerSecret = GetAttributeValue( AttributeKeys.OAuthConsumerSecret );
            var oAuthUrl = "https://api.twitter.com/oauth2/token";
            var screenname = GetAttributeValue( AttributeKeys.ScreenName );

            if ( oAuthConsumerKey.IsNullOrWhiteSpace()
                || oAuthConsumerSecret.IsNullOrWhiteSpace()
                || screenname.IsNullOrWhiteSpace() )
            {
                return "";
            }

            // Do the Authenticate
            var authHeaderFormat = "Basic {0}";

            var authHeader = string.Format( authHeaderFormat,
                Convert.ToBase64String( Encoding.UTF8.GetBytes( Uri.EscapeDataString( oAuthConsumerKey ) + ":" +
                Uri.EscapeDataString( ( oAuthConsumerSecret ) ) )
            ) );

            var postBody = "grant_type=client_credentials";

            HttpWebRequest authRequest = ( HttpWebRequest ) WebRequest.Create( oAuthUrl );
            authRequest.Headers.Add( "Authorization", authHeader );
            authRequest.Method = "POST";
            authRequest.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
            authRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using ( Stream stream = authRequest.GetRequestStream() )
            {
                byte[] content = ASCIIEncoding.ASCII.GetBytes( postBody );
                stream.Write( content, 0, content.Length );
            }

            authRequest.Headers.Add( "Accept-Encoding", "gzip" );

            WebResponse authResponse = authRequest.GetResponse();
            // deserialize into an object
            TwitAuthenticateResponse twitAuthResponse;
            using ( authResponse )
            {
                using ( var reader = new StreamReader( authResponse.GetResponseStream() ) )
                {
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    var objectText = reader.ReadToEnd();
                    twitAuthResponse = JsonConvert.DeserializeObject<TwitAuthenticateResponse>( objectText );
                }
            }

            var timelineFormat = "https://api.twitter.com/1.1/statuses/user_timeline.json?screen_name={0}&include_rts=0&exclude_replies=1&count={1}";
            var timelineUrl = string.Format( timelineFormat, screenname, GetAttributeValue( AttributeKeys.TweetCount ) );
            HttpWebRequest timeLineRequest = ( HttpWebRequest ) WebRequest.Create( timelineUrl );
            var timelineHeaderFormat = "{0} {1}";
            timeLineRequest.Headers.Add( "Authorization", string.Format( timelineHeaderFormat, twitAuthResponse.token_type, twitAuthResponse.access_token ) );
            timeLineRequest.Method = "Get";
            WebResponse timeLineResponse = timeLineRequest.GetResponse();
            var timeLineJson = string.Empty;
            using ( timeLineResponse )
            {
                using ( var reader = new StreamReader( timeLineResponse.GetResponseStream() ) )
                {
                    timeLineJson = reader.ReadToEnd();
                }
            }
            dynamic parsedJson = JsonConvert.DeserializeObject( timeLineJson );

            var urlRegex = new Regex( "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)" );
            var usernameRegex = new Regex( "(^|[^@\\w])@(\\w{1,15})\\b" );
            var hashtagRegex = new Regex( "(^|\\B)#(?![0-9_]+\\b)([a-zA-Z0-9_]{1,30})(\\b|\\r)" );
            string twitterDateTemplate = "ddd MMM dd HH:mm:ss +ffff yyyy";

            var tweets = new List<Tweet>();
            foreach ( var item in parsedJson )
            {
                string text = item["text"];

                text = urlRegex.Replace( text, "<a href='$0'>$0</a>" );
                text = usernameRegex.Replace( text, "<a href='http://twitter.com/$2'>$0</a>" );
                text = hashtagRegex.Replace( text, "<a href='http://twitter.com/hashtag/$2'>$0</a>" );

                DateTime createdAt = DateTime.ParseExact( ( string ) item["created_at"], twitterDateTemplate, new System.Globalization.CultureInfo( "en-US" ) );

                var tweet = new Tweet
                {
                    Text = text,
                    Id = item["id"],
                    Created = createdAt
                };
                tweets.Add( tweet );
            }
            var mergeFields = LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
            mergeFields.Add( "Tweets", tweets );

            var output = GetAttributeValue( AttributeKeys.LavaTemplate ).ResolveMergeFields( mergeFields );

            return output;
        }


    }
    internal class TwitAuthenticateResponse
    {
        public string token_type { get; set; }
        public string access_token { get; set; }
    }

    [DotLiquid.LiquidType( "Id", "Text", "Created" )]
    internal class Tweet
    {
        public long Id { get; set; }
        public string Text { get; set; }
        public DateTime Created { get; set; }
    }
}

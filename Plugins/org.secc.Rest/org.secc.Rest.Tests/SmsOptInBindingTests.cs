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
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using org.secc.Rest.Controllers;
using Xunit;

namespace org.secc.Rest.Tests
{
    /// <summary>
    /// ROCK-9116: how the api/account/create body binds SmsOptIn. The body is read the way Rock's
    /// v1 REST formatter reads it (RockJsonMediaTypeFormatter, configured as
    /// ApiPickerJsonMediaTypeFormatter configures it). Web API passes the formatter a logger that
    /// records conversion errors in ModelState; the test logger records their paths.
    /// </summary>
    public class SmsOptInBindingTests
    {
        private const string OtherFields = "\"FirstName\":\"Pat\",\"NickName\":\"Pat\",\"LastName\":\"Sample\",\"Birthdate\":\"1990-01-02T00:00:00\","
            + "\"Gender\":\"Female\",\"EmailAddress\":\"pat.sample@example.com\",\"MobileNumber\":\"(502) 555-0100\",\"Username\":\"pat.sample\",\"Password\":\"Secret-123\"";

        private class ErrorPathLogger : IFormatterLogger
        {
            public List<string> Paths { get; } = new List<string>();

            public void LogError( string errorPath, string errorMessage )
            {
                Paths.Add( errorPath );
            }

            public void LogError( string errorPath, Exception exception )
            {
                Paths.Add( errorPath );
            }
        }

        private static async Task<AccountController.Account> BindAsync( string json, ErrorPathLogger logger )
        {
            var formatter = new Rock.Utility.RockJsonMediaTypeFormatter();
            formatter.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Unspecified;

            using ( var content = new StringContent( json, Encoding.UTF8, "application/json" ) )
            using ( var stream = await content.ReadAsStreamAsync() )
            {
                return ( AccountController.Account ) await formatter.ReadFromStreamAsync( typeof( AccountController.Account ), stream, content, logger );
            }
        }

        [Fact]
        public async Task MissingFieldBindsAsNull()
        {
            var logger = new ErrorPathLogger();

            var account = await BindAsync( "{" + OtherFields + "}", logger );

            Assert.Null( account.SmsOptIn );
            Assert.Empty( logger.Paths );
        }

        [Theory]
        [InlineData( "null", null )]
        [InlineData( "\"\"", null )]
        [InlineData( "true", true )]
        [InlineData( "false", false )]
        [InlineData( "\"true\"", true )]
        [InlineData( "\"TRUE\"", true )]
        [InlineData( "1", true )]
        [InlineData( "0", false )]
        [InlineData( "2", true )]
        public async Task ValueBinds( string jsonValue, bool? expected )
        {
            var logger = new ErrorPathLogger();

            var account = await BindAsync( "{" + OtherFields + ",\"SmsOptIn\":" + jsonValue + "}", logger );

            Assert.Equal( expected, account.SmsOptIn );
            Assert.Empty( logger.Paths );
        }

        [Fact]
        public async Task NameIsCaseInsensitive()
        {
            var logger = new ErrorPathLogger();

            var account = await BindAsync( "{" + OtherFields + ",\"smsoptin\":true}", logger );

            Assert.True( account.SmsOptIn );
        }

        [Theory]
        [InlineData( "\"yes\"" )]
        [InlineData( "\"on\"" )]
        [InlineData( "\"1\"" )]
        [InlineData( "[]" )]
        [InlineData( "{}" )]
        public async Task OtherValuesAreConversionErrors( string jsonValue )
        {
            var logger = new ErrorPathLogger();

            await BindAsync( "{" + OtherFields + ",\"SmsOptIn\":" + jsonValue + "}", logger );

            Assert.Contains( logger.Paths, p => p.StartsWith( "SmsOptIn", StringComparison.Ordinal ) );
        }

        [Fact]
        public async Task OtherFieldsStillBind()
        {
            var logger = new ErrorPathLogger();

            var account = await BindAsync( "{" + OtherFields + ",\"SmsOptIn\":true}", logger );

            Assert.Empty( logger.Paths );
            Assert.Equal( "Pat", account.FirstName );
            Assert.Equal( "Sample", account.LastName );
            Assert.Equal( new DateTime( 1990, 1, 2 ), account.Birthdate );
            Assert.Equal( "pat.sample@example.com", account.EmailAddress );
            Assert.Equal( "(502) 555-0100", account.MobileNumber );
            Assert.Equal( "pat.sample", account.Username );
            Assert.Equal( "Secret-123", account.Password );
        }
    }
}

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
using System.Linq;
using System.Security.Cryptography;
using org.secc.OAuth.Migrations;
using Xunit;

namespace org.secc.OAuth.Tests
{
    /// <summary>
    /// The plugin's migrations, run the way Rock runs them at startup, each test against a temporary database of its own.
    /// </summary>
    public class OAuthMigrationTests
    {
        private const string SampleName = "Localhost Test";
        private const string SampleCallbackUrl = "https://localhost:44300/";
        private static readonly DateTime Earlier = new DateTime( 2001, 1, 1 );

        [Fact]
        public void Migration6_DeactivatesEveryActiveClientWithTheSampleNameAndCallback()
        {
            using ( var database = new OAuthMigrationTestDatabase() )
            {
                var profile = database.AddScope( "Profile" );
                var family = database.AddScope( "Family" );

                // As migration 4 created it: active, never modified, with inactive Profile and Family client scopes.
                var sample = database.AddClient( SampleName, SampleCallbackUrl, true, null );
                database.AddClientScope( sample, profile, false );
                database.AddClientScope( sample, family, false );

                var duplicate = database.AddClient( SampleName, SampleCallbackUrl, true, Earlier );
                var otherCase = database.AddClient( "LOCALHOST TEST", SampleCallbackUrl, true, Earlier );
                var otherCallbackUrlCase = database.AddClient( SampleName, "HTTPS://LOCALHOST:44300/", true, Earlier );
                var otherCallbackUrl = database.AddClient( SampleName, "https://localhost:44301/", true, Earlier );
                var callbackUrlWithoutTrailingSlash = database.AddClient( SampleName, "https://localhost:44300", true, Earlier );
                var otherName = database.AddClient( "Localhost Test 2", SampleCallbackUrl, true, Earlier );

                var before = new[] { sample, duplicate, otherCase, otherCallbackUrlCase, otherCallbackUrl, callbackUrlWithoutTrailingSlash, otherName }.ToDictionary( id => id, id => database.GetClient( id ) );
                var clientScopesBefore = database.GetRows( "_org_secc_OAuth_ClientScope" );
                var started = database.Now();

                database.RunMigration( 6 );

                var finished = database.Now();
                foreach ( var id in new[] { sample, duplicate, otherCase, otherCallbackUrlCase } )
                {
                    AssertDeactivated( before[id], database.GetClient( id ), started, finished );
                }

                foreach ( var id in new[] { otherCallbackUrl, callbackUrlWithoutTrailingSlash, otherName } )
                {
                    AssertUnchanged( before[id], database.GetClient( id ) );
                }

                AssertRowsUnchanged( clientScopesBefore, database.GetRows( "_org_secc_OAuth_ClientScope" ) );
            }
        }

        [Fact]
        public void Migration6_LeavesInactiveClientsAloneAndCanRunTwice()
        {
            using ( var database = new OAuthMigrationTestDatabase() )
            {
                var inactive = database.AddClient( SampleName, SampleCallbackUrl, false, Earlier );
                var active = database.AddClient( SampleName, SampleCallbackUrl, true, null );
                var inactiveBefore = database.GetClient( inactive );
                var activeBefore = database.GetClient( active );
                var started = database.Now();

                database.RunMigration( 6 );

                var finished = database.Now();
                var activeAfterFirstRun = database.GetClient( active );
                AssertDeactivated( activeBefore, activeAfterFirstRun, started, finished );
                AssertUnchanged( inactiveBefore, database.GetClient( inactive ) );

                database.RunMigration( 6 );

                AssertUnchanged( activeAfterFirstRun, database.GetClient( active ) );
                AssertUnchanged( inactiveBefore, database.GetClient( inactive ) );
            }
        }

        [Fact]
        public void Migration6_DeactivatesEveryActiveClientWithTheSampleKeyAndSecret()
        {
            // Migration 6 runs with the hash of a key and secret generated here in place of the sample client's.
            Assert.Equal( typeof( DeactivateLocalhostTestClient ), OAuthPluginMigrations.Find( 6 ) );
            var apiKey = Guid.NewGuid();
            var apiSecret = Guid.NewGuid();
            var migration = new Migration6WithSampleKeyAndSecret( apiKey, apiSecret );
            Assert.Matches( "^0x[0-9A-F]{64}$", migration.ReleasedSampleApiKeyAndSecretSha256 );

            using ( var database = new OAuthMigrationTestDatabase() )
            {
                var renamed = database.AddClient( "Renamed client", "https://example.org/callback", true, Earlier, apiKey, apiSecret );
                var otherCallbackUrl = database.AddClient( SampleName, "https://localhost:44301/", true, null, apiKey, apiSecret );
                var sampleWithOtherKey = database.AddClient( SampleName, SampleCallbackUrl, true, Earlier );
                var sameKeyOnly = database.AddClient( "Same key", "https://example.org/callback", true, Earlier, apiKey, Guid.NewGuid() );
                var sameSecretOnly = database.AddClient( "Same secret", "https://example.org/callback", true, Earlier, Guid.NewGuid(), apiSecret );
                var swapped = database.AddClient( "Swapped", "https://example.org/callback", true, Earlier, apiSecret, apiKey );
                var inactive = database.AddClient( "Inactive", "https://example.org/callback", false, Earlier, apiKey, apiSecret );

                var deactivated = new[] { renamed, otherCallbackUrl, sampleWithOtherKey };
                var unchanged = new[] { sameKeyOnly, sameSecretOnly, swapped, inactive };
                var before = deactivated.Concat( unchanged ).ToDictionary( id => id, id => database.GetClient( id ) );
                var started = database.Now();

                database.RunMigration( migration );

                var finished = database.Now();
                foreach ( var id in deactivated )
                {
                    AssertDeactivated( before[id], database.GetClient( id ), started, finished );
                }

                foreach ( var id in unchanged )
                {
                    AssertUnchanged( before[id], database.GetClient( id ) );
                }

                var afterFirstRun = before.Keys.ToDictionary( id => id, id => database.GetClient( id ) );

                database.RunMigration( migration );

                foreach ( var id in afterFirstRun.Keys )
                {
                    AssertUnchanged( afterFirstRun[id], database.GetClient( id ) );
                }
            }
        }

        [Fact]
        public void FreshInstall_Migrations4To6_CreateScopesAndNoClient()
        {
            using ( var database = new OAuthMigrationTestDatabase() )
            {
                database.RunMigration( 4 );

                AssertRowCount( database, "_org_secc_OAuth_Client", 0, "migration 4" );
                AssertRowCount( database, "_org_secc_OAuth_ClientScope", 0, "migration 4" );
                var scopes = database.GetRows( "_org_secc_OAuth_Scope" );
                Assert.Equal( 2, scopes.Count );
                Assert.Equal( 1, ( int ) scopes[0]["Id"] );
                Assert.Equal( "Family", scopes[0]["Identifier"] );
                Assert.True( ( bool ) scopes[0]["Active"] );
                Assert.Equal( 2, ( int ) scopes[1]["Id"] );
                Assert.Equal( "Profile", scopes[1]["Identifier"] );
                Assert.True( ( bool ) scopes[1]["Active"] );

                database.RunMigration( 5 );

                Assert.True( database.ObjectExists( "_org_secc_OAuth_Token", "U" ) );
                Assert.True( database.ObjectExists( "_org_secc_OAuth_spAddToken", "P" ) );
                Assert.True( database.ObjectExists( "_org_secc_OAuth_spGetTicket", "P" ) );
                Assert.True( database.ObjectExists( "_org_secc_OAuth_spDeleteToken", "P" ) );

                database.RunMigration( 6 );

                AssertRowCount( database, "_org_secc_OAuth_Client", 0, "migration 6" );
            }
        }

        [Fact]
        public void Migrations_HaveDistinctNumbersOneToSix_AndRunOnThisRockVersion()
        {
            var migrations = OAuthPluginMigrations.All();

            Assert.Equal( new[] { 1, 2, 3, 4, 5, 6 }, migrations.Select( m => m.Value.Number ).OrderBy( n => n ) );
            foreach ( var migration in migrations )
            {
                Assert.True( new Version( migration.Value.MinimumRockVersion ) <= OAuthPluginMigrations.RockVersion,
                    $"{migration.Key.Name} needs Rock {migration.Value.MinimumRockVersion}; this is Rock {OAuthPluginMigrations.RockVersion}." );
            }
        }

        /// <summary>
        /// The client is inactive and was modified while the migration ran; every other column is unchanged.
        /// </summary>
        private static void AssertDeactivated( Dictionary<string, object> before, Dictionary<string, object> after, DateTime started, DateTime finished )
        {
            AssertUnchanged( before, after, "Active", "ModifiedDateTime" );
            Assert.False( ( bool ) after["Active"], $"Client {before["Id"]} is still active." );
            var modified = Assert.IsType<DateTime>( after["ModifiedDateTime"] );
            Assert.InRange( modified, started, finished );
        }

        private static void AssertUnchanged( Dictionary<string, object> before, Dictionary<string, object> after, params string[] changedColumns )
        {
            Assert.True( after != null, $"Row {before["Id"]} no longer exists." );
            Assert.Equal( before.Keys, after.Keys );
            foreach ( var column in before.Keys.Except( changedColumns ) )
            {
                // Values aren't printed: rows hold generated keys and secrets.
                Assert.True( Equals( before[column], after[column] ), $"Row {before["Id"]}: {column} changed." );
            }
        }

        private static void AssertRowsUnchanged( List<Dictionary<string, object>> before, List<Dictionary<string, object>> after )
        {
            Assert.Equal( before.Count, after.Count );
            for ( var i = 0; i < before.Count; i++ )
            {
                AssertUnchanged( before[i], after[i] );
            }
        }

        private static void AssertRowCount( OAuthMigrationTestDatabase database, string table, int expected, string after )
        {
            var actual = database.GetRows( table ).Count;
            Assert.True( actual == expected, $"{table} rows after {after}: expected {expected}, actual {actual}." );
        }

        /// <summary>
        /// Migration 6 with the SHA-256 of the given key and secret in place of the sample client's: the 16 bytes of
        /// Guid.ToByteArray() of the key followed by those of the secret, as a T-SQL binary literal.
        /// </summary>
        private sealed class Migration6WithSampleKeyAndSecret : DeactivateLocalhostTestClient
        {
            private readonly string _sha256;

            public Migration6WithSampleKeyAndSecret( Guid apiKey, Guid apiSecret )
            {
                using ( var sha256 = SHA256.Create() )
                {
                    var hash = sha256.ComputeHash( apiKey.ToByteArray().Concat( apiSecret.ToByteArray() ).ToArray() );
                    _sha256 = "0x" + BitConverter.ToString( hash ).Replace( "-", string.Empty );
                }
            }

            public string ReleasedSampleApiKeyAndSecretSha256 => base.SampleApiKeyAndSecretSha256;

            protected override string SampleApiKeyAndSecretSha256 => _sha256;
        }
    }
}

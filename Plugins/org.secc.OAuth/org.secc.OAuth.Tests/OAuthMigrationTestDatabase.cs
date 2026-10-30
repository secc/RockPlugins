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
using System.Data.SqlClient;
using Rock.Plugin;

namespace org.secc.OAuth.Tests
{
    /// <summary>
    /// A temporary SQL Server database with the OAuth Client, Scope and ClientScope tables, with the columns, types,
    /// keys and unique indexes that InitialCreate gives them (no PersonAlias foreign keys). It uses the collation of
    /// Rock's databases, and is created on (localdb)\MSSQLLocalDB, or on the server named by the
    /// ORG_SECC_TEST_SQLSERVER environment variable, and dropped when the test finishes.
    /// </summary>
    public sealed class OAuthMigrationTestDatabase : IDisposable
    {
        private const string CommonColumns = @"
            [CreatedDateTime] datetime NULL,
            [ModifiedDateTime] datetime NULL,
            [CreatedByPersonAliasId] int NULL,
            [ModifiedByPersonAliasId] int NULL,
            [Guid] uniqueidentifier NOT NULL,
            [ForeignId] int NULL,
            [ForeignGuid] uniqueidentifier NULL,
            [ForeignKey] nvarchar(100) NULL";

        private readonly string _serverConnectionString;
        private readonly string _databaseName = "orgseccOAuthTests_" + Guid.NewGuid().ToString( "N" );

        public string ConnectionString { get; }

        public OAuthMigrationTestDatabase()
        {
            var server = Environment.GetEnvironmentVariable( "ORG_SECC_TEST_SQLSERVER" );
            if ( string.IsNullOrWhiteSpace( server ) )
            {
                server = @"(localdb)\MSSQLLocalDB";
            }

            _serverConnectionString = new SqlConnectionStringBuilder
            {
                DataSource = server,
                IntegratedSecurity = true,
                ConnectTimeout = 60
            }.ConnectionString;
            ConnectionString = new SqlConnectionStringBuilder( _serverConnectionString ) { InitialCatalog = _databaseName }.ConnectionString;

            Execute( _serverConnectionString, "CREATE DATABASE [" + _databaseName + "] COLLATE SQL_Latin1_General_CP1_CI_AS" );
            try
            {
                Execute( ConnectionString, @"
                    CREATE TABLE [dbo].[_org_secc_OAuth_Client] (
                        [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_dbo._org_secc_OAuth_Client] PRIMARY KEY,
                        [ClientName] nvarchar(255) NULL,
                        [ApiKey] uniqueidentifier NOT NULL,
                        [ApiSecret] uniqueidentifier NOT NULL,
                        [CallbackUrl] nvarchar(max) NULL,
                        [Active] bit NOT NULL," + CommonColumns + @" );
                    CREATE UNIQUE INDEX [IX_Guid] ON [dbo].[_org_secc_OAuth_Client] ( [Guid] );

                    CREATE TABLE [dbo].[_org_secc_OAuth_Scope] (
                        [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_dbo._org_secc_OAuth_Scope] PRIMARY KEY,
                        [Identifier] nvarchar(255) NULL,
                        [Description] nvarchar(max) NULL,
                        [Active] bit NOT NULL," + CommonColumns + @" );
                    CREATE UNIQUE INDEX [IX_Guid] ON [dbo].[_org_secc_OAuth_Scope] ( [Guid] );

                    CREATE TABLE [dbo].[_org_secc_OAuth_ClientScope] (
                        [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_dbo._org_secc_OAuth_ClientScope] PRIMARY KEY,
                        [ClientId] int NOT NULL CONSTRAINT [FK_dbo._org_secc_OAuth_ClientScope_dbo._org_secc_OAuth_Client_ClientId]
                            FOREIGN KEY REFERENCES [dbo].[_org_secc_OAuth_Client] ( [Id] ) ON DELETE CASCADE,
                        [ScopeId] int NOT NULL CONSTRAINT [FK_dbo._org_secc_OAuth_ClientScope_dbo._org_secc_OAuth_Scope_ScopeId]
                            FOREIGN KEY REFERENCES [dbo].[_org_secc_OAuth_Scope] ( [Id] ) ON DELETE CASCADE,
                        [Active] bit NOT NULL," + CommonColumns + @" );
                    CREATE UNIQUE INDEX [IX_Guid] ON [dbo].[_org_secc_OAuth_ClientScope] ( [Guid] );" );
            }
            catch
            {
                // The caller never gets an instance to dispose when the constructor throws.
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// Runs a plugin migration's Up() the way Rock's RunPluginMigrations does: on an open connection, in a
        /// transaction of its own that is committed when Up() returns and rolled back when it throws.
        /// </summary>
        public void RunMigration( int number )
        {
            RunMigration( ( Migration ) Activator.CreateInstance( OAuthPluginMigrations.Find( number ) ) );
        }

        /// <summary>
        /// Runs the given migration instance's Up() the same way as <see cref="RunMigration(int)"/>.
        /// </summary>
        public void RunMigration( Migration migration )
        {
            using ( var connection = Open( ConnectionString ) )
            using ( var transaction = connection.BeginTransaction() )
            {
                try
                {
                    migration.SqlConnection = connection;
                    migration.SqlTransaction = transaction;
                    migration.Up();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Adds a client with a newly generated ApiKey, ApiSecret and Guid, and returns its Id.
        /// </summary>
        public int AddClient( string clientName, string callbackUrl, bool active, DateTime? modifiedDateTime )
        {
            return AddClient( clientName, callbackUrl, active, modifiedDateTime, Guid.NewGuid(), Guid.NewGuid() );
        }

        /// <summary>
        /// Adds a client with the given ApiKey and ApiSecret and a newly generated Guid, and returns its Id.
        /// </summary>
        public int AddClient( string clientName, string callbackUrl, bool active, DateTime? modifiedDateTime, Guid apiKey, Guid apiSecret )
        {
            return Insert( @"
                INSERT INTO [dbo].[_org_secc_OAuth_Client] ( [ClientName], [ApiKey], [ApiSecret], [CallbackUrl], [Active], [CreatedDateTime], [ModifiedDateTime], [Guid] )
                OUTPUT INSERTED.[Id]
                VALUES ( @name, @apiKey, @apiSecret, @callbackUrl, @active, @created, @modified, @guid )",
                new SqlParameter( "@name", clientName ),
                new SqlParameter( "@apiKey", apiKey ),
                new SqlParameter( "@apiSecret", apiSecret ),
                new SqlParameter( "@callbackUrl", callbackUrl ),
                new SqlParameter( "@active", active ),
                new SqlParameter( "@created", new DateTime( 2000, 1, 1 ) ),
                new SqlParameter( "@modified", ( object ) modifiedDateTime ?? DBNull.Value ),
                new SqlParameter( "@guid", Guid.NewGuid() ) );
        }

        public int AddScope( string identifier )
        {
            return Insert( @"
                INSERT INTO [dbo].[_org_secc_OAuth_Scope] ( [Identifier], [Description], [Active], [CreatedDateTime], [Guid] )
                OUTPUT INSERTED.[Id]
                VALUES ( @identifier, @identifier, 1, @created, @guid )",
                new SqlParameter( "@identifier", identifier ),
                new SqlParameter( "@created", new DateTime( 2000, 1, 1 ) ),
                new SqlParameter( "@guid", Guid.NewGuid() ) );
        }

        public int AddClientScope( int clientId, int scopeId, bool active )
        {
            return Insert( @"
                INSERT INTO [dbo].[_org_secc_OAuth_ClientScope] ( [ClientId], [ScopeId], [Active], [CreatedDateTime], [Guid] )
                OUTPUT INSERTED.[Id]
                VALUES ( @clientId, @scopeId, @active, @created, @guid )",
                new SqlParameter( "@clientId", clientId ),
                new SqlParameter( "@scopeId", scopeId ),
                new SqlParameter( "@active", active ),
                new SqlParameter( "@created", new DateTime( 2000, 1, 1 ) ),
                new SqlParameter( "@guid", Guid.NewGuid() ) );
        }

        /// <summary>
        /// Every column of the client, or null when there is no client with that Id.
        /// </summary>
        public Dictionary<string, object> GetClient( int id )
        {
            var rows = Query( "SELECT * FROM [dbo].[_org_secc_OAuth_Client] WHERE [Id] = @id", new SqlParameter( "@id", id ) );
            return rows.Count == 1 ? rows[0] : null;
        }

        /// <summary>
        /// Every column of every row of a table, ordered by Id.
        /// </summary>
        public List<Dictionary<string, object>> GetRows( string table )
        {
            return Query( "SELECT * FROM [dbo].[" + table + "] ORDER BY [Id]" );
        }

        /// <summary>
        /// The database server's current time, as GETDATE() returns it.
        /// </summary>
        public DateTime Now()
        {
            using ( var connection = Open( ConnectionString ) )
            using ( var command = new SqlCommand( "SELECT GETDATE()", connection ) )
            {
                return ( DateTime ) command.ExecuteScalar();
            }
        }

        /// <summary>
        /// Whether an object of the given type (U = table, P = stored procedure) exists in the dbo schema.
        /// </summary>
        public bool ObjectExists( string name, string type )
        {
            using ( var connection = Open( ConnectionString ) )
            using ( var command = new SqlCommand( "SELECT CASE WHEN OBJECT_ID( @name, @type ) IS NULL THEN 0 ELSE 1 END", connection ) )
            {
                command.Parameters.AddWithValue( "@name", "dbo." + name );
                command.Parameters.AddWithValue( "@type", type );
                return ( int ) command.ExecuteScalar() == 1;
            }
        }

        public void Dispose()
        {
            SqlConnection.ClearAllPools();
            Execute( _serverConnectionString, "IF DB_ID( '" + _databaseName + "' ) IS NOT NULL BEGIN ALTER DATABASE [" + _databaseName + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [" + _databaseName + "] END" );
        }

        private int Insert( string sql, params SqlParameter[] parameters )
        {
            using ( var connection = Open( ConnectionString ) )
            using ( var command = new SqlCommand( sql, connection ) )
            {
                command.Parameters.AddRange( parameters );
                return ( int ) command.ExecuteScalar();
            }
        }

        private List<Dictionary<string, object>> Query( string sql, params SqlParameter[] parameters )
        {
            var rows = new List<Dictionary<string, object>>();
            using ( var connection = Open( ConnectionString ) )
            using ( var command = new SqlCommand( sql, connection ) )
            {
                command.Parameters.AddRange( parameters );
                using ( var reader = command.ExecuteReader() )
                {
                    while ( reader.Read() )
                    {
                        var row = new Dictionary<string, object>();
                        for ( var i = 0; i < reader.FieldCount; i++ )
                        {
                            row[reader.GetName( i )] = reader.GetValue( i );
                        }

                        rows.Add( row );
                    }
                }
            }

            return rows;
        }

        private static SqlConnection Open( string connectionString )
        {
            var connection = new SqlConnection( connectionString );
            connection.Open();
            return connection;
        }

        private static void Execute( string connectionString, string sql )
        {
            using ( var connection = Open( connectionString ) )
            using ( var command = new SqlCommand( sql, connection ) )
            {
                command.ExecuteNonQuery();
            }
        }
    }
}

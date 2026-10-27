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
using System.Data.SqlClient;
using System.Runtime.Serialization;
using Rock.Model;
using Rock.Security.ExternalAuthentication;

namespace org.secc.Authentication.Tests
{
    /// <summary>
    /// A temporary SQL Server database with a UserLogin table holding the columns that
    /// SMSAuthentication.Authenticate's SQL uses, typed as in Rock's UserLogin model. It is created
    /// on (localdb)\MSSQLLocalDB, or on the server named by the ORG_SECC_TEST_SQLSERVER environment
    /// variable, and dropped when the tests finish.
    /// </summary>
    public sealed class UserLoginTestDatabase : IDisposable
    {
        private readonly string _serverConnectionString;
        private readonly string _databaseName = "orgseccAuthenticationTests_" + Guid.NewGuid().ToString( "N" );

        public string ConnectionString { get; }

        public UserLoginTestDatabase()
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

            Execute( _serverConnectionString, "CREATE DATABASE [" + _databaseName + "]" );
            try
            {
                Execute( ConnectionString, "CREATE TABLE [UserLogin] ( [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY, [Password] nvarchar(128) NULL, [FailedPasswordAttemptCount] int NULL )" );
            }
            catch
            {
                // The test framework doesn't dispose a fixture whose constructor throws.
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// Adds a login holding the hash of a code and returns its Id.
        /// </summary>
        public int AddLogin( string passwordHash, int? failedPasswordAttemptCount )
        {
            using ( var connection = Open( ConnectionString ) )
            using ( var command = new SqlCommand( "INSERT INTO [UserLogin] ( [Password], [FailedPasswordAttemptCount] ) OUTPUT INSERTED.[Id] VALUES ( @password, @count )", connection ) )
            {
                command.Parameters.AddWithValue( "@password", passwordHash );
                command.Parameters.AddWithValue( "@count", ( object ) failedPasswordAttemptCount ?? DBNull.Value );
                return ( int ) command.ExecuteScalar();
            }
        }

        /// <summary>
        /// Loads the login the way a caller of Authenticate does. The code was issued just now.
        /// </summary>
        public UserLogin Load( int id )
        {
            using ( var connection = Open( ConnectionString ) )
            using ( var command = new SqlCommand( "SELECT [Password], [FailedPasswordAttemptCount] FROM [UserLogin] WHERE [Id] = @id", connection ) )
            {
                command.Parameters.AddWithValue( "@id", id );
                using ( var reader = command.ExecuteReader() )
                {
                    reader.Read();
                    return new UserLogin
                    {
                        Id = id,
                        Password = reader.IsDBNull( 0 ) ? null : reader.GetString( 0 ),
                        FailedPasswordAttemptCount = reader.IsDBNull( 1 ) ? ( int? ) null : reader.GetInt32( 1 ),
                        LastPasswordChangedDateTime = Rock.RockDateTime.Now
                    };
                }
            }
        }

        /// <summary>
        /// Stores a new code for the login and resets its attempt count, as a new code request does.
        /// </summary>
        public void ReplaceCode( int id, string passwordHash )
        {
            ExecuteSql( "UPDATE [UserLogin] SET [Password] = @password, [FailedPasswordAttemptCount] = 0 WHERE [Id] = @id",
                new SqlParameter( "@password", passwordHash ),
                new SqlParameter( "@id", id ) );
        }

        /// <summary>
        /// Sets the login's stored code and attempt count directly.
        /// </summary>
        public void SetLogin( int id, string passwordHash, int? failedPasswordAttemptCount )
        {
            ExecuteSql( "UPDATE [UserLogin] SET [Password] = @password, [FailedPasswordAttemptCount] = @count WHERE [Id] = @id",
                new SqlParameter( "@password", passwordHash ),
                new SqlParameter( "@count", ( object ) failedPasswordAttemptCount ?? DBNull.Value ),
                new SqlParameter( "@id", id ) );
        }

        public int? GetFailedPasswordAttemptCount( int id )
        {
            using ( var connection = Open( ConnectionString ) )
            using ( var command = new SqlCommand( "SELECT [FailedPasswordAttemptCount] FROM [UserLogin] WHERE [Id] = @id", connection ) )
            {
                command.Parameters.AddWithValue( "@id", id );
                var value = command.ExecuteScalar();
                return value == DBNull.Value ? ( int? ) null : ( int ) value;
            }
        }

        /// <summary>
        /// Runs a statement on its own connection and returns the number of rows it affected.
        /// </summary>
        public int ExecuteSql( string sql, params SqlParameter[] parameters )
        {
            using ( var connection = Open( ConnectionString ) )
            using ( var command = new SqlCommand( sql, connection ) )
            {
                command.Parameters.AddRange( parameters );
                return command.ExecuteNonQuery();
            }
        }

        public void Dispose()
        {
            SqlConnection.ClearAllPools();
            Execute( _serverConnectionString, "ALTER DATABASE [" + _databaseName + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [" + _databaseName + "]" );
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

    /// <summary>
    /// SMSAuthentication with its SQL sent to a <see cref="UserLoginTestDatabase"/>. It is created without
    /// running its constructors, because Rock's Component constructor loads the component's attributes
    /// from Rock's database; Authenticate doesn't use them.
    /// </summary>
    public class TestSMSAuthentication : SMSAuthentication
    {
        private UserLoginTestDatabase _database;

        /// <summary>
        /// Runs before each of Authenticate's statements, with the statement's text.
        /// </summary>
        public Action<string> BeforeStatement { get; set; }

        public static TestSMSAuthentication Create( UserLoginTestDatabase database )
        {
            var authentication = ( TestSMSAuthentication ) FormatterServices.GetUninitializedObject( typeof( TestSMSAuthentication ) );
            authentication._database = database;
            return authentication;
        }

        protected override int ExecuteSql( string sql, params SqlParameter[] parameters )
        {
            BeforeStatement?.Invoke( sql );
            return _database.ExecuteSql( sql, parameters );
        }
    }
}

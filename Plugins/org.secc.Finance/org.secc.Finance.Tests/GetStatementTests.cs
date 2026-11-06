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
using System.Threading;
using System.Web;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using RockWeb;
using Xunit;

namespace org.secc.Finance.Tests
{
    /// <summary>
    /// Runs GetStatement.ashx requests through BeginProcessRequest and EndProcessRequest, with the
    /// file lookup, the current user and Rock authorization supplied by the test.
    /// </summary>
    public class GetStatementTests
    {
        private const int StatementFileTypeId = 36;
        private const int OtherFileTypeId = 3;
        private const int FileId = 5150;
        private static readonly Guid FileGuid = new Guid( "6d1f0c3a-6f84-4c41-9a3e-2b1f2f0e9c11" );

        public static IEnumerable<object[]> Shapes => new[]
        {
            new object[] { "guid" },
            new object[] { "id" }
        };

        [Theory]
        [MemberData( nameof( Shapes ) )]
        public void OtherFileType_WithoutViewSecurity_IsNotFound( string shape )
        {
            var handler = new TestGetStatement { File = NewFile( OtherFileTypeId, requiresViewSecurity: false ) };

            Run( handler, shape );

            Assert.Equal( 404, handler.ErrorCode );
            Assert.Equal( "File could not be found.", handler.ErrorMessage );
            Assert.False( handler.FileSent );
        }

        [Theory]
        [MemberData( nameof( Shapes ) )]
        public void OtherFileType_ViewableByCurrentUser_IsNotFound( string shape )
        {
            var handler = new TestGetStatement
            {
                File = NewFile( OtherFileTypeId, requiresViewSecurity: true ),
                CurrentUser = NewLogin( "G100", 100 ),
                AuthorizedToView = true
            };

            Run( handler, shape );

            Assert.Equal( 404, handler.ErrorCode );
            Assert.False( handler.FileSent );
        }

        [Theory]
        [MemberData( nameof( Shapes ) )]
        public void OtherFileType_WithMatchingGivingId_IsNotFound( string shape )
        {
            var handler = new TestGetStatement
            {
                File = NewFile( OtherFileTypeId, requiresViewSecurity: true, givingId: "G100" ),
                CurrentUser = NewLogin( "G100", 100 )
            };

            Run( handler, shape );

            Assert.Equal( 404, handler.ErrorCode );
            Assert.False( handler.FileSent );
        }

        [Theory]
        [MemberData( nameof( Shapes ) )]
        public void Statement_WithMatchingGivingId_IsSent( string shape )
        {
            var handler = new TestGetStatement
            {
                File = NewFile( StatementFileTypeId, requiresViewSecurity: true, givingId: "G100", personIds: "200" ),
                CurrentUser = NewLogin( "G100", 100 )
            };

            Run( handler, shape );

            Assert.True( handler.FileSent );
            Assert.Null( handler.ErrorCode );
        }

        [Theory]
        [MemberData( nameof( Shapes ) )]
        public void Statement_WithMatchingPersonId_IsSent( string shape )
        {
            var handler = new TestGetStatement
            {
                File = NewFile( StatementFileTypeId, requiresViewSecurity: true, givingId: "G200", personIds: "200,101" ),
                CurrentUser = NewLogin( "G100", 100, 101 )
            };

            Run( handler, shape );

            Assert.True( handler.FileSent );
            Assert.Null( handler.ErrorCode );
        }

        [Theory]
        [MemberData( nameof( Shapes ) )]
        public void Statement_ViewableByCurrentUser_IsSent( string shape )
        {
            var handler = new TestGetStatement
            {
                File = NewFile( StatementFileTypeId, requiresViewSecurity: true, givingId: "G200", personIds: "200" ),
                CurrentUser = NewLogin( "G100", 100 ),
                AuthorizedToView = true
            };

            Run( handler, shape );

            Assert.True( handler.FileSent );
            Assert.Null( handler.ErrorCode );
        }

        [Theory]
        [MemberData( nameof( Shapes ) )]
        public void Statement_ForAnotherPerson_IsRefused( string shape )
        {
            var handler = new TestGetStatement
            {
                File = NewFile( StatementFileTypeId, requiresViewSecurity: true, givingId: "G200", personIds: "200" ),
                CurrentUser = NewLogin( "G100", 100 )
            };

            Run( handler, shape );

            Assert.Equal( 403, handler.ErrorCode );
            Assert.Equal( "Not authorized to view file.", handler.ErrorMessage );
            Assert.False( handler.FileSent );
        }

        [Theory]
        [MemberData( nameof( Shapes ) )]
        public void Statement_WithNoOneSignedIn_IsRefused( string shape )
        {
            var handler = new TestGetStatement
            {
                File = NewFile( StatementFileTypeId, requiresViewSecurity: true, givingId: "G200", personIds: "200" )
            };

            Run( handler, shape );

            Assert.Equal( 403, handler.ErrorCode );
            Assert.False( handler.FileSent );
        }

        [Theory]
        [MemberData( nameof( Shapes ) )]
        public void Statement_WithoutViewSecurity_ForAnotherPerson_IsRefused( string shape )
        {
            var handler = new TestGetStatement
            {
                File = NewFile( StatementFileTypeId, requiresViewSecurity: false, givingId: "G200", personIds: "200" ),
                CurrentUser = NewLogin( "G100", 100 )
            };

            Run( handler, shape );

            Assert.Equal( 403, handler.ErrorCode );
            Assert.False( handler.FileSent );
        }

        [Theory]
        [MemberData( nameof( Shapes ) )]
        public void Statement_WithoutViewSecurity_WithNoOneSignedIn_IsRefused( string shape )
        {
            var handler = new TestGetStatement
            {
                File = NewFile( StatementFileTypeId, requiresViewSecurity: false, givingId: "G200", personIds: "200" )
            };

            Run( handler, shape );

            Assert.Equal( 403, handler.ErrorCode );
            Assert.False( handler.FileSent );
        }

        [Theory]
        [MemberData( nameof( Shapes ) )]
        public void Statement_WithoutViewSecurity_WithMatchingGivingId_IsSent( string shape )
        {
            var handler = new TestGetStatement
            {
                File = NewFile( StatementFileTypeId, requiresViewSecurity: false, givingId: "G100", personIds: "200" ),
                CurrentUser = NewLogin( "G100", 100 )
            };

            Run( handler, shape );

            Assert.True( handler.FileSent );
            Assert.Null( handler.ErrorCode );
        }

        [Theory]
        [MemberData( nameof( Shapes ) )]
        public void MissingFile_IsNotFound( string shape )
        {
            var handler = new TestGetStatement
            {
                CurrentUser = NewLogin( "G100", 100 ),
                AuthorizedToView = true
            };

            Run( handler, shape );

            Assert.Equal( 404, handler.ErrorCode );
            Assert.False( handler.FileSent );
        }

        [Fact]
        public void FileWithoutType_WhenStatementTypeIsMissing_IsNotFound()
        {
            var file = NewFile( OtherFileTypeId, requiresViewSecurity: false );
            file.BinaryFileTypeId = null;
            var handler = new TestGetStatement
            {
                File = file,
                StatementTypeId = null,
                CurrentUser = NewLogin( "G100", 100 ),
                AuthorizedToView = true
            };

            Run( handler, "guid" );

            Assert.Equal( 404, handler.ErrorCode );
            Assert.False( handler.FileSent );
        }

        [Fact]
        public void Guid_IsLookedUpByGuid()
        {
            var handler = new TestGetStatement();

            Run( handler, "guid" );

            Assert.Equal( FileGuid, handler.RequestedGuid );
            Assert.Null( handler.RequestedId );
        }

        [Fact]
        public void Id_IsLookedUpById()
        {
            var handler = new TestGetStatement();

            Run( handler, "id" );

            Assert.Equal( FileId, handler.RequestedId );
            Assert.Null( handler.RequestedGuid );
        }

        private static void Run( TestGetStatement handler, string shape )
        {
            var query = shape == "guid" ? "guid=" + FileGuid : "id=" + FileId;
            var context = new HttpContext(
                new HttpRequest( "GetStatement.ashx", "http://localhost/GetStatement.ashx?" + query, query ),
                new HttpResponse( new StringWriter() ) );

            var result = handler.BeginProcessRequest( context, null, null );
            handler.EndProcessRequest( result );
        }

        private static BinaryFile NewFile( int fileTypeId, bool requiresViewSecurity, string givingId = null, string personIds = null )
        {
            var file = new BinaryFile
            {
                Id = FileId,
                Guid = FileGuid,
                BinaryFileTypeId = fileTypeId,
                BinaryFileType = new BinaryFileType { Id = fileTypeId, RequiresViewSecurity = requiresViewSecurity },
                AttributeValues = new Dictionary<string, AttributeValueCache>()
            };

            if ( givingId != null )
            {
                file.AttributeValues["GivingId"] = new AttributeValueCache( 1, FileId, givingId );
            }

            if ( personIds != null )
            {
                file.AttributeValues["PersonIds"] = new AttributeValueCache( 2, FileId, personIds );
            }

            return file;
        }

        private static UserLogin NewLogin( string givingId, params int[] aliasPersonIds )
        {
            var person = new Person { Id = aliasPersonIds[0] };
            typeof( Person ).GetProperty( nameof( Person.GivingId ) ).SetValue( person, givingId );
            var aliases = new List<PersonAlias>();
            foreach ( var aliasPersonId in aliasPersonIds )
            {
                aliases.Add( new PersonAlias { PersonId = person.Id, AliasPersonId = aliasPersonId } );
            }

            person.Aliases = aliases;
            return new UserLogin { UserName = "test-" + person.Id, Person = person };
        }
    }

    /// <summary>
    /// GetStatement with the file, the current user and Rock's View check set by the test, and the
    /// response recorded instead of written.
    /// </summary>
    internal class TestGetStatement : GetStatement
    {
        public BinaryFile File { get; set; }

        public int? StatementTypeId { get; set; } = 36;

        public UserLogin CurrentUser { get; set; }

        public bool AuthorizedToView { get; set; }

        public Guid? RequestedGuid { get; private set; }

        public int? RequestedId { get; private set; }

        public int? ErrorCode { get; private set; }

        public string ErrorMessage { get; private set; }

        public bool FileSent { get; private set; }

        protected override RockContext CreateRockContext()
        {
            return null;
        }

        protected override IAsyncResult BeginGetFile( AsyncCallback cb, HttpContext context, Guid fileGuid )
        {
            RequestedGuid = fileGuid;
            return new CompletedResult( context );
        }

        protected override IAsyncResult BeginGetFile( AsyncCallback cb, HttpContext context, int fileId )
        {
            RequestedId = fileId;
            return new CompletedResult( context );
        }

        protected override BinaryFile EndGetFile( IAsyncResult result, HttpContext context, RockContext rockContext )
        {
            if ( File == null )
            {
                return null;
            }

            var found = RequestedGuid.HasValue ? File.Guid == RequestedGuid.Value : File.Id == RequestedId;
            return found ? File : null;
        }

        protected override int? GetStatementFileTypeId()
        {
            return StatementTypeId;
        }

        protected override UserLogin GetCurrentUser( RockContext rockContext )
        {
            return CurrentUser;
        }

        protected override bool IsAuthorizedToView( BinaryFile binaryFile, Person person )
        {
            return AuthorizedToView;
        }

        protected override void LoadAttributes( BinaryFile binaryFile )
        {
            // The test sets the attribute values on the file.
        }

        protected override void SendBinaryFile( HttpContext context, BinaryFile binaryFile )
        {
            FileSent = true;
        }

        protected override void SendError( HttpContext context, int code, string message )
        {
            ErrorCode = code;
            ErrorMessage = message;
        }

        private class CompletedResult : IAsyncResult
        {
            public CompletedResult( object state )
            {
                AsyncState = state;
            }

            public object AsyncState { get; }

            public WaitHandle AsyncWaitHandle => new ManualResetEvent( true );

            public bool CompletedSynchronously => true;

            public bool IsCompleted => true;
        }
    }
}

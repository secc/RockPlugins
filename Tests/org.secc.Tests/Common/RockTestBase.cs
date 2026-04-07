using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using Rock.Data;
using Rock.Model;

namespace org.secc.Tests.Common
{
    /// <summary>
    /// A common base class for all test fixtures.
    /// Provides shared setup, teardown, and reusable Rock entity generators.
    /// </summary>
    [TestFixture]
    public abstract class RockTestBase
    {
        // Expose the mock context so inherited test classes can use it
        public Mock<RockContext> RockContextMock { get; set; }

        [SetUp]
        public virtual void InitialSetup()
        {
            // Initialize a fresh mock of the RockContext for each test
            RockContextMock = new Mock<RockContext>();

            // You can optionally put default mocked DbSets here that every test needs
            // var mockPeople = GetQueryableMockDbSet(new List<Person>());
            // RockContextMock.Setup(m => m.People).Returns(mockPeople.Object);
            // RockContextMock.Setup(m => m.Set<Person>()).Returns(mockPeople.Object);
        }

        [TearDown]
        public virtual void TearDown()
        {
            // Cleanup any mock state if necessary
            RockContextMock = null;
        }

        /// <summary>
        /// Creates a Mock DbSet from a standard List so that LINQ queries 
        /// (Where, Select, FirstOrDefault, etc.) work as if they are querying a database.
        /// </summary>
        public Mock<DbSet<T>> GetQueryableMockDbSet<T>( List<T> sourceList ) where T : class
        {
            var queryable = sourceList.AsQueryable();
            var dbSetMock = new Mock<DbSet<T>>();

            // Wire up the IQueryable implementations to our in-memory list
            dbSetMock.As<IQueryable<T>>().Setup( m => m.Provider ).Returns( queryable.Provider );
            dbSetMock.As<IQueryable<T>>().Setup( m => m.Expression ).Returns( queryable.Expression );
            dbSetMock.As<IQueryable<T>>().Setup( m => m.ElementType ).Returns( queryable.ElementType );
            dbSetMock.As<IQueryable<T>>().Setup( m => m.GetEnumerator() ).Returns( () => queryable.GetEnumerator() );

            // Optional: Wire up Add and Remove so the mock behaves dynamically during tests
            dbSetMock.Setup( d => d.Add( It.IsAny<T>() ) ).Callback<T>( sourceList.Add );
            dbSetMock.Setup( d => d.Remove( It.IsAny<T>() ) ).Callback<T>( t => sourceList.Remove( t ) );

            return dbSetMock;
        }

        /// <summary>
        /// Generates a detached mock Person object for use in tests.
        /// </summary>
        /// <param name="firstName">The person's first name.</param>
        /// <param name="lastName">The person's last name.</param>
        /// <returns>A populated Rock.Model.Person object.</returns>
        public Person GetMockPerson( string firstName, string lastName )
        {
            return new Person
            {
                Id = new Random().Next( 1, 10000 ), // Assign a dummy ID
                Guid = Guid.NewGuid(),
                FirstName = firstName,
                LastName = lastName,
                IsDeceased = false,
                EmailPreference = EmailPreference.EmailAllowed,
                RecordTypeValueId = Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsInteger(),
                RecordStatusValueId = Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsInteger()
            };
        }
    }
}
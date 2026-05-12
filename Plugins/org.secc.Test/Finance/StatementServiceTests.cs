using System.Collections.Generic;
using org.secc.Finance.Services;
using Xunit;

namespace org.secc.Test.Finance
{
    public class StatementServiceTests
    {
        private static StatementService.GivingGroupMember Member( string first, string last, bool isBusiness = false )
        {
            return new StatementService.GivingGroupMember
            {
                FirstName = first,
                LastName = last,
                IsBusiness = isBusiness
            };
        }

        [Fact]
        public void BuildSalutation_NullList_ReturnsEmptyString()
        {
            var result = StatementService.BuildSalutation( null );

            Assert.Equal( string.Empty, result );
        }

        [Fact]
        public void BuildSalutation_EmptyList_ReturnsEmptyString()
        {
            var result = StatementService.BuildSalutation( new List<StatementService.GivingGroupMember>() );

            Assert.Equal( string.Empty, result );
        }

        [Fact]
        public void BuildSalutation_BusinessFirstMember_ReturnsLastNameOnly()
        {
            var members = new List<StatementService.GivingGroupMember>
            {
                Member( "Ignored", "Acme Co", isBusiness: true )
            };

            var result = StatementService.BuildSalutation( members );

            Assert.Equal( "Acme Co", result );
        }

        [Fact]
        public void BuildSalutation_SinglePersonSingleLastName_ReturnsFirstSpaceLast()
        {
            var members = new List<StatementService.GivingGroupMember>
            {
                Member( "John", "Smith" )
            };

            var result = StatementService.BuildSalutation( members );

            Assert.Equal( "John Smith", result );
        }

        [Fact]
        public void BuildSalutation_TwoPeopleSameLastName_JoinsWithAmpersand()
        {
            var members = new List<StatementService.GivingGroupMember>
            {
                Member( "John", "Smith" ),
                Member( "Jane", "Smith" )
            };

            var result = StatementService.BuildSalutation( members );

            Assert.Equal( "John & Jane Smith", result );
        }

        [Fact]
        public void BuildSalutation_ThreePeopleSameLastName_OnlyLastCommaReplaced()
        {
            var members = new List<StatementService.GivingGroupMember>
            {
                Member( "John", "Smith" ),
                Member( "Jane", "Smith" ),
                Member( "Bob", "Smith" )
            };

            var result = StatementService.BuildSalutation( members );

            Assert.Equal( "John, Jane & Bob Smith", result );
        }

        [Fact]
        public void BuildSalutation_TwoPeopleDifferentLastNames_UsesFullNamesAndAmpersand()
        {
            var members = new List<StatementService.GivingGroupMember>
            {
                Member( "John", "Smith" ),
                Member( "Jane", "Doe" )
            };

            var result = StatementService.BuildSalutation( members );

            Assert.Equal( "John Smith & Jane Doe", result );
        }
    }
}

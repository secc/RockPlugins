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
using System.Collections.Generic;
using System.Linq;
using org.secc.Rest.Controllers;
using Rock.Model;
using Xunit;

namespace org.secc.Rest.Tests
{
    /// <summary>
    /// ROCK-8934: which attribute value rows the confirmation-code delete matches, used by the
    /// expired-code cleanup and by confirmaccount.
    /// </summary>
    public class ConfirmationCodeRowsTests
    {
        private const int UserLoginEntityTypeId = 27;
        private const int PersonEntityTypeId = 15;

        private const int CodeHashAttributeId = 7593;
        private const int CodeIssuedAttributeId = 7594;
        private const int OtherUserLoginAttributeId = 8001;
        private const int PersonAttributeId = 9001;

        private static readonly int[] CodeAttributeIds = { CodeHashAttributeId, CodeIssuedAttributeId };
        private static readonly int[] SelectedLoginIds = { 19, 20 };

        private static AttributeValue Row( int id, int attributeId, int attributeEntityTypeId, int? entityId )
        {
            return new AttributeValue
            {
                Id = id,
                AttributeId = attributeId,
                Attribute = new Rock.Model.Attribute { Id = attributeId, EntityTypeId = attributeEntityTypeId },
                EntityId = entityId
            };
        }

        private static int[] MatchedIds( IEnumerable<AttributeValue> rows )
        {
            var predicate = AccountController.ConfirmationCodeRowsOf( CodeAttributeIds, UserLoginEntityTypeId, SelectedLoginIds ).Compile();

            return rows.Where( predicate ).Select( av => av.Id ).ToArray();
        }

        [Fact]
        public void OnlyTheCodeRowsOfTheSelectedLoginsMatch()
        {
            var rows = new[]
            {
                Row( 1, CodeHashAttributeId, UserLoginEntityTypeId, 19 ),
                Row( 2, CodeIssuedAttributeId, UserLoginEntityTypeId, 19 ),
                Row( 3, CodeHashAttributeId, UserLoginEntityTypeId, 20 ),
                Row( 4, CodeIssuedAttributeId, UserLoginEntityTypeId, 20 ),

                // A login that was not selected.
                Row( 5, CodeHashAttributeId, UserLoginEntityTypeId, 21 ),
                Row( 6, CodeIssuedAttributeId, UserLoginEntityTypeId, 21 ),

                // Another attribute of a selected login.
                Row( 7, OtherUserLoginAttributeId, UserLoginEntityTypeId, 19 ),

                // A person whose Id equals a selected login's Id.
                Row( 8, PersonAttributeId, PersonEntityTypeId, 19 ),

                // A code attribute value with no entity.
                Row( 9, CodeHashAttributeId, UserLoginEntityTypeId, null )
            };

            Assert.Equal( new[] { 1, 2, 3, 4 }, MatchedIds( rows ) );
        }

        [Fact]
        public void AttributeOfAnotherEntityType_DoesNotMatch()
        {
            var rows = new[]
            {
                Row( 1, CodeHashAttributeId, UserLoginEntityTypeId, 19 ),
                Row( 2, CodeHashAttributeId, PersonEntityTypeId, 19 )
            };

            Assert.Equal( new[] { 1 }, MatchedIds( rows ) );
        }
    }
}

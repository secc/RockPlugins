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
#nullable enable
using org.secc.RegistrationCart.Domain;
using Xunit;

namespace org.secc.RegistrationCart.Tests;

public class AnswerRulesTests
{
    [Theory]
    [InlineData(AnswerKind.Text)]
    [InlineData(AnswerKind.Address)]
    [InlineData(AnswerKind.Date)]
    [InlineData(AnswerKind.Phone)]
    [InlineData(AnswerKind.Gender)]
    public void Nothing_is_blank_whatever_the_kind(AnswerKind kind)
    {
        Assert.True(AnswerRules.IsBlank(kind, null));
        Assert.True(AnswerRules.IsBlank(kind, ""));
        Assert.True(AnswerRules.IsBlank(kind, "   "));
    }

    [Fact]
    public void Text_with_anything_in_it_is_answered()
    {
        Assert.False(AnswerRules.IsBlank(AnswerKind.Text, "Medium"));
    }

    [Theory]
    [InlineData("{\"street1\":\"\",\"street2\":\"\",\"city\":\"\",\"state\":\"\",\"postalCode\":\"\",\"country\":\"US\"}")] // the control fills in the country
    [InlineData("{\"Street1\":null,\"City\":null,\"Country\":\"US\"}")]
    public void An_address_with_only_a_country_is_blank(string json)
    {
        Assert.True(AnswerRules.IsBlank(AnswerKind.Address, json));
    }

    [Theory]
    [InlineData("{\"street1\":\"1 Main St\",\"city\":\"\",\"country\":\"US\"}")]
    [InlineData("{\"Street1\":\"\",\"City\":\"Louisville\"}")] // C# bag casing
    public void An_address_with_any_part_is_answered(string json)
    {
        Assert.False(AnswerRules.IsBlank(AnswerKind.Address, json));
    }

    [Theory]
    [InlineData("{\"year\":0,\"month\":0,\"day\":0}", true)]
    [InlineData("{\"year\":2015,\"month\":0,\"day\":12}", true)] // no month
    [InlineData("{\"year\":0,\"month\":4,\"day\":12}", false)]  // Rock's picker allows leaving out the year
    [InlineData("{\"Year\":2015,\"Month\":4,\"Day\":12}", false)]
    public void A_date_needs_a_month_and_a_day(string json, bool blank)
    {
        Assert.Equal(blank, AnswerRules.IsBlank(AnswerKind.Date, json));
    }

    [Theory]
    [InlineData("(502) 555-1234", false)]
    [InlineData("{\"number\":\"5025551234\",\"isMessagingEnabled\":true}", false)]
    [InlineData("{\"number\":\"\",\"isMessagingEnabled\":false}", true)]
    [InlineData("( ) -", true)]
    public void A_phone_needs_a_digit(string value, bool blank)
    {
        Assert.Equal(blank, AnswerRules.IsBlank(AnswerKind.Phone, value));
    }

    [Theory]
    [InlineData("0", true)] // unknown
    [InlineData("1", false)]
    [InlineData("2", false)]
    public void Unknown_gender_is_blank(string value, bool blank)
    {
        Assert.Equal(blank, AnswerRules.IsBlank(AnswerKind.Gender, value));
    }
}

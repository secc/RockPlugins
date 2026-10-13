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
using System;
using System.Globalization;
using System.Linq;
using org.secc.RegistrationCart.Domain;
using Xunit;

namespace org.secc.RegistrationCart.Tests;

public class CheckoutRulesTests
{
    [Theory]
    [InlineData(ChargeOutcome.NotAttempted)]
    [InlineData(ChargeOutcome.Declined)]
    public void Cart_reopens_only_when_nothing_was_charged(ChargeOutcome outcome)
    {
        Assert.Equal(CartStatus.Open, CheckoutRules.StatusAfterStop(outcome));
    }

    [Fact]
    public void Charged_cart_closes_even_if_registrations_failed()
    {
        Assert.Equal(CartStatus.CheckedOut, CheckoutRules.StatusAfterStop(ChargeOutcome.Charged));
    }

    [Fact]
    public void Unknown_charge_keeps_the_cart_claimed()
    {
        Assert.Equal(CartStatus.Processing, CheckoutRules.StatusAfterStop(ChargeOutcome.Unknown));
    }

    [Fact]
    public void Every_outcome_that_might_have_charged_keeps_the_cart_from_reopening()
    {
        var mightHaveCharged = Enum.GetValues(typeof(ChargeOutcome)).Cast<ChargeOutcome>()
            .Where(o => o != ChargeOutcome.NotAttempted && o != ChargeOutcome.Declined);

        Assert.All(mightHaveCharged, o => Assert.NotEqual(CartStatus.Open, CheckoutRules.StatusAfterStop(o)));
    }

    [Theory]
    [InlineData("125.00", "125.00")]
    [InlineData("125", "125.00")]
    [InlineData("0", "0.00")]
    [InlineData("0.30000000000000004", "0.30")] // 0.1 + 0.2 after a trip through a JavaScript number
    public void Shown_total_matches_the_same_amount_in_cents(string shown, string current)
    {
        Assert.True(CheckoutRules.TotalMatches(decimal.Parse(shown, CultureInfo.InvariantCulture), decimal.Parse(current, CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData("125.00", "125.01")] // a cent either way is a different charge
    [InlineData("125.01", "125.00")]
    [InlineData("0", "50.00")]      // a free cart that now costs money
    [InlineData("150.00", "125.00")] // a line was removed in another tab
    public void Any_difference_in_cents_is_a_changed_cart(string shown, string current)
    {
        Assert.False(CheckoutRules.TotalMatches(decimal.Parse(shown, CultureInfo.InvariantCulture), decimal.Parse(current, CultureInfo.InvariantCulture)));
    }
}

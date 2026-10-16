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

    // ---- registration window -------------------------------------------

    private static readonly DateTime Now = new(2027, 3, 1, 9, 0, 0);

    [Fact]
    public void Window_with_no_dates_is_always_open()
    {
        Assert.True(CheckoutRules.IsWindowOpen(null, null, Now));
    }

    [Fact]
    public void Window_is_open_from_start_through_end_inclusive()
    {
        Assert.True(CheckoutRules.IsWindowOpen(Now, Now.AddDays(30), Now));
        Assert.True(CheckoutRules.IsWindowOpen(Now.AddDays(-30), Now, Now));
    }

    [Fact]
    public void Window_is_closed_before_it_opens_and_after_it_ends()
    {
        Assert.False(CheckoutRules.IsWindowOpen(Now.AddMinutes(1), null, Now));
        Assert.False(CheckoutRules.IsWindowOpen(null, Now.AddMinutes(-1), Now));
    }

    // ---- capacity ------------------------------------------------------

    [Theory]
    [InlineData(null, 500, 3)]
    [InlineData(0, 500, 3)] // Rock treats 0 as no limit
    public void No_limit_seats_everyone(int? max, int taken, int wanted)
    {
        Assert.Equal(new SeatAssignment(wanted, 0, 0), CheckoutRules.AssignSeats(max, taken, wanted, waitListEnabled: false));
    }

    [Theory]
    [InlineData(100, 97, 3, 3, 0)]  // exactly fills the camp
    [InlineData(100, 98, 3, 2, 1)]  // one of this cart's campers doesn't fit
    [InlineData(100, 100, 2, 0, 2)] // full before this cart
    [InlineData(100, 105, 1, 0, 1)] // already overbooked (e.g. staff added registrants): still just this cart's line
    public void Campers_who_do_not_fit_are_refused_without_a_wait_list(int max, int taken, int wanted, int seated, int refused)
    {
        Assert.Equal(new SeatAssignment(seated, 0, refused), CheckoutRules.AssignSeats(max, taken, wanted, waitListEnabled: false));
    }

    [Theory]
    [InlineData(100, 98, 3, 2, 1)]
    [InlineData(100, 100, 2, 0, 2)]
    [InlineData(100, 105, 1, 0, 1)]
    public void Campers_who_do_not_fit_go_on_the_wait_list_when_the_template_has_one(int max, int taken, int wanted, int seated, int waitListed)
    {
        Assert.Equal(new SeatAssignment(seated, waitListed, 0), CheckoutRules.AssignSeats(max, taken, wanted, waitListEnabled: true));
    }
}

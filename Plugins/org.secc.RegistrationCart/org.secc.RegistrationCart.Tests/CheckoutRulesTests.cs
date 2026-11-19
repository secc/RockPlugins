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
    [InlineData(CartStatus.Processing, null, false, true)]                // the gateway didn't answer, or the request died
    [InlineData(CartStatus.CheckedOut, "Payment X succeeded but registrations could not be created", false, true)]
    [InlineData(CartStatus.CheckedOut, "Payment X succeeded but registrations could not be created", true, false)] // staff closed it
    [InlineData(CartStatus.CheckedOut, null, false, false)]               // paid and registered, or a free cart
    [InlineData(CartStatus.CheckedOut, "  ", false, false)]
    [InlineData(CartStatus.Open, "Payment was declined.", false, false)]  // declined: nothing charged
    [InlineData(CartStatus.Abandoned, "Payment outcome unknown", true, false)] // a claimed cart staff closed
    public void Unconfirmed_checkouts_are_the_ones_that_may_have_been_charged(CartStatus status, string? error, bool resolved, bool pending)
    {
        Assert.Equal(pending, CheckoutRules.IsPendingCheckout(status, error, resolved));
    }

    [Fact]
    public void A_charged_cart_with_its_payment_recorded_is_still_unconfirmed_until_it_registers()
    {
        // The payment is saved before the registrations, so having one no longer means "done".
        Assert.True(CheckoutRules.IsPendingCheckout(CartStatus.CheckedOut, "Payment X (transaction 12) succeeded but registrations could not be created", isResolved: false));
    }

    [Theory]
    [InlineData(true, null, ChargeOutcome.Charged)]
    [InlineData(true, "", ChargeOutcome.Charged)]
    [InlineData(false, "Card declined", ChargeOutcome.Declined)]
    [InlineData(true, "Card declined", ChargeOutcome.Declined)]   // an error wins, as Rock treats it
    [InlineData(false, null, ChargeOutcome.Unknown)]              // a timeout some gateways report as "nothing"
    [InlineData(false, "   ", ChargeOutcome.Unknown)]
    public void A_gateway_answer_with_neither_a_payment_nor_an_error_is_not_a_decline(bool hasPayment, string? error, ChargeOutcome outcome)
    {
        Assert.Equal(outcome, CheckoutRules.OutcomeOfCharge(hasPayment, error));
        if (outcome == ChargeOutcome.Unknown)
            Assert.NotEqual(CartStatus.Open, CheckoutRules.StatusAfterStop(outcome));
    }

    [Fact]
    public void A_claimed_checkout_is_left_alone_while_it_may_still_be_running()
    {
        var now = new DateTime(2026, 11, 15, 12, 0, 0);

        Assert.False(CheckoutRules.IsQuiet(now.AddMinutes(-1), now));
        Assert.False(CheckoutRules.IsQuiet(now.AddMinutes(-CheckoutRules.MinutesBeforeResolving).AddSeconds(1), now));
        Assert.True(CheckoutRules.IsQuiet(now.AddMinutes(-CheckoutRules.MinutesBeforeResolving), now));
        Assert.True(CheckoutRules.IsQuiet(null, now));

        // A shorter wait than the minimum is raised to it; a longer one is honoured.
        Assert.False(CheckoutRules.IsQuiet(now.AddMinutes(-2), now, minutes: 1));
        Assert.False(CheckoutRules.IsQuiet(now.AddMinutes(-20), now, minutes: 30));
        Assert.True(CheckoutRules.IsQuiet(now.AddMinutes(-31), now, minutes: 30));
    }

    [Fact]
    public void Only_a_checkout_that_never_reached_the_gateway_reopens_by_itself()
    {
        var now = new DateTime(2026, 11, 15, 12, 0, 0);
        var claimed = now.AddMinutes(-20);

        Assert.True(CheckoutRules.CanReopenUnsent(CartStatus.Processing, paymentSent: null, claimed, now, minutes: 15));
        Assert.False(CheckoutRules.CanReopenUnsent(CartStatus.Processing, paymentSent: claimed, claimed, now, minutes: 15)); // may have been charged
        Assert.False(CheckoutRules.CanReopenUnsent(CartStatus.Processing, paymentSent: null, now.AddMinutes(-10), now, minutes: 15)); // too soon
        Assert.False(CheckoutRules.CanReopenUnsent(CartStatus.CheckedOut, paymentSent: null, claimed, now, minutes: 15));
        Assert.False(CheckoutRules.CanReopenUnsent(CartStatus.Open, paymentSent: null, claimed, now, minutes: 15));
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

    [Fact]
    public void No_limit_seats_everyone()
    {
        Assert.Null(CheckoutRules.SpotsAvailable(new SeatCounts(null, 500, 10, true, 4)));
        Assert.Equal(new SeatAssignment(3, 0, 0), CheckoutRules.AssignSeats(null, 3, waitListEnabled: false));
    }

    [Fact]
    public void Zero_max_attendees_has_no_spots()
    {
        // Rock: only a blank Max Attendees is unlimited; 0 makes the camp wait-list-only (or closed).
        Assert.Equal(0, CheckoutRules.SpotsAvailable(new SeatCounts(0, 0, 0, false, 0)));
        Assert.Equal(new SeatAssignment(0, 2, 0), CheckoutRules.AssignSeats(0, 2, waitListEnabled: true));
        Assert.Equal(new SeatAssignment(0, 0, 2), CheckoutRules.AssignSeats(0, 2, waitListEnabled: false));
    }

    [Fact]
    public void Wait_listed_registrants_take_seats_as_in_Rock()
    {
        // 50 registered + 5 waiting counts 55 of 50: a cancellation frees nothing for newcomers.
        Assert.Equal(0, CheckoutRules.SpotsAvailable(new SeatCounts(50, 54, 0, false, 0)));
    }

    [Theory]
    [InlineData(true, 3)]  // timeout on: Rock's in-progress registrations hold seats
    [InlineData(false, 5)] // timeout off: they hold nothing
    public void Rock_sessions_count_only_with_the_registration_timeout_on(bool timeoutEnabled, int expected)
    {
        Assert.Equal(expected, CheckoutRules.SpotsAvailable(new SeatCounts(10, 5, 2, timeoutEnabled, 0)));
    }

    [Fact]
    public void Rock_sessions_count_only_while_seats_remain()
    {
        // As Rock: once registrants fill the camp, sessions don't take it further below zero.
        Assert.Equal(0, CheckoutRules.SpotsAvailable(new SeatCounts(10, 10, 4, true, 0)));
    }

    [Fact]
    public void Lines_in_other_carts_mid_checkout_always_take_seats()
    {
        Assert.Equal(1, CheckoutRules.SpotsAvailable(new SeatCounts(10, 6, 0, false, 3)));
    }

    [Fact]
    public void Spots_never_go_below_zero()
    {
        Assert.Equal(0, CheckoutRules.SpotsAvailable(new SeatCounts(10, 12, 0, false, 2))); // staff overfilled it
    }

    [Fact]
    public void Fee_item_without_a_maximum_usage_has_no_limit()
    {
        Assert.Null(CheckoutRules.FeeItemRemaining(null, 40, 3));
    }

    [Theory]
    [InlineData(20, 15, 2, 3)]
    [InlineData(20, 20, 0, 0)] // sold out
    [InlineData(20, 19, 2, 0)] // carts mid-checkout hold the last one
    [InlineData(20, 25, 0, 0)] // staff went past the limit
    public void Fee_item_remaining_is_the_limit_less_used_and_held(int max, int used, int held, int expected)
    {
        Assert.Equal(expected, CheckoutRules.FeeItemRemaining(max, used, held));
    }

    [Theory]
    [InlineData(3, 3, 3, 0)] // exactly fills the camp
    [InlineData(2, 3, 2, 1)] // one of this cart's campers doesn't fit
    [InlineData(0, 2, 0, 2)] // full before this cart
    public void Campers_who_do_not_fit_are_refused_without_a_wait_list(int spots, int wanted, int seated, int refused)
    {
        Assert.Equal(new SeatAssignment(seated, 0, refused), CheckoutRules.AssignSeats(spots, wanted, waitListEnabled: false));
    }

    [Theory]
    [InlineData(2, 3, 2, 1)]
    [InlineData(0, 2, 0, 2)]
    public void Campers_who_do_not_fit_go_on_the_wait_list_when_the_template_has_one(int spots, int wanted, int seated, int waitListed)
    {
        Assert.Equal(new SeatAssignment(seated, waitListed, 0), CheckoutRules.AssignSeats(spots, wanted, waitListEnabled: true));
    }
}

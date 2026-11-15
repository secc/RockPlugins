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

namespace org.secc.RegistrationCart.Domain;

/// <summary>
/// Checkout rules. A checkout claims its cart (Open → Processing) before it calls the gateway;
/// these rules decide whether it may charge and where the cart goes when it stops.
/// </summary>
public static class CheckoutRules
{
    /// <summary>
    /// The status a claimed cart is left in when checkout stops. The cart goes back to Open
    /// (so the shopper can retry) only when nothing was charged. A completed charge closes it,
    /// and an unknown outcome keeps it claimed so no retry can charge it a second time.
    /// </summary>
    public static CartStatus StatusAfterStop(ChargeOutcome outcome) => outcome switch
    {
        ChargeOutcome.NotAttempted => CartStatus.Open,
        ChargeOutcome.Declined => CartStatus.Open,
        ChargeOutcome.Charged => CartStatus.CheckedOut,
        _ => CartStatus.Processing
    };

    /// <summary>
    /// What a gateway's answer to a charge means. An error message is the gateway saying no:
    /// nothing was charged. A payment with no error is a charge. Neither is no answer at all
    /// (some gateways report a timeout or an unreadable response this way), so the card may or
    /// may not have been charged and the cart must not reopen until someone has checked.
    /// </summary>
    public static ChargeOutcome OutcomeOfCharge(bool hasPayment, string? errorMessage) =>
        !string.IsNullOrWhiteSpace(errorMessage) ? ChargeOutcome.Declined
        : hasPayment ? ChargeOutcome.Charged
        : ChargeOutcome.Unknown;

    /// <summary>
    /// A checkout that took, or may have taken, a payment without finishing: still claimed
    /// (the gateway didn't answer, or the request died), or closed with an error (charged but
    /// the registrations failed). Staff check the gateway and reopen or close it on the settings
    /// page; until then the payer is warned and its campers can't be registered again.
    /// </summary>
    /// <param name="isResolved">Staff closed it.</param>
    public static bool IsPendingCheckout(CartStatus status, string? lastCheckoutError, bool isResolved) =>
        status == CartStatus.Processing
        || (status == CartStatus.CheckedOut && !isResolved && !string.IsNullOrWhiteSpace(lastCheckoutError));

    /// <summary>
    /// How long a claimed checkout is left alone before anyone (staff or the maintenance job) may
    /// reopen or close it: the request that claimed it may still be waiting on the gateway.
    /// </summary>
    public const int MinutesBeforeResolving = 5;

    /// <summary>
    /// Whether a claimed checkout has been quiet long enough to be reopened or closed
    /// (<see cref="MinutesBeforeResolving"/>, or <paramref name="minutes"/> when longer).
    /// </summary>
    public static bool IsQuiet(DateTime? lastChanged, DateTime now, int minutes = MinutesBeforeResolving) =>
        lastChanged is null || lastChanged.Value <= now.AddMinutes(-Math.Max(minutes, MinutesBeforeResolving));

    /// <summary>
    /// A checkout the maintenance job may reopen by itself: still claimed, quiet for
    /// <paramref name="minutes"/>, and its charge was never sent to the gateway (the request
    /// died first), so nothing can have been charged.
    /// </summary>
    public static bool CanReopenUnsent(CartStatus status, DateTime? paymentSent, DateTime? lastChanged, DateTime now, int minutes) =>
        status == CartStatus.Processing && paymentSent is null && IsQuiet(lastChanged, now, minutes);

    /// <summary>
    /// Whether the cart still prices to the total the shopper was shown. Compared in cents, so
    /// a total that went through a JavaScript number (e.g. 0.30000000000000004) still matches.
    /// </summary>
    public static bool TotalMatches(decimal shownTotal, decimal currentTotal) =>
        Math.Round(shownTotal, 2, MidpointRounding.AwayFromZero) == Math.Round(currentTotal, 2, MidpointRounding.AwayFromZero);

    /// <summary>Whether registration is open at <paramref name="now"/>. A missing start or end leaves that side open; both ends are inclusive.</summary>
    public static bool IsWindowOpen(DateTime? start, DateTime? end, DateTime now) =>
        (start is null || start.Value <= now) && (end is null || end.Value >= now);

    /// <summary>
    /// Seats a camp has left for one cart, counted the way Rock's own registration does
    /// (RegistrationInstanceService.GetSpotsAvailable) plus the other carts mid-checkout. Null
    /// means no limit: only a missing Max Attendees is unlimited, and 0 means no seats. Everyone
    /// registered counts, wait list included, so a seat that frees up stays with the wait list
    /// until staff move someone off it. In-progress registrations in Rock (sessions) count only
    /// when the instance has Rock's registration timeout on, and only while seats remain. Open
    /// carts hold nothing: whoever checks out first gets the seat.
    /// </summary>
    public static int? SpotsAvailable(SeatCounts counts)
    {
        if (counts.MaxAttendees is not int max)
            return null;

        var spots = max - counts.Registrants;
        if (spots > 0 && counts.TimeoutEnabled)
            spots -= counts.SessionRegistrants;

        spots -= counts.ProcessingLines;
        return Math.Max(0, spots);
    }

    /// <summary>
    /// How many of a fee item with a Maximum Usage (Rock's per-instance limit) are left for one
    /// cart: the limit less what registrants already have and what other carts mid-checkout hold.
    /// Null means no limit.
    /// </summary>
    public static int? FeeItemRemaining(int? maximumUsage, int used, int held) =>
        maximumUsage is int max ? Math.Max(0, max - used - held) : null;

    /// <summary>
    /// Splits one cart's <paramref name="wanted"/> campers for a camp into the seats that are
    /// left and the rest. The first campers in cart order get the seats; the rest go on the
    /// wait list when the template has one, else they are refused.
    /// </summary>
    /// <param name="spotsAvailable">From <see cref="SpotsAvailable"/>; null means no limit.</param>
    public static SeatAssignment AssignSeats(int? spotsAvailable, int wanted, bool waitListEnabled)
    {
        var seated = spotsAvailable is int spots ? Math.Max(0, Math.Min(wanted, spots)) : wanted;
        var rest = wanted - seated;
        return waitListEnabled ? new SeatAssignment(seated, rest, 0) : new SeatAssignment(seated, 0, rest);
    }
}

/// <summary>How one cart's campers for one camp are placed: in a seat, on the wait list, or refused.</summary>
public readonly record struct SeatAssignment(int Seated, int WaitListed, int Refused);

/// <summary>
/// What takes a camp's seats besides the cart being placed.
/// </summary>
/// <param name="MaxAttendees">Rock's Max Attendees on the instance (null = no limit).</param>
/// <param name="Registrants">Registrants on the instance's non-temporary registrations, wait list included.</param>
/// <param name="SessionRegistrants">Registrants in Rock registration sessions that haven't expired.</param>
/// <param name="TimeoutEnabled">The instance has Rock's registration timeout on (sessions hold seats).</param>
/// <param name="ProcessingLines">Lines in other carts mid-checkout, which are about to become registrants.</param>
public readonly record struct SeatCounts(int? MaxAttendees, int Registrants, int SessionRegistrants, bool TimeoutEnabled, int ProcessingLines);

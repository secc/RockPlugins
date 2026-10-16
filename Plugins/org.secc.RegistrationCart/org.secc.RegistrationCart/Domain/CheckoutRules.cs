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
    /// Whether the cart still prices to the total the shopper was shown. Compared in cents, so
    /// a total that went through a JavaScript number (e.g. 0.30000000000000004) still matches.
    /// </summary>
    public static bool TotalMatches(decimal shownTotal, decimal currentTotal) =>
        Math.Round(shownTotal, 2, MidpointRounding.AwayFromZero) == Math.Round(currentTotal, 2, MidpointRounding.AwayFromZero);

    /// <summary>Whether registration is open at <paramref name="now"/>. A missing start or end leaves that side open; both ends are inclusive.</summary>
    public static bool IsWindowOpen(DateTime? start, DateTime? end, DateTime now) =>
        (start is null || start.Value <= now) && (end is null || end.Value >= now);

    /// <summary>
    /// Splits one cart's <paramref name="wanted"/> campers for a camp into the seats that are
    /// left and the rest. The first campers in cart order get the seats; the rest go on the
    /// wait list when the template has one, else they are refused. A missing or zero
    /// <paramref name="maxAttendees"/> means no limit (Rock's convention).
    /// </summary>
    /// <param name="taken">Seats already taken by others (even past the limit, e.g. staff overfilled it).</param>
    public static SeatAssignment AssignSeats(int? maxAttendees, int taken, int wanted, bool waitListEnabled)
    {
        var seated = maxAttendees is > 0 ? Math.Max(0, Math.Min(wanted, maxAttendees.Value - taken)) : wanted;
        var rest = wanted - seated;
        return waitListEnabled ? new SeatAssignment(seated, rest, 0) : new SeatAssignment(seated, 0, rest);
    }
}

/// <summary>How one cart's campers for one camp are placed: in a seat, on the wait list, or refused.</summary>
public readonly record struct SeatAssignment(int Seated, int WaitListed, int Refused);

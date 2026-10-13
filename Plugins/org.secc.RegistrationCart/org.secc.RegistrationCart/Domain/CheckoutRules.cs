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
    /// How many of <paramref name="wanted"/> seats don't fit: 0 when they all fit, never more
    /// than <paramref name="wanted"/> (even if others already overfilled it). A missing or zero
    /// <paramref name="maxAttendees"/> means no limit (Rock's convention).
    /// </summary>
    /// <param name="taken">Seats already taken by others.</param>
    public static int SeatsShort(int? maxAttendees, int taken, int wanted) =>
        maxAttendees is > 0 ? Math.Min(wanted, Math.Max(0, taken + wanted - maxAttendees.Value)) : 0;
}

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
using System.Collections.Generic;
using System.Linq;

namespace org.secc.RegistrationCart.Domain.Pricing;

/// <summary>
/// Splits one applied discount across cart lines in whole cents, so each line's payment can
/// post to its own account. Works in cents (<see cref="long"/>) so the shares always add up
/// exactly. Pure — no Rock.
/// </summary>
public static class DiscountAllocator
{
    /// <summary>
    /// Allocates <paramref name="amountCents"/> to the lines with a positive weight, in
    /// proportion to <paramref name="weights"/>, never giving a line more than its
    /// <paramref name="capacityCents"/> (what is left of its price). If the discount is worth more
    /// than its own lines have left, the excess goes to the other lines in proportion to what
    /// they have left, so the cart total is unchanged. Anything beyond the whole cart's capacity
    /// is not allocated.
    /// </summary>
    /// <returns>Cents per line, indexed like the inputs.</returns>
    public static long[] Allocate(long amountCents, IReadOnlyList<decimal> weights, IReadOnlyList<long> capacityCents)
    {
        if (weights.Count != capacityCents.Count)
            throw new ArgumentException("Each line needs a weight and a capacity.");

        var shares = Distribute(amountCents, weights, capacityCents);

        var left = amountCents - shares.Sum();
        if (left > 0)
        {
            var room = capacityCents.Select((cap, i) => cap - shares[i]).ToArray();
            var spill = Distribute(left, room.Select(r => (decimal)r).ToArray(), room);
            for (var i = 0; i < shares.Length; i++)
                shares[i] += spill[i];
        }

        return shares;
    }

    /// <summary>
    /// Proportional split with caps: largest-remainder rounding (ties to the earlier line), and
    /// whatever a capped line can't take is re-split among the lines that still have room.
    /// Gives out min(amount, total capacity of the weighted lines).
    /// </summary>
    public static long[] Distribute(long amountCents, IReadOnlyList<decimal> weights, IReadOnlyList<long> capacityCents)
    {
        var shares = new long[weights.Count];
        var remaining = amountCents;

        while (remaining > 0)
        {
            var open = Enumerable.Range(0, weights.Count)
                .Where(i => weights[i] > 0m && shares[i] < capacityCents[i])
                .ToList();
            if (open.Count == 0)
                break;

            var totalWeight = open.Sum(i => weights[i]);
            var exact = open.ToDictionary(i => i, i => remaining * weights[i] / totalWeight);
            var round = open.ToDictionary(i => i, i => (long)Math.Floor(exact[i]));

            var leftover = remaining - round.Values.Sum();
            foreach (var i in open.OrderByDescending(i => exact[i] - round[i]).ThenBy(i => i).Take((int)leftover))
                round[i]++;

            var hitCap = false;
            foreach (var i in open)
            {
                var give = Math.Min(round[i], capacityCents[i] - shares[i]);
                hitCap |= give < round[i];
                shares[i] += give;
                remaining -= give;
            }

            // Without a capped line everything was given out; with one, re-split its excess.
            if (!hitCap)
                break;
        }

        return shares;
    }
}

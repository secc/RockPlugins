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
namespace org.secc.RegistrationCart.Domain;

/// <summary>
/// Grade bands a registrant can fall into. Modelled as flags so an offering
/// variant or add-on can declare the set of bands it is available to.
/// A registrant always has exactly one band; eligibility is a bitwise overlap.
/// This is the camp-specific configuration of the generic "eligibility" concept.
/// </summary>
[Flags]
public enum GradeBand
{
    /// <summary>Unknown — e.g. a child with no grade on file. Never eligible.</summary>
    None = 0,
    Elementary = 1,
    MiddleSchool = 2,
    HighSchool = 4,
    /// <summary>Every student band (no adults).</summary>
    All = Elementary | MiddleSchool | HighSchool,
    /// <summary>Adults (graduated / no grade). Lets non-camp offerings sell to grown-ups.</summary>
    Adults = 8,
    Everyone = All | Adults
}

/// <summary>
/// The kind of thing being sold. Camp is the first use case; the rest exist so
/// the catalog/cart engine stays generic and can sell them later without a rebuild.
/// </summary>
public enum OfferingType
{
    Camp = 0,
    Retreat = 1,
    MissionTrip = 2,
    Class = 3,
    Event = 4,
    Merchandise = 5
}

/// <summary>Whether an add-on group allows one selection or many.</summary>
public enum SelectionMode
{
    Single = 0,
    Multi = 1
}

public enum DiscountType
{
    /// <summary>Fixed currency amount off.</summary>
    Amount = 0,
    /// <summary>Percentage off the scoped total.</summary>
    Percent = 1
}

/// <summary>What a discount code applies to.</summary>
public enum DiscountScope
{
    /// <summary>Applies to the whole order subtotal.</summary>
    Order = 0,
    /// <summary>Applies per eligible line item.</summary>
    LineItem = 1,
    /// <summary>Applies only to line items of a specific offering.</summary>
    Offering = 2
}

public enum CartStatus
{
    Open = 0,
    CheckedOut = 1,
    Abandoned = 2,
    /// <summary>
    /// Claimed by a checkout that is charging the gateway. Only one request can move a cart
    /// Open → Processing, so only one can charge it. A cart left here means the payment
    /// outcome is unknown and needs checking against the gateway.
    /// </summary>
    Processing = 3
}

/// <summary>What happened at the payment gateway when a checkout stopped.</summary>
public enum ChargeOutcome
{
    /// <summary>Stopped before the gateway was called (nothing to charge, or a problem found first).</summary>
    NotAttempted = 0,
    /// <summary>The gateway answered with an error: nothing was charged.</summary>
    Declined = 1,
    /// <summary>The gateway took the money.</summary>
    Charged = 2,
    /// <summary>The gateway call failed without an answer: the card may or may not have been charged.</summary>
    Unknown = 3
}

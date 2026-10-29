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
using org.secc.RegistrationCart.Domain.Entities;

namespace org.secc.RegistrationCart.Domain.Pricing;

/// <summary>
/// Decides which variants and add-ons a registrant may select, based on their
/// grade band. Pure and side-effect free so it can drive both the UI filtering
/// and be unit tested directly. This is the generic eligibility layer; grade band
/// is simply the camp configuration of it.
/// </summary>
public class EligibilityService
{
    /// <summary>A variant is eligible if its band set overlaps the registrant's band.</summary>
    public bool IsVariantEligible(OfferingVariant variant, GradeBand registrantBand) =>
        registrantBand != GradeBand.None
        && (variant.EligibleGradeBands & registrantBand) != 0;

    /// <summary>An add-on is eligible if its band set overlaps the registrant's band.</summary>
    public bool IsAddOnEligible(AddOnItem addOn, GradeBand registrantBand) =>
        registrantBand != GradeBand.None
        && (addOn.EligibleGradeBands & registrantBand) != 0;

    /// <summary>The variants of an offering a registrant may register for.</summary>
    public IEnumerable<OfferingVariant> EligibleVariants(Offering offering, GradeBand registrantBand) =>
        offering.Variants.Where(v => IsVariantEligible(v, registrantBand));

    /// <summary>The add-on items in a group a registrant may select.</summary>
    public IEnumerable<AddOnItem> EligibleAddOns(AddOnGroup group, GradeBand registrantBand) =>
        group.Items.Where(i => IsAddOnEligible(i, registrantBand));
}

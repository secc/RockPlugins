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
using org.secc.RegistrationCart.Domain;
using org.secc.RegistrationCart.Domain.Entities;
using org.secc.RegistrationCart.Domain.Pricing;
using Xunit;

namespace org.secc.RegistrationCart.Tests;

public class EligibilityServiceTests
{
    private readonly EligibilityService _svc = new();

    [Theory]
    [InlineData(GradeBand.MiddleSchool, GradeBand.MiddleSchool, true)]
    [InlineData(GradeBand.MiddleSchool | GradeBand.HighSchool, GradeBand.HighSchool, true)]
    [InlineData(GradeBand.Elementary, GradeBand.MiddleSchool, false)]
    [InlineData(GradeBand.All, GradeBand.Elementary, true)]
    public void IsVariantEligible_matches_on_band_overlap(GradeBand variantBands, GradeBand registrant, bool expected)
    {
        var variant = new OfferingVariant { EligibleGradeBands = variantBands };
        Assert.Equal(expected, _svc.IsVariantEligible(variant, registrant));
    }

    [Fact]
    public void Registrant_with_no_band_is_never_eligible()
    {
        var variant = new OfferingVariant { EligibleGradeBands = GradeBand.All };
        Assert.False(_svc.IsVariantEligible(variant, GradeBand.None));
    }

    [Fact]
    public void EligibleVariants_filters_offering_to_matching_bands()
    {
        var offering = new Offering
        {
            Variants =
            {
                new OfferingVariant { Name = "Elem", EligibleGradeBands = GradeBand.Elementary },
                new OfferingVariant { Name = "Mid", EligibleGradeBands = GradeBand.MiddleSchool },
                new OfferingVariant { Name = "High", EligibleGradeBands = GradeBand.HighSchool },
            }
        };

        var result = _svc.EligibleVariants(offering, GradeBand.MiddleSchool).ToList();

        Assert.Single(result);
        Assert.Equal("Mid", result[0].Name);
    }

    [Fact]
    public void EligibleAddOns_gates_items_by_band()
    {
        var group = new AddOnGroup
        {
            Items =
            {
                new AddOnItem { Name = "Zipline", EligibleGradeBands = GradeBand.MiddleSchool | GradeBand.HighSchool },
                new AddOnItem { Name = "Crafts", EligibleGradeBands = GradeBand.Elementary },
                new AddOnItem { Name = "Horseback", EligibleGradeBands = GradeBand.All },
            }
        };

        var elementary = _svc.EligibleAddOns(group, GradeBand.Elementary).Select(i => i.Name).ToList();
        var high = _svc.EligibleAddOns(group, GradeBand.HighSchool).Select(i => i.Name).ToList();

        Assert.Equal(new[] { "Crafts", "Horseback" }, elementary);
        Assert.Equal(new[] { "Zipline", "Horseback" }, high);
    }
}

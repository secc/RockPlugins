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
using org.secc.RegistrationCart.Domain;
using Xunit;

namespace org.secc.RegistrationCart.Tests;

/// <summary>Template discount codes are checked as Rock's RegistrationEntry checks them.</summary>
public class DiscountRulesTests
{
    private static readonly DateTime Today = new(2026, 11, 6);

    private static TemplateDiscountTerms Terms(int? maxUsage = null, int? minRegistrants = null, int? maxRegistrants = null, DateTime? start = null, DateTime? end = null) =>
        new("26CAMPSTAFF50", 0.5m, 0m, maxUsage, minRegistrants, maxRegistrants, start, end);

    [Fact]
    public void Unknown_code_is_not_valid()
    {
        Assert.Equal(DiscountProblem.NotFound, DiscountRules.Check(null, Today, 0));
    }

    [Fact]
    public void Code_with_no_limits_can_be_used_any_number_of_times()
    {
        Assert.Equal(DiscountProblem.None, DiscountRules.Check(Terms(), Today, 220));
    }

    [Fact]
    public void Single_use_code_is_used_up_once_used()
    {
        Assert.Equal(DiscountProblem.None, DiscountRules.Check(Terms(maxUsage: 1), Today, 0));
        Assert.Equal(DiscountProblem.UsedUp, DiscountRules.Check(Terms(maxUsage: 1), Today, 1));
    }

    [Fact]
    public void Dates_are_inclusive()
    {
        var window = Terms(start: new DateTime(2025, 9, 1), end: Today);
        Assert.Equal(DiscountProblem.None, DiscountRules.Check(window, Today, 0));
        Assert.Equal(DiscountProblem.NotYetAvailable, DiscountRules.Check(Terms(start: Today.AddDays(1)), Today, 0));
        Assert.Equal(DiscountProblem.Expired, DiscountRules.Check(Terms(end: Today.AddDays(-1)), Today, 0));
    }

    [Fact]
    public void Code_needing_more_than_one_registrant_never_fits_a_cart_registration()
    {
        Assert.Equal(DiscountProblem.None, DiscountRules.Check(Terms(minRegistrants: 1), Today, 0));
        Assert.Equal(DiscountProblem.TooFewRegistrants, DiscountRules.Check(Terms(minRegistrants: 2), Today, 0));
    }

    [Fact]
    public void Problems_are_reported_in_Rocks_order()
    {
        // Too few registrants is reported before the dates and the usage, as Rock checks them.
        var everything = Terms(maxUsage: 1, minRegistrants: 2, end: Today.AddDays(-1));
        Assert.Equal(DiscountProblem.TooFewRegistrants, DiscountRules.Check(everything, Today, 5));
    }

    [Fact]
    public void Maximum_registrants_of_zero_takes_nothing_off()
    {
        Assert.True(DiscountRules.AppliesToRegistrant(Terms()));
        Assert.True(DiscountRules.AppliesToRegistrant(Terms(maxRegistrants: 1)));
        Assert.False(DiscountRules.AppliesToRegistrant(Terms(maxRegistrants: 0)));
    }

    [Fact]
    public void Codes_match_ignoring_case_and_spaces()
    {
        Assert.True(DiscountRules.SameCode(" 26campstaff50 ", "26CAMPSTAFF50"));
        Assert.False(DiscountRules.SameCode("26CAMPSTAFF50", "26CAMPSTAFF5"));
        Assert.False(DiscountRules.SameCode(null, "X"));
    }

    [Fact]
    public void Messages_use_the_templates_term_as_Rock_words_them()
    {
        Assert.Equal("'ABC' is not a valid Promo Code.", DiscountRules.Message(DiscountProblem.NotFound, "ABC", "Promo Code"));
        Assert.Equal("The 'ABC' Discount Code is no longer available.", DiscountRules.Message(DiscountProblem.UsedUp, "ABC", ""));
        Assert.Equal("The 'ABC' Discount Code requires at least 2 registrants.", DiscountRules.Message(DiscountProblem.TooFewRegistrants, "ABC", "Discount Code", 2));
    }
}

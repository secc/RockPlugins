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
using org.secc.RegistrationCart.Domain;
using org.secc.RegistrationCart.Domain.Entities;
using org.secc.RegistrationCart.Domain.Pricing;
using org.secc.RegistrationCart.Services;
using Xunit;

namespace org.secc.RegistrationCart.Tests;

/// <summary>
/// Tests for <see cref="GradeBands.FromGradeOffset"/>, which maps Rock's GradeOffset
/// (years until high-school graduation: 0 = 12th grade, 12 = Kindergarten) to the
/// cart's eligibility bands.
/// </summary>
public class GradeBandsTests
{
    [Theory]
    [InlineData( 0 )]   // 12th grade
    [InlineData( 1 )]   // 11th grade
    [InlineData( 2 )]   // 10th grade
    [InlineData( 3 )]   // 9th grade
    public void HighSchool_grades_0_to_3( int offset ) =>
        Assert.Equal( GradeBand.HighSchool, GradeBands.FromGradeOffset( offset, isChild: true ) );

    [Theory]
    [InlineData( 4 )]   // 8th grade
    [InlineData( 5 )]   // 7th grade
    [InlineData( 6 )]   // 6th grade
    public void MiddleSchool_grades_4_to_6( int offset ) =>
        Assert.Equal( GradeBand.MiddleSchool, GradeBands.FromGradeOffset( offset, isChild: true ) );

    [Theory]
    [InlineData( 7 )]   // 5th grade
    [InlineData( 9 )]   // 3rd grade
    [InlineData( 11 )]  // 1st grade
    [InlineData( 12 )]  // Kindergarten
    public void Elementary_grades_7_to_12( int offset ) =>
        Assert.Equal( GradeBand.Elementary, GradeBands.FromGradeOffset( offset, isChild: true ) );

    [Fact]
    public void PreK_offset_is_not_banded() =>
        Assert.Equal( GradeBand.None, GradeBands.FromGradeOffset( 13, isChild: true ) );

    [Fact]
    public void Child_without_grade_is_None() =>
        Assert.Equal( GradeBand.None, GradeBands.FromGradeOffset( null, isChild: true ) );

    [Fact]
    public void Adult_without_grade_is_Adults() =>
        Assert.Equal( GradeBand.Adults, GradeBands.FromGradeOffset( null, isChild: false ) );

    [Theory]
    [InlineData( GradeBand.HighSchool )]
    [InlineData( GradeBand.MiddleSchool )]
    [InlineData( GradeBand.Elementary )]
    public void New_child_grade_offset_round_trips_to_its_band( GradeBand band ) =>
        Assert.Equal( band, GradeBands.FromGradeOffset( GradeBands.GradeOffsetFor( band ), isChild: true ) );

    [Fact]
    public void Derived_band_feeds_the_eligibility_check()
    {
        var eligibility = new EligibilityService();
        var variant = new OfferingVariant { EligibleGradeBands = GradeBand.MiddleSchool | GradeBand.HighSchool };

        Assert.True( eligibility.IsVariantEligible( variant, GradeBands.FromGradeOffset( 5, isChild: true ) ) );   // 7th grade
        Assert.False( eligibility.IsVariantEligible( variant, GradeBands.FromGradeOffset( 9, isChild: true ) ) );  // 3rd grade
    }
}

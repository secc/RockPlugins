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
using Xunit;

namespace org.secc.RegistrationCart.Tests;

/// <summary>Tests for <see cref="CampNames.Parse"/>, using 2026 camp names.</summary>
public class CampNamesTests
{
    private static readonly CampusName[] Campuses =
    {
        new CampusName( 1, "Blankenbaker", "BB" ),
        new CampusName( 11, "Bullitt County", "BC" ),
        new CampusName( 6, "La Grange", "LA" ),
        new CampusName( 3, "Indiana", "IN" )
    };

    [Fact]
    public void AllCampusTemplate_TakesCampusFromInstanceName()
    {
        var c = CampNames.Parse( "2026 | HIGH SCHOOL CAMP | STUDENT UNDER 18 | ALL", "2026 | HIGH SCHOOL CAMP | STUDENT UNDER 18 | BULLITT COUNTY", Campuses )!;
        Assert.Equal( "High School Camp", c.Title );
        Assert.Equal( "2026", c.Year );
        Assert.Equal( "Student Under 18", c.RoleText );
        Assert.False( c.IsServing );
        Assert.Equal( 11, c.CampusId );
        Assert.Null( c.MinAge );
        Assert.Equal( 17, c.MaxAge );
    }

    [Fact]
    public void AgeSplitTemplates_ShareOneCamp()
    {
        var under = CampNames.Parse( "2026 | HIGH SCHOOL CAMP | STUDENT UNDER 18 | ALL", "2026 | HIGH SCHOOL CAMP | STUDENT UNDER 18 | BLANKENBAKER", Campuses )!;
        var over = CampNames.Parse( "2026 | HIGH SCHOOL CAMP | STUDENT 18 AND OVER | ALL", "2026 | HIGH SCHOOL CAMP | STUDENT 18 AND OVER | BLANKENBAKER", Campuses )!;
        Assert.Equal( under.CampKey, over.CampKey );
        Assert.Equal( 18, over.MinAge );
        Assert.Null( over.MaxAge );
    }

    [Fact]
    public void PerCampusTemplate_TakesCampusFromShortCode_WhenInstanceHasNone()
    {
        var c = CampNames.Parse( "2026 | 3RD-5TH | CAMPER | LA", "2026 | 3RD-5TH | CAMPER | TEST", Campuses )!;
        Assert.Equal( "3rd-5th Grade Camp", c.Title );
        Assert.Equal( 6, c.CampusId );
    }

    [Fact]
    public void PerCampusTemplates_ShareOneCamp()
    {
        var bb = CampNames.Parse( "2026 | MIDDLE SCHOOL CAMP | STUDENT | BB", "2026 | MIDDLE SCHOOL CAMP | STUDENT | BLANKENBAKER", Campuses )!;
        var la = CampNames.Parse( "2026 | MIDDLE SCHOOL CAMP | STUDENT | LA", "2026 | MIDDLE SCHOOL CAMP | STUDENT | LA GRANGE", Campuses )!;
        Assert.Equal( bb.CampKey, la.CampKey );
        Assert.Equal( 1, bb.CampusId );
        Assert.Equal( 6, la.CampusId );
    }

    [Theory]
    [InlineData( "2026 | MIDDLE SCHOOL CAMP | ADULT LEADER | ALL" )]
    [InlineData( "2026 | MIDDLE SCHOOL CAMP | MINOR LEADER | ALL" )]
    [InlineData( "2026 | HIGH SCHOOL CAMP | OPS TEAM | ALL" )]
    [InlineData( "2026 | Staff Kids Camp | MS/HS ADULT LEADER" )]
    public void LeaderAndOpsRoles_AreServing( string template )
    {
        Assert.True( CampNames.Parse( template, null, Campuses )!.IsServing );
    }

    [Fact]
    public void ServingAndCamperTemplates_AreSeparateCamps()
    {
        var camper = CampNames.Parse( "2026 | MIDDLE SCHOOL CAMP | STUDENT | BB", null, Campuses )!;
        var leader = CampNames.Parse( "2026 | MIDDLE SCHOOL CAMP | ADULT LEADER | ALL", null, Campuses )!;
        Assert.NotEqual( camper.CampKey, leader.CampKey );
    }

    [Fact]
    public void NoCampusInEitherName_IsEveryCampus()
    {
        var c = CampNames.Parse( "2026 | CAMP EMPOWER | CAMPER | ALL", "2026 | CAMP EMPOWER | CAMPER | ALL", Campuses )!;
        Assert.Equal( "Camp Empower", c.Title );
        Assert.Null( c.CampusId );
    }

    [Fact]
    public void MixedCaseCampName_IsKept()
    {
        var c = CampNames.Parse( "2026 | Staff Kids Camp - Elementary | CAMPER", null, Campuses )!;
        Assert.Equal( "Staff Kids Camp - Elementary", c.Title );
        Assert.Equal( "Camper", c.RoleText );
    }

    [Theory]
    [InlineData( "SAMPLE | Family Price Cap" )]
    [InlineData( "Mark Payment Test" )]
    [InlineData( "Wedding Reservation Form - Rev. 3/2020" )]
    [InlineData( "2026 | Something" )]
    [InlineData( null )]
    public void NamesNotInTheCampFormat_AreNotCamps( string? template )
    {
        Assert.Null( CampNames.Parse( template, "anything", Campuses ) );
    }
}

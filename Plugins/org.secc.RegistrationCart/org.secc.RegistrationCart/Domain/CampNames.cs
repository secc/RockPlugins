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
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace org.secc.RegistrationCart.Domain;

/// <summary>A campus as the name parser needs it.</summary>
public record CampusName( int Id, string Name, string ShortCode );

/// <summary>What a camp registration's names say about it.</summary>
/// <param name="CampKey">Groups the templates of one camp (year + camp + camper/serving).</param>
/// <param name="Title">The camp's name for families, e.g. "High School Camp", "3rd-5th Grade Camp".</param>
/// <param name="Year">The season, e.g. "2026" (empty when the name has none).</param>
/// <param name="RoleText">Who the template is for, e.g. "Student Under 18", "Adult Leader".</param>
/// <param name="IsServing">A leader or ops-team registration, not a camper's.</param>
/// <param name="CampusId">The campus the instance (or its template) is for; null = every campus.</param>
/// <param name="MinAge">Youngest age the role names ("18 AND OVER"), if any.</param>
/// <param name="MaxAge">Oldest age the role names ("UNDER 18"), if any.</param>
public record CampName( string CampKey, string Title, string Year, string RoleText, bool IsServing, int? CampusId, int? MinAge, int? MaxAge );

/// <summary>
/// Reads the camp, role and campus out of SECC's registration names so the catalog can show
/// one card per camp instead of one per template. Templates are named
/// "YEAR | CAMP | ROLE | CAMPUS" (the campus part is a short code such as BB, or ALL) and
/// their instances "YEAR | CAMP | ROLE | CAMPUS NAME". A template whose name doesn't start
/// with a year part is not a camp: it stays its own card under its own name.
/// </summary>
public static class CampNames
{
    private static readonly Regex YearPart = new Regex( @"^\d{4}$", RegexOptions.Compiled );
    private static readonly Regex GradeRange = new Regex( @"^(\d{1,2}(ST|ND|RD|TH)|K)\s*-\s*\d{1,2}(ST|ND|RD|TH)$", RegexOptions.Compiled | RegexOptions.IgnoreCase );
    private static readonly Regex Serving = new Regex( @"\b(LEADERS?|OPS)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase );
    private static readonly Regex AndOver = new Regex( @"\b(\d{1,2})\s*(AND|&)\s*(OVER|UP|OLDER)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase );
    private static readonly Regex Under = new Regex( @"\bUNDER\s*(\d{1,2})\b", RegexOptions.Compiled | RegexOptions.IgnoreCase );

    /// <summary>
    /// Parses a template name with one of its instance names. Returns null when the template
    /// name isn't in the camp format.
    /// </summary>
    public static CampName? Parse( string? templateName, string? instanceName, IReadOnlyCollection<CampusName> campuses )
    {
        var parts = Split( templateName );
        if ( parts.Count < 3 || !YearPart.IsMatch( parts[0] ) )
        {
            return null;
        }

        var year = parts[0];
        var camp = parts[1];
        var role = parts[2];
        var isServing = Serving.IsMatch( role );

        // The instance names its campus in full; a per-campus template names it by short code.
        var campusId = FindCampus( Split( instanceName ).LastOrDefault(), campuses )
            ?? ( parts.Count > 3 ? FindCampus( parts[parts.Count - 1], campuses ) : null );

        var over = AndOver.Match( role );
        var under = Under.Match( role );

        return new CampName(
            CampKey: $"{year}|{camp.ToUpperInvariant()}|{( isServing ? "serving" : "camper" )}",
            Title: FriendlyCamp( camp ),
            Year: year,
            RoleText: Friendly( role ),
            IsServing: isServing,
            CampusId: campusId,
            MinAge: over.Success ? int.Parse( over.Groups[1].Value ) : ( int? ) null,
            MaxAge: under.Success ? int.Parse( under.Groups[1].Value ) - 1 : ( int? ) null );
    }

    private static List<string> Split( string? name ) =>
        ( name ?? "" ).Split( '|' ).Select( p => p.Trim() ).Where( p => p.Length > 0 ).ToList();

    private static int? FindCampus( string? text, IReadOnlyCollection<CampusName> campuses )
    {
        if ( string.IsNullOrWhiteSpace( text ) )
        {
            return null;
        }

        var match = campuses.FirstOrDefault( c => string.Equals( c.Name, text, StringComparison.OrdinalIgnoreCase ) )
            ?? campuses.FirstOrDefault( c => !string.IsNullOrWhiteSpace( c.ShortCode ) && string.Equals( c.ShortCode, text, StringComparison.OrdinalIgnoreCase ) );
        return match?.Id;
    }

    /// <summary>"HIGH SCHOOL CAMP" → "High School Camp"; "3RD-5TH" → "3rd-5th Grade Camp".</summary>
    private static string FriendlyCamp( string camp ) =>
        GradeRange.IsMatch( camp ) ? camp.ToLowerInvariant().Replace( "k-", "K-" ) + " Grade Camp" : Friendly( camp );

    /// <summary>Title case for a name typed in capitals; a name already in mixed case is kept.</summary>
    private static string Friendly( string text )
    {
        if ( text.Any( char.IsLower ) )
        {
            return text;
        }

        var titled = CultureInfo.InvariantCulture.TextInfo.ToTitleCase( text.ToLowerInvariant() );
        return Regex.Replace( titled, @"\b(And|Of|The|In)\b", m => m.Index == 0 ? m.Value : m.Value.ToLowerInvariant() );
    }
}

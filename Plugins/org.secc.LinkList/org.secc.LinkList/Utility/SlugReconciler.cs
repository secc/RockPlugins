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
using System;
using System.Collections.Generic;
using System.Linq;

using org.secc.LinkList.Services;

namespace org.secc.LinkList.Utility
{
    /// <summary>An existing ContentChannelItemSlug row (id + text) as read from the DB.</summary>
    public class ExistingSlug
    {
        public int Id { get; set; }

        public string Slug { get; set; }
    }

    /// <summary>
    /// A slug submitted by the editor: canonical text plus whether it's the chosen
    /// primary. Deliberately carries no row id - reconciliation matches on slug text
    /// (the natural key), so an id here would be decoration that later readers could
    /// mistake for the thing being matched on.
    /// </summary>
    public class SubmittedSlug
    {
        public string Slug { get; set; }

        public bool IsPrimary { get; set; }
    }

    /// <summary>
    /// The normalized slug set a save should apply, plus whether the client is
    /// slug-aware. <see cref="FullReconcile"/> true = a slug-aware client sent its
    /// complete set, so the save may DELETE slugs the client dropped. False = a
    /// legacy client that only sent the scalar slug; the save upserts that one
    /// slug and NEVER deletes, so an old cached editor bundle can't silently wipe
    /// a list's other slugs.
    /// </summary>
    public class SlugSubmission
    {
        public List<SubmittedSlug> Slugs { get; set; } = new List<SubmittedSlug>();

        public bool FullReconcile { get; set; }
    }

    /// <summary>
    /// The computed slug reconciliation: which slug texts to add, which existing
    /// rows to delete, and which slug should be primary. <see cref="IsValid"/> is
    /// false (with <see cref="Error"/> set) when the submitted set is invalid.
    /// </summary>
    public class SlugReconciliationResult
    {
        public bool IsValid { get; set; } = true;

        public string Error { get; set; }

        /// <summary>Slug texts present in the submitted set but not in the existing rows.</summary>
        public List<string> SlugsToAdd { get; set; } = new List<string>();

        /// <summary>Ids of existing rows whose slug text is absent from the submitted set.</summary>
        public List<int> SlugIdsToDelete { get; set; } = new List<int>();

        /// <summary>The slug text that should carry IsPrimary, or null when the set is empty.</summary>
        public string PrimarySlug { get; set; }
    }

    /// <summary>
    /// Pure (no RockContext) diff between an item's current slug rows and the set
    /// the editor submitted. The block does the actual DB add/delete/flag; keeping
    /// the diff here makes it unit-testable. Matching is by canonical slug text
    /// (the natural key - a slug is unique within a channel), so an existing slug
    /// that stays in the submitted set is never deleted and re-added ("rename by
    /// adding a new slug and keeping the old" preserves the old URL's row + id).
    /// </summary>
    public static class SlugReconciler
    {
        /// <summary>
        /// Builds the slug set a save should apply from the raw editor payload.
        /// <paramref name="providedSlugs"/> is null ONLY when the client omitted
        /// the slugs collection (a legacy scalar-only client) - that yields an
        /// upsert-only submission (<see cref="SlugSubmission.FullReconcile"/> false)
        /// seeded from <paramref name="scalarSlug"/> so it can never delete other
        /// slugs. A non-null collection (even empty) yields a full reconcile. Every
        /// slug is canonicalized to the text Rock will store (see
        /// <see cref="LinkListService.CanonicalizeSlug"/>), slugs left empty by that
        /// are dropped, and slugs that canonicalize to the same text are merged -
        /// keeping the first position and any primary flag.
        /// An empty result set means "leave this list's slugs alone"; for a list with
        /// no slug rows Rock's own save hook then mints one from the title.
        /// </summary>
        public static SlugSubmission BuildSubmission( IEnumerable<SubmittedSlug> providedSlugs, string scalarSlug )
        {
            if ( providedSlugs != null )
            {
                return new SlugSubmission
                {
                    FullReconcile = true,
                    Slugs = providedSlugs
                        .Where( s => s != null )
                        .Select( s => new SubmittedSlug
                        {
                            Slug = LinkListService.CanonicalizeSlug( s.Slug ),
                            IsPrimary = s.IsPrimary
                        } )
                        .Where( s => !string.IsNullOrWhiteSpace( s.Slug ) )
                        // Two entries the user typed differently ("Give_Now", "give now")
                        // can canonicalize to the same text; merge them rather than
                        // erroring on a duplicate the user never typed.
                        .GroupBy( s => s.Slug, StringComparer.OrdinalIgnoreCase )
                        .Select( g => new SubmittedSlug
                        {
                            Slug = g.Key,
                            IsPrimary = g.Any( s => s.IsPrimary )
                        } )
                        .ToList()
                };
            }

            var submission = new SlugSubmission { FullReconcile = false };
            var scalar = LinkListService.CanonicalizeSlug( scalarSlug );
            if ( !string.IsNullOrWhiteSpace( scalar ) )
            {
                submission.Slugs.Add( new SubmittedSlug { Slug = scalar, IsPrimary = true } );
            }
            return submission;
        }

        /// <summary>
        /// Validates the submitted set: every slug must be a valid stored slug
        /// (via <see cref="LinkListService.IsValidSlug"/>) and the set must contain
        /// no duplicate slug texts. Returns null when valid, else an error naming
        /// the offending slug. Callers pass CANONICALIZED slug text, which
        /// <see cref="BuildSubmission"/> also de-duplicates - so both failures here
        /// are invariant backstops rather than expected user-facing errors.
        /// </summary>
        public static string ValidateSubmitted( IEnumerable<SubmittedSlug> submitted )
        {
            var seen = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
            foreach ( var s in submitted ?? Enumerable.Empty<SubmittedSlug>() )
            {
                var slug = s?.Slug;
                if ( string.IsNullOrWhiteSpace( slug ) )
                {
                    // Blank entries are dropped by the caller before reconciliation;
                    // treat a blank that reaches here as a no-op skip.
                    continue;
                }
                if ( !LinkListService.IsValidSlug( slug ) )
                {
                    return $"Slug '{slug}' must be 1-{LinkListService.MaxSlugLength} chars of lowercase letters, digits, or dashes.";
                }
                if ( !seen.Add( slug ) )
                {
                    return $"Duplicate slug '{slug}'. Each slug in a list must be unique.";
                }
            }
            return null;
        }

        /// <summary>The most slugs one list may carry. See <see cref="ValidateSubmissionSize"/>.</summary>
        public const int MaxSlugsPerList = 50;

        /// <summary>
        /// Rejects an absurd number of slugs. Well above any real list (they exist to
        /// preserve old URLs), and the point is the conflict check: it parameterizes one
        /// value per slug, so an unbounded set would hit SQL Server's ~2100 parameter
        /// limit and surface as an opaque failure instead of a clear message.
        /// </summary>
        public static string ValidateSubmissionSize( SlugSubmission submission )
        {
            var count = submission?.Slugs?.Count ?? 0;
            return count > MaxSlugsPerList
                ? $"A link list can have at most {MaxSlugsPerList} slugs; this one has {count}."
                : null;
        }

        /// <summary>
        /// A slug-aware client may submit an EMPTY set: Rock's ContentChannelItem save
        /// hook then derives a slug from the title, exactly as Rock's own content channel
        /// item block does, and the save leaves the resulting row alone. That only works
        /// if the title yields a usable slug - when it doesn't, the list would be left
        /// unreachable by the viewer, the public web component and REST (all of which
        /// resolve a list only by slug), so it must be rejected before anything is
        /// written. A legacy scalar-only client (<see cref="SlugSubmission.FullReconcile"/>
        /// false) is not held to this: it can't manage slugs and never deletes.
        /// </summary>
        public static string ValidateSubmissionAgainstTitle( SlugSubmission submission, string title )
        {
            if ( submission == null || !submission.FullReconcile || submission.Slugs.Count > 0 )
            {
                return null;
            }
            return string.IsNullOrWhiteSpace( LinkListService.CanonicalizeSlug( title ) )
                ? "A link list needs a slug, and this title has no letters or digits to build one from. Add a slug."
                : null;
        }

        /// <summary>
        /// Diffs <paramref name="submitted"/> against <paramref name="existing"/>.
        /// Validates first (see <see cref="ValidateSubmitted"/>); on failure returns
        /// a result with <see cref="SlugReconciliationResult.IsValid"/> = false.
        /// The chosen primary is the FIRST submitted entry flagged primary, or the
        /// first submitted slug when none is flagged - so a list with any slugs
        /// always has exactly one primary.
        /// </summary>
        public static SlugReconciliationResult Reconcile( IEnumerable<ExistingSlug> existing, IEnumerable<SubmittedSlug> submitted )
        {
            var submittedList = ( submitted ?? Enumerable.Empty<SubmittedSlug>() )
                .Where( s => s != null && !string.IsNullOrWhiteSpace( s.Slug ) )
                .ToList();

            var validationError = ValidateSubmitted( submittedList );
            if ( validationError != null )
            {
                return new SlugReconciliationResult { IsValid = false, Error = validationError };
            }

            var existingList = ( existing ?? Enumerable.Empty<ExistingSlug>() )
                .Where( s => s != null && !string.IsNullOrWhiteSpace( s.Slug ) )
                .ToList();

            var submittedTexts = new HashSet<string>(
                submittedList.Select( s => s.Slug ), StringComparer.OrdinalIgnoreCase );
            var existingTexts = new HashSet<string>(
                existingList.Select( s => s.Slug ), StringComparer.OrdinalIgnoreCase );

            var result = new SlugReconciliationResult();

            // Add: submitted texts not already stored.
            result.SlugsToAdd = submittedList
                .Select( s => s.Slug )
                .Where( slug => !existingTexts.Contains( slug ) )
                .Distinct( StringComparer.OrdinalIgnoreCase )
                .ToList();

            // Delete: existing rows whose text the editor dropped.
            result.SlugIdsToDelete = existingList
                .Where( s => !submittedTexts.Contains( s.Slug ) )
                .Select( s => s.Id )
                .ToList();

            // Primary: the flagged submitted slug, else the first submitted slug.
            if ( submittedList.Count > 0 )
            {
                var primary = submittedList.FirstOrDefault( s => s.IsPrimary ) ?? submittedList[0];
                result.PrimarySlug = primary.Slug;
            }

            return result;
        }
    }
}

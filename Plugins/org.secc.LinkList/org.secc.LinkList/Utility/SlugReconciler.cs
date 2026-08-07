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

    /// <summary>A slug submitted by the editor: id (0 = new), text, and whether it's the chosen primary.</summary>
    public class SubmittedSlug
    {
        public int Id { get; set; }

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
        /// slug is normalized (trim + lowercase) and blanks are dropped.
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
                            Id = s.Id,
                            Slug = LinkListService.NormalizeSlug( s.Slug ),
                            IsPrimary = s.IsPrimary
                        } )
                        .Where( s => !string.IsNullOrWhiteSpace( s.Slug ) )
                        .ToList()
                };
            }

            var submission = new SlugSubmission { FullReconcile = false };
            var scalar = LinkListService.NormalizeSlug( scalarSlug );
            if ( !string.IsNullOrWhiteSpace( scalar ) )
            {
                submission.Slugs.Add( new SubmittedSlug { Id = 0, Slug = scalar, IsPrimary = true } );
            }
            return submission;
        }

        /// <summary>
        /// Validates the submitted set: every slug must be a valid canonical slug
        /// (via <see cref="LinkListService.IsValidSlug"/>) and the set must contain
        /// no duplicate slug texts. Returns null when valid, else an error naming
        /// the offending slug. Callers pass NORMALIZED slug text.
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
                    return $"Slug '{slug}' must be 1-200 chars of lowercase letters, digits, or dashes.";
                }
                if ( !seen.Add( slug ) )
                {
                    return $"Duplicate slug '{slug}'. Each slug in a list must be unique.";
                }
            }
            return null;
        }

        /// <summary>
        /// Diffs <paramref name="submitted"/> against <paramref name="existing"/>.
        /// Validates first (see <see cref="ValidateSubmitted"/>); on failure returns
        /// a result with <see cref="SlugReconciliationResult.IsValid"/> = false.
        /// The chosen primary is the submitted entry flagged primary; if none (or
        /// several) are flagged, the first submitted slug wins so a list with any
        /// slugs always has exactly one primary.
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

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
    /// primary.
    /// </summary>
    public class SubmittedSlug
    {
        public string Slug { get; set; }

        public bool IsPrimary { get; set; }

        /// <summary>
        /// The slug row the CLIENT believes this text is stored as, or 0 when the user
        /// typed it in this session.
        /// <para>
        /// NOT the match key. Reconciliation matches on slug text and nothing else; this
        /// exists solely so <see cref="SlugReconciler.FilterConcurrentlyDeleted"/> can
        /// tell "I still want this stored slug" apart from "this was on screen when I
        /// loaded and I never touched it". Keep that method its only reader.
        /// </para>
        /// </summary>
        public int ClientRowId { get; set; }
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
    /// The outcome of <see cref="SlugReconciler.FilterConcurrentlyDeleted"/>: the slugs
    /// the save should act on, and the texts it discarded because another editor deleted
    /// them (reported to the user so a slug vanishing from their screen is explained).
    /// </summary>
    public class SlugFilterResult
    {
        public List<SubmittedSlug> Kept { get; set; } = new List<SubmittedSlug>();

        public List<string> DroppedSlugs { get; set; } = new List<string>();
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
        /// An empty result set means "leave this list's slugs alone"; a list left with no
        /// slug rows gets one derived from its title by the save (see LinkListDetailBlock
        /// - Rock's own save hook does the same, but only when the item row changed).
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
                            IsPrimary = s.IsPrimary,
                            ClientRowId = s.ClientRowId
                        } )
                        .Where( s => !string.IsNullOrWhiteSpace( s.Slug ) )
                        // Two entries the user typed differently ("Give_Now", "give now")
                        // can canonicalize to the same text; merge them rather than
                        // erroring on a duplicate the user never typed.
                        .GroupBy( s => s.Slug, StringComparer.OrdinalIgnoreCase )
                        .Select( g => new SubmittedSlug
                        {
                            Slug = g.Key,
                            IsPrimary = g.Any( s => s.IsPrimary ),
                            // A typed entry (id 0) anywhere in the group makes the whole
                            // group explicit: a user re-typing a slug that also exists as
                            // a stored row must not inherit that row's id, or a concurrent
                            // deletion of it would silently discard what they typed.
                            ClientRowId = g.Any( s => s.ClientRowId == 0 )
                                ? 0
                                : g.Select( s => s.ClientRowId ).First()
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

        /// <summary>
        /// Narrows <paramref name="plannedIds"/> (from
        /// <see cref="SlugReconciliationResult.SlugIdsToDelete"/>) to the rows the
        /// editor actually had on screen when it loaded the list.
        /// <para>
        /// Reconciliation diffs by slug TEXT, so a row another editor added after this
        /// one loaded looks exactly like a row this user deliberately removed - both are
        /// simply "stored but not submitted". Comparing against the ids the client
        /// loaded tells them apart: an id the client never saw cannot be something it
        /// removed, so it is spared and the two edits merge instead of one clobbering
        /// the other. Deliberate removal still works, because the loaded ids come from
        /// the ORIGINAL load rather than the submitted set.
        /// </para>
        /// A null <paramref name="loadedIds"/> means the caller doesn't track them (a
        /// pre-ROCK-8987 editor bundle, or a hand-rolled API caller); those keep the
        /// plain full-reconcile behaviour.
        /// </summary>
        /// <summary>
        /// Drops submitted slugs that another editor deleted after this client loaded, so
        /// saving a stale screen doesn't resurrect them.
        /// <para>
        /// The mirror image of <see cref="FilterDeletableIds"/>. Reconciliation matches on
        /// text, so a submitted slug carries no evidence of whether the user wants it or
        /// merely still has it on screen - <see cref="SubmittedSlug.ClientRowId"/> is that
        /// evidence: an entry whose row the client named is gone from the list was deleted
        /// by someone else, and re-adding it would undo an explicit deletion.
        /// </para>
        /// Kept regardless: entries with no row id (the user typed them this session, which
        /// includes deliberately re-typing a slug someone else deleted), and entries whose
        /// TEXT still exists under a different row id - a delete-and-recreate. That text
        /// guard matters: dropping such an entry would make its live row look unsubmitted
        /// to <see cref="Reconcile"/> and queue it for deletion.
        /// A null <paramref name="currentRows"/> keeps everything, as in
        /// <see cref="FilterDeletableIds"/>.
        /// </summary>
        public static SlugFilterResult FilterConcurrentlyDeleted(
            IEnumerable<SubmittedSlug> submitted, IEnumerable<ExistingSlug> currentRows )
        {
            var submittedList = ( submitted ?? Enumerable.Empty<SubmittedSlug>() ).ToList();
            if ( currentRows == null )
            {
                return new SlugFilterResult { Kept = submittedList };
            }

            var rows = currentRows.Where( r => r != null ).ToList();
            var currentIds = new HashSet<int>( rows.Select( r => r.Id ) );
            var currentTexts = new HashSet<string>(
                rows.Select( r => r.Slug ).Where( s => !string.IsNullOrWhiteSpace( s ) ),
                StringComparer.OrdinalIgnoreCase );

            var result = new SlugFilterResult();
            foreach ( var s in submittedList )
            {
                var keep = s.ClientRowId == 0
                    || currentIds.Contains( s.ClientRowId )
                    || currentTexts.Contains( s.Slug );
                if ( keep )
                {
                    result.Kept.Add( s );
                }
                else
                {
                    result.DroppedSlugs.Add( s.Slug );
                }
            }
            return result;
        }

        public static List<int> FilterDeletableIds( IEnumerable<int> plannedIds, IEnumerable<int> loadedIds )
        {
            var planned = ( plannedIds ?? Enumerable.Empty<int>() ).ToList();
            if ( loadedIds == null )
            {
                return planned;
            }
            var loaded = new HashSet<int>( loadedIds );
            return planned.Where( id => loaded.Contains( id ) ).ToList();
        }
    }
}

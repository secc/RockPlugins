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
using System.Collections.Generic;
using System.Linq;

using org.secc.LinkList.Utility;

using Xunit;

namespace org.secc.LinkList.Tests
{
    public class SlugReconciliationTests
    {
        private static ExistingSlug Existing( int id, string slug )
        {
            return new ExistingSlug { Id = id, Slug = slug };
        }

        private static SubmittedSlug Submitted( string slug, bool isPrimary = false, int clientRowId = 0 )
        {
            return new SubmittedSlug { Slug = slug, IsPrimary = isPrimary, ClientRowId = clientRowId };
        }

        [Fact]
        public void Reconcile_Adds_A_New_Slug_And_Keeps_Existing()
        {
            var existing = new[] { Existing( 1, "keep" ) };
            var submitted = new[] { Submitted( "keep", isPrimary: true ), Submitted( "added" ) };

            var result = SlugReconciler.Reconcile( existing, submitted );

            Assert.True( result.IsValid );
            Assert.Equal( new[] { "added" }, result.SlugsToAdd.ToArray() );
            Assert.Empty( result.SlugIdsToDelete );
            Assert.Equal( "keep", result.PrimarySlug );
        }

        [Fact]
        public void Reconcile_Deletes_A_Removed_Slug()
        {
            var existing = new[] { Existing( 1, "keep" ), Existing( 2, "gone" ) };
            var submitted = new[] { Submitted( "keep", isPrimary: true ) };

            var result = SlugReconciler.Reconcile( existing, submitted );

            Assert.True( result.IsValid );
            Assert.Empty( result.SlugsToAdd );
            Assert.Equal( new[] { 2 }, result.SlugIdsToDelete.ToArray() );
            Assert.Equal( "keep", result.PrimarySlug );
        }

        [Fact]
        public void Reconcile_Changes_The_Primary_Without_Add_Or_Delete()
        {
            // "a" is primary today; the editor moves primary to "b".
            var existing = new[] { Existing( 1, "a" ), Existing( 2, "b" ) };
            var submitted = new[] { Submitted( "a" ), Submitted( "b", isPrimary: true ) };

            var result = SlugReconciler.Reconcile( existing, submitted );

            Assert.True( result.IsValid );
            Assert.Empty( result.SlugsToAdd );
            Assert.Empty( result.SlugIdsToDelete );
            Assert.Equal( "b", result.PrimarySlug );
        }

        [Fact]
        public void Reconcile_NoOp_Set_Produces_No_Changes()
        {
            var existing = new[] { Existing( 1, "a" ), Existing( 2, "b" ) };
            var submitted = new[] { Submitted( "a", isPrimary: true ), Submitted( "b" ) };

            var result = SlugReconciler.Reconcile( existing, submitted );

            Assert.True( result.IsValid );
            Assert.Empty( result.SlugsToAdd );
            Assert.Empty( result.SlugIdsToDelete );
            Assert.Equal( "a", result.PrimarySlug );
        }

        [Fact]
        public void Reconcile_Renaming_By_Adding_And_Keeping_Old_Preserves_Old_Row()
        {
            // Rename = add the new slug, keep the old one. The old row is neither
            // deleted nor re-added, so the old URL keeps resolving.
            var existing = new[] { Existing( 1, "old-name" ) };
            var submitted = new[]
            {
                Submitted( "old-name" ),
                Submitted( "new-name", isPrimary: true )
            };

            var result = SlugReconciler.Reconcile( existing, submitted );

            Assert.True( result.IsValid );
            Assert.Equal( new[] { "new-name" }, result.SlugsToAdd.ToArray() );
            Assert.Empty( result.SlugIdsToDelete );
            Assert.Equal( "new-name", result.PrimarySlug );
        }

        [Fact]
        public void Reconcile_Rejects_Duplicate_Within_Submitted_Set()
        {
            var existing = new ExistingSlug[0];
            var submitted = new[] { Submitted( "dupe", isPrimary: true ), Submitted( "dupe" ) };

            var result = SlugReconciler.Reconcile( existing, submitted );

            Assert.False( result.IsValid );
            Assert.Contains( "dupe", result.Error );
        }

        [Fact]
        public void Reconcile_Rejects_Invalid_Slug_In_Set()
        {
            var existing = new ExistingSlug[0];
            var submitted = new[] { Submitted( "not valid", isPrimary: true ) };

            var result = SlugReconciler.Reconcile( existing, submitted );

            Assert.False( result.IsValid );
            Assert.NotNull( result.Error );
        }

        [Fact]
        public void Reconcile_Empty_Submitted_Deletes_All_Existing_And_Has_No_Primary()
        {
            // Pure-helper capability, deliberately NOT acted on: the block CAN reach
            // Reconcile with an empty set (a slug-aware client that submitted none,
            // leaving the slug to Rock's title-derived one), and it suppresses the
            // delete pass in exactly that case - see the mayDelete guard in
            // LinkListDetailBlock. A list must never be left with no slug.
            var existing = new[] { Existing( 1, "a" ), Existing( 2, "b" ) };
            var submitted = new SubmittedSlug[0];

            var result = SlugReconciler.Reconcile( existing, submitted );

            Assert.True( result.IsValid );
            Assert.Empty( result.SlugsToAdd );
            Assert.Equal( new[] { 1, 2 }, result.SlugIdsToDelete.OrderBy( i => i ).ToArray() );
            Assert.Null( result.PrimarySlug );
        }

        [Fact]
        public void Reconcile_Falls_Back_To_First_Submitted_When_None_Flagged_Primary()
        {
            // Legacy lists have no IsPrimary flag set anywhere; the first submitted
            // slug becomes primary so a list with slugs always has exactly one.
            var existing = new[] { Existing( 1, "a" ), Existing( 2, "b" ) };
            var submitted = new[] { Submitted( "a" ), Submitted( "b" ) };

            var result = SlugReconciler.Reconcile( existing, submitted );

            Assert.True( result.IsValid );
            Assert.Equal( "a", result.PrimarySlug );
        }

        [Fact]
        public void Reconcile_Matches_Existing_By_Text_Case_Insensitively()
        {
            // The existing row's text differs from the submitted text only by case
            // (submitted is a valid lowercase slug). OrdinalIgnoreCase matching must
            // treat them as the same row: no add, no delete. (An Ordinal comparison
            // would wrongly add + delete here.)
            var existing = new[] { Existing( 5, "Already-Here" ) };
            var submitted = new[] { Submitted( "already-here", isPrimary: true ) };

            var result = SlugReconciler.Reconcile( existing, submitted );

            Assert.True( result.IsValid );
            Assert.Empty( result.SlugsToAdd );
            Assert.Empty( result.SlugIdsToDelete );
        }

        // ---- BuildSubmission: null (legacy scalar-only) vs provided (slug-aware) ----

        [Fact]
        public void BuildSubmission_Null_Slugs_With_Scalar_Is_UpsertOnly_Single_Primary()
        {
            var submission = SlugReconciler.BuildSubmission( null, "My-Slug" );

            Assert.False( submission.FullReconcile ); // legacy client: never deletes
            Assert.Single( submission.Slugs );
            Assert.Equal( "my-slug", submission.Slugs[0].Slug ); // normalized
            Assert.True( submission.Slugs[0].IsPrimary );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        [InlineData( "   " )]
        public void BuildSubmission_Null_Slugs_With_Blank_Scalar_Is_UpsertOnly_Empty( string scalar )
        {
            var submission = SlugReconciler.BuildSubmission( null, scalar );

            Assert.False( submission.FullReconcile );
            Assert.Empty( submission.Slugs ); // nothing to upsert -> block leaves slugs untouched
        }

        [Fact]
        public void BuildSubmission_Provided_Empty_List_Is_FullReconcile_Empty()
        {
            // A slug-aware client that submitted no slugs: full reconcile with an
            // empty set, which the block reads as "leave the slug to Rock" - its save
            // hook derives one from the title. FullReconcile must still be true so
            // the block can tell this apart from a legacy scalar-only client.
            var submission = SlugReconciler.BuildSubmission( new SubmittedSlug[0], "ignored-scalar" );

            Assert.True( submission.FullReconcile );
            Assert.Empty( submission.Slugs );
        }

        [Fact]
        public void BuildSubmission_Provided_Slugs_Normalizes_And_Drops_Blanks()
        {
            var provided = new[]
            {
                new SubmittedSlug { Slug = "  Keep-Me  ", IsPrimary = true },
                new SubmittedSlug { Slug = "ADDED" },
                new SubmittedSlug { Slug = "   " } // blank -> dropped
            };

            var submission = SlugReconciler.BuildSubmission( provided, null );

            Assert.True( submission.FullReconcile );
            Assert.Equal( new[] { "keep-me", "added" }, submission.Slugs.Select( s => s.Slug ).ToArray() );
            Assert.True( submission.Slugs[0].IsPrimary );
        }

        [Fact]
        public void BuildSubmission_Canonicalizes_To_The_Text_Rock_Stores()
        {
            var provided = new[]
            {
                new SubmittedSlug { Slug = "Give Now", IsPrimary = true },
                new SubmittedSlug { Slug = "fall--retreat" },
                new SubmittedSlug { Slug = "trailing-" }
            };

            var submission = SlugReconciler.BuildSubmission( provided, null );

            Assert.Equal(
                new[] { "give-now", "fall-retreat", "trailing" },
                submission.Slugs.Select( s => s.Slug ).ToArray() );
        }

        [Theory]
        [InlineData( "-" )]
        [InlineData( "---" )]
        [InlineData( "!!!" )]
        public void BuildSubmission_Drops_Slugs_Canonicalization_Empties( string emptied )
        {
            var provided = new[]
            {
                new SubmittedSlug { Slug = emptied },
                new SubmittedSlug { Slug = "keep", IsPrimary = true }
            };

            var submission = SlugReconciler.BuildSubmission( provided, null );

            Assert.Equal( new[] { "keep" }, submission.Slugs.Select( s => s.Slug ).ToArray() );
        }

        [Fact]
        public void BuildSubmission_All_Slugs_Emptied_Leaves_The_Slug_To_Rock()
        {
            // Every entry canonicalized to nothing: the same empty full-reconcile
            // submission a client that sent no slugs produces, so the block leaves
            // the list's slugs alone and Rock derives one from the title.
            var provided = new[]
            {
                new SubmittedSlug { Slug = "-" },
                new SubmittedSlug { Slug = "!!!" }
            };

            var submission = SlugReconciler.BuildSubmission( provided, null );

            Assert.True( submission.FullReconcile );
            Assert.Empty( submission.Slugs );
        }

        [Fact]
        public void BuildSubmission_Merges_Slugs_That_Canonicalize_The_Same()
        {
            // The user typed these differently, so erroring on a "duplicate" would
            // name text they never entered. Merge instead: first position wins and a
            // primary flag anywhere in the group wins.
            var provided = new[]
            {
                new SubmittedSlug { Slug = "Give_Now" },
                new SubmittedSlug { Slug = "give now", IsPrimary = true },
                new SubmittedSlug { Slug = "other" }
            };

            var submission = SlugReconciler.BuildSubmission( provided, null );

            Assert.Equal( new[] { "give-now", "other" }, submission.Slugs.Select( s => s.Slug ).ToArray() );
            Assert.True( submission.Slugs[0].IsPrimary );
            Assert.False( submission.Slugs[1].IsPrimary );
        }

        [Fact]
        public void BuildSubmission_Merge_Keeps_Primary_Flagged_On_The_First_Entry()
        {
            var provided = new[]
            {
                new SubmittedSlug { Slug = "Give Now", IsPrimary = true },
                new SubmittedSlug { Slug = "give--now" }
            };

            var submission = SlugReconciler.BuildSubmission( provided, null );

            Assert.Single( submission.Slugs );
            Assert.Equal( "give-now", submission.Slugs[0].Slug );
            Assert.True( submission.Slugs[0].IsPrimary );
        }

        [Fact]
        public void BuildSubmission_Scalar_Slug_Is_Canonicalized_Too()
        {
            var submission = SlugReconciler.BuildSubmission( null, "Summer Camp 2026" );

            Assert.False( submission.FullReconcile );
            Assert.Single( submission.Slugs );
            Assert.Equal( "summer-camp-2026", submission.Slugs[0].Slug );
            Assert.True( submission.Slugs[0].IsPrimary );
        }

        // ---- Empty set + title: the "let Rock derive the slug" precondition ----

        [Theory]
        [InlineData( "Summer Camp" )]
        [InlineData( "2026" )]
        [InlineData( "Rock & Roll" )]
        public void ValidateSubmissionAgainstTitle_Allows_An_Empty_Set_When_The_Title_Yields_A_Slug( string title )
        {
            var submission = SlugReconciler.BuildSubmission( new SubmittedSlug[0], null );

            Assert.Null( SlugReconciler.ValidateSubmissionAgainstTitle( submission, title ) );
        }

        [Theory]
        [InlineData( "!!!" )]
        [InlineData( "---" )]
        [InlineData( "&" )]
        [InlineData( null )]
        public void ValidateSubmissionAgainstTitle_Rejects_An_Empty_Set_When_The_Title_Yields_Nothing( string title )
        {
            // Nothing submitted and nothing derivable would leave the list with no
            // slug at all - unreachable by the viewer, web component and REST.
            var submission = SlugReconciler.BuildSubmission( new SubmittedSlug[0], null );

            var error = SlugReconciler.ValidateSubmissionAgainstTitle( submission, title );

            Assert.NotNull( error );
            Assert.Contains( "needs a slug", error );
        }

        [Fact]
        public void ValidateSubmissionAgainstTitle_Ignores_The_Title_When_Slugs_Were_Submitted()
        {
            var submission = SlugReconciler.BuildSubmission( new[] { Submitted( "explicit" ) }, null );

            Assert.Null( SlugReconciler.ValidateSubmissionAgainstTitle( submission, "!!!" ) );
        }

        [Fact]
        public void ValidateSubmissionAgainstTitle_Ignores_A_Legacy_ScalarOnly_Submission()
        {
            // FullReconcile false: the client can't manage slugs and never deletes, so
            // an empty set means "leave them alone", not "derive one".
            var submission = SlugReconciler.BuildSubmission( null, null );

            Assert.False( submission.FullReconcile );
            Assert.Null( SlugReconciler.ValidateSubmissionAgainstTitle( submission, "!!!" ) );
        }

        // ---- Delete gate: only rows the editor actually saw may be deleted ----

        [Fact]
        public void FilterDeletableIds_Keeps_Rows_The_Editor_Loaded()
        {
            // The user removed slug row 2, which was on screen when they loaded.
            Assert.Equal( new[] { 2 }, SlugReconciler.FilterDeletableIds( new[] { 2 }, new[] { 1, 2 } ).ToArray() );
        }

        [Fact]
        public void FilterDeletableIds_Spares_A_Row_The_Editor_Never_Saw()
        {
            // Row 9 was added by another editor after this one loaded [1, 2]. The diff
            // wants it gone only because it isn't in this payload - it is not something
            // this user removed, so it must survive.
            var deletable = SlugReconciler.FilterDeletableIds( new[] { 2, 9 }, new[] { 1, 2 } );

            Assert.Equal( new[] { 2 }, deletable.ToArray() );
        }

        [Fact]
        public void FilterDeletableIds_Untracked_Caller_Keeps_Full_Reconcile()
        {
            // null = a client that doesn't report what it loaded (old bundle or API
            // caller); it keeps today's behavior rather than silently failing to delete.
            Assert.Equal( new[] { 2, 9 }, SlugReconciler.FilterDeletableIds( new[] { 2, 9 }, null ).ToArray() );
        }

        [Fact]
        public void FilterDeletableIds_Empty_Loaded_Set_Deletes_Nothing()
        {
            // An editor that loaded no slug rows can't have removed any.
            Assert.Empty( SlugReconciler.FilterDeletableIds( new[] { 2, 9 }, new int[0] ) );
        }

        [Fact]
        public void FilterDeletableIds_Handles_A_Null_Plan()
        {
            Assert.Empty( SlugReconciler.FilterDeletableIds( null, new[] { 1 } ) );
        }

        [Fact]
        public void FilterDeletableIds_Composes_With_Reconcile_To_Merge_Concurrent_Adds()
        {
            // End to end on the reported bug. Tab B loaded ["keep"] (row 1), then tab A
            // added "a" (row 9). Tab B now saves ["keep", "b"].
            var existing = new[] { Existing( 1, "keep" ), Existing( 9, "a" ) };
            var submitted = new[] { Submitted( "keep", isPrimary: true ), Submitted( "b" ) };

            var plan = SlugReconciler.Reconcile( existing, submitted );
            var deletable = SlugReconciler.FilterDeletableIds( plan.SlugIdsToDelete, new[] { 1 } );

            // The diff alone would have deleted tab A's slug...
            Assert.Equal( new[] { 9 }, plan.SlugIdsToDelete.ToArray() );
            // ...but tab B never saw row 9, so nothing is deleted and "b" is just added.
            Assert.Empty( deletable );
            Assert.Equal( new[] { "b" }, plan.SlugsToAdd.ToArray() );
        }

        // ---- Resurrect guard: a slug another editor deleted must not come back ----

        [Fact]
        public void FilterConcurrentlyDeleted_Drops_A_Slug_Whose_Row_Is_Gone()
        {
            // The client still shows "x" as row 2, but row 2 no longer exists: another
            // editor deleted it. Re-adding it would undo their deletion.
            var submitted = new[] { Submitted( "orig", isPrimary: true, clientRowId: 1 ), Submitted( "x", clientRowId: 2 ) };
            var current = new[] { Existing( 1, "orig" ) };

            var result = SlugReconciler.FilterConcurrentlyDeleted( submitted, current );

            Assert.Equal( new[] { "orig" }, result.Kept.Select( s => s.Slug ).ToArray() );
            Assert.Equal( new[] { "x" }, result.DroppedSlugs.ToArray() );
        }

        [Fact]
        public void FilterConcurrentlyDeleted_Keeps_Slugs_Whose_Rows_Still_Exist()
        {
            var submitted = new[] { Submitted( "orig", clientRowId: 1 ), Submitted( "x", clientRowId: 2 ) };
            var current = new[] { Existing( 1, "orig" ), Existing( 2, "x" ) };

            var result = SlugReconciler.FilterConcurrentlyDeleted( submitted, current );

            Assert.Equal( 2, result.Kept.Count );
            Assert.Empty( result.DroppedSlugs );
        }

        [Fact]
        public void FilterConcurrentlyDeleted_Always_Keeps_A_Slug_The_User_Typed()
        {
            // Id 0 is explicit intent - including deliberately re-typing a slug someone
            // else just deleted. The user's action wins over the merge rule.
            var submitted = new[] { Submitted( "x" ), Submitted( "brand-new" ) };

            var result = SlugReconciler.FilterConcurrentlyDeleted( submitted, new ExistingSlug[0] );

            Assert.Equal( new[] { "x", "brand-new" }, result.Kept.Select( s => s.Slug ).ToArray() );
            Assert.Empty( result.DroppedSlugs );
        }

        [Fact]
        public void FilterConcurrentlyDeleted_Keeps_A_Slug_Deleted_And_Recreated_Under_A_New_Id()
        {
            // The client knew "x" as row 2; someone deleted row 2 and recreated the same
            // text as row 15. Dropping it would make row 15 look unsubmitted to Reconcile
            // and queue a LIVE slug for deletion - so the text has to win over the id.
            var submitted = new[] { Submitted( "orig", clientRowId: 1 ), Submitted( "x", clientRowId: 2 ) };
            var current = new[] { Existing( 1, "orig" ), Existing( 15, "x" ) };

            var result = SlugReconciler.FilterConcurrentlyDeleted( submitted, current );

            Assert.Equal( 2, result.Kept.Count );
            Assert.Empty( result.DroppedSlugs );
        }

        [Fact]
        public void FilterConcurrentlyDeleted_Null_Current_Rows_Keeps_Everything()
        {
            // Mirrors FilterDeletableIds: null means "caller can't tell us", which must
            // differ from an empty set (a list that genuinely has no slug rows).
            var submitted = new[] { Submitted( "x", clientRowId: 2 ) };

            var result = SlugReconciler.FilterConcurrentlyDeleted( submitted, null );

            Assert.Single( result.Kept );
            Assert.Empty( result.DroppedSlugs );
        }

        [Fact]
        public void FilterConcurrentlyDeleted_No_Ids_Anywhere_Is_A_NoOp()
        {
            // A pre-fix editor bundle or hand-rolled API caller sends no ids at all;
            // it keeps the old behavior rather than having its payload gutted.
            var submitted = new[] { Submitted( "a" ), Submitted( "b" ) };

            var result = SlugReconciler.FilterConcurrentlyDeleted( submitted, new[] { Existing( 7, "unrelated" ) } );

            Assert.Equal( 2, result.Kept.Count );
            Assert.Empty( result.DroppedSlugs );
        }

        [Fact]
        public void FilterConcurrentlyDeleted_Reports_When_Everything_Known_Is_Gone()
        {
            // The block turns this into "reload the list": there is nothing left to
            // reconcile against, so proceeding would wipe the list or invent a slug.
            var submitted = new[] { Submitted( "x", isPrimary: true, clientRowId: 2 ) };
            var current = new[] { Existing( 3, "z" ) };

            var result = SlugReconciler.FilterConcurrentlyDeleted( submitted, current );

            Assert.Empty( result.Kept );
            Assert.Equal( new[] { "x" }, result.DroppedSlugs.ToArray() );
        }

        [Fact]
        public void FilterConcurrentlyDeleted_Composes_With_Reconcile_On_The_Reported_Bug()
        {
            // End to end. List was [orig(1), x(2)]; tab A removed x; tab B, still showing
            // x, adds y. Without the filter Reconcile would re-add x.
            var submitted = new[]
            {
                Submitted( "orig", isPrimary: true, clientRowId: 1 ),
                Submitted( "x", clientRowId: 2 ),
                Submitted( "y" )
            };
            var current = new[] { Existing( 1, "orig" ) };

            var naive = SlugReconciler.Reconcile( current, submitted );
            Assert.Equal( new[] { "x", "y" }, naive.SlugsToAdd.ToArray() ); // the bug

            var filtered = SlugReconciler.FilterConcurrentlyDeleted( submitted, current );
            var plan = SlugReconciler.Reconcile( current, filtered.Kept );

            Assert.Equal( new[] { "y" }, plan.SlugsToAdd.ToArray() );
            Assert.Empty( plan.SlugIdsToDelete );
            Assert.Equal( "orig", plan.PrimarySlug );
        }

        [Fact]
        public void FilterConcurrentlyDeleted_Recreated_Row_Is_Never_A_Delete_Candidate()
        {
            // Composition of the two guards: the text guard keeps "x", so the recreated
            // row 15 is never queued for deletion - which matters because a caller that
            // omits loadedSlugIds has nothing to spare it (FilterDeletableIds(_, null)
            // passes every planned id through).
            var submitted = new[] { Submitted( "orig", isPrimary: true, clientRowId: 1 ), Submitted( "x", clientRowId: 2 ) };
            var current = new[] { Existing( 1, "orig" ), Existing( 15, "x" ) };

            var filtered = SlugReconciler.FilterConcurrentlyDeleted( submitted, current );
            var plan = SlugReconciler.Reconcile( current, filtered.Kept );

            Assert.Empty( plan.SlugIdsToDelete );
            Assert.Empty( SlugReconciler.FilterDeletableIds( plan.SlugIdsToDelete, null ) );
        }

        [Fact]
        public void FilterConcurrentlyDeleted_Dropping_The_Flagged_Primary_Promotes_The_First_Survivor()
        {
            // Accepted behavior: primary is last-writer-wins, so a stale save whose chosen
            // primary is gone falls back to its first surviving slug rather than failing.
            var submitted = new[] { Submitted( "keep", clientRowId: 1 ), Submitted( "x", isPrimary: true, clientRowId: 2 ) };
            var current = new[] { Existing( 1, "keep" ) };

            var filtered = SlugReconciler.FilterConcurrentlyDeleted( submitted, current );
            var plan = SlugReconciler.Reconcile( current, filtered.Kept );

            Assert.Equal( "keep", plan.PrimarySlug );
        }

        [Fact]
        public void BuildSubmission_Carries_The_Client_Row_Id_Through_Canonicalization()
        {
            var provided = new[] { Submitted( "Give Now", clientRowId: 4 ) };

            var submission = SlugReconciler.BuildSubmission( provided, null );

            Assert.Equal( "give-now", submission.Slugs[0].Slug );
            Assert.Equal( 4, submission.Slugs[0].ClientRowId );
        }

        [Fact]
        public void BuildSubmission_Merged_Group_Prefers_A_Typed_Entry()
        {
            // A stored row plus the user re-typing the same slug: the merged entry must be
            // treated as typed (id 0), or a concurrent deletion of that row would discard
            // what the user deliberately entered.
            var provided = new[]
            {
                Submitted( "Already-Here", clientRowId: 5 ),
                Submitted( "already-here" )
            };

            var submission = SlugReconciler.BuildSubmission( provided, null );

            Assert.Single( submission.Slugs );
            Assert.Equal( 0, submission.Slugs[0].ClientRowId );
        }

        [Fact]
        public void BuildSubmission_Scalar_Slug_Has_No_Client_Row_Id()
        {
            var submission = SlugReconciler.BuildSubmission( null, "Give Now" );

            Assert.Equal( 0, submission.Slugs[0].ClientRowId );
        }

        // ---- Set size cap (keeps the conflict query under SQL's parameter limit) ----

        [Fact]
        public void ValidateSubmissionSize_Allows_A_Set_At_The_Cap()
        {
            var provided = Enumerable.Range( 0, SlugReconciler.MaxSlugsPerList )
                .Select( i => Submitted( $"slug-{i}" ) )
                .ToArray();
            var submission = SlugReconciler.BuildSubmission( provided, null );

            Assert.Equal( SlugReconciler.MaxSlugsPerList, submission.Slugs.Count );
            Assert.Null( SlugReconciler.ValidateSubmissionSize( submission ) );
        }

        [Fact]
        public void ValidateSubmissionSize_Rejects_A_Set_Over_The_Cap()
        {
            var provided = Enumerable.Range( 0, SlugReconciler.MaxSlugsPerList + 1 )
                .Select( i => Submitted( $"slug-{i}" ) )
                .ToArray();
            var submission = SlugReconciler.BuildSubmission( provided, null );

            var error = SlugReconciler.ValidateSubmissionSize( submission );

            Assert.NotNull( error );
            Assert.Contains( SlugReconciler.MaxSlugsPerList.ToString(), error );
        }
    }
}

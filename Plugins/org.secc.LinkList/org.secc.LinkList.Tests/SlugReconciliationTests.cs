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

        private static SubmittedSlug Submitted( int id, string slug, bool isPrimary = false )
        {
            return new SubmittedSlug { Id = id, Slug = slug, IsPrimary = isPrimary };
        }

        [Fact]
        public void Reconcile_Adds_A_New_Slug_And_Keeps_Existing()
        {
            var existing = new[] { Existing( 1, "keep" ) };
            var submitted = new[] { Submitted( 1, "keep", isPrimary: true ), Submitted( 0, "added" ) };

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
            var submitted = new[] { Submitted( 1, "keep", isPrimary: true ) };

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
            var submitted = new[] { Submitted( 1, "a" ), Submitted( 2, "b", isPrimary: true ) };

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
            var submitted = new[] { Submitted( 1, "a", isPrimary: true ), Submitted( 2, "b" ) };

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
                Submitted( 1, "old-name" ),
                Submitted( 0, "new-name", isPrimary: true )
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
            var submitted = new[] { Submitted( 0, "dupe", isPrimary: true ), Submitted( 0, "dupe" ) };

            var result = SlugReconciler.Reconcile( existing, submitted );

            Assert.False( result.IsValid );
            Assert.Contains( "dupe", result.Error );
        }

        [Fact]
        public void Reconcile_Rejects_Invalid_Slug_In_Set()
        {
            var existing = new ExistingSlug[0];
            var submitted = new[] { Submitted( 0, "not valid", isPrimary: true ) };

            var result = SlugReconciler.Reconcile( existing, submitted );

            Assert.False( result.IsValid );
            Assert.NotNull( result.Error );
        }

        [Fact]
        public void Reconcile_Empty_Submitted_Deletes_All_Existing_And_Has_No_Primary()
        {
            // Pure-helper capability. The block never actually calls Reconcile with
            // an empty set: ValidateBag rejects an explicitly-empty slug-aware set,
            // and the legacy (upsert-only) path skips reconciliation when empty.
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
            var submitted = new[] { Submitted( 1, "a" ), Submitted( 2, "b" ) };

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
            var submitted = new[] { Submitted( 5, "already-here", isPrimary: true ) };

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
            // A slug-aware client that cleared every slug: full reconcile with an
            // empty set. ValidateBag rejects this ("at least one slug"), but the
            // submission itself must report FullReconcile so that rejection fires.
            var submission = SlugReconciler.BuildSubmission( new SubmittedSlug[0], "ignored-scalar" );

            Assert.True( submission.FullReconcile );
            Assert.Empty( submission.Slugs );
        }

        [Fact]
        public void BuildSubmission_Provided_Slugs_Normalizes_And_Drops_Blanks()
        {
            var provided = new[]
            {
                new SubmittedSlug { Id = 1, Slug = "  Keep-Me  ", IsPrimary = true },
                new SubmittedSlug { Id = 0, Slug = "ADDED" },
                new SubmittedSlug { Id = 0, Slug = "   " } // blank -> dropped
            };

            var submission = SlugReconciler.BuildSubmission( provided, null );

            Assert.True( submission.FullReconcile );
            Assert.Equal( new[] { "keep-me", "added" }, submission.Slugs.Select( s => s.Slug ).ToArray() );
            Assert.True( submission.Slugs[0].IsPrimary );
        }
    }
}

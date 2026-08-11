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

namespace org.secc.LinkList.ViewModels
{
    public class LinkListBag
    {
        public string Guid { get; set; }

        public int? Id { get; set; }

        /// <summary>
        /// The PRIMARY slug (computed, read-only for consumers). Kept for
        /// back-compat: the web component's <c>secc-link-list-loaded</c> event
        /// payload and the fallback title still read this scalar. The editor
        /// edits <see cref="Slugs"/>; the save path recomputes this from the
        /// primary row.
        /// </summary>
        public string Slug { get; set; }

        /// <summary>
        /// All slugs for the list (primary + additional). The editor adds,
        /// removes, and designates the primary here; the save path reconciles
        /// this set against the item's ContentChannelItemSlug rows. Every entry
        /// resolves to the list, so keeping an old slug preserves old URLs.
        /// <para>
        /// NOTE: intentionally NOT initialized. On the save (deserialization)
        /// path, null means "the client omitted this collection" - a legacy
        /// scalar-only client (e.g. a browser holding the pre-multi-slug editor
        /// bundle) - which the block treats as an upsert-only update that never
        /// deletes other slugs. A non-null value (even empty) means a slug-aware
        /// client sent its full set, so the block does a delete-capable reconcile.
        /// The read path (BuildBag / QueryListSummaries) always assigns it.
        /// </para>
        /// </summary>
        public List<LinkListSlugBag> Slugs { get; set; }

        public string Title { get; set; }

        public bool IsPublic { get; set; } = true;

        /// <summary>
        /// Friendly name of the selected design preset (for list/grid display).
        /// Null when no preset is selected. Not persisted; derived from DesignId.
        /// </summary>
        public string DesignName { get; set; }

        /// <summary>
        /// Last modified timestamp of the underlying ContentChannelItem. Drives the
        /// management grid's "Modified" column AND the save's optimistic-concurrency
        /// check: a save whose value no longer matches the stored one is rejected, so
        /// a stale editor can't revert - or delete the slugs of - someone else's edit.
        /// </summary>
        public System.DateTime? ModifiedDateTime { get; set; }

        /// <summary>
        /// Set when a save PARTIALLY succeeded: the list itself is stored but a
        /// follow-on step (link rows, edit-access group) failed. The response still
        /// carries the saved bag so the editor keeps the list's identity and can retry
        /// as an update - without this, a retry would look like a brand new list and be
        /// rejected for conflicting with the slugs it just created. Null on a clean save.
        /// </summary>
        public string SaveWarning { get; set; }

        /// <summary>
        /// Set when a save fully SUCCEEDED but merged a concurrent edit - a slug another
        /// editor added that this save kept, or one they deleted that it did not restore.
        /// Purely informational: unlike <see cref="SaveWarning"/> nothing needs redoing,
        /// so the editor reports it without treating the save as incomplete.
        /// </summary>
        public string SaveNotice { get; set; }

        /// <summary>
        /// True when the current person may delete this list (ADMINISTRATE on
        /// the item - the same check the Delete block action enforces).
        /// Populated only for the management grid; false elsewhere.
        /// </summary>
        public bool CanDelete { get; set; }

        /// <summary>
        /// GUID of the selected design preset (Defined Value under the
        /// "Link List Design" Defined Type), or null when no preset is
        /// selected (legacy items use their own per-item color attrs).
        /// </summary>
        public Guid? DesignId { get; set; }

        // Per-list color OVERRIDES (raw item attribute values; empty = inherit
        // from the selected design preset). The editor binds + saves these.
        public string ContentTextColor { get; set; }

        public string BackgroundColor { get; set; }

        public string ButtonColor { get; set; }

        public string ButtonTextColor { get; set; }

        // WS10: featured-button color overrides (per list; empty = inherit preset).
        public string FeaturedButtonColor { get; set; }

        public string FeaturedButtonTextColor { get; set; }

        // WS7 fix 7: dedicated title color override (empty = inherit preset, then content text).
        public string TitleColor { get; set; }

        // Resolved colors to render: per-list override wins, else the preset
        // value. The viewer + web component use these. (WS3 precedence: preset
        // = base, override wins - the opposite of the old collapse-to-preset.)
        public string EffectiveContentTextColor { get; set; }

        public string EffectiveBackgroundColor { get; set; }

        public string EffectiveButtonColor { get; set; }

        public string EffectiveButtonTextColor { get; set; }

        // WS10: resolved featured-button colors (override wins, else preset).
        public string EffectiveFeaturedButtonColor { get; set; }

        public string EffectiveFeaturedButtonTextColor { get; set; }

        // WS7 fix 7: resolved title color (override, else preset, else content text color).
        public string EffectiveTitleColor { get; set; }

        /// <summary>Intro HTML. Sourced from the native ContentChannelItem.Content field.</summary>
        public string IntroContent { get; set; }

        public string FooterContent { get; set; }

        // ---- WS12: org-wide global header/footer (resolved + sanitized) ----
        // Content only when the matching Active toggle is on; null/empty otherwise.
        // Read once from the channel (cached) and identical for every list.
        public string GlobalHeaderContent { get; set; }

        public string GlobalFooterContent { get; set; }

        /// <summary>WS7: org-wide font choice resolved onto every list — true = load IvyJournal serif, false = Cormorant/Georgia.</summary>
        public bool UseIvyJournalFont { get; set; }

        // ---- Legacy display-parity fields (WS2.5) ----

        /// <summary>Optional title override; when set, displayed instead of Title.</summary>
        public string CustomTitle { get; set; }

        /// <summary>When true, the title is not rendered.</summary>
        public bool HideTitle { get; set; }

        /// <summary>BinaryFile GUID of the header image (rendered via /getimage.ashx).</summary>
        public string HeaderImage { get; set; }

        /// <summary>When true, the header image renders as a circle.</summary>
        public bool RoundHeaderImage { get; set; }

        /// <summary>BinaryFile GUID of a full-viewport background image (suppresses BackgroundColor when set).</summary>
        public string BackgroundImage { get; set; }

        /// <summary>WS11: BinaryFile GUID of a full-width banner behind the header area only.</summary>
        public string HeaderBackgroundImage { get; set; }

        /// <summary>Header video, YouTube. May be a full URL or a bare id; consumers normalize.</summary>
        public string HeaderVideo { get; set; }

        /// <summary>Header video, Vimeo. May be a full URL or a bare id; consumers normalize.</summary>
        public string HeaderVideoVimeoId { get; set; }

        public List<LinkItemBag> Items { get; set; } = new List<LinkItemBag>();

        /// <summary>
        /// Editable members of the list's primary security group. Populated
        /// only when the caller has EDIT on the list (i.e. via the management
        /// block's GetListDetail). REST and viewer responses leave this null.
        /// </summary>
        public List<GroupMemberBag> Members { get; set; }

        /// <summary>
        /// Read-only summary of any *other* AuthRules granting EDIT on the
        /// list (additional groups or individual persons). Populated only on
        /// the editor path. Used to surface Rock's full access picture
        /// without exposing it as editable here.
        /// </summary>
        public List<AuthRuleBag> AlsoHasEditAccess { get; set; }

        /// <summary>
        /// Friendly name of the linked primary security group (informational only).
        /// </summary>
        public string SecurityGroupName { get; set; }
    }
}

using System;
using System.Collections.Generic;

namespace org.secc.LinkList.ViewModels
{
    public class LinkListBag
    {
        public string Guid { get; set; }

        public int? Id { get; set; }

        public string Slug { get; set; }

        public string Title { get; set; }

        public bool IsPublic { get; set; } = true;

        /// <summary>
        /// Friendly name of the selected design preset (for list/grid display).
        /// Null when no preset is selected. Not persisted; derived from DesignId.
        /// </summary>
        public string DesignName { get; set; }

        /// <summary>
        /// Last modified timestamp of the underlying ContentChannelItem
        /// (for the management grid's "Modified" column).
        /// </summary>
        public System.DateTime? ModifiedDateTime { get; set; }

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

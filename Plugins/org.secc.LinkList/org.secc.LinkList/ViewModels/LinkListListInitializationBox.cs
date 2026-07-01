using System.Collections.Generic;

namespace org.secc.LinkList.ViewModels
{
    /// <summary>
    /// Configuration surfaced to the List block's Obsidian frontend
    /// (via GetObsidianBlockInitialization), following Rock's list-block
    /// convention: navigation URLs (with the ((Key)) placeholder) plus the
    /// add/delete capability flags.
    /// </summary>
    public class LinkListListInitializationBox
    {
        public Dictionary<string, string> NavigationUrls { get; set; } = new Dictionary<string, string>();

        public bool IsAddEnabled { get; set; }

        public bool IsDeleteEnabled { get; set; }

        public bool IsBlockVisible { get; set; } = true;

        /// <summary>
        /// WS12: true when the current person may edit the org-wide global
        /// header/footer (Administrate on the LinkList ContentChannel). Gates
        /// the Global Settings panel in the admin block.
        /// </summary>
        public bool CanManageGlobalSettings { get; set; }
    }
}

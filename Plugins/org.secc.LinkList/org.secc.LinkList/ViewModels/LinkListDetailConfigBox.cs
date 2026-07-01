using System.Collections.Generic;

namespace org.secc.LinkList.ViewModels
{
    /// <summary>
    /// Configuration surfaced to the Detail block's Obsidian frontend
    /// (via GetObsidianBlockInitialization). Carries the entity key read from
    /// the page parameter (null/"0" =&gt; add mode) and back-navigation URLs.
    /// </summary>
    public class LinkListDetailConfigBox
    {
        /// <summary>List item key (GUID) from the page parameter; null when adding.</summary>
        public string ItemKey { get; set; }

        public bool IsAddMode { get; set; }

        /// <summary>Default design preset (DefinedValue GUID) applied to new lists.</summary>
        public string DefaultDesignGuid { get; set; }

        public Dictionary<string, string> NavigationUrls { get; set; } = new Dictionary<string, string>();
    }
}

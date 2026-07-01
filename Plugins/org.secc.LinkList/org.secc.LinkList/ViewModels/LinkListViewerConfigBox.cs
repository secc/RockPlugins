namespace org.secc.LinkList.ViewModels
{
    /// <summary>
    /// Configuration surfaced to the Viewer block's Obsidian frontend.
    /// </summary>
    public class LinkListViewerConfigBox
    {
        /// <summary>
        /// URL to redirect to when no list matches the slug. Empty when no
        /// "Not Found Page" is configured (the viewer then shows a message).
        /// </summary>
        public string NotFoundUrl { get; set; }
    }
}

namespace org.secc.LinkList.ViewModels
{
    /// <summary>
    /// WS12: the org-wide global header/footer settings stored on the single
    /// LinkList ContentChannel. Edited from the admin List block; the RAW
    /// (unsanitized) content is carried here so the admin edits the original.
    /// The public display path resolves + sanitizes these in BuildBag.
    /// </summary>
    public class GlobalSettingsBag
    {
        public string HeaderContent { get; set; }

        public bool HeaderActive { get; set; }

        public string FooterContent { get; set; }

        public bool FooterActive { get; set; }

        /// <summary>WS7: load the IvyJournal serif (licensed domains only); off = Cormorant/Georgia fallback.</summary>
        public bool UseIvyJournalFont { get; set; }
    }
}

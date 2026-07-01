using System.Collections.Generic;

namespace org.secc.LinkList.ViewModels
{
    public class LinkListDetailInitializationBox
    {
        public bool CanEdit { get; set; }

        public bool CanDelete { get; set; }

        public LinkListBag LinkList { get; set; }

        /// <summary>
        /// Available design presets for the editor's design dropdown.
        /// Each entry: Value = DefinedValue GUID (string), Text = preset name.
        /// </summary>
        public List<DesignOptionBag> Designs { get; set; } = new List<DesignOptionBag>();
    }
}

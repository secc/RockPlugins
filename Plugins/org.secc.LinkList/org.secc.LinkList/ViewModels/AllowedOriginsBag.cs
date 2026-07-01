using System.Collections.Generic;

namespace org.secc.LinkList.ViewModels
{
    /// <summary>
    /// WS13: the admin Allowed Origins editor state. <see cref="BuiltIn"/> is the
    /// hardcoded bootstrap allowlist (read-only, always trusted); <see cref="Custom"/>
    /// is the admin-managed "Link List Allowed Origins" Defined Type values.
    /// </summary>
    public class AllowedOriginsBag
    {
        public List<string> BuiltIn { get; set; } = new List<string>();

        public List<string> Custom { get; set; } = new List<string>();
    }
}

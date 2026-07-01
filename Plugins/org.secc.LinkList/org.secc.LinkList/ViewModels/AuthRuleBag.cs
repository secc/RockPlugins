namespace org.secc.LinkList.ViewModels
{
    /// <summary>
    /// Read-only projection of an AuthRule that grants EDIT on a Link List
    /// item, surfaced for the "Also has edit access" panel in the editor.
    /// Either <see cref="GroupName"/> or <see cref="PersonName"/> is populated
    /// (the other is null) depending on the AuthRule subject.
    /// </summary>
    public class AuthRuleBag
    {
        public string Kind { get; set; } // "Group" | "Person" | "SpecialRole"

        public string GroupName { get; set; }

        public string PersonName { get; set; }

        public string SpecialRoleLabel { get; set; }

        public int Order { get; set; }
    }
}

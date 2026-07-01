// Shared TypeScript contracts mirroring the C# bag classes.

export interface LinkItemBag {
    guid?: string;
    matrixItemGuid?: string;
    itemType?: string; // "link" | "section" | "separator"
    text?: string;
    url?: string;
    target?: string;
    indentLevel?: number;
    isSectionCollapsed?: boolean;
    subtitle?: string; // link rows: secondary line under the text
    description?: string; // section rows: blurb under the heading
    isFeatured?: boolean; // link rows: the (single) featured button, hoisted to the top
    order?: number;
}

export interface GroupMemberBag {
    groupMemberId: number;
    personGuid: string;
    fullName?: string;
    email?: string;
    photoUrl?: string;
    isCurrentUser?: boolean;
}

export interface AuthRuleBag {
    kind: string; // "Group" | "Person" | "SpecialRole"
    groupName?: string;
    personName?: string;
    specialRoleLabel?: string;
    order?: number;
}

export interface DesignOptionBag {
    value: string;
    text: string;
    description?: string;
    contentTextColor?: string;
    backgroundColor?: string;
    buttonColor?: string;
    buttonTextColor?: string;
    featuredButtonColor?: string;
    featuredButtonTextColor?: string;
    titleColor?: string;
}

export interface LinkListBag {
    guid?: string;
    id?: number | null;
    slug?: string;
    title?: string;
    isPublic?: boolean;
    designId?: string | null;
    designName?: string;
    modifiedDateTime?: string | null;
    contentTextColor?: string;
    backgroundColor?: string;
    buttonColor?: string;
    buttonTextColor?: string;
    featuredButtonColor?: string;
    featuredButtonTextColor?: string;
    titleColor?: string;
    effectiveContentTextColor?: string;
    effectiveBackgroundColor?: string;
    effectiveButtonColor?: string;
    effectiveButtonTextColor?: string;
    effectiveFeaturedButtonColor?: string;
    effectiveFeaturedButtonTextColor?: string;
    effectiveTitleColor?: string;
    introContent?: string;
    footerContent?: string;
    globalHeaderContent?: string; // org-wide, resolved + sanitized (null when inactive)
    globalFooterContent?: string;
    useIvyJournalFont?: boolean; // org-wide font choice resolved onto every list
    customTitle?: string;
    hideTitle?: boolean;
    headerImage?: string;
    roundHeaderImage?: boolean;
    backgroundImage?: string;
    headerBackgroundImage?: string;
    headerVideo?: string;
    headerVideoVimeoId?: string;
    items?: LinkItemBag[];
    members?: GroupMemberBag[] | null;
    alsoHasEditAccess?: AuthRuleBag[] | null;
    securityGroupName?: string;
}

export interface LinkListDetailInitializationBox {
    canEdit: boolean;
    canDelete: boolean;
    linkList?: LinkListBag | null;
    designs?: DesignOptionBag[];
}

/** Config surfaced to the List block (GetObsidianBlockInitialization). */
export interface LinkListListInitializationBox {
    navigationUrls?: Record<string, string> | null;
    isAddEnabled?: boolean;
    isDeleteEnabled?: boolean;
    isBlockVisible?: boolean;
    canManageGlobalSettings?: boolean;
}

/** WS12: org-wide global header/footer settings (raw, edited in the admin block). */
export interface GlobalSettingsBag {
    headerContent?: string;
    headerActive?: boolean;
    footerContent?: string;
    footerActive?: boolean;
    useIvyJournalFont?: boolean;
}

/** WS13: an editable design preset (Link List Design DefinedValue) + usage count. */
export interface DesignPresetBag {
    value?: string; // DefinedValue GUID; empty when creating
    name?: string;
    description?: string;
    order?: number;
    backgroundColor?: string;
    contentTextColor?: string;
    titleColor?: string;
    buttonColor?: string;
    buttonTextColor?: string;
    featuredButtonColor?: string;
    featuredButtonTextColor?: string;
    usageCount?: number;
}

/** WS13: admin Allowed Origins editor state (built-in read-only + admin-managed custom). */
export interface AllowedOriginsBag {
    builtIn?: string[];
    custom?: string[];
}

/** Config surfaced to the Detail block (GetObsidianBlockInitialization). */
export interface LinkListDetailConfigBox {
    itemKey?: string | null;
    isAddMode?: boolean;
    defaultDesignGuid?: string | null;
    navigationUrls?: Record<string, string> | null;
}

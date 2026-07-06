// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
namespace org.secc.LinkList.SystemGuids
{
    /// <summary>
    /// Well-known GUID constants and configuration for the Link List plugin.
    /// </summary>
    public static class LinkListGuids
    {
        /// <summary>The Content Channel that holds Link List items.</summary>
        public const string LinkListChannel = "B4FE630A-B897-4062-8692-83192FB20C1D";

        /// <summary>The Content Channel Type for Link List items.</summary>
        public const string LinkListChannelType = "7C64E45E-471A-425F-BF21-2DEB4C99C20F";

        /// <summary>The Attribute Matrix Template used by the LinkList content channel.</summary>
        public const string LinkListMatrixTemplate = "86A87D50-127D-42AE-B322-3F047249D656";

        // ---- Production ContentChannelType item attributes ----
        public const string TypeAttributeContentTextColor = "79E0E1CB-2FB6-4588-BBD9-B21AD917A20B";
        public const string TypeAttributeCustomTitle = "6C85901B-D56E-49D2-BD27-5B4009201544";
        public const string TypeAttributeHideTitle = "32ED4B46-EA1F-4473-91B2-BE8EF48222A2";
        public const string TypeAttributeHeaderImage = "C9F920B9-B462-447B-ABC8-CE150A135F8D";
        public const string TypeAttributeRoundHeaderImage = "0C82A71D-4159-478F-A36C-DDCE2EE55ACE";
        public const string TypeAttributeBackgroundImage = "7099439C-9216-4400-A234-ACCCBFFA3F6E";
        // WS11: full-width banner behind the header area only. Migration 007.
        public const string TypeAttributeHeaderBackgroundImage = "8C2D5E7F-0A1B-4C3D-9E5F-1A2B3C4D5E6F";
        public const string TypeAttributeHeaderVideo = "2498FFB6-3FB2-4455-BAE1-D1D315A50881";
        public const string TypeAttributeHeaderVideoVimeoId = "2430C2C1-B191-493E-9F90-279FBCAD963C";
        public const string TypeAttributeBackgroundColor = "8880043C-150F-428C-A73D-896C57C26809";
        public const string TypeAttributeButtonColor = "5639D9E9-DC44-4D8B-AD0B-5E6EBF648121";
        public const string TypeAttributeButtonTextColor = "BE8E28B8-FB0C-40F8-B8E5-7097B97325C1";
        public const string TypeAttributeLinkListMatrix = "773A5F01-E099-47FC-82AD-F2E396232B2E";
        public const string TypeAttributeFooterContent = "3E9C7BDF-0C45-4D89-ABD6-9BEE977D341D";

        // ---- New matrix-row attributes added by this plugin (DO NOT change these GUIDs) ----
        public const string MatrixAttributeItemType = "5A2CD3F7-7AD3-4E8F-98A5-B84B9D0970E3";
        public const string MatrixAttributeIndentLevel = "9C98669C-E0CF-4AC3-ABF9-6FD1EB7E90A2";
        public const string MatrixAttributeIsSectionCollapsed = "2DEFA2C8-1A18-4FB0-AD83-0ABDDCE4C673";

        // WS9: per-row Subtitle (link) + Description (section). Migration 005.
        public const string MatrixAttributeSubtitle = "7F3B1C2D-4E5A-4B6C-8D9E-0A1B2C3D4E5F";
        public const string MatrixAttributeDescription = "8A4C2D3E-5F6B-4C7D-9E0A-1B2C3D4E5F6A";

        // WS10: per-row IsFeatured toggle (link rows, max one). Migration 006.
        public const string MatrixAttributeIsFeatured = "3D7A1E4F-6B82-4C93-A0D1-5E6F7A8B9C0D";

        // WS10: per-list featured-button colors (ContentChannelType item attrs). Migration 006.
        public const string TypeAttributeFeaturedButtonColor = "4E8B2F5A-7C93-4DA4-B1E2-6F7A8B9C0D1E";
        public const string TypeAttributeFeaturedButtonTextColor = "5F9C3A6B-8D04-4EB5-C2F3-7A8B9C0D1E2F";

        // WS10: featured-button colors on the Design Defined Type (preset slots). Migration 006.
        public const string DesignAttributeFeaturedButtonColor = "6A0D4B7C-9E15-4FC6-D3A4-8B9C0D1E2F3A";
        public const string DesignAttributeFeaturedButtonTextColor = "7B1E5C8D-AF26-40D7-E4B5-9C0D1E2F3A4B";

        // WS7 fix 7: dedicated title color (per-list + preset). Migration 010.
        public const string TypeAttributeTitleColor = "9E7F1A2B-3C4D-4E5F-A6B7-8C9D0E1F2A3B";
        public const string DesignAttributeTitleColor = "AF802B3C-4D5E-4F60-B7C8-9D0E1F2A3B4C";

        // ---- New ContentChannelItem-level attribute scoped to the LinkList channel ----
        public const string ItemAttributeIsPublic = "4BE03A44-31A4-4BB9-AE19-D0E4D0A57B0C";
        public const string ItemAttributeDesignId = "4F2DBC6E-2B11-4E54-A9F4-7E5316B2F7AE";

        // ---- WS12: org-wide global header/footer on the single LinkList ContentChannel. Migration 008. ----
        public const string ChannelAttributeGlobalHeaderContent = "9D3E6F8A-1B2C-4D5E-8F0A-2B3C4D5E6F70";
        public const string ChannelAttributeGlobalHeaderActive = "AE4F7A9B-2C3D-4E6F-9A1B-3C4D5E6F7081";
        public const string ChannelAttributeGlobalFooterContent = "BF508BAC-3D4E-4F70-AB2C-4D5E6F708192";
        public const string ChannelAttributeGlobalFooterActive = "C0619CBD-4E5F-4081-BC3D-5E6F708192A3";

        // ---- WS7: org-wide font override (IvyJournal serif headings; licensed domains only). Migration 009. ----
        public const string ChannelAttributeUseIvyJournalFont = "D5E6F7A8-1B2C-4D3E-9F40-5A6B7C8D9E0F";

        // ---- Defined Type for Link List Designs (color presets) ----
        public const string DefinedTypeLinkListDesign = "7C6D2D5A-5C9E-4F8B-93A1-C1A4A0B7D3F1";

        // ---- Defined Type for CORS allowed origins (admin-managed) ----
        public const string DefinedTypeLinkListAllowedOrigins = "8934E6E2-B797-46BD-B3FA-1ABEABD0D22C";

        // Defined Value attribute Keys carrying preset color values.
        public const string DesignAttributeContentTextColor = "E9C2D9F7-0F94-4DBF-9A6F-E58B79B82E4D";
        public const string DesignAttributeBackgroundColor = "C7B6F8C5-2F8E-4F8E-8C9F-8B7E55F3B3D2";
        public const string DesignAttributeButtonColor = "3F2A5B5C-1F1B-4C8E-9F2A-6E7D8C1B2A39";
        public const string DesignAttributeButtonTextColor = "BD3E5F1A-7C4F-4D2E-9B5A-2F1A6B3D9E7C";

        // Seeded preset Defined Values.
        public const string DesignSeccDefault = "A1F1B5C7-3E5D-4A9B-8E2D-7B9F1C3D5E7A";
        public const string DesignSeccMove = "B2E2C6D8-4F6E-5BAC-9F3E-8CAE2D4F6F8B";
        public const string DesignLightAiry = "C3F3D7E9-5A7F-6CBD-A04F-9DBF3E5A7B9C";
        public const string DesignHighContrast = "D4A4E8FA-6B8A-7DCE-B15A-AECA4F6B8CAD";

        // ---- Default Rock SecurityRole group type ----
        public const string GroupTypeSecurityRole = "AECE949F-704C-483E-A4FB-93D5E4720C4C";

        // ---- Block type GUIDs ----
        // The legacy combined "Manager" block GUID is repurposed as the List block.
        public const string BlockTypeLinkListList = "0A1D7863-3B9D-4205-B95A-82A96F4AFA02";
        public const string BlockTypeLinkListDetail = "E1672F91-F133-4CB2-A7D8-427FD0204BD3";
        public const string BlockTypeLinkListViewer = "6D230172-C145-43A9-8CE6-7B51CCEC61C2";

        // ---- Matrix attribute Keys (existing keys MUST match what is in production) ----
        public static class MatrixAttributeKey
        {
            // Existing keys (DO NOT recreate; preserved from legacy data)
            public const string Url = "URL";
            public const string LinkText = "LinkText";
            public const string Target = "Target";
            // NOTE: the legacy "AdditionalClasses" matrix attribute is intentionally
            // NOT referenced by this plugin (decided in WS2.5). The DB attribute and
            // its data are left untouched; the plugin neither reads nor writes it.

            // New keys added by the migration
            public const string ItemType = "ItemType";
            public const string IndentLevel = "IndentLevel";
            public const string IsSectionCollapsed = "IsSectionCollapsed";

            // WS9: optional per-row text. Subtitle is meaningful on link rows
            // (secondary line under the link text); Description on section rows.
            public const string Subtitle = "Subtitle";
            public const string Description = "Description";

            // WS10: link row may be the (single) featured button for the list.
            public const string IsFeatured = "IsFeatured";
        }

        public static class ChannelAttributeKey
        {
            // WS12: stored on the single LinkList ContentChannel (org-wide singleton).
            public const string GlobalHeaderContent = "GlobalHeaderContent";
            public const string GlobalHeaderActive = "GlobalHeaderActive";
            public const string GlobalFooterContent = "GlobalFooterContent";
            public const string GlobalFooterActive = "GlobalFooterActive";

            // WS7: when on, the viewer loads the IvyJournal serif (Adobe kit) for
            // headings; off (default) falls back to Cormorant Garamond / Georgia.
            public const string UseIvyJournalFont = "UseIvyJournalFont";
        }

        public static class ItemAttributeKey
        {
            // FooterContent is a type-level attribute; use TypeAttributeKey.FooterContent
            // for both read and write so they can't drift.
            // Intro content uses the native ContentChannelItem.Content field, not
            // an attribute. (The IntroContent attribute is intentionally not used.)
            public const string IsPublic = "IsPublic";
            public const string DesignId = "DesignId";
        }

        public static class TypeAttributeKey
        {
            public const string ContentTextColor = "ContentTextColor";
            public const string CustomTitle = "CustomTitle";
            public const string HideTitle = "HideTitle";
            public const string HeaderImage = "HeaderImage";
            public const string RoundHeaderImage = "RoundHeaderImage";
            public const string BackgroundImage = "BackgroundImage";
            // WS11: full-width header band image (distinct from HeaderImage/BackgroundImage).
            public const string HeaderBackgroundImage = "HeaderBackgroundImage";
            public const string HeaderVideo = "HeaderVideo";
            public const string HeaderVideoVimeoId = "HeaderVideoVimeoID";
            public const string BackgroundColor = "BackgroundColor";
            public const string ButtonColor = "ButtonColor";
            public const string ButtonTextColor = "ButtonTextColor";
            public const string LinkListMatrix = "LinkListMatrix";
            public const string FooterContent = "FooterContent";

            // WS10: featured-button color overrides (per list).
            public const string FeaturedButtonColor = "FeaturedButtonColor";
            public const string FeaturedButtonTextColor = "FeaturedButtonTextColor";

            // WS7 fix 7: dedicated title color (falls back to content text when unset).
            public const string TitleColor = "TitleColor";
        }

        public static class DesignAttributeKey
        {
            public const string ContentTextColor = "ContentTextColor";
            public const string BackgroundColor = "BackgroundColor";
            public const string ButtonColor = "ButtonColor";
            public const string ButtonTextColor = "ButtonTextColor";

            // WS10: featured-button colors carried by each preset.
            public const string FeaturedButtonColor = "FeaturedButtonColor";
            public const string FeaturedButtonTextColor = "FeaturedButtonTextColor";

            // WS7 fix 7: title color carried by each preset.
            public const string TitleColor = "TitleColor";
        }

        /// <summary>Naming prefix for auto-created list-specific security groups.</summary>
        public const string SecurityGroupNamePrefix = "RSR - Link List - ";

        public static class ItemTypeValue
        {
            public const string Link = "link";
            public const string Section = "section";
            public const string Separator = "separator";
        }

        /// <summary>
        /// Allowed origins for the public REST endpoint. The REST controller
        /// echoes the Origin header back only if it matches one of these entries.
        /// </summary>
        public static readonly string[] AllowedOrigins = new[]
        {
            "https://rock.secc.org",
            "https://secc.org",
            "https://www.secc.org",
            "https://my.southeastchristian.org",
            "https://southeastchristian.org",
            "https://www.southeastchristian.org",
            "https://se.church",
            "https://www.se.church"
        };
    }
}

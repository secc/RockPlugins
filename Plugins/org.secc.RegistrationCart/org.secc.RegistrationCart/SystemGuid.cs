// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
// Stable identifiers for everything this plugin creates in Rock. NEVER change a
// value after it has shipped — Rock uses them to find existing rows on upgrade.
namespace org.secc.RegistrationCart.SystemGuid
{
    public static class EntityType
    {
        public const string TEMPLATE_SETTING = "56FBC2AD-04C9-44D1-94AE-03BEA645E6D6";
        public const string INSTANCE_SETTING = "56F28B1F-1649-4875-AE44-6BAE73D875BF";
        public const string FEE_ITEM_SETTING = "AC2530A7-7EEE-4B01-9CBB-097E750CFA94";
        public const string DISCOUNT_CODE = "02A1BC98-B76E-4381-B1C6-F1471A365553";
        public const string CART = "55E049F9-7C9D-4A85-9C7A-B17CD39FE809";
        public const string CART_REGISTRANT = "39EDF736-AB2B-446D-B95F-00E482DA5021";
        public const string CART_LINE_ITEM = "93D8847D-28D5-45FA-BC96-A81937B87123";
        public const string CART_LINE_ITEM_ADD_ON = "193CCC4D-66DE-4D05-AA79-329FC3C1C32A";
        public const string CART_DISCOUNT = "7ED186EB-F1C1-4AA1-A8DD-BEC71483AD03";
        public const string BLOCK_CATALOG = "BE26617A-AF76-4C44-A42F-91C9B4630936";
        public const string BLOCK_OFFERING_DETAIL = "7581C2D7-CEFD-4B4F-8297-3268A03EDCBA";
        public const string BLOCK_CART = "ADDDCFCF-9E4C-4BAC-A8FE-168A6147CED8";
        public const string BLOCK_CHECKOUT = "55094464-858E-47CE-A7AF-E72B2BA550BF";
        public const string BLOCK_ADMIN_CATALOG_SETTINGS = "C4B05E7F-EA1A-4770-A27F-EC335D59DC36";
        public const string BLOCK_ADMIN_DISCOUNT_CODES = "461B02E3-3B8C-43C1-885D-BF5B1B543562";
    }

    public static class BlockType
    {
        public const string CATALOG = "3D9ED7FC-711E-4F15-8779-CF639D5B04F7";
        public const string OFFERING_DETAIL = "B4E8DCA9-B026-4518-A47A-17A4A441A857";
        public const string CART = "9F0160F0-0471-436D-A87E-D69FDB1305DA";
        public const string CHECKOUT = "42A13369-EB9F-4CA1-9415-91A8847EC281";
        public const string ADMIN_CATALOG_SETTINGS = "93B3AC65-60FD-40E2-999F-93DB9AAAD2F4";
        public const string ADMIN_DISCOUNT_CODES = "B0F8CA9C-A4A9-4BFC-8AB0-0FCCB7CB2622";
    }

    public static class BlockTypeAttribute
    {
        public const string CATALOG_OFFERING_DETAIL_PAGE = "156259E4-E5DE-4507-AEB4-3B0029822321";
        public const string CATALOG_CART_PAGE = "C8CCBAA5-0CE0-4159-AABE-E20BD7449FEE";
        public const string OFFERING_DETAIL_CART_PAGE = "E8ABC390-789E-4487-8597-69B8AF01159B";
        public const string OFFERING_DETAIL_CATALOG_PAGE = "30E4B6DD-145C-4FFD-AC7A-0EE674643804";
        public const string CART_CHECKOUT_PAGE = "DEA28D35-8BB1-41FA-9D3B-C6598774B49F";
        public const string CART_CATALOG_PAGE = "B7B84000-CAC4-4EDA-B2F0-B2D0D7DBADFD";
        public const string CHECKOUT_CART_PAGE = "AAF7A16C-5B1F-4F61-9D14-AE220A666371";
        public const string CHECKOUT_CATALOG_PAGE = "98F54855-F8CD-4238-9AEC-4B62FF97CC6E";
        public const string CART_OFFERING_DETAIL_PAGE = "5D2C8B0E-7A41-4F6E-9C3B-2E8A61D4F7A9";
    }

    public static class Page
    {
        public const string CAMP_CART = "0C819A69-E06E-4455-BD8A-D4B6775C4864";
        public const string OFFERING = "18F135DC-A236-4511-942F-CBA73773A7E3";
        public const string CART = "1E96EB24-B4C1-4241-AA29-D9208B1C474D";
        public const string CHECKOUT = "E5487008-F01E-485F-8CA4-F5969EDEF6CD";
        public const string ADMIN_CAMP_CART = "42254376-8BC0-4B23-A96F-C9F768A626D5";
        public const string ADMIN_DISCOUNT_CODES = "EE999A35-3DF4-4FE9-AC66-FD74535343D2";
    }

    public static class PageRoute
    {
        public const string CAMP_CART = "C3AC9D6D-2CA3-454F-B653-426D01811972";
        public const string OFFERING = "643CCA3A-639B-431A-BCB0-DB48D930433D";
        public const string CART = "BE5DDD11-159F-4A3E-B913-706DE13FFEAC";
        public const string CHECKOUT = "8116B61F-838A-42E0-9A8B-A74D6D817F93";
        public const string ADMIN_CAMP_CART = "0F1C0F77-52D7-4AA2-94D1-B14E61C6C00D";
        public const string ADMIN_DISCOUNT_CODES = "6E4C4AEE-DEC2-4C92-BCB3-328F797B51D1";
    }

    public static class Block
    {
        public const string CATALOG = "369334FC-7DA8-4536-BE07-27D407D3556E";
        public const string OFFERING_DETAIL = "05AC6988-0155-498C-AF42-1EB0991F31AB";
        public const string CART = "463C6B56-C228-480F-8903-8854B55FDBFB";
        public const string CHECKOUT = "99785046-6CB3-486E-943B-86A7D608E702";
        public const string ADMIN_CATALOG_SETTINGS = "3123C518-2B16-4C0F-A02E-C0D2990DE2FF";
        public const string ADMIN_DISCOUNT_CODES = "324EBB98-E4E7-4CA4-BB75-C6CB5A7BE268";
    }

    public static class ServiceJob
    {
        public const string CART_MAINTENANCE = "8CE4900A-F653-4003-BB87-7A964DB5B595";
    }

}

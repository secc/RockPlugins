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

using Rock.Model;

namespace org.secc.FamilyCheckin.Utilities
{
    public static class Constants
    {
        public const string VOLUNTEER_ATTRIBUTE_GUID = "F5DAD320-B77D-4282-98C9-35414FB0A6DC";
        public const string CACHE_TAG = "FamilyCheckin";
        public const string KIOSK_CATEGORY_STATION = "7E6EDB6C-7A11-4846-9D98-FB91A2082022";
        public const string KIOSK_CATEGORY_STAFFUSER = "DC20D4E0-6DC2-4183-A8E0-39F618B45CFC";
        public const string KIOSK_CATEGORY_MOBILEUSER = "4650C54F-808A-4C0B-9F40-D88E2F3474A5";
        public const string CHECKIN_SEARCH_TYPE_USERLOGIN = "FDBFFBF6-EC1A-4BA8-8F1E-4B4574D84E9D";
        public const string DEFINED_TYPE_ATTENDANCE_QUALIFIERS = "D5E93600-2196-44F0-A4A6-181586765B0C";
        public const string DEFINED_VALUE_MOBILE_DID_ATTEND = "7506C6D1-E73B-4947-9B6A-B01B93ACA512";
        public const string DEFINED_VALUE_ATTENDANCE_STATUS_WITH_PARENT = "25B4B1A5-9B20-4EDF-920B-A33098438FC3";
        public const string DEFINED_VALUE_MOBILE_NOT_ATTEND = "E1D5F3DB-A76F-43E7-8A76-57CF5B39120C";
        public const string DEFINED_TYPE_DISABLED_GROUPLOCATIONSCHEDULES = "4A3255CE-9363-402C-9998-B2998B6712E0";
        public const string DEFINED_TYPE_NESTED_CAMPUSES = "8C4E4C49-8AFB-4BDE-9DD0-E2253DF7E7B7";
        public const string DEFINED_VALUE_ATTRIBUTE_PARENT_CAMPUS = "ParentCampus";
        public const string DEFINED_VALUE_ATTRIBUTE_CHILD_CAMPUS = "ChildCampus";
        public const string LOCATION_ATTRIBUTE_ROOM_RATIO = "RoomRatio";
        public const string GROUP_ATTRIBUTE_LINK_LOCATIONS = "0A8723A4-4AEE-4D21-A32B-350845FC8FA9";
        public const string GROUP_ATTRIBUTE_MEMBERSHIP_GROUP = "22C7D86E-ABEA-4DFB-A83C-FC18100C9834";
        public const string GROUP_ATTRIBUTE_CHECK_REQUIREMENTS = "5B779E7F-C305-4883-B845-ACA7425D8922";
        public const string GROUP_ATTRIBUTE_MEMBER_ROLE = "A9B650BF-1F9B-4670-BEEE-51577EBCAA0A";
        public const string GROUP_ATTRIBUTE_ATTENDANCE_ON_GROUP = "42F9C881-EF80-452E-819D-B02BBC0F1229";
        public const string GROUP_TYPE_BREAKOUT_GROUPS = "224A3D85-5276-4C33-9436-5FDA7DD24D14";

        //GroupType Filters
        public const string GROUP_TYPE_BY_BASE = "6E7AD783-7614-4721-ABC1-35842113EF59";
        public const string GROUP_TYPE_BY_CHILDREN_CHECKIN_BASE = "5398A1C2-F422-4ADC-A48B-B9EFFE3598AD";
        public const string GROUP_TYPE_BY_AGE = "0572A5FE-20A4-4BF1-95CD-C71DB5281392";
        public const string GROUP_TYPE_BY_ABILITY_LEVEL = "13A6139D-EEEC-412D-8572-773ECA1939CC";
        public const string GROUP_TYPE_BY_GRADE = "4F9565A7-DD5A-41C3-B4E8-13F0B872B10B";
        public const string GROUP_TYPE_BY_BIRTHDAY = "3600C17B-1D92-4929-B7B7-BBC156F2D47A";
        public const string GROUP_TYPE_BY_SPECIAL_NEEDS = "D941EA54-AA6D-4B45-A2E0-ADBD68D61C8C";
        public const string GROUP_TYPE_BY_MEMBERSHIP = "2098AE58-58D4-4CEF-8D40-C2657D2E7A6A";
        public const string GROUP_TYPE_BY_DATAVIEW = "722D1D26-AD6A-4800-8F86-2AAB521A2FFD";
        public const string GROUP_TYPE_BY_TEXT = "374F4210-20D9-4DC9-B2ED-7B12D1EA3406";

        //Cookies
        public const string COOKIE_KIOSK_NAME = "kioskName";
    }
}

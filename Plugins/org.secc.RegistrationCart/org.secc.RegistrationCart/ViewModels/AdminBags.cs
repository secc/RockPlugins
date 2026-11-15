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
using System.Collections.Generic;
using Rock.ViewModels.Utility;

namespace org.secc.RegistrationCart.ViewModels
{
    #region Catalog settings (overlay on Rock registration templates / instances / fees)

    public class CatalogSettingsInitializationBox
    {
        public List<AdminTemplateBag> Templates { get; set; } = new List<AdminTemplateBag>();
        public bool CanEdit { get; set; }
        public string CatalogUrl { get; set; }
        /// <summary>How templates and instances without saved settings behave (the Registration Cart global attributes), to explain the page.</summary>
        public string DefaultsText { get; set; }
        /// <summary>Checkouts that took, or may have taken, a payment without finishing, for staff to reopen or close.</summary>
        public List<PendingCheckoutBag> PendingCheckouts { get; set; } = new List<PendingCheckoutBag>();
    }

    /// <summary>A checkout whose payment needs checking against the gateway.</summary>
    public class PendingCheckoutBag
    {
        public int CartId { get; set; }
        public string PayerName { get; set; }
        public int? PayerPersonId { get; set; }
        /// <summary>When the checkout stopped.</summary>
        public string StoppedText { get; set; }
        public decimal Total { get; set; }
        /// <summary>What is known: "Payment outcome unknown", "Charged, not registered", ...</summary>
        public string StateText { get; set; }
        /// <summary>The error kept on the cart.</summary>
        public string Detail { get; set; }
        /// <summary>The payment's record in Rock, when the charge got that far.</summary>
        public int? TransactionId { get; set; }
        public string TransactionCode { get; set; }
        /// <summary>"Camper: option" per line.</summary>
        public List<string> Lines { get; set; } = new List<string>();
        /// <summary>Nothing recorded as charged, so it can be reopened once the gateway has been checked.</summary>
        public bool CanReopen { get; set; }
        /// <summary>Still inside the minutes a running checkout is left alone; no action yet.</summary>
        public bool IsTooRecent { get; set; }
    }

    public class PendingCheckoutResultBag
    {
        public string ErrorMessage { get; set; }
        public List<PendingCheckoutBag> PendingCheckouts { get; set; } = new List<PendingCheckoutBag>();
    }

    public class AdminTemplateBag
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string CategoryName { get; set; }
        public string RockDescription { get; set; }
        public bool IsVisible { get; set; }
        public string ImageUrl { get; set; }
        public string CatalogDescription { get; set; }
        public int Order { get; set; }
        public List<AdminInstanceBag> Instances { get; set; } = new List<AdminInstanceBag>();
        public List<AdminFeeBag> Fees { get; set; } = new List<AdminFeeBag>();
    }

    public class AdminInstanceBag
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string WindowText { get; set; }
        public string CampusName { get; set; }
        public decimal Price { get; set; }
        public int? MaxAttendees { get; set; }
        public int RegisteredCount { get; set; }
        public bool HasAccount { get; set; }
        public bool IsOpen { get; set; }
        public bool IsVisible { get; set; }
        public int EligibleGradeBands { get; set; }
        public int Order { get; set; }
        /// <summary>yyyy-MM-dd; staff-entered event dates (blank = use the linked event, if any).</summary>
        public string EventStartDate { get; set; }
        public string EventEndDate { get; set; }
        /// <summary>What the cart currently shows for this instance's dates, and where it came from.</summary>
        public string EventDateText { get; set; }
        public string EventDateSource { get; set; }
    }

    public class AdminFeeBag
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string RuleText { get; set; }
        public List<AdminFeeItemBag> Items { get; set; } = new List<AdminFeeItemBag>();
    }

    public class AdminFeeItemBag
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Cost { get; set; }
        public int EligibleGradeBands { get; set; }
    }

    public class SaveTemplateResultBag
    {
        public string ErrorMessage { get; set; }
        public AdminTemplateBag Template { get; set; }
    }

    #endregion
}

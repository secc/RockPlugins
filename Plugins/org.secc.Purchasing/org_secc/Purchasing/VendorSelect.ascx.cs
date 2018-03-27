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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using org.secc.Purchasing;
using org.secc.Purchasing.Helpers;

using Rock;
using Rock.Web.UI;

namespace RockWeb.Plugins.org_secc.Purchasing
{
    public partial class VendorSelect : UserControl
    {
        private bool mCanAddNewVendor = true;
        private bool mIsReadOnly = false;
        private bool mIsNew = true;
        private int mVendorId;
        private string mVendorName;
        private string mVendorAddress;
        private string mVendorPhone;
        private string mVendorWebAddress;

        public bool CanAddNewVendor
        {
            get
            {
                return mCanAddNewVendor;
            }
            set
            {
                mCanAddNewVendor = value;
            }
        }

        private bool IsNew
        {
            get
            {
                if (ViewState["ucVendorSelect_IsNew"] != null)
                    mIsNew = bool.Parse(ViewState["ucVendorSelect_IsNew"].ToString());
                return mIsNew;
            }
            set
            {
                mIsNew = value;
                ViewState["ucVendorSelect_IsNew"] = value;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return mIsReadOnly;
            }
            set
            {
                mIsReadOnly = value;
                EnableTextboxes(value);
            }
        }

        public int VendorID
        {
            get 
            {
                if (ViewState[ "ucVendorSelect_VendorID"] != null)
                    mVendorId = int.Parse(ViewState["ucVendorSelect_VendorID"].ToString());
                return mVendorId; 
            }
            set
            {
                mVendorId = value;
                if (mVendorId == -1)
                    ViewState.Remove("ucVendorSelect_VendorID");
                else
                    ViewState["ucVendorSelect_VendorID"]= value;
                IsNew = false;
            }
        }
        public string VendorName
        {
            get
            {
                if (String.IsNullOrEmpty(mVendorName) && ViewState[ "ucVendorSelect_VendorName"] != null)
                    mVendorName = ViewState[ "ucVendorSelect_VendorName"].ToString();
                return mVendorName;
            }
            set
            {
                mVendorName = value;
                if (!String.IsNullOrEmpty(value))
                    ViewState[ "ucVendorSelect_VendorName"] = value;
                else
                    ViewState.Remove("ucVendorSelect_VendorName");
                IsNew = false;
            }
        }

        public string VendorAddress
        {
            get 
            {
                if (String.IsNullOrEmpty(mVendorAddress) && ViewState["ucVendorSelect_VendorAddress"] != null)
                    mVendorAddress = ViewState["ucVendorSelect_VendorAddress"].ToString();
                return mVendorAddress; 
            }
            set
            {
                mVendorAddress = value;
                if (String.IsNullOrEmpty(mVendorAddress))
                    ViewState.Remove("ucVendorSelect_VendorAddress");
                else
                    ViewState["ucVendorSelect_VendorAddress"] = value;
                IsNew = false;
            }
        }

        public string VendorPhone
        {
            get 
            {
                if (String.IsNullOrEmpty(mVendorPhone) && ViewState["ucVendorSelect_VendorPhone"] != null)
                    mVendorPhone = ViewState["ucVendorSelect_VendorPhone"].ToString(); 
                return mVendorPhone;
            }
            set
            {
                mVendorPhone = value;
                if (String.IsNullOrEmpty(mVendorPhone))
                    ViewState.Remove("ucVendorSelect_VendorPhone");
                else
                    ViewState["ucVendorSelect_VendorPhone"] = value;
                IsNew = false;
            }
        }
        public string VendorWebAddress
        {
            get 
            {
                if (String.IsNullOrEmpty(mVendorWebAddress) && ViewState["ucVendorSelect_VendorWebAddress"] != null)
                    mVendorWebAddress = ViewState["ucVendorSelect_VendorWebAddress"].ToString(); 
                return mVendorWebAddress; 
            }
            set
            {
                mVendorWebAddress = value;
                if (String.IsNullOrEmpty(mVendorWebAddress))
                    ViewState.Remove("ucVendorSelect_VendorWebAddress");
                else
                    ViewState["ucVendorSelect_VendorWebAddress"] = value;
                IsNew = false;
            }
        }


        #region Page Events
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                BuildVendorList(); 
            }
        }


        protected void ddlVendor_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList VendorDDL = (DropDownList)sender;
            int vID = 0;

            if (!int.TryParse(VendorDDL.SelectedValue, out vID))
                return;
            ClearTextFields();

            switch (vID)
            {
                case -1:
                    EnableTextboxes(false);
                    break;
                case 0:
                    EnableTextboxes(true);
                    break;
                default:
                    LoadVendor(vID);
                    EnableTextboxes(false);
                    break;
            }
        }

        #endregion

        #region Public

        public void Show()
        {
            ClearTextFields();
            if (!IsNew)
            {
                LoadVendor(VendorID);
            }
            else
            {
                if (ddlVendor.Items.FindByValue("-1") != null)
                    ddlVendor.SelectedValue = "-1";
                ClearProperties();
                EnableTextboxes(false);
            }

            if (IsReadOnly)
            {
                ScriptManager.RegisterStartupScript(this, typeof(UserControl), "DisableVendorDropDown", "disableVendorDropdown(" + IsReadOnly.ToString().ToLower() + ");", true);
            }
        }

        public void Reset()
        {
            ClearTextFields();
            SetStatusMessage(String.Empty);
            if (!IsNew)
                LoadVendor(VendorID);
            else
                EnableTextboxes(false);
        }

        public bool Update()
        {
            bool updateSuccessful = false;
            Dictionary<string, string> ValErrors = Validate();
            if (ValErrors.Count != 0)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append("<ul type=\"disc\">");
                foreach (KeyValuePair<string, string> item in ValErrors)
                {
                    sb.AppendFormat("<li>{0}-{1}</li>", item.Key, item.Value);
                }
                sb.AppendFormat("</ul>");
                SetStatusMessage(sb.ToString());
            }
            else
            {
                UpdateProperties();
                updateSuccessful = true;
            }
            return updateSuccessful;
        }

        public void Clear()
        {
            ClearProperties();
            ClearTextFields();
            EnableTextboxes(false);
        }

        #endregion

        #region Private

        private void BuildVendorList()
        {
            ddlVendor.Items.Clear();

            ddlVendor.DataValueField = "VendorID";
            ddlVendor.DataTextField = "VendorName";
            ddlVendor.DataSource = Vendor.LoadVendors(true).OrderBy(x => x.VendorName);
            ddlVendor.DataBind();

            ddlVendor.Items.Insert(0, new ListItem("--Select---", "-1"));
            
            if(CanAddNewVendor)
                ddlVendor.Items.Insert(1, new ListItem("[New Vendor]", "0"));
        }

        private void ClearProperties()
        {
       
            VendorID = -1;
            VendorName = String.Empty;
            VendorAddress = String.Empty;
            VendorPhone = String.Empty;
            VendorWebAddress = String.Empty;
            IsNew = true;
        }

        private void ClearTextFields()
        {
            txtName.Text = String.Empty;
            txtAddress.Text = String.Empty;
            txtCity.Text = String.Empty;
            txtState.Text = String.Empty;
            txtZip.Text = String.Empty;
            txtVendorPhone.Text = String.Empty;
            txtVendorPhoneExt.Text = String.Empty;
            txtWebAddress.Text = String.Empty;
            //ddlVendor.SelectedValue = "-1";
        }

        private void EnableTextboxes(bool enabled)
        {
            if (enabled && IsReadOnly)
                enabled = false;

            txtName.ReadOnly = !enabled;
            txtAddress.ReadOnly = !enabled;
            txtCity.ReadOnly = !enabled;
            txtState.ReadOnly = !enabled;
            txtZip.ReadOnly = !enabled;
            txtVendorPhone.ReadOnly = !enabled;
            txtVendorPhoneExt.ReadOnly = !enabled;
            txtWebAddress.ReadOnly = !enabled;

            //ScriptManager.RegisterStartupScript(this, typeof(UserControl), "SetReadOnly", "setReadOnly(" + (!enabled).ToString().ToLower() + ");", true);
        }

        private void LoadVendor(int vID)
        {
            
            if (vID > 0)
            {
                Vendor v = new Vendor(vID);
                EnableTextboxes(false);
                
                if (ddlVendor.SelectedValue != vID.ToString() && ddlVendor.Items.FindByValue(vID.ToString()) != null)
                    ddlVendor.SelectedValue = vID.ToString();

                txtName.Text = v.VendorName;

                if (v.Address != null)
                {
                    txtAddress.Text = v.Address.StreetAddress;
                    txtCity.Text = v.Address.City;
                    txtState.Text = v.Address.State;
                    txtZip.Text = v.Address.PostalCode;
                }

                if (v.Phone != null)
                {
                    txtVendorPhone.Text = v.Phone.Number;

                    if (!String.IsNullOrEmpty(v.Phone.Extension))
                        txtVendorPhoneExt.Text = v.Phone.Extension;
                }

                if (!String.IsNullOrEmpty(v.WebAddress))
                    txtWebAddress.Text = v.WebAddress;
            }
            else if (VendorID == 0)
            {
                EnableTextboxes(true);
                if (ddlVendor.SelectedValue != "0")
                    ddlVendor.SelectedValue = "0";

                txtName.Text = VendorName;

                if (!String.IsNullOrEmpty(VendorAddress) && new Address(VendorAddress).IsValid())
                {
                    Address a = new Address(VendorAddress);
                    txtAddress.Text = a.StreetAddress;
                    txtCity.Text = a.City;
                    txtState.Text = a.State;
                    txtZip.Text = a.PostalCode;
                }

                if (!String.IsNullOrEmpty(VendorPhone) && new PhoneNumber(VendorPhone).IsValid())
                {
                    PhoneNumber p = new PhoneNumber(VendorPhone);
                    txtVendorPhone.Text = p.Number;
                    if (!String.IsNullOrEmpty(p.Extension))
                        txtVendorPhoneExt.Text = p.Extension;
                }

                if (!String.IsNullOrEmpty(VendorWebAddress))
                    txtWebAddress.Text = VendorWebAddress;
            }
        }

        private void SetStatusMessage(string msg)
        {
            lblStatus.Visible = !String.IsNullOrEmpty(msg);
            lblStatus.InnerHtml = msg;

        }

        private void UpdateProperties()
        {
            int vID = 0;
            if (!int.TryParse(ddlVendor.SelectedValue, out vID))
                return;
            ClearProperties();
            switch (vID)
            {
                case -1:
                    break;
                case 0:
                    VendorID = 0;
                    VendorName = txtName.Text.Trim();
                    if (!String.IsNullOrEmpty(txtAddress.Text) && !string.IsNullOrEmpty(txtCity.Text) && !string.IsNullOrEmpty(txtState.Text) && !string.IsNullOrEmpty(txtZip.Text))
                        VendorAddress = new Address(txtAddress.Text, txtCity.Text, txtState.Text, txtZip.Text).ToArenaFormat();
                    else
                        VendorAddress = String.Empty;

                    if (!String.IsNullOrEmpty(txtVendorPhone.Text))
                        VendorPhone = new PhoneNumber(txtVendorPhone.Text, txtVendorPhoneExt.Text).ToArenaFormat();
                    else
                        VendorPhone = String.Empty;

                    if (!String.IsNullOrEmpty(txtWebAddress.Text))
                        VendorWebAddress = txtWebAddress.Text;
                    else
                        VendorWebAddress = String.Empty;
                    break;
                default:
                    Vendor v = new Vendor(vID);
                    if (v.VendorID > 0)
                    {
                        VendorID = v.VendorID;
                        VendorName = v.VendorName;
                        if (v.Address != null)
                            VendorAddress = v.Address.ToArenaFormat();
                        if (v.Phone != null)
                            VendorPhone = v.Phone.ToArenaFormat();
                        if (!String.IsNullOrEmpty(v.WebAddress))
                            VendorWebAddress = v.WebAddress;
                    }
                    break;
            }

        }

        private Dictionary<string,string> Validate()
        {
            Dictionary<string, string> ValErrors = new Dictionary<string, string>();
            bool StopProcessing = false;

            //if (ddlVendor.SelectedIndex == 0)
            //{
            //    StopProcessing = true;
            //    ValErrors.Add("Vendor Selection", "Please select a vendor from the list or [New Vendor]");
            //}
            int vID = 0;
            if (!StopProcessing && int.TryParse(ddlVendor.SelectedValue, out vID))
            {
                if (vID > 0 && (new Vendor(vID)).VendorID <= 0) {
                    //Invalid existing vendor selected
                    ValErrors.Add("Vendor Selection", "Selected Vendor not found.");
                } else if (vID == 0 && !String.IsNullOrEmpty(txtName.Text.Trim())) {
                    var existingVendor = ddlVendor.Items.OfType<ListItem>().FirstOrDefault(v => v.Text.Trim().ToLower() == txtName.Text.Trim().ToLower());
                    //New vendor entered & it already exists
                    if (existingVendor != null) {
                        ddlVendor.SelectedValue = existingVendor.Value;
                        ddlVendor_SelectedIndexChanged(ddlVendor, null);
                        ValErrors.Add("Vendor Name", "Vendor with this name already exists and has been selected for you. Press Update to save the vendor selection.");
                    }
                }
            }

            if (!StopProcessing && String.IsNullOrEmpty(txtName.Text))
                ValErrors.Add("Vendor Name", "Vendor Name is required when choosing a new vendor.");

            //if (!StopProcessing && !String.IsNullOrEmpty(txtAddress.Text.Trim()))
            //    foreach (KeyValuePair<string, string> valError in (new Address(txtAddress.Text.Trim())).Validate())
            //        ValErrors.Add(valError.Key, valError.Value);

            //if (!StopProcessing && !String.IsNullOrEmpty(txtVendorPhone.Text.Trim()))
            //    foreach (KeyValuePair<string, string> valError in (new PhoneNumber(txtVendorPhone.Text.Trim())).Validate())
            //        ValErrors.Add(valError.Key, valError.Value);

            if (!StopProcessing && (!String.IsNullOrEmpty(txtAddress.Text) || !String.IsNullOrEmpty(txtCity.Text) || !String.IsNullOrEmpty(txtState.Text) || !String.IsNullOrEmpty(txtZip.Text)))
            {
                if (String.IsNullOrEmpty(txtAddress.Text))
                    ValErrors.Add("Street Address", "Street Address is required when entering a Vendor's Address");
                if (string.IsNullOrEmpty(txtCity.Text))
                    ValErrors.Add("City", "City is required when entering a Vendor's Address");

                if (String.IsNullOrEmpty(txtState.Text))
                    ValErrors.Add("State", "State is required when entering a Vendor's Address");
                else if (!System.Text.RegularExpressions.Regex.IsMatch(txtState.Text, @"^[a-zA-Z]{2}$"))
                {
                    ValErrors.Add("State", "State is not formatted properly. Please use 2 character abbreviation.");
                }

                if (String.IsNullOrEmpty(txtZip.Text))
                    ValErrors.Add("Zip Code", "Zip Code is required when entering a Vendor's Address");
            }

            if (!StopProcessing && (!String.IsNullOrEmpty(txtVendorPhone.Text) || !String.IsNullOrEmpty(txtVendorPhoneExt.Text)))
            {
                PhoneNumber P = new PhoneNumber(txtVendorPhone.Text, txtVendorPhoneExt.Text);
                foreach (KeyValuePair<string, string> valError in P.Validate())
                    ValErrors.Add(valError.Key, valError.Value);
            }

            StopProcessing = true;

            if (!StopProcessing && !String.IsNullOrEmpty(txtWebAddress.Text) && !System.Text.RegularExpressions.Regex.IsMatch(txtWebAddress.Text, @"^((http|ftp|https):\/\/)?[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?$"))
                ValErrors.Add("Web Address", "Web Address must be in http://www.domain.com or www.domain.com format.");
            return ValErrors;
        }

        #endregion



    }
}
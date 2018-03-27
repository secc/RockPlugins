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
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock.Web.UI;
using Rock;
using org.secc.Purchasing;
using System.ComponentModel;

namespace RockWeb.Plugins.org_secc.Purchasing
{
    [DisplayName("Vendor Detail")]
    [Category("SECC > Purchasing")]
    [Description("View/Edit a specific vendor.")]
    public partial class VendorDetail : RockBlock
    {
        private int VendorID
        {
            get
            {
                int mVendorID = 0;
                if (Request.QueryString["VendorID"] != null)
                {
                    int.TryParse(Request.QueryString["VendorID"], out mVendorID);
                }

                return mVendorID;
            }
        }

        private bool CanEdit
        {
            get
            {
                return UserCanEdit;
            }
        }

        private bool Success
        {
            get
            {
                return PageParameter("Success").AsBoolean();
            }
        }


        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                bool editMode = Request.QueryString["EditMode"].AsBoolean();
                SetMode(CanEdit && (editMode || VendorID == 0));
                if (Success)
                    SetStatus("Vendor Successfully Saved.", false);
            }
        }

        protected void btnEdit_click(object sender, EventArgs e)
        {
            if (CanEdit)
                SetMode(true);

        }


        protected void btnSave_click(object sender, EventArgs e)
        {
            int ReturnedID = SaveVendor();

            if (ReturnedID > 0)
            {
                if (VendorID == 0)
                {
                    Response.Redirect(Request.Url.AbsolutePath+"?VendorID=" + ReturnedID.ToString() + "&EditMode=true&Success=1&t=View Vendor&pb=&sb=");
                }
                else
                {
                    SetMode(false);
                    SetStatus("Vendor Successfully Saved.", false);
                }
            }
        }

        protected void btnReset_click(object sender, EventArgs e)
        {
            ClearForm();
            LoadVendor(true);
        }

        protected void btnCancel_click(object sender, EventArgs e)
        {
            if (VendorID > 0)
            {
                SetMode(false);
            }
            else
            {
                //Response.Redirect("~/default.aspx?page=" + VendorListPageSetting);
            }
        }

        private void ClearForm()
        {

            txtVendorName.Text = String.Empty;
            txtPhone.Text = String.Empty;
            txtExt.Text = String.Empty;
            txtAddressStreet1.Text = String.Empty;
            txtAddressCity.Text = String.Empty;
            txtAddressState.Text = String.Empty;
            txtAddressZip.Text = String.Empty;
            txtWebAddress.Text = String.Empty;
            txtTerms.Text = String.Empty;
            txtWebAddress.Text = string.Empty;
            lblStatus.InnerText = String.Empty;
            lblStatus.Visible = false;
            
        }

        private void SetMode(bool isEditMode)
        {
            ClearForm();

            if (isEditMode && !CanEdit)
            {
                isEditMode = false;
                SetStatus("User does not have permission to edit a vendor.", true);
            }

            if (VendorID > 0)
            {
                LoadVendor(isEditMode);
            }

        }

        private void LoadVendor(bool isEditMode)
        {
            if (VendorID > 0)
            {
                Vendor V = new Vendor(VendorID);

                if (V != null)
                {
                        txtVendorName.Text = V.VendorName;
                        if (V.Address != null)
                        {
                            txtAddressStreet1.Text = V.Address.StreetAddress;
                            txtAddressCity.Text = V.Address.City;
                            txtAddressState.Text = V.Address.State;
                            txtAddressZip.Text = V.Address.PostalCode;
                        }

                        if (V.Phone != null)
                        {
                            txtPhone.Text = V.Phone.FormatNumber();
                            txtExt.Text = V.Phone.Extension;
                        }

                        txtWebAddress.Text = V.WebAddress;
                        txtTerms.Text = V.Terms;
                        chkActiveEdit.Checked = V.Active;
                    if (!isEditMode)
                    {
                        txtVendorName.ReadOnly = true;
                        txtAddressStreet1.ReadOnly = true;
                        txtAddressCity.ReadOnly = true;
                        txtAddressState.ReadOnly = true;
                        txtAddressZip.ReadOnly = true;

                        txtPhone.ReadOnly = true;
                        txtExt.ReadOnly = true;

                        txtWebAddress.ReadOnly = true;
                        txtTerms.ReadOnly = true;
                        chkActiveEdit.Enabled = false;
                        btnSave.Visible = false;
                        btnReset.Visible = false;
                    }
                    
                }
            }
        }

        private int SaveVendor()
        {
            int vID = 0;
            Vendor v = null;
            if (Validate())
            {
                if (VendorID > 0)
                {
                    v = new Vendor(VendorID);
                }
                else
                {
                    v = new Vendor();
                }
                v.VendorName = txtVendorName.Text;

                if (!string.IsNullOrEmpty(txtAddressStreet1.Text) && !string.IsNullOrEmpty(txtAddressCity.Text) && !string.IsNullOrEmpty(txtAddressState.Text) && !string.IsNullOrEmpty(txtAddressZip.Text))
                    v.Address = new org.secc.Purchasing.Helpers.Address(txtAddressStreet1.Text, txtAddressCity.Text, txtAddressState.Text, txtAddressZip.Text);
                else
                    v.Address = null;

                if (!string.IsNullOrEmpty(txtPhone.Text))
                {
                    v.Phone = new org.secc.Purchasing.Helpers.PhoneNumber(txtPhone.Text, txtExt.Text);
                }
                else
                {
                    v.Phone = null;
                }

                if (!string.IsNullOrEmpty(txtWebAddress.Text))
                    v.WebAddress = txtWebAddress.Text;

                if(!string.IsNullOrEmpty(txtTerms.Text))
                {
                    v.Terms = txtTerms.Text;
                }

                v.Active = chkActiveEdit.Checked;

                vID = v.Save(CurrentUser.UserName);
            }

            return vID;
        }

        private bool Validate()
        {
            bool isValid = true;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (String.IsNullOrEmpty(txtVendorName.Text))
            {
                isValid = false;
                sb.AppendLine("- Vendor Name is required.");
            }

            if (!String.IsNullOrEmpty(txtPhone.Text) && !System.Text.RegularExpressions.Regex.IsMatch(txtPhone.Text, @"^\D?(\d{3})\D?\D?(\d{3})\D?(\d{4})$")) 
            {
                isValid = false;
                sb.AppendLine("- Phone is not formatted properly");
            }

            if (!String.IsNullOrEmpty(txtAddressZip.Text) && !System.Text.RegularExpressions.Regex.IsMatch(txtAddressZip.Text, @"^\d{5}-\d{4}|\d{5}|[A-Z]\d[A-Z] \d[A-Z]\d$"))
            {
                isValid = false;
                sb.AppendLine("- Zip/Postal is not formatted properly.");
            }

            if (!String.IsNullOrEmpty(txtWebAddress.Text) && !System.Text.RegularExpressions.Regex.IsMatch(txtWebAddress.Text, @"^(http|ftp|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?$")) 
            {
                isValid = false;
                sb.AppendLine("- Web address must be in http://www.site.com format.");
            }
            if (sb.ToString().Length > 0)
            {
                SetStatus(sb.ToString(), true);
            }

            return isValid;
        }

        private void SetStatus(string msgText, bool isError)
        {
            lblStatus.InnerText = msgText;

            if (isError)
            {
                lblStatus.AddCssClass("alert-danger");
            }
            else
            {
                lblStatus.AddCssClass("alert-success");
            }
            lblStatus.Visible = true;
        }

    }
}
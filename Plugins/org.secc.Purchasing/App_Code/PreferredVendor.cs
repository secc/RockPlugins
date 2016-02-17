using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace org.secc.Purchasing
{
    [Serializable]
    public class PreferredVendor
    {
        private Vendor mVendor;

        public int VendorID { get; set; }
        public string Name { get; set; }
        public Helpers.Address Address { get; set; }
        public Helpers.PhoneNumber Phone { get; set; }
        public string WebAddress { get; set; }

        [XmlIgnore]
        public Vendor Vendor
        {
            get
            {
                if (mVendor == null && VendorID > 0)
                    mVendor = new Vendor(VendorID);
                return mVendor;
            }
        }

        #region Constructors
        public PreferredVendor()
        {
            Init();
        }

        public PreferredVendor(int vendorID)
        {
            Init();
            LoadFromVendor(vendorID);
        }
        #endregion

        public void LoadFromVendor(int vendorID)
        {
            Vendor v = new Vendor(vendorID);
            VendorID = v.VendorID;
            Name = v.VendorName;
            Address = v.Address;
            Phone = v.Phone;
            WebAddress = v.WebAddress;
        }

        #region Private

        private void Load(Vendor v)
        {
            VendorID = v.VendorID;
            Name = v.VendorName;
            Address = v.Address;
            Phone = v.Phone;
            WebAddress = v.WebAddress;
            mVendor = v;

        }

        private void Init()
        {
            VendorID = 0;
            Name = String.Empty;
            Address = null;
            Phone = null;
            WebAddress = String.Empty;
            mVendor = null;
        }
        #endregion

    }
}

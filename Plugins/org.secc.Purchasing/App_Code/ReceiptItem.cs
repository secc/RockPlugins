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
using System.Text;
using System.Xml.Serialization;

using org.secc.Purchasing.DataLayer;
using Rock.Model;
namespace org.secc.Purchasing
{
    public class ReceiptItem: PurchasingBase
    {
        #region Fields
        private Receipt mReceipt;
        private PurchaseOrderItem mPOItem;
        private Person mCreatedBy;
        private Person mModifiedBy;
        #endregion

        #region Properties
        public int ReceiptItemID { get; set; }
        public int ReceiptID { get; set; }
        public int POItemID { get; set; }
        public int QuantityReceived { get; set; }
        public string CreatedByUserID { get; set; }
        public string ModifiedByUserID { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public bool Active { get; set; }

        public Receipt Receipt
        {
            get
            {
                if ((mReceipt == null || mReceipt.ReceiptID != ReceiptID) && ReceiptID > 0)
                    mReceipt = new Receipt(ReceiptID);
                return mReceipt;
            }
        }

        public PurchaseOrderItem POItem
        {
            get
            {
                if ((mPOItem == null || mPOItem.PurchaseOrderItemID != POItemID) && POItemID > 0)
                    mPOItem = new PurchaseOrderItem(POItemID);
                return mPOItem;
            }
        }

        public Person CreatedBy
        {
            get
            {
                if (mCreatedBy == null && !String.IsNullOrEmpty(CreatedByUserID))
                    mCreatedBy = userLoginService.GetByUserName(CreatedByUserID).Person;
                return mCreatedBy;
            }
        }

        public Person ModifiedBy
        {
            get
            {
                if (mModifiedBy == null && !String.IsNullOrEmpty(ModifiedByUserID))
                    mModifiedBy = userLoginService.GetByUserName(ModifiedByUserID).Person;
                return mModifiedBy;
            }
        }


        #endregion

        #region Constructors
        public ReceiptItem() 
        {
            Init();
        }
        
        public ReceiptItem(int receiptItemId)
        {
            Load(receiptItemId);
        }

        public ReceiptItem(ReceiptItemData data)
        {
            Load(data);
        }
        #endregion

        #region Public

        public void Save(string uid)
        {
            try
            {
                if (String.IsNullOrEmpty(uid))
                    throw new ArgumentNullException("UID", "User ID is required.");
                using (PurchasingContext Context = ContextHelper.GetDBContext())
                {
                    Enums.HistoryType ChangeType;
                    ReceiptItem Original = null;
                    ReceiptItemData Data;

                    if (ReceiptItemID <= 0)
                    {
                        Data = new ReceiptItemData();
                        ChangeType = Enums.HistoryType.ADD;
                        Data.date_created = DateTime.Now;
                        Data.created_by = uid;
                    }
                    else
                    {
                        Data = Context.ReceiptItemDatas.FirstOrDefault(x => x.receipt_item_id == ReceiptItemID);
                        Original = new ReceiptItem(Data);
                        ChangeType = Enums.HistoryType.UPDATE;
                    }

                    Data.receipt_id = ReceiptID;
                    Data.purchase_order_item_id = POItemID;
                    Data.quantity_received = QuantityReceived;
                    Data.date_modified = DateTime.Now;
                    Data.modified_by = uid;
                    Data.active = Active;

                    if (ReceiptItemID <= 0)
                        Context.ReceiptItemDatas.InsertOnSubmit(Data);

                    Context.SubmitChanges();

                    Load(Data);

                    SaveHistory(ChangeType, Original, uid);

                }
            }
            catch (Exception ex)
            {
                throw new RequisitionException("An error has occureed while saving receipt item.", ex);
            }
        }

        public void SaveHistory(Enums.HistoryType changeType, ReceiptItem original, string uid)
        {
            History h = new History();

            h.ObjectTypeName = this.GetType().ToString();
            h.Identifier = ReceiptItemID;
            h.OrganizationID = POItem.PurchaseOrder.OrganizationID;
            h.ChangeType = changeType;

            switch (changeType)
            {
                case org.secc.Purchasing.Enums.HistoryType.ADD:
                    h.OriginalXML = null;
                    h.UpdatedXML = Serialize(this);
                    break;
                case org.secc.Purchasing.Enums.HistoryType.UPDATE:
                    h.OriginalXML = Serialize(original);
                    h.UpdatedXML = Serialize(this);
                    break;
                case org.secc.Purchasing.Enums.HistoryType.DELETE:
                    h.OriginalXML = Serialize(this);
                    h.UpdatedXML = null;
                    break;

            }

            h.Active = true;
            h.Save(uid);
        }

        public Dictionary<string, string> Validate()
        {
            Dictionary<string, string> ValErrors = new Dictionary<string, string>();
            if (POItemID <= 0 || POItem != null || POItem.PurchaseOrderItemID <= 0)
                ValErrors.Add("Purchase Order Item ID", "Purchase Order Item ID is required and must be greater than zero.");
            if (QuantityReceived <= 0)
                ValErrors.Add("Quantity Recieved", "Quantity Received must be greater than zero.");

            return ValErrors;
        }
        #endregion

        #region Private
        private void Init()
        {
            ReceiptItemID = 0;
            ReceiptID = 0;
            POItemID = 0;
            QuantityReceived = 0;
            CreatedByUserID = String.Empty;
            ModifiedByUserID = String.Empty;
            DateCreated = DateTime.MinValue;
            DateModified = DateTime.MinValue;
            Active = true;
        }

        private void Load(ReceiptItemData data)
        {
            Init();
            if (data != null)
            {
                ReceiptItemID = data.receipt_item_id;
                ReceiptID = data.receipt_id;
                POItemID = data.purchase_order_item_id;
                QuantityReceived = data.quantity_received;
                CreatedByUserID = data.created_by;
                ModifiedByUserID = data.modified_by;
                DateCreated = data.date_created;
                DateModified = data.date_modified;
                Active = data.active;
            }
        }

        private void Load(int receiptItemId)
        {
            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                Load( Context.ReceiptItemDatas.FirstOrDefault( ri => ri.receipt_item_id == receiptItemId ) );
            }
        }

        #endregion


    }
}

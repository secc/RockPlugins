using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using org.secc.Purchasing.DataLayer;

using Rock;
using Rock.Model;

namespace org.secc.Purchasing
{
    [Serializable]
    public class Requisition : PurchasingBase
    {
        #region Fields
        private DefinedValue mRequisitionType;
        private DefinedValue mState;
        private Person mCreatedBy;
        private Person mModifiedBy;
        private Person mRequester;
        private Person mAcceptedBy;
        private List<Approval> mApprovals;
        private List<RequisitionItem> mItems;
        private List<Note> mNotes;
        private List<Attachment> mAttachments;
        private List<PaymentCharge> mCharges;
        private DefinedValue mMinistry;
        private DefinedValue mLocation;
        private CapitalRequest mCapitalRequest;
        private Person mAssignedTo;

        private static Guid StatusTypeGUID = new Guid("DF34A45F-C4DB-4F6D-878A-29AADB561AF7");
        private static Guid RequisitionTypeGUID = new Guid("BD564328-4C68-4BC1-9F82-63621192AB8A");

        private static Guid DraftStatusGuid = new Guid("46A8A950-FFB8-4EF5-A8A1-1E1C859B246B");
        private static Guid PendingApprovalStatusGuid = new Guid("7A304FD4-1E9D-4AB3-A0BA-A8A7FC26BABB");
        private static Guid ReturnedToRequesterStatusGuid = new Guid("9943F3BA-B077-4569-B65D-8266EA18C623");
        private static Guid ApprovedStatusGuid = new Guid("67239CA2-766E-44AC-B106-F72E046F4316");
        private static Guid SubmittedToPurchasingGuid = new Guid("22026706-A51D-44D7-8072-945AAB64A364");
        private static Guid AcceptedByPurchasingGuid = new Guid("13C2F4A3-0568-48BD-9F3E-170AFEB851D5");
        private static Guid PartiallyOrderedGuid = new Guid("7F765C75-5599-4E5C-AA4D-3C72F7CB8771");
        private static Guid OrderedByPurchasingGuid = new Guid("1D9794AB-7237-4FDF-9232-73B06B92FEAB");
        private static Guid PartiallyReceivedGuid = new Guid("898FC727-6839-4E3B-B837-984869BCF741");
        private static Guid ReceivedGuid = new Guid("9616DE4A-EE02-4C0E-8B9E-435FDF5434BA");
        private static Guid BilledGuid = new Guid("7DA57C64-7E07-4430-91FE-FCE08F869C9E");
        private static Guid ReopenedGuid = new Guid("A3FABBCC-CB72-44B5-A574-ACC21CB74856");
        private static Guid ClosedGuid = new Guid("F5BE7024-0E91-4E72-9741-823929A7D189");
        private static Guid CancelledGuid = new Guid("DD29DBEF-5269-48D6-B953-DDF28C886E89");

        private static Guid PurchaseReqTypeGuid = new Guid( "49100B56-A17D-47A0-84C8-42241EAFF7B4" );
        private static Guid BidRequestReqTypeGuid = new Guid( "10186221-12AC-4758-8F88-CB59822BC1E5" );
        private static Guid BlanketReqTypeGuid = new Guid( "8C8C152C-3022-458A-A667-BA5856080676" );
        private static Guid CapitalReqTypeGuid = new Guid( "BC2C7E72-083A-4158-BBB7-909F7B48A0B4" );
        private static Guid PORequestReqTypeGuid = new Guid( "4CDED713-9D4C-4703-A9F6-18F4AE46B7CF" );
        private static Guid TravelReqTypeGuid = new Guid( "2340ACED-9C85-4845-887A-4A2E2376C342" );

        #endregion

        #region Properties
        public int RequisitionID { get; set; }
        public string Title { get; set; }
        public int RequisitionTypeLUID { get; set; }
        public int RequesterID { get; set; }
        public int StatusLUID { get; set; }
        public string DeliverTo { get; set; }
        public bool IsApproved { get; set; }
        public bool IsOpen { get; set; }
        public PreferredVendor PreferredVendor { get; set; }
        public string CreatedByUserID { get; set; }
        public string ModifiedByUserID { get; set; }
        public DateTime DateAccepted { get; set; }
        public DateTime DatePurchased { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public bool Active { get; set; }
        public int OrganizationID { get; set; }
        public DateTime DateSubmitted { get; set; }
        public int AcceptedByID { get; set; }
        public int MinistryLUID { get; set; }
        public int LocationLUID { get; set; }
        public int CapitalRequestId { get; set; }
        public int AssignedToPersonId { get; set; }

        [XmlIgnore]
        public List<Approval> Approvals
        {
            get
            {
                if (mApprovals == null)
                    mApprovals = LoadApprovals();
                return mApprovals;
            }
            set
            {
                mApprovals = value;
            }
        }

        [XmlIgnore]
        public List<RequisitionItem> Items
        {
            get
            {
                if (mItems == null)
                    mItems = LoadItems();
                return mItems;
            }
            set
            {
                mItems = value;
            }
        }

        [XmlIgnore]
        public DefinedValue RequisitionType
        {
            get
            {
                if ((mRequisitionType == null || mRequisitionType.Id != RequisitionTypeLUID) && RequisitionTypeLUID > 0)
                    mRequisitionType = definedValueService.Get(RequisitionTypeLUID);

                return mRequisitionType;
            }
            set
            {
                if (value == null)
                    RequisitionTypeLUID = 0;
                else
                    RequisitionTypeLUID = value.Id;
                mRequisitionType = value;
            }
        }

        [XmlIgnore]
        public DefinedValue Status
        {
            get
            {
                if((mState == null || mState.Id != StatusLUID) && StatusLUID > 0)
                {
                    mState = definedValueService.Get(StatusLUID);
                }

                return mState;
            }
            set
            {
                if (value == null)
                    StatusLUID = 0;
                else
                    StatusLUID = value.Id;
                mState = value;
            }
        }

        [XmlIgnore]
        public Person CreatedBy
        {
            get
            {
                if (mCreatedBy == null && !String.IsNullOrEmpty(CreatedByUserID))
                    mCreatedBy = userLoginService.GetByUserName(CreatedByUserID).Person;

                return mCreatedBy;
            }
        }

        [XmlIgnore]
        public Person ModifiedBy
        {
            get
            {
                if (mModifiedBy == null && !String.IsNullOrEmpty(ModifiedByUserID))
                    mModifiedBy = userLoginService.GetByUserName(ModifiedByUserID).Person;
                return mModifiedBy;
            }
        }

        [XmlIgnore]
        public Person Requester
        {
            get
            {
                if (mRequester == null && RequesterID > 0)
                    mRequester = personAliasService.Get(RequesterID).Person;

                return mRequester;
            }
        }

        [XmlIgnore]
        public Person AcceptedBy
        {
            get
            {
                if (mAcceptedBy == null && AcceptedByID > 0)
                    mAcceptedBy = personAliasService.Get(AcceptedByID).Person;
                return mAcceptedBy;
            }
        }

        [XmlIgnore]
        public List<Note> Notes
        {
            get
            {
                if (mNotes == null && RequisitionID > 0)
                {
                    mNotes = LoadNotes();
                }
                return mNotes;
            }
            set
            {
                mNotes = value;
            }
        }

        [XmlIgnore]
        public List<Attachment> Attachments
        {
            get
            {
                if (mAttachments == null && RequisitionID > 0)
                    mAttachments = LoadAttachments();

                return mAttachments;
            }
            set
            {
                mAttachments = value;
            }
        }

        [XmlIgnore]
        public List<PaymentCharge> Charges
        {
            get
            {
                if (mCharges == null && RequisitionID > 0)
                    mCharges = LoadCharges();
                return mCharges;
            }
            set
            {
                mCharges = value;
            }
        }

        [XmlIgnore]
        public DefinedValue Ministry
        {
            get
            {
                if ((mMinistry == null || mMinistry.Id != MinistryLUID) && MinistryLUID > 0)
                {
                    mMinistry = definedValueService.Get(MinistryLUID);
                }

                return mMinistry;
            }
            set
            {
                if (value == null)
                    MinistryLUID = 0;
                else
                    MinistryLUID = value.Id;
                mMinistry = value;
            }
        }

        [XmlIgnore]
        public DefinedValue Location
        {
            get
            {
                if (mLocation == null || mLocation.Id != LocationLUID && LocationLUID > 0)
                {
                    mLocation = definedValueService.Get( LocationLUID );
                }

                return mLocation;
            }
            set
            {
                if ( value == null )
                {
                    LocationLUID = 0;
                }
                else
                {
                    LocationLUID = value.Id;
                }

                mLocation = value;
            }
        }

        [XmlIgnore]
        public CapitalRequest CapitalRequest
        {
            get
            {
                if ( ( mCapitalRequest == null && CapitalRequestId > 0 ) || ( mCapitalRequest != null && mCapitalRequest.CapitalRequestId != CapitalRequestId ) )
                {
                    mCapitalRequest = new CapitalRequest( CapitalRequestId );
                }

                return mCapitalRequest;
            }
        }

        public Person AssignedTo
        {
            get
            {
                if ( ( mAssignedTo == null && AssignedToPersonId > 0 ) || ( mAssignedTo != null && mAssignedTo.Id != AssignedToPersonId ) )
                {
                    mAssignedTo = userLoginService.Get(AssignedToPersonId).Person;

                    //person was merged since the assignment was set
                    if ( mAssignedTo.Id != AssignedToPersonId )
                    {
                        AssignedToPersonId = mAssignedTo.Id;
                    }
                }

                return mAssignedTo;
            }
        }
        #endregion

        #region constructors
        public Requisition()
        {
            Init();
        }

        public Requisition(int rID)
        {
            Load(rID);
        }

        public Requisition(RequisitionData data)
        {
            Load(data);
        }

        public static List<DefinedValue> GetStatuses(bool isActive)
        {
            List<DefinedValue> StatusList = definedTypeService.Get(StatusTypeGUID).DefinedValues.OrderBy(x => x.Order).ToList();

            if(isActive)
                StatusList.RemoveAll(x => x.IsValid == false);

            return StatusList;

        }

        public static List<DefinedValue> GetRequisitionTypes(bool isActive)
        {
            List<DefinedValue> RequisitionTypes = definedTypeService.Get(RequisitionTypeGUID).DefinedValues.OrderBy(x => x.Order).ToList();

            if (isActive)
                RequisitionTypes.RemoveAll(x => x.IsValid == false);

            return RequisitionTypes;
        }

        #endregion

        #region Public

        public static List<Requisition> LoadOpen()
        {
            using ( PurchasingContext Context = ContextHelper.GetDBContext() )
            {
                return Context.RequisitionDatas
                        .Where( r => r.is_open )
                        .Select( r => new Requisition( r ) )
                        .ToList();
            }
        }

        public static List<Requisition> LoadAll()
        {
            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                return Context.RequisitionDatas.Select( r => new Requisition( r ) ).ToList();
            }
        }

        public static List<Requisition> LoadByCreator(string uid, bool activeOnly)
        {
            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                var reqQuery = Context.RequisitionDatas
                                .Where( r => r.created_by == uid );

                if ( activeOnly )
                {
                    reqQuery = reqQuery.Where( r => r.active );
                }

                return reqQuery.Select( r => new Requisition( r ) ).ToList();
            }
        }

        public static List<RequisitionListItem> GetRequisitionList(Dictionary<string, string> filter)
        {
            List<RequisitionListItem> ListItems = new List<RequisitionListItem>();
            bool ShowAll = false;
            bool ShowMinistry = false;
            bool ShowApprover = false;
            bool ShowMe = false;
            int PersonID = 0;
            int PONumber = 0;
            int MinistryLUID = 0;
            int LocationLUID = 0;

            if (filter == null || filter.Count == 0)
                return ListItems;

            if (filter.ContainsKey("Show_Me"))
                bool.TryParse(filter["Show_Me"], out ShowMe);
            if (filter.ContainsKey("Show_Ministry"))
                bool.TryParse(filter["Show_Ministry"], out ShowMinistry);
            if (filter.ContainsKey("Show_Approver"))
                bool.TryParse(filter["Show_Approver"], out ShowApprover);
            if(filter.ContainsKey("Show_All"))
                bool.TryParse(filter["Show_All"], out ShowAll);

            if (filter.ContainsKey("MinistryLUID"))
                int.TryParse(filter["MinistryLUID"], out MinistryLUID);

            if ( filter.ContainsKey( "LocationLUID" ) )
                int.TryParse( filter["LocationLUID"], out LocationLUID );

            if (filter.ContainsKey("PONumber"))
                int.TryParse(filter["PONumber"], out PONumber);

            if (filter.ContainsKey("PersonID"))
                int.TryParse(filter["PersonID"], out PersonID);

            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                var Query = Context.RequisitionDatas
                .Join(
                        Context.LookupDatas,
                        req => req.status_luid,
                        statusLU => statusLU.Id,
                        ( req, statusLU ) => new
                        {
                            requisition = req,
                            requisitionStatus = statusLU
                        }
                    )
                .Join(
                        Context.LookupDatas,
                        req2 => req2.requisition.requisition_type_luid,
                        reqType => reqType.Id,
                        ( req2, reqType ) => new
                        {
                            requisition = req2.requisition,
                            requisitionStatus = req2.requisitionStatus,
                            requisitionType = reqType
                        }
                    )
                .GroupJoin(
                        Context.PersonAliasDatas,
                        req => req.requisition.requester_id,
                        alias => alias.Id,
                        ( req, alias ) => new
                        {
                            requisition = req.requisition,
                            requisitionStatus = req.requisitionStatus,
                            requisitionType = req.requisitionType,
                            requesterMerged = alias
                        }
                    )
                .SelectMany(
                        joinedReq => joinedReq.requesterMerged.DefaultIfEmpty(),
                        ( joinedReq, alias ) => new
                        {
                            requisition = joinedReq.requisition,
                            requisitionStatus = joinedReq.requisitionStatus,
                            requisitionType = joinedReq.requisitionType,
                            requester = alias
                        }
                    )
                .GroupJoin(
                        Context.PersonAliasDatas,
                        joinedReq => joinedReq.requisition.assigned_to_person_id,
                        merged => merged.Id,
                        ( joinedReq, merged ) => new
                        {
                            requisition = joinedReq.requisition,
                            requisitionStatus = joinedReq.requisitionStatus,
                            requisitionType = joinedReq.requisitionType,
                            requester = joinedReq.requester,
                            assignee = merged
                        }
                    )
                .SelectMany(
                        joinedReq => joinedReq.assignee.DefaultIfEmpty(),
                        ( joinedReq, merged ) => new
                        {
                            requisition = joinedReq.requisition,
                            requisitionStatus = joinedReq.requisitionStatus,
                            requisitionType = joinedReq.requisitionType,
                            requester = joinedReq.requester,
                            assignee = merged
                        }
                    )
                .Select(
                    joinedReq => new
                    {
                        RequisitionId = joinedReq.requisition.requisition_id,
                        RequesterId = joinedReq.requester.Id,
                        RequesterLastFirst = string.Format( "{0}, {1}", joinedReq.requester.PersonData.LastName, joinedReq.requester.PersonData.NickName ),
                        Title = joinedReq.requisition.title,
                        DateSubmitted = joinedReq.requisition.date_submitted,
                        IsApproved = joinedReq.requisition.is_approved,
                        IsAccepted = joinedReq.requisition.date_accepted != null,
                        StatusLUID = joinedReq.requisition.status_luid,
                        Status = joinedReq.requisitionStatus.Value,
                        TypeLUID = joinedReq.requisition.requisition_type_luid,
                        TypeName = joinedReq.requisitionType.Value,
                        CreatedBy = joinedReq.requisition.created_by,
                        Active = joinedReq.requisition.active,
                        MinistryLUID = joinedReq.requisition.ministry_luid,
                        LocationLUID = joinedReq.requisition.location_luid,
                        ItemCount = joinedReq.requisition.RequisitionItemDatas
                                    .Where( ri => ri.active )
                                    .Count(),
                        NoteCount = Context.NoteDatas
                                    .Where( reqNote => reqNote.object_type_name == typeof( Requisition ).ToString() )
                                    .Where( reqNote => reqNote.identifier == joinedReq.requisition.requisition_id )
                                    .Where( reqNote => reqNote.active )
                                    .Count(),
                        AttachmentCount = Context.AttachmentDatas
                                    .Where( attach => attach.parent_object_type_name == typeof( Requisition ).ToString() )
                                    .Where( attach => attach.parent_identifier == joinedReq.requisition.requisition_id )
                                    .Where( attach => attach.active )
                                    .Count(),
                        AssignedToPersonId = joinedReq.assignee == null ? (Int32?)null : (Int32?)joinedReq.assignee.Id,
                        //AssignedToLastFirst = joinedReq.assignee == null ? null : string.Format( "{0}, {1}", joinedReq.assignee.last_name, joinedReq.assignee.nick_name ),
                        ApproverPersonIds = Context.ApprovalDatas
                                            .Where( approval => approval.object_type_name == typeof( Requisition ).ToString() )
                                            .Where( approval => approval.identifier == joinedReq.requisition.requisition_id )
                                            .Where( approval => approval.active )
                                            .GroupJoin(
                                                    Context.PersonAliasDatas,
                                                    approval => approval.approver_id,
                                                    merged => merged.Id,
                                                    ( approval, merged ) => new
                                                    {
                                                        approval = approval,
                                                        merged = merged
                                                    }
                                            )
                                            .SelectMany(
                                                    approvalJoined => approvalJoined.merged.DefaultIfEmpty(),
                                                    ( approvalJoined, merged ) =>
                                                        new
                                                        {
                                                            ApproverId = merged == null ? approvalJoined.approval.approver_id : merged.Id
                                                        }
                            ),
                        ReqPOIDs = joinedReq.requisition.RequisitionItemDatas
                                    .Where( reqItems => reqItems.active )
                                    .Select( reqItems => reqItems.PurchaseOrderItemDatas
                                                        .Where( poi => poi.active )
                                                        .Where( poi => poi.PurchaseOrderData.active )
                                                        .Where( poi => poi.PurchaseOrderData.status_luid != PurchaseOrder.PurchaseOrderStatusCancelledLUID() )
                                                        .Select( poi => poi.PurchaseOrderData.purchase_order_id )
                                            )
                                    .SelectMany( poNum => poNum )
                                    .Distinct()
                    } );



                int[] StatusLUIDs = filter["StatusLUID"].Split(",".ToCharArray()).Select(s => int.Parse(s)).ToArray();

                Query = Query.Where(r => StatusLUIDs.Contains(r.StatusLUID));

                int[] RequisitionTypeLUIDs = filter["TypeLUID"].Split(",".ToCharArray()).Select(t => int.Parse(t)).ToArray();
                Query = Query.Where(r => RequisitionTypeLUIDs.Contains(r.TypeLUID));

                DateTime SubmitOnStart;
                if (filter.ContainsKey("SubmitOnStart") && DateTime.TryParse(filter["SubmitOnStart"], out SubmitOnStart))
                    Query = Query.Where(r => r.DateSubmitted != null && ((DateTime)r.DateSubmitted).Date >= SubmitOnStart);

                DateTime SubmitOnEnd;
                if (filter.ContainsKey("SubmitOnEnd") && DateTime.TryParse(filter["SubmitOnEnd"], out SubmitOnEnd))
                    Query = Query.Where(r => r.DateSubmitted != null && ((DateTime)r.DateSubmitted).Date <= SubmitOnEnd);

                int RequesterID = 0;
                if (filter.ContainsKey("RequesterID") && int.TryParse(filter["RequesterID"], out RequesterID))
                    Query = Query.Where(r => r.RequesterId == RequesterID);

                if (PONumber > 0)
                    Query = Query.Where(r => r.ReqPOIDs.Contains(PONumber));

                if (MinistryLUID > 0)
                    Query = Query.Where(r => r.MinistryLUID == MinistryLUID);

                if ( LocationLUID > 0 )
                    Query = Query.Where( r => r.LocationLUID == LocationLUID );

                bool ShowInactive = false;
                if (filter.ContainsKey("ShowInactive"))
                    bool.TryParse(filter["ShowInactive"], out ShowInactive);

                if (!ShowInactive)
                    Query = Query.Where(r => r.Active);
                
                if (ShowAll)
                {
                    ListItems.AddRange(Query.Select(q => new RequisitionListItem{
                            RequisitionID = q.RequisitionId,
                            Title = q.Title,
                            RequesterID = q.RequesterId,
                            RequesterLastFirst = q.RequesterLastFirst,
                            Status = q.Status,
                            RequisitionType = q.TypeName,
                            ItemCount = q.ItemCount,
                            NoteCount = q.NoteCount,
                            AttachmentCount = q.AttachmentCount,
                            DateSubmitted = q.DateSubmitted,
                            IsApproved = q.IsApproved,
                            IsAccepted = q.IsAccepted
                        }));
                }
                else
                {
                    if (ShowMe)
                    {
                        if (PersonID > 0 && filter.ContainsKey("UserName"))
                        {
                            var range = Query.Where(q => filter["UserName"].IndexOf(q.CreatedBy) >= 0)
                                                        .Select(q => new RequisitionListItem
                                                        {
                                                            RequisitionID = q.RequisitionId,
                                                            Title = q.Title,
                                                            RequesterID = q.RequesterId,
                                                            RequesterLastFirst = q.RequesterLastFirst,
                                                            Status = q.Status,
                                                            RequisitionType = q.TypeName,
                                                            ItemCount = q.ItemCount,
                                                            NoteCount = q.NoteCount,
                                                            AttachmentCount = q.AttachmentCount,
                                                            DateSubmitted = q.DateSubmitted,
                                                            IsApproved = q.IsApproved,
                                                            IsAccepted = q.IsAccepted
                                                        });
                            ListItems.AddRange( range );

                            ListItems.AddRange( Query.Where( q => q.RequesterId == PersonID )
                            .Select( q => new RequisitionListItem
                            {
                                RequisitionID = q.RequisitionId,
                                Title = q.Title,
                                RequesterID = q.RequesterId,
                                RequesterLastFirst = q.RequesterLastFirst,
                                Status = q.Status,
                                RequisitionType = q.TypeName,
                                ItemCount = q.ItemCount,
                                NoteCount = q.NoteCount,
                                AttachmentCount = q.AttachmentCount,
                                DateSubmitted = q.DateSubmitted,
                                IsApproved = q.IsApproved,
                                IsAccepted = q.IsAccepted
                            } ) );
                        }
                    }
                    if (ShowMinistry)
                    {
                        int MyMinistryID = 0;
                        int MyLocationID = 0;

                        var query2 = Query;

                        if ( filter.ContainsKey( "MyMinistryID" ) && int.TryParse( filter["MyMinistryID"], out MyMinistryID ) )
                        {
                            query2 = query2.Where( q => q.MinistryLUID == MyMinistryID );
                            
                            if (filter.ContainsKey("MyLocationID") && int.TryParse(filter["MyLocationID"], out MyLocationID))
                            {
                                query2 = query2.Where( q => q.LocationLUID == MyLocationID );
                            }

                            ListItems.AddRange(query2.Where( r => !ListItems.Select( li => li.RequisitionID ).Contains( r.RequisitionId ) )
                                                         .Select( q => new RequisitionListItem
                                                         {
                                                             RequisitionID = q.RequisitionId,
                                                             Title = q.Title,
                                                             RequesterID = q.RequesterId,
                                                             RequesterLastFirst = q.RequesterLastFirst,
                                                             Status = q.Status,
                                                             RequisitionType = q.TypeName,
                                                             ItemCount = q.ItemCount,
                                                             NoteCount = q.NoteCount,
                                                             AttachmentCount = q.AttachmentCount,
                                                             DateSubmitted = q.DateSubmitted,
                                                             IsApproved = q.IsApproved,
                                                             IsAccepted = q.IsAccepted
                                                         } ) );
                        }

                    }
                    if (ShowApprover)
                    {
                        if (PersonID > 0)
                        {
                            ListItems.AddRange(Query.Where(q => q.ApproverPersonIds.Where(ra => ra.ApproverId == PersonID).Count() > 0)
                                                .Select(q => new RequisitionListItem
                                                         {
                                                             RequisitionID = q.RequisitionId,
                                                             Title = q.Title,
                                                             RequesterID = q.RequesterId,
                                                             RequesterLastFirst = q.RequesterLastFirst,
                                                             Status = q.Status,
                                                             RequisitionType = q.TypeName,
                                                             ItemCount = q.ItemCount,
                                                             NoteCount = q.NoteCount,
                                                             AttachmentCount = q.AttachmentCount,
                                                             DateSubmitted = q.DateSubmitted,
                                                             IsApproved = q.IsApproved,
                                                             IsAccepted = q.IsAccepted
                                                         }));

                        }
                            
                                
                    }
                }

            }

            return ListItems.Distinct(new RequisitionListItemDistinctItemComparer()).ToList <RequisitionListItem>();
        }

        public List<RequisitionChargeSummary> GetChargeSummary()
        {
            using(PurchasingContext context = ContextHelper.GetDBContext())
            {
                return context.PaymentChargeDatas
                    .Where( pc => pc.active )
                    .Where( pc => pc.PaymentData.active )
                    .Where( pc => pc.PaymentData.PurchaseOrderData.active )
                    .Where( pc => pc.PaymentData.PurchaseOrderData.status_luid != PurchaseOrder.PurchaseOrderStatusCancelledLUID() )
                    .Where(pc => pc.requisition_id == this.RequisitionID)
                    .Select( pc => new RequisitionChargeSummary
                        {
                            PaymentChargeId = pc.charge_id,
                            PaymentDate = pc.PaymentData.payment_date,
                            PaymentMethodId = pc.PaymentData.payment_method_id,
                            PaymentMethodName = pc.PaymentData.PaymentMethodData.name,
                            PurchaseOrderId = pc.PaymentData.purchase_order_id,
                            Account = string.Format( "{0}-{1}-{2}", pc.fund_id, pc.department_id, pc.account_id ),
                            ChargeAmount = pc.amount,
                            VendorId = pc.PaymentData.PurchaseOrderData.vendor_id,
                            VendorName = pc.PaymentData.PurchaseOrderData.VendorData.vendor_name
                        } ).ToList();


            }


        }

        public static List<Requisition> LoadByRequester(int requesterID, bool activeOnly)
        {
            using ( PurchasingContext Context = ContextHelper.GetDBContext() )
            {
                var reqQuery = Context.RequisitionDatas.Where( r => r.requester_id == requesterID );

                if ( activeOnly )
                {
                    reqQuery = reqQuery.Where( r => r.active );
                }

                return reqQuery.Select( r => new Requisition( r ) ).ToList();
            }
        }

        public static List<Requisition> LoadByMinistry(int ministryLUID, int ministryLookupTypeID, bool activeOnly)
        {
            var MinistryPersonIDs = Helpers.Person.GetPersonIDByAttributeValue(ministryLookupTypeID, ministryLUID);

            using ( PurchasingContext Context = ContextHelper.GetDBContext() )
            {
                var reqQuery = Context.RequisitionDatas.Where( x => MinistryPersonIDs.Contains( x.requester_id ) );

                if ( activeOnly )
                {
                    reqQuery = reqQuery.Where( x => x.active );
                }

                return reqQuery.Select( r => new Requisition( r ) ).ToList();
            }
        }

        public static List<Requisition> LoadByStatus(int[] statusLUIDs)
        {

            using ( PurchasingContext Context = ContextHelper.GetDBContext() )
            {
                return Context.RequisitionDatas
                        .Where( r => statusLUIDs.Contains( r.status_luid ) )
                        .Where( r => r.active )
                        .Select( r => new Requisition( r ) )
                        .ToList();
            }
        }

        public static List<Requisition> LoadByVendor(int vendorId)
        {

            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                return Context.RequisitionDatas
                        .Where(r => r.pref_vendor_id == vendorId)
                        .Select(r => new Requisition(r))
                        .ToList();
            }
        }

        public static List<RequistionItemsWithUnassignedRequisitionItems> LoadAcceptedRequisitonsWithItemsNotOnPO(int ministryAttributeID)
        {
            List<RequistionItemsWithUnassignedRequisitionItems> Reqs = new List<RequistionItemsWithUnassignedRequisitionItems>();
            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                Reqs.AddRange(Context.RequisitionDatas
                                .GroupJoin(Context.PersonAliasDatas,
                                    r => r.requester_id,
                                    p => p.Id,
                                    (r, p1) => new {
                                        r = r,
                                        p1 = p1
                                    })
                                .SelectMany(temp0 => temp0.p1.DefaultIfEmpty(),
                                    (temp0, LOJp) => new {
                                        temp0 = temp0,
                                        LOJp = LOJp
                                    })
                                .Where( temp1 =>
                                    (((temp1.temp0.r.date_accepted != (DateTime?)null) && temp1.temp0.r.active) &&
                                        temp1.temp0.r.status_luid != Requisition.CancelledLUID() &&
                                        (((Int32?)(temp1.temp0.r.RequisitionItemDatas.Where(ri => ri.active)
                                            .Select(ri => ri.quantity).Sum()) ?? 0) >
                                        ((Int32?)(temp1.temp0.r.RequisitionItemDatas.Where(ri => ri.active)
                                            .Select(ri =>
                                                ((Int32?)(ri.PurchaseOrderItemDatas
                                                    .Where(poi => (poi.active && poi.PurchaseOrderData.active && poi.PurchaseOrderData.status_luid != PurchaseOrder.PurchaseOrderStatusCancelledLUID()))
                                                    .Select(poi => poi.quantity).Sum()) ?? 0)).Sum() ) ?? 0))))
                                .Select(temp1 => new RequistionItemsWithUnassignedRequisitionItems{
                                    RequisitionID = temp1.temp0.r.requisition_id,
                                    Title = temp1.temp0.r.title,
                                    RequesterID = temp1.temp0.r.requester_id,
                                    DateSubmitted = temp1.temp0.r.date_submitted,
                                    IsApproved =  temp1.temp0.r.is_approved,
                                    RequesterName = ((temp1.LOJp.PersonData.NickName + " ") + temp1.LOJp.PersonData.LastName),
                                    RequesterLastFirst = ((temp1.LOJp.PersonData.LastName + ",") + temp1.LOJp.PersonData.NickName)
                                }));

                foreach (var item in Reqs.Select(r => r.RequesterID).Distinct())
            	{
                    var attribute = Rock.Web.Cache.AttributeCache.Read( ministryAttributeID );
                    var Ministry = Helpers.Person.GetMyMinistryLookup(item, attribute.Key);
                    foreach (var reqItem in Reqs.Where(r => r.RequesterID == item))
                    {
                        if (Ministry.Id > 0)
                        {
                            reqItem.MinistryID = Ministry.Id;
                            reqItem.MinistryName = Ministry.Value;
                        }
                    }
	            }
            }


            return Reqs;
        }

        
        public static int DraftLUID()
        {
            return definedValueService.Get(DraftStatusGuid).Id;
        }

        public static int SubmittedToPurchasingLUID()
        {
            return definedValueService.Get(SubmittedToPurchasingGuid).Id;
        }

        public static int PendingApprovalLUID()
        {
            return definedValueService.Get(PendingApprovalStatusGuid).Id;
        }

        public static int ApprovedLUID()
        {
            return definedValueService.Get(ApprovedStatusGuid).Id;
        }

        public static int ReturnedToRequesterLUID()
        {
            return definedValueService.Get(ReturnedToRequesterStatusGuid).Id;
        }

        public static int AcceptedByPurchasingLUID()
        {
            return definedValueService.Get(AcceptedByPurchasingGuid).Id;
        }

        public static int OrderedByPurchasingLUID()
        {
            return definedValueService.Get(OrderedByPurchasingGuid).Id;
        }

        public static int PartiallyReceivedLUID()
        {
            return definedValueService.Get(PartiallyReceivedGuid).Id;
        }

        public static int PartiallyOrderedLUID()
        {
            return definedValueService.Get(PartiallyOrderedGuid).Id;
        }

        public static int ReceievedLUID()
        {
            return definedValueService.Get(ReceivedGuid).Id;
        }

        public static int BilledLUID()
        {
            return definedValueService.Get(BilledGuid).Id;
        }

        public static int ReopenedLUID()
        {
            return definedValueService.Get(ReopenedGuid).Id;
        }

        public static int ClosedLUID()
        {
            return definedValueService.Get(ClosedGuid).Id;
        }

        public static int CancelledLUID()
        {
            return definedValueService.Get(CancelledGuid).Id;
        }

        public static int PurchaseReqTypeLUID()
        {
            return definedValueService.Get(PurchaseReqTypeGuid).Id;
        }

        public static int BidRequestReqTypeLUID()
        {
            return definedValueService.Get(BidRequestReqTypeGuid).Id;
        }

        public static int BlanketReqTypeLUID()
        {
            return definedValueService.Get(BlanketReqTypeGuid).Id;
        }

        public static int CapitalReqTypeLUID()
        {
            return definedValueService.Get(CapitalReqTypeGuid).Id;
        }

        public static int PORequestReqTypeLUID()
        {
            return definedValueService.Get(PORequestReqTypeGuid).Id;
        }

        public static int TravelReqTypeLUID()
        {
            return definedValueService.Get(TravelReqTypeGuid).Id;
        }

        public void Cancel(string uid)
        {
            StatusLUID = CancelledLUID();
            IsOpen = false;
            Save(uid);

        }

        public List<RequisitionItemListItem> GetListItems()
        {
            using (PurchasingContext context = ContextHelper.GetDBContext())
            {
                return context.RequisitionItemDatas.Where(ri => ri.requisition_id == RequisitionID)
                    .Select(ri => new RequisitionItemListItem()
                        {
                            ItemID = ri.requisition_item_id,
                            Quantity = ri.quantity,
                            QuantityReceived = (int?)ri.PurchaseOrderItemDatas.Where(poi => poi.active)
                                                .Where(poi => poi.PurchaseOrderData.active)
                                                .Where(poi => poi.PurchaseOrderData.status_luid != PurchaseOrder.PurchaseOrderStatusCancelledLUID() )
                                                .Select(poi => (int?)poi.ReceiptItemDatas.Where(rid => rid.active)
                                                    .Select(rid => (int?)rid.quantity_received ?? 0)
                                                    .Sum() ?? 0)
                                                .Sum() ?? 0,
                            ItemNumber = ri.item_number,
                            Description = ri.description,
                            DateNeeded = ri.date_needed,
                            ExpeditedShipping = ri.is_expedited_shipping_allowed,
                            AccountNumber = string.Format("{0}-{1}-{2}", ri.fund_id, ri.department_id, ri.account_id),
                            PONumbers = ri.PurchaseOrderItemDatas.Where(poi => poi.active && poi.PurchaseOrderData.active)
                                .Where(poi => poi.PurchaseOrderData.active && poi.PurchaseOrderData.status_luid != PurchaseOrder.PurchaseOrderStatusCancelledLUID() )
                                .Select(poi => poi.PurchaseOrderData.purchase_order_id).Distinct().ToList(),
                            EstimatedCost = ri.price,
                            LineItemCost = ri.price == null ? null : ri.price * ri.quantity,
                            Active = ri.active
                        }).ToList();
            }       
        }

        public int GetPOCount()
        {
            int poCount = 0;
            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                poCount = (
                            from r in Context.RequisitionDatas
                            join ri in Context.RequisitionItemDatas on r.requisition_id equals ri.requisition_id
                            join poi in Context.PurchaseOrderItemDatas on ri.requisition_item_id equals poi.requisition_item_id
                            join po in Context.PurchaseOrderDatas on poi.purchase_order_id equals po.purchase_order_id
                            where r.requisition_id == RequisitionID && ri.active && poi.active && po.active && po.status_luid != PurchaseOrder.PurchaseOrderStatusCancelledLUID()
                            select po.purchase_order_id
                          ).Distinct().Count();
            }

            return poCount;

        }

        public int GetPOCount(int statusLUID)
        {
            int poCount = 0;
            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                poCount = (
                            from r in Context.RequisitionDatas
                            join ri in Context.RequisitionItemDatas on r.requisition_id equals ri.requisition_id
                            join poi in Context.PurchaseOrderItemDatas on ri.requisition_item_id equals poi.requisition_item_id
                            join po in Context.PurchaseOrderDatas on poi.purchase_order_id equals po.purchase_order_id
                            where r.requisition_id== RequisitionID && ri.active && poi.active && po.status_luid == statusLUID
                            select po.purchase_order_id
                          ).Distinct().Count();

            }

            return poCount;
        }


        public void RefreshAttachments()
        {
            Attachments = null;
            Attachments = LoadAttachments();
        }

        public void RefreshCharges()
        {
            Charges = null;
            Charges = LoadCharges();
        }

        public void RefreshItems()
        {
            Items = null;
            Items = LoadItems();
        }

        public void RefreshApprovals()
        {
            Approvals = null;
            Approvals = LoadApprovals();
        }

        public void RefreshNotes()
        {
            Notes = null;
            Notes = LoadNotes();
        }

        public void Save(string uid)
        {
            try
            {
                Dictionary<string, string> ValErrors = Validate();
                if (ValErrors.Count > 0)
                    throw new RequisitionNotValidException("Requisition is not valid.", ValErrors);

                using (PurchasingContext Context = ContextHelper.GetDBContext())
                {
                    Enums.HistoryType ChangeType;
                    Requisition Original = null;

                    RequisitionData data = null;
                    if (RequisitionID > 0)
                    {
                        ChangeType = Enums.HistoryType.UPDATE;
                        data = Context.RequisitionDatas.Where(x => x.requisition_id == RequisitionID).FirstOrDefault();
                        Original = new Requisition(data);
                    }
                    else
                    {
                        ChangeType = Enums.HistoryType.ADD;
                        data = new RequisitionData();
                        data.created_by = uid;
                        data.date_created = DateTime.Now;
                    }

                    data.title = Title;
                    data.requisition_type_luid = RequisitionTypeLUID;
                    data.requester_id = RequesterID;
                    data.status_luid = StatusLUID;
                    data.deliver_to = DeliverTo;
                    data.is_approved = IsApproved;
                    data.is_open = IsOpen;

                    if (PreferredVendor != null)
                    {
                        if (PreferredVendor.VendorID > 0)
                            data.pref_vendor_id = PreferredVendor.VendorID;
                        else
                        {
                            data.pref_vendor_name = PreferredVendor.Name;
                            if (PreferredVendor.Address != null && PreferredVendor.Address.IsValid())
                                data.pref_vendor_address = PreferredVendor.Address.ToArenaFormat();
                            if (PreferredVendor.Phone != null && PreferredVendor.Phone.IsValid())
                                data.pref_vendor_phone = PreferredVendor.Phone.ToArenaFormat();
                            if (!String.IsNullOrEmpty(PreferredVendor.WebAddress))
                                data.pref_vendor_webaddress = PreferredVendor.WebAddress;
                        }
                    }
                    else
                    {
                        data.pref_vendor_id = null;
                        data.pref_vendor_name = null;
                        data.pref_vendor_address = null;
                        data.pref_vendor_phone = null;
                        data.pref_vendor_webaddress = null;
                    }

                    if (DateAccepted != DateTime.MinValue)
                        data.date_accepted = DateAccepted;
                    else
                        data.date_accepted = null;

                    if (DatePurchased != DateTime.MinValue)
                        data.date_purchased = DatePurchased;
                    else
                        data.date_purchased = null;

                    data.date_modified = DateTime.Now;
                    data.modified_by = uid;
                    data.organization_id = OrganizationID;
                    data.active = Active;

                    if (DateSubmitted != DateTime.MinValue)
                        data.date_submitted = DateSubmitted;
                    else
                        data.date_submitted = null;

                    if (MinistryLUID > 0)
                    {
                        data.ministry_luid = MinistryLUID;
                    }
                    else
                    {
                        data.ministry_luid = null;
                    }

                    if ( LocationLUID > 0 )
                    {
                        data.location_luid = LocationLUID;
                    }
                    else
                    {
                        data.location_luid = null;
                    }

                    if ( AcceptedByID > 0 )
                    {
                        data.accepted_by_personid = AcceptedByID;
                    }
                    else
                    {
                        data.accepted_by_personid = null;
                    }

                    if ( CapitalRequestId > 0 )
                    {
                        data.capital_request_id = CapitalRequestId;
                    }
                    else
                    {
                        data.capital_request_id = null;
                    }

                    if (RequisitionID == 0)
                    {
                        Context.RequisitionDatas.InsertOnSubmit(data);
                    }

                    Context.SubmitChanges();
                    Load(data);
                    SaveHistory(ChangeType, Original, uid);

                }
            }
            catch (Exception ex)
            {
                throw new RequisitionException("An error has occurred while saving requisition", ex);
            }
        }

        public void SaveItem(RequisitionItem item, string uid, bool refresh)
        {
            if (item.RequisitionID <= 0)
                item.RequisitionID = RequisitionID;
            else if (item.RequisitionID != RequisitionID)
                throw new RequisitionException("Item is not associated with current requisition.");

            item.Save(uid);

            if (refresh)
                RefreshItems();
        }

        public int GetItemCount()
        {
            int count = 0;

            foreach (var i in Items.Where(x => x.Active))
            {
                count += i.Quantity;
            }

            return count;
        }

        public int GetItemDetailCount(bool activeOnly)
        {
            int DetailCount = 0;
            if (activeOnly)
                DetailCount = Items.Count(x => x.Active);
            else
                DetailCount = Items.Count();

            return DetailCount;
        }

        public bool HasChanged()
        {
            if (RequisitionID == 0)
                return true;
            
            bool HasChanged = false;
            Requisition Original = new Requisition(RequisitionID);

            if (!HasChanged && Original.Title != Title)
                HasChanged = true;

            if (!HasChanged && Original.RequisitionTypeLUID != RequisitionTypeLUID)
                HasChanged = true;

            if (!HasChanged && Original.RequesterID != RequesterID)
                HasChanged = true;

            if (!HasChanged && Original.StatusLUID != StatusLUID)
                HasChanged = true;

            if (!HasChanged && Original.DeliverTo != DeliverTo)
                HasChanged = true;

            if (!HasChanged && Original.IsApproved != IsApproved)
                HasChanged = true;

            if (!HasChanged && Original.IsOpen != IsOpen)
                HasChanged = true;

            if (!HasChanged && Original.DateAccepted != DateAccepted)
                HasChanged = true;

            if (!HasChanged && Original.DatePurchased != DatePurchased)
                HasChanged = true;

            if (!HasChanged && Original.DateSubmitted != DateSubmitted)
                HasChanged = true;

            if (!HasChanged && Original.AcceptedByID != AcceptedByID)
                HasChanged = true;

            if (!HasChanged && PreferredVendor != null)
            {
                if (Original.PreferredVendor == null)
                    HasChanged = true;

                if (!HasChanged && Original.PreferredVendor.GetHashCode() != PreferredVendor.GetHashCode())
                {
                    if (!HasChanged && Original.PreferredVendor.VendorID != PreferredVendor.VendorID)
                        HasChanged = true;
                    if (!HasChanged && Original.PreferredVendor.Name != PreferredVendor.Name)
                        HasChanged = true;
                    if (!HasChanged && Original.PreferredVendor.WebAddress != PreferredVendor.WebAddress)
                        HasChanged = true;
                    if (!HasChanged && Original.PreferredVendor.Address == null && PreferredVendor.Address != null)
                        HasChanged = true;
                    else if (!HasChanged && Original.PreferredVendor.Address != null && PreferredVendor.Address != null)
                    {
                        if (Original.PreferredVendor.Address.ToArenaFormat() != PreferredVendor.Address.ToArenaFormat())
                            HasChanged = true;
                    }

                    if (!HasChanged && Original.PreferredVendor.Phone == null && PreferredVendor.Phone != null)
                        HasChanged = true;
                    else if (!HasChanged && Original.PreferredVendor.Phone != null && PreferredVendor.Phone != null)
                    {
                        if (Original.PreferredVendor.Phone.ToArenaFormat() != PreferredVendor.Phone.ToArenaFormat())
                            HasChanged = true;
                    }
                }
            }

            if (!HasChanged && MinistryLUID != Original.MinistryLUID)
            {
                HasChanged = true;
            }

            if ( !HasChanged && CapitalRequestId != Original.CapitalRequestId )
            {
                HasChanged = true;
            }

            if ( !HasChanged && AssignedToPersonId != Original.AssignedToPersonId )
            {
                HasChanged = true;
            }

            return HasChanged;           

        }

        public void SaveHistory(Enums.HistoryType ht, Requisition Original, string uid)
        {
            History h = new History();
            h.ObjectTypeName = this.GetType().ToString();
            h.Identifier = RequisitionID;
            h.ChangeType = ht;
            h.OrganizationID = OrganizationID;
            h.Active = true;

            switch (ht)
            {
                case org.secc.Purchasing.Enums.HistoryType.ADD:
                    h.OriginalXML = null;
                    h.UpdatedXML = base.Serialize(this);
                    break;
                case org.secc.Purchasing.Enums.HistoryType.UPDATE:
                    h.OriginalXML = base.Serialize(Original);
                    h.UpdatedXML = base.Serialize(this);
                    break;
                case org.secc.Purchasing.Enums.HistoryType.DELETE:
                    h.OriginalXML = base.Serialize(this);
                    h.UpdatedXML = null;
                    break;
            }

            h.Save(uid);
        }

        public bool CreatorIsInMyMinistry(int personID, string attribute)
        {

            if (CreatedBy != null && CreatedBy.Id > 0)
            {
                Person person = personAliasService.Get(personID).Person;
                person.LoadAttributes();
                CreatedBy.LoadAttributes();
                return person.GetAttributeValue(attribute).Equals(CreatedBy.GetAttributeValue(attribute));
            }

            return false;
        }

        public bool RequesterIsInMyMinistry(int personID, string attribute)
        {
            if (RequesterID > 0)
            {
                Person person = personAliasService.Get(personID).Person;
                person.LoadAttributes();
                Person requester = personAliasService.Get(RequesterID).Person;
                requester.LoadAttributes();
                return person.GetAttributeValue(attribute).Equals(requester.GetAttributeValue(attribute));
            }

            return false;
        }

        public bool UserIsApprover(int personID)
        {
            bool isApprover = false;
            if (Approvals != null)
            {
                isApprover = Approvals.Where(a => a.ApproverID == personID).Count() > 0;
            }

            return isApprover;
        }

        public void SyncStatus(string uid)
        {
            int TotalQty = Items.Where(x => x.Active).Select(x => x.Quantity).Sum();
            int QtyOrdered = Items.Where(i => i.Active)
                    .Select(i => (int?)i.POItems
                        .Where(poi => poi.Active && poi.PurchaseOrder.DateOrdered > DateTime.MinValue)
                        .Where(poi => poi.PurchaseOrder.Active)
                        .Where(poi => poi.PurchaseOrder.StatusLUID != PurchaseOrder.PurchaseOrderStatusCancelledLUID())
                        .Select(poi => poi.Quantity).Sum() ?? 0).Sum();

            int TempStatusLUID = 0;
            int QtyReceived = 0;
            bool IsFullyOrdered = false;
            bool IsFullyReceived = false;

            Status.LoadAttributes();
            if (DateAccepted == DateTime.MinValue || Status.GetAttributeValue("IsClosed").AsBoolean() )
                return;


            if (QtyOrdered > 0)
            {
                if (QtyOrdered < TotalQty)
                {
                    TempStatusLUID = PartiallyOrderedLUID();
                }

                if (QtyOrdered >= TotalQty)
                {
                    TempStatusLUID = OrderedByPurchasingLUID();
                    IsFullyOrdered = true;
                }
            }
            else
            {
                TempStatusLUID = AcceptedByPurchasingLUID();
            }

            if (IsFullyOrdered)
            {
                bool ItemPartiallyRecieved = false;
                foreach (RequisitionItem item in Items.Where(i => i.Active))
                {
                    int ItemQtyReceived = item.GetQuantityReceived();

                    if (!ItemPartiallyRecieved && item.Quantity > ItemQtyReceived)
                    {
                        ItemPartiallyRecieved = true;
                    }

                    QtyReceived += ItemQtyReceived;
                }

                if (ItemPartiallyRecieved && QtyReceived > 0)
                {
                    TempStatusLUID = PartiallyReceivedLUID();
                }
                else if (!ItemPartiallyRecieved)
                {
                    TempStatusLUID = ReceievedLUID();
                    IsFullyReceived = true;
                }
            }

            if (IsFullyReceived)
            {
                int POCount = GetPOCount();
                int POCountBilled = GetPOCount(PurchaseOrder.PurchaseOrderStatusBilledLUID());
                int POCountClosed = GetPOCount(PurchaseOrder.PurchaseOrderStatusClosedLUID());

                if (POCount == POCountClosed)
                {
                    TempStatusLUID = ClosedLUID();
                }
                else if (POCount == (POCountClosed + POCountBilled))
                {
                    TempStatusLUID = BilledLUID();
                }
            }

            if (TempStatusLUID > 0)
            {
                StatusLUID = TempStatusLUID;
                DefinedValue status = definedValueService.Get( TempStatusLUID );
                status.LoadAttributes();
                IsOpen = !( status.GetAttributeValue("IsClosed").AsBoolean());

            }

            if (HasChanged())
            {
                Save(uid);
            }
        }

        #endregion

        #region Private
        private void Init()
        {
            RequisitionID = 0;
            Title = String.Empty;
            RequisitionTypeLUID = 0;
            RequesterID = 0;
            StatusLUID = GetStatuses(true).FirstOrDefault().Id;
            DeliverTo = String.Empty;
            IsApproved = false;
            IsOpen = true;
            PreferredVendor = null;
            CreatedByUserID = String.Empty;
            ModifiedByUserID = String.Empty;
            DateAccepted = DateTime.MinValue;
            DatePurchased = DateTime.MinValue;
            DateCreated = DateTime.MinValue;
            DateModified = DateTime.MinValue;
            Active = true;
            OrganizationID = 1;
            DateSubmitted = DateTime.MinValue;
            AcceptedByID = 0;
            MinistryLUID = 0;
            LocationLUID = 0;
            CapitalRequestId = 0;
            AssignedToPersonId = 0;
            mCreatedBy = null;
            mModifiedBy = null;
            mRequester = null;
            mState = null;
            mRequisitionType = null;
            mAcceptedBy = null;
            mNotes = null;
            mAttachments = null;
            mItems = null;
            mApprovals = null;
            mMinistry = null;
            mLocation = null;
            mCapitalRequest = null;
            mAssignedTo = null;
        }

        private void Load(RequisitionData data)
        {
            Init();
            if (data != null)
            {
                RequisitionID = data.requisition_id;
                Title = data.title;
                RequisitionTypeLUID = data.requisition_type_luid;
                RequesterID = data.requester_id;
                StatusLUID = data.status_luid;
                DeliverTo = data.deliver_to;
                IsApproved = data.is_approved;
                IsOpen = data.is_open;

                if (data.pref_vendor_id != null && data.pref_vendor_id > 0)
                    PreferredVendor = new PreferredVendor((int)data.pref_vendor_id);
                else if (!String.IsNullOrEmpty(data.pref_vendor_name))
                {
                    PreferredVendor = new PreferredVendor();
                    PreferredVendor.Name = data.pref_vendor_name;

                    if (!String.IsNullOrEmpty(data.pref_vendor_address))
                        PreferredVendor.Address = new Helpers.Address(data.pref_vendor_address);
                    if (!String.IsNullOrEmpty(data.pref_vendor_phone))
                        PreferredVendor.Phone = new Helpers.PhoneNumber(data.pref_vendor_phone);
                    if (!String.IsNullOrEmpty(data.pref_vendor_webaddress))
                        PreferredVendor.WebAddress = data.pref_vendor_webaddress;
                }
                if (data.date_accepted != null)
                    DateAccepted = (DateTime)data.date_accepted;
                if (data.date_purchased != null)
                    DatePurchased = (DateTime)data.date_purchased;
                if (data.date_submitted != null)
                    DateSubmitted = (DateTime)data.date_submitted;
                CreatedByUserID = data.created_by;
                ModifiedByUserID = data.modified_by;
                DateCreated = data.date_created;
                DateModified = (DateTime)data.date_modified;
                Active = data.active;
                OrganizationID = data.organization_id;

                if (data.accepted_by_personid != null)
                    AcceptedByID = (int)data.accepted_by_personid;

                if (data.ministry_luid != null)
                {
                    MinistryLUID = (int)data.ministry_luid;
                }

                if ( data.location_luid != null )
                {
                    LocationLUID = (int)data.location_luid;
                }

                if ( data.capital_request_id != null )
                {
                    CapitalRequestId = (int)data.capital_request_id;
                }

                if ( data.assigned_to_person_id != null )
                {
                    AssignedToPersonId = (int)data.assigned_to_person_id;
                }

            }
        }

        private void Load(int rId)
        {
            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                Load(Context.RequisitionDatas.Where(r => r.requisition_id == rId).FirstOrDefault());
            }
        }

        private List<PaymentCharge> LoadCharges()
        {
            List<PaymentCharge> ChargeList = new List<PaymentCharge>();

            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                foreach (PaymentChargeData pcd in Context.RequisitionDatas.FirstOrDefault(x => x.requisition_id == RequisitionID).PaymentChargeDatas)
                {
                    ChargeList.Add(new PaymentCharge(pcd));
                }
            }

            return ChargeList;
        }

        private List<RequisitionItem> LoadItems()
        {
            List<RequisitionItem> Items = new List<RequisitionItem>();
            if (RequisitionID > 0)
            {
                using (PurchasingContext Context = ContextHelper.GetDBContext())
                {
                    List<RequisitionItemData> ItemData = Context.RequisitionDatas.Where(x => x.requisition_id == RequisitionID).FirstOrDefault().RequisitionItemDatas.ToList();

                    foreach (RequisitionItemData item in ItemData)
                    {
                        Items.Add(new RequisitionItem(item));
                    }
                }
            }
            return Items;
        }

        private List<Attachment> LoadAttachments()
        {
            return Attachment.GetObjectAttachments(this.GetType().ToString(), RequisitionID, true);
        }

        private List<Approval> LoadApprovals()
        {
            List<Approval> Approvals = new List<Approval>();

            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                Approvals.AddRange( Context.ApprovalDatas
                                    .Where( a => a.object_type_name == this.GetType().ToString() )
                                    .Where( a => a.identifier == RequisitionID )
                                    .Select( a => new Approval( a ) ).ToList() );


            }

            return Approvals;
        }

        private List<Note> LoadNotes()
        {
            return Note.GetNotes(this.GetType().ToString(), RequisitionID, true);
        }

        //private void SaveApprovals(string uid)
        //{
        //    foreach (Approval a in Approvals)
        //    {
        //        SaveApproval(a, uid, false);
        //    }
        //    RefreshApprovals();
        //}

        //private void SaveItems(string uid)
        //{
        //    foreach (RequisitionItem item in Items)
        //    {
        //        if (item.HasChanged())
        //            SaveItem(item, uid, false);
        //    }

        //    RefreshItems();
        //}

        private Dictionary<string, string> Validate()
        {
            Dictionary<string, string> ValErrors = new Dictionary<string, string>();

            if (String.IsNullOrEmpty(Title))
                ValErrors.Add("Title", "Title is required.");
            if (RequesterID <= 0)
                ValErrors.Add("Requester", "Requester is required.");
            if (String.IsNullOrEmpty(DeliverTo))
                ValErrors.Add("Deliver To", "Deliver to is required.");

            if (PreferredVendor != null && PreferredVendor.VendorID > 0)
                if (new Vendor(PreferredVendor.VendorID).VendorID <= 0)
                    ValErrors.Add("Vendor", "Vendor not found.");


            if (StatusLUID <= 0)
                ValErrors.Add("Status", "Status is required");
            else if (GetStatuses(false).Where(s => s.Id == StatusLUID).Count() == 0)
                ValErrors.Add("Status", "Status not found.");

            
            if (RequisitionTypeLUID <= 0)
                ValErrors.Add("Requisition Type", "Requisition Type is required.");
            else if (GetRequisitionTypes(false).Where(r => r.Id == RequisitionTypeLUID).Count() == 0)
                ValErrors.Add("Requisition Type", "Requisition Type is not found.");

            return ValErrors;

        }

        #endregion
    }

    public class RequisitionListItem
    {
        public int RequisitionID { get; set; }
        public string Title { get; set; }
        public int RequesterID { get; set; }
        public string RequesterLastFirst { get; set; }
        public string RequesterName { get; set; }
        public string Status { get; set; }
        public string RequisitionType { get; set; }
        public int ItemCount { get; set; }
        public int NoteCount { get; set; }
        public int AttachmentCount { get; set; }
        public DateTime? DateSubmitted { get; set; }
        public bool IsApproved { get; set; }
        public bool IsAccepted { get; set; }
        public decimal CurrentChargeTotal { get; set; }
    }

    public class RequistionItemsWithUnassignedRequisitionItems
    {
        public int RequisitionID { get; set; }
        public string Title { get; set; }
        public int RequesterID { get; set; }
        public DateTime? DateSubmitted { get; set; }
        public bool IsApproved { get; set; }
        public string RequesterName { get; set; }
        public string RequesterLastFirst { get; set; }
        public int MinistryID { get; set; }
        public string MinistryName { get; set; }
    }

    public class RequisitionChargeSummary
    {
        public int PaymentChargeId { get; set; }
        public DateTime PaymentDate { get; set; }
        public int PaymentMethodId { get; set; }
        public string PaymentMethodName { get; set; }
        public int PurchaseOrderId { get; set; }
        public string Account { get; set; }
        public decimal ChargeAmount { get; set; }
        public int VendorId { get; set; }
        public string VendorName { get; set; }

    }

    [Serializable]
    public class RequisitionException : Exception
    {
        public RequisitionException() { }
        public RequisitionException(string message) : base(message) { }
        public RequisitionException(string message, Exception inner) : base(message, inner) { }
        protected RequisitionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class RequisitionNotValidException : Exception
    {
        private Dictionary<string, string> mInvalidProperties = null;

        public RequisitionNotValidException() { }
        public RequisitionNotValidException(string message) : base(message) { }

        public RequisitionNotValidException(string message, Dictionary<string, string> invalidProps)
            : base(message)
        {
            mInvalidProperties = invalidProps;
        }

        public virtual Dictionary<string, string> InvalidProperties
        {
            get
            {
                return mInvalidProperties;
            }
        }

        public override string Message
        {
            get
            {
                string msg = base.Message;
                if (mInvalidProperties.Count > 0)
                {

                    System.Text.StringBuilder sb = new StringBuilder();
                    sb.AppendLine("The following fields are not valid:");
                    foreach (KeyValuePair<string, string> kvp in InvalidProperties)
                    {
                        sb.Append(kvp.Key + " - " + kvp.Value);
                    }

                    msg += "\n" + sb.ToString();
                }

                return msg;
            }
        }

        public RequisitionNotValidException(string message, Exception inner) : base(message, inner) { }
        protected RequisitionNotValidException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    class RequisitionListItemDistinctItemComparer : IEqualityComparer<RequisitionListItem>
    {

        public bool Equals(RequisitionListItem x, RequisitionListItem y)
        {
            return x.RequisitionID == y.RequisitionID;
        }

        public int GetHashCode(RequisitionListItem obj)
        {
            return obj.RequisitionID.GetHashCode();
        }
    }

}

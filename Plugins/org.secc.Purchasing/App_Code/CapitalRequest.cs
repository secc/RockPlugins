using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

using org.secc.Purchasing.DataLayer;
using Rock;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Purchasing
{
    public class CapitalRequest : PurchasingBase
    {

        #region Fields
        private DefinedValue mStatus = null;
        private Person mRequester = null;
        private DefinedValue mMinistry = null;
        private DefinedValue mLocation = null;
        //private Person mMinistryApprover = null;
        private Person mCreatedByPerson = null;
        private Person mModifiedByPerson = null;

        private List<CapitalRequestBid> mBids = null;
        private List<Requisition> mRequisitions = null;
        private List<Note> mNotes = null;
        private List<Attachment> mAttachments = null;
        private List<Approval> mApprovalRequests = null;

        public static string STATUS_LOOKUP_TYPE_GUID = "78100E0F-AA29-4002-8494-6064B7737AEA";
        public static string LOOKUP_STATUS_NEW_GUID = "FA804E46-5BB9-4D31-AD4B-1236B817E0C8";
        public static string LOOKUP_STATUS_RETURNED_TO_REQUESTER_GUID = "E2A86DC8-B369-486B-8234-EE62330D6A67";
        public static string LOOKUP_STATUS_PENDING_MINISTRY_APPROVAL_GUID = "066725B3-0B8E-4DCC-8245-A086094DF81A";
        public static string LOOKUP_STATUS_PENDING_FINANCE_APPROVAL_GUID = "40A7BA0E-A649-48B8-8FF9-C62294B8F8C7";
        public static string LOOKUP_STATUS_OPEN_GUID = "CE058A9F-A77D-414F-AEA9-F5674C7FAA69";
        public static string LOOKUP_STATUS_CLOSED_GUID = "10BEE60A-6B04-46AF-888D-F7B602689132";
        public static string LOOKUP_STATUS_CANCELLED_GUID = "7A38BE52-A88D-4513-A6FD-FB830ED560EE";
        #endregion

        #region Properties
        public int CapitalRequestId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectDescription { get; set; }
        public DateTime RequestedOn { get; set; }
        public int StatusLUID { get; set; }
        public int RequesterId { get; set; }
        public int MinistryLUID { get; set; }
        public decimal PurchaseCost { get; set; }
        public decimal InstallationCost { get; set; }
        public decimal OngoingMaintenanceCost { get; set; }
        public string ItemLocation { get; set; }
        public DateTime AnticipatedInserviceDate { get; set; }
        public int? GLCompanyId { get; set; }
        public int? GLFundId { get; set; }
        public int? GLDepartmentId { get; set; }
        public int? GLAccountId { get; set; }
        public DateTime GLFiscalYearStartDate { get; set; }
        public string CreatedByUserId { get; set; }
        public string ModifiedByUserId {get; set; }
        public DateTime DateCreated {  get; set; }
        public DateTime DateModified { get; set; }
        public bool Active { get; set; }
        public int OrganizationId { get; set; }
        public int LocationLUID { get; set; }


        [XmlIgnore]
        public string GLAccountNumberFormatted
        {
            get
            {
                string formattedAccount = null;

                if ( GLFundId != null && GLDepartmentId != null && GLAccountId != null )
                {
                    formattedAccount = string.Format( "{0}-{1}-{2}", GLFundId, GLDepartmentId, GLAccountId );
                }

                return formattedAccount;
            }
        }

        [XmlIgnore]
        public  DefinedValue Status
        {
            get
            {
                if ( ( mStatus == null && StatusLUID > 0 ) || ( mStatus != null && mStatus.Id != StatusLUID ) )
                {
                    mStatus = definedValueService.Get( StatusLUID );
                }
                return mStatus;
            }
        }
        [XmlIgnore]
        public Person Requester
        {
            get
            {
                if ( ( mRequester == null && RequesterId > 0 ) || ( mRequester != null && mRequester.Id != RequesterId ) )
                {
                    mRequester = personAliasService.Get(RequesterId).Person;

                    if (mRequester.Id > 0 && mRequester.Id != RequesterId)
                    {
                        RequesterId = mRequester.Id;
                    }
                }

                return mRequester;
            }
        }

        [XmlIgnore]
        public DefinedValue Ministry
        {
            get
            {
                if ( ( mMinistry == null && MinistryLUID > 0 ) || ( mMinistry != null && mMinistry.Id != MinistryLUID ) )
                {
                    mMinistry = definedValueService.Get( MinistryLUID );
                }

                return mMinistry;
            }
        }

        [XmlIgnore]
        public DefinedValue Location
        {
            get
            {
                if((mLocation == null && LocationLUID > 0) || (mLocation != null && mLocation.Id != LocationLUID))
                {
                    mLocation = definedValueService.Get(LocationLUID);
                }

                return mLocation;
            }
        }


        [XmlIgnore]
        public Person CreatedByPerson
        {
            get
            {
                if ( mCreatedByPerson == null && !String.IsNullOrWhiteSpace( CreatedByUserId ) )
                {
                    mCreatedByPerson = userLoginService.GetByUserName( CreatedByUserId ).Person;
                }

                return mCreatedByPerson;
            }
        }

        [XmlIgnore]
        public Person ModifiedByPerson
        {
            get
            {
                if ( mModifiedByPerson == null && !String.IsNullOrWhiteSpace( ModifiedByUserId ) )
                {
                    mModifiedByPerson = userLoginService.GetByUserName(ModifiedByUserId).Person;
                }

                return mModifiedByPerson;
            }
        }

        [XmlIgnore]
        public List<CapitalRequestBid> Bids
        {
            get
            {
                if ( mBids == null )
                {
                    LoadBids();
                }

                return mBids;
            }
        }

        [XmlIgnore]
        public List<Approval> ApprovalRequests
        {
            get
            {
                if ( mApprovalRequests == null )
                {
                    LoadApprovalRequests();
                }

                return mApprovalRequests;
            }
        }

        [XmlIgnore]
        public List<Requisition> Requisitions
        {
            get
            {
                if ( mRequisitions == null )
                {
                    LoadRequisitions();
                }

                return mRequisitions;
            }
        }

        [XmlIgnore]
        public List<Note> Notes
        {
            get
            {
                if ( mNotes == null )
                {
                    LoadNotes();
                }

                return mNotes;
            }
        }

        [XmlIgnore]
        private List<Attachment> Attachments
        {
            get
            {
                if ( mAttachments == null )
                {
                    LoadAttachments();
                }

                return mAttachments;
            }
        }

      
        #endregion

        #region Constructors
        public CapitalRequest()
        {
            Init();
        }

        public CapitalRequest( int id )
        {
            Load( id );
        }

        public CapitalRequest( CapitalRequestData data )
        {
            Load( data );
        }

        #endregion

        #region Public

        public int AddMinistryApprovalRequest( int approverId, string userID )
        {
            var requestsForApprover = ApprovalRequests.Where( a => a.Active )
                                .Where( a => a.ApprovalTypeLUID == Approval.MinistryApprovalTypeLUID() )
                                .Where( a => a.Approver.Id == approverId );

            if ( requestsForApprover.Count() > 0 )
            {
                return requestsForApprover.FirstOrDefault().ApprovalID;
            }

            Approval approvalRequest = new Approval();
            approvalRequest.ApprovalTypeLUID = Approval.MinistryApprovalTypeLUID();
            approvalRequest.ApprovalStatusLUID = Approval.NotSubmittedStatusLUID();
            approvalRequest.ApproverID = approverId;
            approvalRequest.ObjectTypeName = this.GetType().ToString();
            approvalRequest.Identifier = CapitalRequestId;
            approvalRequest.Save( userID );

            RefreshApprovalRequests();

            return approvalRequest.ApprovalID;

        }

        public int AddRequisition( string title, int requesterId, string deliverTo, string userId )
        {
            Requisition req = new Requisition();
            req.Title = title;
            req.DeliverTo = deliverTo;
            req.RequesterID = requesterId;
            req.RequisitionTypeLUID = Requisition.CapitalReqTypeLUID();
            req.CapitalRequestId = CapitalRequestId;
            req.MinistryLUID = MinistryLUID;

            if ( LocationLUID > 0 )
            {
                req.LocationLUID = LocationLUID;
            }
            
            req.Save( userId );
            RefreshRequisitions();

            return req.RequisitionID;
        }

        public void ChangeStatus( string statusGuidString, string userId )
        {
            Guid statusGuid = Guid.Empty;

            if ( !Guid.TryParse( statusGuidString, out statusGuid ) )
            {
                throw new RequisitionException( "Status Guid is not valid." );
            }

            DefinedValue newStatus = definedValueService.Get( statusGuid );

            if ( newStatus.Id <= 0 )
            {
                throw new RequisitionException( "New Status is not valid." );
            }

            StatusLUID = newStatus.Id;
            Save( userId );
        }

        public static List<CapitalRequestListItem> GetCapitalRequestList( Dictionary<string, string> filter )
        {
            List<CapitalRequestListItem> listItems = new List<CapitalRequestListItem>();

            int currentPersonId = 0;
            int currentMinistryId = 0;
            string currentUserId = null;
            bool isFinanceApprover = false;

            if ( filter.ContainsKey( "PersonId" ) )
            {
                int.TryParse( filter["PersonId"], out currentPersonId );
            }

            if ( filter.ContainsKey( "UserId" ) && !String.IsNullOrWhiteSpace( filter["UserId"] ) )
            {
                currentUserId = filter["UserId"];
            }


            if ( filter.ContainsKey( "MinistryId" ) )
            {
                int.TryParse( filter["MinistryId"], out currentMinistryId );
            }

            if ( filter.ContainsKey( "FinanceApprover" ) )
            {
                bool.TryParse( filter["FinanceApprover"], out isFinanceApprover );
            }

            using ( PurchasingContext context = ContextHelper.GetDBContext() )
            {
                int requisitionCancelledLUID = Requisition.CancelledLUID();

                var query = context.CapitalRequestDatas
                                .Join( context.LookupDatas,
                                    cr => cr.status_luid,
                                    statusLU => statusLU.Id,
                                    ( cr, statusLU ) => new
                                    {
                                        capitalRequest = cr,
                                        capitalRequestStatus = statusLU
                                    }
                                )
                                .Join( context.LookupDatas,
                                        cr => cr.capitalRequest.ministry_luid,
                                        ministryLU => ministryLU.Id,
                                        ( cr, ministryLU ) => new
                                        {
                                            capitalRequest = cr.capitalRequest,
                                            capitalRequestStatus = cr.capitalRequestStatus,
                                            capitalRequestMinitry = ministryLU
                                        }
                                )
                                .GroupJoin( context.PersonAliasDatas,
                                    cr => cr.capitalRequest.requester_id,
                                    merged => merged.Id,
                                    ( cr, merged ) => new
                                    {
                                        capitalRequest = cr.capitalRequest,
                                        capitalRequestStatus = cr.capitalRequestStatus,
                                        capitalRequestMinistry = cr.capitalRequestMinitry,
                                        requesterMerged = merged
                                    }
                                )
                                .SelectMany( joinedCR => joinedCR.requesterMerged.DefaultIfEmpty(),
                                             ( joinedCR, alias ) => new
                                             {
                                                 capitalRequest = joinedCR.capitalRequest,
                                                 capitalRequestStatus = joinedCR.capitalRequestStatus,
                                                 capitalRequestMinistry = joinedCR.capitalRequestMinistry,
                                                 requester = alias
                                             }
                                )
                                .Select( cr => new CapitalRequestListItem()
                                    {
                                        CapitalRequestId = cr.capitalRequest.capital_request_id,
                                        ProjectName = cr.capitalRequest.project_name,
                                        RequestingMinistryLUID = cr.capitalRequest.ministry_luid,
                                        RequesterId = cr.requester.Id,
                                        RequestingMinistry = cr.capitalRequestMinistry.Value,
                                        RequesterNickName = cr.requester.PersonData.NickName,
                                        RequesterLastName = cr.requester.PersonData.LastName,
                                        StatusLUID = cr.capitalRequest.status_luid,
                                        Status = cr.capitalRequestStatus.Value,
                                        RequisitionCount = cr.capitalRequest.RequisitionDatas.Where( req => req.active ).Where( req => req.status_luid != requisitionCancelledLUID ).Count(),
                                        TotalCharges = cr.capitalRequest.RequisitionDatas.Where( req => req.active )
                                                        .Where( req => req.status_luid != requisitionCancelledLUID )
                                                        .Select( req => (decimal?)req.PaymentChargeDatas
                                                                        .Where( c => c.active )
                                                                        .Select( c => c.amount ).Sum() ).Sum() ?? new decimal( 0 ),
                                        FiscalYearBeginDate = cr.capitalRequest.gl_fiscal_year_start,
                                        FundId = cr.capitalRequest.gl_fund_id,
                                        DepartmentId = cr.capitalRequest.gl_department_id,
                                        AccountId = cr.capitalRequest.gl_account_id,
                                        CreatedByUserId = cr.capitalRequest.created_by,
                                        Active = cr.capitalRequest.active,
                                        LocationLUID = cr.capitalRequest.location_luid,
                                        ApprovalItems = context.ApprovalDatas
                                                            .Where( a => a.object_type_name == typeof( CapitalRequest ).ToString() )
                                                            .Where( a => a.identifier == cr.capitalRequest.capital_request_id )
                                                            .Where( a => a.active )
                                                            .GroupJoin( context.PersonAliasDatas,
                                                                        a => a.approver_id,
                                                                        merged => merged.Id,
                                                                        ( a, merged ) => new
                                                                        {
                                                                            approval = a,
                                                                            approverMerged = merged
                                                                        } )
                                                            .SelectMany( joinedApproval => joinedApproval.approverMerged.DefaultIfEmpty(),
                                                                         ( joinedApproval, merged ) => new
                                                                         {
                                                                             approval = joinedApproval.approval,
                                                                             merged = merged
                                                                         } )
                                                            .GroupJoin( context.PersonAliasDatas,
                                                                        joinedApproval => joinedApproval.merged == null ?
                                                                        joinedApproval.approval.approver_id :
                                                                        joinedApproval.merged.Id,
                                                                        person => person.Id,
                                                                        ( joinedApproval, person ) => new
                                                                        {
                                                                            approval = joinedApproval.approval,
                                                                            approver = person
                                                                        } )
                                                            .SelectMany( joinedApproval => joinedApproval.approver.DefaultIfEmpty(),
                                                                            ( joinedApproval, person ) => new
                                                                            {
                                                                                approval = joinedApproval.approval,
                                                                                approver = person
                                                                            } )
                                                            .Select( a => new ApprovalListItem()
                                                            {
                                                                ApprovalId = a.approval.approval_id,
                                                                ApprovalTypeLUID = a.approval.approval_type_luid,
                                                                ApproverId = a.approver.Id,
                                                                ApprovalStatusLUID = a.approval.approval_status_luid,
                                                                DateApprovedString = a.approval.date_approved.ToString()
                                                            } )
                                    } );

                if ( filter.ContainsKey( "StatusLUID" ) )
                {
                    List<int> selectedStatusLUID = filter["StatusLUID"].Split( ",".ToCharArray() ).Select( s => int.Parse( s ) ).ToList();
                    query = query.Where( q => selectedStatusLUID.Contains( q.StatusLUID ) );
                }

                if ( filter.ContainsKey( "LocationId" ) )
                {
                    int locationLUID = 0;

                    if ( int.TryParse( filter["LocationId"], out locationLUID ) && locationLUID > 0 )
                    {
                        query = query.Where( q => q.LocationLUID == locationLUID );
                    }
                }

                if ( filter.ContainsKey( "RequestingMinistry" ) )
                {
                    int ministryLUID = 0;

                    if ( int.TryParse( filter["RequestingMinistry"], out ministryLUID ) && ministryLUID > 0 )
                    {
                        query = query.Where( q => q.RequestingMinistryLUID == ministryLUID );
                    }
                }

                if ( filter.ContainsKey( "Requester" ) )
                {
                    int requester = 0;

                    if ( int.TryParse( filter["Requester"], out requester ) && requester > 0 )
                    {
                        query = query.Where( q => q.RequesterId == requester );
                    }
                }

                if ( filter.ContainsKey( "GLAccount" ) && !String.IsNullOrWhiteSpace( filter["GLAccount"] ) )
                {
                    string[] projAccount = filter["GLAccount"].Split( "-".ToCharArray() );
                    int fundId = 0;
                    int deptId = 0;
                    int acctId = 0;

                    int.TryParse( projAccount[0], out fundId );
                    int.TryParse( projAccount[1], out deptId );
                    int.TryParse( projAccount[2], out acctId );

                    query = query.Where( q => q.FundId == fundId )
                                .Where( q => q.DepartmentId == deptId )
                                .Where( q => q.AccountId == acctId );
                }

                if ( filter.ContainsKey( "FiscalYear" ) )
                {
                    DateTime startDate;

                    if ( DateTime.TryParse( filter["FiscalYear"], out startDate ) && startDate > DateTime.MinValue )
                    {
                        query = query.Where( q => q.FiscalYearBeginDate == startDate );
                    }
                }


                if(filter.ContainsKey("Show_All"))
                {
                    bool showAll = false;

                    if ( bool.TryParse( filter["Show_All"], out showAll ) && showAll )
                    {
                        listItems.AddRange( query.ToList() );
                        return listItems;
                    }
                }

                if ( filter.ContainsKey( "Show_Me" ) )
                {
                    bool showMe = false;
                    if ( bool.TryParse( filter["Show_Me"], out showMe ) && showMe )
                    {
                        PersonAliasService aliasService = new PersonAliasService( new Rock.Data.RockContext() );
                        var aliasIds = aliasService.Queryable().Where( a => a.PersonId == currentPersonId ).Select( a => a.Id ).ToList();

                        listItems.AddRange( query.Where( q => aliasIds.Contains(q.RequesterId)).ToList() );

                        listItems.AddRange( query.Where( q => q.CreatedByUserId == currentUserId )
                                                .Where( q => !listItems.Select( l => l.CapitalRequestId ).Contains( q.CapitalRequestId ) )
                                                .ToList() );
                    }
                }

                if ( filter.ContainsKey( "Show_Ministry" ) )
                {
                    bool showMinistry = false;
                    if ( bool.TryParse( filter["Show_Ministry"], out showMinistry ) && showMinistry )
                    {
                        listItems.AddRange( query.Where( q => q.RequestingMinistryLUID == currentMinistryId )
                                                .Where( q => !listItems.Select( l => l.CapitalRequestId ).Contains( q.CapitalRequestId ) )
                                                .ToList() );
                    }
                }

                if ( filter.ContainsKey( "Show_Approver" ) )
                {
                    bool showApprover = false;
                    if ( bool.TryParse( filter["Show_Approver"], out showApprover ) && showApprover )
                    {
                        listItems.AddRange( query.Where( q => q.ApprovalItems.Where(a => a.ApprovalStatusLUID != Approval.NotSubmittedStatusLUID()).Select( a => a.ApproverId ).Contains( currentPersonId ) )
                                                .Where( q => !listItems.Select( l => l.CapitalRequestId ).Contains( q.CapitalRequestId ) )
                                                .ToList() );

                        if ( isFinanceApprover )
                        {
                            listItems.AddRange( query.Where( q => q.ApprovalItems.Where( a => a.ApprovalTypeLUID == Approval.FinanceApprovalTypeLUID() )
                                                                .Count() > 0 )
                                              .Where( q => !listItems.Select( l => l.CapitalRequestId ).Contains( q.CapitalRequestId ) )
                                              .ToList() );
                                                                               
                        }
                    }
                }
            }

            return listItems;
        }

        public static DefinedValueCache GetDefaultStatus()
        {
            return DefinedValueCache.Read( LOOKUP_STATUS_NEW_GUID );
        }

        public void RefreshApprovalRequests()
        {
            if ( CapitalRequestId > 0 )
            {
                LoadApprovalRequests();
            }
        }

        public void RefreshBids()
        {
            if ( CapitalRequestId > 0 )
            {
                LoadBids();
            }
        }

        public void RefreshRequisitions()
        {
            if ( CapitalRequestId > 0 )
            {
                LoadRequisitions();
            }
        }

        public void Refresh()
        {
            Load( CapitalRequestId );
        }

        public void RemoveApprovalRequest( int approvalId, string userID )
        {
            if ( Bids == null )
            {
                return;
            }

            var approvalReq = ApprovalRequests.FirstOrDefault( a => a.ApprovalID == approvalId );

            if ( approvalReq == null || approvalReq == default( Approval ) )
            {
                return;
            }

            approvalReq.Active = false;
            approvalReq.Save( userID );
            RefreshApprovalRequests();
        }

        public void RemoveBid( int bidId, string userId )
        {
            if ( Bids == null )
            {
                return;
            }

            var bid = Bids.FirstOrDefault( b => b.BidID == bidId );

            if ( bid == null || bid == default( CapitalRequestBid ) )
            {
                return;
            }

            bid.Active = false;
            bid.Save( userId );
            RefreshBids();
        }

        public void RequestFinanceApproval( Guid? templateId, int approverProfile, string userId, string cerLink )
        {
            Approval approvalRequest = ApprovalRequests
                                        .Where( a => a.Active )
                                        .Where( a => a.ApprovalTypeLUID == Approval.FinanceApprovalTypeLUID() )
                                        .FirstOrDefault();

            if ( approvalRequest == null )
            {
                approvalRequest = new Approval();
                approvalRequest.ObjectTypeName = this.GetType().ToString();
                approvalRequest.Identifier = CapitalRequestId;
                approvalRequest.ApprovalTypeLUID = Approval.FinanceApprovalTypeLUID();
                approvalRequest.ApprovalStatusLUID = Approval.PendingApprovalStatusLUID();
            }
            else
            {
                approvalRequest.ApprovalStatusLUID = Approval.PendingApprovalStatusLUID();
            }

            approvalRequest.ApproverID = 0;

            approvalRequest.Save(userId);
            RefreshApprovalRequests();

            if (!templateId.HasValue)
            {
                return;
            }
            Rock.Model.SystemEmail template = systemEmailService.Get(templateId.Value);
            Dictionary<string, object> Fields = GlobalAttributesCache.GetMergeFields(CurrentPerson);
            Fields.Add("RequestTitle", ProjectName);
            Fields.Add("RequestDescription", ProjectDescription);
            Fields.Add("RequestingMinistry", Ministry.Value);
            Fields.Add("ProjectCost", string.Format("{0:c}", PurchaseCost));
            Fields.Add("CERLink", cerLink);

            template.Body = template.Body.ResolveMergeFields(Fields);

            List<Person> approvers = new List<Person>();

            GroupService groupService = new GroupService(new Rock.Data.RockContext());

            foreach ( GroupMember gm in groupService.Get(approverProfile).Members )
            {
                if ( gm.GroupMemberStatus == GroupMemberStatus.Active )
                {
                    approvers.Add( gm.Person );
                }
            }

            SendCommunication(template, null, approvers);

        }

        public void RequestMinistryApproval(Guid? templateId, string userId, string cerLink)
        {
            var currentRequest = ApprovalRequests.Where( a => a.Active )
                            .Where( a => a.ApprovalTypeLUID == Approval.MinistryApprovalTypeLUID() )
                            .Where( a => a.ApprovalStatusLUID == Approval.NotSubmittedStatusLUID() || a.ApprovalStatusLUID == Approval.NotApprovedStatusLUID() )
                            .OrderBy( a => a.ApprovalID )
                            .FirstOrDefault();

            if ( currentRequest == null )
            {
                throw new RequisitionException( "Approval Request not found." );
            }

            currentRequest.ApprovalStatusLUID = Approval.PendingApprovalStatusLUID();
            currentRequest.Save( userId );
            RefreshApprovalRequests();

            if (!templateId.HasValue)
            {
                return;
            }
            SystemEmail template = systemEmailService.Get(templateId.Value);

            Dictionary<string, object> Fields = GlobalAttributesCache.GetMergeFields(CurrentPerson);
            Fields.Add("ApproverName", currentRequest.Approver.NickName);
            Fields.Add("Requester", Requester.FullName);
            Fields.Add("ProjectTitle", ProjectName);
            Fields.Add("CERLink", cerLink);

            template.Body = template.Body.ResolveMergeFields(Fields);

            List<Person> approver = new List<Person>() { currentRequest.Approver };

            SendCommunication( template, null, approver );
        }

        public void Save( string userId )
        {

            try
            {
                Dictionary<string, string> valErrors;

                if ( !Validate( out valErrors ) )
                {
                    throw new RequisitionNotValidException( "Capital Request not valid.", valErrors );
                }

                using ( PurchasingContext context = ContextHelper.GetDBContext() )
                {
                    CapitalRequest original = null;
                    Enums.HistoryType ht = Enums.HistoryType.ADD;
                    CapitalRequestData capRequest;


                    if ( CapitalRequestId > 0 )
                    {
                        capRequest = context.CapitalRequestDatas.FirstOrDefault( cr => cr.capital_request_id == CapitalRequestId );
                        original = new CapitalRequest( CapitalRequestId );
                        ht = Enums.HistoryType.UPDATE;
                    }
                    else
                    {
                        capRequest = new CapitalRequestData();
                        capRequest.created_by = userId;
                        capRequest.date_created = DateTime.Now;
                    }

                    capRequest.project_name = ProjectName;
                    capRequest.project_description = ProjectDescription;

                    if ( RequestedOn == DateTime.MinValue )
                    {
                        capRequest.requested_on = null;
                    }
                    else
                    {
                        capRequest.requested_on = RequestedOn;
                    }

                    capRequest.status_luid = StatusLUID;
                    capRequest.requester_id = RequesterId;
                    capRequest.ministry_luid = MinistryLUID;

                    if ( LocationLUID != 0 )
                    {
                        capRequest.location_luid = LocationLUID;
                    }
                    else
                    {
                        capRequest.location_luid = null;
                    }

                    if ( PurchaseCost > 0 )
                    {
                        capRequest.purchase_cost = PurchaseCost;
                    }
                    else
                    {
                        capRequest.purchase_cost = null;
                    }

                    if ( InstallationCost > 0 )
                    {
                        capRequest.install_train_cost = InstallationCost;
                    }
                    else
                    {
                        capRequest.install_train_cost = null;
                    }


                    if ( OngoingMaintenanceCost > 0 )
                    {
                        capRequest.ongoing_maintenance_cost = OngoingMaintenanceCost;
                    }
                    else
                    {
                        capRequest.ongoing_maintenance_cost = null;
                    }

                    capRequest.item_location = ItemLocation;

                    if ( AnticipatedInserviceDate == DateTime.MinValue )
                    {
                        capRequest.anticipated_inservice_date = null;
                    }
                    else
                    {
                        capRequest.anticipated_inservice_date = AnticipatedInserviceDate;
                    }

                    capRequest.gl_company_id = GLCompanyId;
                    capRequest.gl_fund_id = GLFundId;
                    capRequest.gl_department_id = GLDepartmentId;
                    capRequest.gl_account_id = GLAccountId;
                    capRequest.gl_fiscal_year_start = GLFiscalYearStartDate;

                    capRequest.modified_by = userId;
                    capRequest.date_modified = DateTime.Now;

                    capRequest.active = Active;
                    capRequest.organization_id = OrganizationId;

                    if ( CapitalRequestId == 0 )
                    {
                        context.CapitalRequestDatas.InsertOnSubmit( capRequest );
                    }

                    context.SubmitChanges();

                    Load( capRequest );
                    SaveHistory( ht, original, userId );

                }
            }
            catch ( Exception ex )
            {
                throw new RequisitionException( "An error occurred saving the capital request.", ex );
            }

        }

        public void SaveHistory( Enums.HistoryType ht, CapitalRequest original, string uid )
        {
            History h = new History();
            h.ObjectTypeName = this.GetType().ToString();
            h.Identifier = CapitalRequestId;
            h.ChangeType = ht;
            h.OrganizationID = OrganizationId;
            h.Active = true;

            switch ( ht )
            {
                case org.secc.Purchasing.Enums.HistoryType.ADD:
                    h.OriginalXML = null;
                    h.UpdatedXML = base.Serialize( this );
                    break;
                case org.secc.Purchasing.Enums.HistoryType.UPDATE:
                    h.OriginalXML = base.Serialize( original );
                    h.UpdatedXML = base.Serialize( this );
                    break;
                case org.secc.Purchasing.Enums.HistoryType.DELETE:
                    h.OriginalXML = base.Serialize( this );
                    h.UpdatedXML = null;
                    break;
            }
            h.Save( uid );
        }

        public void SetPreferredBid( int preferredBidId, string userId )
        {
            using ( PurchasingContext context = ContextHelper.GetDBContext() )
            {
                var bid = context.CapitalRequestBidDatas.FirstOrDefault( b => b.bid_id == preferredBidId );

                if ( bid == null )
                {
                    return;
                }

                if ( bid.is_preferred_bid )
                {
                    bid.is_preferred_bid = false;
                }
                else
                {
                    bid.is_preferred_bid = true;
                }
                context.SubmitChanges();
            }

            RefreshBids();
        }

        public void SendApprovedNotification( Guid? templateId, string cerLink )
        {

            if (!templateId.HasValue)
            {
                return;
            }
            SystemEmail template = systemEmailService.Get( templateId.Value );

            Dictionary<string, object> Fields = GlobalAttributesCache.GetMergeFields(CurrentPerson);
            Fields.Add("RequesterName", Requester.NickName);
            Fields.Add("ProjectTitle", ProjectName);
            Fields.Add("CERLink", cerLink);

            template.Body = template.Body.ResolveMergeFields(Fields);

            SendCommunication( template, null, new List<Person>() { Requester } );
        }

        public void SendReturnToRequesterNotification( Guid? templateId, int approvalId, string cerLink )
        {
            Approval approvalReq = ApprovalRequests.Where( a => a.ApprovalID == approvalId ).FirstOrDefault();

            if ( !templateId.HasValue )
            {
                return;
            }

            SystemEmail template = systemEmailService.Get( templateId.Value );


            Dictionary<string, object> Fields = GlobalAttributesCache.GetMergeFields(CurrentPerson);
            Fields.Add("RequesterName", Requester.NickName);
            Fields.Add("ApproverName", approvalReq.Approver.FullName);
            Fields.Add("ProjectName", ProjectName);
            Fields.Add("ApprovalType", approvalReq.ApprovalType.Value);
            var note = approvalReq.Notes.OrderByDescending(n => n.NoteID).FirstOrDefault();

            Fields.Add("ReturnReason", note.Body);
            Fields.Add("CERLink", cerLink);

            template.Body = template.Body.ResolveMergeFields(Fields);


            SendCommunication( template, null, new List<Person>() { Requester } );

        }

        #endregion

        #region Private


        private void Init()
        {
            mStatus = null;
            mRequester = null;
            mMinistry = null;
            mLocation = null;
            mCreatedByPerson = null;
            mModifiedByPerson = null;
            mRequisitions = null;
            mNotes = null;
            mAttachments = null;
            mApprovalRequests = null;

            CapitalRequestId = 0;
            ProjectName = null;
            ProjectDescription = null;
            RequestedOn = DateTime.MinValue;
            StatusLUID = GetDefaultStatus().Id;
            RequesterId = 0;
            MinistryLUID = 0;
            LocationLUID = 0;
            PurchaseCost = 0;
            InstallationCost = 0;
            OngoingMaintenanceCost = 0;
            ItemLocation = null;
            AnticipatedInserviceDate = DateTime.MinValue;
            GLCompanyId = Accounting.Company.GetDefaultCompany().CompanyID;
            GLFundId = null;
            GLDepartmentId = null;
            GLAccountId = null;
            Accounting.Company company = new Accounting.Company((int)GLCompanyId);
            if (company.GetCurrentFiscalYear() != null) {
                GLFiscalYearStartDate = company.GetCurrentFiscalYear().StartDate;
            }
            CreatedByUserId = null;
            ModifiedByUserId = null;
            DateCreated = DateTime.MinValue;
            DateModified = DateTime.MinValue;
            Active = true;
            OrganizationId = 1;

        }

        private void Load( int id )
        {
            using ( PurchasingContext context = ContextHelper.GetDBContext() )
            {
                Load( context.CapitalRequestDatas.FirstOrDefault( cr => cr.capital_request_id == id ) );
            }
        }

        private void Load( CapitalRequestData data )
        {
            Init();

            if ( data != null )
            {
                CapitalRequestId = data.capital_request_id;
                ProjectName = data.project_name;
                ProjectDescription = data.project_description;

                if ( data.requested_on != null )
                {
                    RequestedOn = (DateTime)data.requested_on;
                }

                StatusLUID = data.status_luid;
                RequesterId = data.requester_id;
                MinistryLUID = data.ministry_luid;

                if ( data.location_luid != null )
                {
                    LocationLUID = (int)data.location_luid;
                }

                if ( data.purchase_cost != null )
                {
                    PurchaseCost = (Decimal) data.purchase_cost;
                }

                if ( data.install_train_cost != null )
                {
                    InstallationCost = (Decimal) data.install_train_cost;
                }

                if ( data.ongoing_maintenance_cost != null )
                {
                    OngoingMaintenanceCost = (Decimal) data.ongoing_maintenance_cost;
                }
                
                ItemLocation = data.item_location;

                if ( data.anticipated_inservice_date != null )
                {
                    data.anticipated_inservice_date = data.anticipated_inservice_date;
                }

                GLCompanyId = data.gl_company_id;
                GLFundId = data.gl_fund_id;
                GLDepartmentId = data.gl_department_id;
                GLAccountId = data.gl_account_id;

                if ( data.gl_fiscal_year_start != null )
                {
                    GLFiscalYearStartDate = (DateTime)data.gl_fiscal_year_start;
                }

                CreatedByUserId = data.created_by;
                ModifiedByUserId = data.modified_by;

                DateCreated = data.date_created;
                DateModified = data.date_modified;
                Active = data.active;
                OrganizationId = data.organization_id;

            }
        }

        private void LoadApprovalRequests()
        {
            mApprovalRequests = new List<Approval>();

            if(CapitalRequestId <= 0)
            {
                return;
            }

            using ( PurchasingContext context = ContextHelper.GetDBContext() )
            {
                mApprovalRequests.AddRange(
                        context.ApprovalDatas
                            .Where( a => a.object_type_name == this.GetType().ToString() )
                            .Where( a => a.identifier == CapitalRequestId )
                            .OrderBy( a => a.approval_id )
                            .Select( a => new Approval( a ) ) );

            }
        }

        private void LoadAttachments()
        {
            mAttachments = new List<Attachment>();

            if ( CapitalRequestId > 0 )
            {
                mAttachments.AddRange( Attachment.GetObjectAttachments( this.GetType().ToString(), CapitalRequestId, false ) );
            }
        }

        private void LoadBids()
        {
            mBids = new List<CapitalRequestBid>();

            if ( CapitalRequestId > 0 )
            {
                using ( PurchasingContext context = ContextHelper.GetDBContext() )
                {
                    mBids.AddRange( context.CapitalRequestDatas
                                    .FirstOrDefault( cr => cr.capital_request_id == CapitalRequestId )
                                    .CapitalRequestBidDatas
                                    .Select( b => new CapitalRequestBid( b ) ) );
                }
            }

        }

        private void LoadNotes()
        {
            mNotes = new List<Note>();

            if ( CapitalRequestId > 0 )
            {
                mNotes.AddRange( Note.GetNotes( this.GetType().ToString(), CapitalRequestId, false ) );
            }
        }
        
        private void LoadRequisitions()
        {
            mRequisitions = new List<Requisition>();

            if ( CapitalRequestId > 0 )
            {
                using ( PurchasingContext context = ContextHelper.GetDBContext() )
                {
                    mRequisitions.AddRange( context.CapitalRequestDatas
                                            .FirstOrDefault( cr => cr.capital_request_id == CapitalRequestId )
                                            .RequisitionDatas
                                            .Select( r => new Requisition( r ) ) );
                }
            }
        }

        private void SendCommunication(Rock.Model.SystemEmail template, Person sender, List<Person> recepientList )
        {
            string fromName = null;
            string fromEmail = null;
            string replyToEmail = null;

            if ( sender != null )
            {
                var senderEmail = sender.Email;
                replyToEmail = sender.Email;
                if ( senderEmail != null )
                {
                    fromEmail = senderEmail;
                    fromName = sender.FullName;
                }
                else
                {
                    fromEmail = template.From;
                    fromName = template.FromName;
                }
            }
            else
            {
                replyToEmail = template.From;
            }

            List<String> recepients = recepientList.Select(p => p.Email).ToList();

            Rock.Communication.Email.Send(fromEmail, fromName, template.Subject, recepients, template.Body);

            //Arena.Utility.ArenaSendMail.SendMail( fromEmail, fromName, recepients, template.ReplyEmail, "", "", template.Subject, template.HtmlMessage, template.TextMessage );
        }

        private bool Validate(out Dictionary<string,string> valErrors)
        {
            valErrors = new Dictionary<string, string>();

            if ( String.IsNullOrWhiteSpace( ProjectName ) )
            {
                valErrors.Add( "Project Name", "Project Name is required." );
            }
            
            if ( String.IsNullOrWhiteSpace( ProjectDescription ) )
            {
                valErrors.Add( "Project Description", "Project Description is required." );
            }
            
            if(PurchaseCost <= 0)
            {
                valErrors.Add( "Purchase Cost", "Purchase Cost must be greater than 0.00." );
            }

            if ( InstallationCost < 0 )
            {
                valErrors.Add( "Initial Costs", "Initial Costs must be greater than 0.00." );
            }

            if ( OngoingMaintenanceCost < 0 )
            {
                valErrors.Add( "Ongoing Costs", "Ongoing Costs must be greater than 0.00." );
            }

            if ( RequestedOn == DateTime.MinValue )
            {
                valErrors.Add( "Date Requested", "Date Requested must be provided" );
            }


            if ( RequesterId == 0 )
            {
                valErrors.Add( "Requester", "Requester is required." );
            }
            else
            {
                Person requester = personAliasService.Get(RequesterId).Person;
                if ( requester == null || requester.Id <= 0 )
                {
                    valErrors.Add( "Requester", "Requester not found." );
                }
                else
                {
                    RequesterId = requester.PrimaryAliasId.Value;
                }
            }

            if ( MinistryLUID == 0 )
            {
                valErrors.Add( "Requesting Ministry", "Requesting Ministry is required." );
            }
            else
            {
                DefinedValue ministryLookup = definedValueService.Get( MinistryLUID );

                if ( ministryLookup == null || ministryLookup.Id <= 0 )
                {
                    valErrors.Add( "Requesting Ministry", "Requesting Ministry not found." );
                }
            }

            if ( GLCompanyId != null && GLFundId != null && GLDepartmentId != null && GLAccountId != null )
            {
                Accounting.Account glaccount = new Accounting.Account( (int)GLCompanyId, (int)GLFundId, (int)GLDepartmentId, (int)GLAccountId, GLFiscalYearStartDate );

                if ( glaccount == null || glaccount.AccountID <= 0 )
                {
                    valErrors.Add( "General Ledger Account", "General Ledger Account is not valid." );
                }
            }

            if (StatusLUID != definedValueService.Get(new Guid(LOOKUP_STATUS_NEW_GUID)).Id && StatusLUID != definedValueService.Get(new Guid(LOOKUP_STATUS_CANCELLED_GUID)).Id)
            {
                if ( GLCompanyId == null || GLFundId == null || GLDepartmentId == null || GLAccountId == null )
                {
                    valErrors.Add( "Project Account", "Project Account is required" );
                }
            }

            
            return valErrors.Count == 0;
        }

        #endregion

    }

    public class CapitalRequestListItem
    {
        public int CapitalRequestId { get; set; }
        public string ProjectName { get; set; }
        public int RequestingMinistryLUID { get; set; }
        public int? LocationLUID { get; set; }
        public int RequesterId { get; set; }
        public int StatusLUID { get; set; }
        public string RequestingMinistry { get; set; }
        public string RequesterNickName { get; set; }
        public string RequesterLastName { get; set; }
        public string Status { get; set; }
        public int RequisitionCount { get; set; }
        public decimal TotalCharges { get; set; }
        public DateTime? FiscalYearBeginDate { get; set; }
        public int? FundId { get; set; }
        public int? DepartmentId { get; set; }
        public int? AccountId { get; set; }
        public string CreatedByUserId { get; set; }
        public bool Active { get; set; }
        public IQueryable<ApprovalListItem> ApprovalItems { get; set; }

        public string FullAccountNumber
        {
            get
            {
                string fullAcctNumber = null;
                if ( FundId != null && DepartmentId != null && AccountId != null )
                {
                    fullAcctNumber = string.Format( "{0}-{1}-{2}", FundId, DepartmentId, AccountId );

                }

                return fullAcctNumber;
            }
        }

        public string RequesterName
        {
            get
            {
                return string.Format( "{0} {1}", RequesterNickName, RequesterLastName );
            }
        }

        public string RequesterNameLastFirst
        {
            get
            {
                return string.Format( "{0}, {1}", RequesterLastName, RequesterNickName );
            }
        }
    }


}

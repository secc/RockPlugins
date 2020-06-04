using System;
using RestSharp.Deserializers;

namespace org.secc.Purchasing.Intacct.Model
{
    [DeserializeAs( Name = "location" )]
    public class Location : IntacctModel
    {

        public override int Id
        {
            get
            {
                int id;
                int.TryParse( LocationId.Substring( 1 ), out id );
                return id;
            }
        }

        public override string ApiId
        {
            get
            {
                return LocationId;
            }
        }

        [DeserializeAs( Name = "LOCATIONID" )]
        public string LocationId { get; set; }

        [DeserializeAs( Name = "NAME" )]
        public string Name { get; set; }

        [DeserializeAs( Name = "PARENTID" )]
        public string ParentId { get; set; }

        [DeserializeAs( Name = "SUPERVISORNAME" )]
        public string SupervisorName { get; set; }

        [DeserializeAs( Name = "SUPERVISORID" )]
        public string SupervisorId { get; set; }

        [DeserializeAs( Name = "CONTACTINFO.CONTACTNAME" )]
        public string ContactName { get; set; }

        [DeserializeAs( Name = "CONTACTINFO.PRINTAS" )]
        public string ContactPrintAs { get; set; }

        [DeserializeAs( Name = "CONTACTINFO.PHONE1" )]
        public string ContactPhone1 { get; set; }

        [DeserializeAs( Name = "CONTACTINFO.PHONE2" )]
        public string ContactPhone2 { get; set; }

        [DeserializeAs( Name = "CONTACTINFO.EMAIL1" )]
        public string ContactEmail1 { get; set; }

        [DeserializeAs( Name = "CONTACTINFO.EMAIL2" )]
        public string ContactEmail2 { get; set; }

        [DeserializeAs( Name = "CONTACTINFO.MAILADDRESS.ADDRESS1" )]
        public string ContactAddress1 { get; set; }

        [DeserializeAs( Name = "CONTACTINFO.MAILADDRESS.ADDRESS2" )]
        public string ContactAddress2 { get; set; }

        [DeserializeAs( Name = "CONTACTINFO.MAILADDRESS.CITY" )]
        public string ContactCity { get; set; }

        [DeserializeAs( Name = "CONTACTINFO.MAILADDRESS.STATE" )]
        public string ContactState { get; set; }

        [DeserializeAs( Name = "CONTACTINFO.MAILADDRESS.ZIP" )]
        public string ContactZip { get; set; }

        [DeserializeAs( Name = "CONTACTINFO.MAILADDRESS.COUNTRY" )]
        public string ContactCountry { get; set; }

        [DeserializeAs( Name = "CONTACTINFO.MAILADDRESS.COUNTRYCODE" )]
        public string ContactCountryCode { get; set; }

        [DeserializeAs( Name = "STARTDATE" )]
        public DateTime? StartDate { get; set; }

        [DeserializeAs( Name = "ENDDATE" )]
        public DateTime? EndDate { get; set; }

        [DeserializeAs( Name = "SHIPTO.SHIPTO" )]
        public string ShipToName { get; set; }

        [DeserializeAs( Name = "SHIPTO.PHONE1" )]
        public string ShipToPhone1 { get; set; }

        [DeserializeAs( Name = "SHIPTO.PHONE2" )]
        public string ShipToPhone2 { get; set; }

        [DeserializeAs( Name = "SHIPTO.MAILADDRESS.ADDRESS1" )]
        public string ShipToAddress1 { get; set; }

        [DeserializeAs( Name = "SHIPTO.MAILADDRESS.ADDRESS2" )]
        public string ShipToAddress2 { get; set; }

        [DeserializeAs( Name = "SHIPTO.MAILADDRESS.CITY" )]
        public string ShipToCity { get; set; }

        [DeserializeAs( Name = "SHIPTO.MAILADDRESS.STATE" )]
        public string ShipToState { get; set; }

        [DeserializeAs( Name = "SHIPTO.MAILADDRESS.ZIP" )]
        public string ShipToZip { get; set; }

        [DeserializeAs( Name = "SHIPTO.MAILADDRESS.COUNTRY" )]
        public string ShipToCountry { get; set; }

        [DeserializeAs( Name = "SHIPTO.MAILADDRESS.COUNTRYCODE" )]
        public string ShipToCountryCode { get; set; }

        [DeserializeAs( Name = "STATUS" )]
        public string Status { get; set; }

        [DeserializeAs( Name = "WHENCREATED" )]
        public DateTime? WhenCreated { get; set; }

        [DeserializeAs( Name = "WHENMODIFIED" )]
        public DateTime? WhenModified { get; set; }

        [DeserializeAs( Name = "FEDERALID" )]
        public string FederalId { get; set; }

        [DeserializeAs( Name = "FIRSTMONTH" )]
        public int? FirstMonth { get; set; }

        [DeserializeAs( Name = "WEEKSTART" )]
        public int? WeekStart { get; set; }

        [DeserializeAs( Name = "IEPAYABLE.ACCOUNT" )]
        public string IEPayableAccount { get; set; }

        [DeserializeAs( Name = "IEPAYABLE.NUMBER" )]
        public int? IEPayableNumber { get; set; }

        [DeserializeAs( Name = "IERECEIVABLE.ACCOUNT" )]
        public string IEReceivableAccount { get; set; }

        [DeserializeAs( Name = "IERECEIVABLE.NUMBER" )]
        public int? IEReceivableNumber { get; set; }

        [DeserializeAs( Name = "MESSAGE_TEXT" )]
        public string MessageText { get; set; }

        [DeserializeAs( Name = "MARKETING_TEXT" )]
        public string MarketingText { get; set; }

        [DeserializeAs( Name = "FOOTNOTETEXT" )]
        public string FootnoteText { get; set; }

        [DeserializeAs( Name = "REPORTPRINTAS" )]
        public string ReportPrintAs { get; set; }

        [DeserializeAs( Name = "ISROOT" )]
        public string IsRoot { get; set; }

        [DeserializeAs( Name = "RESERVEAMT" )]
        public string ReserveAmt { get; set; }

        [DeserializeAs( Name = "VENDORNAME" )]
        public string VendorName { get; set; }

        [DeserializeAs( Name = "VENDORID" )]
        public int? VendorId { get; set; }

        [DeserializeAs( Name = "CUSTOMERID" )]
        public int? CustomerId { get; set; }

        [DeserializeAs( Name = "CUSTOMERNAME" )]
        public string CustomerName { get; set; }

        [DeserializeAs( Name = "CURRENCY" )]
        public string Currency { get; set; }

        [DeserializeAs( Name = "ENTITY" )]
        public string Entity { get; set; }

        [DeserializeAs( Name = "ENTITYRECORDNO" )]
        public int? EntityRecordNo { get; set; }

        [DeserializeAs( Name = "HAS_IE_RELATION" )]
        public bool? HasIeRelation { get; set; }

        [DeserializeAs( Name = "CUSTTITLE" )]
        public string CustTitle { get; set; }

        [DeserializeAs( Name = "BUSINESSDAYS" )]
        public string BusinessDays { get; set; }

        [DeserializeAs( Name = "WEEKENDS" )]
        public string Weekends { get; set; }

        [DeserializeAs( Name = "FIRSTMONTHTAX" )]
        public int? FirstMonthTax { get; set; }

        [DeserializeAs( Name = "CONTACTKEY" )]
        public string ContactKey { get; set; }

        [DeserializeAs( Name = "SUPERVISORKEY" )]
        public string SupervisorKey { get; set; }

        [DeserializeAs( Name = "PARENTKEY" )]
        public string ParentKey { get; set; }

        [DeserializeAs( Name = "SHIPTOKEY" )]
        public string ShipToKey { get; set; }

        [DeserializeAs( Name = "IEPAYABLEACCTKEY" )]
        public int? IePayableAcctKey { get; set; }

        [DeserializeAs( Name = "IERECEIVABLEACCTKEY" )]
        public int? IeReceivableAcctKey { get; set; }

        [DeserializeAs( Name = "VENDENTITY" )]
        public string VendEntity { get; set; }

        [DeserializeAs( Name = "CUSTENTITY" )]
        public string CustEntity { get; set; }

        [DeserializeAs( Name = "TAXID" )]
        public int? TaxId { get; set; }

        [DeserializeAs( Name = "CREATEDBY" )]
        public int? CreatedBy { get; set; }

        [DeserializeAs( Name = "MODIFIEDBY" )]
        public int? ModifiedBy { get; set; }
    }
}

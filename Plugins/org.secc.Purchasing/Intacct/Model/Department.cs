using RestSharp.Deserializers;

namespace org.secc.Purchasing.Intacct.Model
{
    [DeserializeAs( Name = "department" )]
    public class Department : IntacctModel
    {

        public override int Id
        {
            get
            {
                int id;
                int.TryParse( DepartmentId, out id );
                return id;
            }
        }

        public override string ApiId
        {
            get
            {
                return DepartmentId.ToString();
            }
        }

        [DeserializeAs( Name = "DEPARTMENTID" )]
        public string DepartmentId { get; set; }

        [DeserializeAs( Name = "TITLE" )]
        public string Title { get; set; }

        [DeserializeAs( Name = "PARENTKEY" )]
        public string ParentKey { get; set; }

        [DeserializeAs( Name = "PARENTID" )]
        public string ParentId { get; set; }

        [DeserializeAs( Name = "SUPERVISORKEY" )]
        public string SupervisorKey { get; set; }

        [DeserializeAs( Name = "SUPERVISORID" )]
        public string SupervisorId { get; set; }

        [DeserializeAs( Name = "WHENCREATED" )]
        public string WhenCreated { get; set; }

        [DeserializeAs( Name = "WHENMODIFIED" )]
        public string WhenModified { get; set; }

        [DeserializeAs( Name = "SUPERVISORNAME" )]
        public string SupervisorName { get; set; }

        [DeserializeAs( Name = "STATUS" )]
        public string Status { get; set; }

        [DeserializeAs( Name = "CUSTTITLE" )]
        public string CustTitle { get; set; }

        [DeserializeAs( Name = "CREATEDBY" )]
        public string CreatedBy { get; set; }

        [DeserializeAs( Name = "MODIFIEDBY" )]
        public string ModifiedBy { get; set; }

    }
}

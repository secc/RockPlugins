using RestSharp.Serializers;

namespace org.secc.Purchasing.Intacct.Auth
{
    public class Login
    {
        [SerializeAs( Name = "userid" )]
        public string UserId { get; set; }

        [SerializeAs( Name = "companyid" )]
        public string CompanyId { get; set; }

        [SerializeAs( Name = "password" )]
        public string Password { get; set; }
    }
}

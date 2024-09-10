using Rock.Address;

namespace org.secc.Rest.Models
{
    public class GroupAppGroupMember
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string GroupRole { get; set; }
        public string Status { get; set; }
        public string Address { get; set; }
        public string Email { get; set;}
        public string Phone { get; set;}
        public string PhotoURL { get; set; }
    }
}

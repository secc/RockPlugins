using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.ServiceReef.Contracts
{
    public class Member
    {
        public DateTime Birthdate { get; set; }
        public object Website { get; set; }
        public object Facebook { get; set; }
        public object Twitter { get; set; }
        public string ThumbUrl { get; set; }
        public string ImageUrl { get; set; }
        public List<Trip> Trips { get; set; }
        public List<object> MasterApplicationAnswers { get; set; }
        public List<string> Badges { get; set; }
        public int ArenaId { get; set; }
        public object FacebookId { get; set; }
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public object Phone { get; set; }
        public Address Address { get; set; }
        public object BackgroundCheckDate { get; set; }
        public string ProfileUrl { get; set; }

        public class Trip
        {
            public int EventId { get; set; }
            public string Name { get; set; }
            public string Url { get; set; }
            public string StartDate { get; set; }
        }
    }
}

using System;

namespace org.secc.FamilyCheckin.Model
{
    public class SFChildcareParticipant
    {
        public int PersonId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CheckinTime { get; set; }
        public DateTime CheckoutTime { get; set; }
        public int CreditsUsed { get; set; }

        public TimeSpan TotalTimeCheckedIn
        {
            get
            {
                return CheckoutTime - CheckinTime;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using Rock;
using Rock.Data;
using Rock.Lava;

namespace org.secc.FamilyCheckin.Model
{
    public class SFChildcareParticipant : LavaDataObject
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
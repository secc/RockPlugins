using System;
using System.Collections.Generic;
using Rock;
using Rock.Data;
using Rock.Lava;

namespace org.secc.FamilyCheckin.Model
{
    public class SFChildcareParticipant : ILiquidizable
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


        [LavaHidden]
        public object this[object key]
        {
            get
            {
                switch ( key.ToStringSafe() )
                {
                    case "PersonId": return PersonId;
                    case "FirstName": return FirstName;
                    case "LastName": return LastName;
                    case "CheckinTime": return CheckinTime;
                    case "CheckoutTime": return CheckoutTime;
                    case "CreditsUsed": return CreditsUsed;
                    default: return String.Empty;
                }
            }
        }

        [LavaHidden]
        public List<string> AvailableKeys
        {
            get
            {
                return new List<string> {
                "PersonId",
                "FirstName",
                "LastName",
                "CheckinTime",
                "CheckoutTime",
                "CreditsUsed",
                "TotalTimeCheckedIn" };
            }
        }

    public bool ContainsKey( object key )
        {
            var keys = new List<string> { 
                "PersonId", 
                "FirstName", 
                "LastName", 
                "CheckinTime", 
                "CheckoutTime", 
                "CreditsUsed", 
                "TotalTimeCheckedIn" };

            return keys.Contains( key.ToStringSafe() );
        }

        public object ToLiquid()
        {
            return this;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Model;

namespace org.secc.FamilyCheckin.Model
{
    /// <summary>
    /// Sports and Fitness Childcare Receipt
    /// </summary>
    public class SFChilcareReceipt
    {
        public int FamilyId { get; set; }
        public int CreditsBeginning { get; set; }
        public int CreditsEnding
        {
            get
            {
                return CreditsBeginning - CreditsUsed;
            }
        }
        public List<SFChildcareParticipant> Participants { get; set; }

        public int CreditsUsed
        {
            get
            {
                if(Participants != null)
                {
                    return Participants.Sum( a => a.CreditsUsed );
                }
                return 0;
            }
        }

        public void AddParticipant(Person p, DateTime checkinTime, DateTime checkoutTime, int creditsUsed)
        {
            if(Participants == null)
            {
                Participants = new List<SFChildcareParticipant>();
            }

            Participants.Add( new SFChildcareParticipant
            {
                PersonId = p.Id,
                LastName = p.LastName,
                FirstName = p.NickName,
                CheckinTime = checkinTime,
                CheckoutTime = checkoutTime,
                CreditsUsed = creditsUsed
            } );

        }
    }

  
}

using System;

namespace org.secc.FamilyCheckin.Model
{
    /// <summary>
    /// One participant on a Sports &amp; Fitness childcare receipt.
    /// </summary>
    /// <remarks>
    /// This type is persisted as JSON in the "CheckoutReceiptData" workflow attribute
    /// (see <c>SportsAndFitnessChildcareCredits</c>) and read back with <c>FromJsonOrNull</c>,
    /// so it must stay a plain POCO. Do not derive from <c>LavaDataObject</c>: that type is an
    /// <c>IDictionary</c> whose <c>Add</c> throws, so Json.NET cannot deserialize it. When the
    /// receipt is merged into the label template, <c>SportsAndFitnessChidcareReceipt</c> wraps
    /// each participant in <c>new LavaDataObject( participant )</c> for the Lava engines.
    /// </remarks>
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

using Rock.Bus.Message;
using Rock.Bus.Queue;

namespace org.secc.FamilyCheckin.Cache
{
    /// <summary>
    /// Extended cache message that includes additional data for updates
    /// </summary>
    public class CheckinCacheMessage : CacheWasUpdatedMessage
    {
        /// <summary>
        /// Gets or sets the additional data for the cache update.
        /// This is used to serialize the actual cache item when updating.
        /// </summary>
        public string AdditionalData { get; set; }

        /// <summary>
        /// Returns a string representation of the message for debugging purposes.
        /// </summary>
        /// <returns>A string containing the message details.</returns>
        public string ToDebugString()
        {
            return $"CacheType: {CacheTypeName}, Key: {Key}, Region: {Region}, Sender: {SenderNodeName}, HasAdditionalData: {!string.IsNullOrEmpty(AdditionalData)}";
        }
    }
}

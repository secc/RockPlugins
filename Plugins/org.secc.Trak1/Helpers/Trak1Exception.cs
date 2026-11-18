using System;

namespace org.secc.Trak1.Helpers
{
    /// <summary>
    /// A failed or invalid Trak-1 response.
    /// </summary>
    public class Trak1Exception : Exception
    {
        /// <summary>
        /// The longest response body kept on the exception.
        /// </summary>
        public const int MaxResponseBodyLength = 200;

        public Trak1Exception( string message, int? statusCode = null, string responseBody = null, string vendorMessage = null )
            : base( message )
        {
            StatusCode = statusCode;
            ResponseBody = Truncate( responseBody );
            VendorMessage = vendorMessage;
        }

        public Trak1Exception( string message, string responseBody, Exception innerException )
            : base( message, innerException )
        {
            ResponseBody = Truncate( responseBody );
        }

        /// <summary>
        /// The HTTP status code, when the request reached Trak-1 and failed.
        /// </summary>
        public int? StatusCode { get; }

        /// <summary>
        /// The start of the response body (at most <see cref="MaxResponseBodyLength"/> characters).
        /// </summary>
        public string ResponseBody { get; }

        /// <summary>
        /// The error message Trak-1 returned in the response body, if there was one.
        /// </summary>
        public string VendorMessage { get; }

        internal static string Truncate( string value )
        {
            if ( value == null || value.Length <= MaxResponseBodyLength )
            {
                return value;
            }

            return value.Substring( 0, MaxResponseBodyLength );
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.RoomScanner.Models
{
    public class Response
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public bool Overridable { get; set; }
        public bool RequireConfirmation { get; set; }
        public int PersonId { get; set; }

        public Response( bool success, string message, bool overridable, bool requireConfirmation = false, int personId = 0)
        {
            this.Success = success;
            this.Message = message;
            this.Overridable = overridable;
            this.RequireConfirmation = requireConfirmation;
            this.PersonId = personId;
        }
    }
}

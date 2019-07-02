using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Data;

namespace org.secc.ChangeManager.Model
{
    public class BasicEntity : IEntity
    {
        public int Id { get; set; }
        public Guid Guid { get; set; }
        public int? ForeignId { get; set; }
        public Guid? ForeignGuid { get; set; }
        public string ForeignKey { get; set; }

        public int TypeId { get { return 0; } }

        public string TypeName { get { return "BasicEntity"; } }

        public string EncryptedKey { get { return ""; } }

        public string ContextKey { get { return ""; } }

        public List<ValidationResult> ValidationResults { get { return new List<ValidationResult>(); } }

        public bool IsValid { get { return true; } }

        public Dictionary<string, object> AdditionalLavaFields { get; set; }

        public IEntity Clone()
        {
            return this;
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>();
        }
    }
}

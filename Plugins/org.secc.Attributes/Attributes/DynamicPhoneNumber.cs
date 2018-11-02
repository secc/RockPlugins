using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using org.secc.Attributes.FieldTypes;
using Rock.Attribute;

namespace org.secc.Attributes.Attributes
{
    public class DynamicPhoneNumber : FieldAttribute
    {
        public DynamicPhoneNumber( string name, string description = "", bool required = true, string defaultGroupGuid = "", string category = "", int order = 0, string key = null )
            : base( name, description, required, defaultGroupGuid, category, order, key, typeof( DynamicPhoneNumberFieldType ).FullName )
        {
        }
    }
}

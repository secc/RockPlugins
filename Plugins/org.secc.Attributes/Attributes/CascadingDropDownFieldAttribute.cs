// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using org.secc.Attributes.FieldTypes;
using Rock.Attribute;

namespace org.secc.Attributes.Attributes
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = true )]
    public class CascadingDropDownFieldAttribute : FieldAttribute
    {
        private const string CONFIG = "configuration";

        public CascadingDropDownFieldAttribute( string name, string description, string matrix, bool required = true, string defaultValue = "", string category = "", int order = 0, string key = null )
            : base( name, description, required, defaultValue, category, order, key, typeof( CascadingDropDownFieldType ).FullName, "org.secc.Attributes" )
        {
            FieldConfigurationValues.Add( CONFIG, new Rock.Field.ConfigurationValue( matrix ) );
        }
    }
}

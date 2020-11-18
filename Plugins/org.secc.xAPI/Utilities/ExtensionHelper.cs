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
using System.Linq;
using Humanizer;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.xAPI.Utilities
{
    public static class ExtensionHelper
    {
        public static DefinedValueCache GetOrCreateExtension( string url )
        {
            var extensions = DefinedTypeCache.Get( Constants.DEFINED_TYPE_EXTENSIONS );
            var extension = extensions.DefinedValues.Where( v => v.Value == url ).FirstOrDefault();
            if ( extension == null )
            {
                extension = CreateExtension( url );
            }
            return extension;
        }

        private static DefinedValueCache CreateExtension( string url )
        {
            var names = url.Split( new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries );
            var name = names.Last().Humanize();

            return CreateExtension( url, name );
        }

        private static DefinedValueCache CreateExtension( string url, string name )
        {
            RockContext rockContext = new RockContext();
            DefinedValueService definedValueService = new DefinedValueService( rockContext );
            var verbTypeId = DefinedTypeCache.Get( Constants.DEFINED_TYPE_EXTENSIONS ).Id;

            var extension = new DefinedValue
            {
                DefinedTypeId = verbTypeId,
                Value = url
            };

            definedValueService.Add( extension );
            rockContext.SaveChanges();

            extension.LoadAttributes();
            extension.SetAttributeValue( Constants.DEFINED_VALUE_EXTENSIONS_ATTRIBUTE_KEY_NAME, name );
            extension.SaveAttributeValues();

            return DefinedValueCache.Get( extension.Id );
        }
    }
}

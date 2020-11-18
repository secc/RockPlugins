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
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.xAPI.Utilities
{
    public static class VerbHelper
    {
        public static DefinedValueCache GetOrCreateVerb( string url )
        {
            var verbs = DefinedTypeCache.Get( Constants.DEFINED_TYPE_VERBS );
            var verb = verbs.DefinedValues.Where( v => v.Value == url ).FirstOrDefault();
            if ( verb == null )
            {
                verb = CreateVerb( url );
            }
            return verb;
        }

        private static DefinedValueCache CreateVerb( string url )
        {
            var names = url.Split( new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries );
            //this is an inexact estimation of the name based on the url
            var name = names.Last().TrimEnd( 'e' ) + "ed";

            return CreateVerb( url, name );
        }

        private static DefinedValueCache CreateVerb( string url, string name )
        {
            RockContext rockContext = new RockContext();
            DefinedValueService definedValueService = new DefinedValueService( rockContext );
            var verbTypeId = DefinedTypeCache.Get( Constants.DEFINED_TYPE_VERBS ).Id;

            var verb = new DefinedValue
            {
                DefinedTypeId = verbTypeId,
                Value = url
            };

            definedValueService.Add( verb );
            rockContext.SaveChanges();

            verb.LoadAttributes();
            verb.SetAttributeValue( Constants.DEFINED_VALUE_VERBS_ATTRIBUTE_KEY_NAME, name );
            verb.SaveAttributeValues();

            return DefinedValueCache.Get( verb.Id );
        }
    }
}

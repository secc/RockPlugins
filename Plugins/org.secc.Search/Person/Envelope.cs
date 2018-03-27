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
// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Search;
using Rock.Web.Cache;

namespace org.secc.Search.Person
{
    /// <summary>
    /// Searches for people with matching envelope numbers
    /// </summary>
    [Description( "Person Envelope Search" )]
    [Export( typeof( SearchComponent ) )]
    [ExportMetadata( "ComponentName", "SECC Person Envelope" )]
    public class Envelope : SearchComponent
    {
        /// <summary>
        /// Gets the attribute value defaults.
        /// </summary>
        /// <value>
        /// The attribute defaults.
        /// </value>
        public override Dictionary<string, string> AttributeValueDefaults
        {
            get
            {
                var defaults = new Dictionary<string, string>();
                defaults.Add( "SearchLabel", "Envelope" );
                return defaults;
            }
        }

        /// <summary>
        /// Returns a list of matching people
        /// </summary>
        /// <param name="searchterm"></param>
        /// <returns></returns>
        public override IQueryable<string> Search( string searchterm )
        {
            var rockContext = new RockContext();
            var aService = new AttributeService(rockContext);
            var avService = new AttributeValueService( rockContext );
            var personEntityTypeId = EntityTypeCache.Read( Rock.SystemGuid.EntityType.PERSON.AsGuid() ).Id;
            var attribute = aService.Queryable().Where( a => a.Key == "GivingEnvelopeNumber" && a.EntityTypeId == personEntityTypeId ).FirstOrDefault();
            if (attribute != null)
            {
                return avService.Queryable().Where(av => av.AttributeId == attribute.Id && av.Value.Equals(searchterm) ).Select( av => av.Value );
            }

            return null;
        }
    }
}
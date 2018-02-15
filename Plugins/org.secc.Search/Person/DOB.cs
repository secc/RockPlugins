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
using Rock.Data;
using Rock.Model;
using Rock.Search;
using Rock;

namespace org.secc.Search.Person
{
    /// <summary>
    /// Searches for people with matching address
    /// </summary>
    [Description( "Person DOB Search" )]
    [Export( typeof( SearchComponent ) )]
    [ExportMetadata( "ComponentName", "SECC Person DOB" )]
    public class DOB : SearchComponent
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
                defaults.Add( "SearchLabel", "DOB" );
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
            // Case insensitive
            searchterm = searchterm.ToLower();

            var rockContext = new RockContext();

            var personService = new PersonService(rockContext);
            var birthDates = personService.Queryable().Where( p => p.BirthDate.HasValue ).Select( p => p.BirthDate ).Distinct().AsEnumerable();
            var shortDateSearch = birthDates.Where( p => p.Value.ToString( "d" ).ToLower().Contains( searchterm ) || p.Value.ToString( "MM/dd/yyyy" ).ToLower().Contains( searchterm ) ).Select( p => p.Value.ToString( "d" ) );
            var longDateSearch =  birthDates.Where( p => p.Value.ToString( "MMMM d, yyyy" ).ToLower().Contains( searchterm ) ).Select( p => p.Value.ToString( "MMMM d, yyyy" ) );
            if ( shortDateSearch.Union( longDateSearch ).Any() )
            {
                return shortDateSearch.Union( longDateSearch ).Distinct().AsQueryable();
            }
            // Just get crazy with things (Yep, you can find out who was born on a Tuesday)
            var longestDateSearch = birthDates.Where( p => p.Value.ToString( "D" ).ToLower().Contains( searchterm ) ).Select( p => p.Value.ToString( "D" ) );
            return longestDateSearch.Distinct().AsQueryable();
        }
    }
}
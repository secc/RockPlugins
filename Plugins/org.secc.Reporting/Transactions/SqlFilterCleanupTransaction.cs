// <copyright>

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
using System.Collections.Generic;
using System.Linq;
using org.secc.Reporting.Data;
using org.secc.Reporting.DataFilter;
using org.secc.Reporting.Model;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Transactions;
using Rock.Web.Cache;

namespace org.secc.Reporting.Transactions
{
    public class SqlFilterCleanupTransaction : ITransaction
    {
        public void Execute()
        {
            var filterEntityId = EntityTypeCache.GetId( typeof( SQLFilter ) );
            if ( !filterEntityId.HasValue )
            {
                throw new System.Exception( "Could not find SQL Filter" );
            }

            RockContext rockContext = new RockContext();
            DataViewFilterService dataViewFilterService = new DataViewFilterService( rockContext );
            DataViewSqlFilterStoreService dataViewSqlFilterStoreService = new DataViewSqlFilterStoreService( rockContext );

            var filters = dataViewFilterService.Queryable().Where( s => s.EntityTypeId == filterEntityId.Value ).ToList();

            List<string> hashes = new List<string>();

            foreach ( var filter in filters )
            {
                hashes.Add( Helpers.Md5Hash( filter.Selection ) );
            }

            ReportingContext reportingContext = new ReportingContext();
            var toDelete = reportingContext.DataViewSQLFilterStores
                .Where( s => !hashes.Contains( s.Hash ) )
                .Select( s => s.Hash ).Distinct().ToList();

            foreach ( var hash in toDelete )
            {
                rockContext.Database.ExecuteSqlCommand( string.Format( "DELETE FROM _org_secc_Reporting_DataViewSQLFilterStore WHERE Hash = '{0}'", hash ) );
            }
        }
    }
}

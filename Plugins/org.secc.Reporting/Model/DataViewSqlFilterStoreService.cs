﻿// <copyright>
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
using org.secc.Reporting.Data;
using Rock.Data;

namespace org.secc.Reporting.Model
{
    public class DataViewSqlFilterStoreService : ReportingService<DataViewSQLFilterStore>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PublishGroup"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public DataViewSqlFilterStoreService( RockContext context ) : base( context )
        {

        }

    }
}

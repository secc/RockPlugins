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
using System.Linq;
using System.Data;
using System.Data.Entity;
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using System.Collections.Generic;
using System;
using System.Net;
using System.IO;
using Rock.Web.Cache;

namespace org.secc.Jobs
{
    [DataViewField( "DataView", "DataView of peope to remove the devices from.", true, "", "Rock.Model.Person" )]

    [DisallowConcurrentExecution]
    public class RemoveDevicesFromPersons : IJob
    {
        private readonly string authentication = string.Format( "authorization-token:{0}", GlobalAttributesCache.Value( "FrontporchAPIToken" ) );
        private readonly string host = GlobalAttributesCache.Value( "FrontporchHost" );

        public void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            var rockContext = new RockContext();

            var dv = dataMap.GetString( "DataView" ).AsGuid();
            DataViewService dataService = new DataViewService( rockContext );
            var items = dataService
                .Get( dv );
            List<string> list = new List<string>();
            var qry = items.GetQuery( null, null, out _ );
            var personalDeviceService = new PersonalDeviceService( rockContext );
            var persons = ( ( IQueryable<Person> ) qry ).Select( p => p.Id ).ToList();
            var devices = personalDeviceService.Queryable()
                .Where( d => persons.Contains( d.PersonAlias.PersonId ) )
                .ToList();

            var errors = new List<string>();

            foreach ( var device in devices )
            {
                device.PersonAliasId = null; //disaccociate the device but don't delete
                if ( device.MACAddress.IsNotNullOrWhiteSpace() )
                {
                    RemoveDeviceFromFrontPorch( device, errors );
                    device.MACAddress = null;
                }
                rockContext.SaveChanges();
            }

            context.Result = string.Format( "Removed {0} devices. {1} exceptions recorded", devices.Count(), errors.Count() );
        }

        private void RemoveDeviceFromFrontPorch( PersonalDevice device, List<string> errors )
        {
            var url = string.Format( "https://{0}/api/user/delete?mac={1}", host, device.MACAddress );
            HttpWebRequest request = ( HttpWebRequest ) WebRequest.Create( url );
            request.Headers.Add( authentication );
            try
            {
                HttpWebResponse response = ( HttpWebResponse ) request.GetResponse();
            }
            catch ( Exception e )
            {
                ExceptionLogService.LogException( e );
                errors.Add( e.Message );
            }
        }
    }
}

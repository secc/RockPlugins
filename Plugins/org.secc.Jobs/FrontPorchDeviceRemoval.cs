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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Jobs
{
    [SlidingDateRangeField( "Date Range", "The date range of transactions to include", true, "Previous|12|Month||" )]
    public class FrontPorchDeviceRemoval : IJob
    {
        int devicesProcessed = 0;
        int deviceErrors = 0;
        int deviceExceptions = 0;
        List<string> _processingErrors;
        List<string> _exceptionMsgs;


        public void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            var rockContext = new RockContext();
            _processingErrors = new List<string>();
            _exceptionMsgs = new List<string>();


            // Load all of the attributes
            DateRange dateRange = Rock.Web.UI.Controls.SlidingDateRangePicker.CalculateDateRangeFromDelimitedValues( dataMap.GetString( "DateRange" ) ?? "-12||" );

            var personalDevices = new PersonalDeviceService( rockContext ).Queryable()
                .Where( pd => pd.ModifiedDateTime <= dateRange.Start );

            if ( personalDevices.Count() > 0 )
            {
                foreach ( var personalDevice in personalDevices )
                {
                    //deleteDevice( personalDevice, rockContext );
                    string macAddress = personalDevice.MACAddress;
                    if ( macAddress != null )
                    {
                        var authToken = GlobalAttributesCache.Value( "APIAuthorizationToken" );
                        var hostAddr = GlobalAttributesCache.Value( "Host" );
                        var authentication = string.Format( "authorization-token:{0}", authToken );
                        var url = string.Format( "https://{0}/api/user/delete?mac={1}", hostAddr, macAddress );
                        if ( authToken.IsNotNullOrWhiteSpace() && hostAddr.IsNotNullOrWhiteSpace() )
                        {
                            HttpWebRequest request = ( HttpWebRequest ) WebRequest.Create( url );

                            try
                            {
                                HttpWebResponse response = ( HttpWebResponse ) request.GetResponse();

                                if ( response.StatusCode == HttpStatusCode.OK )
                                {
                                    devicesProcessed++;
                                    personalDevice.ModifiedDateTime = RockDateTime.Now;
                                    if ( personalDevice.MACAddress != null )
                                    {

                                        personalDevice.LoadAttributes();
                                        if ( personalDevice.Attributes.ContainsKey( "ArchivedMACAddress" ) )
                                        {
                                            personalDevice.SetAttributeValue( "ArchivedMACAddress", personalDevice.MACAddress );
                                        }
                                        personalDevice.MACAddress = null;

                                    }





                                }
                                else
                                {
                                    deviceErrors++;
                                    _processingErrors.Add( string.Format( "{0}", macAddress ) );

                                }
                            }
                            catch ( Exception ex )
                            {
                                string deviceDetails = string.Format( "{0}", macAddress );
                                _exceptionMsgs.Add( deviceDetails + ": " + ex.Message );
                                //throw new Exception( "Exception occurred processing workflow: " + deviceDetails, ex );


                            }
                        }
                    }
                    personalDevice.SaveAttributeValues( );
                   

                }

                rockContext.SaveChanges();

            }

            var resultMsg = new StringBuilder();
            resultMsg.AppendFormat( "{0} Devices processed", devicesProcessed );
            if ( deviceErrors > 0 )
            {
                resultMsg.AppendFormat( ", {0} devices reported an error", deviceErrors );
            }
            if ( deviceExceptions > 0 )
            {
                resultMsg.AppendFormat( ", {0} devices caused an exception", deviceExceptions );
            }
            if ( _processingErrors.Any() )
            {
                resultMsg.Append( Environment.NewLine + _processingErrors.AsDelimited( Environment.NewLine ) );
            }

            if ( _exceptionMsgs.Any() )
            {
                throw new Exception( "One or more exceptions occurred processing devices..." + Environment.NewLine + _exceptionMsgs.AsDelimited( Environment.NewLine ) );
            }

            context.Result = resultMsg.ToString();



        }
        public void deleteDevice( PersonalDevice personalDevice, RockContext rockContext )
        {
            // Check to see if a mac address exists, if so, delete from front Porch

            string macAddress = personalDevice.MACAddress;
            if ( macAddress != null )
            {
                var authToken = GlobalAttributesCache.Value( "APIAuthorizationToken" );
                var hostAddr = GlobalAttributesCache.Value( "Host" );
                var authentication = string.Format( "authorization-token:{0}", authToken );
                var url = string.Format( "https://{0}/api/user/delete?mac={1}", hostAddr, macAddress );
                if ( authToken.IsNotNullOrWhiteSpace() && hostAddr.IsNotNullOrWhiteSpace() )
                {
                    HttpWebRequest request = ( HttpWebRequest ) WebRequest.Create( url );

                    try
                    {
                        HttpWebResponse response = ( HttpWebResponse ) request.GetResponse();

                        if ( response.StatusCode == HttpStatusCode.OK )
                        {
                            devicesProcessed++;
                            personalDevice.ModifiedDateTime = RockDateTime.Now;
                            if ( personalDevice.MACAddress != null )
                            {

                                personalDevice.LoadAttributes();
                                if ( personalDevice.Attributes.ContainsKey( "ArchivedMACAddress" ) )
                                {
                                    personalDevice.SetAttributeValue( "ArchivedMACAddress", personalDevice.MACAddress );
                                }
                                personalDevice.MACAddress = null;
                                personalDevice.SaveAttributeValues( rockContext );
                                rockContext.SaveChanges();

                            }





                        }
                        else
                        {
                            deviceErrors++;
                            _processingErrors.Add( string.Format( "{0}", macAddress ) );

                        }
                    }
                    catch ( Exception ex )
                    {
                        string deviceDetails = string.Format( "{0}", macAddress );
                        _exceptionMsgs.Add( deviceDetails + ": " + ex.Message );
                        //throw new Exception( "Exception occurred processing workflow: " + deviceDetails, ex );


                    }
                }
            }
        }
    }
}


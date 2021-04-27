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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Net;
using org.secc.SystemsMonitor.Model;
using Rock;
using Rock.Attribute;
using Rock.Model;

namespace org.secc.SystemsMonitor.Component
{
    [Export( typeof( SystemTestComponent ) )]
    [ExportMetadata( "ComponentName", "Server Is Up" )]
    [Description( "Checks to see if web server is up." )]

    [UrlLinkField( "Url", "Url to check to see if the website is up", order: 0 )]
    [IntegerField( "Timeout", "Amount of time in seconds to allow for a response", DefaultIntegerValue = 30, Order = 1 )]

    public class ServerIsUp : SystemTestComponent
    {
        public override string Name => "Server Is Up";

        public override string Icon => "fa fa-laptop-code";

        public override List<AlarmCondition> SupportedAlarmConditions
        {
            get => new List<AlarmCondition> {
                AlarmCondition.Never,
                AlarmCondition.Fail,
            };
        }

        public override SystemTestResult RunTest( SystemTest test )
        {
            try
            {
                WebClientEx webClient = new WebClientEx
                {
                    Timeout = test.GetAttributeValue( "Timeout" ).AsInteger()
                };
                var response = webClient.OpenRead( test.GetAttributeValue( "Url" ) );

                return new SystemTestResult
                {
                    Passed = true
                };
            }
            catch
            {
                return new SystemTestResult
                {
                    Passed = false,
                    Message = "An exception occurred while trying to reach website."
                };
            }


        }

        public class WebClientEx : WebClient
        {
            public int Timeout { get; set; }

            protected override WebRequest GetWebRequest( Uri address )
            {
                var request = base.GetWebRequest( address );
                request.Timeout = Timeout * 10000;
                return request;
            }
        }
    }
}

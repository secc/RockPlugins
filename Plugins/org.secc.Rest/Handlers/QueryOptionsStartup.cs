// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Web.Http;
using Rock.Model;
using Rock.Utility;

namespace org.secc.Rest.Handlers
{
    /// <summary>
    /// Adds <see cref="QueryOptionsHandler"/> as the first message handler of Rock's Web API configuration at startup.
    /// </summary>
    public class QueryOptionsStartup : IRockStartup
    {
        /// <summary>
        /// Runs before the other startup classes.
        /// </summary>
        public int StartupOrder => int.MinValue;

        /// <summary>
        /// Inserts the handler at the front of the message handlers. Errors are logged and not rethrown, so the
        /// startup classes that run after this one still run.
        /// </summary>
        public void OnStartup()
        {
            try
            {
                GetHttpConfiguration().MessageHandlers.Insert( 0, new QueryOptionsHandler() );
            }
            catch ( Exception ex )
            {
                LogException( ex );
            }
        }

        /// <summary>
        /// The Web API configuration the handler is added to.
        /// </summary>
        protected virtual HttpConfiguration GetHttpConfiguration()
        {
            return GlobalConfiguration.Configuration;
        }

        /// <summary>
        /// Logs an error raised while adding the handler.
        /// </summary>
        protected virtual void LogException( Exception ex )
        {
            ExceptionLogService.LogException( ex );
        }
    }
}

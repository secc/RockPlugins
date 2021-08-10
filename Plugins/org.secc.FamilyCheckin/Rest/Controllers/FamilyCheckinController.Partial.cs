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
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using System.Web.SessionState;
using org.secc.FamilyCheckin.Cache;
using org.secc.FamilyCheckin.Rest.Handlers;
using org.secc.FamilyCheckin.Utilities;
using Rock;
using Rock.CheckIn;
using Rock.Model;
using Rock.Rest;
using Rock.Web.Cache;

namespace org.secc.FamilyCheckin.Rest.Controllers
{
    /// <summary>
    /// TaggedItems REST API
    /// </summary>
    public partial class FamilyCheckinController : ApiController, IHasCustomHttpRoutes
    {
        /// <summary>
        /// Add Custom route for flushing cached attributes
        /// </summary>
        /// <param name="routes"></param>
        public void AddRoutes( HttpRouteCollection routes )
        {
            RouteTable.Routes.MapHttpRoute(
                name: "FamiliesByPhone",
                routeTemplate: "api/org.secc/familycheckin/{action}/{param}",
                defaults: new
                {
                    controller = "familycheckin",
                    entityqualifier = RouteParameter.Optional,
                    entityqualifiervalue = RouteParameter.Optional
                } ).RouteHandler = new SessionRouteHandler();
        }

        /// <summary>
        /// Posts the specified entity type identifier.
        /// </summary>
        /// <param name="entityTypeId">The entity type identifier.</param>
        /// <param name="ownerId">The owner identifier.</param>
        /// <param name="entityGuid">The entity unique identifier.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public HttpResponseMessage Post( string phone )
        {

            return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, phone );
        }

        [HttpGet()]
        public HttpResponseMessage Family( string param )
        {
            try
            {
                var session = HttpContext.Current.Session;

                var localDeviceConfigCookie = HttpContext.Current.Request.Cookies[CheckInCookieKey.LocalDeviceConfig].Value;
                var localDevice = localDeviceConfigCookie.FromJsonOrNull<LocalDeviceConfiguration>();

                var currentKioskId = localDevice.CurrentKioskId.Value;
                Guid blockGuid = ( Guid ) session["BlockGuid"];
                var currentCheckInState = new CheckInState( localDevice );
                currentCheckInState.CheckIn.UserEnteredSearch = true;
                currentCheckInState.CheckIn.ConfirmSingleFamily = true;
                currentCheckInState.CheckIn.SearchType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_PHONE_NUMBER );
                currentCheckInState.CheckIn.SearchValue = param;

                var rockContext = new Rock.Data.RockContext();
                var block = BlockCache.Get( blockGuid );
                string workflowActivity = block.GetAttributeValue( "WorkflowActivity" );
                Guid? workflowGuid = block.GetAttributeValue( "WorkflowType" ).AsGuidOrNull();

                List<string> errors;
                var workflowService = new WorkflowService( rockContext );
                var workflowType = WorkflowTypeCache.Get( workflowGuid.Value );

                var CurrentWorkflow = Rock.Model.Workflow.Activate( workflowType, currentCheckInState.Kiosk.Device.Name, rockContext );

                var activityType = workflowType.ActivityTypes.Where( a => a.Name == workflowActivity ).FirstOrDefault();
                if ( activityType != null )
                {
                    WorkflowActivity.Activate( activityType, CurrentWorkflow, rockContext );
                    if ( workflowService.Process( CurrentWorkflow, currentCheckInState, out errors ) )
                    {
                        if ( errors.Any() )
                        {
                            var innerException = new Exception( string.Join( " -- ", errors ) );
                            ExceptionLogService.LogException( new Exception( "Process Mobile Checkin failed initial workflow. See inner exception for details.", innerException ) );
                        }

                        // Keep workflow active for continued processing
                        CurrentWorkflow.CompletedDateTime = null;
                        SaveState( session, currentCheckInState );
                        List<CheckInFamily> families = currentCheckInState.CheckIn.Families;
                        families = families.OrderBy( f => f.Caption ).ToList();
                        return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, families );
                    }
                    else
                    {
                        if ( errors.Any() )
                        {
                            var innerException = new Exception( string.Join( " -- ", errors ) );
                            ExceptionLogService.LogException( new Exception( "Process Mobile Checkin failed initial workflow. See inner exception for details.", innerException ) );
                        }
                        else
                        {
                            ExceptionLogService.LogException( new Exception( "Process Mobile Checkin failed initial workflow. See inner exception for details." ) );
                        }
                    }
                }
                else
                {
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.InternalServerError, string.Format( "Workflow type does not have a '{0}' activity type", workflowActivity ) );
                }
                return ControllerContext.Request.CreateResponse( HttpStatusCode.InternalServerError, String.Join( "\n", errors ) );

            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex, HttpContext.Current );
                return ControllerContext.Request.CreateResponse( HttpStatusCode.Forbidden, "Forbidden" );
            }
        }

        [HttpGet()]
        public HttpResponseMessage KioskStatus( int param )
        {
            CheckInState currentCheckInState;
            var kioskType = KioskTypeCache.Get( param );
            var Session = HttpContext.Current.Session;
            if ( Session["CheckInState"] != null )
            {
                currentCheckInState = Session["CheckInState"] as CheckInState;
            }
            else
            {
                return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, new Dictionary<string, bool> { { "active", false } } );
            }

            if ( kioskType == null
                || currentCheckInState == null
                || !kioskType.IsOpen()
                || currentCheckInState.Kiosk.FilteredGroupTypes( currentCheckInState.ConfiguredGroupTypes ).Count == 0
                || !currentCheckInState.Kiosk.HasLocations( currentCheckInState.ConfiguredGroupTypes ) )
            {
                return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, new Dictionary<string, bool> { { "active", false } } );
            }

            return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, new Dictionary<string, bool> { { "active", true } } );
        }

        [HttpGet()]
        public HttpResponseMessage ProcessMobileCheckin( string param )
        {
            try
            {
                var session = HttpContext.Current.Session;

                var currentCheckInState = session["CheckInState"] as CheckInState;
                if ( currentCheckInState.CheckIn.SearchType.Guid != Constants.CHECKIN_SEARCH_TYPE_USERLOGIN.AsGuid() )
                {
                    throw new Exception(); //We'll catch this later and return a forbidden
                }

                var localDeviceConfigCookie = HttpContext.Current.Request.Cookies[CheckInCookieKey.LocalDeviceConfig].Value;
                var localDevice = localDeviceConfigCookie.FromJsonOrNull<LocalDeviceConfiguration>();

                var rockContext = new Rock.Data.RockContext();

                UserLoginService userLoginService = new UserLoginService( rockContext );
                var family = userLoginService.Queryable().AsNoTracking()
                    .Where( u => u.UserName == currentCheckInState.CheckIn.SearchValue )
                    .Select( u => u.Person.PrimaryFamily )
                    .FirstOrDefault();
                var checkinFamily = new CheckInFamily
                {
                    Group = family.Clone( false ),
                    Caption = family.ToString(),
                    Selected = true
                };
                currentCheckInState.CheckIn.Families.Add( checkinFamily );
                SaveState( session, currentCheckInState );

                Guid blockGuid = ( Guid ) session["BlockGuid"];
                var block = BlockCache.Get( blockGuid );
                Guid? workflowGuid = block.GetAttributeValue( "WorkflowType" ).AsGuidOrNull();
                string workflowActivity = block.GetAttributeValue( "WorkflowActivity" );

                List<string> errors;
                var workflowService = new WorkflowService( rockContext );
                var workflowType = WorkflowTypeCache.Get( workflowGuid.Value );

                var CurrentWorkflow = Rock.Model.Workflow.Activate( workflowType, currentCheckInState.Kiosk.Device.Name, rockContext );

                var activityType = workflowType.ActivityTypes.Where( a => a.Name == workflowActivity ).FirstOrDefault();
                if ( activityType != null )
                {
                    WorkflowActivity.Activate( activityType, CurrentWorkflow, rockContext );
                    if ( workflowService.Process( CurrentWorkflow, currentCheckInState, out errors ) )
                    {
                        if ( errors.Any() )
                        {
                            var innerException = new Exception( string.Join( " -- ", errors ) );
                            ExceptionLogService.LogException( new Exception( "Process Mobile Checkin failed initial workflow. See inner exception for details.", innerException ) );
                        }

                        // Keep workflow active for continued processing
                        CurrentWorkflow.CompletedDateTime = null;
                        SaveState( session, currentCheckInState );
                        List<CheckInFamily> families = currentCheckInState.CheckIn.Families;
                        families = families.OrderBy( f => f.Caption ).ToList();
                        return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, param );
                    }
                    else
                    {
                        if ( errors.Any() )
                        {
                            var innerException = new Exception( string.Join( " -- ", errors ) );
                            ExceptionLogService.LogException( new Exception( "Process Mobile Checkin failed initial workflow. See inner exception for details.", innerException ) );
                        }
                        else
                        {
                            ExceptionLogService.LogException( new Exception( "Process Mobile Checkin failed initial workflow. See inner exception for details." ) );
                        }
                    }
                }
                else
                {
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.InternalServerError, string.Format( "Workflow type does not have a '{0}' activity type", workflowActivity ) );
                }
                return ControllerContext.Request.CreateResponse( HttpStatusCode.InternalServerError, String.Join( "\n", errors ) );

            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex, HttpContext.Current );
                return ControllerContext.Request.CreateResponse( HttpStatusCode.Forbidden, "Forbidden" );
            }
        }

        private void SaveState( HttpSessionState Session, CheckInState currentCheckInState )
        {
            if ( Session != null )
            {
                if ( currentCheckInState != null )
                {
                    Session["CheckInState"] = currentCheckInState;
                }
                else
                {
                    Session.Remove( "CheckInState" );
                }
            }

        }
    }
}
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
using System.Collections.Concurrent;
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
using Rock.Rest.Filters;
using Rock.Security;
using Rock.Web.Cache;

namespace org.secc.FamilyCheckin.Rest.Controllers
{
    /// <summary>
    /// Family Checkin REST API
    /// </summary>
    public partial class FamilyCheckinController : Rock.Rest.ApiControllerBase, IHasCustomHttpRoutes
    {
        /// <summary>
        /// Block type guid of the only block (QuickSearch) allowed to establish a
        /// session that may call the phone-number Family search (ROCK-8765). Mobile
        /// check-in sessions carry the MobileCheckinStart block guid and must not be
        /// able to run phone-number enumeration. Guid matches migration 024_FamilyCheckinPages.
        /// </summary>
        private static readonly Guid QuickSearchBlockTypeGuid = "315A175F-C682-4810-9F33-1BDB93904A4E".AsGuid();

        /// <summary>
        /// Sliding-window rate limit for phone searches, per ASP.NET session (ROCK-8765).
        /// </summary>
        private const int MaxSearchesPerWindow = 20;
        private static readonly TimeSpan SearchWindow = TimeSpan.FromSeconds( 60 );
        private static readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _searchHistory = new ConcurrentDictionary<string, ConcurrentQueue<DateTime>>();

        /// <summary>
        /// Add custom route (session-enabled so we can read check-in state).
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

        [HttpGet()]
        public HttpResponseMessage Family( string param )
        {
            try
            {
                var session = HttpContext.Current.Session;

                // ROCK-8765: Only sessions established by the kiosk QuickSearch block may
                // run a phone-number search. Mobile check-in sessions (or anything else
                // holding a check-in cookie) are rejected before any lookup happens.
                var blockGuidObject = session["BlockGuid"];
                if ( blockGuidObject == null )
                {
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.Forbidden, "Forbidden" );
                }

                Guid blockGuid = ( Guid ) blockGuidObject;
                var block = BlockCache.Get( blockGuid );
                if ( block == null
                    || block.BlockType == null
                    || block.BlockType.Guid != QuickSearchBlockTypeGuid )
                {
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.Forbidden, "Forbidden" );
                }

                // ROCK-8765: Enforce the block's phone-length rules server side. The kiosk
                // JS already enforces these, so legitimate traffic is unaffected; short
                // suffix searches are what make enumeration cheap.
                var digits = new string( ( param ?? string.Empty ).Where( char.IsDigit ).ToArray() );
                var minLength = block.GetAttributeValue( "MinimumPhoneNumberLength" ).AsIntegerOrNull() ?? 4;
                var maxLength = block.GetAttributeValue( "MaximumPhoneNumberLength" ).AsIntegerOrNull() ?? 10;
                if ( digits.Length < minLength || digits.Length > maxLength )
                {
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.BadRequest, "Invalid search value." );
                }

                // ROCK-8765: Rate limit per session to deny enumeration the volume it needs.
                if ( IsRateLimited( session.SessionID ) )
                {
                    return ControllerContext.Request.CreateResponse( ( HttpStatusCode ) 429, "Too many searches. Please wait a moment and try again." );
                }

                var localDeviceConfigCookie = Encryption.DecryptString( HttpContext.Current.Request.Cookies[CheckInCookieKey.LocalDeviceConfig].Value );
                var localDevice = localDeviceConfigCookie.FromJsonOrNull<LocalDeviceConfiguration>();

                var currentKioskId = localDevice.CurrentKioskId.Value;
                var currentCheckInState = new CheckInState( localDevice );
                currentCheckInState.CheckIn.UserEnteredSearch = true;
                currentCheckInState.CheckIn.ConfirmSingleFamily = true;
                currentCheckInState.CheckIn.SearchType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_PHONE_NUMBER );
                currentCheckInState.CheckIn.SearchValue = digits;

                var rockContext = new Rock.Data.RockContext();
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

                        // ROCK-8765: Return only the fields the kiosk UI consumes rather
                        // than fully serialized CheckInFamily graphs. Full family/person
                        // data stays server side in session state; ChooseFamily postbacks
                        // look families up by Group.Id from that state.
                        var families = currentCheckInState.CheckIn.Families
                            .OrderBy( f => f.Caption )
                            .Select( f => new
                            {
                                Caption = f.Caption,
                                SubCaption = f.SubCaption,
                                Group = new { Id = f.Group.Id }
                            } )
                            .ToList();
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
            // ROCK-8765: Intentionally anonymous — physical kiosks are unauthenticated
            // browsers and this returns only an active/inactive boolean. It still
            // requires an established check-in session below.
            CheckInState currentCheckInState;
            var kioskType = CheckinKioskTypeCache.Get( param );
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

        [Authenticate, Secured]
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
                var target = userLoginService.Queryable().AsNoTracking()
                    .Where( u => u.UserName == currentCheckInState.CheckIn.SearchValue )
                    .Select( u => new { u.PersonId, Family = u.Person.PrimaryFamily } )
                    .FirstOrDefault();

                if ( target == null || target.Family == null )
                {
                    throw new Exception(); //We'll catch this later and return a forbidden
                }

                // ROCK-8765: The authenticated caller must be the person whose session
                // this is. MobileCheckinStart's admin/debug impersonation (?UserName=) is
                // still allowed for Rock administrators.
                var currentPerson = GetPerson();
                if ( currentPerson == null
                    || ( target.PersonId != currentPerson.Id && !IsRockAdministrator( currentPerson.Id, rockContext ) ) )
                {
                    throw new Exception(); //We'll catch this later and return a forbidden
                }

                var family = target.Family;
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

        /// <summary>
        /// Sliding-window rate limiter keyed by session id (ROCK-8765).
        /// </summary>
        private static bool IsRateLimited( string sessionId )
        {
            var now = RockDateTime.Now;
            var queue = _searchHistory.GetOrAdd( sessionId, _ => new ConcurrentQueue<DateTime>() );

            // Drop entries outside the window.
            DateTime timestamp;
            while ( queue.TryPeek( out timestamp ) && now - timestamp > SearchWindow )
            {
                queue.TryDequeue( out timestamp );
            }

            if ( queue.Count >= MaxSearchesPerWindow )
            {
                return true;
            }

            queue.Enqueue( now );

            // Opportunistic cleanup so abandoned sessions don't accumulate forever.
            if ( _searchHistory.Count > 1000 )
            {
                foreach ( var key in _searchHistory.Keys )
                {
                    ConcurrentQueue<DateTime> stale;
                    if ( _searchHistory.TryGetValue( key, out stale )
                        && ( !stale.TryPeek( out timestamp ) || now - timestamp > SearchWindow ) )
                    {
                        _searchHistory.TryRemove( key, out stale );
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Whether the person is an active member of the Rock Administration security role (ROCK-8765).
        /// </summary>
        private static bool IsRockAdministrator( int personId, Rock.Data.RockContext rockContext )
        {
            var adminGroupGuid = Rock.SystemGuid.Group.GROUP_ADMINISTRATORS.AsGuid();
            return new GroupMemberService( rockContext ).Queryable().AsNoTracking()
                .Any( gm => gm.Group.Guid == adminGroupGuid
                    && gm.PersonId == personId
                    && gm.GroupMemberStatus == GroupMemberStatus.Active );
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

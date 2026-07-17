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
using System.Threading;
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
        /// A physical kiosk is one browser session shared by the whole line of families,
        /// so the cap is set well above a human line's peak (~a search every few seconds,
        /// with typo retries) while still far below an automated enumeration loop. This is
        /// defense-in-depth over the accepted physical-kiosk residual risk documented in the
        /// README; per-session keying means the limit can be reset by minting a new session,
        /// which is acceptable for that already-gated threat.
        /// </summary>
        private const int MaxSearchesPerWindow = 60;
        private static readonly TimeSpan SearchWindow = TimeSpan.FromSeconds( 60 );
        private static readonly ConcurrentDictionary<string, Queue<DateTime>> _searchHistory = new ConcurrentDictionary<string, Queue<DateTime>>();
        private static long _lastCleanupTicks = 0;

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

                // ROCK-8765: Enforce the block's phone-length rules server side. Short
                // suffix searches are what make enumeration cheap. Over-length input is
                // normalized rather than rejected so legitimate entries the kiosk allows
                // (e.g. an 11-digit number with a leading country "1", which the on-screen
                // keypad can produce past the textbox MaxLength) still search instead of
                // erroring the kiosk out.
                var digits = PhoneNumber.CleanNumber( param ) ?? string.Empty;
                var minLength = block.GetAttributeValue( "MinimumPhoneNumberLength" ).AsIntegerOrNull() ?? 4;
                var maxLength = block.GetAttributeValue( "MaximumPhoneNumberLength" ).AsIntegerOrNull() ?? 10;

                // Drop a leading country "1" then trim to the max so a valid long-form
                // number collapses to the same search value the kiosk would send short.
                if ( digits.Length == maxLength + 1 && digits[0] == '1' )
                {
                    digits = digits.Substring( 1 );
                }
                if ( digits.Length > maxLength )
                {
                    digits = digits.Substring( digits.Length - maxLength );
                }
                if ( digits.Length < minLength )
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
                            ExceptionLogService.LogException( new Exception( "Family phone search failed initial workflow. See inner exception for details.", innerException ) );
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
                            ExceptionLogService.LogException( new Exception( "Family phone search failed initial workflow. See inner exception for details.", innerException ) );
                        }
                        else
                        {
                            ExceptionLogService.LogException( new Exception( "Family phone search failed initial workflow. See inner exception for details." ) );
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

                // ROCK-8765: Missing/expired session or a non-UserLogin search is an
                // expected unauthorized case, not an error — return Forbidden directly
                // instead of throwing (which would log noise as a fake exception).
                var currentCheckInState = session["CheckInState"] as CheckInState;
                if ( currentCheckInState == null
                    || currentCheckInState.CheckIn.SearchType == null
                    || currentCheckInState.CheckIn.SearchType.Guid != Constants.CHECKIN_SEARCH_TYPE_USERLOGIN.AsGuid() )
                {
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.Forbidden, "Forbidden" );
                }

                var rockContext = new Rock.Data.RockContext();

                UserLoginService userLoginService = new UserLoginService( rockContext );
                var target = userLoginService.Queryable().AsNoTracking()
                    .Where( u => u.UserName == currentCheckInState.CheckIn.SearchValue )
                    .Select( u => new { u.PersonId, Family = u.Person.PrimaryFamily } )
                    .FirstOrDefault();

                if ( target == null || target.Family == null )
                {
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.Forbidden, "Forbidden" );
                }

                var blockGuidObject = session["BlockGuid"];
                var block = blockGuidObject != null ? BlockCache.Get( ( Guid ) blockGuidObject ) : null;
                if ( block == null )
                {
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.Forbidden, "Forbidden" );
                }

                // ROCK-8765: The authenticated caller must be the person whose session
                // this is. MobileCheckinStart's ?UserName= impersonation is preserved by
                // matching that block's own gate (MobileCheckinStart.ascx.cs): block
                // ADMINISTRATE authorization, or the block's DebugMode attribute.
                var currentPerson = GetPerson();
                if ( currentPerson == null )
                {
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.Forbidden, "Forbidden" );
                }

                var isImpersonationAllowed = block.IsAuthorized( Rock.Security.Authorization.ADMINISTRATE, currentPerson )
                    || block.GetAttributeValue( "DebugMode" ).AsBoolean();
                if ( target.PersonId != currentPerson.Id && !isImpersonationAllowed )
                {
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.Forbidden, "Forbidden" );
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
        /// Sliding-window rate limiter keyed by session id (ROCK-8765). Trim, count-check
        /// and enqueue happen under a lock on the per-session queue so concurrent requests
        /// for one session can't slip past the cap.
        /// </summary>
        private static bool IsRateLimited( string sessionId )
        {
            var now = RockDateTime.Now;
            var queue = _searchHistory.GetOrAdd( sessionId, _ => new Queue<DateTime>() );

            lock ( queue )
            {
                // Drop entries outside the window.
                while ( queue.Count > 0 && now - queue.Peek() > SearchWindow )
                {
                    queue.Dequeue();
                }

                if ( queue.Count >= MaxSearchesPerWindow )
                {
                    return true;
                }

                queue.Enqueue( now );
            }

            CleanupStaleSessions( now );
            return false;
        }

        /// <summary>
        /// Evict sessions with no in-window activity so abandoned sessions don't accumulate.
        /// Time-gated so the O(n) sweep runs at most once per window regardless of load, and
        /// only removes queues that are empty after trimming — never an active session's
        /// in-window entries (ROCK-8765).
        /// </summary>
        private static void CleanupStaleSessions( DateTime now )
        {
            var lastCleanup = new DateTime( Interlocked.Read( ref _lastCleanupTicks ), DateTimeKind.Unspecified );
            if ( now - lastCleanup < SearchWindow )
            {
                return;
            }

            // Claim the sweep; if another thread beat us to it, skip.
            if ( Interlocked.CompareExchange( ref _lastCleanupTicks, now.Ticks, lastCleanup.Ticks ) != lastCleanup.Ticks )
            {
                return;
            }

            foreach ( var key in _searchHistory.Keys )
            {
                Queue<DateTime> queue;
                if ( !_searchHistory.TryGetValue( key, out queue ) )
                {
                    continue;
                }

                bool empty;
                lock ( queue )
                {
                    while ( queue.Count > 0 && now - queue.Peek() > SearchWindow )
                    {
                        queue.Dequeue();
                    }
                    empty = queue.Count == 0;
                }

                if ( empty )
                {
                    _searchHistory.TryRemove( key, out queue );
                }
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

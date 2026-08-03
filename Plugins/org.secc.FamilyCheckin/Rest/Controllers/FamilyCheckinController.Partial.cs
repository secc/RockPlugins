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
        private static readonly Guid QuickSearchBlockTypeGuid = Constants.BLOCK_TYPE_QUICKSEARCH.AsGuid();

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
        private const string SearchHistorySessionKey = "FamilySearchHistory";

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
                // suffix searches are what make enumeration cheap.
                var digits = PhoneNumber.CleanNumber( param ) ?? string.Empty;
                var minLength = block.GetAttributeValue( "MinimumPhoneNumberLength" ).AsIntegerOrNull() ?? 4;
                var maxLength = block.GetAttributeValue( "MaximumPhoneNumberLength" ).AsIntegerOrNull() ?? 10;

                // Drop a leading country "1" (e.g. an 11-digit number, which the on-screen
                // keypad can produce past the textbox MaxLength) so a valid long-form
                // number still searches.
                if ( digits.Length == maxLength + 1 && digits[0] == '1' )
                {
                    digits = digits.Substring( 1 );
                }

                // Anything still over-length can't match a stored number. Mirror the
                // kiosk's no-results UX rather than trimming to a substring — a trimmed
                // value is a *different* phone number and can surface a family the user
                // never typed.
                if ( digits.Length > maxLength )
                {
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, new List<object>() );
                }
                if ( digits.Length < minLength )
                {
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.BadRequest, "Invalid search value." );
                }

                // ROCK-8765: Rate limit per session to deny enumeration the volume it needs.
                if ( IsRateLimited( session ) )
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

                return RunCheckinWorkflow( block, currentCheckInState, rockContext, session, "Family phone search", () =>
                {
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
                } );
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

                var blockGuidObject = session["BlockGuid"];
                var block = blockGuidObject != null ? BlockCache.Get( ( Guid ) blockGuidObject ) : null;
                if ( block == null )
                {
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.Forbidden, "Forbidden" );
                }

                // ROCK-8765: The caller must be the person whose session this is, judged by
                // the same gate as MobileCheckinStart's ?UserName= impersonation (shared
                // helper): an authenticated caller with block ADMINISTRATE, or any
                // authenticated caller while the block's DebugMode attribute is on.
                // Anonymous callers are always rejected — debug/load-test runs must
                // authenticate (e.g. a .ROCK auth cookie in the JMeter plan). All
                // in-memory checks run before the UserLogin lookup so rejected calls
                // never pay the DB round-trip.
                var currentPerson = GetPerson();
                if ( currentPerson == null )
                {
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.Forbidden, "Forbidden" );
                }

                var isImpersonationAllowed = MobileCheckinAuthorization.IsImpersonationAllowed( block, currentPerson );

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

                if ( !isImpersonationAllowed && target.PersonId != currentPerson.Id )
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

                return RunCheckinWorkflow( block, currentCheckInState, rockContext, session, "Process Mobile Checkin",
                    () => ControllerContext.Request.CreateResponse( HttpStatusCode.OK, param ) );
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex, HttpContext.Current );
                return ControllerContext.Request.CreateResponse( HttpStatusCode.Forbidden, "Forbidden" );
            }
        }

        /// <summary>
        /// Sliding-window rate limiter with history stored in session state (ROCK-8765).
        /// The route's SessionRouteHandler runs with full session state, so ASP.NET already
        /// serializes concurrent requests within one session — no locking needed — and
        /// session expiry evicts the history for free. Timestamps are UTC so a DST
        /// fall-back can't leave future-stamped entries pinning the window shut.
        /// </summary>
        private static bool IsRateLimited( HttpSessionState session )
        {
            var now = DateTime.UtcNow;
            var history = session[SearchHistorySessionKey] as Queue<DateTime> ?? new Queue<DateTime>();

            // Drop entries outside the window.
            while ( history.Count > 0 && now - history.Peek() > SearchWindow )
            {
                history.Dequeue();
            }

            if ( history.Count >= MaxSearchesPerWindow )
            {
                return true;
            }

            history.Enqueue( now );
            session[SearchHistorySessionKey] = history;
            return false;
        }

        /// <summary>
        /// Shared activate-workflow / process / log / save-state block for the Family and
        /// ProcessMobileCheckin handlers, so the two paths can't drift. On success the
        /// workflow is kept active (CompletedDateTime = null), state is saved to session,
        /// and <paramref name="onSuccess"/> builds the response.
        /// </summary>
        private HttpResponseMessage RunCheckinWorkflow( BlockCache block, CheckInState currentCheckInState, Rock.Data.RockContext rockContext, HttpSessionState session, string logContext, Func<HttpResponseMessage> onSuccess )
        {
            string workflowActivity = block.GetAttributeValue( "WorkflowActivity" );
            Guid? workflowGuid = block.GetAttributeValue( "WorkflowType" ).AsGuidOrNull();

            List<string> errors;
            var workflowService = new WorkflowService( rockContext );
            var workflowType = workflowGuid.HasValue ? WorkflowTypeCache.Get( workflowGuid.Value ) : null;
            if ( workflowType == null )
            {
                // A missing/invalid WorkflowType is a block misconfiguration, not an
                // authorization failure — surface it as a 500 (matching the
                // activityType == null branch below) instead of letting the outer
                // catch mask it as a 403.
                return ControllerContext.Request.CreateResponse( HttpStatusCode.InternalServerError, "Block is missing a valid WorkflowType configuration." );
            }

            var currentWorkflow = Rock.Model.Workflow.Activate( workflowType, currentCheckInState.Kiosk.Device.Name, rockContext );

            var activityType = workflowType.ActivityTypes.Where( a => a.Name == workflowActivity ).FirstOrDefault();
            if ( activityType == null )
            {
                return ControllerContext.Request.CreateResponse( HttpStatusCode.InternalServerError, string.Format( "Workflow type does not have a '{0}' activity type", workflowActivity ) );
            }

            WorkflowActivity.Activate( activityType, currentWorkflow, rockContext );
            var processed = workflowService.Process( currentWorkflow, currentCheckInState, out errors );

            if ( errors.Any() || !processed )
            {
                var message = logContext + " failed initial workflow. See inner exception for details.";
                var exception = errors.Any()
                    ? new Exception( message, new Exception( string.Join( " -- ", errors ) ) )
                    : new Exception( message );
                ExceptionLogService.LogException( exception );
            }

            if ( !processed )
            {
                return ControllerContext.Request.CreateResponse( HttpStatusCode.InternalServerError, String.Join( "\n", errors ) );
            }

            // Keep workflow active for continued processing
            currentWorkflow.CompletedDateTime = null;
            SaveState( session, currentCheckInState );
            return onSuccess();
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

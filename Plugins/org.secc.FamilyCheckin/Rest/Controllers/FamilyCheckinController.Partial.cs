using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

using Rock;
using Rock.Model;
using Rock.Rest.Filters;
using Rock.Rest;
using Rock.CheckIn;
using System.Web;
using Rock.Web.Cache;
using System.Collections.Generic;
using System.Web.SessionState;

namespace org.secc.FamilyCheckin.Rest.Controllers
{
    /// <summary>
    /// TaggedItems REST API
    /// </summary>
    public partial class FamilyCheckinController :  ApiController, IHasCustomRoutes, IRequiresSessionState
    {

        protected CheckInState CurrentCheckInState;

        protected int? CurrentKioskId { get; set; }

        /// <summary>
        /// Add Custom route for flushing cached attributes
        /// </summary>
        /// <param name="routes"></param>
        public void AddRoutes( System.Web.Routing.RouteCollection routes )
        {
            routes.MapHttpRoute(
                name: "FamiliesByPhone",
                routeTemplate: "api/family/{kioskValue}/{blockGuid}/{phone}",
                defaults: new
                {
                    controller = "familycheckin",
                    entityqualifier = RouteParameter.Optional,
                    entityqualifiervalue = RouteParameter.Optional
                } );
        }

        /// <summary>
        /// Posts the specified entity type identifier.
        /// </summary>
        /// <param name="entityTypeId">The entity type identifier.</param>
        /// <param name="ownerId">The owner identifier.</param>
        /// <param name="entityGuid">The entity unique identifier.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public HttpResponseMessage Post(int kioskValue, Guid blockGuid, string phone)
        {
            
            return ControllerContext.Request.CreateResponse(HttpStatusCode.OK, phone);
        }
        
        public HttpResponseMessage Get(int kioskValue, Guid blockGuid, string phone)
        {
            CurrentKioskId = kioskValue;
            List<int> CheckInGroupTypeIds = new List<int>();
            CurrentCheckInState = new CheckInState(kioskValue, CheckInGroupTypeIds);
            CurrentCheckInState.CheckIn.UserEnteredSearch = true;
            CurrentCheckInState.CheckIn.ConfirmSingleFamily = true;
            CurrentCheckInState.CheckIn.SearchType = DefinedValueCache.Read(Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_PHONE_NUMBER);
            CurrentCheckInState.CheckIn.SearchValue = phone;
            List<CheckInFamily> families = CurrentCheckInState.CheckIn.Families;

            var rockContext = new Rock.Data.RockContext();
            var block = BlockCache.Read(blockGuid, rockContext);
            string workflowActivity = block.GetAttributeValue("WorkflowActivity");
            Guid? guid = block.GetAttributeValue("WorkflowType").AsGuidOrNull();

            List<string> errors;
            var workflowTypeService = new WorkflowTypeService(rockContext);
            var workflowService = new WorkflowService(rockContext);
            var workflowType = workflowTypeService.Queryable("ActivityTypes")
                .Where(w => w.Guid.Equals(guid.Value))
                .FirstOrDefault();
            var CurrentWorkflow = Rock.Model.Workflow.Activate(workflowType, CurrentCheckInState.Kiosk.Device.Name, rockContext);

            var activityType = workflowType.ActivityTypes.Where(a => a.Name == workflowActivity).FirstOrDefault();
            if (activityType != null)
            {
                WorkflowActivity.Activate(activityType, CurrentWorkflow, rockContext);
                if (workflowService.Process(CurrentWorkflow, CurrentCheckInState, out errors))
                {
                    // Keep workflow active for continued processing
                    CurrentWorkflow.CompletedDateTime = null;
                    SaveState();
                    return ControllerContext.Request.CreateResponse(HttpStatusCode.OK, families);
                }
            }
            else
            {
                return ControllerContext.Request.CreateResponse(HttpStatusCode.InternalServerError, string.Format("Workflow type does not have a '{0}' activity type", workflowActivity));
            }
            return ControllerContext.Request.CreateResponse(HttpStatusCode.InternalServerError, String.Join("\n", errors));

        }
        protected void SaveState()
        {
            var Session = HttpContext.Current.Session;

            if(Session != null)
            {
                if (CurrentCheckInState != null)
                {
                    Session["CheckInState"] = CurrentCheckInState;
                }
                else
                {
                    Session.Remove("CheckInState");
                }
            }

        }
    }
}
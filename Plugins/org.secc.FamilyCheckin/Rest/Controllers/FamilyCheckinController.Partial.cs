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
using org.secc.FamilyCheckin.Rest.Handlers;
using System.Web.SessionState;

namespace org.secc.FamilyCheckin.Rest.Controllers
{
    /// <summary>
    /// TaggedItems REST API
    /// </summary>
    public partial class FamilyCheckinController :  ApiController, IHasCustomRoutes
    {

        protected CheckInState CurrentCheckInState;

        protected int CurrentKioskId { get; set; }

        /// <summary>
        /// Add Custom route for flushing cached attributes
        /// </summary>
        /// <param name="routes"></param>
        public void AddRoutes( System.Web.Routing.RouteCollection routes )
        {
            routes.MapHttpRoute(
                name: "FamiliesByPhone",
                routeTemplate: "api/org.secc/familycheckin/family/{phone}",
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
        public HttpResponseMessage Post(string phone)
        {
            
            return ControllerContext.Request.CreateResponse(HttpStatusCode.OK, phone);
        }
        
        public HttpResponseMessage Get(string phone)
        {
            try
            {
                var Session = HttpContext.Current.Session;

                CurrentKioskId = (int)Session["CheckInKioskId"];
                Guid blockGuid = (Guid)Session["BlockGuid"];
                List<int> CheckInGroupTypeIds = (List<int>)Session["CheckInGroupTypeIds"];
                CurrentCheckInState = new CheckInState(CurrentKioskId, CheckInGroupTypeIds);
                CurrentCheckInState.CheckIn.UserEnteredSearch = true;
                CurrentCheckInState.CheckIn.ConfirmSingleFamily = true;
                CurrentCheckInState.CheckIn.SearchType = DefinedValueCache.Read(Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_PHONE_NUMBER);
                CurrentCheckInState.CheckIn.SearchValue = phone;
                

                var rockContext = new Rock.Data.RockContext();
                var block = BlockCache.Read(blockGuid, rockContext);
                string workflowActivity = block.GetAttributeValue("WorkflowActivity");
                Guid? workflowGuid = block.GetAttributeValue("WorkflowType").AsGuidOrNull();

                List<string> errors;
                var workflowTypeService = new WorkflowTypeService(rockContext);
                var workflowService = new WorkflowService(rockContext);
                var workflowType = workflowTypeService.Queryable("ActivityTypes")
                    .Where(w => w.Guid.Equals(workflowGuid.Value))
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
                        SaveState(Session);
                        List<CheckInFamily> families = CurrentCheckInState.CheckIn.Families;
                        return ControllerContext.Request.CreateResponse(HttpStatusCode.OK, families);
                    }
                }
                else
                {
                    return ControllerContext.Request.CreateResponse(HttpStatusCode.InternalServerError, string.Format("Workflow type does not have a '{0}' activity type", workflowActivity));
                }
                return ControllerContext.Request.CreateResponse(HttpStatusCode.InternalServerError, String.Join("\n", errors));

            }
            catch
            {
                return ControllerContext.Request.CreateResponse(HttpStatusCode.Forbidden, "Forbidden");
            }
        }

        private void SaveState(HttpSessionState Session)
        {
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
using System;
using System.Linq;
using System.Web.Http;
using org.secc.Cms.ViewModels;
using org.secc.Warehouse.Model;
using Rock.Data;
using Rock.Rest;

namespace org.secc.Cms.Controllers
{
    public partial class DailyInteractionController : ApiControllerBase
    {
        [System.Web.Http.Route( "api/cms/dailyinteraction/{pageId}" )]
        public IHttpActionResult GetInteractions( int pageId, DateTime? startDate = null, DateTime? endDate = null )
        {
            var rockContext = new RockContext();
            var dailyInteractionService = new DailyInteractionService( rockContext );


            var qry = dailyInteractionService.Queryable()
                .Where( d => d.PageId == pageId );

            if ( startDate.HasValue && endDate.HasValue )
            {
                qry = qry
                    .Where( d => d.Date >= startDate.Value )
                    .Where( d => d.Date <= endDate.Value );
            }

            var interactions = qry.ToList()
                .Select( i => new InteractionViewModel( i ) )
                .ToList();

            return Json( interactions );
        }
    }
}

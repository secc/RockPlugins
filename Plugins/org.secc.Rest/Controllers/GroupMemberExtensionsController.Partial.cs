using System;
using System.Linq;
using System.Net;
using System.Net.Http;

using Rock;
using System.Web.Http;
using Rock.Data;
using Rock.Model;
using Rock.Rest.Filters;
using System.Web;
using System.IO;
using Rock.Utility;
using System.Web.Http.OData.Query;
using System.Collections.Generic;
using System.Reflection;

namespace org.secc.Rest.Controllers
{
    /// <summary>
    /// REST API for GroupMembers
    /// </summary>
    public partial class GroupMemberExtensionsController : Rock.Rest.ApiControllerBase
    {

        /// <summary>
        /// Custom OData controller method which supports expanding GroupMembers
        /// </summary>
        /// <returns>A queryable collection of GroupMembers, including expansions, that match the provided query.</returns>
        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route("api/GroupMembers/OData")]
        public IQueryable<GroupMember> OData(ODataQueryOptions opts)
        {
            
            var settings = new ODataValidationSettings()
            {
                // Initialize settings as needed.
                AllowedFunctions = AllowedFunctions.AllMathFunctions
            };
            opts.Validate(settings);
            GroupMemberService service = new GroupMemberService(new RockContext());
            service.Context.Configuration.ProxyCreationEnabled = false;
            IQueryable results = opts.ApplyTo(service.Queryable());
            var retList = new List<GroupMember>();
            foreach (var result in results)
            {
                if (result is GroupMember)
                {
                    retList.Add((GroupMember)result);
                }
                else
                {
                    PropertyInfo propertyInfo = result.GetType().GetProperty("Instance");
                    if (propertyInfo != null)
                    {
                        retList.Add((GroupMember)propertyInfo.GetValue(result));
                    }
                }
            }
            return retList.AsQueryable();
        }
    }
}
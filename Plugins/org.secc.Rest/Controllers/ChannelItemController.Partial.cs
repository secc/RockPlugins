using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using org.secc.Rest.Models;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest;
using Rock.Web.Cache;
using System.Linq.Dynamic;
using Rock.Security;
using System.Net;
using System.Net.Http;

namespace org.secc.Rest.Controllers
{
    public class ChannelItemController : ApiControllerBase
    {
        int contentChanelItemEntityTypeId = EntityTypeCache.Get( typeof( Rock.Model.ContentChannelItem ) ).Id;

        [HttpGet]
        [System.Web.Http.Route( "api/ChannelItems/{contentChannelId}" )]
        [System.Web.Http.Route( "api/ChannelItems/{contentChannelId}/{tag}" )]
        public IHttpActionResult ChannelItems( int contentChannelId, string tag = "", int take = 20, int page = 0, bool hideInactive = false,
                        string orderby = "StartDateTime", bool reverse = false )
        {
            var cacheKey = Request.RequestUri.ToString();

            RockContext rockContext = new RockContext();
            ContentChannelItemService contentChannelItemService = new ContentChannelItemService( rockContext );
            ContentChannelService contentChannelService = new ContentChannelService( rockContext );
            var contentChannel = contentChannelService.Get( contentChannelId );

            if ( !contentChannel.IsAuthorized( Rock.Security.Authorization.VIEW, GetPerson() ) )
            {
                throw new HttpResponseException( HttpStatusCode.Unauthorized );
            }

            var contentItems = contentChannelItemService.Queryable().Where( c => c.ContentChannelId == contentChannelId );

            if ( hideInactive )
            {
                contentItems = contentItems.Where( i => i.Status == ContentChannelItemStatus.Approved );
            }

            if ( tag.IsNotNullOrWhiteSpace() )
            {
                TagService tagService = new TagService( rockContext );
                var taggedItemGuids = tagService.Queryable().Where( i => i.EntityTypeId == contentChanelItemEntityTypeId && i.Name == tag )
                    .SelectMany( t => t.TaggedItems ).Select( ti => ti.EntityGuid );
                contentItems = contentItems.Where( c => taggedItemGuids.Contains( c.Guid ) );
            }

            if ( reverse )
            {
                contentItems = contentItems.OrderByDescending( orderby );
            }
            else
            {
                contentItems = contentItems.OrderBy( orderby );
            }

            var contentChannelTypeId = contentChannel.ContentChannelTypeId.ToString();
            var contentChanelIdString = contentChannel.Id.ToString();

            var attributeQry = new AttributeService( rockContext ).Queryable()
                .Where( a => a.EntityTypeId == contentChanelItemEntityTypeId
                    && ( ( a.EntityTypeQualifierColumn == "ContentChannelTypeId" && a.EntityTypeQualifierValue == contentChannelTypeId )
                    || ( a.EntityTypeQualifierColumn == "ContentChannelId" && a.EntityTypeQualifierValue == contentChanelIdString ) ) )
                    ;

            var attributeValueQry = new AttributeValueService( rockContext ).Queryable()
                .Where( av => attributeQry.Select( a => a.Id ).Contains( av.AttributeId ) );

            //AttributeFiltering
            var ignoreKeys = new List<string> { "contentchannelid", "tag", "take", "page", "hideInactive", "orderby", "reverse" };

            var urlKeyValues = ControllerContext.Request.GetQueryNameValuePairs();
            foreach ( var pair in urlKeyValues )
            {
                if ( !ignoreKeys.Contains( pair.Key.ToLower() ) )
                {
                    var filterQry = new AttributeValueService( rockContext ).Queryable()
                       .Where( av => attributeQry.Where( a => a.Key == pair.Key ).Select( a => a.Id ).Contains( av.AttributeId ) )
                       .Where( av => av.Value == pair.Value );

                    contentItems = contentItems.Where( i => filterQry.Select( av => av.EntityId ).Contains( i.Id ) );
                }
            }

            //Pagination
            if ( page > 0 )
            {
                var skip = ( page - 1 ) * take;
                contentItems = contentItems.Skip( skip );
                contentItems = contentItems.Take( take );
            }

            //Join attributes and other content channel items
            var complex = contentItems
                .GroupJoin( attributeValueQry,
                i => i.Id,
                av => av.EntityId,
                ( i, av ) => new
                {
                    ContentChanelItem = i,
                    Children = i.ChildItems.OrderBy( ci => ci.Order ).Select( ci => ci.ChildContentChannelItemId ),
                    ParentItems = i.ParentItems.Select( ci => ci.ContentChannelItemId ),
                    AttributeValues = av
                } );

            var items = complex.ToList()
                .Select( i => new ChannelItem
                {
                    Id = i.ContentChanelItem.Id,
                    DateTime = i.ContentChanelItem.StartDateTime,
                    Title = i.ContentChanelItem.Title,
                    Content = i.ContentChanelItem.Content,
                    Priority = i.ContentChanelItem.Priority,
                    Order = i.ContentChanelItem.Order,
                    Attributes = i.AttributeValues.ToDictionary( a => a.AttributeKey, a => a.Value ),
                    Tags = GetTags( i.ContentChanelItem, rockContext ),
                    Slug = i.ContentChanelItem.PrimarySlug,
                    CreatedBy = i.ContentChanelItem.CreatedByPersonName,
                    ChildItems = i.Children.ToList(),
                    ParentItems = i.ParentItems.ToList()
                } ).ToList();

            return Json( items );
        }

        [HttpGet]
        [System.Web.Http.Route( "api/ChannelItem/{identifier}" )]
        public IHttpActionResult ChannelItem( string identifier )
        {
            RockContext rockContext = new RockContext();
            ContentChannelItemService contentChannelItemService = new ContentChannelItemService( rockContext );

            IQueryable<ContentChannelItem> contentChannelItem;

            var itemId = identifier.AsInteger();
            if ( itemId != 0 )
            {
                contentChannelItem = contentChannelItemService.Queryable().Where( i => i.Id == itemId );
            }
            else
            {
                //Get by slug
                ContentChannelItemSlugService contentChannelItemSlugService = new ContentChannelItemSlugService( rockContext );
                contentChannelItem = contentChannelItemSlugService.Queryable()
                    .Where( s => s.Slug == identifier )
                    .Select( s => s.ContentChannelItem );
            }

            var contentChannel = contentChannelItem.Select( i => i.ContentChannel ).FirstOrDefault();

            if ( contentChannel == null )
            {
                throw new HttpResponseException( HttpStatusCode.NotFound );
            }

            if ( !contentChannel.IsAuthorized( Rock.Security.Authorization.VIEW, GetPerson() ) )
            {
                throw new HttpResponseException( HttpStatusCode.Unauthorized );
            }

            var contentChannelTypeId = contentChannel.ContentChannelTypeId.ToString();
            var contentChanelIdString = contentChannel.Id.ToString();

            var attributeQry = new AttributeService( rockContext ).Queryable()
                .Where( a => a.EntityTypeId == contentChanelItemEntityTypeId
                    && ( ( a.EntityTypeQualifierColumn == "ContentChannelTypeId" && a.EntityTypeQualifierValue == contentChannelTypeId )
                    || ( a.EntityTypeQualifierColumn == "ContentChannelId" && a.EntityTypeQualifierValue == contentChanelIdString ) ) )
                    .Select( a => a.Id );

            var attributeValueQry = new AttributeValueService( rockContext ).Queryable().Where( av => attributeQry.Contains( av.AttributeId ) );

            var complex = contentChannelItem
                .GroupJoin( attributeValueQry,
                i => i.Id,
                av => av.EntityId,
                ( i, av ) => new
                {
                    ContentChanelItem = i,
                    Children = i.ChildItems.OrderBy( ci => ci.Order ).Select( ci => ci.ChildContentChannelItemId ),
                    ParentItems = i.ParentItems.Select( ci => ci.ContentChannelItemId ),
                    AttributeValues = av
                } )
                .ToList();

            var items = complex
                .Select( i => new ChannelItem
                {
                    Id = i.ContentChanelItem.Id,
                    DateTime = i.ContentChanelItem.StartDateTime,
                    Title = i.ContentChanelItem.Title,
                    Content = i.ContentChanelItem.Content,
                    Priority = i.ContentChanelItem.Priority,
                    Order = i.ContentChanelItem.Order,
                    Attributes = i.AttributeValues.ToDictionary( a => a.AttributeKey, a => a.Value ),
                    Tags = GetTags( i.ContentChanelItem, rockContext ),
                    Slug = i.ContentChanelItem.PrimarySlug,
                    CreatedBy = i.ContentChanelItem.CreatedByPersonName,
                    ChildItems = i.Children.ToList(),
                    ParentItems = i.ParentItems.ToList()
                } ).ToList();

            return Json( items.FirstOrDefault() );
        }

        private List<string> GetTags( ContentChannelItem contentChannelItem, RockContext rockContext )
        {
            TaggedItemService taggedItemService = new TaggedItemService( rockContext );
            var tags = taggedItemService.Queryable()
                .Where( ti => ti.EntityGuid == contentChannelItem.Guid )
                .Select( ti => ti.Tag.Name );
            return tags.ToList();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using org.secc.SermonFeed.Models;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest;
using Rock.Web.Cache;

namespace org.secc.SermonFeed.Rest
{
    public partial class SermonController : ApiControllerBase
    {
        const int CONTENT_CHANNEL = 24;
        const int SERMON_CONTENT_CHANNEL = 23;
        const int SPEAKER_ATTRIBUTE = 30285;

        [HttpGet]
        [System.Web.Http.Route( "api/sermonfeed/" )]
        [System.Web.Http.Route( "api/sermonfeed/{slug}/" )]
        public IHttpActionResult GetSpeakerSeries( string slug = "", string speaker = "", int offset = 0 )
        {
            var cacheKey = string.Format( "api/sermonfeed/{0}/{1}/{2}", slug, speaker, offset );
            var output = RockCache.Get( cacheKey ) as List<SermonSeries>;
            if ( output != null )
            {
                return Json( output );
            }


            string imageLocation = GlobalAttributesCache.Get().GetValue( "PublicApplicationRoot" ).EnsureTrailingForwardslash() + "GetImage.ashx?Guid=";
            string audioLocation = GlobalAttributesCache.Get().GetValue( "PublicApplicationRoot" ).EnsureTrailingForwardslash() + "GetFile.ashx?Guid=";
            RockContext rockContext = new RockContext();

            var speakerQry = new AttributeValueService( rockContext ).Queryable().Where( av => av.AttributeId == SPEAKER_ATTRIBUTE && av.Value == speaker );
            ContentChannelItemService contentChannelItemService = new ContentChannelItemService( rockContext );
            var sermonSeriesQry = contentChannelItemService.Queryable()
                .Where( i => i.ContentChannelId == CONTENT_CHANNEL );
            if ( !string.IsNullOrWhiteSpace( slug ) )
            {
                sermonSeriesQry = sermonSeriesQry.Where( i => i.ContentChannelItemSlugs.Where( s => s.ContentChannelItemId == i.Id && s.Slug == slug ).Any() );
            }
            var sermonSeriesList = sermonSeriesQry.SelectMany( i => i.ChildItems )
                .Select( i => i.ChildContentChannelItem )
                .Join(
                    speakerQry,
                    i => i.Id,
                    a => a.EntityId,
                    ( i, a ) => i
                )
                .Select( i => i.ParentItems.FirstOrDefault().ContentChannelItem )
                .DistinctBy( i => i.Id )
                .OrderByDescending( i => i.StartDateTime )
                .Skip( offset )
                .Take( 12 )
                .ToList();

            output = new List<SermonSeries>();

            foreach ( var seriesItem in sermonSeriesList )
            {
                seriesItem.LoadAttributes();
                var sermonSeries = new SermonSeries
                {
                    id = seriesItem.Id,
                    title = seriesItem.Title,
                    description = seriesItem.Content,
                    slug = seriesItem.PrimarySlug,
                    image = imageLocation + seriesItem.GetAttributeValue( "Image" ),
                    image_url = imageLocation + seriesItem.GetAttributeValue( "Image" ),
                    last_updated = ConvertTime( seriesItem.StartDateTime ),
                    sermons = new List<Sermon>()
                };
                if ( string.IsNullOrWhiteSpace( seriesItem.GetAttributeValue( "Image" ) ) )
                {
                    var childItem = seriesItem.ChildItems.FirstOrDefault().ChildContentChannelItem;
                    childItem.LoadAttributes();
                    sermonSeries.image_url = imageLocation + childItem.GetAttributeValue( "Image" );
                    sermonSeries.image = imageLocation + childItem.GetAttributeValue( "Image" );
                }

                output.Add( sermonSeries );

                //add sermons to series
                foreach ( var sermonItem in seriesItem.ChildItems.Where(s => s.ChildContentChannelItem.ContentChannelId == SERMON_CONTENT_CHANNEL).Select( s => s.ChildContentChannelItem ) )
                {
                    sermonItem.LoadAttributes();
                    if ( sermonItem.GetAttributeValue( "Speaker" ).ToLower() != speaker.ToLower() )
                    {
                        continue;
                    }

                    sermonSeries.sermons.Add(
                        new Sermon
                        {
                            id = sermonItem.Id,
                            title = sermonItem.Title,
                            description = sermonItem.Content,
                            slug = sermonItem.PrimarySlug,
                            audio_link = audioLocation + sermonItem.GetAttributeValue( "Audio" ),
                            date = ConvertTime( sermonItem.StartDateTime ),
                            duration = sermonItem.GetAttributeValue( "Duration" ).AsInteger(),
                            image = imageLocation + sermonItem.GetAttributeValue( "Image" ),
                            image_url = imageLocation + sermonItem.GetAttributeValue( "Image" ),
                            speaker = sermonItem.GetAttributeValue( "Speaker" ),
                            vimeo_id = sermonItem.GetAttributeValue( "VimeoId" ).AsInteger()
                        } );
                }
            }
            RockCache.AddOrUpdate( cacheKey, null, output, RockDateTime.Now.AddHours( 1 ) );

            return Json( output );
        }


        private long ConvertTime( DateTime dateTime )
        {
            return ( Int32 ) ( dateTime.ToUniversalTime().Subtract( new DateTime( 1970, 1, 1 ) ) ).TotalSeconds;
        }
    }
}

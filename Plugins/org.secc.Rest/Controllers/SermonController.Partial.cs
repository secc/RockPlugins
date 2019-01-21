using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using org.secc.SermonFeed.Models;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest;

namespace org.secc.SermonFeed.Rest
{
    public partial class SermonController : ApiControllerBase
    {
        const int CONTENT_CHANNEL = 24;
        const int SPEAKER_ATTRIBUTE = 30285;

        [HttpGet]
        [System.Web.Http.Route( "api/sermonfeed/" )]
        [System.Web.Http.Route( "api/sermonfeed/{slug}/" )]
        public IHttpActionResult GetSpeakerSeries( string slug = "", string speaker = "", int offset = 0 )
        {
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

            var output = new List<SermonSeries>();

            foreach ( var seriesItem in sermonSeriesList )
            {
                seriesItem.LoadAttributes();
                var sermonSeries = new SermonSeries
                {
                    id = seriesItem.Id,
                    title = seriesItem.Title,
                    description = seriesItem.Content,
                    slug = seriesItem.PrimarySlug,
                    image = "https://southeastchristian.org/GetImage.ashx?Guid=" + seriesItem.GetAttributeValue( "Image" ),
                    image_url = "https://southeastchristian.org/GetImage.ashx?Guid=" + seriesItem.GetAttributeValue( "Image" ),
                    last_updated = ConvertTime( seriesItem.StartDateTime ),
                    sermons = new List<Sermon>()
                };
                if ( string.IsNullOrWhiteSpace( seriesItem.GetAttributeValue( "Image" ) ) )
                {
                    var childItem = seriesItem.ChildItems.FirstOrDefault().ChildContentChannelItem;
                    childItem.LoadAttributes();
                    sermonSeries.image_url = "https://southeastchristian.org/GetImage.ashx?Guid=" + childItem.GetAttributeValue( "Image" );
                    sermonSeries.image = "https://southeastchristian.org/GetImage.ashx?Guid=" + childItem.GetAttributeValue( "Image" );
                }

                output.Add( sermonSeries );

                //add sermons to series
                foreach ( var sermonItem in seriesItem.ChildItems.Select( s => s.ChildContentChannelItem ) )
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
                            audio_link = "https://southeastchristian.org/GetFile.ashx?Guid=" + sermonItem.GetAttributeValue( "Audio" ),
                            date = ConvertTime( sermonItem.StartDateTime ),
                            duration = sermonItem.GetAttributeValue( "Duration" ).AsInteger(),
                            image = "https://southeastchristian.org/GetImage.ashx?Guid=" + sermonItem.GetAttributeValue( "Image" ),
                            image_url = "https://southeastchristian.org/GetImage.ashx?Guid=" + sermonItem.GetAttributeValue( "Image" ),
                            speaker = sermonItem.GetAttributeValue( "Speaker" ),
                            vimeo_id = sermonItem.GetAttributeValue( "VimeoId" ).AsInteger()
                        } );
                }
            }
            return Json( output );
        }


        private long ConvertTime( DateTime dateTime )
        {
            return ( Int32 ) ( dateTime.ToUniversalTime().Subtract( new DateTime( 1970, 1, 1 ) ) ).TotalSeconds;
        }
    }
}

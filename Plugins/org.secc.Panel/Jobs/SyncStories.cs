using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using org.secc.Panel.Models;
using org.secc.Panel.Utilities;
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;

namespace org.secc.Panel.Jobs
{
    [ContentChannelField( "StoriesContentChannel" )]

    [DisallowConcurrentExecution]
    class SyncStories : IJob
    {
        public void Execute( IJobExecutionContext context )
        {
            int storyCount = 0;
            int newStoryCount = 0;

            JobDataMap dataMap = context.JobDetail.JobDataMap;

            List<Story> stories = new List<Story>();
            RockContext rockContext = new RockContext();
            ContentChannelService contentChannelService = new ContentChannelService( rockContext );
            ContentChannelItemService contentChannelItemService = new ContentChannelItemService( rockContext );
            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
            BinaryFileType binaryFileType = new BinaryFileTypeService( rockContext ).Get( Rock.SystemGuid.BinaryFiletype.MEDIA_FILE.AsGuid() );
            var storiesSeriesChannel = contentChannelService.Get( dataMap.GetString( "StoriesContentChannel" ).AsGuid() );

            var dbCon = DBConnection.Instance();
            dbCon.DatabaseName = "secccp_main";
            if ( dbCon.IsConnect() )
            {
                stories = GetStories( dbCon );

                foreach ( var story in stories )
                {
                    storyCount++;
                    var item = contentChannelItemService.Queryable().Where( i => i.ForeignId == story.id && i.ContentChannelId == storiesSeriesChannel.Id ).FirstOrDefault();
                    if ( item == null )
                    {
                        newStoryCount++;
                        item = new ContentChannelItem()
                        {
                            ContentChannelId = storiesSeriesChannel.Id,
                            ForeignId = story.id,
                            ContentChannelTypeId = storiesSeriesChannel.ContentChannelTypeId
                        };
                        contentChannelItemService.Add( item );
                    }
                    item.Title = story.title;
                    item.Content = story.description;
                    item.StartDateTime = Helpers.FromUnixTime( story.datecreated );
                    rockContext.SaveChanges();

                    item.LoadAttributes();
                    item.SetAttributeValue( "Slug", story.slug );
                    item.SetAttributeValue( "VimeoId", story.vimeo_id );
                    item.SetAttributeValue( "VimeoStreamingUrl", story.vimeo_live_url );
                    item.SetAttributeValue( "VimeoDownloadUrl", story.vimeo_sd_url );
                    item.SetAttributeValue( "Tags", story.tags );
                    item.SetAttributeValue( "Duration", story.duration );

                    if ( string.IsNullOrWhiteSpace( item.GetAttributeValue( "Image" ) ) )
                    {
                        WebClient client = new WebClient();
                        try
                        {
                            using ( MemoryStream stream = new MemoryStream( client.DownloadData( string.Format( "http://panel.secc.org/upload/stories/cover-images/story-{0}.jpg", story.id ) ) ) )
                            {
                                BinaryFile binaryFile = new BinaryFile();
                                binaryFileService.Add( binaryFile );
                                binaryFile.IsTemporary = false;
                                binaryFile.BinaryFileTypeId = binaryFileType.Id;
                                binaryFile.MimeType = "image/jpg";
                                binaryFile.FileName = string.Format( "Story-{0}.jpg", story.id );
                                binaryFile.ContentStream = stream;
                                rockContext.SaveChanges();
                                item.SetAttributeValue( "Image", binaryFile.Guid.ToString() );
                            }
                        }
                        catch ( Exception ex )
                        {
                            var a = ex;
                        }
                    }

                    item.SaveAttributeValues();
                }
            }
            context.Result = string.Format( "Synced {0} sermons ({1} New Sermon)", storyCount, newStoryCount );
        }

        private static List<Story> GetStories( DBConnection dbCon )
        {
            while ( !dbCon.IsConnect() )
            {
                dbCon = DBConnection.Instance();
                dbCon.DatabaseName = "secccp_main";
            }

            List<Story> output = new List<Story>();
            string query = "SELECT * FROM stories";
            var cmd = new MySqlCommand( query, dbCon.Connection );
            var reader = cmd.ExecuteReader();
            while ( reader.Read() )
            {
                output.Add( new Story()
                {
                    id = reader.GetInt32( 0 ),
                    title = reader.GetString( 1 ),
                    slug = reader.GetString( 2 ),
                    description = reader.GetString( 3 ),
                    duration = reader.GetInt32( 4 ),
                    vimeo_id = reader.GetInt64( 5 ),
                    vimeo_sd_url = reader.GetString( 6 ),
                    vimeo_live_url = reader.GetString( 7 ),
                    tags = reader.GetString( 8 ),
                    datecreated = reader.GetInt64( 9 ),

                } );
            }
            reader.Close();
            return output;
        }
    }
}
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
using Rock.Attribute;
using Rock;
using Rock.Data;
using Rock.Model;

namespace org.secc.Panel.Jobs
{
    [ContentChannelField( "SermonContentChannel" )]
    [ContentChannelField( "SermonSeriesContentChannel" )]

    [DisallowConcurrentExecution]
    class SyncSermons : IJob
    {
        public void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;

            int sermonCount = 0;
            int newSermonCount = 0;

            List<SermonSeries> sermonSeries = new List<SermonSeries>();
            RockContext rockContext = new RockContext();
            ContentChannelService contentChannelService = new ContentChannelService( rockContext );
            ContentChannelItemService contentChannelItemService = new ContentChannelItemService( rockContext );
            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
            BinaryFileType binaryFileType = new BinaryFileTypeService( rockContext ).Get( Rock.SystemGuid.BinaryFiletype.MEDIA_FILE.AsGuid() );
            var sermonSeriesChannel = contentChannelService.Get( dataMap.GetString( "SermonSeriesContentChannel" ).AsGuid() );
            var sermonChannel = contentChannelService.Get( dataMap.GetString( "SermonContentChannel" ).AsGuid() );

            var dbCon = DBConnection.Instance();
            dbCon.DatabaseName = "secccp_main";
            if ( dbCon.IsConnect() )
            {
                sermonSeries = GetSermonSeries( dbCon );

                foreach ( var series in sermonSeries.Where( s => s.deleted == false ) )
                {
                    AddSermons( dbCon, series );
                    var item = contentChannelItemService.Queryable().Where( i => i.ForeignId == series.id && i.ContentChannelId == sermonSeriesChannel.Id ).FirstOrDefault();
                    if ( item == null )
                    {
                        item = new ContentChannelItem()
                        {
                            ContentChannelId = sermonSeriesChannel.Id,
                            ForeignId = series.id,
                            ContentChannelTypeId = sermonSeriesChannel.ContentChannelTypeId
                        };
                        contentChannelItemService.Add( item );
                    }
                    item.Title = series.title;
                    item.Content = series.description;
                    if ( series.sermons.Any() )
                    {
                        item.StartDateTime = Helpers.FromUnixTime( series.sermons.FirstOrDefault().date );
                    }
                    else
                    {
                        item.StartDateTime = Rock.RockDateTime.Now;
                    }

                    rockContext.SaveChanges();
                    item.LoadAttributes();
                    item.SetAttributeValue( "Slug", series.slug );

                    if ( string.IsNullOrWhiteSpace( item.GetAttributeValue( "Image" ) ) )
                    {
                        WebClient client = new WebClient();
                        try
                        {
                            using ( MemoryStream stream = new MemoryStream( client.DownloadData( string.Format( "http://files.secc.org/sermons/series/series-{0}.jpg", series.id ) ) ) )
                            {
                                BinaryFile binaryFile = new BinaryFile();
                                binaryFileService.Add( binaryFile );
                                binaryFile.IsTemporary = false;
                                binaryFile.BinaryFileTypeId = binaryFileType.Id;
                                binaryFile.MimeType = "image/jpg";
                                binaryFile.FileName = string.Format( "Series-{0}.jpg", series.id );
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
                foreach ( var series in sermonSeries.Where( ss => !ss.deleted ) )
                {
                    //add in sermons
                    foreach ( var sermon in series.sermons.Where( s => !s.deleted ) )
                    {
                        sermonCount++;
                        var child = contentChannelItemService.Queryable().Where( i => i.ForeignId == sermon.id && i.ContentChannelId == sermonChannel.Id ).FirstOrDefault();
                        if ( child == null )
                        {
                            newSermonCount++;
                            child = new ContentChannelItem()
                            {
                                ContentChannelId = sermonChannel.Id,
                                ForeignId = sermon.id,
                                ContentChannelTypeId = sermonChannel.ContentChannelTypeId,
                                StartDateTime = Helpers.FromUnixTime( sermon.date )
                            };
                            contentChannelItemService.Add( child );
                            rockContext.SaveChanges();
                            var item = contentChannelItemService.Queryable().Where( i => i.ForeignId == series.id ).FirstOrDefault();
                            item.ChildItems.Add( new ContentChannelItemAssociation
                            {
                                ContentChannelItemId = item.Id,
                                ChildContentChannelItemId = child.Id,
                            } );
                            rockContext.SaveChanges();
                        }
                        child.Title = sermon.title;
                        child.Content = sermon.description;
                        child.StartDateTime = Helpers.FromUnixTime( sermon.date );
                        rockContext.SaveChanges();
                        child.LoadAttributes();
                        child.SetAttributeValue( "Slug", sermon.slug );
                        child.SetAttributeValue( "Speaker", sermon.speaker );
                        child.SetAttributeValue( "Duration", sermon.duration );
                        child.SetAttributeValue( "VimeoId", sermon.vimeo_id );
                        child.SetAttributeValue( "VimeoDownloadUrl", sermon.vimeo_sd_url );
                        child.SetAttributeValue( "VimeoStreamingUrl", sermon.vimeo_live_url );

                        if ( string.IsNullOrWhiteSpace( child.GetAttributeValue( "Image" ) ) )
                        {
                            WebClient client = new WebClient();
                            try
                            {
                                using ( MemoryStream stream = new MemoryStream( client.DownloadData( string.Format( "http://panel.secc.org/upload/sermon/images/images-{0}.jpg", sermon.id ) ) ) )
                                {
                                    BinaryFile binaryFile = new BinaryFile();
                                    binaryFileService.Add( binaryFile );
                                    binaryFile.IsTemporary = false;
                                    binaryFile.BinaryFileTypeId = binaryFileType.Id;
                                    binaryFile.MimeType = "image/jpg";
                                    binaryFile.FileName = string.Format( "Sermon-{0}.jpg", series.id );
                                    binaryFile.ContentStream = stream;
                                    rockContext.SaveChanges();
                                    child.SetAttributeValue( "Image", binaryFile.Guid.ToString() );
                                }
                            }
                            catch ( Exception ex )
                            {
                                var a = ex;
                            }
                        }

                        if ( string.IsNullOrWhiteSpace( child.GetAttributeValue( "Audio" ) ) )
                        {
                            try
                            {
                                using ( WebClient client = new WebClient() )
                                {
                                    using ( MemoryStream stream = new MemoryStream( client.DownloadData( sermon.audio_link ) ) )
                                    {
                                        BinaryFile binaryFile = new BinaryFile();
                                        binaryFileService.Add( binaryFile );
                                        binaryFile.IsTemporary = false;
                                        binaryFile.BinaryFileTypeId = binaryFileType.Id;
                                        binaryFile.MimeType = "audio/mpeg";
                                        binaryFile.FileName = string.Format( "Sermon-{0}.mp3", sermon.id );
                                        binaryFile.ContentStream = stream;
                                        rockContext.SaveChanges();
                                        child.SetAttributeValue( "Audio", binaryFile.Guid.ToString() );
                                    }
                                }
                            }
                            catch ( Exception ex )
                            {
                                var a = ex;
                            }
                        }


                        child.SaveAttributeValues();
                    }
                }
            }
            context.Result = string.Format( "Synced {0} sermons ({1} New Sermon)", sermonCount, newSermonCount );
        }

        private static void AddSermons( DBConnection dbCon, SermonSeries series )
        {
            string query = "SELECT * FROM sermons WHERE series = @seriesId ";
            var cmd = new MySqlCommand( query, dbCon.Connection );
            cmd.Parameters.AddWithValue( "@seriesId", series.id );
            cmd.Prepare();
            var reader = cmd.ExecuteReader();
            while ( reader.Read() )
            {
                var sermon = new Sermon();
                sermon.id = reader.GetInt32( 0 );
                sermon.title = reader.GetString( 1 );
                sermon.slug = reader.GetString( 2 );
                sermon.description = reader.GetString( 3 );
                sermon.speaker = reader.GetString( 4 );
                sermon.duration = reader.GetInt32( 5 );
                sermon.kitclub_id = reader.GetInt32( 6 );
                sermon.vimeo_id = reader.GetInt64( 7 );
                sermon.vimeo_sd_url = reader.GetString( 8 );
                sermon.vimeo_live_url = reader.GetString( 9 );
                sermon.audio_link = reader.GetString( 10 );
                sermon.audio_size = reader.GetString( 11 );
                sermon.series = reader.GetInt32( 12 );
                sermon.campus = reader.GetInt32( 13 );
                sermon.podcast_views = reader.GetInt32( 14 );
                sermon.date = reader.GetInt64( 15 );
                sermon.notes_downloaded = reader.GetInt32( 16 );
                sermon.questions_downloaded = reader.GetInt32( 17 );
                sermon.deleted = reader.GetBoolean( 18 );
                series.sermons.Add( sermon );
            }
            reader.Close();
        }

        private static List<SermonSeries> GetSermonSeries( DBConnection dbCon )
        {
            while ( !dbCon.IsConnect() )
            {
                dbCon = DBConnection.Instance();
                dbCon.DatabaseName = "secccp_main";
            }

            List<SermonSeries> output = new List<SermonSeries>();
            //"Whats in your wallet?" is the only deleted sermon series
            string query = "SELECT * FROM series";
            var cmd = new MySqlCommand( query, dbCon.Connection );
            var reader = cmd.ExecuteReader();
            while ( reader.Read() )
            {
                output.Add( new SermonSeries()
                {
                    id = reader.GetInt32( 0 ),
                    title = reader.GetString( 1 ),
                    slug = reader.GetString( 2 ),
                    image = reader.GetString( 3 ),
                    description = reader.GetString( 4 ),
                    lastupdated = reader.GetInt64( 5 ),
                    deleted = reader.GetBoolean( 6 ),
                    sermons = new List<Sermon>()
                } );
            }
            reader.Close();
            return output;
        }
    }
}

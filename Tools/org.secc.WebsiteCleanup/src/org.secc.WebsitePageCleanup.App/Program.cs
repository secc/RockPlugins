using System.Text.Json;
using org.secc.WebsitePageCleanup.App.Model;

namespace org.secc.WebsitePageCleanup.App;

public class Program( Configuration _configuration )
{
    static string configFileName = "appSettings.json";
    private readonly Configuration configuration = _configuration;

    public static void Main( string[] args )
    {
        var configPath = GetConfigPath( configFileName );
        var config = LoadConfiguration( configPath );
        var program = new Program( config );
    }


    static string GetConfigPath( string configFileName )
    {
        var path = AppContext.BaseDirectory + configFileName;
        return path;

    }

    static Configuration LoadConfiguration(string configPath)
    {
        Configuration config;
        using (var sr = new StreamReader( configPath ))
        {
            var configText = sr.ReadToEnd();
            config = JsonSerializer.Deserialize<Configuration>( configText )!;

        }
        return config;
    }

    internal void ProcessPages()
    {
        var connectionString = configuration.ConnectionStrings["RockRMS"];
        var minimumInteractionDate = DateTime.Today.AddMonths( -configuration.InteractionTimeframeMonths );
        var maximumInteractionDate = DateTime.Now;
        var minimumInteractionCount = configuration.MinimumInteractionCount;

        var interactionChannel = PageCleanupItem.GetInteractionChannel( configuration.SiteId, connectionString );

        if(!interactionChannel.HasValue)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine( "Interaction Color Not Found." );
            Console.ResetColor();
            return;
        }

        Console.WriteLine( "Loading Site Pages..." );
        var pages = PageCleanupItem.GetSitePages( configuration.SiteId, connectionString );
        var initialPageCount = pages.Count;

        Console.WriteLine( "Start Processing Pages..." );
        var counter = 0;

        while (pages.Where( p => p.PageStatus == PageStatus.NOT_PROCESSED ).Any())
        {
            foreach (var page in pages.Where( p => p.PageStatus == PageStatus.NOT_PROCESSED ))
            {

                var hasUnprocessedChildren = pages.Where( p => p.PageId == page.PageId )
                    .Where( p => p.PageStatus == PageStatus.NOT_PROCESSED )
                    .Any();

                if (hasUnprocessedChildren)
                {
                    continue;
                }

                var hasRetainedChildren = pages.Where( p => p.PageId == page.Id )
                    .Where( p => p.PageStatus == PageStatus.SAVED || p.PageStatus == PageStatus.SAVED_HAS_CHILDREN )
                    .Any();


                page.LoadInteractionCount( interactionChannel.Value, minimumInteractionDate, maximumInteractionDate, connectionString );

                if ((page.InteractionCount ?? 0) > minimumInteractionCount)
                {
                    page.PageStatus = PageStatus.SAVED;
                }
                else if (hasRetainedChildren)
                {
                    page.PageStatus = PageStatus.SAVED_HAS_CHILDREN;
                }
                else
                {
                    page.PageStatus = PageStatus.REMOVED;
                }

                page.SaveLogItem( connectionString );

                counter += 1;
            }
            Console.WriteLine( $"Processed {counter} of {initialPageCount}." );

            pages.RemoveAll( p => p.PageStatus == PageStatus.REMOVED );
        }

        Console.WriteLine( "Processing Complete." );
        
    }


}
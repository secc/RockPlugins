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

    }


}
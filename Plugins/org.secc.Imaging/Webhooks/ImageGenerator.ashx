<%@ WebHandler Language="C#" Class="ImageGenerator" %>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Security.Cryptography;

using Rock;
using Rock.Web.Cache;
using Newtonsoft.Json;
using ImageResizer;
using Rock.Utility;

using org.secc.Imaging;

/// <summary>
/// A webhook for processing the request with Lava. Does basic decoding of FORM, JSON
/// and XML data and provides basic HttpRequest information to the Lava script.
/// </summary>
public class ImageGenerator : IHttpHandler
{
    /// <summary>
    /// Process the incoming http request. This is the web handler entry point.
    /// </summary>
    /// <param name="context">The context that contains all information about this request.</param>
    public void ProcessRequest( HttpContext context )
    {
        try
        {
            // Convert the context values to a dictionary
            var mergeFields = RequestToDictionary( context.Request );

            // Find the valid api handler for this request
            var api = GetApiForRequest( context.Request, mergeFields );
            if ( api != null )
            {
                // Get the necessary values from the defined value.
                string lava = api.GetAttributeValue( "Template" );
                string enabledLavaCommands = api.GetAttributeValue( "EnabledLavaCommands" );
                string imageType = "png";
                var currentUser = Rock.Model.UserLoginService.GetCurrentUser();

                string html = lava.ResolveMergeFields( mergeFields, currentUser != null ? currentUser.Person : null, enabledLavaCommands );

                var width = api.GetAttributeValue( "CanvasWidth" ).AsIntegerOrNull();
                var height = api.GetAttributeValue( "CanvasHeight" ).AsIntegerOrNull();

                if(!Directory.Exists(context.Request.MapPath( "~/App_Data/Files/ComputedImages/" )))
                    Directory.CreateDirectory(context.Request.MapPath( "~/App_Data/Files/ComputedImages/" ));

                var filename = context.Request.MapPath( string.Format( "~/App_Data/Files/ComputedImages/{0}_w{1}_h{2}.{3}",
                               Md5Hash(html + context.Request.Url.Query),
                               width.ToString(),
                               height.ToString(),
                               imageType
                               ) );

                byte[] image;

                if ( File.Exists( filename ) )
                {
                    image = File.ReadAllBytes( filename );
                }
                else
                {
                    image = HtmlToImage.GenerateImage( UnicodeEncode( html), imageType.IsNotNullOrWhiteSpace() ? imageType : "png", width, height );

                    // Now set things up and run it through the ImageResizer so we can have all that power too!
                    Stream fileStream = new MemoryStream(image);
                    ResizeSettings settings = new ResizeSettings( context.Request.QueryString );

                    if ( settings["mode"] == null || settings["mode"] == "clip" )
                    {
                        settings.Add( "mode", "max" );

                        if ( !string.IsNullOrEmpty( settings["width"] ) && !string.IsNullOrEmpty( settings["height"] ) )
                        {
                            if ( settings["width"].AsInteger() > settings["height"].AsInteger() )
                            {
                                settings.Remove( "height" );
                            }
                            else
                            {
                                settings.Remove( "width" );
                            }
                        }
                    }

                    settings.Add( "autorotate.default", "true" );

                    MemoryStream resizedStream = new MemoryStream();

                    ImageBuilder.Current.Build( fileStream, resizedStream, settings, false );

                    image = resizedStream.ToArray();
                    File.WriteAllBytes( filename, image );

                }
                context.Response.BinaryWrite( image );

                // Output the content type.
                context.Response.ContentType = "image/"+imageType;

                // check that the format of the image wasn't changed by a format query parm if so adjust the mime-type to reflect the conversion
                if ( context.Request["format"].IsNotNullOrWhiteSpace() )
                {
                    switch ( context.Request["format"] )
                    {
                        case "png":
                            {
                                context.Response.ContentType = "image/png";
                                break;
                            }
                        case "gif":
                            {
                                context.Response.ContentType = "image/gif";
                                break;
                            }
                        case "jpg":
                            {
                                context.Response.ContentType = "image/jpeg";
                                break;
                            }
                    }
                }

                // Add the response headers
                var headers = api.GetAttributeValue( "ResponseHeaders" );
                if ( headers != null ) {
                    foreach( var header in headers.AsDictionary() )
                    {
                        context.Response.AddHeader( header.Key.ResolveMergeFields(mergeFields, currentUser != null ? currentUser.Person : null, enabledLavaCommands ), header.Value.ResolveMergeFields(mergeFields, currentUser != null ? currentUser.Person : null, enabledLavaCommands ) );
                    }
                }
                return;
            }

            // If we got here then something went wrong, probably we couldn't find a matching api.
            context.Response.ContentType = "text/plain";
            context.Response.StatusCode = 404;
            context.Response.Write( "Path not found." );
        }
        catch ( Exception e )
        {
            context.Response.StatusCode = 500;
            WriteToLog( e.Message );
        }
    }
	
	public string UnicodeEncode( string text ) {
		char[] chars = text.ToCharArray();
		StringBuilder result = new StringBuilder( text.Length + (int)( text.Length * 0.1 ) );

		foreach ( char c in chars ) {
			int value = Convert.ToInt32( c );
			if ( value > 127 )
				result.AppendFormat("&#{0};",value); 
			else
				result.Append( c );
		}

		return result.ToString();
	}

    /// <summary>
    /// These webhooks are not reusable and must only be used once.
    /// </summary>
    public bool IsReusable
    {
        get
        {
            return false;
        }
    }

    public string Md5Hash(string str)
    {
        {
            using ( var crypt = MD5.Create() )
            {
                var hash = crypt.ComputeHash( Encoding.UTF8.GetBytes( str ) );

                StringBuilder sb = new StringBuilder();
                foreach ( byte b in hash )
                {
                    // Can be "x2" if you want lowercase
                    sb.Append( b.ToString( "x2" ) );
                }
                return sb.ToString();
            }
        }
    }

    #region Main Methods

    /// <summary>
    /// Retrieve the DefinedValueCache for this request by matching the Method and Url
    /// </summary>
    /// <param name="request">The HttpRequest object that this Api request is for.</param>
    /// <returns>
    /// A DefinedValue for the API request that was matched or null if one was not found.
    /// </returns>
    protected DefinedValueCache GetApiForRequest( HttpRequest request, Dictionary<string, object> mergeFields )
    {
        var url = "/" + string.Join( "", request.Url.Segments.SkipWhile( s => !s.EndsWith( ".ashx", StringComparison.InvariantCultureIgnoreCase ) && !s.EndsWith( ".ashx/", StringComparison.InvariantCultureIgnoreCase ) ).Skip( 1 ).ToArray() );

        var dt = DefinedTypeCache.Get( Constants.HtmlToImageDefinedType.AsGuid() );
        if ( dt != null )
        {
            foreach ( DefinedValueCache api in dt.DefinedValues.OrderBy( h => h.Order ) )
            {
                string apiUrl = api.Value;
                List<string> variables = new List<string>();

                //
                // Check for empty url filter, match all.
                //
                if ( string.IsNullOrEmpty( apiUrl ) )
                {
                    return api;
                }

                if ( !apiUrl.StartsWith( "/" ) && !apiUrl.StartsWith( "^" ) )
                {
                    apiUrl = "/" + apiUrl;
                }

                //
                // Check for any {variable} style routing replacements.
                //
                var apiUrlMatches = Regex.Matches( apiUrl, "\\{(.+?)\\}", RegexOptions.IgnoreCase );
                foreach ( Match routeMatch in apiUrlMatches )
                {
                    var variable = routeMatch.Groups[1].Value;
                    variables.Add( variable );
                    apiUrl = apiUrl.Replace( string.Format( "{{{0}}}", variable ), string.Format( "(?<{0}>[^\\/]*)", variable ) );
                }

                //
                // Ensure the url is a full-line match if they did not specify any
                // regular expression options.
                //
                if ( !apiUrl.StartsWith( "^" ) && !apiUrl.EndsWith( "$" ) )
                {
                    apiUrl = string.Format( "^{0}$", apiUrl );
                }

                //
                // Use regular expression to see if this is a match.
                //
                var regex = new Regex( apiUrl, RegexOptions.IgnoreCase );
                var match = regex.Match( url );
                if ( match.Success )
                {
                    foreach ( var variable in variables )
                    {
                        mergeFields.AddOrReplace( variable, match.Groups[variable].Value );
                    }

                    return api;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Convert the request into a generic JSON object that can provide information
    /// to the workflow. If a subclass does needs to customize this data they can
    /// call the base method and then modify the content before returning it.
    /// </summary>
    /// <param name="request">The HttpRequest of the currently executing webhook.</param>
    /// <returns>A dictionary that can be passed to Lava as the merge fields.</returns>
    protected Dictionary<string, object> RequestToDictionary( HttpRequest request )
    {
        var dictionary = Rock.Lava.LavaHelper.GetCommonMergeFields( null );
        var host = WebRequestHelper.GetHostNameFromRequest( HttpContext.Current );
        // Set the standard values to be used.
        dictionary.Add( "Url", "/" + string.Join( "", request.Url.Segments.SkipWhile( s => !s.EndsWith( ".ashx", StringComparison.InvariantCultureIgnoreCase ) && !s.EndsWith( ".ashx/", StringComparison.InvariantCultureIgnoreCase ) ).Skip( 1 ).ToArray() ) );
        dictionary.Add( "RawUrl", request.Url.AbsoluteUri );
        dictionary.Add( "Method", request.HttpMethod );
        dictionary.Add( "QueryString", request.QueryString.Cast<string>().ToDictionary( q => q, q => request.QueryString[q] ) );
        dictionary.Add( "RemoteAddress", request.UserHostAddress );
        dictionary.Add( "RemoteName", request.UserHostName );
        dictionary.Add( "ServerName", host );

        // Add in the raw body content.
        using ( StreamReader reader = new StreamReader( request.InputStream, Encoding.UTF8 ) )
        {
            dictionary.Add( "RawBody", reader.ReadToEnd() );
        }

        // Parse the body content if it is JSON or standard Form data.
        if ( request.ContentType == "application/json" )
        {
            try
            {
                dictionary.Add( "Body", JsonConvert.DeserializeObject( ( string ) dictionary["RawBody"] ) );
            }
            catch { }
        }
        else if ( request.ContentType == "application/x-www-form-urlencoded" )
        {
            try
            {
                dictionary.Add( "Body", request.Form.Cast<string>().ToDictionary( q => q, q => request.Form[q] ) );
            }
            catch { }
        }
        else if ( request.ContentType == "application/xml" )
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml( ( string ) dictionary["RawBody"] );
                string jsonText = JsonConvert.SerializeXmlNode( doc );
                dictionary.Add( "Body", JsonConvert.DeserializeObject( ( jsonText ) ) );
            }
            catch { }
        }

        // Add the headers
        var headers = request.Headers.Cast<string>()
            .Where( h => !h.Equals( "Authorization", StringComparison.InvariantCultureIgnoreCase ) )
            .Where( h => !h.Equals( "Cookie", StringComparison.InvariantCultureIgnoreCase ) )
            .ToDictionary( h => h, h => request.Headers[h] );
        dictionary.Add( "Headers", headers );

        // Add the cookies
        try
        {
            dictionary.Add( "Cookies", request.Cookies.Cast<string>().ToDictionary( q => q, q => request.Cookies[q].Value ) );
        }
        catch { }

        return dictionary;
    }

    /// <summary>
    /// Log a message to the LavaApi.txt file. The message is prefixed by
    /// the date and the class name.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    protected void WriteToLog( string message )
    {
        string logFile = HttpContext.Current.Server.MapPath( "~/App_Data/Logs/Lava.txt" );

        // Write to the log, but if an ioexception occurs wait a couple seconds and then try again (up to 3 times).
        var maxRetry = 3;
        for ( int retry = 0; retry < maxRetry; retry++ )
        {
            try
            {
                using ( FileStream fs = new FileStream( logFile, FileMode.Append, FileAccess.Write ) )
                {
                    using ( StreamWriter sw = new StreamWriter( fs ) )
                    {
                        sw.WriteLine( string.Format( "{0} [{2}] - {1}", RockDateTime.Now.ToString(), message, GetType().Name ) );
                        break;
                    }
                }
            }
            catch ( IOException )
            {
                if ( retry < maxRetry - 1 )
                {
                    System.Threading.Tasks.Task.Delay( 2000 ).Wait();
                }
            }
        }

    }

    #endregion
}
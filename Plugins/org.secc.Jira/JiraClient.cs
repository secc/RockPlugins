using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using org.secc.DevLib.Components;
using org.secc.Jira.Components;
using org.secc.Jira.Contracts;
using org.secc.Jira.Model;
using org.secc.Jira.Utilities;
using Rock.Security;

namespace org.secc.Jira
{
    public class JiraClient
    {
        private JiraSettings jiraSettings;

        public JiraClient()
        {
            var settingsComponent = SettingsComponent.GetComponent<JiraSettingsComponent>();
            jiraSettings = settingsComponent.GetSettings();
        }

        public JiraClient( JiraSettings settings )
        {
            jiraSettings = settings;
        }

        public async Task<List<Issue>> GetTickets( string jql )
        {
            HttpClient httpClient = new HttpClient();
            var base64EncodedAuthenticationString = Convert.ToBase64String( System.Text.Encoding.UTF8.GetBytes( jiraSettings.User + ":" + jiraSettings.Token ) );
            httpClient.DefaultRequestHeaders.Add( "Authorization", "Basic " + base64EncodedAuthenticationString );

            var page = 0;
            var issues = new List<Issue>();
            var active = true;

            while ( active )
            {
                var url = $"{jiraSettings.BaseUrl}{ HttpUtility.UrlEncode( jql )}&startAt={ page * 50 }";

                var result = await httpClient.GetAsync( url );
                var content = await result.Content.ReadAsStringAsync();

                var response = JsonConvert.DeserializeObject<JiraResponse>( content );
                issues.AddRange( response.Issues );

                if ( page * 50 + 50 < response.Total )
                {
                    page++;
                }
                else
                {
                    active = false;
                }
            }

            return issues;
        }
    }
}

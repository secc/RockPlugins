// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
namespace org.secc.ConnectionsDigest.Migrations
{
    using Rock.Plugin;
    [MigrationNumber(1, "1.7.0")]
    public partial class InitialCreate : Rock.Plugin.Migration
    {
        public override void Up()
        {
            Sql( @"INSERT INTO[dbo].[SystemEmail]
                       ([IsSystem],
                        [Title],
                        [Subject],
                        [Body],
                        [Guid],
                        [CategoryId] )
                 VALUES
                       (0,
                       'Connection Digest Email',
                       'Connections Status Update',
                       '<style>
 @media screen and (max-device-width: 768px) {
  div[class=mobilecontent]{
    display: block !important;
    max-height: none !important;
  }
}
</style>
{% assign totalNewRequests = 0 %}
{% assign totalRequests = 0 %}
{% assign totalIdleRequests = 0 %}
{% assign totalCriticalRequests = 0 %}
{% for newRequest in NewConnectionRequests %}
    {% assign newRequestCount = newRequest | Size %}
    {% assign totalNewRequests = totalNewRequests | Plus: newRequestCount %}
{% endfor %}
{% for requests in ConnectionRequests %}
    {% assign requestCount = requests | Size %}
    {% assign totalRequests = totalRequests | Plus: requestCount %}
{% endfor %}
{% for idleRequest in IdleConnectionRequestIds %}
    {% assign idleRequestCount = idleRequest | Size %}
    {% assign totalIdleRequests = totalIdleRequests | Plus: idleRequestCount %}
{% endfor %}
{% for criticalRequest in CriticalConnectionRequestIds %}
    {% assign criticalRequestCount = criticalRequest | Size %}
    {% assign totalCriticalRequests = totalCriticalRequests | Plus: criticalRequestCount %}
{% endfor %}
{{ ''Global'' | Attribute:''EmailHeader'' }}

<table width=""100%"">
<tr>
<td align=""center"" width=""25%"" style=""border-right: 3px solid #FFF;background: #88bb54; text-align:center; color: #fff; font-family: sans-serif; padding-top:20px;""><h1 style=""color: #fff; margin-top: 20px; font-family: sans-serif;"">{{totalNewRequests}}</h1>New*</td>
<td align=""center"" width=""25%"" style=""border-right: 3px solid #FFF;background: #4099ad; text-align:center; color: #fff; font-family: sans-serif; padding-top:20px;""><h1 style=""color: #fff; margin-top: 20px; font-family: sans-serif;"">{{totalRequests}}</h1>Active</td>
<td align=""center"" width=""25%"" style=""border-right: 3px solid #FFF;background: #ee7624; text-align:center; color: #fff; font-family: sans-serif; padding-top:20px;""><h1 style=""color: #fff; margin-top: 20px; font-family: sans-serif;"">{{totalCriticalRequests}}</h1>Critical</td>
<td align=""center"" width=""25%"" style=""background: #bb5454; text-align:center; color: #fff; font-family: sans-serif; padding-top:20px;""><h1 style=""color: #fff; margin-top: 20px; font-family: sans-serif;"">{{totalIdleRequests}}</h1>Idle</td>
</tr>
</table>
<br />
{% if totalNewRequests > 0 %}
    <h2 style=""font-family: sans-serif;margin-bottom: 5px;"">New Connection Requests</h2>
    <table border=""0"" cellspacing=""0"" cellpadding=""3"" width=""100%"" style=""border: 1px solid #ccc; font-family: sans-serif;"">
    <thead>
        <tr>
            <th align=""left"" style=""background: #999; color:#fff; font-family: sans-serif;"">Requestor</th>
            <th align=""left"" style=""background: #999; color:#fff; font-family: sans-serif;"">Status</th>
            <!--[if gte mso 9]>
            <th align=""left"" style=""background: #999; color:#fff; font-family: sans-serif;"">Connection</th>
            <![endif]-->
        </tr>
    </thead>
    <tbody>
        {% for newRequests in NewConnectionRequests %}
            {% for request in newRequests %}
                <tr style=""background: {% cycle ''#FFF'', ''#DDD'' %}"">
                    <td><a style=""color: #999"" href=""https://rock.secc.org/page/408?ConnectionRequestId={{request.Id}}&ConnectionOpportunityId={{request.ConnectionOpportunityId}}"">{{request.PersonAlias.Person.FullName}}</a></td>
                    <td>{{request.ConnectionStatus.Name}}</td>
                    <!--[if gte mso 9]>
                    <td>{{request.ConnectionOpportunity.Name}}</td>
                    <![endif]-->
                </tr>
            {% endfor %}
        {% endfor %}
    </tbody>
    </table>
{% endif %}
<br />
<h2 style=""font-family: sans-serif;margin-bottom: 5px;"">Connection Opportunity Breakdown</h2>
<!--[if gte mso 9]>
<table border=""0"" cellspacing=""0"" cellpadding=""3"" width=""100%"" style=""border: 1px solid #ccc; font-family: sans-serif;"">
<thead><tr><th align=""left"" style=""background: #999; color:#fff; font-family: sans-serif;"">Connection Opportunity</th><th colspan=""4"" style=""background: #999; color:#fff; font-family: sans-serif;"">Statuses</th></thead>
<tbody>
{% for ConnectionOpportunity in ConnectionOpportunities %}
    {% assign totalNewRequests = 0 %}
    {% assign totalRequests = 0 %}
    {% assign totalIdleRequests = 0 %}
    {% assign totalCriticalRequests = 0 %}
    {% for requests in NewConnectionRequests %}
        {% for request in requests %}
            {% if request.ConnectionOpportunity.Id == ConnectionOpportunity.Id %}
                {% assign totalNewRequests = totalNewRequests | Plus: 1 %}
            {% endif %}
        {% endfor %}
    {% endfor %}
    {% for requests in ConnectionRequests %}
        {% for request in requests %}
            {% if request.ConnectionOpportunity.Id == ConnectionOpportunity.Id %}
                {% assign totalRequests = totalRequests | Plus: 1 %}
            {% endif %}
        {% endfor %}
    {% endfor %}
    {% for idleRequests in IdleConnectionRequestIds %}
        {% for request in idleRequests %}
            {% if request.ConnectionOpportunityId == ConnectionOpportunity.Id %}
                {% assign totalIdleRequests = totalIdleRequests | Plus: 1 %}
            {% endif %}
        {% endfor %}
    {% endfor %}
    {% for criticalRequests in CriticalConnectionRequestIds %}
        {% for request in criticalRequests %}
            {% if request.ConnectionOpportunityId == ConnectionOpportunity.Id %}
                {% assign totalCriticalRequests = totalCriticalRequests | Plus: 1 %}
            {% endif %}
        {% endfor %}
    {% endfor %}
    <tr>
        <td style=""border-right: 1px solid #ccc; border-top: 1px solid #ccc; font-family: sans-serif;""><a style=""color: #999"" href=""https://rock.secc.org/page/407"" target=""_blank"">{{ConnectionOpportunity.Name}}</a></td>
        <td style=""background: #88bb54; color: #fff;text-align:center;border-right: 1px solid #ccc; border-top: 1px solid #ccc; font-family: sans-serif;"">{{totalNewRequests}}</td>
        <td style=""background: #4099ad; color: #fff;text-align:center;border-right: 1px solid #ccc; border-top: 1px solid #ccc; font-family: sans-serif;"">{{totalRequests}}</td>
        <td style=""background: #ee7624; color: #fff;text-align:center;border-right: 1px solid #ccc; border-top: 1px solid #ccc; font-family: sans-serif;"">{{totalCriticalRequests}}</td>
        <td style=""background: #bb5454; color: #fff;text-align:center;border-top: 1px solid #ccc; font-family: sans-serif;"">{{totalIdleRequests}}</td>
    </tr>
    {% capture mobileContent %}
    {{mobileContent}}
    <tr>
        <td colspan=""4"" style=""border-right: 1px solid #ccc; border-top: 1px solid #ccc; font-family: sans-serif;""><a style=""color: #999"" href=""https://rock.secc.org/page/407"" target=""_blank"">{{ConnectionOpportunity.Name}}</a></td>
    </tr>
    <tr>
        <td style=""background: #88bb54; color: #fff;text-align:center;border-right: 1px solid #ccc; border-top: 1px solid #ccc; font-family: sans-serif;"">{{totalNewRequests}}</td>
        <td style=""background: #4099ad; color: #fff;text-align:center;border-right: 1px solid #ccc; border-top: 1px solid #ccc; font-family: sans-serif;"">{{totalRequests}}</td>
        <td style=""background: #ee7624; color: #fff;text-align:center;border-right: 1px solid #ccc; border-top: 1px solid #ccc; font-family: sans-serif;"">{{totalCriticalRequests}}</td>
        <td style=""background: #bb5454; color: #fff;text-align:center;border-top: 1px solid #ccc; font-family: sans-serif;"">{{totalIdleRequests}}</td>
    </tr>
    {% endcapture %}
{% endfor %}
</tbody>
</table>
<![endif]-->

<!--[if !mso]><!-->
<div class=""mobilecontent"" style=""display:none;max-height:0px;overflow:hidden;"">
<table border=""0"" cellspacing=""0"" cellpadding=""3"" width=""100%"" style=""border: 1px solid #ccc; font-family: sans-serif;"">
<thead><tr><th colspan=""4"" align=""left"" style=""background: #999; color:#fff; font-family: sans-serif;"">Connection Opportunity</th></thead>
<tbody>
{{mobileContent}}
</tbody>
</table>
</div>
<!--<![endif]-->
<br />
<small>*Since last run date/time: {{LastRunDate}}</small>
{{ ''Global'' | Attribute:''EmailFooter'' }}',
                       '10059716-8B49-46EA-BEDF-AE388DE9F7FF',
                       (SELECT Id FROM Category WHERE NAME = 'Plugins')
            );");
        }

        public override void Down()
        {
            Sql( "DELETE FROM SystemEmail WHERE Guid = '10059716-8B49-46EA-BEDF-AE388DE9F7FF';" );
        }
    }
}

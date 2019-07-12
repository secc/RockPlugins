// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Attribute;
using System.Data.Entity;
using Rock.Security;
using org.secc.Finance.Utility;

namespace RockWeb.Plugins.org_secc.Finance
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Contribution Statement Lava" )]
    [Category( "SECC > Finance" )]
    [Description( "Block for displaying a Lava based contribution statement." )]
    [AccountsField( "Accounts", "A selection of accounts to include on the statement. If none are selected all accounts that are tax-deductible will be uses.", false, order: 0 )]
    [BooleanField( "Display Pledges", "Determines if pledges should be shown.", true, order: 1 )]
    [CodeEditorField( "Lava Template", "The Lava template to use for the contribution statement.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 500, true, @"{% capture pageTitle %}{{ 'Global' | Attribute:'OrganizationName' }} | Contribution Statement{%endcapture%}
{{ pageTitle | SetPageTitle }}

<div class=""row margin-b-xl"">
    <div class=""col-md-6"">
        <div class=""pull-left"">
            <img src=""{{ 'Global' | Attribute:'PublicApplicationRoot' }}{{ 'Global' | Attribute:'EmailHeaderLogo' }}"" width=""100px"" />
        </div>
        
        <div class=""pull-left margin-l-md margin-t-sm"">
            <strong>{{ 'Global' | Attribute:'OrganizationName' }}</strong><br />
            {{ 'Global' | Attribute:'OrganizationAddress' }}<br />
            {{ 'Global' | Attribute:'OrganizationWebsite' }}
        </div>
    </div>
    <div class=""col-md-6 text-right"">
        <h4>Charitable Contributions for the Year {{ StatementStartDate | Date:'yyyy' }}</h4>
        <p>{{ StatementStartDate | Date:'M/d/yyyy' }} - {{ StatementEndDate | Date:'M/d/yyyy' }}<p>
    </div>
</div>

<h4>
{{ Salutation }} <br />
{{ StreetAddress1 }} <br />
{% if StreetAddress2 and StreetAddress2 != '' %}
    {{ StreetAddress2 }} <br />
{% endif %}
{{ City }}, {{ State }} {{ PostalCode }}
</h4>


<div class=""clearfix"">
    <div class=""pull-right"">
        <a href=""#"" class=""btn btn-primary hidden-print"" onClick=""window.print();""><i class=""fa fa-print""></i> Print Statement</a> 
    </div>
</div>

<hr style=""opacity: .5;"" />

<h4 class=""margin-t-md margin-b-md"">Gift List</h4>


    <table class=""table table-bordered table-striped table-condensed"">
        <thead>
            <tr>
                <th>Date</th>
                <th>Giving Area</th>
                <th>Check/Trans #</th>
                <th align=""right"">Amount</th>
            </tr>
        </thead>    

        {% for transaction in TransactionDetails %}
            <tr>
                <td>{{ transaction.Transaction.TransactionDateTime | Date:'M/d/yyyy' }}</td>
                <td>{{ transaction.Account.Name }}</td>
                <td>{{ transaction.Transaction.TransactionCode }}</td>
                <td align=""right"">{{ 'Global' | Attribute:'CurrencySymbol' }}{{ transaction.Amount }}</td>
            </tr>
        {% endfor %}
    
    </table>




<div class=""row"">
    <div class=""col-xs-6 col-xs-offset-6"">
        <h4 class=""margin-t-md margin-b-md"">Fund Summary</h4>
        <div class=""row"">
            <div class=""col-xs-6"">
                <strong>Fund Name</strong>
            </div>
            <div class=""col-xs-6 text-right"">
                <strong>Total Amount</strong>
            </div>
        </div>
        
        {% for accountsummary in AccountSummary %}
            <div class=""row"">
                <div class=""col-xs-6"">{{ accountsummary.AccountName }}</div>
                <div class=""col-xs-6 text-right"">{{ 'Global' | Attribute:'CurrencySymbol' }}{{ accountsummary.Total }}</div>
            </div>
         {% endfor %}
    </div>
</div>

{% assign pledgeCount = Pledges | Size %}

{% if pledgeCount > 0 %}
    <hr style=""opacity: .5;"" />
    <h4 class=""margin-t-md margin-b-md"">Pledges <small>(as of {{ StatementEndDate | Date:'M/dd/yyyy' }})</small></h4>
 
    {% for pledge in Pledges %}
        <div class=""row"">
            <div class=""col-xs-6"">
                <strong>{{ pledge.AccountName }}</strong>
                
                <p>
                    Amt Pledged: {{ 'Global' | Attribute:'CurrencySymbol' }}{{ pledge.AmountPledged }} <br />
                    Amt Given: {{ 'Global' | Attribute:'CurrencySymbol' }}{{ pledge.AmountGiven }} <br />
                    Amt Remaining: {{ 'Global' | Attribute:'CurrencySymbol' }}{{ pledge.AmountRemaining }}
                </p>
            </div>
            <div class=""col-xs-6 padding-t-md"">
                <div class=""hidden-print"">
                    Pledge Progress
                    <div class=""progress"">
                      <div class=""progress-bar"" role=""progressbar"" aria-valuenow=""{{ pledge.PercentComplete }}"" aria-valuemin=""0"" aria-valuemax=""100"" style=""width: {{ pledge.PercentComplete }}%;"">
                        {{ pledge.PercentComplete }}%
                      </div>
                    </div>
                </div>
                <div class=""visible-print-block"">
                    Percent Complete <br />
                    {{ pledge.PercentComplete }}%
                </div>
            </div>
        </div>
    {% endfor %}
{% endif %}

<hr style=""opacity: .5;"" />
<p class=""text-center"">
    Thank you for your continued support of the {{ 'Global' | Attribute:'OrganizationName' }}. If you have any questions about your statement,
    email {{ 'Global' | Attribute:'OrganizationEmail' }} or call {{ 'Global' | Attribute:'OrganizationPhone' }}.
</p>

<p class=""text-center"">
    <em>Unless otherwise noted, the only goods and services provided are intangible religious benefits.</em>
</p>", order: 2 )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.FINANCIAL_CURRENCY_TYPE, "Excluded Currency Types", "Select the currency types you would like to excluded.", false, true, order: 4 )]
    [BooleanField( "Allow Person Querystring", "Determines if any person other than the currently logged in person is allowed to be passed through the querystring. For security reasons this is not allowed by default.", false, order: 5 )]
    public partial class ContributionStatementLava : Rock.Web.UI.RockBlock
    {
        #region Base Control Methods

        //  overrides of the base RockBlock methods (i.e. OnInit, OnLoad)

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                DisplayResults();
            }
        }

        #endregion

        #region Events

        // handlers called by the controls on your block

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            DisplayResults();
        }

        #endregion

        #region Methods

        private void DisplayResults()
        {
            RockContext rockContext = new RockContext();

            var statementYear = RockDateTime.Now.Year;
            DateRange dateRange = new DateRange();

            if (Request["StatementYear"] != null)
            {
                Int32.TryParse( Request["StatementYear"].ToString(), out statementYear );
            }
            DateTime startDate = new DateTime( statementYear, 1, 1 );
            DateTime endDate = new DateTime( statementYear, 12, 31 );
            if (Request["StatementStartDate"] != null)
            {
                DateTime.TryParse( Request["StatementStartDate"].ToString(), out startDate);
            }
            if (Request["StatementEndDate"] != null)
            {
                DateTime.TryParse( Request["StatementEndDate"].ToString(), out endDate );
            }
            dateRange.Start = startDate;
            dateRange.End = endDate;

            Person targetPerson = CurrentPerson;

            // get excluded currency types setting
            List<Guid> excludedCurrencyTypes = new List<Guid>();
            if (GetAttributeValue( "ExcludedCurrencyTypes" ).IsNotNullOrWhiteSpace())
            {
                excludedCurrencyTypes = GetAttributeValue( "ExcludedCurrencyTypes" ).Split( ',' ).Select( Guid.Parse ).ToList();
            }

            var personGuid = Request["PersonGuid"].AsGuidOrNull();

            if (personGuid.HasValue)
            {
                // if "AllowPersonQueryString is False", only use the PersonGuid if it is a Guid of one of the current person's businesses
                var isCurrentPersonsBusiness = targetPerson != null && targetPerson.GetBusinesses().Any( b => b.Guid == personGuid.Value );
                if (GetAttributeValue( "AllowPersonQuerystring" ).AsBoolean() || isCurrentPersonsBusiness)
                {
                    var person = new PersonService( rockContext ).Get( personGuid.Value );
                    if (person != null)
                    {
                        targetPerson = person;
                    }
                }
            }

            List<Guid> accountGuids = null;
            if (!string.IsNullOrWhiteSpace( GetAttributeValue( "Accounts" ) ))
            {
                accountGuids = GetAttributeValue( "Accounts" ).Split( ',' ).Select( Guid.Parse ).ToList();
            }

            var template = GetAttributeValue( "LavaTemplate" );

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, targetPerson );
            Statement.AddMergeFields( mergeFields, targetPerson, dateRange, excludedCurrencyTypes, accountGuids );
            lResults.Text = template.ResolveMergeFields( mergeFields );

        }

        #endregion

    }
}

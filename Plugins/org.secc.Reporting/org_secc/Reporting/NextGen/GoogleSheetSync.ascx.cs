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
using System;
using System.ComponentModel;
using System.Web.UI;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using System.Collections.Generic;
using Google.Apis.Sheets.v4;
using Google.Apis.Services;
using Google.Apis.Auth.OAuth2;
using Rock;
using Rock.Data;
using System.Linq;
using Google.Apis.Sheets.v4.Data;

namespace RockWeb.Blocks.Reporting.NextGen
{
    /// <summary>
    /// Block for pulling data into Group Member Attributes from a Google Sheet
    /// </summary>
    [DisplayName("Google Sheet Sync")]
    [Category("SECC > Reporting > NextGen")]
    [Description("A tool for pulling data into Group Member Attributes from a Google Sheet")]


    [TextField("Start Column", "The column of the left-most column to fetch.", true, category: "Columns", order:1)]
    [TextField("End Column", "The column of the right-most column to fetch.", true, category: "Columns", order: 2)]
    [IntegerField("First Name Column", "The column of the first name.", true, category:"Columns", order: 3)]
    [IntegerField("Last Name Column", "The column of the first name.", true, category: "Columns", order: 4)]
    [IntegerField("Grade Column", "The column of the person's grade.", true, category: "Columns", order: 5)]
    [IntegerField("Role Column", "The column of the person's role.", true, category: "Columns", order: 6)]
    [KeyValueListField("Column Mapping", "Use this to map columns to Group Member attributes.", true, "", "Column Index", "Attribute Key", category: "Columns", order:7)]

    [TextField("Person Id Column", "An optional column which contains the person id.  Data will be set into this field when a person is matched.", false, category:"")]
    [TextField("Person Phone Column", "An optional column which will contain the person's mobile phone number.  Data will be set into this field when a person is matched.", false, category: "")]

    [CodeEditorField("Service Account Key", "A JSON string for the service account to access this sheet (https://developers.google.com/identity/protocols/OAuth2ServiceAccount)", CodeEditorMode.JavaScript)]
    [IntegerField("First Data Row", "The first row of data after the header rows where this will start synchronizing", true, 2, category: "Sheet Information")]
    [TextField("Spreadsheet Id", "The Google Sheets spreadsheet id.", true, category:"Sheet Information")]
    [TextField("Sheet Name", "The Google Sheet name.", true, category: "Sheet Information")]
    [GroupField("Group", "The group for managing the members of this trip.", true)]
    public partial class GoogleSheetSync : RockBlock
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/sheets.googleapis.com-dotnet-quickstart.json
        static string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly, SheetsService.Scope.Spreadsheets };
        static string ApplicationName = "Rock Google Sheet Sync";

        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad(EventArgs e)
        {
           
            if (!Page.IsPostBack)
            {
               
            }
        }

        #endregion

        #region Internal Methods

        #endregion


        protected void btnRunSync_Click(object sender, EventArgs e)
        {
            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService(rockContext);

            Group group = groupService.Get(GetAttributeValue("Group").AsGuid());

            GoogleCredential credential = GoogleCredential.FromJson(GetAttributeValue("ServiceAccountKey"));

            credential = credential.CreateScoped(Scopes);

            // Create Google Sheets API service.
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = BlockName
            });

            // Define request parameters.
            String spreadsheetId = GetAttributeValue("SpreadsheetId");
            SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(spreadsheetId, "'" + GetAttributeValue("SheetName") + "'!" + GetAttributeValue("StartColumn") + GetAttributeValue("FirstDataRow") + ":" + GetAttributeValue("EndColumn"));
            IList<IList<Object>> data = request.Execute().Values;

            /*


            updateRequest = service.Spreadsheets.Values.Update(new ValueRange() { Values = new List<IList<object>>() { new List<object>() { "3076401977" }, new List<object>() { "5021123365" } } }, spreadsheetId, "B&B Test!B2");
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            result = updateRequest.Execute();*/

            // Prints the names and majors of students in a sample spreadsheet:
            // https://docs.google.com/spreadsheets/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/edit
            litOutput.Text = "<h3>Google Sync Results</h3>";
            if (data != null )
            {
                var personIds = new ValueRange() { Values = new List<IList<object>>() };
                var phoneNumbers = new ValueRange() { Values = new List<IList<object>>() };

                for (var i = 0; i < data.Count; i++)
                {
                    string firstName = data[i][GetAttributeValue("FirstNameColumn").AsInteger()].ToString();
                    string lastName = data[i][GetAttributeValue("LastNameColumn").AsInteger()].ToString();
                    string grade = data[i][GetAttributeValue("GradeColumn").AsInteger()].ToString();
                    string role = data[i][GetAttributeValue("RoleColumn").AsInteger()].ToString();

                    var members = group.Members.Where(m => (m.Person.NickName.ToLower() == firstName.ToLower().Trim(' ') || m.Person.FirstName.ToLower() == firstName.ToLower().Trim(' ')) && m.Person.LastName.ToLower() == lastName.ToLower().Trim(' '));
                    if (members.Count() > 1)
                    {
                        litOutput.Text += "Matched more than one matching Group Member: " + firstName + " " + lastName + "(" + ")<br />";

                        personIds.Values.Add(new List<object>() { "" });
                        phoneNumbers.Values.Add(new List<object>() { "" });

                    }
                    else if (members.Count() == 1)
                    {
                        GroupMember member = members.FirstOrDefault();
                        personIds.Values.Add(new List<object>() { member.PersonId });
                        phoneNumbers.Values.Add(new List<object>() { member.Person.PhoneNumbers.Where(pn => pn.NumberTypeValue.Guid.ToString() == Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE).Select(pn => pn.NumberFormatted) });

                        continue;
                        member.LoadAttributes();
                        var kvList = GetAttributeValue("ColumnMapping").ToKeyValuePairList();

                        foreach(var kvp in kvList)
                        {
                            string value = "";
                            var separator = "";
                            foreach(var index in kvp.Key.ToString().Split(','))
                            {
                                value += separator + data[i][index.AsInteger()].ToString();
                                separator = "-";
                            }
                            member.SetAttributeValue(kvp.Value.ToString(), value);
                        }
                        member.SaveAttributeValues();

                        litOutput.Text += "Updated " + firstName + " " + lastName + "<br />";
                    }
                    else
                    {
                        personIds.Values.Add(new List<object>() { "" });
                        phoneNumbers.Values.Add(new List<object>() { "" });
                        litOutput.Text += "Unable to find Group Member for " + firstName + " " + lastName + "<br />";
                    }
                }

                if (GetAttributeValue("PersonIdColumn") != "")
                {
                    var updateRequest = service.Spreadsheets.Values.Update(personIds, spreadsheetId, GetAttributeValue("SheetName") + "!" + GetAttributeValue("PersonIdColumn") + GetAttributeValue("FirstDataRow") + ":" + GetAttributeValue("PersonIdColumn"));
                    updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                    var result = updateRequest.Execute();
                    
                    litOutput.Text += result.ToString() + "Updated Person Id's in Column " + GetAttributeValue("PersonIdColumn");
                }
            }
            mdShowOutput.Show();
        }
    }
}
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
using Rock.Web.Cache;

namespace RockWeb.Blocks.Reporting.NextGen
{
    /// <summary>
    /// Block for pulling data into Group Member Attributes from a Google Sheet
    /// </summary>
    [DisplayName( "Google Sheet Sync" )]
    [Category( "SECC > Reporting > NextGen" )]
    [Description( "A tool for pulling data into Group Member Attributes from a Google Sheet" )]


    [TextField( "Start Column", "The column of the left-most column to fetch.", true, category: "Columns", order: 1 )]
    [TextField( "End Column", "The column of the right-most column to fetch.", true, category: "Columns", order: 2 )]
    [IntegerField( "First Name Column", "The column of the first name.", true, category: "Columns", order: 3 )]
    [IntegerField( "Last Name Column", "The column of the first name.", true, category: "Columns", order: 4 )]
    [IntegerField( "Grade Column", "The column of the person's grade.", true, category: "Columns", order: 5 )]
    [IntegerField( "Role Column", "The column of the person's role.", true, category: "Columns", order: 6 )]
    [IntegerField( "Campus Column", "The column containing the person's campus.", false, category: "Columns", order: 7 )]
    [KeyValueListField( "Column Mapping", "Use this to map columns to Group Member attributes.", true, "", "Column Index", "Attribute Key", category: "Columns", order: 8 )]

    [TextField( "Person Id Column", "An optional column which contains the person id.  Data will be set into this field when a person is matched.", false, category: "Update Fields" )]
    [TextField( "Person Phone Column", "An optional column which will contain the person's mobile phone number.  Data will be set into this field when a person is matched.", false, category: "Update Fields" )]
    [TextField( "Parent 1 Column", "An optional column which will contain the first parent's name.  Data will be set into this field when a person is matched.", false, category: "Update Fields" )]
    [TextField( "Parent 1 Phone Column", "An optional column which will contain the first parent's phone number.  Data will be set into this field when a person is matched.", false, category: "Update Fields" )]
    [TextField( "Parent 2 Column", "An optional column which will contain the second parent's name.  Data will be set into this field when a person is matched.", false, category: "Update Fields" )]
    [TextField( "Parent 2 Phone Column", "An optional column which will contain the second parent's phone number.  Data will be set into this field when a person is matched.", false, category: "Update Fields" )]

    [CampusField( "Campus", "The campus to process for this sync.", false, includeInactive: false )]
    [CustomDropdownListField( "Role", "The role to process for this sync.", "Student, Other^Other (Leader/Staff/Advance)", true )]

    [CodeEditorField( "Service Account Key", "A JSON string for the service account to access this sheet (https://developers.google.com/identity/protocols/OAuth2ServiceAccount)", CodeEditorMode.JavaScript )]
    [IntegerField( "First Data Row", "The first row of data after the header rows where this will start synchronizing", true, 2, category: "Sheet Information" )]
    [TextField( "Spreadsheet Id", "The Google Sheets spreadsheet id.", true, category: "Sheet Information" )]
    [ValueListField( "Sheet Name", "The Google Sheet names.", true, category: "Sheet Information" )]
    [GroupField( "Group", "The group for managing the members of this trip.", true )]
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
        protected override void OnLoad( EventArgs e )
        {

            if ( !Page.IsPostBack )
            {

            }
        }

        #endregion

        #region Internal Methods

        #endregion


        protected void btnRunSync_Click( object sender, EventArgs e )
        {

            var kvList = GetAttributeValue( "ColumnMapping" ).ToKeyValuePairList();

            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            PersonService personService = new PersonService( rockContext );

            Group group = groupService.Get( GetAttributeValue( "Group" ).AsGuid() );

            foreach ( GroupMember gm in group.Members )
            {
                gm.LoadAttributes();
                litSuccess.Text += "Clearing attributes for " + gm.Person + "<br />";
                foreach ( var kvp in kvList )
                {
                    gm.SetAttributeValue( kvp.Value.ToString(), "" );
                }

                gm.SaveAttributeValues();

            }

            GoogleCredential credential = GoogleCredential.FromJson( GetAttributeValue( "ServiceAccountKey" ) );

            credential = credential.CreateScoped( Scopes );

            // Create Google Sheets API service.
            var service = new SheetsService( new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = BlockName
            } );

            // Define request parameters.
            String spreadsheetId = GetAttributeValue( "SpreadsheetId" );

            var allPersonIds = new ValueRange() { Values = new List<IList<object>>() };
            litErrors.Text = "";
            litErrorsSummary.Text = "";
            litOutput.Text = "";
            litOutputSummary.Text = "";
            litSuccess.Text = "";
            litSuccessSummary.Text = "";
            int errorCount = 0;
            int successCount = 0;


            foreach ( String sheetName in GetAttributeValue( "SheetName" ).SplitDelimitedValues( false ) )
            {
                if ( string.IsNullOrWhiteSpace( sheetName ) )
                {
                    continue;
                }

                SpreadsheetsResource.ValuesResource.GetRequest request =
                        service.Spreadsheets.Values.Get( spreadsheetId, "'" + sheetName + "'!" + GetAttributeValue( "StartColumn" ) + GetAttributeValue( "FirstDataRow" ) + ":" + GetAttributeValue( "EndColumn" ) );
                IList<IList<Object>> data = request.Execute().Values;


                // Prints the names and majors of students in a sample spreadsheet:
                // https://docs.google.com/spreadsheets/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/edit
                if ( data != null )
                {

                    var personIds = new ValueRange() { Values = new List<IList<object>>() };
                    var phoneNumbers = new ValueRange() { Values = new List<IList<object>>() };
                    var parent1Names = new ValueRange() { Values = new List<IList<object>>() };
                    var parent1PhoneNumbers = new ValueRange() { Values = new List<IList<object>>() };
                    var parent2Names = new ValueRange() { Values = new List<IList<object>>() };
                    var parent2PhoneNumbers = new ValueRange() { Values = new List<IList<object>>() };
                    for ( var i = 0; i < data.Count; i++ )
                    {
                        if ( data[i].Count == 0 )
                        {
                            continue;
                        }
                        try
                        {
                            string firstName = data[i][GetAttributeValue( "FirstNameColumn" ).AsInteger()].ToString();
                            string lastName = data[i][GetAttributeValue( "LastNameColumn" ).AsInteger()].ToString();
                            string grade = data[i][GetAttributeValue( "GradeColumn" ).AsInteger()].ToString();
                            string role = data[i][GetAttributeValue( "RoleColumn" ).AsInteger()].ToString();
                            string campus = "";
                            if ( !string.IsNullOrEmpty( GetAttributeValue( "CampusColumn" ) ) )
                            {
                                campus = data[i][GetAttributeValue( "CampusColumn" ).AsInteger()].ToString();
                            }
                            string campusShortCode = "";
                            if ( !string.IsNullOrEmpty( GetAttributeValue( "Campus" ) ) )
                            {
                                campusShortCode = CampusCache.Read( GetAttributeValue( "Campus" ).AsGuid() ).ShortCode;
                                if ( campusShortCode == "920" )
                                {
                                    campusShortCode = "BL";
                                }
                            }


                            string personId = "";
                            string phone = "";
                            string parent1Name = "";
                            string parent1PhoneNumber = "";
                            string parent2Name = "";
                            string parent2PhoneNumber = "";
                            if ( GetAttributeValue( "PersonIdColumn" ) != "" )
                            {
                                int columnIndex = ColumnNumber( GetAttributeValue( "PersonIdColumn" ).ToString() ) - ColumnNumber( GetAttributeValue( "StartColumn" ).ToString() );

                                if ( data[i].Count >= columnIndex + 1 )
                                {
                                    if ( System.Text.RegularExpressions.Regex.IsMatch( data[i][columnIndex].ToString(), "^\\d+$" ) )
                                    {
                                        personId = data[i][columnIndex].ToString();
                                    }
                                }

                            }

                            if ( GetAttributeValue( "PersonPhoneColumn" ) != "" )
                            {
                                int columnIndex = ColumnNumber( GetAttributeValue( "PersonPhoneColumn" ).ToString() ) - ColumnNumber( GetAttributeValue( "StartColumn" ).ToString() );

                                if ( data[i].Count >= columnIndex + 1 )
                                {
                                    phone = data[i][columnIndex].ToString();
                                }
                            }

                            if ( GetAttributeValue( "Parent1Column" ) != "" )
                            {
                                int columnIndex = ColumnNumber( GetAttributeValue( "Parent1Column" ).ToString() ) - ColumnNumber( GetAttributeValue( "StartColumn" ).ToString() );

                                if ( data[i].Count >= columnIndex + 1 )
                                {
                                    parent1Name = data[i][columnIndex].ToString();
                                }
                            }

                            if ( GetAttributeValue( "Parent1PhoneColumn" ) != "" )
                            {
                                int columnIndex = ColumnNumber( GetAttributeValue( "Parent1PhoneColumn" ).ToString() ) - ColumnNumber( GetAttributeValue( "StartColumn" ).ToString() );

                                if ( data[i].Count >= columnIndex + 1 )
                                {
                                    parent1PhoneNumber = data[i][columnIndex].ToString();
                                }
                            }
                            if ( GetAttributeValue( "Parent2Column" ) != "" )
                            {
                                int columnIndex = ColumnNumber( GetAttributeValue( "Parent2Column" ).ToString() ) - ColumnNumber( GetAttributeValue( "StartColumn" ).ToString() );

                                if ( data[i].Count >= columnIndex + 1 )
                                {
                                    parent2Name = data[i][columnIndex].ToString();
                                }
                            }

                            if ( GetAttributeValue( "Parent2PhoneColumn" ) != "" )
                            {
                                int columnIndex = ColumnNumber( GetAttributeValue( "Parent2PhoneColumn" ).ToString() ) - ColumnNumber( GetAttributeValue( "StartColumn" ).ToString() );

                                if ( data[i].Count >= columnIndex + 1 )
                                {
                                    parent2PhoneNumber = data[i][columnIndex].ToString();
                                }
                            }

                            // If the role or the campus short code don't match, just skip this
                            if ( ( GetAttributeValue( "Role" ).ToLower() == "other" && role.ToLower() != "student" || role.ToLower() == GetAttributeValue( "Role" ).ToLower() ) && ( string.IsNullOrEmpty( campus ) || campus == campusShortCode ) && firstName != "" && lastName != "" )
                            {
                                var members = group.Members.Where( m => ( m.Person.NickName.ToLower() == firstName.ToLower().Trim( ' ' ) || m.Person.FirstName.ToLower() == firstName.ToLower().Trim( ' ' ) ) && ( m.Person.LastName.ToLower() == lastName.ToLower().Trim( ' ' ) || m.Person.LastName.ToLower() == lastName.ToLower().Replace( "jr.", "" ).Trim( ' ' ) ) );

                                if ( members.Count() > 1 )
                                {
                                    litErrors.Text += "Matched more than one matching Group Member: " + firstName + " " + lastName + " (Sheet \"" + sheetName + "\" - Row " + ( i + GetAttributeValue( "FirstDataRow" ).AsInteger() ) + ")<br />";
                                    errorCount++;
                                }
                                else if ( members.Count() == 1 )
                                {
                                    GroupMember member = members.FirstOrDefault();
                                    if ( allPersonIds.Values.Select( v => v[0] ).Contains( member.PersonId ) )
                                    {
                                        litErrors.Text += "Duplicate person " + firstName + " " + lastName + " (Sheet: \"" + sheetName + "\" PersonId: " + member.PersonId + " Row: " + ( i + GetAttributeValue( "FirstDataRow" ).AsInteger() ) + " Duplicate Row: " + ( personIds.Values.Select( v => v[0] ).ToList().IndexOf( member.PersonId ) + GetAttributeValue( "FirstDataRow" ).AsInteger() ) + ")<br />";
                                        errorCount++;
                                    }
                                    else
                                    {
                                        personId = member.PersonId.ToString();
                                        phone = member.Person.PhoneNumbers.Where( pn => pn.NumberTypeValue.Guid == Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() ).Select( pn => pn.NumberFormatted ).FirstOrDefault();

                                        var parent1 = member.Person.GetFamilyMembers().Where( fm => fm.GroupRole.Guid.ToString() == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT ).Select( fm => fm.Person ).FirstOrDefault();

                                        if ( parent1 != null )
                                        {
                                            parent1Name = parent1.NickName + " " + parent1.LastName;
                                            parent1PhoneNumber = personService.Get( parent1.Id ).PhoneNumbers.Where( pn => pn.NumberTypeValueId == 12 ).Select( pn => pn.NumberFormatted ).FirstOrDefault();
                                            var parent2 = member.Person.GetFamilyMembers().Where( fm => fm.GroupRole.Guid.ToString() == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT && fm.PersonId != parent1.Id ).Select( fm => fm.Person ).FirstOrDefault();

                                            if ( parent2 != null )
                                            {
                                                parent2Name = parent2.NickName + " " + parent2.LastName;
                                                parent2PhoneNumber = personService.Get( parent2.Id ).PhoneNumbers.Where( pn => pn.NumberTypeValueId == 12 ).Select( pn => pn.NumberFormatted ).FirstOrDefault();

                                            }
                                        }

                                        member.LoadAttributes();

                                        foreach ( var kvp in kvList )
                                        {
                                            string value = "";
                                            var separator = "";
                                            foreach ( var index in kvp.Key.ToString().Replace( "%2C", "," ).Split( ',' ) )
                                            {
                                                if ( data[i].Count >= index.AsInteger() + 1 && !string.IsNullOrWhiteSpace( data[i][index.AsInteger()].ToString() ) )
                                                {
                                                    value += separator + data[i][index.AsInteger()].ToString();
                                                    separator = "-";
                                                }
                                            }
                                            member.SetAttributeValue( kvp.Value.ToString(), value );
                                        }
                                        member.SaveAttributeValues();

                                        litSuccess.Text += "Updated " + firstName + " " + lastName + " (Sheet \"" + sheetName + "\" - Row " + ( i + GetAttributeValue( "FirstDataRow" ).AsInteger() ) + ")<br />";
                                        successCount++;
                                    }
                                }
                                else
                                {
                                    litErrors.Text += "Unable to find Group Member for " + firstName + " " + lastName + " (Sheet \"" + sheetName + "\" - Row " + ( i + GetAttributeValue( "FirstDataRow" ).AsInteger() ) + ")<br />";
                                    errorCount++;
                                }
                            }

                            personIds.Values.Add( new List<object>() { personId } );
                            allPersonIds.Values.Add( new List<object>() { personId } );
                            phoneNumbers.Values.Add( new List<object>() { phone } );
                            parent1Names.Values.Add( new List<object>() { parent1Name } );
                            parent1PhoneNumbers.Values.Add( new List<object>() { parent1PhoneNumber } );
                            parent2Names.Values.Add( new List<object>() { parent2Name } );
                            parent2PhoneNumbers.Values.Add( new List<object>() { parent2PhoneNumber } );
                        }
                        catch ( Exception exception )
                        {
                            litErrors.Text += "Error: " + exception.Message + " - Row " + ( i + GetAttributeValue( "FirstDataRow" ).AsInteger() ) + "<br />";
                        }
                    }

                    if ( GetAttributeValue( "PersonIdColumn" ) != "" )
                    {
                        var updateRequest = service.Spreadsheets.Values.Update( personIds, spreadsheetId, sheetName + "!" + GetAttributeValue( "PersonIdColumn" ) + GetAttributeValue( "FirstDataRow" ) + ":" + GetAttributeValue( "PersonIdColumn" ) );
                        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                        var result = updateRequest.Execute();

                        litOutput.Text += ( litOutput.Text.Length > 0 ? "<br />" : "" ) + "Updated " + ( personIds.Values.Select( v => v[0] ).Where( v => v != null && v.ToString() != "" ).Count() ) + " Person Id's on Sheet " + sheetName + " in Column " + GetAttributeValue( "PersonIdColumn" );
                    }
                    if ( GetAttributeValue( "PersonPhoneColumn" ) != "" )
                    {
                        var updateRequest = service.Spreadsheets.Values.Update( phoneNumbers, spreadsheetId, sheetName + "!" + GetAttributeValue( "PersonPhoneColumn" ) + GetAttributeValue( "FirstDataRow" ) + ":" + GetAttributeValue( "PersonPhoneColumn" ) );
                        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                        var result = updateRequest.Execute();

                        litOutput.Text += "<br />Updated " + ( phoneNumbers.Values.Select( v => v[0] ).Where( v => v != null && v.ToString() != "" ).Count() ) + " Phone Numbers on Sheet " + sheetName + " in Column " + GetAttributeValue( "PersonPhoneColumn" );
                    }
                    if ( GetAttributeValue( "Parent1Column" ) != "" )
                    {
                        var updateRequest = service.Spreadsheets.Values.Update( parent1Names, spreadsheetId, sheetName + "!" + GetAttributeValue( "Parent1Column" ) + GetAttributeValue( "FirstDataRow" ) + ":" + GetAttributeValue( "Parent1Column" ) );
                        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                        var result = updateRequest.Execute();

                        litOutput.Text += "<br />Updated " + ( parent1Names.Values.Select( v => v[0] ).Where( v => v != null && v.ToString() != "" ).Count() ) + " Parent 1 Names on Sheet " + sheetName + " in Column " + GetAttributeValue( "Parent1Column" );
                    }
                    if ( GetAttributeValue( "Parent1PhoneColumn" ) != "" )
                    {
                        var updateRequest = service.Spreadsheets.Values.Update( parent1PhoneNumbers, spreadsheetId, sheetName + "!" + GetAttributeValue( "Parent1PhoneColumn" ) + GetAttributeValue( "FirstDataRow" ) + ":" + GetAttributeValue( "Parent1PhoneColumn" ) );
                        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                        var result = updateRequest.Execute();

                        litOutput.Text += "<br />Updated " + ( parent1PhoneNumbers.Values.Select( v => v[0] ).Where( v => v != null && v.ToString() != "" ).Count() ) + " Parent 1 Phone Numbers on Sheet " + sheetName + " in Column " + GetAttributeValue( "Parent1PhoneColumn" );
                    }
                    if ( GetAttributeValue( "Parent2Column" ) != "" )
                    {
                        var updateRequest = service.Spreadsheets.Values.Update( parent2Names, spreadsheetId, sheetName + "!" + GetAttributeValue( "Parent2Column" ) + GetAttributeValue( "FirstDataRow" ) + ":" + GetAttributeValue( "Parent2Column" ) );
                        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                        var result = updateRequest.Execute();

                        litOutput.Text += "<br />Updated " + ( parent2Names.Values.Select( v => v[0] ).Where( v => v != null && v.ToString() != "" ).Count() ) + " Parent 2 Names on Sheet " + sheetName + " in Column " + GetAttributeValue( "Parent2Column" );
                    }
                    if ( GetAttributeValue( "Parent2PhoneColumn" ) != "" )
                    {
                        var updateRequest = service.Spreadsheets.Values.Update( parent2PhoneNumbers, spreadsheetId, sheetName + "!" + GetAttributeValue( "Parent2PhoneColumn" ) + GetAttributeValue( "FirstDataRow" ) + ":" + GetAttributeValue( "Parent2PhoneColumn" ) );
                        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                        var result = updateRequest.Execute();

                        litOutput.Text += "<br />Updated " + ( parent2PhoneNumbers.Values.Select( v => v[0] ).Where( v => v != null && v.ToString() != "" ).Count() ) + " Parent 2 Phone Numbers on Sheet " + sheetName + " in Column " + GetAttributeValue( "Parent2PhoneColumn" );
                    }
                }
            }

            var missedPeople = group.Members.Where( gm => !allPersonIds.Values.Select( v => v[0] ).Contains( gm.PersonId.ToString() ) && gm.GroupMemberStatus == GroupMemberStatus.Active );

            foreach ( var person in missedPeople )
            {

                litErrors.Text += "Unable to find entry in Google Sheet for " + person.Person + "<br />";
                errorCount++;
            }

            litErrorsSummary.Text += errorCount + " Record Errors<br />";
            litSuccessSummary.Text += successCount + " Records Updated";
            litOutputSummary.Text = "General Information";

            mdShowOutput.Show();
        }

        public string ColumnAdress( int col )
        {
            col++;
            if ( col <= 26 )
            {
                return Convert.ToChar( col + 64 ).ToString();
            }
            int div = col / 26;
            int mod = col % 26;
            if ( mod == 0 )
            { mod = 26; div--; }
            return ColumnAdress( div ) + ColumnAdress( mod );
        }

        public int ColumnNumber( string colAdress )
        {
            int[] digits = new int[colAdress.Length];
            for ( int i = 0; i < colAdress.Length; ++i )
            {
                digits[i] = Convert.ToInt32( colAdress[i] ) - 64;
            }
            int mul = 1;
            int res = 0;
            for ( int pos = digits.Length - 1; pos >= 0; --pos )
            {
                res += digits[pos] * mul;
                mul *= 26;
            }
            return res - 1;
        }
    }
}
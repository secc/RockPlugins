﻿// <copyright>
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.Crm
{
    [DisplayName( "Person Search" )]
    [Category( "SECC > CRM" )]
    [Description( "Displays list of people that match a given search type and term." )]

    [LinkedPage( "Person Detail Page" )]
    [BooleanField( "Show Performance", "Displays how long the search took.", false )]
    public partial class PersonSearch : Rock.Web.UI.RockBlock
    {
        #region Fields

        private DefinedValueCache _inactiveStatus = null;
        private Stopwatch _sw = new Stopwatch();
        private Literal _lPerf = new Literal();
        #endregion

        #region Base Control Methods

        protected override void OnInit( EventArgs e )
        {
            _sw.Start();

            base.OnInit( e );

            RockPage.AddScriptLink( ResolveRockUrl( "~/Scripts/jquery.lazyload.min.js" ) );

            gPeople.DataKeyNames = new string[] { "Id" };
            gPeople.Actions.ShowAdd = false;
            gPeople.GridRebind += gPeople_GridRebind;
            gPeople.RowDataBound += gPeople_RowDataBound;
            gPeople.PersonIdField = "Id";

            if ( GetAttributeValue( "ShowPerformance" ).AsBoolean() )
            {
                gPeople.Actions.AddCustomActionControl( _lPerf );
            }
        }

        protected override void OnPreRender( EventArgs e )
        {
            base.OnPreRender( e );
            _sw.Stop();


            _lPerf.Text = string.Format( "<small class='pull-left' style='margin-top: 6px;'>Search time: {0} ms.</small>", _sw.Elapsed.Milliseconds );

        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                BindGrid();
            }
        }

        #endregion

        #region Events

        void gPeople_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        void gPeople_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            if ( e.Row.RowType == DataControlRowType.DataRow )
            {
                var person = e.Row.DataItem as PersonSearchResult;
                if ( person != null )
                {
                    if ( _inactiveStatus != null &&
                        person.RecordStatusValueId.HasValue &&
                        person.RecordStatusValueId.Value == _inactiveStatus.Id )
                    {
                        e.Row.AddCssClass( "inactive" );
                    }

                    if ( person.IsDeceased )
                    {
                        e.Row.AddCssClass( "deceased" );
                    }

                    string delimitedCampuses = string.Empty;
                    if ( person.CampusIds.Any() )
                    {
                        var campuses = new List<string>();
                        foreach ( var campusId in person.CampusIds )
                        {
                            var campus = CampusCache.Get( campusId );
                            if ( campus != null )
                            {
                                campuses.Add( campus.Name );
                            }
                        }
                        if ( campuses.Any() )
                        {
                            delimitedCampuses = campuses.AsDelimited( ", " );
                            var lCampus = e.Row.FindControl( "lCampus" ) as Literal;
                            if ( lCampus != null )
                            {
                                lCampus.Text = delimitedCampuses;
                            }
                        }
                    }

                    var lPerson = e.Row.FindControl( "lPerson" ) as Literal;

                    if ( !person.IsBusiness )
                    {
                        StringBuilder sbPersonDetails = new StringBuilder();
                        sbPersonDetails.Append( string.Format( "<div class=\"photo-round photo-round-sm pull-left\" data-original=\"{0}&w=100\" style=\"background-image: url('{1}');\"></div>", person.PhotoUrl, ResolveUrl( "~/Assets/Images/person-no-photo-male.svg" ) ) );
                        sbPersonDetails.Append( "<div class=\"pull-left margin-l-sm\">" );
                        sbPersonDetails.Append( string.Format( "<strong>{0}</strong> ", person.FullNameReversed ) );
                        sbPersonDetails.Append( string.Format( "<small class=\"hidden-sm hidden-md hidden-lg\"><br>{0}</br></small>", delimitedCampuses ) );
                        sbPersonDetails.Append( string.Format( "<small class=\"hidden-sm hidden-md hidden-lg\">{0}</small>", DefinedValueCache.GetName( person.ConnectionStatusValueId ) ) );
                        sbPersonDetails.Append( string.Format( " <small class=\"hidden-md hidden-lg\">{0}</small>", person.AgeFormatted ) );
                        if ( !string.IsNullOrWhiteSpace( person.Email ) )
                        {
                            sbPersonDetails.Append( string.Format( "<br/><small>{0}</small>", person.Email ) );
                        }

                        // add home addresses
                        foreach ( var location in person.HomeAddresses )
                        {
                            if ( string.IsNullOrWhiteSpace( location.Street1 ) &&
                                string.IsNullOrWhiteSpace( location.Street2 ) &&
                                string.IsNullOrWhiteSpace( location.City ) )
                            {
                                continue;
                            }

                            string format = string.Empty;
                            var countryValue = Rock.Web.Cache.DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.LOCATION_COUNTRIES.AsGuid() )
                                .DefinedValues
                                .Where( v => v.Value.Equals( location.Country, StringComparison.OrdinalIgnoreCase ) )
                                .FirstOrDefault();

                            if ( countryValue != null )
                            {
                                format = countryValue.GetAttributeValue( "AddressFormat" );
                            }

                            if ( !string.IsNullOrWhiteSpace( format ) )
                            {
                                var dict = location.ToDictionary();
                                dict["Country"] = countryValue.Description;
                                sbPersonDetails.Append( string.Format( "<small><br>{0}</small>", format.ResolveMergeFields( dict ).ConvertCrLfToHtmlBr().Replace( "<br/><br/>", "<br/>" ) ) );
                            }
                            else
                            {
                                sbPersonDetails.Append( string.Format( string.Format( "<small><br>{0}<br>{1} {2}, {3} {4}</small>", location.Street1, location.Street2, location.City, location.State, location.PostalCode ) ) );
                            }
                        }
                        sbPersonDetails.Append( "</div>" );

                        lPerson.Text = sbPersonDetails.ToString();
                    }
                    else
                    {
                        lPerson.Text = string.Format( "{0}", person.LastName );
                    }
                }
            }
        }

        protected void gPeople_RowSelected( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( "PersonDetailPage", "PersonId", ( int ) e.RowKeyId );
        }

        #endregion

        #region Internal Methods

        private void BindGrid()
        {

            string type = PageParameter( "SearchType" );
            string term = PageParameter( "SearchTerm" );

            if ( !string.IsNullOrWhiteSpace( type ) && !string.IsNullOrWhiteSpace( term ) )
            {
                term = term.Trim();
                type = type.Trim();
                var rockContext = new RockContext();

                var personService = new PersonService( rockContext );
                IQueryable<Person> people = null;

                switch ( type.ToLower() )
                {
                    case ( "name" ):
                        {
                            bool allowFirstNameOnly = false;
                            if ( !bool.TryParse( PageParameter( "allowFirstNameOnly" ), out allowFirstNameOnly ) )
                            {
                                allowFirstNameOnly = false;
                            }
                            people = personService.GetByFullName( term, allowFirstNameOnly, true );
                            break;
                        }
                    case ( "phone" ):
                        {
                            var phoneService = new PhoneNumberService( rockContext );
                            var personIds = phoneService.GetPersonIdsByNumber( term );
                            people = personService.Queryable().Where( p => personIds.Contains( p.Id ) );
                            break;
                        }
                    case ( "address" ):
                        {
                            var personIds2 = GetPersonIdsByHomePrevAddress( rockContext, term );
                            people = personService.Queryable().Where( p => personIds2.Contains( p.Id ) );
                            break;
                        }
                    case ( "email" ):
                        {
                            people = personService.Queryable().Where( p => p.Email.Contains( term ) );
                            break;
                        }
                    case ( "envelope" ):
                        {
                            var aService = new AttributeService( rockContext );
                            var avService = new AttributeValueService( rockContext );
                            var personEntityTypeId = EntityTypeCache.Get( Rock.SystemGuid.EntityType.PERSON.AsGuid() ).Id;
                            var attribute = aService.Queryable().Where( a => a.Key == "GivingEnvelopeNumber" && a.EntityTypeId == personEntityTypeId ).FirstOrDefault();
                            if ( attribute != null )
                            {
                                var personIds = avService.Queryable().Where( av => av.AttributeId == attribute.Id && av.Value.Equals( term ) ).Select( av => av.EntityId );
                                people = personService.Queryable().Where( p => personIds.Contains( p.Id ) );
                            }
                            break;
                        }
                    case ( "dob" ):
                        {
                            term = term.ToLower();
                            List<DateTime?> birthDateList = new List<DateTime?>();
                            var birthDates = personService.Queryable().Where( p => p.BirthDate.HasValue ).Select( p => p.BirthDate ).Distinct().AsEnumerable();
                            var shortDateSearch = birthDates.Where( p => p.Value.ToString( "d" ).ToLower().Contains( term ) || p.Value.ToString( "MM/dd/yyyy" ).ToLower().Contains( term ) );
                            var longDateSearch = birthDates.Where( p => p.Value.ToString( "MMMM d, yyyy" ).ToLower().Contains( term ) );
                            if ( shortDateSearch.Union( longDateSearch ).Any() )
                            {
                                birthDateList = shortDateSearch.Union( longDateSearch ).Distinct().ToList();
                            }
                            else
                            {
                                // Just get crazy with things (Yep, you can find out who was born on a Tuesday)
                                var longestDateSearch = birthDates.Where( p => p.Value.ToString( "D" ).ToLower().Contains( term ) );
                                birthDateList = longestDateSearch.Distinct().ToList();

                            }
                            gPeople.Columns[3].Visible = true;
                            people = personService.Queryable().Where( p => birthDateList.Contains( p.BirthDate ) );
                            break;
                        }

                }

                SortProperty sortProperty = gPeople.SortProperty;
                if ( sortProperty != null )
                {
                    people = people.Sort( sortProperty );
                }
                else
                {
                    people = people.OrderBy( p => p.LastName ).ThenBy( p => p.FirstName );
                }

                Guid familyGuid = new Guid( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY );
                Guid homeAddressTypeGuid = new Guid( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME );

                var personList = people.Select( p => new PersonSearchResult
                {
                    Id = p.Id,
                    FirstName = p.FirstName,
                    NickName = p.NickName,
                    LastName = p.LastName,
                    BirthDate = p.BirthDate,
                    BirthYear = p.BirthYear,
                    BirthMonth = p.BirthMonth,
                    BirthDay = p.BirthDay,
                    ConnectionStatusValueId = p.ConnectionStatusValueId,
                    RecordStatusValueId = p.RecordStatusValueId,
                    RecordTypeValueId = p.RecordTypeValueId,
                    SuffixValueId = p.SuffixValueId,
                    IsDeceased = p.IsDeceased,
                    Email = p.Email,
                    Gender = p.Gender,
                    PhotoId = p.PhotoId,
                    CampusIds = new List<int> { p.PrimaryFamily.CampusId ?? 0 },
                    HomeAddresses = p.PrimaryFamily.GroupLocations.Select( gl => gl.Location )
                } ).ToList();

                if ( personList.Count == 1 )
                {
                    Response.Redirect( string.Format( "~/Person/{0}", personList[0].Id ), false );
                    Context.ApplicationInstance.CompleteRequest();
                }
                else
                {
                    if ( type.ToLower() == "name" )
                    {
                        var similarNames = personService.GetSimilarNames( term,
                            personList.Select( p => p.Id ).ToList(), true );
                        if ( similarNames.Any() )
                        {
                            var hyperlinks = new List<string>();
                            foreach ( string name in similarNames.Distinct() )
                            {
                                var pageRef = CurrentPageReference;
                                pageRef.Parameters["SearchTerm"] = name;
                                hyperlinks.Add( string.Format( "<a href='{0}'>{1}</a>", pageRef.BuildUrl(), name ) );
                            }
                            string altNames = string.Join( ", ", hyperlinks );
                            nbNotice.Text = string.Format( "Other Possible Matches: {0}", altNames );
                            nbNotice.Visible = true;
                        }
                    }

                    _inactiveStatus = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE );
                    gPeople.EntityTypeId = EntityTypeCache.GetId<Person>();

                    gPeople.DataSource = personList;
                    gPeople.DataBind();
                }
            }
        }

        /// <summary>
        /// Gets a list of <see cref="System.Int32"/> PersonIds whose home or previous address matches the given search value.
        /// </summary>
        /// <param name="partialHomePrevAddress">a partial address search string</param>
        /// <returns>A queryable list of <see cref="System.Int32"/> PersonIds</returns>
        private IQueryable<int> GetPersonIdsByHomePrevAddress( RockContext rockContext, string partialHomePrevAddress )
        {
            Guid groupTypefamilyGuid = new Guid( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY );
            Guid homeAddressTypeGuid = new Guid( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME );
            Guid previousAddressTypeGuid = new Guid( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_PREVIOUS );
            var homeAddressTypeValueId = DefinedValueCache.Get( homeAddressTypeGuid, rockContext ).Id;
            var previousAddressTypeValueId = DefinedValueCache.Get( previousAddressTypeGuid, rockContext ).Id;

            var service = new GroupMemberService( rockContext );

            //the search bar will pre fill out some search parameters
            //this splits them apart so we can search
            var split = partialHomePrevAddress.Replace( " - ", "^" )
                .Split( new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries );

            if ( split.Length > 1 )
            {
                var street = split[0];
                var postalcode = split[1];

                return service.Queryable()
                    .Where( m => m.Group.GroupType.Guid == groupTypefamilyGuid )
                    .SelectMany( g => g.Group.GroupLocations )
                    .Where( gl => ( gl.GroupLocationTypeValueId == homeAddressTypeValueId || gl.GroupLocationTypeValueId == previousAddressTypeValueId ) &&
                        ( gl.Location.Street1.Contains( street )
                        && gl.Location.PostalCode.Contains( postalcode ) ) )
                    .SelectMany( gl => gl.Group.Members )
                    .Select( gm => gm.PersonId )
                    .Distinct();
            }

            return service.Queryable()
                .Where( m => m.Group.GroupType.Guid == groupTypefamilyGuid )
                .SelectMany( g => g.Group.GroupLocations )
                .Where( gl => ( gl.GroupLocationTypeValueId == homeAddressTypeValueId || gl.GroupLocationTypeValueId == previousAddressTypeValueId ) &&
                        ( gl.Location.Street1.Contains( partialHomePrevAddress )
                        || gl.Location.PostalCode.Contains( partialHomePrevAddress )
                        )
                    )
                .SelectMany( gl => gl.Group.Members )
                .Select( gm => gm.PersonId )
                .Distinct();
        }
        #endregion

    }
    #region result models
    public class PersonSearchResult
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>
        /// The id.
        /// </value>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the full name last first.
        /// </summary>
        /// <value>
        /// The full name last first.
        /// </value>
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the name of the nick.
        /// </summary>
        /// <value>
        /// The name of the nick.
        /// </value>
        public string NickName { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is business.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is business; otherwise, <c>false</c>.
        /// </value>
        public bool IsBusiness
        {
            get
            {
                int recordTypeValueIdBusiness = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_BUSINESS.AsGuid() ).Id;
                return this.RecordTypeValueId.HasValue && this.RecordTypeValueId.Value == recordTypeValueIdBusiness;
            }
        }

        /// <summary>
        /// Gets or sets the home addresses.
        /// </summary>
        /// <value>
        /// The home addresses.
        /// </value>
        public IEnumerable<Location> HomeAddresses { get; set; }

        /// <summary>
        /// Gets the photo URL.
        /// </summary>
        /// <value>
        /// The photo URL.
        /// </value>
        public string PhotoUrl
        {
            get
            {
                if ( RecordTypeValueId.HasValue )
                {
                    var recordType = DefinedValueCache.Get( RecordTypeValueId.Value );
                    if ( recordType != null )
                    {
                        return Person.GetPersonPhotoUrl( this.Id, 200, 200 );
                    }
                }
                return Person.GetPersonPhotoUrl( this.Id, 200, 200 );
            }
            private set { }
        }

        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        /// <value>
        /// The last name.
        /// </value>
        public string LastName { get; set; }

        /// <summary>
        /// Gets the full name reversed.
        /// </summary>
        /// <value>
        /// The full name reversed.
        /// </value>
        public virtual string FullNameReversed
        {
            get
            {
                if ( this.IsBusiness )
                {
                    return LastName;
                }

                var fullName = new StringBuilder();

                fullName.Append( LastName );

                // Use the SuffixValueId and DefinedValue cache instead of referencing SuffixValue property so 
                // that if FullName is used in datagrid, the SuffixValue is not lazy-loaded for each row
                if ( SuffixValueId.HasValue )
                {
                    var suffix = DefinedValueCache.GetName( SuffixValueId.Value );
                    if ( suffix != null )
                    {
                        fullName.AppendFormat( " {0}", suffix );
                    }
                }

                fullName.AppendFormat( ", {0}", NickName );
                return fullName.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the birth date.
        /// </summary>
        /// <value>
        /// The birth date.
        /// </value>
        public DateTime? BirthDate { get; set; }

        /// <summary>
        /// Gets or sets the birth year.
        /// </summary>
        /// <value>
        /// The birth year.
        /// </value>
        public int? BirthYear { get; set; }

        /// <summary>
        /// Gets or sets the email.
        /// </summary>
        /// <value>
        /// The email.
        /// </value>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the photo identifier.
        /// </summary>
        /// <value>
        /// The photo identifier.
        /// </value>
        public int? PhotoId { get; set; }

        /// <summary>
        /// Gets or sets the birth month.
        /// </summary>
        /// <value>
        /// The birth month.
        /// </value>
        public int? BirthMonth { get; set; }

        /// <summary>
        /// Gets or sets the birth day.
        /// </summary>
        /// <value>
        /// The birth day.
        /// </value>
        public int? BirthDay { get; set; }

        /// <summary>
        /// Gets or sets the families.
        /// </summary>
        /// <value>
        /// The families.
        /// </value>
        public List<int> CampusIds { get; set; }

        /// <summary>
        /// Gets or sets the gender.
        /// </summary>
        /// <value>The gender.</value>
        public Gender Gender { get; set; }

        /// <summary>
        /// Gets or sets the is deceased.
        /// </summary>
        /// <value>
        /// The is deceased.
        /// </value>
        public bool IsDeceased { get; set; }

        /// <summary>
        /// Gets the age.
        /// </summary>
        /// <value>
        /// The age.
        /// </value>
        public int? Age
        {
            get
            {
                if ( BirthYear.HasValue )
                {
                    DateTime? bd = BirthDate;
                    if ( bd.HasValue )
                    {
                        DateTime today = RockDateTime.Today;
                        int age = today.Year - bd.Value.Year;
                        if ( bd.Value > today.AddYears( -age ) )
                            age--;
                        return age;
                    }
                }
                return null;
            }
            private set { }
        }

        /// <summary>
        /// Gets the age formatted.
        /// </summary>
        /// <value>
        /// The age formatted.
        /// </value>
        public string AgeFormatted
        {
            get
            {
                if ( this.Age.HasValue )
                {
                    return string.Format( "({0})", this.Age.Value.ToString() );
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets or sets the connection status.
        /// </summary>
        /// <value>The connection status.</value>
        public int? ConnectionStatusValueId { get; set; }

        /// <summary>
        /// Gets or sets the record type value.
        /// </summary>
        /// <value>
        /// The record type value.
        /// </value>
        public int? RecordTypeValueId { get; set; }

        /// <summary>
        /// Gets or sets the suffix value.
        /// </summary>
        /// <value>
        /// The suffix value.
        /// </value>
        public int? SuffixValueId { get; set; }

        /// <summary>
        /// Gets or sets the record status.
        /// </summary>
        /// <value>The member status.</value>
        public int? RecordStatusValueId { get; set; }

        /// <summary>
        /// Gets or sets the name of the spouse.
        /// </summary>
        /// <value>
        /// The name of the spouse.
        /// </value>
        public string SpouseName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is active.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is active; otherwise, <c>false</c>.
        /// </value>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the picker item details HTML.
        /// </summary>
        /// <value>
        /// The picker item details HTML.
        /// </value>
        public string PickerItemDetailsHtml { get; set; }
    }
    #endregion
}
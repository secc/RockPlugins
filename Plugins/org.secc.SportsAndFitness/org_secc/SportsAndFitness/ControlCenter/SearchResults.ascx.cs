using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Web.UI;

using org.secc.DevLib.SportsAndFitness;
using Rock.Data;
using Rock.Security.Authentication;
using System.Data.Entity;

namespace RockWeb.Plugins.org_secc.SportsAndFitness.ControlCenter
{

    [DisplayName("Search Results")]
    [Category("Sports and Fitness > Control Center")]
    [Description("Sports and Fitness Participant Search Results")]

    [LinkedPage("Person Detail",
        Description = "Person Detail Page",
        IsRequired = true,
        Order = 0,
        Key = AttributeKey.PersonDetail)]
    [DefinedValueField("Sports and Fitness PIN Purpose",
        Description = "The Defined Value that identifies that a PIN can be used for Sports & Fitness",
        AllowAddingNewValues = false,
        AllowMultiple = false,
        DefinedTypeGuid = "4a5527c1-4bab-4de7-849c-27ea3e6f14c9",
        Order = 1,
        Key = AttributeKey.PINPurposeDV )]

    public partial class SearchResults : RockBlock
    {
        #region Attribute Keys
        public static class AttributeKey
        {
            public const string PersonDetail = "PersonDetail";
            public const string PINPurposeDV = "PINPurposeDV";
        }
        #endregion Attribute Keys

        private ControlCenterSearchItem SearchValue
        {
            get
            {
                if (Session["ControlCenterSearch"] != null)
                {
                    var searchString = Session["ControlCenterSearch"].ToString();
                    if (searchString.IsNotNullOrWhiteSpace())
                    {
                        return searchString.FromJsonOrNull<ControlCenterSearchItem>();
                    }
                }
                return null;
            }
        }

        #region Base Control Methods
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            this.BlockUpdated += SearchResults_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upMain );
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            pnlDebug.Visible = false;
            nbAlert.Visible = false;

            if (SearchValue != null)
            {
                pnlDebug.Visible = true;
                lSearchTerm.Text = SearchValue.SearchTerm;
                lSearchByPIN.Text = SearchValue.SearchByPIN.ToYesNo();
                lSearchByPhone.Text = SearchValue.SearchByPhone.ToYesNo();
                LoadSearchResults();
                
            }
            else
            {
                nbAlert.Title = "Search Value Not Found";
                nbAlert.Visible = true;
            }
        }
        #endregion

        #region Events
        private void SearchResults_BlockUpdated( object sender, EventArgs e )
        {
            NavigateToCurrentPage();
        }

        #endregion

        #region Internal Methods

        public IEnumerable<int> SearchByName(RockContext rockContext, string searchValue)
        {
            var inactivePersonDV = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE );
            var personRecordTypeDV = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON );
            var personService = new PersonService( rockContext );

            var searchValueSplit = searchValue.SplitDelimitedValues();
            var personIds = new List<int>();

            var qry = personService.Queryable().AsNoTracking()
                .Where( p => p.RecordTypeValueId == personRecordTypeDV.Id )
                .Where( p => p.RecordStatusValueId != inactivePersonDV.Id );

            if(searchValueSplit.Length == 1)
            {
                var splitValue0 = searchValueSplit[0];
                qry = qry.Where( p => p.FirstName.StartsWith( splitValue0 )
                        || p.NickName.StartsWith( splitValue0 )
                        || p.LastName.StartsWith( splitValue0 ) );

                personIds.AddRange( qry.Select( p => p.Id ).ToList() );

            }
            else if(searchValueSplit.Length > 1)
            {
                var splitValue0 = searchValueSplit[0];
                var splitValue1 = searchValueSplit[1];
                qry = qry.Where( p => p.FirstName.StartsWith( splitValue0 )
                        || p.FirstName.StartsWith( splitValue1 )
                        || p.NickName.StartsWith( splitValue0 )
                        || p.NickName.StartsWith( splitValue1 ) )
                    .Where( p => p.LastName.StartsWith( splitValue0 )
                        || p.LastName.StartsWith( splitValue1 ) );

                personIds.AddRange( qry.Select( p => p.Id ).ToList() );
            }

            return personIds;
        }

        public IEnumerable<int> SearchByPIN( RockContext rockContext, string searchValue )
        {
            var pinAuthenticationEntityType = EntityTypeCache.Get( typeof( PINAuthentication ) );
            var inactivePersonDV = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE );
            var userLogin = new UserLoginService( rockContext ).Queryable().AsNoTracking()
                .Where( u => u.EntityTypeId == pinAuthenticationEntityType.Id )
                .Where( u => u.IsLockedOut != false )
                .Where( u => u.UserName.Equals( searchValue ) )
                .Where( u => u.Person.RecordStatusValueId != inactivePersonDV.Id)
                .FirstOrDefault();

            if(userLogin == null)
            {
                return new List<int>();
            }

            var SFPINPurposeDVGuid = GetAttributeValue( AttributeKey.PINPurposeDV );
            userLogin.LoadAttributes();

            var isSportsPin = userLogin.GetAttributeValue( "PINPurpose" ).Split( ",".ToCharArray() )
                .Contains( SFPINPurposeDVGuid );

            if(isSportsPin)
            {
                return new List<int> { userLogin.PersonId.Value };
            }

            return new List<int>();
            ;
        }

        public IEnumerable<int> SearchByPhone(RockContext rockContext, string searchValue)
        {
            var phoneNumberService = new PhoneNumberService( rockContext );
            var phoneCleaned = PhoneNumber.CleanNumber( searchValue );
            var inactivePersonDV = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE );
            var personPersonTypeDV = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON );

            if(phoneCleaned.Length < 7)
            {
                return new List<int>();
            }

            return phoneNumberService.Queryable().AsNoTracking()
                .Where( p => p.Number.StartsWith( phoneCleaned )
                        || p.Number.EndsWith(phoneCleaned))
                .Where( p => p.Person.RecordTypeValueId == personPersonTypeDV.Id )
                .Where( p => p.Person.RecordStatusValueId != inactivePersonDV.Id )
                .Select( p => p.PersonId )
                .ToList();
        }



        private void LoadSearchResults()
        {
            var rockContext = new RockContext();

            var inputAsNumeric = new String( SearchValue.SearchTerm.Where( Char.IsDigit ).ToArray() );

            List<int> personIds = new List<int>();

            if(inputAsNumeric.IsNotNullOrWhiteSpace())
            {
                personIds.Add( inputAsNumeric.AsInteger() );

                if (SearchValue.SearchByPIN)
                {
                    personIds.AddRange( SearchByPIN( rockContext, inputAsNumeric ) );
                }

                if (SearchValue.SearchByPhone)
                {
                    personIds.AddRange( SearchByPhone( rockContext, inputAsNumeric ) );
                }
            }

            personIds.AddRange( SearchByName( rockContext, SearchValue.SearchTerm ) );

            lResultCount.Text = new PersonService( rockContext ).Queryable().AsNoTracking()
                .Where( p => personIds.Contains( p.Id ) )
                .Count().ToString();
        }

        #endregion

    }
}
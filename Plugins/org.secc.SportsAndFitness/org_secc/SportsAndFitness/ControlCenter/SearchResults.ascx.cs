using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI.WebControls;
using DotLiquid;
using org.secc.DevLib.SportsAndFitness;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Lava;
using Rock.Model;
using Rock.Security.Authentication;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.SportsAndFitness.ControlCenter
{

    [DisplayName( "Search Results" )]
    [Category( "Sports and Fitness > Control Center" )]
    [Description( "Sports and Fitness Participant Search Results" )]

    [LinkedPage( "Person Detail",
        Description = "Person Detail Page",
        IsRequired = true,
        Order = 0,
        Key = AttributeKey.PersonDetail )]
    [DefinedValueField( "Sports and Fitness PIN Purpose",
        Description = "The Defined Value that identifies that a PIN can be used for Sports & Fitness",
        AllowAddingNewValues = false,
        AllowMultiple = false,
        DefinedTypeGuid = "4a5527c1-4bab-4de7-849c-27ea3e6f14c9",
        Order = 1,
        Key = AttributeKey.PINPurposeDV )]
    [CodeEditorField( "Results Lava",
        Description = "Lava to display result list",
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        EditorHeight = 600,
        IsRequired = false,
        Order = 2,
        Key = AttributeKey.ResultLavaTemplate )]
    [LavaCommandsField( "Lava Commands",
        Description = "Enabled Lava Commands",
        IsRequired = false,
        Order = 3,
        Key = AttributeKey.LavaCommands )]
    [BooleanField("Show Debug Panel",
        Description = "Should the Debug Panel be displayed",
        DefaultBooleanValue = false,
        IsRequired = false,
        Order = 4,
        Key = AttributeKey.ShowDebugPanel)]

    public partial class SearchResults : RockBlock
    {
        #region Attribute Keys
        public static class AttributeKey
        {
            public const string PersonDetail = "PersonDetail";
            public const string PINPurposeDV = "PINPurposeDV";
            public const string LavaCommands = "LavaCommands";
            public const string ResultLavaTemplate = "ResultsTemplate";
            public const string ShowDebugPanel = "ShowDebugPanel";

        }
        #endregion Attribute Keys

        Guid SportsAndFitnessGroupGuid = "9ce3122e-8d0c-4ae9-9a8b-7a839c67d310".AsGuid();
        Guid GroupFitnessGroupGuid = "b7e40984-8a03-4113-8b1a-b154c0a00d8b".AsGuid();
        Guid PickleballGroupGuid = "658cce25-9cc5-4329-9a43-8adb5cda2a76".AsGuid();

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
                bool showDebugPanel = GetAttributeValue( AttributeKey.ShowDebugPanel ).AsBoolean();

                if (showDebugPanel)
                {
                    pnlDebug.Visible = true;
                    lSearchTerm.Text = SearchValue.SearchTerm;
                    lSearchByPIN.Text = SearchValue.SearchByPIN.ToYesNo();
                    lSearchByPhone.Text = SearchValue.SearchByPhone.ToYesNo();
                }
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

        public IEnumerable<int> SearchByName( RockContext rockContext, string searchValue )
        {
            var inactivePersonDV = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE );
            var personRecordTypeDV = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON );
            var personService = new PersonService( rockContext );

            var searchValueSplit = searchValue.SplitDelimitedValues();
            var personIds = new List<int>();

            var qry = personService.Queryable().AsNoTracking()
                .Where( p => p.RecordTypeValueId == personRecordTypeDV.Id )
                .Where( p => p.RecordStatusValueId != inactivePersonDV.Id );

            if (searchValueSplit.Length == 1)
            {
                var splitValue0 = searchValueSplit[0];
                qry = qry.Where( p => p.FirstName.StartsWith( splitValue0 )
                        || p.NickName.StartsWith( splitValue0 )
                        || p.LastName.StartsWith( splitValue0 ) );

                personIds.AddRange( qry.Select( p => p.Id ).ToList() );

            }
            else if (searchValueSplit.Length > 1)
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
                .Where( u => u.Person.RecordStatusValueId != inactivePersonDV.Id )
                .FirstOrDefault();

            if (userLogin == null)
            {
                return new List<int>();
            }

            var SFPINPurposeDVGuid = GetAttributeValue( AttributeKey.PINPurposeDV );
            var SPPurposeDV = DefinedValueCache.Get( SFPINPurposeDVGuid );
            userLogin.LoadAttributes();

            var isSportsPin = userLogin.GetAttributeValue( "PINPurpose" ).Split( ",".ToCharArray() )
                .Contains( SPPurposeDV.Id.ToString() );

            if (isSportsPin)
            {
                return new List<int> { userLogin.PersonId.Value };
            }

            return new List<int>();
            ;
        }

        public IEnumerable<int> SearchByPhone( RockContext rockContext, string searchValue )
        {
            var phoneNumberService = new PhoneNumberService( rockContext );
            var phoneCleaned = PhoneNumber.CleanNumber( searchValue );
            var inactivePersonDV = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE );
            var personPersonTypeDV = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON );

            if (phoneCleaned.Length < 7)
            {
                return new List<int>();
            }

            return phoneNumberService.Queryable().AsNoTracking()
                .Where( p => p.Number.StartsWith( phoneCleaned )
                        || p.Number.EndsWith( phoneCleaned ) )
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

            if (inputAsNumeric.IsNotNullOrWhiteSpace())
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

            var mobilePhoneDV = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE );


            var personService = new PersonService( rockContext );
            var mobilePhoneQry = new PhoneNumberService( rockContext ).Queryable().AsNoTracking()
                .Where( ph => ph.IsUnlisted == false )
                .Where( ph => ph.NumberTypeValueId == mobilePhoneDV.Id );

            var sportsandFitnessMembers = new GroupMemberService( rockContext ).Queryable()
                .Where( m => !m.IsArchived )
                .Where( m => m.GroupMemberStatus == GroupMemberStatus.Active )
                .Where( m => m.Group.Guid == SportsAndFitnessGroupGuid );

            var groupFitnessMembers = new GroupMemberService( rockContext ).Queryable()
                .Where( m => !m.IsArchived )
                .Where( m => m.GroupMemberStatus == GroupMemberStatus.Active )
                .Where( m => m.Group.Guid == GroupFitnessGroupGuid );

            var pickleBallMembers = new GroupMemberService( rockContext ).Queryable()
                .Where( m => !m.IsArchived )
                .Where( m => m.GroupMemberStatus == GroupMemberStatus.Active )
                .Where( m => m.Group.Guid == PickleballGroupGuid );

            var results = personService.Queryable()
                .Where( p => personIds.Contains( p.Id ) )
                .GroupJoin( mobilePhoneQry, p => p.Id, m => m.PersonId,
                    ( p, m ) => new { Person = p, MobilePhone = m.Select( m1 => m1.NumberFormatted ).FirstOrDefault() } )
                .GroupJoin( sportsandFitnessMembers, p => p.Person.Id, sm => sm.PersonId,
                    ( p, sm ) => new { p.Person, p.MobilePhone, SportsAndFitnessMemberID = sm.Select( sm1 => sm1.Id ).FirstOrDefault() } )
                .GroupJoin( groupFitnessMembers, p => p.Person.Id, gm => gm.PersonId,
                    ( p, gm ) => new { p.Person, p.MobilePhone, p.SportsAndFitnessMemberID, GroupFitnessMemberId = gm.Select( gm1 => gm1.Id ).FirstOrDefault() } )
                .GroupJoin( pickleBallMembers, p => p.Person.Id, pm => pm.PersonId,
                    ( p, pm ) => new { p.Person, p.MobilePhone, p.SportsAndFitnessMemberID, p.GroupFitnessMemberId, PickleballMemberId = pm.Select( pm1 => pm1.Id ).FirstOrDefault() } )
                .Select( p => new PersonResults
                {
                    PersonResult = p.Person,
                    ConnectionStatusValue = p.Person.ConnectionStatusValue.Value,
                    RecordStatusValue = p.Person.RecordStatusValue.Value,
                    SportsAndFitnessMemberId = p.SportsAndFitnessMemberID,
                    GroupFitnessMemberId = p.GroupFitnessMemberId,
                    PickleBallMemberId = p.PickleballMemberId,
                    MobilePhone = p.MobilePhone
                } )
                .OrderBy(p => p.PersonResult.LastName)
                .ThenBy(p => p.PersonResult.NickName)
                .ToList();

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, CurrentPerson, new Rock.Lava.CommonMergeFieldsOptions { GetLegacyGlobalMergeFields = false } );
            mergeFields.Add( "SearchResults", results );
            mergeFields.Add( "LinkedPageUrl", LinkedPageRoute( AttributeKey.PersonDetail ) );

            lResults.Text = GetAttributeValue( AttributeKey.ResultLavaTemplate ).ResolveMergeFields( mergeFields, GetAttributeValue( AttributeKey.LavaCommands ) );

            if(GetAttributeValue(AttributeKey.ShowDebugPanel).AsBoolean())
            {
                lResultCount.Text = results.Count().ToString();
            }
        }

        #endregion
    }

    public class PersonResults : Rock.Lava.ILiquidizable

    {
        public Person PersonResult { get; set; }
        public string ConnectionStatusValue { get; set; }
        public string RecordStatusValue { get; set; }
        public int? SportsAndFitnessMemberId { get; set; }
        public int? GroupFitnessMemberId { get; set; }
        public int? PickleBallMemberId { get; set; }
        public string MobilePhone { get; set; }

        public object ToLiquid()
        {
            return this;
        }

       [LavaHidden]
        public List<string> AvailableKeys
        {
            get
            {
                var availableKeys = new List<string>
                    {
                        "PersonResult",
                        "ConnectionStatusValue",
                        "RecordStatusValue",
                        "SportsAndFitnessMemberId",
                        "GroupFitnessMemberId",
                        "PickleBallMemberId",
                        "MobilePhone"
                    };

                return availableKeys;
            }
        }

        [LavaHidden]
        public object this[object key]
        {
            get
            {
                switch (key.ToStringSafe())
                {
                    case "PersonResult":
                        return PersonResult;
                    case "ConnectionStatusValue":
                        return ConnectionStatusValue;
                    case "RecordStatusValue":
                        return RecordStatusValue;
                    case "SportsAndFitnessMemberId":
                        return SportsAndFitnessMemberId;
                    case "GroupFitnessMemberId":
                        return GroupFitnessMemberId;
                    case "PickleBallMemberId":
                        return PickleBallMemberId;
                    case "MobilePhone":
                        return MobilePhone;
                    default:
                        return string.Empty;
                }
            }
        }

        public bool ContainsKey( object key )
        {
            var keyList = new List<string>
                {
                    "PersonResult",
                    "ConnectionStatusValue",
                    "RecordStatusValue",
                    "SportsAndFitnessMemberId",
                    "GroupFitnessMemberId",
                    "PickleBallMemberId",
                    "MemberPhone"
                };


            return keyList.Contains( key.ToStringSafe() );
        }
    }

}
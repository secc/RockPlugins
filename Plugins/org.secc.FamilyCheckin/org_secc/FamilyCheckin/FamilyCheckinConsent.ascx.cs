using System;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI.WebControls;
using org.secc.FamilyCheckin.Cache;
using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;



namespace RockWeb.Plugins.org_secc.FamilyCheckin
{

    [DisplayName( "Family Check-In Consent" )]
    [Category( "SECC > Check-in" )]
    [Description( "Family Check-In Consent screen." )]
    [CodeEditorField( "Verify Information Text",
        Description = "The information to display on the verify information screen.",
        EditorMode = Rock.Web.UI.Controls.CodeEditorMode.Lava,
        EditorTheme = Rock.Web.UI.Controls.CodeEditorTheme.Rock,
        Key = "VerifyInfoText",
        Order = 0 )]
    [CodeEditorField("Medical Consent Text",
        Description = "Message to display for medical consent.",
        EditorMode = Rock.Web.UI.Controls.CodeEditorMode.Lava,
        EditorTheme = Rock.Web.UI.Controls.CodeEditorTheme.Rock,
        Key = "MedicalConsentText",
        Order = 1)]
    [LavaCommandsField( "Lava Commands",
        Description = "Lava commands that are enabled on this block.",
        IsRequired = false,
        Key = "LavaCommands",
        Order = 2 )]



    public partial class FamilyCheckinConsent : CheckInBlock
    {
        #region Fields
        KioskTypeCache KioskType = null;
        int AdultAge = 18;
        #endregion

        #region Control Events
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            btnInfoNo.Click += btnInfo;
            btnInfoYes.Click += btnInfo;
            btnMedicalIConsentYes.Click += btnMedicalIConsentYes_Click;
            btnMedicalConsentSkip.Click += btnMedicalConsentSkip_Click;

            var kioskTypeCookie = this.Page.Request.Cookies["KioskTypeId"];
            if (kioskTypeCookie != null)
            {
                KioskType = KioskTypeCache.Get( kioskTypeCookie.Value.AsInteger() );
            }

            if (KioskType == null)
            {
                NavigateToHomePage();
            }

        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if (!IsPostBack)
            {
                if (!FamilyNeedsConsent())
                {
                    NavigateToNextPage();
                    return;
                }
                BuildInfoVerifyPanel();

            }
        }
        #endregion 

        #region Event
        private void btnInfo( object sender, EventArgs e )
        {
            pnlVerifyInfo.Visible = false;
            BuildMedicalConsent();
        }

        private void btnMedicalIConsentYes_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();
            Person parentGuardian = null;

            var familyId = CurrentCheckInState.CheckIn.CurrentFamily.Group.Id;
            var phoneSearchType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_PHONE_NUMBER.AsGuid() );
            var personActiveStatus = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() );
            var userLoginSearchType = DefinedValueCache.Get( org.secc.FamilyCheckin.Utilities.Constants.CHECKIN_SEARCH_TYPE_USERLOGIN.AsGuid() );

            if(CurrentCheckInState.CheckIn.SearchType.Id == phoneSearchType.Id)
            {
                var searchValue = CurrentCheckInState.CheckIn.SearchValue;
                parentGuardian = new PersonService( rockContext ).Queryable().AsNoTracking()
                    .Where( p => p.PrimaryFamilyId == familyId )
                    .Where( p => p.AgeClassification == AgeClassification.Adult )
                    .Where( p => p.PhoneNumbers.Where( n => n.Number.EndsWith( searchValue ) ).Any() )
                    .OrderBy(p => p.Id)
                    .FirstOrDefault();
            }
            else if (CurrentCheckInState.CheckIn.SearchType.Id == userLoginSearchType.Id)
            {
                var searchValue = CurrentCheckInState.CheckIn.SearchValue;
                parentGuardian = new UserLoginService( rockContext ).Queryable().AsNoTracking()
                    .Where( u => u.UserName == searchValue )
                    .Select( u => u.Person )
                    .FirstOrDefault();
            }
            else if (CurrentCheckInState.CheckIn.CheckedInByPersonAliasId.HasValue)
            {
                parentGuardian = new PersonAliasService( rockContext )
                    .GetPerson( CurrentCheckInState.CheckIn.CheckedInByPersonAliasId.Value );
            }
            var adultBirthDate = RockDateTime.Today.AddYears( -AdultAge );
            var children = new PersonService(rockContext).Queryable()
                .Where(c => c.PrimaryFamilyId == familyId)
                .Where(c => c.RecordStatusValueId == personActiveStatus.Id)
                .Where( c => c.AgeClassification == AgeClassification.Child )
                .Where( c => c.BirthDate > adultBirthDate )
                .ToList();

            foreach (var child in children)
            {
                string consentAttributeValue = $"{parentGuardian.FullName} {RockDateTime.Today.ToShortDateString()}";
                child.LoadAttributes( rockContext );
                child.SetAttributeValue( "MedicalConsent", consentAttributeValue );
                child.SaveAttributeValue( "MedicalConsent", rockContext );
                rockContext.SaveChanges();
            }
            NavigateToNextPage();

        }

        private void btnMedicalConsentSkip_Click( object sender, EventArgs e )
        {
            NavigateToNextPage();
        }
        #endregion

        #region Methods
        private void BuildInfoVerifyPanel()
        {
            var rockContext = new RockContext();
            var personService = new PersonService( rockContext );

            var familyId = CurrentCheckInState.CheckIn.CurrentFamily.Group.Id;
            var phoneSearchType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_PHONE_NUMBER.AsGuid() );
            var userLoginSearchType = DefinedValueCache.Get( org.secc.FamilyCheckin.Utilities.Constants.CHECKIN_SEARCH_TYPE_USERLOGIN.AsGuid() );
            Person phoneOwner = null;

            if (CurrentCheckInState.CheckIn.SearchType.Id == phoneSearchType.Id)
            {
                var searchValue = CurrentCheckInState.CheckIn.SearchValue;
                phoneOwner = personService.Queryable().AsNoTracking()
                    .Where( p => p.PrimaryFamilyId == familyId )
                    .Where( p => p.AgeClassification == AgeClassification.Adult )
                    .Where( p => p.PhoneNumbers.Where( n => n.Number.EndsWith( searchValue ) ).Any() )
                    .FirstOrDefault();

                if (phoneOwner == null)
                {
                    NavigateToNextPage();
                    return;
                }
            }
            if(CurrentCheckInState.CheckIn.SearchType.Id == userLoginSearchType.Id )
            {
                var searchValue = CurrentCheckInState.CheckIn.SearchValue;
                phoneOwner = new UserLoginService( rockContext ).Queryable().AsNoTracking()
                    .Where( u => u.UserName == searchValue )
                    .Select( u => u.Person )
                    .FirstOrDefault();

                if(phoneOwner == null)
                {
                    NavigateToNextPage();
                    return;
                }
            }
            else if(CurrentCheckInState.CheckIn.CheckedInByPersonAliasId.HasValue)
            {
                phoneOwner = new PersonAliasService( rockContext ).GetPerson( CurrentCheckInState.CheckIn.CheckedInByPersonAliasId.Value );
            }
            else
            {
                NavigateToNextPage();
                return;
            }


            var activePersonStatusId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() ).Id;
            var adultBirthdate = RockDateTime.Today.AddYears( -AdultAge );
            var family = personService.Queryable().AsNoTracking()
                .Where(p => p.PrimaryFamilyId == familyId)
                .Where(p => p.RecordStatusValueId == activePersonStatusId)
                .Where(p => p.Id != phoneOwner.Id)                
                .ToList();


            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage );
            mergeFields.Add( "Person", phoneOwner );
            mergeFields.Add( "Spouse", family.Where( f => f.AgeClassification == AgeClassification.Adult && f.BirthDate <= adultBirthdate ).FirstOrDefault() );
            mergeFields.Add( "Children", family.Where( p => p.BirthDate > adultBirthdate ).OrderBy( p => p.BirthDate ).ToList() );
            mergeFields.Add( "FamilyName", CurrentCheckInState.CheckIn.CurrentFamily.Group.Name );

            var lavaTemplate = GetAttributeValue( "VerifyInfoText" );
            lHouseholdInfo.Text = lavaTemplate.ResolveMergeFields( mergeFields, GetAttributeValue( "LavaCommands" ) );
            pnlVerifyInfo.Visible = true;
        }

        private void BuildMedicalConsent()
        {
            var lavaTemplate = GetAttributeValue( "MedicalConsentText" );
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage );
            lConsentMessage.Text = lavaTemplate.ResolveMergeFields( mergeFields, GetAttributeValue( "LavaCommands" ) );
            pnlMedicalConsent.Visible = true;
        }

        private bool FamilyNeedsConsent()
        {

            var context = new RockContext();

            if (!KioskType.RequiresMedicalConsent)
            {
                return false;
            }

            var personEntityTypeId = EntityTypeCache.Get( typeof( Person ) ).Id;
            var avQuery = new AttributeValueService( context )
                .Queryable().AsNoTracking()
                .Where( v => v.Attribute.EntityTypeId == personEntityTypeId )
                .Where( v => v.Attribute.Key == "MedicalConsent" );

            var minAdultBirthdate = RockDateTime.Today.AddYears( -AdultAge );
            var family = CurrentCheckInState.CheckIn.CurrentFamily;
            var activeRecordStatusLUID = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() );
            var familyHasMinorsNeedingConsent = new GroupMemberService( context )
                .Queryable().AsNoTracking()
                .Where( gm => gm.GroupId == family.Group.Id )
                .Where( gm => gm.GroupMemberStatus == GroupMemberStatus.Active )
                .Where( gm => gm.IsArchived == false )
                .Where( gm => gm.Group.IsArchived == false )
                .Where( gm => gm.Person.RecordStatusValueId == activeRecordStatusLUID )
                .Where( gm => gm.Person.BirthDate > minAdultBirthdate )
                .GroupJoin( avQuery, gm => gm.Person.Id, v => v.EntityId,
                    ( p, v ) => new { PersonId = p.Id, Value = v.Select( v1 => v1.Value ).FirstOrDefault() ?? "" } )
                .Where( v => v.Value == "" )
                .ToList();

            return familyHasMinorsNeedingConsent.Any();
        }

        #endregion
    }
}
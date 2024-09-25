using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;


using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;
using org.secc.FamilyCheckin.Cache;
using org.secc.FamilyCheckin.Exceptions;
using org.secc.FamilyCheckin.Model;
using org.secc.FamilyCheckin.Utilities;
using System.Data.Entity;



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
    [LavaCommandsField( "Lava Commands",
        Description = "Lava commands that are enabled on this block.",
        IsRequired = false,
        Key = "LavaCommands",
        Order = 1 )]



    public partial class FamilyCheckinConsent : CheckInBlock
    {
        KioskTypeCache KioskType = null;

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
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

            if(!IsPostBack)
            {
                if (!FamilyNeedsConsent())
                {
                    NavigateToNextPage();
                    return;
                }
                BuildInfoVerifyPanel();

            }
        }

        private void BuildInfoVerifyPanel()
        {
            var rockContext = new RockContext();
            var familyId = CurrentCheckInState.CheckIn.CurrentFamily.Group.Id;
            var phoneSearchType = DefinedTypeCache.Get( Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_PHONE_NUMBER.AsGuid() );
            int? personId = null;

            if(CurrentCheckInState.CheckIn.SearchType.Id == phoneSearchType.Id)
            {
                personId = GetPhoneOwnerId( familyId, CurrentCheckInState.CheckIn.SearchValue );

                if(!personId.HasValue)
                {
                    NavigateToNextPage();
                    return;
                }
            }
            var activePersonStatusId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() ).Id;
            var phoneOwner = new PersonService( rockContext ).Get( personId.Value );

            var familyMembers = new GroupMemberService( rockContext )
                .Queryable().AsNoTracking()
                .Where( gm => gm.GroupId == familyId )
                .Where( gm => gm.GroupMemberStatus == GroupMemberStatus.Active )
                .Where( gm => gm.Person.RecordStatusValueId == activePersonStatusId )
                .Select( gm => gm.Person )
                .ToList();

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage );
            mergeFields.Add( "FamilyMembers", familyMembers );

            var lavaTemplate = GetAttributeValue( "VerifyInfoText" );
            lVerifyInfoHtml.Text = lavaTemplate.ResolveMergeFields( mergeFields,phoneOwner, GetAttributeValue( "LavaCommands" ) );
            pnlVerifyInfo.Visible = true;


            
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

            var minAdultBirthdate = RockDateTime.Today.AddYears( -18 );
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
                .GroupJoin(avQuery, gm => gm.Person.Id, p => p.Id,
                    (p,v) => new {PersonId = p.Id, Value = v.Select(v1 => v1.Value).FirstOrDefault() ?? ""})
                .Where(v => v.Value == "")
                .Any();

            return familyHasMinorsNeedingConsent;

        }

        private int? GetPhoneOwnerId(int familyId, string phoneNumber)
        {
            var rockContext = new RockContext();
            var personIds = CurrentCheckInState.CheckIn.CurrentFamily.Group.ActiveMembers()
                .Select( p => p.PersonId ).ToList();
            var ownerPersonId = new PhoneNumberService( rockContext )
                .Queryable().AsNoTracking()
                .Include( p => p.Person )
                .Where( n => n.Number.EndsWith( phoneNumber ) )
                .Where( n => personIds.Contains( n.PersonId ) )
                .Where( n => n.Person.AgeClassification == AgeClassification.Adult )
                .Select( n => n.PersonId )
                .FirstOrDefault();

            return ownerPersonId > 0 ? ownerPersonId : (int?)null;
        }
    }
}
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
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using org.secc.PersonMatch;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

[DisplayName( "Childrens Pre-Registration" )]
[Category( "SECC > Check-in" )]
[Description( "A tool for pre-registering families in Rock especially geared toward children's ministry." )]

[WorkflowTypeField( "Person Workflow", "The workflow to launch when the new family is added. Entity will be the first adult person created. Extra information provided by the visitor will be added into the workflow attribute 'ExtraInformation'. ", false, false )]
[CodeEditorField( "Confirmation", "Confirmation content.", CodeEditorMode.Html, defaultValue: @"<p>We're so excited to worship with you!</p>
<h2>Now What ?</h2>
<ul>
    <li>When you arrive, just head to the Children's Ministry Check-in Desk to check in your children.</li>
    <li>If you have any questions when you are trying to check in children, please see a volunteer to help you.</li>
    <li>You will receive a tag to place on each child, as well as a tag for you to use to pick up your children after the service.</li>
    <li>Then, just take your children to the room listed on their tag.</li >
    <li>When the service is over, return to the same room where you dropped off your children and present your other tag to check them out.</li>
</ul> " )]
[TextField( "Allergies Key", "The key name of the person attribute to save allergy information in.", defaultValue: "Allergy" )]
[TextField( "Medical Note Key", "The key name of the person attribute to save medical/special/other needs notes information in.", defaultValue: "MedicalNotes" )]
[TextField( "Medical Consent Key", "The key name of the person attribute to record medical consent.", defaultValue: "MedicalConsent" )]
[DefinedValueField(
        "Connection Status",
        Description = "The connection status for any new people that are created from this form (default = 'Prospect').",
        DefinedTypeGuid = "2E6540EA-63F0-40FE-BE50-F2A84735E600",
        IsRequired = true,
        AllowMultiple = false,
        DefaultValue = "b91ba046-bc1e-400c-b85d-638c1f4e0ce2" )]

public partial class Plugins_org_secc_FamilyCheckin_PreRegistration : Rock.Web.UI.RockBlock
{
    protected void Page_Load( object sender, EventArgs e )
    {
    }
    /// <summary>
    /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
    /// </summary>
    /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
    protected override void OnLoad( EventArgs e )
    {
        base.OnLoad( e );

        dpSignatureDate.SelectedDate = RockDateTime.Today;

        if ( !Page.IsPostBack )
        {
            cpCampus.Campuses = CampusCache.All();

            var gradeLabel = "";
            var today = RockDateTime.Today;
            var transitionDate = GlobalAttributesCache.Get().GetValue( "GradeTransitionDate" ).AsDateTime() ?? new DateTime( RockDateTime.Today.Year, 6, 1 );

            if ( transitionDate > today )
            {
                gradeLabel = string.Format( "Grade (For School Year {0}-{1})", today.AddYears( -1 ).Year, today.Year );
            }
            else
            {
                gradeLabel = string.Format( "Grade (For School Year {0}-{1})", today.Year, today.AddYears( 1 ).Year );
            }
            gpGrade.Label = gradeLabel;
        }
    }

    protected void cbHasAllergies_CheckedChanged( object sender, EventArgs e )
    {
        tbAllergies.Visible = cbHasAllergies.Checked;
        tbAllergies.Required = cbHasAllergies.Checked;
    }

    protected void cbHasSpecialNote_CheckedChanged( object sender, EventArgs e )
    {
        tbSpecialNote.Visible = cbHasSpecialNote.Checked;
        tbSpecialNote.Required = cbHasSpecialNote.Checked;
    }

    protected void btnRegistrationNext_Click( object sender, EventArgs e )
    {
        pnlRegistration.Visible = false;
        pnlChild.Visible = true;
    }

    protected void btnChildNext_Click( object sender, EventArgs e )
    {
        storeChild();
        pnlChild.Visible = false;
        pnlChildSummary.Visible = true;
    }
    protected void btnChildCancel_Click( object sender, EventArgs e )
    {
        pnlChild.Visible = false;
        pnlChildSummary.Visible = true;
    }

    protected void btnChildSummaryNext_Click( object sender, EventArgs e )
    {
        pnlChildSummary.Visible = false;
        pnlAdditionalInfo.Visible = true;
        pnlAskCampus.Visible = CampusCache.All().Count() > 1;
    }

    protected void btnChildAddAnother_Click( object sender, EventArgs e )
    {
        storeChild();
    }

    protected void btnChildAddAnotherFromSummary_Click( object sender, EventArgs e )
    {
        pnlChildSummary.Visible = false;
        pnlChild.Visible = true;
    }

    protected void btnAddKnownRelationship_Click( object sender, EventArgs e )
    {
        pnlAdult.Visible = true;
        pnlAdditionalInfo.Visible = false;
    }
    protected void btnAdultAddAnother_Click( object sender, EventArgs e )
    {
        if ( pnbAdultPhone.Text.IsNotNullOrWhiteSpace() || ebAdultEmail.Text.IsNotNullOrWhiteSpace() || dpAdultDateOfBirth.SelectedDate.HasValue )
            storeAdult();
    }
    protected void btnAdultNext_Click( object sender, EventArgs e )
    {
        if ( pnbAdultPhone.Text.IsNotNullOrWhiteSpace() || ebAdultEmail.Text.IsNotNullOrWhiteSpace() || dpAdultDateOfBirth.SelectedDate.HasValue )
        {
            storeAdult();
            pnlAdult.Visible = false;
            pnlAdditionalInfo.Visible = true;
        }
    }
    protected void btnAdultCancel_Click( object sender, EventArgs e )
    {
        pnlAdult.Visible = false;
        pnlAdditionalInfo.Visible = true;
    }

    protected void btnCampusBack_Click( object sender, EventArgs e )
    {
        pnlAdditionalInfo.Visible = false;
        pnlChildSummary.Visible = true;
    }

    protected void btnCampusNext_Click( object sender, EventArgs e )
    {
        pnlAdditionalInfo.Visible = false;
        showReview();
    }

    protected void btnReviewBack_Click( object sender, EventArgs e )
    {
        pnlReview.Visible = false;
        pnlAdditionalInfo.Visible = true;
    }

    protected void btnReviewFinish_Click( object sender, EventArgs e )
    {
        if ( cbMedicalConsent.Checked == false )
        {
            showReview();
            return;
        }
        processRegistration();
        pnlReview.Visible = false;
        pnlConfirmationContent.Controls.Add( new HtmlGenericControl() { InnerHtml = GetAttributeValue( "Confirmation" ) } );
        pnlConfirmation.Visible = true;
    }

    protected void btnEditChild_Click( object sender, EventArgs e )
    {
        BootstrapButton btn = ( BootstrapButton ) sender;
        int? i = btn.CommandArgument.AsIntegerOrNull();
        if ( i != null )
        {
            ViewState["CurrentChild"] = i;
            List<Child> children = ( ( List<Child> ) ViewState["Children"] );
            tbChildFirstname.Text = children[i.Value].FirstName;
            tbChildLastname.Text = children[i.Value].LastName;
            bpChildBirthday.Text = children[i.Value].DateOfBirth.ToShortDateString();
            rblGender.SelectedValue = children[i.Value].Gender;
            gpGrade.SelectedValue = children[i.Value].Grade.ToString();
            tbAllergies.Text = children[i.Value].Allergies;
            tbSpecialNote.Text = children[i.Value].MedicalNote;
        }
        pnlChildSummary.Visible = false;
        pnlChild.Visible = true;
        SaveViewState();
    }

    protected void btnEditAdult_Click( object sender, EventArgs e )
    {
        BootstrapButton btn = ( BootstrapButton ) sender;
        int? i = btn.CommandArgument.AsIntegerOrNull();
        if ( i != null )
        {
            ViewState["CurrentAdult"] = i;
            List<KnownAdult> knownAdults = ( ( List<KnownAdult> ) ViewState["KnownAdults"] );
            tbAdultFirstName.Text = knownAdults[i.Value].FirstName;
            tbAdultLastName.Text = knownAdults[i.Value].LastName;
            dpAdultDateOfBirth.Text = knownAdults[i.Value].DateOfBirth.ToShortDateString();
            ebAdultEmail.Text = knownAdults[i.Value].Email;
            pnbAdultPhone.Text = knownAdults[i.Value].MobileNumber;
        }
        pnlAdditionalInfo.Visible = false;
        pnlAdult.Visible = true;
        SaveViewState();
    }

    protected void btnDeleteChild_Click( object sender, EventArgs e )
    {
        BootstrapButton btn = ( BootstrapButton ) sender;
        int? i = btn.CommandArgument.AsIntegerOrNull();
        if ( i != null )
        {
            List<Child> children = ( ( List<Child> ) ViewState["Children"] );
            children.Remove( children[i.Value] );
            if ( !children.Any() )
            {
                btnChildCancel.Visible = false;
                pnlChildSummary.Visible = false;
                pnlChild.Visible = true;
            }

        }
        SaveViewState();
    }
    protected void btnDeleteAdult_Click( object sender, EventArgs e )
    {
        BootstrapButton btn = ( BootstrapButton ) sender;
        int? i = btn.CommandArgument.AsIntegerOrNull();
        if ( i != null )
        {
            List<KnownAdult> knownAdults = ( ( List<KnownAdult> ) ViewState["KnownAdults"] );
            knownAdults.Remove( knownAdults[i.Value] );
            if ( !knownAdults.Any() )
            {
                ViewState["KnownAdults"] = null;
            }
        }
        SaveViewState();
    }
    private void populateChildSummary()
    {

        phChildSummary.Controls.Clear();

        List<Child> children = ( ( List<Child> ) ViewState["Children"] );

        if ( children == null )
        {
            return;
        }

        HtmlGenericControl count = new HtmlGenericControl();
        count.InnerHtml = "<div class=\"col-sm-12\"><h3>" + children.Count + ( children.Count == 1 ? " Child" : " Children" ) + " Entered:</h4></div>";
        phChildSummary.Controls.Add( count );
        int i = 0;
        foreach ( Child child in children )
        {
            Panel childContainer = new Panel();
            childContainer.CssClass = "col-sm-6 col-md-4";
            phChildSummary.Controls.Add( childContainer );

            Panel cardContainer = new Panel();
            cardContainer.CssClass = "contact-card text-center";
            childContainer.Controls.Add( cardContainer );

            cardContainer.Controls.Add( new HtmlGenericControl() { InnerHtml = "<div class=\"title\">" + child.FirstName + " " + child.LastName + "</div>" } );

            Panel infoContainer = new Panel();
            infoContainer.CssClass = "clearfix info";
            cardContainer.Controls.Add( infoContainer );


            HtmlGenericControl info = new HtmlGenericControl();
            info.Controls.Add( new RockLiteral() { Label = "Gender:", Text = child.Gender } );
            info.Controls.Add( new RockLiteral() { Label = "Birthdate:", Text = child.DateOfBirth.ToShortDateString() + " (" + child.DateOfBirth.Age() + " Yrs)" } );
            info.Controls.Add( new RockLiteral() { Label = "Grade:", Text = ( child.Grade == null ? "Pre-school" : DefinedValueCache.Get( child.Grade.Value ).Description ) } );
            info.Controls.Add( new RockLiteral() { Label = "Allergies:", Text = !string.IsNullOrEmpty( child.Allergies ) ? child.Allergies : "[None]" } );
            info.Controls.Add( new RockLiteral() { Label = "Special&nbsp;Needs:", Text = !string.IsNullOrEmpty( child.MedicalNote ) ? child.MedicalNote : "[None]" } );

            infoContainer.Controls.Add( info );
            //cardContainer.Controls.Add(new HtmlGenericControl() { InnerHtml = "<hr>" });

            BootstrapButton button = new BootstrapButton();
            button.AddCssClass( "btn btn-link" );
            button.ID = "editChild_" + i;
            button.CommandArgument = i.ToString();
            button.Click += btnEditChild_Click;
            button.Text = "Edit";
            cardContainer.Controls.Add( button );

            button = new BootstrapButton();
            button.AddCssClass( "btn btn-link" );
            button.ID = "removeChild_" + i;
            button.CommandArgument = i.ToString();
            button.Click += btnDeleteChild_Click;
            button.Text = "Remove";
            cardContainer.Controls.Add( button );

            i++;
        }
    }

    private void populateKnownRelationshipSummary()
    {

        phKnownRelationshipSummary.Controls.Clear();

        List<KnownAdult> knownAdults = ( ( List<KnownAdult> ) ViewState["KnownAdults"] );

        if ( knownAdults == null )
        {
            btnAddKnownRelationship.Text = "Add Known Relationship";
            return;
        }

        if ( knownAdults.Count > 0 )
            btnAddKnownRelationship.Text = "Add Another Known Relationship";

        HtmlGenericControl count = new HtmlGenericControl();
        count.InnerHtml = "<div class=\"col-sm-12\"><h3>" + knownAdults.Count + ( knownAdults.Count == 1 ? " Known Adult" : " Known Adults" ) + " Entered:</h4></div>";
        phKnownRelationshipSummary.Controls.Add( count );
        int i = 0;
        foreach ( KnownAdult adult in knownAdults )
        {
            Panel adultContainer = new Panel();
            adultContainer.CssClass = "col-sm-6 col-md-4";
            phKnownRelationshipSummary.Controls.Add( adultContainer );

            Panel cardContainer = new Panel();
            cardContainer.CssClass = "contact-card text-center";
            adultContainer.Controls.Add( cardContainer );

            cardContainer.Controls.Add( new HtmlGenericControl() { InnerHtml = "<div class=\"title\">" + adult.FirstName + " " + adult.LastName + "</div>" } );

            Panel infoContainer = new Panel();
            infoContainer.CssClass = "clearfix info";
            cardContainer.Controls.Add( infoContainer );


            HtmlGenericControl info = new HtmlGenericControl();
            info.Controls.Add( new RockLiteral() { Label = "Birthdate:", Text = ( adult.DateOfBirth != DateTime.MinValue && adult.DateOfBirth != null ) ? adult.DateOfBirth.ToShortDateString() : "[Not Set]" } );
            info.Controls.Add( new RockLiteral() { Label = "Email:", Text = !string.IsNullOrEmpty( adult.Email ) ? adult.Email : "[Not Set]" } );
            info.Controls.Add( new RockLiteral() { Label = "Mobile:", Text = !string.IsNullOrEmpty( adult.MobileNumber ) ? adult.MobileNumber : "[Not Set]" } );

            infoContainer.Controls.Add( info );
            //cardContainer.Controls.Add(new HtmlGenericControl() { InnerHtml = "<hr>" });

            BootstrapButton button = new BootstrapButton();
            button.AddCssClass( "btn btn-link" );
            button.ID = "editAdult_" + i;
            button.CommandArgument = i.ToString();
            button.Click += btnEditAdult_Click;
            button.Text = "Edit";
            cardContainer.Controls.Add( button );

            button = new BootstrapButton();
            button.AddCssClass( "btn btn-link" );
            button.ID = "removeAdult_" + i;
            button.CommandArgument = i.ToString();
            button.Click += btnDeleteAdult_Click;
            button.Text = "Remove";
            cardContainer.Controls.Add( button );

            i++;
        }
    }

    private void processRegistration()
    {
        // Setup the Rock context
        var rockContext = new RockContext();
        var groupLocationService = new GroupLocationService( rockContext );
        var connectionStatus = DefinedValueCache.Get( GetAttributeValue( "ConnectionStatus" ).AsGuid() );
        Group family = null;
        List<Child> children = ( ( List<Child> ) ViewState["Children"] );
        List<Person> createdChildren = new List<Person>();
        PersonService personService = new PersonService( rockContext );
        var matchingPeople = personService.GetByMatch( tbFirstname.Text, tbLastName.Text, dpBirthday.SelectedDate, ebEmail.Text, pnbPhone.Text, acAddress.Street1, acAddress.PostalCode );
        bool match = matchingPeople.Count() == 1;
        if ( match && !string.IsNullOrWhiteSpace( tbFirstName2.Text ) )
        {
            var matchingPeople2 = personService.GetByMatch( tbFirstName2.Text, tbLastName2.Text, dpBirthday2.SelectedDate, ebEmail2.Text, pnbPhone2.Text, acAddress.Street1, acAddress.PostalCode );
            match = matchingPeople.Count() == 1;
        }

        // If we get exactly one match given the specificity of the search criteria this is probably a safe bet
        if ( match )
        {
            var medicalConsentKey = GetAttributeValue( "MedicalConsentKey" );
            var medicalConsent = $"{tbSignature.Text} {String.Format( "{0:MM/dd/yy}", dpSignatureDate.SelectedDate )}";
            bool updated = false;

            // See if the family member already exists
            foreach ( Child child in children )
            {
                foreach ( GroupMember gm in matchingPeople.FirstOrDefault().GetFamilyMembers() )
                {
                    if ( gm.Person.BirthDate == child.DateOfBirth && gm.Person.FirstName == child.FirstName )
                    {
                        child.MedicalConsent = medicalConsent;
                        if ( gm.Person.Gender != ( child.Gender == "Male" ? Gender.Male : Gender.Female ) )
                        {
                            var childPerson = personService.Get( gm.Person.Id );
                            childPerson.Gender = ( child.Gender == "Male" ? Gender.Male : Gender.Female );
                        }
                        child.SaveAttributes( gm.Person );
                        updated = true;
                        break;
                    }

                }
                if ( !updated )
                {
                    // If we get here, it's time to create a new family member
                    var newChild = child.SaveAsPerson( matchingPeople.FirstOrDefault().GetFamily().Id, rockContext, connectionStatus.Id );
                    newChild.Gender = child.Gender == "Male" ? Gender.Male : Gender.Female;

                    newChild.LoadAttributes();

                    if ( !string.IsNullOrWhiteSpace( medicalConsentKey ) )
                    {
                        newChild.SetAttributeValue( medicalConsentKey, medicalConsent );
                    }
                    newChild.SaveAttributeValues();

                    family = newChild.GetFamily();

                    createdChildren.Add( newChild );
                }
            }
            rockContext.SaveChanges();

            var personWorkflowGuid = GetAttributeValue( "PersonWorkflow" );
            if ( !string.IsNullOrWhiteSpace( personWorkflowGuid ) )
            {
                matchingPeople.FirstOrDefault().PrimaryAlias.LaunchWorkflow( new Guid( personWorkflowGuid ), matchingPeople.FirstOrDefault().ToString() + " Pre-Registration", new Dictionary<string, string>() { { "ExtraInformation", tbExtraInformation.Text } } );
            }
        }
        else
        {
            DefinedValueCache mobilePhone = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE );

            // Create the adult
            Person adult = new Person();
            adult.FirstName = tbFirstname.Text;
            adult.LastName = tbLastName.Text;
            if ( dpBirthday.SelectedDate != null )
            {
                adult.BirthDay = dpBirthday.SelectedDate.Value.Day;
                adult.BirthMonth = dpBirthday.SelectedDate.Value.Month;
                adult.BirthYear = dpBirthday.SelectedDate.Value.Year;
            }
            adult.RecordTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
            adult.ConnectionStatusValueId = connectionStatus.Id;
            adult.RecordStatusValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() ).Id;
            adult.UpdatePhoneNumber( mobilePhone.Id, pnbPhone.CountryCode, pnbPhone.Number, false, false, rockContext );
            adult.Email = ebEmail.Text;

            family = PersonService.SaveNewPerson( adult, rockContext, cpCampus.SelectedCampusId );

            if ( !string.IsNullOrWhiteSpace( tbFirstName2.Text ) )
            {
                Person adult2 = new Person();
                adult2.FirstName = tbFirstName2.Text;
                adult2.LastName = tbLastName2.Text;
                if ( dpBirthday2.SelectedDate != null )
                {
                    adult2.BirthDay = dpBirthday2.SelectedDate.Value.Day;
                    adult2.BirthMonth = dpBirthday2.SelectedDate.Value.Month;
                    adult2.BirthYear = dpBirthday2.SelectedDate.Value.Year;
                }
                adult2.RecordTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
                adult2.ConnectionStatusValueId = connectionStatus.Id;
                adult2.RecordStatusValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() ).Id;
                adult2.UpdatePhoneNumber( mobilePhone.Id, pnbPhone2.CountryCode, pnbPhone2.Number, false, false, rockContext );
                adult2.Email = ebEmail2.Text;

                PersonService.AddPersonToFamily( adult2, true, family.Id, 3, rockContext );
            }

            // Now create all the children
            foreach ( Child child in children )
            {
                child.MedicalConsent = $"{tbSignature.Text} {String.Format( "{0:MM/dd/yy}", dpSignatureDate.SelectedDate )}";
                var newChild = child.SaveAsPerson( family.Id, rockContext, connectionStatus.Id );
                newChild.Gender = child.Gender == "Male" ? Gender.Male : Gender.Female;
                createdChildren.Add( newChild );
            }



            //rockContext.SaveChanges();
            var personWorkflowGuid = GetAttributeValue( "PersonWorkflow" );
            if ( !string.IsNullOrWhiteSpace( personWorkflowGuid ) )
            {
                adult.PrimaryAlias.LaunchWorkflow( new Guid( GetAttributeValue( "PersonWorkflow" ) ), adult.ToString() + " Pre-Registration", new Dictionary<string, string>() { { "ExtraInformation", tbExtraInformation.Text } } );
            }
        }
        // Save the family address
        var homeLocationType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() );
        if ( homeLocationType != null )
        {

            // Find a location record for the address that was entered
            var loc = new Location();
            acAddress.GetValues( loc );
            if ( acAddress.Street1.IsNotNullOrWhiteSpace() && loc.City.IsNotNullOrWhiteSpace() )
            {
                loc = new LocationService( rockContext ).Get(
                    loc.Street1, loc.Street2, loc.City, loc.State, loc.PostalCode, loc.Country, family, true );
            }
            else
            {
                loc = null;
            }


            if ( family != null && !groupLocationService.Queryable()
                .Where( gl =>
                    gl.GroupId == family.Id &&
                    gl.GroupLocationTypeValueId == homeLocationType.Id &&
                    gl.LocationId == loc.Id )
                .Any() )
            {
                var groupType = GroupTypeCache.Get( family.GroupTypeId );
                var prevLocationType = groupType.LocationTypeValues.FirstOrDefault( l => l.Guid.Equals( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_PREVIOUS.AsGuid() ) );
                if ( prevLocationType != null )
                {
                    foreach ( var prevLoc in groupLocationService.Queryable( "Location,GroupLocationTypeValue" )
                        .Where( gl =>
                            gl.GroupId == family.Id &&
                            gl.GroupLocationTypeValueId == homeLocationType.Id ) )
                    {
                        prevLoc.GroupLocationTypeValueId = prevLocationType.Id;
                        prevLoc.IsMailingLocation = false;
                        prevLoc.IsMappedLocation = false;
                    }
                }


                string addressChangeField = homeLocationType.Value;

                var groupLocation = groupLocationService.Queryable()
                    .Where( gl =>
                        gl.GroupId == family.Id &&
                        gl.LocationId == loc.Id )
                    .FirstOrDefault();
                if ( groupLocation == null )
                {
                    groupLocation = new GroupLocation();
                    groupLocation.Location = loc;
                    groupLocation.IsMailingLocation = true;
                    groupLocation.IsMappedLocation = true;
                    groupLocation.LocationId = loc.Id;
                    groupLocation.GroupId = family.Id;
                    groupLocationService.Add( groupLocation );

                }
                groupLocation.GroupLocationTypeValueId = homeLocationType.Id;


            }

            rockContext.SaveChanges();
        }

        // Add known relationships
        List<KnownAdult> knownAdults = ( ( List<KnownAdult> ) ViewState["KnownAdults"] );
        if ( knownAdults != null )
        {
            foreach ( KnownAdult adult in knownAdults )
            {
                if ( adult.DateOfBirth == DateTime.MinValue )
                {
                    adult.DateOfBirth = null;
                }
                // do a person match to see if the person already exists
                var matchingAdults = personService.GetByMatch( adult.FirstName, adult.LastName, adult.DateOfBirth, adult.Email, adult.MobileNumber, null, null );
                if ( matchingAdults.Count() == 1 )
                {
                    // add a known check-in relationship from this person to each child in the createdChildren list
                    foreach ( Person child in createdChildren )
                    {
                        addKnownRelationship( matchingAdults.FirstOrDefault(), child );
                    }
                }
                else
                {
                    Person newPerson = new Person()
                    {
                        FirstName = adult.FirstName,
                        LastName = adult.LastName,
                        RecordTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id,
                        ConnectionStatusValueId = connectionStatus.Id,
                        RecordStatusValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() ).Id,
                        Email = adult.Email,
                        AgeClassification = AgeClassification.Adult
                    };

                    // save the person to the person service
                    personService.Add( newPerson );
                    rockContext.SaveChanges();

                    newPerson.UpdatePhoneNumber( DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() ).Id, PhoneNumber.DefaultCountryCode(), adult.MobileNumber, false, false, rockContext );

                    rockContext.SaveChanges();

                    // add a known check-in relationship from this person to each child in the createdChildren list
                    foreach ( Person child in createdChildren )
                    {
                        addKnownRelationship( newPerson, child );
                    }
                }
            }
        }

        rockContext.SaveChanges();

    }


    private void showReview()
    {
        pnlReview.Visible = true;
        rlName.Text = tbFirstname.Text + " " + tbLastName.Text;
        rlPhone.Text = pnbPhone.Text;
        rlDOB.Text = dpBirthday.SelectedDate.HasValue ? dpBirthday.SelectedDate.Value.ToShortDateString() : "";
        if ( CampusCache.All().Count() > 1 && rlCampus.Text.IsNotNullOrWhiteSpace() )
        {
            rlCampus.Text = CampusCache.Get( cpCampus.Text.AsInteger() ).Name;
        }
        else
        {
            rlCampus.Visible = false;
        }
        rlEmail.Text = ebEmail.Text;
        rlAddress.Text = acAddress.Street1 + "<br />" + acAddress.City + " " + acAddress.State + " " + acAddress.PostalCode;
        rlExtraInformation.Text = tbExtraInformation.Text;
        if ( !string.IsNullOrWhiteSpace( tbFirstName2.Text ) )
        {
            pnlParent2.Visible = true;
            rlName2.Text = tbFirstName2.Text + " " + tbLastName2.Text;
            rlPhone2.Text = pnbPhone2.Text;
            rlDOB2.Text = dpBirthday2.SelectedDate.HasValue ? dpBirthday2.SelectedDate.Value.ToShortDateString() : "";
            rlEmail2.Text = ebEmail2.Text;
        }

        List<Child> children = ( ( List<Child> ) ViewState["Children"] );
        foreach ( Child child in children )
        {
            pnlChildren.Controls.Add( new HtmlGenericControl() { InnerHtml = child.FirstName + " " + child.LastName + ", age " + child.DateOfBirth.Age() + " yrs<br />" } );
        }

        List<KnownAdult> knownAdults = ( ( List<KnownAdult> ) ViewState["KnownAdults"] );
        if ( knownAdults != null && knownAdults.Count > 0 )
        {
            pnlKnownAdults.Visible = true;

            foreach ( KnownAdult adult in knownAdults )
            {
                pnlKnownAdultsList.Controls.Add( new HtmlGenericControl() { InnerHtml = adult.FirstName + " " + adult.LastName + "<br />" } );
            }
        }
    }


    private void storeChild()
    {
        if ( ViewState["Children"] == null )
        {
            ViewState["Children"] = new List<Child>();
        }
        Child child = null;
        if ( ViewState["CurrentChild"] != null )
        {
            child = ( ( List<Child> ) ViewState["Children"] )[( int ) ViewState["CurrentChild"]];
        }
        else
        {
            child = new Child();
            ( ( List<Child> ) ViewState["Children"] ).Add( child );
        }
        ViewState["CurrentChild"] = null;

        child.FirstName = tbChildFirstname.Text;
        child.LastName = tbChildLastname.Text;
        if ( bpChildBirthday.SelectedDate.HasValue )
        {
            child.DateOfBirth = bpChildBirthday.SelectedDate.Value;
        }
        child.Gender = rblGender.Text;
        child.Grade = gpGrade.SelectedValue.AsGuidOrNull();
        child.Allergies = tbAllergies.Text;
        child.AllergiesKey = GetAttributeValue( "AllergiesKey" );
        child.MedicalNote = tbSpecialNote.Text;
        child.MedicalNoteKey = GetAttributeValue( "MedicalNoteKey" );
        child.MedicalConsentKey = GetAttributeValue( "MedicalConsentKey" );

        // Now clear the form
        tbChildFirstname.Text = "";
        tbChildLastname.Text = "";
        bpChildBirthday.SelectedDate = null;
        rblGender.ClearSelection();
        gpGrade.SelectedIndex = 0;
        tbAllergies.Text = "";
        tbSpecialNote.Text = "";
        cbHasAllergies.Checked = false;
        tbAllergies.Visible = false;
        cbHasSpecialNote.Checked = false;
        tbSpecialNote.Visible = false;

        ( ( List<Child> ) ViewState["Children"] ).Sort();
        SaveViewState();
        btnChildCancel.Visible = true;
    }

    private void storeAdult()
    {
        if ( ViewState["KnownAdults"] == null )
        {
            ViewState["KnownAdults"] = new List<KnownAdult>();
        }
        List<KnownAdult> knownAdults = ( List<KnownAdult> ) ViewState["KnownAdults"];
        KnownAdult adult = null;
        if ( ViewState["CurrentAdult"] != null )
        {
            int currentIndex;
            if ( int.TryParse( ViewState["CurrentAdult"].ToString(), out currentIndex ) && currentIndex < knownAdults.Count )
            {
                adult = knownAdults[currentIndex];
            }
        }
        if ( adult == null )
        {
            adult = new KnownAdult();
            knownAdults.Add( adult );
        }
        ViewState["CurrentChild"] = null;

        adult.FirstName = tbAdultFirstName.Text;
        adult.LastName = tbAdultLastName.Text;
        if ( dpAdultDateOfBirth.SelectedDate.HasValue )
        {
            adult.DateOfBirth = dpAdultDateOfBirth.SelectedDate.Value;
        }
        adult.Email = ebAdultEmail.Text;
        adult.MobileNumber = pnbAdultPhone.Text;

        // Now clear the form
        tbAdultFirstName.Text = "";
        tbAdultLastName.Text = "";
        dpAdultDateOfBirth.SelectedDate = null;
        ebAdultEmail.Text = "";
        pnbAdultPhone.Text = "";

        knownAdults.Sort();
        SaveViewState();
    }

    protected override void EnsureChildControls()
    {
        base.EnsureChildControls();
        populateChildSummary();
        populateKnownRelationshipSummary();
    }

    /// <summary>
    /// Inner class for storing child details in the ViewState
    /// </summary>
    [Serializable]
    protected class Child : IComparable
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public Guid? Grade { get; set; }
        public string Allergies { get; set; }
        public string AllergiesKey { get; set; }
        public string MedicalNote { get; set; }
        public string MedicalNoteKey { get; set; }
        public string MedicalConsent { get; set; }
        public string MedicalConsentKey { get; set; }

        // Default comparer for Child type.
        public int CompareTo( object obj )
        {
            // A null value means that this object is greater.
            if ( obj.GetType() != typeof( Child ) || obj == null )
            {
                return 1;
            }

            else
                return this.DateOfBirth.CompareTo( ( ( Child ) obj ).DateOfBirth );
        }

        /// <summary>
        /// Save this child as a person in a family
        /// </summary>
        /// <param name="familyId">The family to add this child to</param>
        /// <param name="rockContext">The RockContext</param>
        /// <returns></returns>
        public Person SaveAsPerson( int familyId, RockContext rockContext, int ConnectionStatusValueId )
        {
            var familyGroupType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY );
            var childRoleId = familyGroupType.Roles
                .Where( r => r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid() ) )
                .Select( r => r.Id )
                .FirstOrDefault();
            var person = new Person();
            person.FirstName = this.FirstName;
            person.LastName = this.LastName;
            person.BirthDay = this.DateOfBirth.Day;
            person.BirthMonth = this.DateOfBirth.Month;
            person.BirthYear = this.DateOfBirth.Year;
            person.RecordTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
            person.ConnectionStatusValueId = ConnectionStatusValueId;
            person.RecordStatusValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() ).Id;
            if ( this.Grade.HasValue )
            {
                person.GraduationYear = Person.GraduationYearFromGradeOffset( DefinedValueCache.Get( this.Grade.Value ).Value.AsInteger() );
            }

            PersonService.AddPersonToFamily( person, true, familyId, childRoleId, rockContext );

            SaveAttributes( person );
            return person;
        }

        public void SaveAttributes( Person person )
        {
            // These attributes should probably be block settings.
            person.LoadAttributes();
            if ( !string.IsNullOrWhiteSpace( AllergiesKey ) )
            {
                person.SetAttributeValue( AllergiesKey, Allergies );
            }
            if ( !string.IsNullOrWhiteSpace( MedicalNoteKey ) )
            {
                person.SetAttributeValue( MedicalNoteKey, MedicalNote );
            }
            if ( !string.IsNullOrWhiteSpace( MedicalConsentKey ) )
            {
                person.SetAttributeValue( MedicalConsentKey, MedicalConsent );
            }
            person.SaveAttributeValues();
        }

    }

    /// <summary>
    /// Inner class for storing known adult details in the ViewState
    /// </summary>
    [Serializable]
    protected class KnownAdult : IComparable
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Email { get; set; }
        public string MobileNumber { get; set; }

        // Default comparer for KnownAdult type.
        public int CompareTo( object obj )
        {
            // A null value means that this object is greater.
            if ( obj == null || obj.GetType() != typeof( KnownAdult ) )
            {
                return 1;
            }

            var other = ( KnownAdult ) obj;

            // Compare by LastName first
            int lastNameComparison = this.LastName.CompareTo( other.LastName );
            if ( lastNameComparison != 0 )
            {
                return lastNameComparison;
            }

            // If LastName is the same, compare by FirstName
            return this.FirstName.CompareTo( other.FirstName );
        }

    }

    protected void cvMedicalConsent_ServerValidate( object source, ServerValidateEventArgs e )
    {
        if ( pnlReview.Visible )
        {
            e.IsValid = ( cbMedicalConsent.Checked == true );
        }
        else
        {
            e.IsValid = true;
        }
    }
    protected void cvKnownAdultInfo_ServerValidate( object source, ServerValidateEventArgs e )
    {
        if ( pnlAdult.Visible )
        {
            e.IsValid = ( pnbAdultPhone.Text.IsNotNullOrWhiteSpace() || ebAdultEmail.Text.IsNotNullOrWhiteSpace() || dpAdultDateOfBirth.SelectedDate.HasValue );
        }
        else
        {
            e.IsValid = true;
        }
    }

    protected void addKnownRelationship( Person knownAdult, Person child )
    {
        var rockContext = new RockContext();
        var groupMemberService = new GroupMemberService( rockContext );
        groupMemberService.CreateKnownRelationship( knownAdult.Id, child.Id, 9 );
        rockContext.SaveChanges();
    }
}
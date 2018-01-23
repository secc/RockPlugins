using Rock.Web.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Rock;
using Rock.Web.UI.Controls;
using Rock.Model;
using Rock.Data;
using org.secc.PersonMatch;
using System.ComponentModel;
using Rock.Attribute;

[DisplayName( "Children's Pre-Registration" )]
[Category( "SECC > Check-in" )]
[Description( "A tool for pre-registering families in Rock especially geared toward children's ministry." )]

[WorkflowTypeField( "Person Workflow", "The workflow to launch when the new family is added. Entity will be the first adult person created. Extra information provided by the visitor will be added into the workflow attribute 'ExtraInformation'. ", false, false )]
[CodeEditorField( "Confirmation", "Confirmation content.", CodeEditorMode.Html, defaultValue: @"<p>We're so excited to worship with you!</p>
<h2>Now What ?</h2>
<ul>
    <li>When you arrive, just head to the Children's Ministry Check-in Desk to check-in your children.</li>
    <li>If you have any questions when you are trying to check in children, please see a volunteer to help you.</li>
    <li>You will receive a tag to place on each child, as well as a tag for you to use to pick up your children after the service.</li>
    <li>Then, just take your children to the room listed on their tag.</li >
    <li>When the service is over, return to the same room where you dropped off your children and present your other tag to check them out.</li>
</ul> " )]
[TextField( "Allergies Key", "The key name of the person attribute to save allergy information in.", defaultValue: "Allergy" )]
[TextField( "Special Note Key", "The key name of the person attribute to save special information in.", defaultValue: "LegalNotes" )]
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

        if ( !Page.IsPostBack )
        {
            cpCampus.Campuses = CampusCache.All();

            var gradeLabel = "";
            var today = RockDateTime.Today;
            var transitionDate = GlobalAttributesCache.Read().GetValue( "GradeTransitionDate" ).AsDateTime() ?? new DateTime( RockDateTime.Today.Year, 6, 1 );

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
        pnlCampus.Visible = true;
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

    protected void btnCampusBack_Click( object sender, EventArgs e )
    {
        pnlCampus.Visible = false;
        pnlChildSummary.Visible = true;
    }

    protected void btnCampusNext_Click( object sender, EventArgs e )
    {
        pnlCampus.Visible = false;
        showReview();
    }

    protected void btnReviewBack_Click( object sender, EventArgs e )
    {
        pnlReview.Visible = false;
        pnlCampus.Visible = true;
    }

    protected void btnReviewFinish_Click( object sender, EventArgs e )
    {
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
            tbSpecialNote.Text = children[i.Value].SpecialNote;
        }
        pnlChildSummary.Visible = false;
        pnlChild.Visible = true;
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
            info.Controls.Add( new RockLiteral() { Label = "Grade:", Text = ( child.Grade == null ? "Pre-school" : DefinedValueCache.Read( child.Grade.Value ).Description ) } );
            info.Controls.Add( new RockLiteral() { Label = "Allergies:", Text = !string.IsNullOrEmpty( child.Allergies ) ? child.Allergies : "[None]" } );
            info.Controls.Add( new RockLiteral() { Label = "Special&nbsp;Needs:", Text = !string.IsNullOrEmpty( child.SpecialNote ) ? child.SpecialNote : "[None]" } );

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

    private void processRegistration()
    {
        // Setup the Rock context
        var rockContext = new RockContext();

        List<Child> children = ( ( List<Child> ) ViewState["Children"] );
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
            bool updated = false;
            // See if the family member already exists
            foreach ( Child child in children )
            {
                foreach ( GroupMember gm in matchingPeople.FirstOrDefault().GetFamilyMembers() )
                {
                    if ( gm.Person.BirthDate == child.DateOfBirth && gm.Person.FirstName == child.FirstName )
                    {
                        child.SaveAttributes( gm.Person );
                        updated = true;
                        break;
                    }

                }
                if ( !updated )
                {
                    // If we get here, it's time to create a new family member
                    var newChild = child.SaveAsPerson( matchingPeople.FirstOrDefault().GetFamily().Id, rockContext );
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
            DefinedValueCache homePhone = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME );

            // Create the adult
            Person adult = new Person();
            adult.FirstName = tbFirstname.Text;
            adult.LastName = tbLastName.Text;
            adult.BirthDay = dpBirthday.SelectedDate.Value.Day;
            adult.BirthMonth = dpBirthday.SelectedDate.Value.Month;
            adult.BirthYear = dpBirthday.SelectedDate.Value.Year;
            adult.RecordTypeValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
            adult.ConnectionStatusValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_WEB_PROSPECT.AsGuid() ).Id;
            adult.RecordStatusValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() ).Id;
            adult.UpdatePhoneNumber( homePhone.Id, pnbPhone.CountryCode, pnbPhone.Number, false, false, rockContext );
            adult.Email = ebEmail.Text;

            Group family = PersonService.SaveNewPerson( adult, rockContext, cpCampus.SelectedCampusId );

            if ( !string.IsNullOrWhiteSpace( tbFirstName2.Text ) )
            {
                Person adult2 = new Person();
                adult2.FirstName = tbFirstName2.Text;
                adult2.LastName = tbLastName2.Text;
                adult2.BirthDay = dpBirthday2.SelectedDate.Value.Day;
                adult2.BirthMonth = dpBirthday2.SelectedDate.Value.Month;
                adult2.BirthYear = dpBirthday2.SelectedDate.Value.Year;
                adult2.RecordTypeValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
                adult2.ConnectionStatusValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_WEB_PROSPECT.AsGuid() ).Id;
                adult2.RecordStatusValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() ).Id;
                adult2.UpdatePhoneNumber( homePhone.Id, pnbPhone2.CountryCode, pnbPhone2.Number, false, false, rockContext );
                adult2.Email = ebEmail2.Text;

                PersonService.AddPersonToFamily( adult2, true, family.Id, 3, rockContext );
            }

            // Now create all the children
            foreach ( Child child in children )
            {
                child.SaveAsPerson( family.Id, rockContext );
            }

            rockContext.SaveChanges();
            var personWorkflowGuid = GetAttributeValue( "PersonWorkflow" );
            if ( !string.IsNullOrWhiteSpace( personWorkflowGuid ) )
            {
                adult.PrimaryAlias.LaunchWorkflow( new Guid( GetAttributeValue( "PersonWorkflow" ) ), adult.ToString() + " Pre-Registration", new Dictionary<string, string>() { { "ExtraInformation", tbExtraInformation.Text } } );
            }
        }
    }


    private void showReview()
    {
        pnlReview.Visible = true;
        rlName.Text = tbFirstname.Text + " " + tbLastName.Text;
        rlPhone.Text = pnbPhone.Text;
        rlDOB.Text = dpBirthday.SelectedDate.HasValue ? dpBirthday.SelectedDate.Value.ToShortDateString() : "";
        if ( CampusCache.All().Count() > 1 )
        {
            rlCampus.Text = CampusCache.Read( cpCampus.Text.AsInteger() ).Name;
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
        child.SpecialNote = tbSpecialNote.Text;
        child.SpecialNoteKey = GetAttributeValue( "SpecialNoteKey" );

        // Now clear the form
        tbChildFirstname.Text = "";
        tbChildLastname.Text = "";
        bpChildBirthday.SelectedDate = null;
        rblGender.ClearSelection();
        gpGrade.SelectedIndex = 0;
        tbAllergies.Text = "";
        tbSpecialNote.Text = "";

        ( ( List<Child> ) ViewState["Children"] ).Sort();
        SaveViewState();
        btnChildCancel.Visible = true;
    }

    protected override void EnsureChildControls()
    {
        base.EnsureChildControls();
        populateChildSummary();
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
        public string SpecialNote { get; set; }
        public string SpecialNoteKey { get; set; }

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
        public Person SaveAsPerson( int familyId, RockContext rockContext )
        {

            var familyGroupType = GroupTypeCache.Read( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY );
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
            person.RecordTypeValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
            person.ConnectionStatusValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_WEB_PROSPECT.AsGuid() ).Id;
            person.RecordStatusValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() ).Id;
            if ( this.Grade.HasValue )
            {
                person.GraduationYear = Person.GraduationYearFromGradeOffset( DefinedValueCache.Read( this.Grade.Value ).Value.AsInteger() );
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
            if ( !string.IsNullOrWhiteSpace( SpecialNoteKey ) )
            {
                person.SetAttributeValue( SpecialNoteKey, SpecialNote );
            }
            person.SaveAttributeValues();
        }

    }
}
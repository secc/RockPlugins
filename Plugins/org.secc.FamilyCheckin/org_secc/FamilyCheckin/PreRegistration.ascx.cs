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

public partial class Plugins_org_secc_FamilyCheckin_PreRegistration : Rock.Web.UI.RockBlock
{
    protected void Page_Load(object sender, EventArgs e)
    {
    }
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (!Page.IsPostBack)
        {
            cpCampus.Campuses = CampusCache.All();
        }
    }

    protected void btnRegistrationNext_Click(object sender, EventArgs e)
    {
        pnlRegistration.Visible = false;
        pnlChild.Visible = true;
    }

    protected void btnChildNext_Click(object sender, EventArgs e)
    {
        storeChild();
        pnlChild.Visible = false;
        pnlChildSummary.Visible = true;
    }

    protected void btnChildSummaryNext_Click(object sender, EventArgs e)
    {
        pnlChildSummary.Visible = false;
        pnlCampus.Visible = true;
    }

    protected void btnChildAddAnother_Click(object sender, EventArgs e)
    {
        storeChild();
    }

    protected void btnChildAddAnotherFromSummary_Click(object sender, EventArgs e)
    {
        pnlChildSummary.Visible = false;
        pnlChild.Visible = true;
    }
    protected void btnCampusNext_Click(object sender, EventArgs e)
    {
        pnlCampus.Visible = false;
        showReview();
    }

    protected void btnReviewFinish_Click(object sender, EventArgs e)
    {
        processRegistration();
        pnlReview.Visible = false;
        pnlConfirmation.Visible = true;
    }

    protected void btnEditChild_Click(object sender, EventArgs e)
    {
        BootstrapButton btn = (BootstrapButton)sender;
        int? i = btn.CommandArgument.AsIntegerOrNull();
        if (i != null)
        {
            ViewState["CurrentChild"] = i;
            List<Child> children = ((List<Child>)ViewState["Children"]);
            tbChildFirstname.Text = children[i.Value].FirstName;
            tbChildLastname.Text = children[i.Value].LastName;
            bpChildBirthday.Text = children[i.Value].DateOfBirth.ToShortDateString();
            rblGender.SelectedValue = children[i.Value].Gender;
            gpGrade.SelectedValue = children[i.Value].Grade.ToString();
            tbAllergies.Text = children[i.Value].Allergies;
            tbSpecialNeeds.Text = children[i.Value].SpecialNeeds;
        }
        pnlChildSummary.Visible = false;
        pnlChild.Visible = true;
        SaveViewState();
    }
    protected void btnDeleteChild_Click(object sender, EventArgs e)
    {
        BootstrapButton btn = (BootstrapButton)sender;
        int? i = btn.CommandArgument.AsIntegerOrNull();
        if (i != null)
        {
            List<Child> children = ((List<Child>)ViewState["Children"]);
            children.Remove(children[i.Value]);
        }
        SaveViewState();
    }
    private void populateChildSummary()
    {

        phChildSummary.Controls.Clear();

        List<Child> children = ((List<Child>)ViewState["Children"]);

        if (children == null)
        {
            return;
        }

        HtmlGenericControl count = new HtmlGenericControl();
        count.InnerHtml = "<div class=\"col-sm-12\"><h3>" + children.Count + (children.Count == 1?" Child":" Children") + " Entered:</h4></div>";
        phChildSummary.Controls.Add(count);
        int i = 0;
        foreach (Child child in children)
        {
            Panel childContainer = new Panel();
            childContainer.CssClass = "col-sm-6 col-md-4";
            phChildSummary.Controls.Add(childContainer);

            Panel cardContainer = new Panel();
            cardContainer.CssClass = "contact-card text-center";
            childContainer.Controls.Add(cardContainer);

            cardContainer.Controls.Add(new HtmlGenericControl() { InnerHtml = "<div class=\"title\">" + child.FirstName + " " + child.LastName + "</div>" });

            Panel infoContainer = new Panel();
            infoContainer.CssClass = "clearfix info";
            cardContainer.Controls.Add(infoContainer);


            HtmlGenericControl info = new HtmlGenericControl();
            info.Controls.Add(new RockLiteral() { Label = "Gender:", Text = child.Gender });
            info.Controls.Add(new RockLiteral() { Label = "Birthdate:", Text = child.DateOfBirth.ToShortDateString() + " (" + child.DateOfBirth.Age() + " Yrs)" });
            info.Controls.Add(new RockLiteral() { Label = "Grade:", Text = (child.Grade == null ? "Pre-school" : DefinedValueCache.Read(child.Grade.Value).Description) });
            info.Controls.Add(new RockLiteral() { Label = "Allergies:", Text = !string.IsNullOrEmpty(child.Allergies)?child.Allergies:"[None]" });
            info.Controls.Add(new RockLiteral() { Label = "Special&nbsp;Needs:", Text = !string.IsNullOrEmpty(child.SpecialNeeds) ? child.SpecialNeeds : "[None]" });

            infoContainer.Controls.Add(info);
            //cardContainer.Controls.Add(new HtmlGenericControl() { InnerHtml = "<hr>" });

            BootstrapButton button = new BootstrapButton();
            button.AddCssClass("btn btn-link");
            button.ID = "editChild_" + i;
            button.CommandArgument = i.ToString();
            button.Click += btnEditChild_Click;
            button.Text = "Edit";
            cardContainer.Controls.Add(button);

            button = new BootstrapButton();
            button.AddCssClass("btn btn-link");
            button.ID = "removeChild_" + i;
            button.CommandArgument = i.ToString();
            button.Click += btnDeleteChild_Click;
            button.Text = "Remove";
            cardContainer.Controls.Add(button);
            
            i++;
        }
    }

    private void processRegistration()
    {
        // Setup a few things
        var rockContext = new RockContext();
        var familyGroupType = GroupTypeCache.Read(Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY);

        var adultRoleId = familyGroupType.Roles
            .Where(r => r.Guid.Equals(Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid()))
            .Select(r => r.Id)
            .FirstOrDefault();

        List<Child> children = ((List<Child>)ViewState["Children"]);
        PersonService personService = new PersonService(rockContext);
        var matchingPeople = personService.GetByMatch(tbFirstname.Text, tbLastName.Text, dpBirthday.SelectedDate, ebEmail.Text, pnbPhone.Text, adAddress.Street1, adAddress.PostalCode);
        // If we get exactly one match given the specificity of the search criteria this is probably a safe bet
        if (matchingPeople.Count() == 1)
        {
            bool updated = false;
            // See if the family member already exists
            foreach (Child child in children)
            {
                foreach (GroupMember gm in matchingPeople.FirstOrDefault().GetFamilyMembers())
                {
                    if (gm.Person.BirthDate == child.DateOfBirth && gm.Person.FirstName == child.FirstName)
                    {
                        child.SaveAttributes(gm.Person);
                        updated = true;
                        break;
                    }

                }
                if (!updated)
                {
                    // If we get here, it's time to create a new family member
                    child.SaveAsPerson(matchingPeople.FirstOrDefault().GetFamily().Id, rockContext);
                }
            }
        }
        else
        {
            // Create the adult
            Person adult = new Person();
            // TODO: populate this adult
            Group family = PersonService.SaveNewPerson(adult, rockContext, cpCampus.SelectedCampusId);

            // Now create all the children
            foreach (Child child in children)
            {
                child.SaveAsPerson(family.Id, rockContext);
            }
        }
    }


    private void showReview()
    {
        pnlReview.Visible = true;
        rlName.Text = tbFirstname.Text + " " + tbLastName.Text;
        rlPhone.Text = pnbPhone.Text;
        rlDOB.Text = dpBirthday.SelectedDate.HasValue ? dpBirthday.SelectedDate.Value.ToShortDateString() : "";
        rlCampus.Text = CampusCache.Read(cpCampus.Text.AsInteger()).Name;
        rlEmail.Text = ebEmail.Text;
        rlAddress.Text = adAddress.Street1 + "<br />" + adAddress.City + " " + adAddress.State + " " + adAddress.PostalCode;

        List<Child> children = ((List<Child>)ViewState["Children"]);
        foreach (Child child in children)
        {
            pnlChildren.Controls.Add(new HtmlGenericControl() { InnerHtml = child.FirstName + " " + child.LastName + ", age " + child.DateOfBirth.Age() + " yrs<br />" });
        }
    }

    private void storeChild()
    {
        if (ViewState["Children"] == null)
        {
            ViewState["Children"] = new List<Child>();
        }
        Child child = null;
        if (ViewState["CurrentChild"] != null)
        {
            child = ((List<Child>)ViewState["Children"])[(int)ViewState["CurrentChild"]];
        } else
        {
            child = new Child();
            ((List<Child>)ViewState["Children"]).Add(child);
        }
        ViewState["CurrentChild"] = null;

        child.FirstName = tbChildFirstname.Text;
        child.LastName = tbChildLastname.Text;
        if (bpChildBirthday.SelectedDate.HasValue)
        {
            child.DateOfBirth = bpChildBirthday.SelectedDate.Value;
        }
        child.Gender = rblGender.Text;
        child.Grade = gpGrade.SelectedValueAsInt();
        child.Allergies = tbAllergies.Text;
        child.SpecialNeeds = tbSpecialNeeds.Text;

        // Now clear the form
        tbChildFirstname.Text = "";
        tbChildLastname.Text = "";
        bpChildBirthday.SelectedDate = null;
        rblGender.ClearSelection();
        gpGrade.SelectedIndex = 0;
        tbAllergies.Text = "";
        tbSpecialNeeds.Text = "";

        ((List<Child>)ViewState["Children"]).Sort();

        SaveViewState();
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
        public int? Grade { get; set; }
        public string Allergies { get; set; }
        public string SpecialNeeds { get; set; }


        // Default comparer for Child type.
        public int CompareTo(object obj)
        {
            // A null value means that this object is greater.
            if (obj.GetType() != typeof(Child) || obj == null)
            {
                return 1;
            }

            else
                return this.DateOfBirth.CompareTo(((Child)obj).DateOfBirth);
        }
        
        /// <summary>
        /// Save this child as a person in a family
        /// </summary>
        /// <param name="familyId">The family to add this child to</param>
        /// <param name="rockContext">The RockContext</param>
        /// <returns></returns>
        public Person SaveAsPerson(int familyId, RockContext rockContext)
        {

            var familyGroupType = GroupTypeCache.Read(Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY);
            var childRoleId = familyGroupType.Roles
                .Where(r => r.Guid.Equals(Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid()))
                .Select(r => r.Id)
                .FirstOrDefault();
            var person = new Person();
            person.FirstName = this.FirstName;
            person.LastName = this.LastName;
            person.BirthDay = this.DateOfBirth.Day;
            person.BirthMonth = this.DateOfBirth.Month;
            person.BirthYear = this.DateOfBirth.Year;
            person.RecordTypeValueId = DefinedValueCache.Read(Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid()).Id;
            person.ConnectionStatusValueId = DefinedValueCache.Read(Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_WEB_PROSPECT.AsGuid()).Id;
            person.RecordStatusValueId = DefinedValueCache.Read(Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid()).Id;
            person.GraduationYear = Person.GraduationYearFromGradeOffset(this.Grade);

            PersonService.AddPersonToFamily(person, true, familyId, childRoleId, rockContext);

            SaveAttributes(person);
            return person;
        }

        public void SaveAttributes(Person person)
        {
            // These attributes should probably be block settings.
            person.LoadAttributes();
            person.AttributeValues["Allergy"].Value = this.Allergies;
            if (!string.IsNullOrWhiteSpace(this.SpecialNeeds))
            {
                person.AttributeValues["HasSpecialNeeds"].Value = "Y";
                person.AttributeValues["SpecialNote"].Value = this.SpecialNeeds;
            }
            person.SaveAttributeValues();
        }

    }


}
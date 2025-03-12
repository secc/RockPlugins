import {Page} from "@playwright/test";
import {login} from "./support/login";
import goto from "../support/goto";


async function wait(page:Page)
{
    await page.waitForTimeout(500);
}

/**
 * A very simple test for demo purposes
 */
export async function baseline(page: Page){

  await page.goto('');

    await login(page);

}



export async function SRU13_CreateADataViewAndReport(page: Page)
{

    await goto(page,'');

    await login(page);


    await goto(page, 'reporting/dataviews');

    // Check-in / Southwest / Mid-Week Volunteers
    await goto(page, 'reporting/dataviews?DataViewId=278&ExpandedIds=C2259%2CC307%2CC442')

    // Check-in / Southwest / SW Nursery - People
    await goto(page, 'reporting/dataviews?DataViewId=63&ExpandedIds=C2259%2CC307%2CC442');

    // Foundational Views / FDV | Children in Elementary
    await goto(page, 'reporting/dataviews?DataViewId=1585&ExpandedIds=C2259%2CC131');

    // Foundational Views / FDV | Serving Groups | ACTIVE
    await goto(page, 'reporting/dataviews?DataViewId=2760&ExpandedIds=C131');

    // Check-in / Blankenbaker / Preschool Volunteers
    await goto(page, 'reporting/dataviews?DataViewId=445&ExpandedIds=C307%2CC439');

    // Check-in / Blankenbaker / Elementary 3-5 Small Group - People
    await goto(page, 'reporting/dataviews?DataViewId=79&ExpandedIds=C307%2CC439');


    await page.waitForLoadState();
}

export async function SRU15_CheckingAPersonProfile(page: Page)
{

    await goto(page, '');

    await login(page);


    // Check Josh M Sewell profile
    await goto(page, 'person/124835');

    // Go to Josh Attributes
    await goto(page, 'person/124835/extendedAttributes');

    // Go to Josh Steps
    await goto(page, 'person/124835/steps');

     // Go to Josh History / Person History
     await goto(page, 'person/124835/History/PersonHistory');

     // Go to Josh History / Attendance
     await goto(page, 'person/124835/History/Attendance');


     // Go to Josh History / Attendance
     await goto(page, 'page/3224?PersonId=124835');

     // Go to Josh History / Communication
     await goto(page, 'page/178?PersonId=124835');


    await page.waitForLoadState();
}

export async function SRU16_ViewingEventRegistration(page: Page)
{

    await goto(page, '');

    await login(page);

    // Go to Event Registrations
    await goto(page, 'web/event-registrations');

    // Chose 2023 | Flight Your Way to A Better Marriage because it has many people (210)
    await goto(page, 'RegistrationInstance/4733');

    // Look at the Registrants
    await goto(page, 'web/event-registrations/4733/registrants');

     // Check the payments
     await goto(page, 'web/event-registrations/4733/payments');

     // See one particular participant payment details
     await goto(page, 'finance/transactions/3668131');


     // Go to Discounts
     await goto(page, 'web/event-registrations/4733/discounts');

    // Go to Linkages
    await goto(page, 'web/event-registrations/4733/linkages');

    await page.waitForLoadState();
}

export async function SRU17_VisitExternalPageAccountAndBrowse(page: Page)
{

    await goto(page, '');

    await login(page);

    // Home Page
    await goto(page, 'page/1665');

    // Go to My Account
    await goto(page, 'MyAccount');

    try {
        await page.click('text=Accept Cookies');
        await page.waitForTimeout(500);
      } catch (e) {

      }


     // Go to My Account / Classes
     await goto(page, 'my-classes');

     // Go to Giving
     await goto(page, 'MyGiving');


     // Go to My Events
     await goto(page, 'MyEvents');

    // Go My Schedule
    await goto(page, 'MySchedule');

    // Go to Locations
    await goto(page, 'locations');


    /*
    At this point in the card attached video, we are navigating in
    southeastchristian.org page no in the Azure site. So I am skipping the
    next steps.
    */
    await page.waitForLoadState();
}

export async function SRU18_Groups(page: Page)
{

    await goto(page, '');

    await login(page);

    // Home Page
    await goto(page, 'groups');

    // Go to On Campus Groups
    await goto(page, 'groups/oncampus/');


     // Go to campus group: Chappel in the woods
     await goto(page, 'groups/oncampus/chapelinthewoods/all?');

     // Go to campus group: Chappel in the woords -> Men's Bible Study
     await goto(page, 'groups/oncampus/details/menscoffeefellowship');

     // Go to campus group: Chappel in the woords -> Classic Workship
     await goto(page, 'groups/oncampus/details/groups/oncampus/details/senioradultclassicworship');



      // Go to Well Home Group registration
    await goto(page, 'groups/homegroups/registration/381305');


    /*
    At this point in the card attached video, we are navigating in
    southeastchristian.org page no in the Azure site. So I am skipping the
    next steps.
    */
    await page.waitForLoadState();
}

export async function SRU19_EventExternalPage(page: Page)
{

    await goto(page, '');

    await login(page);

    // Go to Passion Conference event (external event)
    await goto(page, 'events/details?EventItemId=806&campusId=8');


     // Click BlankenBaker
     await goto(page, 'registration/BBPassionConf25');

     // Click Next
     await page.click('text=Next');


    await page.waitForLoadState();
}




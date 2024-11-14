import {Page} from "@playwright/test";
import {login} from "./support/login";

/**
 * A very simple test for demo purposes
 */
export async function baseline(page: Page){

  await page.goto('');
    const user = 'julio.cachay@datably.io';
    const password = 'Chattanooga25!';

    await login(page, user, password);

    await page.goto('1/events');

}

/**
 * Logs in, and navigates to several CMS routes
 * @param page
 * @constructor
 */
export async function loginAndNavigateRoutes(page: Page){


    const user = 'julio.cachay@datably.io';
    const password = 'Chattanooga25!';

    await page.goto('');

    await login(page, user, password);

    // keep me logged in
    await page.getByText(/Keep/).check();

    await page.waitForTimeout(1000);

    await page.goto('person/172382');

    await page.waitForTimeout(2000);
      
    await page.goto('reporting/dataviews?DataViewId=4164&ExpandedIds=C307%2CC439%2CC413%2CC2107%2CC1124');

   // await page.waitForTimeout(1000);

  //await page.goto(getUri('1/events'));

  //  await page.waitForTimeout(1000);

 // await page.click('text=ACCEPT COOKIES');

   // await page.waitForTimeout(1000);

 //   await page.reload();

    //await page.waitForTimeout(1000);

 //  await page.goto(getUri('1/upcomingEvents'));

   //await page.waitForTimeout(1000);

 //  await page.goto(getUri('about/baptism'));

 // await page.goto(getUri('person/172382'));

  // await login(page, user, password);

 
}

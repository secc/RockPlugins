import {Page} from "@playwright/test";
import {getUri} from "./support/getUri";
import {login} from "./support/login";

/**
 * A very simple test for demo purposes
 */
export async function baseline(page: Page){

    const user = 'julio.cachay@datably.io';
    const password = 'Chattanooga25!';

    await login(page, user, password);

    await page.goto(getUri('1/events'));

}

/**
 * Logs in, and navigates to several CMS routes
 * @param page
 * @constructor
 */
export async function loginAndNavigateRoutes(page: Page){

    const user = 'julio.cachay@datably.io';
    const password = 'Chattanooga25!';

    await login(page, user, password);

    await page.waitForTimeout(1000);

    await page.goto(getUri('1/events'));

    await page.waitForTimeout(1000);

    await page.click('text=ACCEPT COOKIES');

    await page.waitForTimeout(1000);

    await page.reload();

    await page.waitForTimeout(1000);

   await page.goto(getUri('1/upcomingEvents'));

   await page.waitForTimeout(1000);

   await page.goto(getUri('about/baptism'));

   await page.waitForTimeout(2000);
}

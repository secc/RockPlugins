import {Page} from "@playwright/test";
import {login} from "./support/login";

export async function member1(page: Page)
{
    await login(page);

    await page.waitForTimeout(1000);

    await page.goto('MyAccount');




}
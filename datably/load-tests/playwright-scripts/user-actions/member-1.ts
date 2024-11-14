import {Page} from "@playwright/test";
import {login} from "./support/login";

export async function member1(page: Page)
{
    const user = 'dota.bytes@datably.io';
    const password = 'Woof123@4';

    await login(page, user, password);

    await page.waitForTimeout(1000);

    await page.goto('MyAccount');




}
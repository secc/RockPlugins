import {Page} from "@playwright/test";
import {getUri} from "./getUri";

export async function login(page: Page, user:string, password:string)
{
    await page.goto(getUri(''));

    const userInput = await page.getByLabel('Username').fill(user);

    const passwordInput = await page.getByLabel('Password').fill(password);
}
import {Page} from "@playwright/test";
import {getUri} from "./getUri";

export async function login(page: Page, user:string, password:string)
{


    const userInput = await page.getByLabel('Username').fill(user);

    const passwordInput = await page.getByLabel('Password').fill(password);

    const loginButton = await page.locator('input:has-text("Login")');
  await loginButton.click();
}
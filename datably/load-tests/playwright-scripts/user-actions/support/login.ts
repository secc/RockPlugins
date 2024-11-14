import {Page} from "@playwright/test";

export async function login(page: Page, user:string, password:string)
{
    await page.getByLabel('Username').fill(user);
    await page.getByLabel('Password').fill(password);

    const loginButton = page.locator('input:has-text("Login")');
    await loginButton.click();
}
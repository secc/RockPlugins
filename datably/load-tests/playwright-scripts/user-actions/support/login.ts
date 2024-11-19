import {Page} from "@playwright/test";

export async function login(page: Page)
{
    page.setDefaultTimeout(120000);
    const user = process.env.USERNAME;
    const password = process.env.PASSWORD;

    await page.getByLabel('Username').fill(user);
    await page.getByLabel('Password').fill(password);

    const loginButton = page.locator('input:has-text("Login")');
    await loginButton.click();

    await page.waitForTimeout(2000);
}
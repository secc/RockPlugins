import {Page} from "@playwright/test";

export default async function goto(page: Page, path: string)
{
    const baseUrl = 'https://datablyrockdev.secc.org/';
    const url = new URL(path, baseUrl).toString();

    await page.goto(url);
}
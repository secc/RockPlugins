import {Page} from "@playwright/test";

export default async function openCheckInPage(page: Page, kioskType: string) {
    await page.goto(`${process.env.BASE_URL}/familycheckin`);
    if (page.url().includes('/page/2814')) {
        await login(page);
        await assertKioskTypeAndCreate(page, kioskType);
    }
}

async function login(page: Page) {
    await page.getByLabel('Username').fill(process.env.USERNAME);
    await page.getByLabel('Password').fill(process.env.PASSWORD);

    await page.getByRole('button', { name: 'Login' }).click();
}

async function assertKioskTypeAndCreate(page: Page, kioskType: string) {
    await page.getByLabel('Kiosk Name').fill('Load Test Kiosk for ' + kioskType);

    const select = page.getByLabel('Kiosk Type');

    const optionLabels = await select.locator('option').allTextContents();
    const trimmedLabels = optionLabels.map(l => l.trim());

    if (!trimmedLabels.includes(kioskType)) {
        const close = trimmedLabels.filter(l => l.includes(kioskType) || kioskType.includes(l));
        throw new Error(
            `Kiosk Type "${kioskType}" not found in dropdown. ` +
            `Close matches: ${JSON.stringify(close)}. ` +
            `All options (${trimmedLabels.length}): ${JSON.stringify(trimmedLabels)}`
        );
    }

    await select.selectOption({ label: kioskType });

    await page.getByRole('link', { name: 'Start' }).click();
}
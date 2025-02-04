import {expect, Locator, Page} from "@playwright/test";

export async function openCheckInPage(page: Page, kioskType: string) {
    await page.goto(`${process.env.BASE_URL}/familycheckin`);
    if (page.url().includes('/page/2814')) {
        await login(page);
        await createKiosk(page, kioskType);
    }
}

export async function login(page: Page) {
    await page.getByLabel('Username').fill(process.env.USERNAME);
    await page.getByLabel('Password').fill(process.env.PASSWORD);
    
    await page.getByRole('button', { name: 'Login' }).click();
}

export async function createKiosk(page: Page, kioskType: string) {
    await page.getByLabel('Kiosk Name').fill('Load Test Kiosk for ' + kioskType);

    const kioskTypes = await page.getByLabel('Kiosk Type').innerText();
    expect(kioskTypes).toContain(kioskType);
    
    await page.getByLabel('Kiosk Type').selectOption({ label: kioskType });

    await page.getByRole('link', { name: 'Start' }).click();
}

export async function checkInAllKids(page: Page, phoneNumber: string, schedule: string) {
    // TODO
    await page.waitForTimeout(1000);
    await page.waitForTimeout(1000);
    
    await page.keyboard.type(phoneNumber);
    await page.getByRole('link', { name: 'Search' }).click();
    
    // TODO
    await page.waitForTimeout(1000);

    const familySelector = page.getByText('Select your family to');
    if (await familySelector.isVisible()) {
        // Fail the test if there are multiple families with the same phone number.
        // We don't have the data for the family names, so we can't differentiate between them.
        expect('Number of Families with number ' + phoneNumber).toBe(1);
    }
    else {
        const loading = page.getByText('Loading your family');
        await expect(loading).toBeVisible({ visible: false, timeout: 600_000 });
    }
    
    if (await page.getByRole('heading', { name: 'Please Select One Or More' }).isVisible()) {
        await page.getByRole('link', { name: schedule }).click();
        // TODO: handle the possibility of multiple schedules
        // Can we differentiate between a selected schedule and non-selected schedule?
        // Are all of the links available to us on this page related only to the schedules?

        await page.getByRole('link', { name: 'Next' }).click();
    }

    // TODO
    await page.waitForTimeout(1000);

    const links = await page.getByRole('link').all();
    const goodLinks = [];
    
    for (const link of links) {
        const text = await link.textContent();
        if (validateLinkText(text)) {
            goodLinks.push(link);
        }
    }

    for (const goodLink of goodLinks) {
        await goodLink.click();
        // TODO
        await page.waitForLoadState('load');
    }

    const checkInButton = page.getByRole('link', { name: 'Check-In', exact: true });
    await checkInButton.waitFor({ state: 'visible' });
    
    // TODO reinitiate actual check-in when ready
    // await checkInButton.click();
    // await expect(page.getByText('Welcome.')).toBeVisible();

    function validateLinkText(text: string) {
        return !(text.includes('Select Room To Checkin')
            || text === 'Check-in'
            || text === schedule
            || text === 'Next'
            || text === '');
    }
}

async function logLocator(locator: Locator) {
    const text = await locator.textContent();
    console.log('locator', locator);
    console.log('text', text);
}
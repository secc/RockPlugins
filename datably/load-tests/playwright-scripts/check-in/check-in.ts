import {expect, Locator, Page} from "@playwright/test";

export async function openCheckInPage(page: Page, kioskType: string) {
    await page.goto(`${process.env.BASE_URL}/familycheckin`);
    if (page.url().includes('/page/2814')) {
        await login(page);
        await assertKioskTypeAndCreate(page, kioskType);
    }
}

export async function login(page: Page) {
    await page.getByLabel('Username').fill(process.env.USERNAME);
    await page.getByLabel('Password').fill(process.env.PASSWORD);
    
    await page.getByRole('button', { name: 'Login' }).click();
}

export async function assertKioskTypeAndCreate(page: Page, kioskType: string) {
    const kioskTypes = await page.getByLabel('Kiosk Type').innerText();
    expect(kioskTypes).toContain(kioskType);
    
    await page.getByLabel('Kiosk Name').fill('Load Test Kiosk for ' + kioskType);
    await page.getByLabel('Kiosk Type').selectOption({ label: kioskType });

    await page.getByRole('link', { name: 'Start' }).click();
}

export async function checkInFamily(page: Page, phoneNumber: string, schedule: string) {
    // TODO
    await page.waitForTimeout(1000);
    await page.waitForTimeout(1000);
    
    await page.keyboard.type(phoneNumber);
    await page.getByRole('link', { name: 'Search' }).click();
    
    // TODO
    await page.waitForTimeout(1000);

    await assertOneFamilyWithPhoneNumber(page, phoneNumber);

    const loading = page.getByText('Loading your family');
    await expect(loading).toBeVisible({ visible: false, timeout: 600_000 });
    
    const scheduleTexts = await checkForMultipleSchedules(page, schedule);

    // TODO
    await page.waitForTimeout(1000);
    
    await checkInFamilyMembers(page, scheduleTexts);
    
    const checkInButton = page.getByRole('link', { name: 'Check-In', exact: true });
    await checkInButton.waitFor({ state: 'visible' });
    
    // TODO reinitiate actual check-in when ready
    // await checkInButton.click();
    // await expect(page.getByText('Welcome.')).toBeVisible();
}

// Fail the test if there are multiple families with the same phone number.
// We don't have the data for the family names, so we can't differentiate between them.
async function assertOneFamilyWithPhoneNumber(page: Page, phoneNumber: string) {
    const familySelector = page.getByText('Select your family to');
    if (await familySelector.isVisible()) {
        expect('Number of Families with number ' + phoneNumber).toBe(1);
    }
}

async function checkForMultipleSchedules(page: Page, schedule: string): Promise<string[]> {
    const headingIsVisible = await page.getByRole('heading', { name: 'Please Select One Or More' }).isVisible();
    if (!headingIsVisible) {
        return [];
    }
    
    // Remove the last 4 as they are not schedule links.
    const scheduleLinks = (await page.getByRole('link').all()).slice(0, -4);
    
    const scheduleTexts: string[] = [];
    for (const scheduleLink of scheduleLinks) {
        const text = await scheduleLink.textContent();
        const color = await scheduleLink.evaluate((e) => {
            return window.getComputedStyle(e).getPropertyValue("color")
        });
        scheduleTexts.push(text);

        // If the color is white, the schedule is selected.
        if (text === schedule && color !== 'rgb(255, 255, 255)') {
            await scheduleLink.click();
        }
        else if (text !== schedule && color === 'rgb(255, 255, 255)') {
            await scheduleLink.click();
        }
        
        // TODO
        await page.waitForTimeout(500);
    }
    
    await page.getByRole('link', { name: 'Next' }).click();
    
    return scheduleTexts;
}

async function checkInFamilyMembers(page: Page, scheduleTexts: string[]) {
    const links = await page.getByRole('link').all();
    const goodLinks = [];

    for (const link of links) {
        const text = await link.textContent();
        if (validateLinkText(text, scheduleTexts)) {
            goodLinks.push(link);
        }
    }

    for (const goodLink of goodLinks) {
        await goodLink.click();
        // TODO
        await page.waitForLoadState('load');
    }
}

function validateLinkText(text: string, invalidTexts: string[]) {
    return !(text.includes('Select Room To Checkin')
        || text === 'Check-in'
        || text === 'Next'
        || text === ''
        || invalidTexts.includes(text));
}

import {expect, Locator, Page} from "@playwright/test";

const white = 'rgb(255, 255, 255)';

export async function openCheckInPage(page: Page, kioskType: string) {
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
    const kioskTypes = await page.getByLabel('Kiosk Type').innerText();
    if (!kioskTypes.includes(kioskType)) {
        console.error(`Kiosk type \"${kioskType}\" not found.`);
        return;
    }
    
    await page.getByLabel('Kiosk Name').fill('Load Test Kiosk for ' + kioskType);
    await page.getByLabel('Kiosk Type').selectOption({ label: kioskType });

    await page.getByRole('link', { name: 'Start' }).click();
}

export async function checkInFamily(page: Page, phoneNumber: string, schedule: string) {
    await expect(page).toHaveURL(/\/page\/438/);
    
    const searchButton = page.getByRole('link', { name: 'Search' });
    await expect(searchButton).toBeVisible();

    await page.keyboard.type(phoneNumber);
    await searchButton.click();

    const familySelector = page.getByText('Select your family to');
    while (page.url().includes('/page/438')) {
        // Exit check-in for this family if there are other families with the same phone number.
        // We don't have the data for the family names, so we can't differentiate between them.
        if (await familySelector.isVisible()) {
            console.error(`\tMultiple families with phone number \"${phoneNumber}\" found.`);
            return;
        }
    }
    
    await expect(page).toHaveURL(/\/page\/439/);
    
    // Exit check-in for this family if we hit these error pages.
    const errorHeadings = [
        'Please see a SE!KIDS Representative.',
        'There are no members of your family who are able to check-in at this kiosk right now.'
    ]
    for (const error of errorHeadings) {
        if (await page.getByRole('heading', { name: error }).isVisible()) {
            console.error(`\tFamily with number ${phoneNumber} received \"${error}\"`);
            
            await page.getByRole('link', { name: 'OK' }).click();
            await expect(page).toHaveURL(/\/page\/438/);
            return;
        }
    }

    const scheduleTexts = await selectSchedule(page, schedule);

    let checkInButtonVisible = await page.getByText('Select Room To Checkin').nth(0).isVisible();
    while (!checkInButtonVisible) {
        if (await page.getByText('504').isVisible()) {
            console.error('\tHit gateway timeout. Starting next family...');
            return;
        }

        checkInButtonVisible = await page.getByText('Select Room To Checkin').nth(0).isVisible();
    }
    
    await checkInFamilyMembers(page, scheduleTexts);
    
    const checkInButton = page.getByRole('link', { name: 'Check-In', exact: true });
    await expect(checkInButton).toBeVisible();
    // TODO reinitiate actual check-in when ready
    await page.locator('#ctl00_main_ctl02_ctl01_ctl00_btnCancel').click();
    // await checkInButton.click();
    // await expect(page.getByText('Welcome.')).toBeVisible();
    
    await expect(page).toHaveURL(/\/page\/438/);
}

async function selectSchedule(page: Page, schedule: string): Promise<string[]> {
    
    const headingIsVisible = await page.getByRole('heading', { name: 'Please Select One Or More' }).isVisible();
    if (!headingIsVisible) {
        return [schedule];
    }
    
    // Remove the last 4 as they are not schedule links.
    const scheduleLinks = (await page.getByRole('link').all()).slice(0, -4);

    const scheduleTexts: string[] = [];
    for (const scheduleLink of scheduleLinks) {
        const text = await scheduleLink.textContent();
        scheduleTexts.push(text);
        
        let background = await getBackgroundColor(scheduleLink);

        // If the background is white, the schedule is not selected.
        if (text === schedule && background === white) {
            await scheduleLink.click();
            while (background === white) {
                background = await getBackgroundColor(scheduleLink);
            }    
        }
        else if (text !== schedule && background !== white) {
            await scheduleLink.click();
            while (background !== white) {
                background = await getBackgroundColor(scheduleLink);
            }
        }
    }
    
    await page.getByRole('link', { name: 'Next' }).click();
    
    return scheduleTexts;
}

async function checkInFamilyMembers(page: Page, scheduleTexts: string[]) {

    const links = await page.getByRole('link').all();
    const personLinks: Locator[] = [];

    for (const link of links) {
        const text = await link.textContent();
        if (validatePersonLink(text, scheduleTexts)) {
            // .first() fixes the issue where multiple people have the same name, however it deselects the 
            // first duplicated person and never selects the second, so neither person will be checked in.
            // We anticipate this will not have a significant impact on the load test.
            personLinks.push(page.getByRole('link', { name: text, exact: true }).first());
        }
    }

    for (const personLink of personLinks) {
        await checkInPerson(page, personLink);
    }
}

async function checkInPerson(page: Page, personLink: Locator) {
    const text = await personLink.textContent();
    console.log(`\tChecking in ${text}`);
    
    await personLink.click();

    let background = await getBackgroundColor(personLink);

    // If the color is white, the person is not ready for submitting check-in.
    while (background === white) {
        const roomSelectionModal = page.locator('#ctl00_main_ctl02_ctl01_ctl00_mdChoose_modal_dialog_panel');
        if (await roomSelectionModal.isVisible()) {
            await roomSelectionModal.getByRole('link').nth(0).click();
            await expect(roomSelectionModal).toBeVisible({ visible: false });
        }

        background = await getBackgroundColor(personLink);
    }
}

async function getBackgroundColor(locator: Locator) {
    return await locator.evaluate((e) => {
        return window.getComputedStyle(e).getPropertyValue('background-color');
    });
}

function validatePersonLink(text: string, invalidTexts: string[]) {
    for (const invalid of invalidTexts) {
        if (text.includes(invalid)) {
            return false;
        }
    }
    
    return !(text.includes('Select Room To Checkin')
        || text === 'Check-in'
        || text === 'Check-In'
        || text === 'Next'
        || text === '+'
        || text === '');
}

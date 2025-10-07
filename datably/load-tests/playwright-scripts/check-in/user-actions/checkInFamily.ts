import {expect, Locator, Page} from "@playwright/test";

const white = 'rgb(255, 255, 255)';

export default async function checkInFamily(page: Page, phoneNumber: string, schedule: string) {
    await expect(page).toHaveURL(/\/page\/438/);

    const searchButton = page.getByRole('link', { name: 'Search' });
    await expect(searchButton).toBeVisible();

    await page.getByRole('link', { name: 'Clear' }).click();
    await page.keyboard.type(phoneNumber);
    await searchButton.click();

    const familySelector = page.getByText('Select your family to');
    while (page.url().includes('/page/438')) {
        // Exit check-in for this family if there are other families with the same phone number.
        // We don't have the data for the family names, so we can't differentiate between them.
        if (await familySelector.isVisible()) {
            console.error(`\tMultiple families with phone number ${phoneNumber} found.`);
            return;
        }
        
        if (await page.getByText('Sorry, we could not find your').isVisible()) {
            console.error(`\tFamily with number ${phoneNumber} not found.`);
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
            console.error(`\tFamily with number ${phoneNumber} received error: \"${error}\"`);

            await page.getByRole('link', { name: 'OK' }).click();
            await expect(page).toHaveURL(/\/page\/438/);
            return;
        }
    }

    const scheduleTexts = await selectSchedule(page, schedule);

    let checkInButtonVisible = await page.getByText('Select Room To Checkin').first().isVisible();
    while (!checkInButtonVisible) {
        if (await page.getByText('504').isVisible()) {
            console.error('\tHit gateway timeout.');
            await page.goto(`${process.env.BASE_URL}/page/438`);
            return;
        }
        else if (await page.getByText('has already been checked-in').first().isVisible()) {
            console.error(`\tFamily with number ${phoneNumber} has already been checked-in.`);
            await page.goto(`${process.env.BASE_URL}/page/438`);
            return;
        }

        checkInButtonVisible = await page.getByText('Select Room To Checkin').first().isVisible();
    }

    await checkInFamilyMembers(page, scheduleTexts);

    const checkInButton = page.getByRole('link', { name: 'Check-In', exact: true });
    await expect(checkInButton).toBeVisible();

    await checkInButton.click();
    await expect(page.getByText('Welcome')).toBeVisible();

    // Wait for the page to automatically route
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
            // Using .first() fixes the issue where multiple people have the same name, however it deselects the 
            // first duplicated person and never selects the second, so neither person will be checked in.
            // We anticipate this to not have a significant impact on the load test.
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
            await roomSelectionModal.getByRole('link').first().click();
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
        || text.includes('|')
        || text.includes(':')
        || text.includes('-')
        || text.includes('+')
        || text.includes('&')
        || text === 'Check-in'
        || text === 'Check-In'
        || text === 'Next'
        || text === '');
}

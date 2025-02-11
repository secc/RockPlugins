import {test} from "@playwright/test";
import {checkInFamily, openCheckInPage} from "./check-in";
import {getData, getUniqueConfigurationNames} from "./data/dataExporter";

const data = getData(1);


// getUniqueConfigurationNames().forEach((config) => {
[getUniqueConfigurationNames()[1]].forEach((config) => {
    test(config, async ({ page }) => {
        test.setTimeout(0);

        await openCheckInPage(page, config);

        let i = 1;
        const families = data.filter(d => d.ConfigurationName === config);
        for (const family of families) {
            console.log(`Checking in family ${i++}: ${family.Number}`);
            await checkInFamily(page, family.Number, 'Training First');
        }
    });
});


test('check-in: fail with non-existent kiosk type', async ({ context }) => {
    const page = await context.newPage();
    await openCheckInPage(page, 'Non-existent Kiosk Type');
});

test('check-in: fail with multiple families with same phone number', async ({ context }) => {
    const page = await context.newPage();
    await openCheckInPage(page, 'BB Kids and Volunteers');
    await checkInFamily(page, '1234567890', 'Training First');
});


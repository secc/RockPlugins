import {test} from "@playwright/test";
import {getData, getUniqueConfigurationNames} from "./data/dataExporter";
import checkInFamily from "./user-actions/checkInFamily";
import openCheckInPage from "./user-actions/openCheckInPage";

const vm = Number(process.env.CHECK_IN_VM_NUMBER);
const vmCount = Number(process.env.CHECK_IN_VM_COUNT);

const data = getData().filter(x => x.Id % vmCount == vm);

getUniqueConfigurationNames().forEach((config) => {
    test(config, async ({ browser }) => {
        
        const context = await browser.newContext();
        
        // If we do not add this cookie, /familycheckin returns a 404.
        // I'm guessing this is because SECC has a web farm, but not all servers have check-in.
        await context.addCookies([
            {
                name: 'last_site',
                value: '7',
                domain: 'rockbeta.secc.org',
                path: '/'
            },
        ])
        
        const page = await context.newPage();
        await openCheckInPage(page, config);
        
        let i = 1;
        const families = data.filter(d => d.ConfigurationName === config);
        for (const family of families) {
            console.log(`Checking in family ${i++}: ${family.Number}`);
            await checkInFamily(page, family.Number, 'Training First');
        }
    });
});

// To run these tests, comment out test.skip()
// To validate these tests passed properly, watch the console output
test.describe('Edge cases and error routes for check-in', () => {
    test.skip();
    
    test('Allow multiple people with the same name', async ({ context }) => {
        const page = await context.newPage();
        await openCheckInPage(page, 'BC Kids and Volunteers');
        await checkInFamily(page, '1150099668', 'Training First');
    });
    
    test('Return when multiple families have the same phone number', async ({ context }) => {
        const page = await context.newPage();
        await openCheckInPage(page, 'BB Kids and Volunteers');
        await checkInFamily(page, '1234567890', '');
    });

    test('Return when loading family leads to an error page', async ({ context }) => {
        // Error: "Please see a SE!KIDS Representative."
        const page = await context.newPage();
        await openCheckInPage(page, 'BT Kids and Volunteers');
        await checkInFamily(page, '1150132996', '');
    });
    
    test('Return when a family could not be found with the given phone number', async ({ context }) => {
        const page = await context.newPage();
        await openCheckInPage(page, 'BB Other Volunteers');
        await checkInFamily(page, '1151393272', '');
    })
});
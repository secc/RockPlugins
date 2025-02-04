import {test} from "@playwright/test";
import {checkInFamily, openCheckInPage} from "./check-in";
import getData from "./data/dataExporter";

test('check-in', async ({ context }) => {
    test.setTimeout(60_000);
    const data = getData(0);
    
    const page = await context.newPage();
    // for (const family of data) {
    //     await openCheckInPage(page);
    //     await checkInAllKids(page, family.Number, 'Training First');
    // }
    await openCheckInPage(page, data[1].ConfigurationName);
    await checkInFamily(page, data[1].Number, 'Training First');
});

// test('check-in: fail with non-existent kiosk type', async ({ context }) => {
//     const data = getData();
//
//     const page = await context.newPage();
//     await openCheckInPage(page, 'Non-existent Kiosk Type');
//     await checkInFamily(page, data[1].Number, 'Training First');
// });
//
// test('check-in: fail with multiple families with same phone number', async ({ context }) => {
//     const data = getData();
//
//     const page = await context.newPage();
//     await openCheckInPage(page, data[1].ConfigurationName);
//     await checkInFamily(page, '1234567890', 'Training First');
// });


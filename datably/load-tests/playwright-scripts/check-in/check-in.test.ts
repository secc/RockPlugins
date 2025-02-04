import {test} from "@playwright/test";
import {checkInAllKids, openCheckInPage} from "./check-in";
import getData from "./data/dataExporter";

test('check-in', async ({ context }) => {
    const data = getData();
    
    const page = await context.newPage();
    // for (const family of data) {
    //     await openCheckInPage(page);
    //     await checkInAllKids(page, family.Number, 'Training First');
    // }
    await openCheckInPage(page, data[1].ConfigurationName);
    await checkInAllKids(page, data[1].Number, 'Training First');
});


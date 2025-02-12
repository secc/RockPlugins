import {test} from "@playwright/test";
import {getData, getUniqueConfigurationNames} from "../data/dataExporter";
import openCheckInPage from "../scripts/openCheckInPage";
import checkInFamily from "../scripts/checkInFamily";

const data = getData(2);

getUniqueConfigurationNames().forEach((config) => {
    test(config, async ({ page }) => {
        await openCheckInPage(page, config);

        let i = 1;
        const families = data.filter(d => d.ConfigurationName === config);
        for (const family of families) {
            console.log(`Checking in family ${i++}: ${family.Number}`);
            await checkInFamily(page, family.Number, 'Training First');
        }
    });
});
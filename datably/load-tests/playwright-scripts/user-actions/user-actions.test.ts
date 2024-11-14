import {test} from "@playwright/test";
import {baseline, loginAndNavigateRoutes} from "./user-actions";


test('baseline', async ({ page }) => {
    await baseline(page);
});

test('login and navigate demo', async ({ page }) => {
    test.setTimeout(120000);
    await loginAndNavigateRoutes(page);
});


import {test} from "@playwright/test";
import {baseline, SRU13_CreateADataViewAndReport, SRU15_CheckingAPersonProfile, SRU16_ViewingEventRegistration, SRU17_VisitExternalPageAccountAndBrowse, SRU18_Groups, SRU19_EventExternalPage} from "./user-actions";


test('baseline', async ({ page }) => {
    await baseline(page);
});

test('SRU13_CreateADataViewAndReport', async ({ page }) => {
    test.setTimeout(120000);
    await SRU13_CreateADataViewAndReport(page);
});

test('SRU15_CheckingAPersonProfile', async ({ page }) => {
    test.setTimeout(120000);
    await SRU15_CheckingAPersonProfile(page);
});

test('SRU16_ViewingEventRegistration', async ({ page }) => {
    test.setTimeout(120000);
    await SRU16_ViewingEventRegistration(page);
});

test('SRU17_VisitExternalPageAccountAndBrowse', async ({ page }) => {
    test.setTimeout(120000);
    await SRU17_VisitExternalPageAccountAndBrowse(page);
});

test('SRU18_Groups', async ({ page }) => {
    test.setTimeout(120000);
    await SRU18_Groups(page);
});

test('SRU19_EventExternalPage', async ({ page }) => {
    test.setTimeout(120000);
    await SRU19_EventExternalPage(page);
});


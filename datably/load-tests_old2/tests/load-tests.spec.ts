import { test, expect } from '@playwright/test';
import {userActions} from "../../load-tests/playwright-scripts/user-actions/user-actions";
import {member1} from "../../load-tests/playwright-scripts/user-actions/member-1";


test('login and navigate demo', async ({ page }) => {
  await userActions(page);
});

test('Login as member 1', async ({ page }) => {
  await member1(page);
});

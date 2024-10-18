const { test, expect } = require('@playwright/test');

test.describe('Load Tests', ()=>{

    const user = 'julio.cachay@datably.io';
    const password = 'Chattanooga25!';

    test('Can access rock', async ({page})=>{


        await page.goto('https://datablyrockdev.secc.org/');


        const userInput = await page.getByLabel('Username').fill(user);

        const passwordInput = await page.getByLabel('Password').fill(password);

        await page.getByRole('button', {name: 'Login'}).click();

        await page.waitForTimeout(1000);

        await page.goto('https://datablyrockdev.secc.org/1/events');
    
      //  const loginButton = await page.locator('button', { hasText: 'Login' });
       // await expect(loginButton).toBeVisible();
    });
});


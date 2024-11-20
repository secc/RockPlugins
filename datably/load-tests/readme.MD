
# Load testing for Secc rock

We will use [Artillery](https://www.artillery.io/docs/get-started/get-artillery) to simulate virtual users performing actions.

Those actions are defined in scripts under /user-actions/user-actions.ts

## Viewing user actions
In some cases, you want to see, in the browser, how the user actions work.

For that, there is a user-actions.test.ts file, you can run that using playwright

under playwright-scripts/user-actions:
> npx playwright test --ui

## Running the load tests

> artillery run  load-test.yml



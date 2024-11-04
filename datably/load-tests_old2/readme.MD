
# Artillery tests

[Artillery](https://www.artillery.io/docs/reference/cli/run) is a library to perform load testing. Follow the installation guide (you need the CLI installed)

Artillery uses a playwright script (see tests/run-functions)

The Playwright script defines the action a virtual user will take.
Artillery simulates several virtual users performing those actions.

## Run the test
under tests/

> artillery run load-tests.spec.ts
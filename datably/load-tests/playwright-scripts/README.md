## Check-In Load Tests
These tests are for analyzing the load that the dev server can handle when checking in users.

The tests rely on data found in the `./check-in/data` directory. 

The tests are split up into three equivalent sections, with each section needing to be run from its own Virtual Machine so the load balancer will move the network traffic properly.

### Running the Load Tests
For each VM:
- Download the repository
- Navigate to this directory
- Run `npm ci`
- Create a `.env` file in the current directory, using `.env.example` as a template
  - `BASE_URL` is already set with the URL of the Datably development server for your convenience
  - `USERNAME` and `PASSWORD` refer to the credentials that will be used to sign in before creating a new kiosk
  - `CHECK_IN_VM_COUNT` refers to the total number of VMs running the load tests
  - `CHECK_IN_VM_NUMBER` refers to the specific VM running load tests
    - Each VM should have a unique value for this variable and it should lie within the range `0 to (CHECK_IN_VM_COUNT - 1)`
    - This value determines which families the VM will be responsible for checking in

After the `.env` files have been created and the node packages have been installed on all VMs, run the following command on each from this directory:

`npm run test:check-in`

The console should output progress updates, such as when it is checking in a user and if the script runs into any issues.

Note that the load tests can only be run once per check-in session, since we could not configure it to allow duplicate check-ins. Check-in sessions more than likely last an entire day. Chris Funk should be supplying a script to reset users' check-in status so that the load tests can be run multiple times per check-in session. Once we receive that, we will update this documentation to include that process.

### How to increase/decrease load
The Playwright engine is configured to run a certain number of tests in parallel, equal to 50% of the number of CPU cores on the host machine. This can be adjusted from `playwright.config.ts` by setting the `workers` property. 

To increase the amount of load as much as possible, you can set `workers` to `23`, since the number of tests is dependent on the number of different check-in configurations in the data, which is 23. However, this will have the effect of greatly slowing down the host machine if it has 23 or less CPU cores.

To decrease the load, you can lower the value for `workers`. Setting it to `1` will run the tests sequentially.
## Check-In Load Tests
These tests are for analyzing the load that the dev server can handle when checking in users.

They rely on data found in the `/data` directory. 

The tests are split up into three equivalent sections, with each section needing to be ran from its own Virtual Machine.

### Running the Load Tests
For each VM:
- Download the repository
- Navigate to the `datably/load-tests/playwright-scripts` directory
- Run `npm ci`
- Create a `.env` file in the current directory, using `.env.example` as a template

After the node packages have been installed on all VMs, run the following command on each:

`npm run test:check-in:n`, where `n` is the number of the VM (1, 2, or 3)

The console should output progress updates, such as when it is checking in a user and if there are any issues with a specific family.


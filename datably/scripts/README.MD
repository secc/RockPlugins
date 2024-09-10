# Scripts to run V1.13.7
## Overview
The main motivation for these scripts is to facilitate local development so we can more efficiently:
1. Resolve bugs.
2. Upgrade versions.
3. Test Plugins.

## Mechanics.
1. Verify requirements (e.g. node 16)
2. Copy Rock source code to a temporary src folder.
3. Copy Secc plugins to "src/Rock/secc"
4. Add the Secc plugin projects to the Rock solution.
5. Create the symbolic links in the Rock.Web project.
6. Install front-end dependencies (NPM)
7. Start a fresh database.

# Run Rock
You will need:
- Docker
- Net Framework SDK 4.7.2
- Visual Studio

## 1. Clone Rock
1. Clone rock. Currently we are using hotfix-1.13.7 from SECC
2. In buildrock.sh and start-db.sh, replace the variables at the top with the correct file locations.
3. Run buildrock.sh

Open the src/Rock solution with visual studio, build and run.
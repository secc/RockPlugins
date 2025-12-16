# GitHub Copilot Instructions for RockPlugins

## Repository Overview

This repository contains custom plugins and themes for Rock RMS (Rock Management System) developed and maintained by Southeast Christian Church (SECC). Rock RMS is a comprehensive church management system, and this repository extends its functionality with custom plugins specific to SECC's needs.

## Technology Stack

- **Language**: C# (.NET Framework 4.7.2)
- **Primary Platform**: Rock RMS
- **Key Dependencies**:
  - Entity Framework 6.x
  - ASP.NET Web Forms
  - Rock RMS Core Libraries
  - Various third-party APIs (Google Sheets, PayPal, etc.)
- **Build System**: MSBuild (Visual Studio project format)

## Repository Structure

```
/Plugins/                    # Individual plugin projects
  /org.secc.*/              # Each plugin is namespaced under org.secc
    /*.csproj               # C# project file
    /bin/                   # Build output (git-ignored)
    /obj/                   # Build intermediates (git-ignored)
/Assets/                    # Shared assets
/Content/                   # Shared content
/Tools/                     # Development and utility tools
/Themes/                    # Custom Rock RMS themes
```

## Plugin Organization

- Each plugin is a separate C# class library project
- Plugins follow the naming convention: `org.secc.[PluginName]`
- Common plugin categories:
  - **Administration**: Administrative tools and utilities
  - **Communication**: Email, SMS, and other communication features
  - **Authentication/OAuth**: Authentication and authorization extensions
  - **Finance**: Payment processing and financial tools
  - **Reporting**: Custom reports and data analysis
  - **Themes**: Custom UI themes for Rock RMS

## Coding Standards

### C# Style Guidelines

1. **Namespace Convention**: All code uses the `org.secc.[PluginName]` namespace
2. **Copyright Headers**: Include Southeast Christian Church copyright header on new files:
   ```csharp
   // <copyright>
   // Copyright Southeast Christian Church
   //
   // Licensed under the  Southeast Christian Church License (the "License");
   // you may not use this file except in compliance with the License.
   // A copy of the License shoud be included with this file.
   //
   // Unless required by applicable law or agreed to in writing, software
   // distributed under the License is distributed on an "AS IS" BASIS,
   // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   // See the License for the specific language governing permissions and
   // limitations under the License.
   // </copyright>
   ```
3. **Code Formatting**:
   - Use 4-space indentation
   - Use PascalCase for public members, methods, and classes
   - Use camelCase for private fields (prefix with underscore `_fieldName`)
   - Opening braces on new line (Allman style)
4. **Rock RMS Patterns**:
   - Extend Rock RMS base classes where appropriate (e.g., `Rock.Data.Service<T>`)
   - Use Rock's data context (`RockContext`) for database operations
   - Leverage Rock's caching mechanisms (e.g., `CampusCache`)
   - Follow Rock's attribute and workflow patterns

## Build and Deployment

### Building Projects

- Projects use MSBuild and can be built with Visual Studio 2017+
- Each project has a PostBuildEvent that copies built assemblies to the RockWeb directory
- Build outputs go to `bin/Debug/` or `bin/Release/` (these are git-ignored)

### Dependencies

- NuGet packages are managed per-project with `packages.config`
- Shared dependencies are stored in `$(SolutionDir)packages/`
- Some dependencies reference the parent Rock RMS solution

### Deployment Notes

- Built plugins are copied to `$(SolutionDir)RockWeb/Plugins/org_secc/`
- DLLs are copied to `$(SolutionDir)RockWeb/Bin/`
- This repository is meant to be used alongside a Rock RMS installation

## Licensing

**Important**: This repository uses a custom Southeast Christian Church License that:
- Permits use, reproduction, and derivative works **solely for internal use by Faith Based Organizations**
- Requires contacting Southeast Christian Church for commercial use
- Is NOT a standard open-source license

When contributing code:
- Ensure all contributions comply with the license terms
- Include the appropriate copyright header on new files
- Do not introduce code with incompatible licenses

## Development Workflow

### Working with Issues

- Issues should be well-scoped with clear acceptance criteria
- Reference the specific plugin(s) affected
- Include Rock RMS version compatibility information when relevant

### Pull Requests

- Keep changes focused on a single plugin or feature
- Test changes in a Rock RMS development environment
- Ensure builds succeed before submitting
- Include any necessary migration code for database changes

### Testing

- Test changes within a working Rock RMS installation
- Verify plugin functionality in the Rock RMS admin interface
- Check for any database migration requirements
- Test with appropriate Rock RMS permissions

## Common Patterns

### Creating a New Plugin

1. Create a new C# Class Library project in `/Plugins/`
2. Use naming convention: `org.secc.[PluginName]`
3. Target .NET Framework 4.7.2
4. Reference Rock RMS core libraries from the parent solution
5. Add appropriate PostBuildEvent for deployment
6. Include copyright header in source files

### Data Access

- Use `RockContext` for Entity Framework operations
- Extend `Rock.Data.Service<T>` for custom service classes
- Use Rock's data annotations and validation attributes
- Follow Rock's migration pattern for database changes

### Working with Rock Components

- Use Rock's attribute system for configuration
- Leverage Rock's workflow engine for business processes
- Use Rock's block system for UI components
- Follow Rock's security and permission patterns

## Files to Avoid Modifying

- Do not modify `/bin/` or `/obj/` directories (build artifacts)
- Do not modify `/packages/` directories (NuGet packages)
- Be cautious with `.csproj` files unless adding new files
- Do not modify parent Rock RMS solution files

## Security Considerations

- Never commit sensitive data (credentials, API keys, connection strings)
- Use Rock's attribute system for configuration that may contain secrets
- Follow Rock's security model for permission checks
- Validate and sanitize all user inputs
- Use parameterized SQL queries to prevent SQL injection

## Additional Resources

- [Rock RMS Documentation](https://www.rockrms.com/Rock/Developer)
- [Rock RMS Community](https://community.rockrms.com/)
- Project License: See `License.txt` in repository root

## Contact

For questions about this repository or commercial licensing, contact Southeast Christian Church at applications@secc.org

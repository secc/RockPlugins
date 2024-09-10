PathToPlugins="C:\Users\Julio\Documents\repos\secc-rock-upgrade\RockPlugins"
PathToRock="C:\Users\Julio\Documents\repos\secc-rock-upgrade\Rock"
PathToTempSrcFolder="C:\Users\Julio\Documents\repos\secc-rock-upgrade\src"
UpdateSeccNetFramework="v4.7.2" # Set to empty string to not update the framework version

# Check Node.js version
required_version="16"
node_version=$(node -v)

if [[ $node_version != v$required_version* ]]; then
    echo "Error: Node.js version $required_version.x.x is required. Current version is $node_version."
    exit 1
fi

# Recreate source folder
if [ -d "$PathToTempSrcFolder" ]; then
    echo "removing temporary src folder"
    rm -rf "$PathToTempSrcFolder"
else
    echo "$PathToTempSrcFolder does not exist."
fi
mkdir "$PathToTempSrcFolder"
    
# Copy Rock
echo "Copying rock files to temporary src folder"
cp -r "$PathToRock" "$PathToTempSrcFolder"

# Copy Plugins
echo "Copying secc plugins to temporary src folder"
#cp -r "$PathToPlugins" "$PathToTempSrcFolder/Rock/secc"

# Add all Plugin projects to the Rock solution
#find "$PathToTempSrcFolder/Rock/secc" -name "*.csproj" ! -path "*/Tools/*" -exec dotnet sln "$PathToTempSrcFolder/Rock/Rock.sln" add {} \;



# Update the Net framwork version
if [ -n "$UpdateSeccNetFramework" ]; then
    echo "Updating .NET Framework version to $UpdateSeccNetFramework..."
#    find "$PathToTempSrcFolder/Rock/secc" -name "*.csproj" ! -path "*/Tools/*" -exec sed -i "s|<TargetFrameworkVersion>.*</TargetFrameworkVersion>|<TargetFrameworkVersion>$UpdateSeccNetFramework</TargetFrameworkVersion>|g" {} \;
    # This is needed for V1.13.7
    echo "Adding SECC Plugin references to Rock.Common so it works for version 13"
 #   find "$PathToTempSrcFolder/Rock/secc" -name "*.csproj" ! -path "*/Tools/*" -exec sh -c 'dotnet add "$1" reference "$2"' _ {} "$PathToTempSrcFolder/Rock/Rock.Common/Rock.Common.csproj" \;


else
    echo "UpdateSeccNetFramework is empty. Skipping .NET Framework version update."
fi

if [ -d "$PathToRock/Rock.JavaScript.EditorJs" ]; then 
    echo "Installing Rock.Javascript.EditorJs dependencies"
    cd "$PathToRock/Rock.JavaScript.EditorJs"
    npm install
    cd -
    cd "$PathToRock/Rock.JavaScript.Obsidian"
    npm install
    cd -
fi 
# Create Symbolic links
#./makelinks.sh 

# Copy connection strings
cp web.ConnectionStrings.config $PathToTempSrcFolder/Rock/RockWeb/web.ConnectionStrings.config

# Setup Database
#./start-db.sh 





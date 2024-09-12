PathToPlugins="C:\Users\Julio\Documents\repos\secc-rock-upgrade\RockPlugins"
PathToRock="C:\Users\Julio\Documents\repos\secc-rock-upgrade\Rock"
PathToTempSrcFolder="C:\Users\Julio\Documents\repos\secc-rock-upgrade\src2"
Port=6234

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
cp -r "$PathToPlugins" "$PathToTempSrcFolder/Rock/secc"

# Add all Plugin projects to the Rock solution
find "$PathToTempSrcFolder/Rock/secc" -name "*.csproj" ! -path "*/Tools/*" -exec dotnet sln "$PathToTempSrcFolder/Rock/Rock.sln" add {} \;

if [ -d "$PathToRock/Rock.JavaScript.EditorJs" ]; then 
    echo "Installing Rock.Javascript.EditorJs dependencies"
    cd "$PathToRock/Rock.JavaScript.EditorJs"
    npm install
    cd -
    cd "$PathToRock/Rock.JavaScript.Obsidian"
    npm install
    cd -
fi 

# Replace port number in Rock.sln
sed -i "s/6229/$Port/g" "$PathToTempSrcFolder/Rock/Rock.sln"

# Create Symbolic links
./makelinks.sh "$PathToTempSrcFolder/Rock/secc/Plugins" "$PathToTempSrcFolder/Rock/RockWeb/Plugins"

# Copy connection strings
cp web.ConnectionStrings.config $PathToTempSrcFolder/Rock/RockWeb/web.ConnectionStrings.config

# Setup Database
./start-db.sh 





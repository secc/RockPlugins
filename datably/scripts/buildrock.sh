PathToPlugins="E:\repos\secc-rock-upgrade\RockPlugins"
PathToRock="E:\repos\secc-rock-upgrade\Rock"
PathToTempSrcFolder="E:\repos\secc-rock-upgrade\src"
Port=6232
ConnectionStringName="web.ConnectionStrings.azure.config"
IncludePlugins=true # true or false
ApplyMigrations=true
DeployMode=false #true or false. When true, instead of creating symbolic links for plugins it will copy the code

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
if [ "$IncludePlugins" = true ]; then
    echo "Copying secc plugins to temporary src folder"
    cp -r "$PathToPlugins" "$PathToTempSrcFolder/Rock/secc"
fi

# Add all Plugin projects to the Rock solution
if [ "$IncludePlugins" = true ]; then
find "$PathToTempSrcFolder/Rock/secc" -name "*.csproj" ! -path "*/Tools/*" -exec dotnet sln "$PathToTempSrcFolder/Rock/Rock.sln" add {} \;
fi 

# Create Symbolic links
if [ "$IncludePlugins" = true ]; then
echo "Adding plugins"
./makelinks.sh "$PathToTempSrcFolder/Rock/secc/Plugins" "$PathToTempSrcFolder/Rock/RockWeb/Plugins" $DeployMode
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

# Replace port number in Rock.sln
sed -i "s/6229/$Port/g" "$PathToTempSrcFolder/Rock/Rock.sln"



if [ "$ApplyMigrations" = true ]; then
    echo "Applying migrations"
    cd "$PathToTempSrcFolder/Rock/RockWeb/App_Data"
    touch run.migration
    cd -
fi

# Copy connection strings
cp $ConnectionStringName $PathToTempSrcFolder/Rock/RockWeb/web.ConnectionStrings.config

# Setup Database
#./start-db.sh 







CurrentPluginsPath=$1
DesiredPluginsPath=$2
DeployMode=$3

# Delete the target folder to start fresh
if [ -d "$DesiredPluginsPath/org_secc" ]; then
    rm -rf "$DesiredPluginsPath/org_secc"
    echo "Removed $DesiredPluginsPath/org_secc directory and all its contents."
fi

# This function adds a symbolic link for the specified plugin
copy_plugin() {
    #C:\Users\jcach\Documents\repos\secc-rock-upgrade\RockPlugins\Plugins\org.secc.Administration\org_secc\Administration
    #C:\Users\jcach\Documents\repos\secc-rock-upgrade\src\Rock\RockWeb\Plugins\org_secc\Authentication
    local PluginName=$1
    
    mkdir -p "$DesiredPluginsPath/org_secc"
    # Add a symbolic link in the RockWeb/Plugins/... pointing to the plugin physical location
    ls $CurrentPluginsPath/org.secc.$PluginName/org_secc/$PluginName

    if [ $DeployMode = true ]; then 
        echo "Copying code to RockWeb for plugin: $PluginName"
        cp -r "$CurrentPluginsPath/org.secc.$PluginName/org_secc/$PluginName" "$DesiredPluginsPath/org_secc/$PluginName"
    else 
         echo "Adding Symbolic Link for plugin: $PluginName"
         ln -s "$CurrentPluginsPath/org.secc.$PluginName/org_secc/$PluginName" "$DesiredPluginsPath/org_secc/$PluginName"
    fi 
   
}



copy_plugin "Administration"
copy_plugin "Authentication"
copy_plugin "ChangeManager"
copy_plugin "CheckinMonitor"
copy_plugin "Cms"
copy_plugin "Communication"
copy_plugin "Connection"
copy_plugin "ConnectionCards"
copy_plugin "EMS"
copy_plugin "Equip"
copy_plugin "Event"
copy_plugin "GroupManager"
copy_plugin "Imaging"
copy_plugin "Jira"
copy_plugin "LessInclude"
copy_plugin "Microframe"
copy_plugin "OAuth"
copy_plugin "PastoralCare"
copy_plugin "PayPalExpress"
copy_plugin "PayPalReporting"
copy_plugin "PDFExamples"
copy_plugin "Purchasing"
copy_plugin "RecurringCommunications"
copy_plugin "Reporting"
copy_plugin "RoomScanner"
copy_plugin "SafetyAndSecurity"
copy_plugin "Sass"
copy_plugin "Search"
copy_plugin "Security"
copy_plugin "SportsAndFitness"
copy_plugin "SystemsMonitor"
copy_plugin "Trak1"
copy_plugin "Widgities"
copy_plugin "Workflow"

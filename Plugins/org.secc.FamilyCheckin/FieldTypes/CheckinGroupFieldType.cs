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
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using org.secc.FamilyCheckin.UI;
using Rock;
using Rock.Data;
using Rock.Field;
using Rock.Web.UI.Controls;

namespace org.secc.FamilyCheckin.FieldTypes
{
    /// <summary>
    /// Field Type to select a single (or null) Group
    /// Stored as Group.Guid
    /// </summary>
    public class CheckinGroupFieldType : FieldType
    {
        #region Configuration

        /// <summary>
        /// Configuration Key for GroupAndRole Picker Label
        /// </summary>
        public static readonly string CONFIG_GROUP_PICKER_ALLOW_MULTI_SELECT = "groupPickerAllowMultiSelect";

        /// <summary>
        /// Returns a list of the configuration keys
        /// </summary>
        /// <returns></returns>
        public override List<string> ConfigurationKeys()
        {
            List<string> configKeys = new List<string>();
            configKeys.Add( CONFIG_GROUP_PICKER_ALLOW_MULTI_SELECT );
            return configKeys;
        }

        /// <summary>
        /// Creates the HTML controls required to configure this type of field
        /// </summary>
        /// <returns></returns>
        public override List<Control> ConfigurationControls()
        {
            List<Control> controls = new List<Control>();

            var groupPickerAllowMultiSelect = new RockCheckBox();
            groupPickerAllowMultiSelect.AutoPostBack = true;
            groupPickerAllowMultiSelect.Text = "Yes";
            controls.Add( groupPickerAllowMultiSelect );
            groupPickerAllowMultiSelect.Label = "Allow Multi-Select";
            groupPickerAllowMultiSelect.Help = "When checked, more than one group can be selected.";

            return controls;
        }

        /// <summary>
        /// Gets the configuration value.
        /// </summary>
        /// <param name="controls">The controls.</param>
        /// <returns></returns>
        public override Dictionary<string, ConfigurationValue> ConfigurationValues( List<Control> controls )
        {
            Dictionary<string, ConfigurationValue> configurationValues = new Dictionary<string, ConfigurationValue>();
            configurationValues.Add( CONFIG_GROUP_PICKER_ALLOW_MULTI_SELECT, new ConfigurationValue( "Allow Multi-Select", "When checked, more than one group can be selected.", string.Empty ) );

            if ( controls != null && controls.Count == 1 )
            {
                if ( controls[0] != null && controls[0] is RockCheckBox )
                {
                    configurationValues[CONFIG_GROUP_PICKER_ALLOW_MULTI_SELECT].Value = ( ( RockCheckBox ) controls[0] ).Checked.ToString();
                }
            }

            return configurationValues;
        }

        /// <summary>
        /// Sets the configuration value.
        /// </summary>
        /// <param name="controls">The controls.</param>
        /// <param name="configurationValues">The configuration values.</param>
        public override void SetConfigurationValues( List<Control> controls, Dictionary<string, ConfigurationValue> configurationValues )
        {
            if ( controls != null && controls.Count == 1 && configurationValues != null )
            {
                if ( controls[0] != null && controls[0] is RockCheckBox && configurationValues.ContainsKey( CONFIG_GROUP_PICKER_ALLOW_MULTI_SELECT ) )
                {
                    ( ( RockCheckBox ) controls[0] ).Checked = configurationValues[CONFIG_GROUP_PICKER_ALLOW_MULTI_SELECT].Value.AsBoolean();
                }
            }
        }

        #endregion

        #region Formatting

        /// <summary>
        /// Returns the field's current value(s)
        /// </summary>
        /// <param name="parentControl">The parent control.</param>
        /// <param name="value">Information about the value</param>
        /// <param name="Values">The configuration values.</param>
        /// <param name="condensed">Flag indicating if the value should be condensed (i.e. for use in a grid column)</param>
        /// <returns></returns>
        public override string FormatValue( Control parentControl, string value, Dictionary<string, ConfigurationValue> configurationValues, bool condensed )
        {
            string formattedValue = value;

            if ( !string.IsNullOrWhiteSpace( value ) )
            {
                var guids = value.Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries )
                    .Select( g => g.AsGuid() )
                    .ToList();
                var groupNames = new Rock.Model.GroupService( new RockContext() )
                    .GetByGuids( guids )
                    .Select( g => g.Name );
                if ( groupNames.Any() )
                {
                    formattedValue = string.Join( ", ", groupNames );
                }
            }
            return base.FormatValue( parentControl, formattedValue, configurationValues, condensed );
        }

        #endregion

        #region Edit Control

        /// <summary>
        /// Creates the control(s) necessary for prompting user for a new value
        /// </summary>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="id"></param>
        /// <returns>
        /// The control
        /// </returns>
        public override System.Web.UI.Control EditControl( Dictionary<string, ConfigurationValue> configurationValues, string id )
        {
            bool allowMultiSelect = false;
            if ( configurationValues != null
                && configurationValues.ContainsKey( CONFIG_GROUP_PICKER_ALLOW_MULTI_SELECT )
                && configurationValues[CONFIG_GROUP_PICKER_ALLOW_MULTI_SELECT] != null )
            {
                allowMultiSelect = configurationValues[CONFIG_GROUP_PICKER_ALLOW_MULTI_SELECT].Value.AsBoolean();
            }

            CheckinGroupPicker groupPicker = new CheckinGroupPicker { ID = id, AllowMultiSelect = allowMultiSelect };
            return groupPicker;
        }

        /// <summary>
        /// Reads new values entered by the user for the field (as id)
        /// </summary>
        /// <param name="control">Parent control that controls were added to in the CreateEditControl() method</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <returns></returns>
        public override string GetEditValue( System.Web.UI.Control control, Dictionary<string, ConfigurationValue> configurationValues )
        {
            bool allowMultiSelect = false;
            if ( configurationValues != null
                && configurationValues.ContainsKey( CONFIG_GROUP_PICKER_ALLOW_MULTI_SELECT )
                && configurationValues[CONFIG_GROUP_PICKER_ALLOW_MULTI_SELECT] != null )
            {
                allowMultiSelect = configurationValues[CONFIG_GROUP_PICKER_ALLOW_MULTI_SELECT].Value.AsBoolean();
            }

            CheckinGroupPicker picker = control as CheckinGroupPicker;
            if ( picker != null )
            {
                if ( allowMultiSelect )
                {
                    return string.Join( "|", picker.SelectedGroups.Select( g => g.Guid.ToString() ) );
                }
                else
                {
                    if ( picker.SelectedGroup != null )
                    {
                        return picker.SelectedGroup.Guid.ToString();
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Sets the value (as id)
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="value">The value.</param>
        public override void SetEditValue( System.Web.UI.Control control, Dictionary<string, ConfigurationValue> configurationValues, string value )
        {
            
            var picker = control as CheckinGroupPicker;

            if ( picker != null )
            {
                bool allowMultiSelect = false;
                if ( configurationValues != null
                && configurationValues.ContainsKey( CONFIG_GROUP_PICKER_ALLOW_MULTI_SELECT )
                && configurationValues[CONFIG_GROUP_PICKER_ALLOW_MULTI_SELECT] != null )
                {
                    allowMultiSelect = configurationValues[CONFIG_GROUP_PICKER_ALLOW_MULTI_SELECT].Value.AsBoolean();
                }

                if ( allowMultiSelect )
                {
                    if ( !string.IsNullOrWhiteSpace( value ) )
                    {
                        var groupGuids = value.Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries )
                            .Select( g => g.AsGuid() )
                            .ToList();
                        var groups = new Rock.Model.GroupService( new RockContext() ).GetByGuids( groupGuids );
                        picker.SelectedGroups = groups;
                    }
                }
                else
                {
                    Guid guid = value.AsGuid();

                    // get the item (or null) and set it
                    var group = new Rock.Model.GroupService( new RockContext() ).Get( guid );
                    picker.SelectedGroup = group;
                }
            }
        }

        #endregion
    }
}
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
using System.Web.UI;
using System.Web.UI.WebControls;
using org.secc.Attributes.Controls;
using org.secc.Attributes.Helpers;
using Rock;
using Rock.Field;
using Rock.Web.UI.Controls;

namespace org.secc.Attributes.FieldTypes
{
    /// <summary>
    /// Field used to save and display a dynamic phone number
    /// </summary>
    [Serializable]
    public class CascadingDropDownFieldType : Rock.Field.FieldType
    {

        #region Configuration

        private const string CONFIG = "configuration";

        /// <summary>
        /// Returns a list of the configuration keys
        /// </summary>
        /// <returns></returns>
        public override List<string> ConfigurationKeys()
        {
            var configKeys = base.ConfigurationKeys();
            configKeys.Add( CONFIG );
            return configKeys;
        }

        /// <summary>
        /// Creates the HTML controls required to configure this type of field
        /// </summary>
        /// <returns></returns>
        public override List<Control> ConfigurationControls()
        {
            var controls = new List<Control>();
            var textBox = new RockTextBox
            {
                AutoPostBack = true,
                Label = "Data Matrix",
                Help = "Matrix of data to filter by. Create a comma separated key^value row for each set. For SQL return a column for each dropdown with the format 'key^value'. ",
                Required = true,
                TextMode = TextBoxMode.MultiLine,
                Height = 300
            };
            textBox.TextChanged += OnQualifierUpdated;
            controls.Add( textBox );
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
            configurationValues.Add( CONFIG, new ConfigurationValue( "Data Matrix", "Matrix of data to filter by. Create coma separated key^value row for each set. For SQL return a column for each dropdown with the format 'key^value'. ", "" ) );

            if ( controls.Count > 0 && controls[0] is RockTextBox )
            {
                configurationValues[CONFIG].Value = ( ( RockTextBox ) controls[0] ).Text;
            }

            return configurationValues;
        }

        /// <summary>
        /// Sets the configuration value.
        /// </summary>
        /// <param name="controls"></param>
        /// <param name="configurationValues"></param>
        public override void SetConfigurationValues( List<Control> controls, Dictionary<string, ConfigurationValue> configurationValues )
        {
            if ( configurationValues != null )
            {
                if ( controls.Count > 0 && controls[0] is RockTextBox && configurationValues.ContainsKey( CONFIG ) )
                {
                    ( ( RockTextBox ) controls[0] ).Text = configurationValues[CONFIG].Value;
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
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="condensed">Flag indicating if the value should be condensed (i.e. for use in a grid column)</param>
        /// <returns></returns>
        public override string FormatValue( Control parentControl, string value, Dictionary<string, ConfigurationValue> configurationValues, bool condensed )
        {
            var config = "";
            if ( configurationValues != null &&
  configurationValues.ContainsKey( CONFIG ) )
            {
                config = configurationValues[CONFIG].Value;
            }

            if ( config.IsNullOrWhiteSpace() )
            {
                return null;
            }

            var matrix = new KeyValueMatrix( config );

            return matrix.FormatValue( value );
        }

        /// <summary>
        /// Reads new values entered by the user for the field (as id)
        /// </summary>
        /// <param name="control">Parent control that controls were added to in the CreateEditControl() method</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <returns></returns>
        public override string GetEditValue( System.Web.UI.Control control, Dictionary<string, ConfigurationValue> configurationValues )
        {
            if ( control is CascadingDropDownList )
            {
                var cddl = control as CascadingDropDownList;
                return cddl.SelectedValue;
            }
            return "";
        }

        public override void SetEditValue( System.Web.UI.Control control, Dictionary<string, ConfigurationValue> configurationValues, string value )
        {
            if ( control is CascadingDropDownList )
            {
                var cddl = control as CascadingDropDownList;
                cddl.SelectedValue = value;
            }
        }

        #endregion

        #region Edit Control

        /// <summary>
        /// Creates the control(s) necessary for prompting user for a new value
        /// </summary>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="id">The id.</param>
        /// <returns>
        /// The control
        /// </returns>
        public override Control EditControl( Dictionary<string, ConfigurationValue> configurationValues, string id )
        {
            var config = "";
            if ( configurationValues != null &&
  configurationValues.ContainsKey( CONFIG ) )
            {
                config = configurationValues[CONFIG].Value;
            }

            if ( config.IsNullOrWhiteSpace() )
            {
                return null;
            }

            CascadingDropDownList cascadingDropDownList = new CascadingDropDownList
            {
                ID = id
            };
            cascadingDropDownList.SetConfiguration( config );

            return cascadingDropDownList;
        }

        #endregion

    }
}
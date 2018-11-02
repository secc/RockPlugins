// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
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
using Rock.Model;
using Rock.Field;
using Rock.Reporting;
using Rock.Web.UI.Controls;
using Rock;
using Rock.Web.Cache;
using org.secc.Attributes.Controls;

namespace org.secc.Attributes.FieldTypes
{
    /// <summary>
    /// Field used to save and display a dynamic phone number
    /// </summary>
    [Serializable]
    public class DynamicPhoneNumberFieldType : Rock.Field.FieldType
    {

        #region Configuration

        /// <summary>
        /// Returns a list of the configuration keys
        /// </summary>
        /// <returns></returns>
        public override List<string> ConfigurationKeys()
        {
            return base.ConfigurationKeys();
        }

        /// <summary>
        /// Creates the HTML controls required to configure this type of field
        /// </summary>
        /// <returns></returns>
        public override List<Control> ConfigurationControls()
        {
            return base.ConfigurationControls();
        }

        /// <summary>
        /// Gets the configuration value.
        /// </summary>
        /// <param name="controls">The controls.</param>
        /// <returns></returns>
        public override Dictionary<string, ConfigurationValue> ConfigurationValues( List<Control> controls )
        {
            return base.ConfigurationValues( controls );
        }



        /// <summary>
        /// Sets the configuration value.
        /// </summary>
        /// <param name="controls"></param>
        /// <param name="configurationValues"></param>
        public override void SetConfigurationValues( List<Control> controls, Dictionary<string, ConfigurationValue> configurationValues )
        {

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
            var items = value.SplitDelimitedValues( false );
            if ( items.Length < 2 )
            {
                return "";
            }

            var dv = DefinedValueCache.Get( items[0].AsInteger() );
            if ( dv == null )
            {
                return "";
            }

            return string.Format( "{0}: {1}", dv.Value, items[1] );
        }

        /// <summary>
        /// Reads new values entered by the user for the field (as id)
        /// </summary>
        /// <param name="control">Parent control that controls were added to in the CreateEditControl() method</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <returns></returns>
        public override string GetEditValue( System.Web.UI.Control control, Dictionary<string, ConfigurationValue> configurationValues )
        {
            DynamicPhoneNumberPicker picker = control as DynamicPhoneNumberPicker;
            if ( string.IsNullOrWhiteSpace( picker.PhoneNumber ) )
            {
                return "";
            }
            var value = string.Format( "{0}|{1}", picker.PhoneNumberType, picker.PhoneNumber );
            return value;
        }

        public override void SetEditValue( System.Web.UI.Control control, Dictionary<string, ConfigurationValue> configurationValues, string value )
        {

            var picker = control as DynamicPhoneNumberPicker;

            if ( picker != null )
            {
                var items = value.SplitDelimitedValues( false );
                if ( items.Length < 2 )
                {
                    return;
                }

                var dv = DefinedValueCache.Get( items[0].AsInteger() );
                if ( dv == null )
                {
                    return;
                }
                picker.PhoneNumberType = items[0];
                picker.PhoneNumber = items[1];
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
            DynamicPhoneNumberPicker dynamicPhoneNumberPicker = new DynamicPhoneNumberPicker { ID = id };
            return dynamicPhoneNumberPicker;
        }

        #endregion

    }
}
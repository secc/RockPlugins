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
using System.ComponentModel.Composition;
using Rock.Extension;

namespace org.secc.DevLib.Components
{
    public class SettingsContainer : Container<SettingsComponent, IComponentData>
    {
        /// <summary>
        /// Singleton instance
        /// </summary>
        private static readonly Lazy<SettingsContainer> instance =
            new Lazy<SettingsContainer>( () => new SettingsContainer() );

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>
        /// The instance.
        /// </value>
        public static SettingsContainer Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Gets the component with the matching Entity Type Name.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <returns></returns>
        public static SettingsComponent GetComponent( string entityType )
        {
            return Instance.GetComponentByEntity( entityType );
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <returns></returns>
        public static string GetComponentName( string entityType )
        {
            return Instance.GetComponentNameByEntity( entityType );
        }

        /// <summary>
        /// Gets or sets the MEF components.
        /// </summary>
        /// <value>
        /// The MEF components.
        /// </value>
        [ImportMany( typeof( SettingsComponent ) )]
        protected override IEnumerable<Lazy<SettingsComponent, IComponentData>> MEFComponents { get; set; }

    }
}

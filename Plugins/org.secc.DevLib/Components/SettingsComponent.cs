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
using System.Linq;
using Rock;

namespace org.secc.DevLib.Components
{
    public abstract class SettingsComponent : Rock.Extension.Component
    {
        public abstract string Name { get; }

        public static T GetComponent<T>()
            where T : SettingsComponent
        {
            var type = typeof( T );

            var component = ( T ) SettingsContainer.Instance.Dictionary
               .Where( c => c.Value.Value.EntityType.Name == type.FullName )
               .Select( c => c.Value.Value )
               .FirstOrDefault();

            //We have to reload attributes for safety on a multi server system
            component.Attributes = null;
            component.AttributeValues = null;
            component.LoadAttributes();

            return component;
        }
    }
}

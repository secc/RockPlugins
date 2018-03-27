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
using Rock.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace org.secc.Purchasing
{
    [Serializable]
    public class PurchasingBase
    {
        protected PersonAliasService personAliasService = new PersonAliasService(new Rock.Data.RockContext());
        protected UserLoginService userLoginService = new UserLoginService(new Rock.Data.RockContext());
        protected static DefinedTypeService definedTypeService = new DefinedTypeService(new Rock.Data.RockContext());
        protected static DefinedValueService definedValueService = new DefinedValueService(new Rock.Data.RockContext());
        protected static SystemEmailService systemEmailService = new SystemEmailService(new Rock.Data.RockContext());

        
        private Person mCurrentPerson;
        [XmlIgnore]
        public Person CurrentPerson
        {
            get { return mCurrentPerson;  }
            set { mCurrentPerson = value; }
        }

        #region internal
        internal string Serialize(object toSerialize)
        {
            System.IO.StringWriter sw = new System.IO.StringWriter();
            XmlSerializer xmlSerial = new XmlSerializer(toSerialize.GetType());          
            xmlSerial.Serialize(sw, toSerialize);

            return sw.ToString();
        }

        internal static object Deserialize(string xml, Type objType)
        {
            System.IO.StringReader sr = new System.IO.StringReader(xml);
            XmlSerializer XmlSerial = new XmlSerializer(objType);
            object V = XmlSerial.Deserialize(sr);

            return V;
        }

        internal bool TypeExists(string typeName)
        {
            var t = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                     from type in assembly.GetTypes()
                     where type.Name == typeName
                     select type);
            return (t.Count() > 0);
        }
        #endregion
    }
}

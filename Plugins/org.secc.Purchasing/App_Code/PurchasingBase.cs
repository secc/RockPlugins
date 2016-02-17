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
        protected static CommunicationTemplateService communicationTemplateService = new CommunicationTemplateService(new Rock.Data.RockContext());


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

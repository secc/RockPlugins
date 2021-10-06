using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock;

namespace org.secc.Widgities.Lava
{
    public static class CustomFilters
    {
        public static string TwoPass( DotLiquid.Context context, object input )
        {
            var lava = input.ToString();
            var mergeObjects = new Dictionary<string, object>();
            if ( context.Environments.Count > 0 )
            {
                foreach ( var keyVal in context.Environments[0] )
                {
                    mergeObjects.Add( keyVal.Key, keyVal.Value );
                }
            }

            string enabledCommands = null;
            if ( context.Registers.ContainsKey( "EnabledCommands" ) )
            {
                enabledCommands = context.Registers["EnabledCommands"].ToString();
            }

            return lava.ResolveMergeFields( mergeObjects, enabledCommands );
        }
    }
}

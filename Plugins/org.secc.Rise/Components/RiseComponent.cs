using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using org.secc.xAPI.Component;
using Rock.Attribute;

namespace org.secc.Rise.Components
{
    [Export( typeof( xAPIComponent ) )]
    [ExportMetadata( "ComponentName", "Rise Component" )]
    [Description( "Rise LMS" )]

    [TextField( "API Key", "The api key for Rise.", order: 0 )]
    public class RiseComponent : xAPIComponent
    {
        public override string Name => "Rise";

        public override string Icon => "fa fa-grin";
    }
}

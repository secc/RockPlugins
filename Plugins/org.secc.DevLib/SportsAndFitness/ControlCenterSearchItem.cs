using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Rock;

namespace org.secc.DevLib.SportsAndFitness
{
    public class ControlCenterSearchItem
    {
        public string SearchTerm { get; set; }
        public bool SearchByPIN { get; set; }
        public bool SearchByPhone { get; set; }

        public ControlCenterSearchItem()
        {
            
        }

        public override string ToString()
        {
            return this.ToJson();
        }


    }
}

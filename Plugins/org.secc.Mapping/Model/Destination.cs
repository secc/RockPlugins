using System;
using System.Collections.Generic;
using System.Text;
using Rock.Model;
using Rock;
using Rock.Data;

namespace org.secc.Mapping
{
    public class Destination
    {
        [LavaInclude]
        public object Entity { get; set; }
        private string _address = "";
        public string Address
        {
            get
            {
                if ( Location != null )
                {
                    return Location.FormattedAddress;
                }
                return _address;
            }
            set
            {
                _address = value;
            }
        }
        public Location Location { get; set; }
        public double TravelDistance { get; set; }
        public double TravelDuration { get; set; }
        public bool IsCalculated { get; set; } = false;
    }
}
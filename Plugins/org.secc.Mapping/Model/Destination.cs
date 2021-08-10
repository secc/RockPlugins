using Rock;
using Rock.Data;
using Rock.Model;

namespace org.secc.Mapping
{
    public class Destination
    {
        [LavaInclude]
        public int? EntityId { get; set; }
        private string _address = "";
        public string Address
        {
            get
            {
                if ( _address.IsNullOrWhiteSpace() )
                {
                    RockContext rockContext = new RockContext();
                    LocationService locationService = new LocationService( rockContext );
                    var location = locationService.Get( LocationId ?? 0 );

                    if ( location != null )
                    {
                        _address = location.FormattedAddress;
                    }
                }
                return _address;
            }
            set
            {
                _address = value;
            }
        }
        public int? LocationId { get; set; }

        public double TravelDistance { get; set; }
        public double TravelDuration { get; set; }
        public bool IsCalculated { get; set; } = false;
    }
}
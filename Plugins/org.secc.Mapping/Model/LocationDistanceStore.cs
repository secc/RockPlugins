using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Rock.Data;
using Rock.Security;

namespace org.secc.Mapping.Model
{
    [Table( "_org_secc_Mapping_LocationDistanceStore" )]
    [DataContract]
    public class LocationDistanceStore : Model<LocationDistanceStore>, ISecured, IRockEntity
    {
        [Index]
        [DataMember]
        public string Origin { get; set; }

        [Index]
        [DataMember]
        public string Destination { get; set; }

        [DataMember]
        public double TravelDistance { get; set; }
        [DataMember]
        public double TravelDuration { get; set; }

        [DataMember]
        public string CalculatedBy { get; set; }
    }

    public partial class LocationDistanceStoreConfiguration : EntityTypeConfiguration<LocationDistanceStore>
    {
        public LocationDistanceStoreConfiguration()
        {
            this.HasEntitySetName( "LocationDistanceStore" );
        }
    }
}

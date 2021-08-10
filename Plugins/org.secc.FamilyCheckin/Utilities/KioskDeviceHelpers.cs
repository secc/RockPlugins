using System.Collections.Generic;
using System.Linq;
using org.secc.FamilyCheckin.Model;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.FamilyCheckin.Utilities
{
    public static class KioskDeviceHelpers
    {
        public static void Clear( KioskDevice kioskDevice )
        {
            var groupTypeIds = kioskDevice.KioskGroupTypes.Select( gt => gt.GroupType.Id ).ToList();
            Clear( groupTypeIds );
        }

        public static void Clear( List<int> groupTypeIds )
        {
            RockContext rockContext = new RockContext();
            KioskTypeService kioskTypeService = new KioskTypeService( rockContext );
            KioskService kioskService = new KioskService( rockContext );
            DeviceService deviceService = new DeviceService( rockContext );



            var kioskTypeIds = kioskTypeService.Queryable()
                .Where( k => k.GroupTypes.Select( gt => gt.Id ).Where( i => groupTypeIds.Contains( i ) ).Any() )
                .Select( kt => kt.Id )
                .ToList();

            var deviceNames = kioskService.Queryable()
                .Where( k => kioskTypeIds.Contains( k.KioskTypeId ?? 0 ) ).Select( k => k.Name )
                .ToList();

            var devices = deviceService.Queryable().Where( d => deviceNames.Contains( d.Name ) ).ToList();

            foreach ( var device in devices )
            {
                FlushItem( device.Id );
            }
        }

        public static void Clear()
        {
            RockContext rockContext = new RockContext();
            DeviceService deviceService = new DeviceService( rockContext );
            var devices = deviceService.Queryable().ToList();
            foreach ( var device in devices )
            {
                FlushItem( device.Id );
            }
        }


        public static void FlushItem( int id )
        {
            var key = $"{typeof( KioskDevice ).Name}:{id}";
            RockCacheManager<KioskDevice>.Instance.Remove( key );
        }
    }
}


using System.Collections.Generic;
using System.Linq;

using Rock.Data;

namespace Rock.Utility.EntityCoding
{

    public class WidgityTypeExporter : IExporter
    {
        public bool DoesPathNeedNewGuid( EntityPath path )
        {
            return ( path == "" ||
                path == "AttributeTypes" ||
                path == "AttributeTypes.AttributeQualifiers");
        }

        public ICollection<Reference> GetUserReferencesForPath( IEntity parentEntity, EntityPath path )
        {
            if ( path == "" )
            {
                return new List<Reference>
                {
                    Reference.UserDefinedReference( "CategoryId", "Category" )
                };
            }

            return null;
        }

        public bool IsPathCritical( EntityPath path )
        {
            return ( DoesPathNeedNewGuid( path ) );
        }

 
        public bool ShouldFollowPathProperty( EntityPath path )
        {
            if ( path == "CategoryId" )
            {
                return false;
            }

            if ( path.Count > 0 )
            {
                var lastComponent = path.Last();

                if ( lastComponent.Entity.TypeName == "Rock.Model.EntityType" && lastComponent.PropertyName == "EntityType" )
                {
                    return false;
                }
            }

            return true;
        }
    }
}

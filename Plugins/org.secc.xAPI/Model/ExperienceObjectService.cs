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
using System.Linq;
using org.secc.xAPI.Data;
using Rock;
using Rock.Data;
using Rock.Web.Cache;

namespace org.secc.xAPI.Model
{
    public class ExperienceObjectService : xAPIService<ExperienceObject>
    {
        public ExperienceObjectService( RockContext context ) : base( context )
        {
        }

        public ExperienceObject Get( IEntity entity )
        {
            var entityType = EntityTypeCache.Get( entity.GetType() );

            if ( entityType == null )
            {
                return null;
            }

            var xObj = Queryable()
                .Where( x => x.EntityTypeId == entityType.Id && x.ObjectId == entity.Id.ToString() )
                .FirstOrDefault();

            if ( xObj == null ) // Create 
            {
                RockContext rockContext = new RockContext();
                ExperienceObjectService experienceObjectService = new ExperienceObjectService( rockContext );
                var xObject = new ExperienceObject
                {
                    EntityTypeId = entityType.Id,
                    ObjectId = entity.Id.ToString()
                };
                experienceObjectService.Add( xObject );
                rockContext.SaveChanges();

                xObj = Get( entity );
            }

            return xObj;
        }
    }

}

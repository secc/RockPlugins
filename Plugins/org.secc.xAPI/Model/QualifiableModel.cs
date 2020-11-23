using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Attribute;
using Rock.Data;
using Rock.Security;
using Rock.Web.Cache;

namespace org.secc.xAPI.Model
{
    public class QualifiableModel<T> : Model<T>
        where T : Model<T>, ISecured, new()
    {
        public List<ExperienceQualifier> GetQualifiers()
        {
            RockContext rockContext = new RockContext();
            ExperienceQualifierService experienceQualifierService = new ExperienceQualifierService( rockContext );

            var entityTypeId = EntityTypeCache.Get( typeof( Experience ) ).Id;
            return experienceQualifierService.Queryable().Where( q => q.EntityTypeId == entityTypeId && q.ParentId == Id ).ToList();
        }

        public ExperienceQualifier GetQualifier( string qualifierKey )
        {
            return GetQualifier( qualifierKey, new RockContext() );
        }

        public ExperienceQualifier GetQualifier( string qualifierKey, RockContext rockContext )
        {
            ExperienceQualifierService experienceQualifierService = new ExperienceQualifierService( rockContext );

            var entityTypeId = EntityTypeCache.Get( typeof( Experience ) ).Id;
            return experienceQualifierService.Queryable()
                .Where( q => q.EntityTypeId == entityTypeId && q.ParentId == Id && q.Key == qualifierKey )
                .FirstOrDefault();
        }

        public ExperienceQualifier AddQualifier( string key )
        {
            return AddQualifier( key, null );
        }

        public ExperienceQualifier AddQualifier( DefinedValueCache extensionValue )
        {
            return AddQualifier( extensionValue.Value, null );
        }

        public ExperienceQualifier AddQualifier( DefinedValueCache extensionValue, string value )
        {
            return AddQualifier( extensionValue.Value, value );
        }

        public ExperienceQualifier AddQualifier( string key, string value )
        {
            RockContext rockContext = new RockContext();
            ExperienceQualifierService experienceQualifierService = new ExperienceQualifierService( rockContext );
            var qualifier = GetQualifier( key, rockContext );

            if ( qualifier == null )
            {
                qualifier = new ExperienceQualifier
                {
                    EntityTypeId = this.TypeId,
                    ParentId = this.Id,
                    Key = key,
                };
                experienceQualifierService.Add( qualifier );
            }
            qualifier.Value = value;
            rockContext.SaveChanges();

            return qualifier;
        }
    }
}

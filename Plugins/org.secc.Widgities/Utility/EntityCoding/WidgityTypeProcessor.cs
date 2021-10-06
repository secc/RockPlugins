using System;
using System.Collections.Generic;
using System.Linq;
using org.secc.Widgities.Model;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace Rock.Utility.EntityCoding.Processors
{
    /// <summary>
    /// Handle processing of Workflow Type entities.
    /// </summary>
    public class WidgityTypeProcessor : EntityProcessor<WidgityType>
    {
        /// <summary>
        /// The unique identifier for this entity processor. This is used to identify the correct
        /// processor to use when importing so we match the one used during export.
        /// </summary>
        public override Guid Identifier { get { return new Guid( "20fed760-3610-4215-8625-3744fc2e854d" ); } }

        /// <summary>
        /// Evaluate the list of child entities. This is a list of key value pairs that identify
        /// the property that the child came from as well as the child entity itself. Implementations
        /// of this method may add or remove from this list. For example, a WorkflowActionForm has
        /// it's actions encoded in a single string. This must processed to include any other
        /// objects that should exist (such as a DefinedValue for the button type).
        /// </summary>
        /// <param name="entity">The parent entity of the children.</param>
        /// <param name="children">The child entities and what properties of the parent they came from.</param>
        /// <param name="helper">The helper class for this export.</param>
        protected override void EvaluateChildEntities( WidgityType entity, List<KeyValuePair<string, IEntity>> children, EntityCoder helper )
        {
            var attributeService = new AttributeService( helper.RockContext );

            var entityIds = new List<int?> { EntityTypeCache.GetId( typeof( Widgity ) ), EntityTypeCache.GetId( typeof( WidgityItem ) ) };

            var items = attributeService
               .Queryable()
                .Where( a =>
                    entityIds.Contains( a.EntityTypeId ) &&
                    a.EntityTypeQualifierColumn.Equals( "WidgityTypeId", StringComparison.OrdinalIgnoreCase ) &&
                    a.EntityTypeQualifierValue.Equals( entity.Id.ToString() ) )
                .ToList();

            //
            // We have to special process the attributes since we modify them.
            //
            foreach ( var item in items )
            {
                children.Add( new KeyValuePair<string, IEntity>( "AttributeTypes", item ) );
            }
        }
    }
}

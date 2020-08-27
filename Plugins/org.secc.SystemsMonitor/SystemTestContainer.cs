using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using org.secc.SystemsMonitor.Model;
using Rock.Data;
using Rock.Extension;
using Rock.Web.Cache;

namespace org.secc.SystemsMonitor
{
    public class SystemTestContainer : Container<SystemTestComponent, IComponentData>
    {
        /// <summary>
        /// Singleton instance
        /// </summary>
        private static readonly Lazy<SystemTestContainer> instance =
            new Lazy<SystemTestContainer>( () => new SystemTestContainer() );

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>
        /// The instance.
        /// </value>
        public static SystemTestContainer Instance
        {
            get { return instance.Value; }
        }

        public override void Refresh()
        {
            base.Refresh();

            // Create any attributes that need to be created
            int monitorEnityTypeId = EntityTypeCache.Get( typeof( SystemTest ) ).Id;
            using ( var rockContext = new RockContext() )
            {
                foreach ( var monitor in this.Components )
                {
                    Type monitorType = monitor.Value.Value.GetType();
                    int entityTypeId = EntityTypeCache.Get( monitorType ).Id;
                    Rock.Attribute.Helper.UpdateAttributes( monitorType, monitorEnityTypeId, "EntityTypeId", entityTypeId.ToString(), rockContext );
                }
            }
        }

        /// <summary>
        /// Gets the component with the matching Entity Type Name.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <returns></returns>
        public static SystemTestComponent GetComponent( string entityType )
        {
            return Instance.GetComponentByEntity( entityType );
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <returns></returns>
        public static string GetComponentName( string entityType )
        {
            return Instance.GetComponentNameByEntity( entityType );
        }

        /// <summary>
        /// Gets or sets the MEF components.
        /// </summary>
        /// <value>
        /// The MEF components.
        /// </value>
        [ImportMany( typeof( SystemTestComponent ) )]
        protected override IEnumerable<Lazy<SystemTestComponent, IComponentData>> MEFComponents { get; set; }

    }
}

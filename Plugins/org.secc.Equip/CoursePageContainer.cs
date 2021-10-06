﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using Rock.Extension;

namespace org.secc.Equip
{
    /// <summary>
    /// MEF Container class for Video Embed Components
    /// </summary>
    public class CoursePageContainer : Container<CoursePageComponent, IComponentData>
    {
        /// <summary>
        /// Singleton instance
        /// </summary>
        private static readonly Lazy<CoursePageContainer> instance =
            new Lazy<CoursePageContainer>( () => new CoursePageContainer() );

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>
        /// The instance.
        /// </value>
        public static CoursePageContainer Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Gets the component with the matching Entity Type Name.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <returns></returns>
        public static CoursePageComponent GetComponent( string entityType )
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
        [ImportMany( typeof( CoursePageComponent) )]
        protected override IEnumerable<Lazy<CoursePageComponent, IComponentData>> MEFComponents { get; set; }

    }
}
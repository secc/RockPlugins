// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rock.Plugin;

namespace org.secc.OAuth.Tests
{
    /// <summary>
    /// Finds the plugin migrations in org.secc.OAuth the way Rock's RunPluginMigrations does at startup: every
    /// non-abstract type deriving from Rock.Plugin.Migration with a MigrationNumber attribute, compared with the Rock
    /// version from Rock.VersionInfo.
    /// </summary>
    public static class OAuthPluginMigrations
    {
        public static Assembly Assembly => typeof( org.secc.OAuth.Model.Client ).Assembly;

        public static Version RockVersion => new Version( Rock.VersionInfo.VersionInfo.GetRockProductVersionNumber() );

        /// <summary>
        /// Every migration type in the plugin assembly with its MigrationNumber attribute.
        /// </summary>
        public static List<KeyValuePair<Type, MigrationNumberAttribute>> All()
        {
            Type[] types;
            try
            {
                types = Assembly.GetTypes();
            }
            catch ( ReflectionTypeLoadException ex )
            {
                types = ex.Types.Where( t => t != null ).ToArray();
            }

            return types
                .Where( t => !t.IsAbstract && typeof( Migration ).IsAssignableFrom( t ) )
                .Select( t => new KeyValuePair<Type, MigrationNumberAttribute>( t, t.GetCustomAttribute<MigrationNumberAttribute>() ) )
                .Where( m => m.Value != null )
                .ToList();
        }

        /// <summary>
        /// The migration Rock would run as the given number on this Rock version.
        /// </summary>
        public static Type Find( int number )
        {
            var matches = All().Where( m => m.Value.Number == number ).ToList();
            if ( matches.Count == 0 )
            {
                throw new InvalidOperationException( $"Migration {number} not found in org.secc.OAuth." );
            }

            if ( matches.Count > 1 )
            {
                throw new InvalidOperationException( $"org.secc.OAuth has more than one migration numbered {number}." );
            }

            if ( new Version( matches[0].Value.MinimumRockVersion ).CompareTo( RockVersion ) > 0 )
            {
                throw new InvalidOperationException( $"Migration {number} would be skipped on Rock {RockVersion}." );
            }

            return matches[0].Key;
        }
    }
}

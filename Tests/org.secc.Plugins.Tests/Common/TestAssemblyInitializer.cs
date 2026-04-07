using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rock.Configuration;

namespace org.secc.Plugins.Tests.Common
{
    /// <summary>
    /// Runs once before any test in this assembly executes.
    /// Initializes <see cref="RockApp.Current"/> with stub settings so that
    /// <see cref="Rock.Data.RockContext"/> can be constructed without a NullReferenceException,
    /// and pre-seeds the campus cache with an empty list so no actual database query is made.
    /// </summary>
    [TestClass]
    public sealed class TestAssemblyInitializer
    {
        [AssemblyInitialize]
        public static void AssemblyInitialize( TestContext context )
        {
            InitializeRockApp();
            SeedEmptyCampusCache();
            SeedGlobalAttributesCache();
        }

        /// <summary>
        /// Creates a minimal <see cref="RockApp"/> instance (using reflection because its
        /// constructor and the <c>Current</c> setter are both <c>internal</c>) and assigns
        /// it to <see cref="RockApp.Current"/>.
        /// </summary>
        internal static void InitializeRockApp()
        {
            if ( RockApp.Current != null )
            {
                return;
            }

            // Build a minimal IServiceProvider that satisfies the services RockApp requires.
            var serviceProvider = new SimpleServiceProvider();
            serviceProvider.AddSingleton<IInitializationSettings>( new TestInitializationSettings() );
            serviceProvider.AddSingleton<IHostingSettings>( new TestHostingSettings() );

            // RockApp has an internal constructor: RockApp(IServiceProvider).
            var rockAppType = typeof( RockApp );
            var ctor = rockAppType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: new[] { typeof( IServiceProvider ) },
                modifiers: null );

            if ( ctor == null )
            {
                throw new InvalidOperationException(
                    "Could not find the internal RockApp(IServiceProvider) constructor. " +
                    "The Rock assembly may have changed." );
            }

            var rockApp = ( RockApp ) ctor.Invoke( new object[] { serviceProvider } );

            // RockApp.Current has an internal setter.
            var currentProperty = rockAppType.GetProperty(
                "Current",
                BindingFlags.Public | BindingFlags.Static );

            var setter = currentProperty?.GetSetMethod( nonPublic: true );

            if ( setter == null )
            {
                throw new InvalidOperationException(
                    "Could not find the internal setter for RockApp.Current. " +
                    "The Rock assembly may have changed." );
            }

            setter.Invoke( null, new object[] { rockApp } );
        }

        /// <summary>
        /// Pre-seeds the <see cref="Rock.Web.Cache.CampusCache"/> "all keys" entry in the
        /// in-process cache with an empty list.  This prevents <c>CampusCache.All()</c> from
        /// falling through to its database-backed factory function during unit tests.
        /// </summary>
        internal static void SeedEmptyCampusCache()
        {
            try
            {
                // ItemCache<CampusCache>.AddKeys(Func<List<string>>) is internal protected static.
                // It writes the supplied list into the in-process cache under the AllKey region so
                // that subsequent calls to GetOrAddKeys() find a value and skip the DB factory.
                var itemCacheType = typeof( Rock.Web.Cache.ItemCache<> )
                    .MakeGenericType( typeof( Rock.Web.Cache.CampusCache ) );

                var addKeysMethod = itemCacheType.GetMethod(
                    "AddKeys",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy );

                if ( addKeysMethod == null )
                {
                    return;
                }

                Func<List<string>> emptyFactory = () => new List<string>();
                addKeysMethod.Invoke( null, new object[] { emptyFactory } );
            }
            catch
            {
                // Not fatal — if the cache system isn't available in this test run,
                // individual tests may still pass as long as no code path calls CampusCache.All().
            }
        }

        // -------------------------------------------------------------------------
        // Minimal helpers — no external DI package required
        // -------------------------------------------------------------------------

        /// <summary>
        /// Seeds <see cref="Rock.Web.Cache.GlobalAttributesCache"/> with an empty
        /// <c>_attributeIds</c> list so that calls to <c>Attributes.get</c> skip the
        /// database load entirely.  The Lava engine reads
        /// <c>DefaultEnabledLavaCommands</c> from this cache; with an empty list that
        /// key is not found and an empty string is returned, which is safe for unit
        /// tests whose templates contain no Lava commands.
        /// </summary>
        internal static void SeedGlobalAttributesCache()
        {
            try
            {
                // GetOrAddExisting returns (and caches) a fresh GlobalAttributesCache
                // instance whose _attributeIds is null.  Set it to an empty non-null
                // list so the double-checked lock skips the RockContext call.
                var instance = Rock.Web.Cache.GlobalAttributesCache.Get();

                var field = instance.GetType().GetField(
                    "_attributeIds",
                    BindingFlags.NonPublic | BindingFlags.Instance );

                field?.SetValue( instance, new System.Collections.Generic.List<int>() );
            }
            catch
            {
                // Not fatal — ResolveMergeFields will still work for templates that
                // don't rely on globally-enabled Lava commands.
            }
        }

        /// <summary>
        /// A very small <see cref="IServiceProvider"/> backed by a type-keyed dictionary.
        /// Sufficient for wiring up <see cref="RockApp"/> in unit tests.
        /// </summary>
        private sealed class SimpleServiceProvider : IServiceProvider
        {
            private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

            public void AddSingleton<TService>( TService instance )
            {
                _services[typeof( TService )] = instance;
            }

            public object GetService( Type serviceType )
            {
                _services.TryGetValue( serviceType, out var service );
                return service;
            }
        }

        /// <summary>
        /// Stub implementation of <see cref="IHostingSettings"/> for unit tests.
        /// </summary>
        private sealed class TestHostingSettings : IHostingSettings
        {
            public DateTime ApplicationStartDateTime => DateTime.Now;
            public string DotNetVersion => System.Runtime.InteropServices.RuntimeEnvironment.GetSystemVersion();
            public string WebRootPath => AppDomain.CurrentDomain.BaseDirectory;
            public string VirtualRootPath => "/";
            public string MachineName => Environment.MachineName;
            public string NodeName => Environment.MachineName;
        }
    }
}

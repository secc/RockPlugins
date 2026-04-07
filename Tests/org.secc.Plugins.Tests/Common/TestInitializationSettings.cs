using System.Collections.Generic;
using Rock.Configuration;

namespace org.secc.Plugins.Tests.Common
{
    /// <summary>
    /// Stub implementation of <see cref="IInitializationSettings"/> for use in unit tests.
    /// All connection strings are intentionally empty so no real database connection is made.
    /// </summary>
    internal sealed class TestInitializationSettings : IInitializationSettings
    {
        public bool IsRunScheduledJobsEnabled => false;

        public string OrganizationTimeZone => "Mountain Standard Time";

        public string PasswordKey => string.Empty;

        public IReadOnlyList<string> OldPasswordKeys => new List<string>();

        public string DataEncryptionKey => string.Empty;

        public IReadOnlyList<string> OldDataEncryptionKeys => new List<string>();

        public string RockStoreUrl => string.Empty;

        public bool IsDuplicateGroupMemberRoleAllowed => false;

        public bool IsCacheStatisticsEnabled => false;

        public string ObservabilityServiceName => string.Empty;

        public string AzureSignalREndpoint => string.Empty;

        public string AzureSignalRAccessKey => string.Empty;

        public string SparkApiUrl => string.Empty;

        public string NodeName => string.Empty;

        /// <summary>
        /// Empty connection string — no real database connection will be attempted
        /// as long as no EF queries are executed.
        /// </summary>
        public string ConnectionString => string.Empty;

        public string ReadOnlyConnectionString => string.Empty;

        public string AnalyticsConnectionString => string.Empty;
    }
}

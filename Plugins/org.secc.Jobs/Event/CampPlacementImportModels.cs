// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Collections.Generic;

namespace org.secc.Jobs.Event
{
    /// <summary>
    /// Status values for an import run record.
    /// </summary>
    public enum ImportRunStatus
    {
        Queued = 0,
        Running = 1,
        Completed = 2,
        Failed = 3
    }

    /// <summary>
    /// All the information the background runner needs to process an import.
    /// This is serialized to JSON and stored in the run record so the
    /// runner has everything it needs without depending on the web request.
    /// </summary>
    [Serializable]
    public class CampPlacementImportRequest
    {
        public int? RegistrationInstanceId { get; set; }
        public int? BinaryFileId { get; set; }
        public string FirstNameCol { get; set; }
        public string LastNameCol { get; set; }
        public int BatchSize { get; set; }
        public int DefaultGroupMemberStatusValue { get; set; }
        public List<CampPlacementMappingData> Mappings { get; set; }
    }

    /// <summary>
    /// Serializable form of a column-to-group mapping.
    /// (The private PlacementMapping class on the block cannot be referenced from the runner.)
    /// </summary>
    [Serializable]
    public class CampPlacementMappingData
    {
        public string CsvColumnName { get; set; }
        public int ParentGroupId { get; set; }
    }

    /// <summary>
    /// Lightweight read-only projection of _org_secc_CampPlacementImportRun
    /// used by the polling timer on the block.
    /// </summary>
    public class CampPlacementImportRunRecord
    {
        public int Id { get; set; }
        public int Status { get; set; }
        public string StatusMessage { get; set; }
        public int PercentComplete { get; set; }
        public int ProcessedRows { get; set; }
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int SkippedCount { get; set; }
        public int ErrorCount { get; set; }
        public string ResultHtml { get; set; }
    }
}
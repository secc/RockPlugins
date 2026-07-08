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

namespace org.secc.LinkList.ViewModels
{
    /// <summary>
    /// ROCK-7164: analytics for one Link List over a date range, returned by
    /// the Detail block's GetAnalytics action for the editor's Analytics panel.
    /// </summary>
    public class LinkListAnalyticsBag
    {
        /// <summary>The (clamped) range actually used: 30, 90, or 365.</summary>
        public int Days { get; set; }

        public int TotalViews { get; set; }

        public int TotalClicks { get; set; }

        /// <summary>Views per day, gap-filled (every day in range), ascending.</summary>
        public List<AnalyticsPointBag> ViewsByDay { get; set; } = new List<AnalyticsPointBag>();

        /// <summary>Clicks per day, gap-filled (every day in range), ascending.</summary>
        public List<AnalyticsPointBag> ClicksByDay { get; set; } = new List<AnalyticsPointBag>();

        /// <summary>Per-link click counts, descending by clicks.</summary>
        public List<LinkClickCountBag> Links { get; set; } = new List<LinkClickCountBag>();
    }

    /// <summary>One day's count in a time series.</summary>
    public class AnalyticsPointBag
    {
        public DateTime Date { get; set; }

        public int Count { get; set; }
    }

    /// <summary>Click count for a single link row.</summary>
    public class LinkClickCountBag
    {
        /// <summary>AttributeMatrixItem guid; null when the row was deleted.</summary>
        public string MatrixItemGuid { get; set; }

        /// <summary>Current link text, else the latest recorded InteractionSummary.</summary>
        public string Text { get; set; }

        /// <summary>Current link URL, else the latest recorded InteractionData.</summary>
        public string Url { get; set; }

        public int Clicks { get; set; }

        /// <summary>True when the link row no longer exists on the list.</summary>
        public bool IsDeleted { get; set; }
    }
}

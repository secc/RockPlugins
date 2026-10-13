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
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace org.secc.RegistrationCart.Domain.Entities;

public class Campus
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = "";

    public int? RockCampusId { get; set; }

    public ICollection<OfferingVariant> Variants { get; set; } = new List<OfferingVariant>();
}

/// <summary>
/// Something a person can register for (a camp, retreat, class, event, ...).
/// Add-on groups live here (not on a variant) so they are defined once per offering.
/// A group may optionally be campus-scoped (CampusId non-null) for location-specific extras.
/// </summary>
public class Offering
{
    public int Id { get; set; }
    public OfferingType Type { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = "";

    [MaxLength(2000)]
    public string Description { get; set; } = "";

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<OfferingVariant> Variants { get; set; } = new List<OfferingVariant>();

    /// <summary>
    /// Add-on groups defined once at the offering level. Filter by CampusId at
    /// display/checkout time to surface only groups relevant to the chosen variant's campus.
    /// </summary>
    public ICollection<AddOnGroup> AddOnGroups { get; set; } = new List<AddOnGroup>();
}

/// <summary>
/// One priced variant per campus/session/grade-band combination.
/// "Elementary Adventure @ North" = EligibleGradeBands=Elementary, CampusId=NorthId.
/// </summary>
public class OfferingVariant
{
    public int Id { get; set; }
    public int OfferingId { get; set; }
    public Offering Offering { get; set; } = null!;

    public int? CampusId { get; set; }
    public Campus? Campus { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    [Range(0.0, 99999.99)]
    public decimal BasePrice { get; set; }

    public DateTime? SessionStart { get; set; }
    public DateTime? SessionEnd { get; set; }

    public GradeBand EligibleGradeBands { get; set; } = GradeBand.All;

    /// <summary>Max registrations allowed. Null = no cap.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1.")]
    public int? Capacity { get; set; }

    public int? RockRegistrationInstanceId { get; set; }
}

/// <summary>
/// A group of add-on options attached to an offering. CampusId null = shown for all campuses.
/// </summary>
public class AddOnGroup
{
    public int Id { get; set; }

    public int OfferingId { get; set; }
    public Offering Offering { get; set; } = null!;

    public int? CampusId { get; set; }
    public Campus? Campus { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    public bool Required { get; set; }
    public SelectionMode SelectionMode { get; set; } = SelectionMode.Multi;

    [Range(0, 999)]
    public int SortOrder { get; set; }

    public ICollection<AddOnItem> Items { get; set; } = new List<AddOnItem>();
}

/// <summary>
/// A single selectable extra. EligibleGradeBands is independent of the variant's bands.
/// </summary>
public class AddOnItem
{
    public int Id { get; set; }
    public int AddOnGroupId { get; set; }
    public AddOnGroup AddOnGroup { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    [Range(0.0, 9999.99)]
    public decimal Price { get; set; }

    public GradeBand EligibleGradeBands { get; set; } = GradeBand.All;

    public int? RockFeeId { get; set; }
    public int? RockFeeItemId { get; set; }
}

public class DiscountCode
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = "";

    public DiscountType Type { get; set; }

    [Range(0.0, 100000.0, ErrorMessage = "Value must be a positive number.")]
    public decimal Value { get; set; }

    public DiscountScope Scope { get; set; }

    public int? OfferingId { get; set; }

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Date-only comparison: the time component of <paramref name="date"/> is ignored.</summary>
    public bool IsValidOn(DateTime date)
    {
        var day = date.Date;
        return IsActive
            && (ValidFrom is null || day >= ValidFrom.Value.Date)
            && (ValidTo is null || day <= ValidTo.Value.Date);
    }
}

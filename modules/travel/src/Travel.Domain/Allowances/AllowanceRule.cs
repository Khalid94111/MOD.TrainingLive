using System;
using System.Collections.Generic;
using System.Linq;
using Travel.TravelTypes;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Travel.Allowances;

public class AllowanceRule : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public AllowanceType AllowanceType { get; private set; }
    public Guid? TravelTypeDefinitionId { get; private set; }
    public AllowanceCategory? Category { get; private set; }
    public bool? AppliesWhenAccommodationIncluded { get; private set; }
    public decimal? AccommodationMultiplier { get; private set; }
    public int Priority { get; private set; }
    public bool IsActive { get; private set; }
    public ICollection<AllowanceRuleSegment> Segments { get; private set; } = new List<AllowanceRuleSegment>();

    protected AllowanceRule()
    {
    }

    public AllowanceRule(
        Guid id,
        string name,
        AllowanceType allowanceType,
        Guid? travelTypeDefinitionId,
        AllowanceCategory? category,
        bool? appliesWhenAccommodationIncluded,
        decimal? accommodationMultiplier,
        int priority,
        bool isActive) : base(id)
    {
        SetName(name);
        AllowanceType = allowanceType;
        TravelTypeDefinitionId = travelTypeDefinitionId;
        Category = category;
        AppliesWhenAccommodationIncluded = appliesWhenAccommodationIncluded;
        AccommodationMultiplier = accommodationMultiplier;
        Priority = priority;
        IsActive = isActive;
    }

    public void Update(
        string name,
        AllowanceType allowanceType,
        Guid? travelTypeDefinitionId,
        AllowanceCategory? category,
        bool? appliesWhenAccommodationIncluded,
        decimal? accommodationMultiplier,
        int priority,
        bool isActive)
    {
        SetName(name);
        AllowanceType = allowanceType;
        TravelTypeDefinitionId = travelTypeDefinitionId;
        Category = category;
        AppliesWhenAccommodationIncluded = appliesWhenAccommodationIncluded;
        AccommodationMultiplier = accommodationMultiplier;
        Priority = priority;
        IsActive = isActive;
    }

    public void AddSegment(int fromDay, int toDay, decimal percentage, int? appliesWhenTotalDaysFrom, int? appliesWhenTotalDaysTo)
    {
        if (fromDay < 1) throw new BusinessException(TravelErrorCodes.InvalidAllowanceSegment);
        if (toDay < fromDay) throw new BusinessException(TravelErrorCodes.InvalidAllowanceSegment);
        if (percentage < 0 || percentage > 100) throw new BusinessException(TravelErrorCodes.InvalidAllowanceSegment);
        if (appliesWhenTotalDaysFrom.HasValue && appliesWhenTotalDaysFrom.Value < 1) throw new BusinessException(TravelErrorCodes.InvalidAllowanceSegment);
        if (appliesWhenTotalDaysTo.HasValue && appliesWhenTotalDaysTo.Value < 1) throw new BusinessException(TravelErrorCodes.InvalidAllowanceSegment);
        if (appliesWhenTotalDaysFrom.HasValue && appliesWhenTotalDaysTo.HasValue && appliesWhenTotalDaysTo.Value < appliesWhenTotalDaysFrom.Value) throw new BusinessException(TravelErrorCodes.InvalidAllowanceSegment);

        Segments.Add(new AllowanceRuleSegment(Guid.NewGuid(), Id, fromDay, toDay, percentage, appliesWhenTotalDaysFrom, appliesWhenTotalDaysTo));
    }

    public void ClearSegments()
    {
        Segments.Clear();
    }

    public void ReplaceSegments(List<(int FromDay, int ToDay, decimal Percentage, int? AppliesWhenTotalDaysFrom, int? AppliesWhenTotalDaysTo)> segments)
    {
        ClearSegments();
        foreach (var segment in segments.OrderBy(s => s.FromDay))
        {
            AddSegment(segment.FromDay, segment.ToDay, segment.Percentage, segment.AppliesWhenTotalDaysFrom, segment.AppliesWhenTotalDaysTo);
        }
    }

    private void SetName(string name)
    {
        Check.NotNullOrWhiteSpace(name, nameof(name));
        if (name.Length > 256)
            throw new BusinessException(TravelErrorCodes.NameTooLong);
        Name = name;
    }

}

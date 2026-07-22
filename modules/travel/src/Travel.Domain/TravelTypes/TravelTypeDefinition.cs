using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Travel.TravelTypes;

public class TravelTypeDefinition : FullAuditedAggregateRoot<Guid>
{
    public int Code { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    protected TravelTypeDefinition()
    {
    }

    public TravelTypeDefinition(Guid id, int code, string name, bool isActive = true) : base(id)
    {
        SetCode(code);
        SetName(name);
        IsActive = isActive;
    }

    public void Update(int code, string name, bool isActive)
    {
        SetCode(code);
        SetName(name);
        IsActive = isActive;
    }

    private void SetCode(int code)
    {
        if (code <= 0)
        {
            throw new BusinessException(TravelErrorCodes.InvalidTravelTypeCode);
        }

        Code = code;
    }

    private void SetName(string name)
    {
        Check.NotNullOrWhiteSpace(name, nameof(name));
        if (name.Length > 128)
        {
            throw new BusinessException(TravelErrorCodes.NameTooLong);
        }

        Name = name;
    }
}

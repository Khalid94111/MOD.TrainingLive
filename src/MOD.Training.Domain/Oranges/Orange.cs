using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using JetBrains.Annotations;
using Volo.Abp;

namespace MOD.Training.Oranges;

public abstract class OrangeBase : FullAuditedAggregateRoot<Guid>
{
    [NotNull]
    public virtual string ArabicName { get; set; }

    protected OrangeBase()
    {
    }

    public OrangeBase(Guid id, string arabicName)
    {
        Id = id;
        Check.NotNull(arabicName, nameof(arabicName));
        ArabicName = arabicName;
    }
}
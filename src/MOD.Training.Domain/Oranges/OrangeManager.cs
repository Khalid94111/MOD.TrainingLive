using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Data;

namespace MOD.Training.Oranges;

public abstract class OrangeManagerBase : DomainService
{
    protected IOrangeRepository _orangeRepository;

    public OrangeManagerBase(IOrangeRepository orangeRepository)
    {
        _orangeRepository = orangeRepository;
    }

    public virtual async Task<Orange> CreateAsync(string arabicName)
    {
        Check.NotNullOrWhiteSpace(arabicName, nameof(arabicName));
        var orange = new Orange(GuidGenerator.Create(), arabicName);
        return await _orangeRepository.InsertAsync(orange);
    }

    public virtual async Task<Orange> UpdateAsync(Guid id, string arabicName, [CanBeNull] string? concurrencyStamp = null)
    {
        Check.NotNullOrWhiteSpace(arabicName, nameof(arabicName));
        var orange = await _orangeRepository.GetAsync(id);
        orange.ArabicName = arabicName;
        orange.SetConcurrencyStampIfNotNull(concurrencyStamp);
        return await _orangeRepository.UpdateAsync(orange);
    }
}
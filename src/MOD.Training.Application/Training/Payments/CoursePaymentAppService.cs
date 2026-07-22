using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.CasualCourses;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance;
using MOD.Training.Training.Managers;
using MOD.Training.Training.Payments.Dtos;
using MOD.Training.Training.Payments.Storage;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.Plans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.BlobStoring;
using Volo.Abp.Content;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Uow;

namespace MOD.Training.Training.Payments;

/// <summary>
/// Course-level invoice CRUD + invoice file upload/download + atomic Confirm.
///
/// Confirm flips the invoice status atomically. Casual-course travel expenses are
/// executed by the Travel module and do not generate training budget reallocations.
///
/// File storage: the typed <see cref="CourseInvoiceContainer"/> is wired to BlobStoring.FileSystem
/// in <c>TrainingDomainModule.ConfigureServicesAsync</c>, with BasePath driven by the
/// <c>Gtms.Files.Path</c> ABP setting. Per-tenant subfolders are auto-created by ABP.
/// </summary>
[Authorize(TrainingPaymentsPermissions.CoursePayments.Default)]
public class CoursePaymentAppService(
    IRepository<CoursePayment, Guid> repository,
    IRepository<CasualCourse, Guid> casualCourseRepo,
    IRepository<CourseSession, Guid> sessionRepo,
    IRepository<Course, Guid> courseRepo,
    IRepository<TrainingProvider, Guid> providerRepo,
    IRepository<PriceQuote, Guid> priceQuoteRepo,
    IRepository<IdentityUser, Guid> userRepo,
    IBlobContainer<CourseInvoiceContainer> invoiceBlobs,
    CourseNameResolver courseNameResolver,
    CoursePaymentToDtoMapper toDtoMapper,
    CreateUpdateCoursePaymentToEntityMapper toEntityMapper)
    : ApplicationService, ICoursePaymentAppService
{
    private const long MaxInvoiceBytes = 25L * 1024 * 1024;     // 25 MB — government invoices
    private const string PdfContentType = "application/pdf";

    public async Task<CoursePaymentDto> GetAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        return await BuildDtoAsync(entity);
    }

    public async Task<PagedResultDto<CoursePaymentDto>> GetListAsync(CoursePaymentGetListInput input)
    {
        var queryable = await repository.GetQueryableAsync();

        if (input.CasualCourseId.HasValue)
            queryable = queryable.Where(x => x.CasualCourseId == input.CasualCourseId.Value);
        if (input.SessionId.HasValue)
            queryable = queryable.Where(x => x.SessionId == input.SessionId.Value);
        if (input.TrainingProviderId.HasValue)
            queryable = queryable.Where(x => x.TrainingProviderId == input.TrainingProviderId.Value);
        if (input.Status.HasValue)
            queryable = queryable.Where(x => x.Status == input.Status.Value);
        if (input.InvoiceDateFrom.HasValue)
            queryable = queryable.Where(x => x.InvoiceDate >= input.InvoiceDateFrom.Value);
        if (input.InvoiceDateTo.HasValue)
            queryable = queryable.Where(x => x.InvoiceDate <= input.InvoiceDateTo.Value);

        var totalCount = await AsyncExecuter.CountAsync(queryable);
        queryable = queryable.OrderByDescending(x => x.CreationTime).PageBy(input);
        var entities = await AsyncExecuter.ToListAsync(queryable);

        var dtos = await BuildDtosAsync(entities);
        return new PagedResultDto<CoursePaymentDto>(totalCount, dtos);
    }

    [Authorize(TrainingPaymentsPermissions.CoursePayments.Create)]
    public async Task<CoursePaymentDto> CreateAsync(CreateUpdateCoursePaymentDto input)
    {
        ValidatePolymorphicParent(input);
        ValidateAmounts(input);
        await ValidateParentReadyAsync(input.CasualCourseId, input.SessionId);
        await ValidateNoDuplicateAsync(input.CasualCourseId, input.SessionId);
        await ValidateProviderMatchAsync(
            input.CasualCourseId,
            input.SessionId,
            input.TrainingProviderId);

        var entity = toEntityMapper.Map(input);
        entity.Status = PaymentStatus.Draft;

        await repository.InsertAsync(entity, autoSave: true);
        return await BuildDtoAsync(entity);
    }

    [Authorize(TrainingPaymentsPermissions.CoursePayments.Update)]
    public async Task<CoursePaymentDto> UpdateAsync(Guid id, CreateUpdateCoursePaymentDto input)
    {
        var entity = await repository.GetAsync(id);
        EnsureDraft(entity);
        ValidateAmounts(input);
        await ValidateParentReadyAsync(entity.CasualCourseId, entity.SessionId);
        await ValidateProviderMatchAsync(
            entity.CasualCourseId,
            entity.SessionId,
            input.TrainingProviderId);

        // Polymorphic parent + lifecycle + invoice refs are immutable on Update.
        var sessionOriginal = entity.SessionId;
        var casualOriginal = entity.CasualCourseId;
        var statusOriginal = entity.Status;
        var blobOriginal = entity.InvoiceBlobName;
        var fileNameOriginal = entity.InvoiceOriginalFileName;

        toEntityMapper.Map(input, entity);

        entity.SessionId = sessionOriginal;
        entity.CasualCourseId = casualOriginal;
        entity.Status = statusOriginal;
        entity.InvoiceBlobName = blobOriginal;
        entity.InvoiceOriginalFileName = fileNameOriginal;

        await repository.UpdateAsync(entity, autoSave: true);
        return await BuildDtoAsync(entity);
    }

    [Authorize(TrainingPaymentsPermissions.CoursePayments.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        EnsureDraft(entity);

        // Best-effort blob cleanup; payment row is the source of truth.
        if (!string.IsNullOrEmpty(entity.InvoiceBlobName))
            await invoiceBlobs.DeleteAsync(entity.InvoiceBlobName);

        await repository.DeleteAsync(id);
    }

    [Authorize(TrainingPaymentsPermissions.CoursePayments.UploadInvoice)]
    public async Task<CoursePaymentDto> UploadInvoiceAsync(Guid id, IRemoteStreamContent file)
    {
        var entity = await repository.GetAsync(id);
        EnsureDraft(entity);

        if (!string.Equals(file.ContentType, PdfContentType, StringComparison.OrdinalIgnoreCase))
            throw new BusinessException("Training:CoursePayment:InvalidFileType");

        var contentLength = file.ContentLength ?? -1;
        if (contentLength > MaxInvoiceBytes)
            throw new BusinessException("Training:CoursePayment:FileTooLarge");

        // Replace any prior blob so we don't accumulate orphaned uploads.
        if (!string.IsNullOrEmpty(entity.InvoiceBlobName))
            await invoiceBlobs.DeleteAsync(entity.InvoiceBlobName);

        var blobName = $"invoice-{entity.Id:N}-{Guid.NewGuid():N}.pdf";
        await using (var stream = file.GetStream())
        {
            // ContentLength may be unset by some clients; enforce on the actual stream too.
            await invoiceBlobs.SaveAsync(blobName, stream, overrideExisting: true);
        }

        entity.InvoiceBlobName = blobName;
        entity.InvoiceOriginalFileName = file.FileName;

        await repository.UpdateAsync(entity, autoSave: true);
        return await BuildDtoAsync(entity);
    }

    [Authorize(TrainingPaymentsPermissions.CoursePayments.DownloadInvoice)]
    public async Task<IRemoteStreamContent?> DownloadInvoiceAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);
        if (string.IsNullOrEmpty(entity.InvoiceBlobName)) return null;

        var stream = await invoiceBlobs.GetAsync(entity.InvoiceBlobName);
        return new RemoteStreamContent(
            stream,
            fileName: entity.InvoiceOriginalFileName ?? entity.InvoiceBlobName,
            contentType: PdfContentType);
    }

    /// <summary>
    /// Atomically: flips Status → Confirmed AND generates BudgetReallocations for casual courses
    /// (Scenario 2/3). Wrapped in a transactional UnitOfWork — if the generator throws, the
    /// status update rolls back. Idempotency is guarded by the generator's AnyAsync check.
    /// </summary>
    [Authorize(TrainingPaymentsPermissions.CoursePayments.Confirm)]
    [UnitOfWork(isTransactional: true)]
    public async Task<CoursePaymentConfirmResultDto> ConfirmAsync(Guid id)
    {
        var entity = await repository.GetAsync(id);

        if (entity.Status != PaymentStatus.Draft)
            throw new BusinessException("Training:CoursePayment:InvalidStatusTransition");
        if (string.IsNullOrEmpty(entity.InvoiceBlobName))
            throw new BusinessException("Training:CoursePayment:InvoiceRequired");

        await ValidateParentReadyAsync(entity.CasualCourseId, entity.SessionId);
        await ValidateProviderMatchAsync(
            entity.CasualCourseId,
            entity.SessionId,
            entity.TrainingProviderId);

        entity.Status = PaymentStatus.Confirmed;
        entity.ConfirmedAt = Clock.Now;
        entity.ConfirmedById = CurrentUser.Id;

        await repository.UpdateAsync(entity, autoSave: true);

        var dto = await BuildDtoAsync(entity);
        return new CoursePaymentConfirmResultDto
        {
            Payment = dto,
            GeneratedReallocationsCount = 0
        };
    }

    // ── Validation helpers ──────────────────────────────────────────────

    private static void ValidatePolymorphicParent(CreateUpdateCoursePaymentDto input)
    {
        if (input.CasualCourseId.HasValue == input.SessionId.HasValue)
            throw new BusinessException("Training:CoursePayment:OnePolymorphicParentRequired");
    }

    private static void ValidateAmounts(CreateUpdateCoursePaymentDto input)
    {
        if (input.InvoiceAmountOMR <= 0)
            throw new BusinessException("Training:CoursePayment:NegativeAmount");
    }

    private async Task ValidateNoDuplicateAsync(Guid? casualCourseId, Guid? sessionId)
    {
        var exists = casualCourseId.HasValue
            ? await repository.AnyAsync(x => x.CasualCourseId == casualCourseId.Value)
            : await repository.AnyAsync(x => x.SessionId == sessionId!.Value);

        if (exists)
            throw new BusinessException("Training:CoursePayment:DuplicateForParent");
    }

    private async Task ValidateParentReadyAsync(Guid? casualCourseId, Guid? sessionId)
    {
        if (casualCourseId.HasValue)
        {
            var course = await casualCourseRepo.FindAsync(casualCourseId.Value)
                ?? throw new EntityNotFoundException(typeof(CasualCourse), casualCourseId.Value);
            if (course.CourseType == CourseType.Internal)
                throw new BusinessException("Training:CoursePayment:InternalCasualCourseNotAllowed");
            if (course.Status != CasualCourseStatus.THApproved)
                throw new BusinessException("Training:CoursePayment:CourseNotReady");
            if (!course.SelectedPriceQuoteId.HasValue)
                throw new BusinessException("Training:CoursePayment:CourseNotReady");
        }
        else
        {
            var session = await sessionRepo.FindAsync(sessionId!.Value)
                ?? throw new EntityNotFoundException(typeof(CourseSession), sessionId.Value);
            if (session.CourseType == CourseType.Internal)
                throw new BusinessException("Training:CoursePayment:InternalSessionNotAllowed");
            if (session.Status == SessionStatus.Planned || session.Status == SessionStatus.Cancelled)
                throw new BusinessException("Training:CoursePayment:CourseNotReady");
            if (!session.SelectedPriceQuoteId.HasValue)
                throw new BusinessException("Training:CoursePayment:CourseNotReady");
        }
    }

    /// <summary>
    /// The payment provider must always equal the provider of the selected quote.
    /// </summary>
    private async Task ValidateProviderMatchAsync(
        Guid? casualCourseId,
        Guid? sessionId,
        Guid trainingProviderId)
    {
        Guid selectedQuoteId;
        if (casualCourseId.HasValue)
        {
            var course = await casualCourseRepo.GetAsync(casualCourseId.Value);
            selectedQuoteId = course.SelectedPriceQuoteId
                ?? throw new BusinessException("Training:CoursePayment:CourseNotReady");
        }
        else
        {
            var session = await sessionRepo.GetAsync(sessionId!.Value);
            selectedQuoteId = session.SelectedPriceQuoteId
                ?? throw new BusinessException("Training:CoursePayment:CourseNotReady");
        }

        var quote = await priceQuoteRepo.FindAsync(selectedQuoteId)
            ?? throw new BusinessException("Training:CoursePayment:CourseNotReady");

        if (trainingProviderId != quote.ProviderId)
            throw new BusinessException("Training:CoursePayment:ProviderMismatch");
    }

    private static void EnsureDraft(CoursePayment entity)
    {
        if (entity.Status != PaymentStatus.Draft)
            throw new BusinessException("Training:CoursePayment:InvalidStatusTransition");
    }

    // ── DTO enrichment ─────────────────────────────────────────────────

    private async Task<CoursePaymentDto> BuildDtoAsync(CoursePayment entity)
        => (await BuildDtosAsync(new[] { entity }))[0];

    private async Task<List<CoursePaymentDto>> BuildDtosAsync(IReadOnlyList<CoursePayment> entities)
    {
        var dtos = entities.Select(e =>
        {
            var d = toDtoMapper.Map(e);
            d.HasInvoice = !string.IsNullOrEmpty(e.InvoiceBlobName);
            return d;
        }).ToList();
        if (dtos.Count == 0) return dtos;

        var providerIds = entities.Select(e => e.TrainingProviderId).Distinct().ToList();
        var providerLookup = await BatchByIdsAsync(providerRepo, providerIds);

        var casualIds = entities.Where(e => e.CasualCourseId.HasValue).Select(e => e.CasualCourseId!.Value).Distinct().ToList();
        var casualLookup = await BatchByIdsAsync(casualCourseRepo, casualIds);

        var sessionIds = entities.Where(e => e.SessionId.HasValue).Select(e => e.SessionId!.Value).Distinct().ToList();
        var sessionLookup = await BatchByIdsAsync(sessionRepo, sessionIds);

        // Phase 4C-α (v4.10.0): CourseSession exposes TenantCourseId directly — no Course indirection.
        var tenantCourseIds = casualLookup.Values.Select(c => c.TenantCourseId)
            .Concat(sessionLookup.Values.Select(s => s.TenantCourseId))
            .Distinct().ToList();
        var nameLookup = await courseNameResolver.BatchResolveAsync(tenantCourseIds);

        var confirmerIds = entities.Where(e => e.ConfirmedById.HasValue).Select(e => e.ConfirmedById!.Value).Distinct().ToList();
        var confirmerLookup = await BatchByIdsAsync(userRepo, confirmerIds);

        for (var i = 0; i < dtos.Count; i++)
        {
            var dto = dtos[i];
            var ent = entities[i];

            if (providerLookup.TryGetValue(ent.TrainingProviderId, out var prov))
                dto.TrainingProviderName = prov.ProviderNameAr;

            Guid? tenantCourseId = null;
            if (ent.CasualCourseId.HasValue && casualLookup.TryGetValue(ent.CasualCourseId.Value, out var casual))
            {
                tenantCourseId = casual.TenantCourseId;
                dto.FundingScenario = casual.FundingScenario;
            }
            else if (ent.SessionId.HasValue && sessionLookup.TryGetValue(ent.SessionId.Value, out var session))
            {
                tenantCourseId = session.TenantCourseId;
            }
            if (tenantCourseId.HasValue && nameLookup.TryGetValue(tenantCourseId.Value, out var name))
                dto.CourseNameAr = name.NameAr;

            if (ent.ConfirmedById.HasValue && confirmerLookup.TryGetValue(ent.ConfirmedById.Value, out var user))
                dto.ConfirmedByName = user.UserName;
        }

        return dtos;
    }

    private async Task<Dictionary<Guid, T>> BatchByIdsAsync<T>(IRepository<T, Guid> repo, IEnumerable<Guid> ids)
        where T : class, IEntity<Guid>
    {
        var idList = ids.Where(g => g != Guid.Empty).Distinct().ToList();
        if (idList.Count == 0) return new Dictionary<Guid, T>();
        var queryable = await repo.GetQueryableAsync();
        var rows = await AsyncExecuter.ToListAsync(queryable.Where(x => idList.Contains(x.Id)));
        return rows.ToDictionary(x => x.Id);
    }
}

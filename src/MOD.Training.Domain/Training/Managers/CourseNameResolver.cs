using MOD.Training.Training.Catalog;
using MOD.Training.Training.TenantCourses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
 

namespace MOD.Training.Training.Managers;

/// <summary>
/// Resolves TenantCourseId → CourseCatalog name (Ar/En).
/// Used by all AppServices that need to display course names in DTOs.
/// Batch-loads to avoid N+1 queries.
/// </summary>
public class CourseNameResolver(
    IRepository<TenantCourse, Guid> tenantCourseRepo,
    IRepository<CourseCatalog, Guid> catalogRepo)
    : ITransientDependency
{
    public record CourseName(string NameAr, string NameEn);

    /// <summary>
    /// Resolves a single TenantCourseId to course names.
    /// </summary>
    public async Task<CourseName?> ResolveAsync(Guid tenantCourseId)
    {
        var tc = await tenantCourseRepo.FindAsync(tenantCourseId);
        if (tc == null) return null;

        var catalog = await catalogRepo.FindAsync(tc.CatalogCourseId);
        if (catalog == null) return null;

        return new CourseName(catalog.CourseNameAr, catalog.CourseNameEn);
    }

    /// <summary>
    /// Batch-resolves multiple TenantCourseIds.
    /// Returns Dictionary&lt;TenantCourseId, CourseName&gt;.
    /// </summary>
    public async Task<Dictionary<Guid, CourseName>> BatchResolveAsync(List<Guid> tenantCourseIds)
    {
        if (tenantCourseIds.Count == 0) return new();

        var distinctIds = tenantCourseIds.Distinct().ToList();

        // Load TenantCourses
        var tcQueryable = await tenantCourseRepo.GetQueryableAsync();
        var tenantCourses = await tenantCourseRepo.AsyncExecuter.ToListAsync(
            tcQueryable.Where(x => distinctIds.Contains(x.Id)));

        // Load corresponding Catalogs
        var catalogIds = tenantCourses.Select(x => x.CatalogCourseId).Distinct().ToList();
        var catQueryable = await catalogRepo.GetQueryableAsync();
        var catalogs = await catalogRepo.AsyncExecuter.ToListAsync(
            catQueryable.Where(x => catalogIds.Contains(x.Id)));

        var catalogLookup = catalogs.ToDictionary(x => x.Id);

        var result = new Dictionary<Guid, CourseName>();
        foreach (var tc in tenantCourses)
        {
            if (catalogLookup.TryGetValue(tc.CatalogCourseId, out var cat))
            {
                result[tc.Id] = new CourseName(cat.CourseNameAr, cat.CourseNameEn);
            }
        }

        return result;
    }
}

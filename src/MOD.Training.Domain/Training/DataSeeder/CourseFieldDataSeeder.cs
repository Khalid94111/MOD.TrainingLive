using MOD.Training.Training.Catalog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.DataSeeder;

public interface ICourseFieldDataSeeder
{
    Task SeedAsync(DataSeedContext context);
}

public class CourseFieldDataSeeder(
    IRepository<CourseField, Guid> repo,
    ICurrentTenant currentTenant)
    : ITransientDependency, ICourseFieldDataSeeder
{
    public async Task SeedAsync(DataSeedContext context)
    {
        using (currentTenant.Change(null))
        {
            if (await repo.AnyAsync(x => x.Id == FieldIds.Security))
                return;

            await repo.InsertManyAsync(new List<CourseField>
            {
                new(FieldIds.Security) { FieldNameAr = "أمن المعلومات", FieldNameEn = "Information Security", IsActive = true },
                new(FieldIds.Leadership) { FieldNameAr = "القيادة", FieldNameEn = "Leadership", IsActive = true },
                new(FieldIds.Technology) { FieldNameAr = "التقنية", FieldNameEn = "Technology", IsActive = true },
                new(FieldIds.Management) { FieldNameAr = "الإدارة", FieldNameEn = "Management", IsActive = true },
                new(FieldIds.Engineering) { FieldNameAr = "الهندسة", FieldNameEn = "Engineering", IsActive = true },
                new(FieldIds.Medical) { FieldNameAr = "الطب والإسعاف", FieldNameEn = "Medical & First Aid", IsActive = true },
            }, autoSave: true);
        }
    }
}

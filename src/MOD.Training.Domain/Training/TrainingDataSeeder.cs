using MOD.Training.Training.Catalog;
using MOD.Training.Training.Enums;
using System;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training;

/// <summary>
/// Seeds initial training data: CourseFields + sample CourseCatalog + conditions.
/// Register in your DbMigrator or Host module.
/// Runs supra-tenant (no tenant filter).
/// </summary>
public class TrainingDataSeeder : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<CourseField, Guid> _fieldRepo;
    private readonly IRepository<CourseCatalog, Guid> _catalogRepo;
    private readonly IRepository<CatalogEnrollmentCondition, Guid> _conditionRepo;
    private readonly IGuidGenerator _guidGenerator;
    private readonly ICurrentTenant _currentTenant;

    public TrainingDataSeeder(
        IRepository<CourseField, Guid> fieldRepo,
        IRepository<CourseCatalog, Guid> catalogRepo,
        IRepository<CatalogEnrollmentCondition, Guid> conditionRepo,
        IGuidGenerator guidGenerator,
        ICurrentTenant currentTenant)
    {
        _fieldRepo = fieldRepo;
        _catalogRepo = catalogRepo;
        _conditionRepo = conditionRepo;
        _guidGenerator = guidGenerator;
        _currentTenant = currentTenant;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        // Run without tenant filter (supra-tenant data)
        using (_currentTenant.Change(null))
        {
            await SeedCourseFieldsAsync();
            await SeedCourseCatalogAsync();
        }
    }

    private async Task SeedCourseFieldsAsync()
    {
        if (await _fieldRepo.GetCountAsync() > 0) return;

        var fields = new (string Ar, string En)[]
        {
            ("تقنية المعلومات", "Information Technology"),
            ("العلوم العسكرية", "Military Sciences"),
            ("الإدارة والقيادة", "Management & Leadership"),
            ("الهندسة", "Engineering"),
            ("لغات", "Languages"),
            ("المالية والمحاسبة", "Finance & Accounting"),
            ("القانون والتشريعات", "Law & Legislation"),
            ("الصحة والسلامة", "Health & Safety"),
        };

        foreach (var (ar, en) in fields)
        {
            await _fieldRepo.InsertAsync(new CourseField
            {
                //Id = _guidGenerator.Create(),
                FieldNameAr = ar,
                FieldNameEn = en,
                IsActive = true,
            }, autoSave: true);
        }
    }

    private async Task SeedCourseCatalogAsync()
    {
        if (await _catalogRepo.GetCountAsync() > 0) return;

        // Get field IDs
        var fields = await _fieldRepo.GetListAsync();
        var itField = fields.Find(f => f.FieldNameEn == "Information Technology")!;
        var mgmtField = fields.Find(f => f.FieldNameEn == "Management & Leadership")!;
        var milField = fields.Find(f => f.FieldNameEn == "Military Sciences")!;
        var engField = fields.Find(f => f.FieldNameEn == "Engineering")!;
        var langField = fields.Find(f => f.FieldNameEn == "Languages")!;

        // ── Course 1: Advanced Cybersecurity ──
        var cyber = await _catalogRepo.InsertAsync(new CourseCatalog
        {
            //Id = _guidGenerator.Create(),
            CourseNameAr = "الأمن السيبراني المتقدم",
            CourseNameEn = "Advanced Cybersecurity",
            DescriptionAr = "دورة متقدمة في أمن المعلومات والشبكات",
            DescriptionEn = "Advanced course in information and network security",
            Category = "Military",
            Nature = "Qualifying",
            FieldId = itField.Id,
            ResultType = ResultType.GradeScore,
            RequiresEvaluation = true,
            RequiresProviderEvaluation = false,
            HasCertificate = true,
            EvaluationBlocksCertificate = false,
            IsActive = true,
        }, autoSave: true);

        await _conditionRepo.InsertAsync(new CatalogEnrollmentCondition
        {
            //Id = _guidGenerator.Create(),
            CatalogCourseId = cyber.Id,
            ConditionType = ConditionType.Rank,
            ConditionValue = "{\"min\":\"Captain\",\"max\":\"Colonel\"}",
        }, autoSave: true);

        await _conditionRepo.InsertAsync(new CatalogEnrollmentCondition
        {
            //Id = _guidGenerator.Create(),
            CatalogCourseId = cyber.Id,
            ConditionType = ConditionType.ServiceYears,
            ConditionValue = "{\"min\":5}",
        }, autoSave: true);

        // ── Course 2: Strategic Leadership ──
        var leadership = await _catalogRepo.InsertAsync(new CourseCatalog
        {
            //Id = _guidGenerator.Create(),
            CourseNameAr = "القيادة الإستراتيجية",
            CourseNameEn = "Strategic Leadership",
            DescriptionAr = "دورة في مهارات القيادة الاستراتيجية",
            DescriptionEn = "Course in strategic leadership skills",
            Category = "Military",
            Nature = "Mandatory",
            FieldId = mgmtField.Id,
            ResultType = ResultType.PassFail,
            RequiresEvaluation = true,
            RequiresProviderEvaluation = true,
            HasCertificate = true,
            EvaluationBlocksCertificate = true,
            IsActive = true,
        }, autoSave: true);

        await _conditionRepo.InsertAsync(new CatalogEnrollmentCondition
        {
            //Id = _guidGenerator.Create(),
            CatalogCourseId = leadership.Id,
            ConditionType = ConditionType.Rank,
            ConditionValue = "{\"min\":\"Major\",\"max\":\"BrigadierGeneral\"}",
        }, autoSave: true);

        await _conditionRepo.InsertAsync(new CatalogEnrollmentCondition
        {
            //Id = _guidGenerator.Create(),
            CatalogCourseId = leadership.Id,
            ConditionType = ConditionType.ServiceYears,
            ConditionValue = "{\"min\":10}",
        }, autoSave: true);

        await _conditionRepo.InsertAsync(new CatalogEnrollmentCondition
        {
            //Id = _guidGenerator.Create(),
            CatalogCourseId = leadership.Id,
            ConditionType = ConditionType.Education,
            ConditionValue = "{\"min\":\"Bachelor\"}",
        }, autoSave: true);

        // ── Course 3: Professional Project Management ──
        await _catalogRepo.InsertAsync(new CourseCatalog
        {
            //Id = _guidGenerator.Create(),
            CourseNameAr = "إدارة المشاريع الاحترافية",
            CourseNameEn = "Professional Project Management (PMP)",
            DescriptionAr = "دورة إدارة المشاريع الاحترافية PMP",
            DescriptionEn = "Professional project management PMP certification prep",
            Category = "Civilian",
            Nature = "Qualifying",
            FieldId = mgmtField.Id,
            ResultType = ResultType.AttendanceOnly,
            RequiresEvaluation = false,
            HasCertificate = true,
            IsActive = true,
        }, autoSave: true);

        // ── Course 4: Heavy Equipment Maintenance ──
        var heavy = await _catalogRepo.InsertAsync(new CourseCatalog
        {
            //Id = _guidGenerator.Create(),
            CourseNameAr = "صيانة المعدات الثقيلة",
            CourseNameEn = "Heavy Equipment Maintenance",
            Category = "Civilian",
            Nature = "Qualifying",
            FieldId = engField.Id,
            ResultType = ResultType.CompletedNotCompleted,
            HasCertificate = true,
            IsActive = true,
        }, autoSave: true);

        await _conditionRepo.InsertAsync(new CatalogEnrollmentCondition
        {
            //Id = _guidGenerator.Create(),
            CatalogCourseId = heavy.Id,
            ConditionType = ConditionType.MedicalFitness,
            ConditionValue = "{\"required\":true}",
        }, autoSave: true);

        // ── Course 5: Advanced English ──
        await _catalogRepo.InsertAsync(new CourseCatalog
        {
            //Id = _guidGenerator.Create(),
            CourseNameAr = "اللغة الإنجليزية — المستوى المتقدم",
            CourseNameEn = "English Language — Advanced Level",
            Category = "Civilian",
            Nature = "Qualifying",
            FieldId = langField.Id,
            ResultType = ResultType.GradeScore,
            RequiresEvaluation = true,
            HasCertificate = false,
            IsActive = true,
        }, autoSave: true);

        // ── Course 6: Reconnaissance (Military, inactive) ──
        await _catalogRepo.InsertAsync(new CourseCatalog
        {
            //Id = _guidGenerator.Create(),
            CourseNameAr = "الاستطلاع والمراقبة المتقدمة",
            CourseNameEn = "Advanced Reconnaissance & Surveillance",
            Category = "Military",
            Nature = "Mandatory",
            FieldId = milField.Id,
            ResultType = ResultType.PassFail,
            RequiresEvaluation = true,
            HasCertificate = true,
            IsActive = false, // inactive sample
        }, autoSave: true);
    }
}

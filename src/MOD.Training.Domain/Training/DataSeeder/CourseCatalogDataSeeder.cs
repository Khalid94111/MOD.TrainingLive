using MOD.Training.Training.Catalog;
using MOD.Training.Training.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.DataSeeder;

public interface ICourseCatalogDataSeeder
{
    Task SeedAsync(DataSeedContext context);
}

public class CourseCatalogDataSeeder(
    IRepository<CourseCatalog, Guid> catalogRepo,
    ICurrentTenant currentTenant)
    : ITransientDependency, ICourseCatalogDataSeeder
{
    public async Task SeedAsync(DataSeedContext context)
    {
        using (currentTenant.Change(null))
        {
            if (await catalogRepo.AnyAsync(x => x.Id == CatalogIds.Cybersecurity))
                return;

            await SeedCatalogAsync();
        }
    }

    private async Task SeedCatalogAsync()
    {
        await catalogRepo.InsertManyAsync(new List<CourseCatalog>
        {
            new(CatalogIds.Cybersecurity) { CourseNameAr = "الأمن السيبراني المتقدم", CourseNameEn = "Advanced Cybersecurity", FieldId = FieldIds.Security,
                DescriptionAr = "دورة متقدمة في حماية الأنظمة والشبكات من التهديدات السيبرانية", DescriptionEn = "Advanced course in protecting systems and networks from cyber threats",
                Category = "Military", Nature = "Qualifying", ResultType = ResultType.PassFail,
                RequiresEvaluation = true, RequiresProviderEvaluation = true, HasCertificate = true, EvaluationBlocksCertificate = true, IsActive = true },

            new(CatalogIds.Leadership) { CourseNameAr = "أساسيات القيادة العسكرية", CourseNameEn = "Military Leadership Fundamentals", FieldId = FieldIds.Leadership,
                DescriptionAr = "مبادئ القيادة العسكرية الحديثة وأساليب اتخاذ القرار", DescriptionEn = "Modern military leadership principles and decision-making",
                Category = "Military", Nature = "Mandatory", ResultType = ResultType.AttendanceOnly,
                RequiresEvaluation = true, HasCertificate = true, IsActive = true },

            new(CatalogIds.ProjectMgmt) { CourseNameAr = "إدارة المشاريع الاحترافية", CourseNameEn = "Professional Project Management (PMP)", FieldId = FieldIds.Management,
                DescriptionAr = "التأهيل لشهادة PMP في إدارة المشاريع", DescriptionEn = "PMP certification preparation",
                Category = "Civilian", Nature = "Qualifying", ResultType = ResultType.GradeScore,
                RequiresEvaluation = true, RequiresProviderEvaluation = true, HasCertificate = true, EvaluationBlocksCertificate = true, IsActive = true },

            new(CatalogIds.NetworkEng) { CourseNameAr = "هندسة الشبكات المتقدمة", CourseNameEn = "Advanced Network Engineering (CCNP)", FieldId = FieldIds.Technology,
                DescriptionAr = "تصميم وإدارة شبكات المؤسسات المعقدة", DescriptionEn = "Enterprise network design and management",
                Category = "Civilian", Nature = "Qualifying", ResultType = ResultType.PassFail,
                RequiresEvaluation = true, RequiresProviderEvaluation = true, HasCertificate = true, IsActive = true },

            new(CatalogIds.FirstAid) { CourseNameAr = "الإسعافات الأولية الميدانية", CourseNameEn = "Field First Aid", FieldId = FieldIds.Medical,
                DescriptionAr = "إجراءات الإسعاف الأولي في بيئة العمليات الميدانية", DescriptionEn = "First aid procedures in field operations",
                Category = "Military", Nature = "Mandatory", ResultType = ResultType.CompletedNotCompleted,
                HasCertificate = true, IsActive = true },

            new(CatalogIds.StrategicPlanning) { CourseNameAr = "التخطيط الاستراتيجي", CourseNameEn = "Strategic Planning", FieldId = FieldIds.Management,
                DescriptionAr = "منهجيات التخطيط الاستراتيجي المؤسسي", DescriptionEn = "Institutional strategic planning methodologies",
                Category = "Military", Nature = "Military", ResultType = ResultType.AttendanceOnly,
                RequiresEvaluation = true, IsActive = true },

            new(CatalogIds.DataAnalysis) { CourseNameAr = "تحليل البيانات", CourseNameEn = "Data Analysis & Visualization", FieldId = FieldIds.Technology,
                DescriptionAr = "تحليل البيانات باستخدام أدوات حديثة", DescriptionEn = "Data analysis using modern tools and visualization",
                Category = "Civilian", Nature = "Qualifying", ResultType = ResultType.GradeScore,
                RequiresEvaluation = true, RequiresProviderEvaluation = true, HasCertificate = true, EvaluationBlocksCertificate = true, IsActive = true },

            new(CatalogIds.FitnessInstructor) { CourseNameAr = "مدرب اللياقة البدنية", CourseNameEn = "Physical Fitness Instructor", FieldId = FieldIds.Medical,
                DescriptionAr = "تأهيل مدربي اللياقة البدنية العسكرية", DescriptionEn = "Military physical fitness instructor qualification",
                Category = "Military", Nature = "Military", ResultType = ResultType.PassFail,
                HasCertificate = true, IsActive = true },

            new(CatalogIds.CombatEngineering) { CourseNameAr = "الهندسة القتالية", CourseNameEn = "Combat Engineering", FieldId = FieldIds.Engineering,
                DescriptionAr = "أسس الهندسة القتالية والموانع والتحصينات", DescriptionEn = "Combat engineering fundamentals",
                Category = "Military", Nature = "Military", ResultType = ResultType.CompletedNotCompleted, IsActive = true },

            new(CatalogIds.MilitaryWriting) { CourseNameAr = "الكتابة العسكرية العربية", CourseNameEn = "Arabic Military Writing", FieldId = FieldIds.Leadership,
                DescriptionAr = "صياغة المراسلات والتقارير العسكرية الرسمية", DescriptionEn = "Drafting official military correspondence",
                Category = "Military"   , Nature = "Military", ResultType = ResultType.AttendanceOnly, IsActive = true },
        }, autoSave: true);
    }
}

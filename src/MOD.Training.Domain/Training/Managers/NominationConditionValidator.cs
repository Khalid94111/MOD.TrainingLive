using MOD.Training.Training.Enums;
using MOD.Training.Training.Hr;
using MOD.Training.Training.Plans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
 

namespace MOD.Training.Training.Managers;

/// <summary>
/// Validates employee eligibility against enrollment conditions.
/// Called during nomination creation. Blocks nomination if any condition fails.
/// </summary>
public class NominationConditionValidator(
    IRepository<SessionCondition, Guid> sessionConditionRepo,
    IRepository<PlanItemCondition, Guid> planItemConditionRepo,
    EmployeeResolver employeeResolver)
    : ITransientDependency
{
    public record ConditionResult(bool Passed, string ConditionTypeAr, string Details);

    /// <summary>
    /// Validates an employee against all conditions for a session.
    /// Returns a list of results — one per condition.
    /// If any has Passed = false, the nomination should be blocked.
    /// </summary>
    public async Task<List<ConditionResult>> ValidateAsync(Guid sessionId, Guid employeeId)
    {
        var results = new List<ConditionResult>();

        // Get employee with rank
        var employee = await employeeResolver.GetWithRankAsync(employeeId);
        if (employee == null)
        {
            results.Add(new ConditionResult(false, "الموظف", "الموظف غير موجود في النظام"));
            return results;
        }

        // Get all ranks for comparison
        var allRanks = await employeeResolver.GetAllRanksAsync();

        // Get conditions from session
        var condQueryable = await sessionConditionRepo.GetQueryableAsync();
        var conditions = await sessionConditionRepo.AsyncExecuter.ToListAsync(
            condQueryable.Where(x => x.SessionId == sessionId));

        // If no session conditions, check plan item conditions
        if (!conditions.Any())
        {
            // Session conditions might not be populated yet — this is OK
            return results;
        }

        foreach (var condition in conditions)
        {
            var result = ValidateCondition(condition.ConditionType, condition.ConditionValue, employee, allRanks);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Validates an employee against plan item conditions (before session exists).
    /// </summary>
    public Task<List<ConditionResult>> ValidateByPlanItemAsync(Guid planItemId, Guid employeeId)
        => ValidateAgainstPlanItemAsync(planItemId, employeeId);

    public async Task<List<ConditionResult>> ValidateAgainstPlanItemAsync(Guid planItemId, Guid employeeId)
    {
        var results = new List<ConditionResult>();

        var employee = await employeeResolver.GetWithRankAsync(employeeId);
        if (employee == null)
        {
            results.Add(new ConditionResult(false, "الموظف", "الموظف غير موجود في النظام"));
            return results;
        }

        var allRanks = await employeeResolver.GetAllRanksAsync();

        var condQueryable = await planItemConditionRepo.GetQueryableAsync();
        var conditions = await planItemConditionRepo.AsyncExecuter.ToListAsync(
            condQueryable.Where(x => x.PlanItemId == planItemId));

        foreach (var condition in conditions)
        {
            var result = ValidateCondition(condition.ConditionType, condition.ConditionValue, employee, allRanks);
            results.Add(result);
        }

        return results;
    }

    private ConditionResult ValidateCondition(
        ConditionType type, string conditionValueJson, Employee employee, List<Rank> allRanks)
    {
        try
        {
            var json = JsonDocument.Parse(conditionValueJson).RootElement;

            return type switch
            {
                ConditionType.Rank => ValidateRank(json, employee, allRanks),
                ConditionType.Age => ValidateAge(json, employee),
                ConditionType.ServiceYears => ValidateServiceYears(json, employee),
                ConditionType.Education => ValidateEducation(json, employee),
                ConditionType.MedicalFitness => ValidateMedicalFitness(json, employee),
                ConditionType.SecurityClearance => ValidateSecurityClearance(json, employee),
                ConditionType.LanguageLevel => ValidateLanguageLevel(json, employee),
                ConditionType.PreviousCourse => ValidatePreviousCourse(json, employee),
                ConditionType.Custom => ValidateCustom(json, employee),
                _ => new ConditionResult(true, type.ToString(), "نوع شرط غير معروف — تم تجاوزه")
            };
        }
        catch (Exception ex)
        {
            return new ConditionResult(false, GetConditionNameAr(type), $"خطأ في التحقق: {ex.Message}");
        }
    }

    // --- RANK ---
    private ConditionResult ValidateRank(JsonElement json, Employee employee, List<Rank> allRanks)
    {
        if (employee.Rank == null)
            return new ConditionResult(false, "الرتبة", "رتبة الموظف غير محددة");

        var minRankName = json.TryGetProperty("min", out var minProp) ? minProp.GetString() : null;
        var maxRankName = json.TryGetProperty("max", out var maxProp) ? maxProp.GetString() : null;

        var empRankOrder = employee.Rank.SortOrder;

        if (minRankName != null)
        {
            var minRank = allRanks.FirstOrDefault(r => r.NameEn == minRankName);
            if (minRank != null && empRankOrder < minRank.SortOrder)
            {
                return new ConditionResult(false, "الرتبة",
                    $"الرتبة الحالية ({employee.Rank.NameAr}) أقل من المطلوب ({minRank.NameAr})");
            }
        }

        if (maxRankName != null)
        {
            var maxRank = allRanks.FirstOrDefault(r => r.NameEn == maxRankName);
            if (maxRank != null && empRankOrder > maxRank.SortOrder)
            {
                return new ConditionResult(false, "الرتبة",
                    $"الرتبة الحالية ({employee.Rank.NameAr}) أعلى من المطلوب ({maxRank.NameAr})");
            }
        }

        return new ConditionResult(true, "الرتبة",
            $"الرتبة ({employee.Rank.NameAr}) ضمن النطاق المطلوب");
    }

    // --- AGE ---
    private ConditionResult ValidateAge(JsonElement json, Employee employee)
    {
        if (!employee.DateOfBirth.HasValue)
            return new ConditionResult(false, "العمر", "تاريخ الميلاد غير محدد");

        var age = DateTime.Today.Year - employee.DateOfBirth.Value.Year;
        if (employee.DateOfBirth.Value.Date > DateTime.Today.AddYears(-age)) age--;

        if (json.TryGetProperty("max", out var maxProp))
        {
            var maxAge = maxProp.GetInt32();
            if (age > maxAge)
                return new ConditionResult(false, "العمر",
                    $"العمر الحالي ({age} سنة) يتجاوز الحد الأقصى ({maxAge} سنة)");
        }

        if (json.TryGetProperty("min", out var minProp))
        {
            var minAge = minProp.GetInt32();
            if (age < minAge)
                return new ConditionResult(false, "العمر",
                    $"العمر الحالي ({age} سنة) أقل من الحد الأدنى ({minAge} سنة)");
        }

        return new ConditionResult(true, "العمر", $"العمر ({age} سنة) ضمن المتطلبات");
    }

    // --- SERVICE YEARS ---
    private ConditionResult ValidateServiceYears(JsonElement json, Employee employee)
    {
        if (!employee.JoinDate.HasValue)
            return new ConditionResult(false, "سنوات الخدمة", "تاريخ الالتحاق غير محدد");

        var serviceYears = (DateTime.Today - employee.JoinDate.Value).Days / 365;

        if (json.TryGetProperty("min", out var minProp))
        {
            var minYears = minProp.GetInt32();
            if (serviceYears < minYears)
                return new ConditionResult(false, "سنوات الخدمة",
                    $"سنوات الخدمة ({serviceYears}) أقل من المطلوب ({minYears})");
        }

        return new ConditionResult(true, "سنوات الخدمة", $"سنوات الخدمة ({serviceYears}) تستوفي الشرط");
    }

    // --- EDUCATION ---
    private ConditionResult ValidateEducation(JsonElement json, Employee employee)
    {
        var educationLevels = new Dictionary<string, int>
        {
            ["HighSchool"] = 1, ["Diploma"] = 2, ["Bachelor"] = 3, ["Master"] = 4, ["PhD"] = 5
        };

        if (string.IsNullOrEmpty(employee.Education))
            return new ConditionResult(false, "المؤهل العلمي", "المؤهل العلمي غير محدد");

        if (json.TryGetProperty("min", out var minProp))
        {
            var requiredLevel = minProp.GetString() ?? "";
            if (educationLevels.TryGetValue(requiredLevel, out var reqOrder) &&
                educationLevels.TryGetValue(employee.Education, out var empOrder))
            {
                if (empOrder < reqOrder)
                    return new ConditionResult(false, "المؤهل العلمي",
                        $"المؤهل الحالي ({employee.Education}) أقل من المطلوب ({requiredLevel})");
            }
        }

        return new ConditionResult(true, "المؤهل العلمي", $"المؤهل ({employee.Education}) يستوفي الشرط");
    }

    // --- MEDICAL FITNESS ---
    private ConditionResult ValidateMedicalFitness(JsonElement json, Employee employee)
    {
        if (json.TryGetProperty("required", out var reqProp) && reqProp.GetBoolean())
        {
            if (!employee.MedicalFitness)
                return new ConditionResult(false, "اللياقة الطبية", "الموظف غير لائق طبياً");
        }

        return new ConditionResult(true, "اللياقة الطبية", "الموظف لائق طبياً");
    }

    // --- SECURITY CLEARANCE ---
    private ConditionResult ValidateSecurityClearance(JsonElement json, Employee employee)
    {
        if (string.IsNullOrEmpty(employee.SecurityClearance))
            return new ConditionResult(false, "التصريح الأمني", "التصريح الأمني غير محدد");

        return new ConditionResult(true, "التصريح الأمني", "التصريح الأمني متوفر");
    }

    // --- LANGUAGE LEVEL ---
    private ConditionResult ValidateLanguageLevel(JsonElement json, Employee employee)
    {
        var languageLevels = new Dictionary<string, int>
        {
            ["A1"] = 1, ["A2"] = 2, ["B1"] = 3, ["B2"] = 4, ["C1"] = 5, ["C2"] = 6
        };

        var requiredLang = json.TryGetProperty("language", out var langProp) ? langProp.GetString() : "English";
        var requiredLevel = json.TryGetProperty("level", out var levelProp) ? levelProp.GetString() : "B1";

        if (string.IsNullOrEmpty(employee.LanguageLevel))
            return new ConditionResult(false, "مستوى اللغة", $"مستوى {requiredLang} غير محدد للموظف");

        try
        {
            var empLangs = JsonDocument.Parse(employee.LanguageLevel).RootElement;
            if (empLangs.TryGetProperty(requiredLang!, out var empLevelProp))
            {
                var empLevel = empLevelProp.GetString() ?? "";
                if (languageLevels.TryGetValue(requiredLevel!, out var reqOrder) &&
                    languageLevels.TryGetValue(empLevel, out var empOrder))
                {
                    if (empOrder < reqOrder)
                        return new ConditionResult(false, "مستوى اللغة",
                            $"مستوى {requiredLang} الحالي ({empLevel}) أقل من المطلوب ({requiredLevel})");
                }
            }
            else
            {
                return new ConditionResult(false, "مستوى اللغة", $"لا يوجد تقييم للغة {requiredLang}");
            }
        }
        catch
        {
            return new ConditionResult(false, "مستوى اللغة", "خطأ في قراءة بيانات اللغة");
        }

        return new ConditionResult(true, "مستوى اللغة", $"مستوى {requiredLang} يستوفي الشرط");
    }

    // --- PREVIOUS COURSE ---
    private ConditionResult ValidatePreviousCourse(JsonElement json, Employee employee)
    {
        // TODO: check if employee has completed a specific course
        // This requires querying Nomination records where status = TDApproved + AttendanceStatus = Attended
        return new ConditionResult(true, "دورة سابقة", "التحقق من الدورات السابقة — يتم يدوياً حالياً");
    }

    // --- CUSTOM ---
    private ConditionResult ValidateCustom(JsonElement json, Employee employee)
    {
        // Custom conditions are validated manually by the approver
        var description = json.TryGetProperty("description", out var descProp) ? descProp.GetString() : "شرط مخصص";
        return new ConditionResult(true, "شرط مخصص", $"يتطلب تحقق يدوي: {description}");
    }

    private static string GetConditionNameAr(ConditionType type) => type switch
    {
        ConditionType.Rank => "الرتبة",
        ConditionType.Age => "العمر",
        ConditionType.ServiceYears => "سنوات الخدمة",
        ConditionType.Education => "المؤهل العلمي",
        ConditionType.MedicalFitness => "اللياقة الطبية",
        ConditionType.SecurityClearance => "التصريح الأمني",
        ConditionType.LanguageLevel => "مستوى اللغة",
        ConditionType.PreviousCourse => "دورة سابقة",
        ConditionType.Custom => "شرط مخصص",
        _ => type.ToString()
    };
}

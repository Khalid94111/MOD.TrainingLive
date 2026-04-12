using MOD.Training.Training.Centers;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance;
using System;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training
{
    // private readonly IRepository<TrainingCenter, Guid> _centerRepository;
    // private readonly IRepository<CenterRoleAssignment, Guid> _roleAssignmentRepository;
    // private readonly IRepository<CenterPlanWindow, Guid> _windowRepository;
    // private readonly IRepository<TrainingCenterPlan, Guid> _planRepository;
    // private readonly IRepository<TrainingCenterPlanItem, Guid> _planItemRepository;
    // private readonly IRepository<TrainingCenterPlanItemUnit, Guid> _planItemUnitRepository;
    // private readonly IGuidGenerator _guidGenerator;
    public class CenterDataSeeder(
    IGuidGenerator _guidGenerator,
    IRepository<TrainingCenter, Guid> _centerRepository,
    IRepository<CenterRoleAssignment, Guid> _roleAssignmentRepository,
    IRepository<TrainingCenterPlan, Guid> _planRepository,
    IRepository<TrainingCenterPlanItem, Guid> _planItemRepository,
        IRepository<TrainingCenterPlanItemUnit, Guid> _planItemUnitRepository,
             ICurrentTenant _currentTenant,

    IRepository<CenterPlanWindow, Guid> _windowRepository)
    : IDataSeedContributor, ITransientDependency
    {
        public async Task SeedAsync(DataSeedContext context)
        {
            // Run without tenant filter (supra-tenant data)
            using (_currentTenant.Change(null))
            {
                await SeedCentersAsync();
             }
        }
        public async Task SeedCentersAsync()
        {
            // Skip if already seeded
            if (await _centerRepository.AnyAsync())
                return;

            // --- 1. Get or create OrgUnits ---
            // NOTE: OrgUnits typically come from HR. These are sample references.
            // In production, use actual OrgUnit IDs from the HR module.
            var militaryOrgUnitId = _guidGenerator.Create();
            var technicalOrgUnitId = _guidGenerator.Create();

            // --- 2. Create Training Centers ---
            var center1 = new TrainingCenter
            {
                OrgUnitId = militaryOrgUnitId,
                CenterNameAr = "مركز التدريب العسكري",
                CenterNameEn = "Military Training Center",
                Location = "معسكر المرتفعة",
                IsActive = true
            };

            var center2 = new TrainingCenter
            {
                OrgUnitId = technicalOrgUnitId,
                CenterNameAr = "مركز التدريب التقني",
                CenterNameEn = "Technical Training Center",
                Location = "مسقط",
                IsActive = true
            };

            await _centerRepository.InsertAsync(center1, autoSave: true);
            await _centerRepository.InsertAsync(center2, autoSave: true);

            // --- 3. Create Role Assignments (2 TCOs + 2 TCMs) ---
            var sampleEmployeeId1 = _guidGenerator.Create();
            var sampleEmployeeId2 = _guidGenerator.Create();
            var sampleEmployeeId3 = _guidGenerator.Create();
            var sampleEmployeeId4 = _guidGenerator.Create();

            // Center 1: 1 TCM + 2 TCOs
            await _roleAssignmentRepository.InsertAsync(new CenterRoleAssignment
            {
                CenterId = center1.Id,
                RoleType = CenterRoleType.TCM,
                AssignmentType = CenterAssignmentType.Employee,
                EmployeeId = sampleEmployeeId1,
                ServiceNumber = "12345"
            }, autoSave: true);

            await _roleAssignmentRepository.InsertAsync(new CenterRoleAssignment
            {
                CenterId = center1.Id,
                RoleType = CenterRoleType.TCO,
                AssignmentType = CenterAssignmentType.Employee,
                EmployeeId = sampleEmployeeId2,
                ServiceNumber = "54321"
            }, autoSave: true);

            await _roleAssignmentRepository.InsertAsync(new CenterRoleAssignment
            {
                CenterId = center1.Id,
                RoleType = CenterRoleType.TCO,
                AssignmentType = CenterAssignmentType.Position,
                PositionId = _guidGenerator.Create()
            }, autoSave: true);

            // Center 2: 1 TCM + 1 TCO
            await _roleAssignmentRepository.InsertAsync(new CenterRoleAssignment
            {
                CenterId = center2.Id,
                RoleType = CenterRoleType.TCM,
                AssignmentType = CenterAssignmentType.Employee,
                EmployeeId = sampleEmployeeId3,
                ServiceNumber = "67890"
            }, autoSave: true);

            await _roleAssignmentRepository.InsertAsync(new CenterRoleAssignment
            {
                CenterId = center2.Id,
                RoleType = CenterRoleType.TCO,
                AssignmentType = CenterAssignmentType.Employee,
                EmployeeId = sampleEmployeeId4,
                ServiceNumber = "09876"
            }, autoSave: true);

            // --- 4. Create CenterPlanWindow for current year ---
            var window = new CenterPlanWindow
            {
                Year = DateTime.Now.Year,
                OpenDate = new DateTime(DateTime.Now.Year, 1, 1),
                CloseDate = new DateTime(DateTime.Now.Year, 6, 30),
                OpenedById = sampleEmployeeId1,
                OpenedAt = DateTime.Now
            };
            await _windowRepository.InsertAsync(window, autoSave: true);

            // --- 5. Create Sample Plan with 3 Items ---
            var plan = new TrainingCenterPlan
            {
                CenterId = center1.Id,
                Year = DateTime.Now.Year,
                Status = CenterPlanStatus.Draft,
                OpenedAt = DateTime.Now,
                OpenedById = sampleEmployeeId2
            };
            await _planRepository.InsertAsync(plan, autoSave: true);

            // NOTE: TenantCourseId should reference actual TenantCourses from Phase 1 seed data.
            // Using placeholder GUIDs here — replace with actual IDs in production.
            var sampleTenantCourseId1 = _guidGenerator.Create();
            var sampleTenantCourseId2 = _guidGenerator.Create();
            var sampleTenantCourseId3 = _guidGenerator.Create();

            var item1 = new TrainingCenterPlanItem
            {
                PlanId = plan.Id,
                TenantCourseId = sampleTenantCourseId1,
                EstimatedStartDate = new DateTime(DateTime.Now.Year, 4, 1),
                EstimatedEndDate = new DateTime(DateTime.Now.Year, 5, 15),
                Capacity = 30,
                DurationWeeks = 6,
                Objective = "تطوير المهارات القيادية الميدانية",
                BeneficiaryType = BeneficiaryType.Internal,
                BatchNumber = 1
            };

            var item2 = new TrainingCenterPlanItem
            {
                PlanId = plan.Id,
                TenantCourseId = sampleTenantCourseId2,
                EstimatedStartDate = new DateTime(DateTime.Now.Year, 6, 1),
                EstimatedEndDate = new DateTime(DateTime.Now.Year, 7, 10),
                Capacity = 25,
                DurationWeeks = 5,
                BeneficiaryType = BeneficiaryType.Internal,
                BatchNumber = 2
            };

            var item3 = new TrainingCenterPlanItem
            {
                PlanId = plan.Id,
                TenantCourseId = sampleTenantCourseId3,
                EstimatedStartDate = new DateTime(DateTime.Now.Year, 9, 1),
                EstimatedEndDate = new DateTime(DateTime.Now.Year, 10, 15),
                Capacity = 20,
                DurationWeeks = 6,
                BeneficiaryType = BeneficiaryType.Shared,
                BatchNumber = 3
            };

            await _planItemRepository.InsertAsync(item1, autoSave: true);
            await _planItemRepository.InsertAsync(item2, autoSave: true);
            await _planItemRepository.InsertAsync(item3, autoSave: true);

            // --- 6. Create PlanItemUnits for Internal items ---
            var sampleUnitId1 = _guidGenerator.Create();
            var sampleUnitId2 = _guidGenerator.Create();

            // Item 1 units
            await _planItemUnitRepository.InsertAsync(new TrainingCenterPlanItemUnit
            {
                PlanItemId = item1.Id,
                UnitId = sampleUnitId1
            }, autoSave: true);

            await _planItemUnitRepository.InsertAsync(new TrainingCenterPlanItemUnit
            {
                PlanItemId = item1.Id,
                UnitId = sampleUnitId2
            }, autoSave: true);

            // Item 2 units
            await _planItemUnitRepository.InsertAsync(new TrainingCenterPlanItemUnit
            {
                PlanItemId = item2.Id,
                UnitId = sampleUnitId1
            }, autoSave: true);
        }


    }
}
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class RebuildTrainingBudgetsByFinancialItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ;WITH Allocations AS
                (
                    SELECT
                        p.TenantId,
                        p.[Year],
                        pf.FinancialItemId,
                        SUM(pf.EstimatedAmountOMR) AS TotalAmount
                    FROM AppTrainingPlans p
                    INNER JOIN AppTrainingPlanItems pi
                        ON pi.PlanId = p.Id
                        AND pi.IsDeleted = 0
                    INNER JOIN AppPlanItemFinancialItems pf
                        ON pf.PlanItemId = pi.Id
                        AND pf.IsDeleted = 0
                    WHERE p.IsDeleted = 0
                        AND p.Status = 5
                    GROUP BY p.TenantId, p.[Year], pf.FinancialItemId
                )
                UPDATE budget
                SET
                    budget.TotalAmount = allocation.TotalAmount,
                    budget.LastModificationTime = SYSUTCDATETIME()
                FROM TrnTrainingBudgets budget
                INNER JOIN Allocations allocation
                    ON allocation.[Year] = budget.[Year]
                    AND allocation.FinancialItemId = budget.FinancialItemId
                    AND (
                        allocation.TenantId = budget.TenantId
                        OR (allocation.TenantId IS NULL AND budget.TenantId IS NULL)
                    )
                WHERE budget.IsDeleted = 0;

                ;WITH Allocations AS
                (
                    SELECT
                        p.TenantId,
                        p.[Year],
                        pf.FinancialItemId,
                        SUM(pf.EstimatedAmountOMR) AS TotalAmount
                    FROM AppTrainingPlans p
                    INNER JOIN AppTrainingPlanItems pi
                        ON pi.PlanId = p.Id
                        AND pi.IsDeleted = 0
                    INNER JOIN AppPlanItemFinancialItems pf
                        ON pf.PlanItemId = pi.Id
                        AND pf.IsDeleted = 0
                    WHERE p.IsDeleted = 0
                        AND p.Status = 5
                    GROUP BY p.TenantId, p.[Year], pf.FinancialItemId
                )
                INSERT INTO TrnTrainingBudgets
                (
                    Id,
                    TenantId,
                    [Year],
                    TotalAmount,
                    SpentAmount,
                    AlertThreshold,
                    CreationTime,
                    CreatorId,
                    LastModificationTime,
                    LastModifierId,
                    IsDeleted,
                    DeleterId,
                    DeletionTime,
                    FinancialItemId
                )
                SELECT
                    NEWID(),
                    allocation.TenantId,
                    allocation.[Year],
                    allocation.TotalAmount,
                    0,
                    80,
                    SYSUTCDATETIME(),
                    NULL,
                    NULL,
                    NULL,
                    0,
                    NULL,
                    NULL,
                    allocation.FinancialItemId
                FROM Allocations allocation
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM TrnTrainingBudgets budget
                    WHERE budget.IsDeleted = 0
                        AND budget.[Year] = allocation.[Year]
                        AND budget.FinancialItemId = allocation.FinancialItemId
                        AND (
                            budget.TenantId = allocation.TenantId
                            OR (budget.TenantId IS NULL AND allocation.TenantId IS NULL)
                        )
                );

                ;WITH ApprovedYears AS
                (
                    SELECT DISTINCT TenantId, [Year]
                    FROM AppTrainingPlans
                    WHERE IsDeleted = 0
                        AND Status = 5
                ),
                AllocationKeys AS
                (
                    SELECT DISTINCT
                        p.TenantId,
                        p.[Year],
                        pf.FinancialItemId
                    FROM AppTrainingPlans p
                    INNER JOIN AppTrainingPlanItems pi
                        ON pi.PlanId = p.Id
                        AND pi.IsDeleted = 0
                    INNER JOIN AppPlanItemFinancialItems pf
                        ON pf.PlanItemId = pi.Id
                        AND pf.IsDeleted = 0
                    WHERE p.IsDeleted = 0
                        AND p.Status = 5
                )
                UPDATE budget
                SET
                    budget.TotalAmount = 0,
                    budget.LastModificationTime = SYSUTCDATETIME()
                FROM TrnTrainingBudgets budget
                INNER JOIN ApprovedYears approvedYear
                    ON approvedYear.[Year] = budget.[Year]
                    AND (
                        approvedYear.TenantId = budget.TenantId
                        OR (approvedYear.TenantId IS NULL AND budget.TenantId IS NULL)
                    )
                WHERE budget.IsDeleted = 0
                    AND budget.TotalAmount <> 0
                    AND NOT EXISTS
                    (
                        SELECT 1
                        FROM AllocationKeys allocation
                        WHERE allocation.[Year] = budget.[Year]
                            AND allocation.FinancialItemId = budget.FinancialItemId
                            AND (
                                allocation.TenantId = budget.TenantId
                                OR (allocation.TenantId IS NULL AND budget.TenantId IS NULL)
                            )
                    );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data-only correction. Keeping the detailed allocations is safer than
            // restoring the deprecated parent-level aggregation.
        }
    }
}

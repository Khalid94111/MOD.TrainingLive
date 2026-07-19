using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class FixCenterPlanEnumStringValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The previous migration converted these enum columns from int to nvarchar.
            // Existing rows were left with numeric strings (e.g. '2') instead of enum names
            // ('Approved'), so approved plans/items were not matched by EF queries.
            migrationBuilder.Sql(@"
                UPDATE [TrnTrainingCenterPlans]
                SET [Status] = CASE [Status]
                    WHEN '0' THEN 'Draft'
                    WHEN '1' THEN 'Submitted'
                    WHEN '2' THEN 'Approved'
                    WHEN '3' THEN 'Rejected'
                    ELSE [Status]
                END
                WHERE [Status] IN ('0','1','2','3');
            ");

            migrationBuilder.Sql(@"
                UPDATE [TrnTrainingCenterPlanItems]
                SET [BeneficiaryType] = CASE [BeneficiaryType]
                    WHEN '0' THEN 'Internal'
                    WHEN '1' THEN 'Shared'
                    ELSE [BeneficiaryType]
                END
                WHERE [BeneficiaryType] IN ('0','1');
            ");

            migrationBuilder.Sql(@"
                UPDATE [TrnCenterRoleAssignments]
                SET [RoleType] = CASE [RoleType]
                    WHEN '0' THEN 'TCO'
                    WHEN '1' THEN 'TCM'
                    ELSE [RoleType]
                END,
                    [AssignmentType] = CASE [AssignmentType]
                    WHEN '0' THEN 'Employee'
                    WHEN '1' THEN 'Position'
                    ELSE [AssignmentType]
                END
                WHERE [RoleType] IN ('0','1') OR [AssignmentType] IN ('0','1');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}

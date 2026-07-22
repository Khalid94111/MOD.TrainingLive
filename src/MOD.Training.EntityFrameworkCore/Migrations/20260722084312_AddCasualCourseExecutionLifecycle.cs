using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class AddCasualCourseExecutionLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExecutionStatus",
                table: "AppCasualCourses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Preserve the position of existing approved courses. Internal courses are
            // immediately schedulable; external courses become scheduled once a winner
            // and confirmed dates already exist.
            migrationBuilder.Sql("""
                UPDATE [AppCasualCourses]
                SET [ExecutionStatus] = 1
                WHERE [IsDeleted] = 0
                  AND [Status] = 6
                  AND ([CourseType] = 0 OR [SelectedPriceQuoteId] IS NOT NULL);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExecutionStatus",
                table: "AppCasualCourses");
        }
    }
}

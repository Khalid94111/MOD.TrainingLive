using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEnrollmentConditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppCatalogEnrollmentConditions");

            migrationBuilder.DropTable(
                name: "AppPlanItemConditions");

            migrationBuilder.DropTable(
                name: "AppSessionConditions");

            migrationBuilder.DropTable(
                name: "AppTenantCourseConditions");

            migrationBuilder.DropColumn(
                name: "ConditionSnapshotJson",
                table: "AppCasualCourseNominations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConditionSnapshotJson",
                table: "AppCasualCourseNominations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppCatalogEnrollmentConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConditionType = table.Column<int>(type: "int", nullable: false),
                    ConditionValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppCatalogEnrollmentConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppCatalogEnrollmentConditions_AppCourseCatalog_CatalogCourseId",
                        column: x => x.CatalogCourseId,
                        principalTable: "AppCourseCatalog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppPlanItemConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConditionType = table.Column<int>(type: "int", nullable: false),
                    ConditionValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppPlanItemConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppPlanItemConditions_AppTrainingPlanItems_PlanItemId",
                        column: x => x.PlanItemId,
                        principalTable: "AppTrainingPlanItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppSessionConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConditionType = table.Column<int>(type: "int", nullable: false),
                    ConditionValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSessionConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppSessionConditions_AppCourseSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AppCourseSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppTenantCourseConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConditionType = table.Column<int>(type: "int", nullable: false),
                    ConditionValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTenantCourseConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppTenantCourseConditions_AppTenantCourses_TenantCourseId",
                        column: x => x.TenantCourseId,
                        principalTable: "AppTenantCourses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppCatalogEnrollmentConditions_CatalogCourseId",
                table: "AppCatalogEnrollmentConditions",
                column: "CatalogCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPlanItemConditions_PlanItemId",
                table: "AppPlanItemConditions",
                column: "PlanItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSessionConditions_SessionId",
                table: "AppSessionConditions",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTenantCourseConditions_TenantCourseId",
                table: "AppTenantCourseConditions",
                column: "TenantCourseId");
        }
    }
}

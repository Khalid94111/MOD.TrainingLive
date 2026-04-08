using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CatalogEnrollmentConditions_CourseCatalog_CatalogCourseId",
                table: "CatalogEnrollmentConditions");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseCatalog_CourseFields_FieldId",
                table: "CourseCatalog");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseProposals_CourseFields_FieldId",
                table: "CourseProposals");

            migrationBuilder.DropForeignKey(
                name: "FK_TenantCourseConditions_TenantCourses_TenantCourseId",
                table: "TenantCourseConditions");

            migrationBuilder.DropForeignKey(
                name: "FK_TenantCourses_CourseCatalog_CatalogCourseId",
                table: "TenantCourses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TenantCourses",
                table: "TenantCourses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TenantCourseConditions",
                table: "TenantCourseConditions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CourseProposals",
                table: "CourseProposals");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CourseFields",
                table: "CourseFields");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CourseCatalog",
                table: "CourseCatalog");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Countries",
                table: "Countries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CatalogEnrollmentConditions",
                table: "CatalogEnrollmentConditions");

            migrationBuilder.RenameTable(
                name: "TenantCourses",
                newName: "AppTenantCourses");

            migrationBuilder.RenameTable(
                name: "TenantCourseConditions",
                newName: "AppTenantCourseConditions");

            migrationBuilder.RenameTable(
                name: "CourseProposals",
                newName: "AppCourseProposals");

            migrationBuilder.RenameTable(
                name: "CourseFields",
                newName: "AppCourseFields");

            migrationBuilder.RenameTable(
                name: "CourseCatalog",
                newName: "AppCourseCatalog");

            migrationBuilder.RenameTable(
                name: "Countries",
                newName: "AppCountries");

            migrationBuilder.RenameTable(
                name: "CatalogEnrollmentConditions",
                newName: "AppCatalogEnrollmentConditions");

            migrationBuilder.RenameIndex(
                name: "IX_TenantCourses_TenantId_CatalogCourseId",
                table: "AppTenantCourses",
                newName: "IX_AppTenantCourses_TenantId_CatalogCourseId");

            migrationBuilder.RenameIndex(
                name: "IX_TenantCourses_TenantId",
                table: "AppTenantCourses",
                newName: "IX_AppTenantCourses_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_TenantCourses_CatalogCourseId",
                table: "AppTenantCourses",
                newName: "IX_AppTenantCourses_CatalogCourseId");

            migrationBuilder.RenameIndex(
                name: "IX_TenantCourseConditions_TenantCourseId",
                table: "AppTenantCourseConditions",
                newName: "IX_AppTenantCourseConditions_TenantCourseId");

            migrationBuilder.RenameIndex(
                name: "IX_CourseProposals_Status",
                table: "AppCourseProposals",
                newName: "IX_AppCourseProposals_Status");

            migrationBuilder.RenameIndex(
                name: "IX_CourseProposals_ProposedById",
                table: "AppCourseProposals",
                newName: "IX_AppCourseProposals_ProposedById");

            migrationBuilder.RenameIndex(
                name: "IX_CourseProposals_FieldId",
                table: "AppCourseProposals",
                newName: "IX_AppCourseProposals_FieldId");

            migrationBuilder.RenameIndex(
                name: "IX_CourseCatalog_FieldId",
                table: "AppCourseCatalog",
                newName: "IX_AppCourseCatalog_FieldId");

            migrationBuilder.RenameIndex(
                name: "IX_CourseCatalog_CourseNameEn",
                table: "AppCourseCatalog",
                newName: "IX_AppCourseCatalog_CourseNameEn");

            migrationBuilder.RenameIndex(
                name: "IX_CourseCatalog_CourseNameAr",
                table: "AppCourseCatalog",
                newName: "IX_AppCourseCatalog_CourseNameAr");

            migrationBuilder.RenameIndex(
                name: "IX_Countries_Code",
                table: "AppCountries",
                newName: "IX_AppCountries_Code");

            migrationBuilder.RenameIndex(
                name: "IX_CatalogEnrollmentConditions_CatalogCourseId",
                table: "AppCatalogEnrollmentConditions",
                newName: "IX_AppCatalogEnrollmentConditions_CatalogCourseId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppTenantCourses",
                table: "AppTenantCourses",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppTenantCourseConditions",
                table: "AppTenantCourseConditions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppCourseProposals",
                table: "AppCourseProposals",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppCourseFields",
                table: "AppCourseFields",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppCourseCatalog",
                table: "AppCourseCatalog",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppCountries",
                table: "AppCountries",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppCatalogEnrollmentConditions",
                table: "AppCatalogEnrollmentConditions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AppCatalogEnrollmentConditions_AppCourseCatalog_CatalogCourseId",
                table: "AppCatalogEnrollmentConditions",
                column: "CatalogCourseId",
                principalTable: "AppCourseCatalog",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AppCourseCatalog_AppCourseFields_FieldId",
                table: "AppCourseCatalog",
                column: "FieldId",
                principalTable: "AppCourseFields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AppCourseProposals_AppCourseFields_FieldId",
                table: "AppCourseProposals",
                column: "FieldId",
                principalTable: "AppCourseFields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AppTenantCourseConditions_AppTenantCourses_TenantCourseId",
                table: "AppTenantCourseConditions",
                column: "TenantCourseId",
                principalTable: "AppTenantCourses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AppTenantCourses_AppCourseCatalog_CatalogCourseId",
                table: "AppTenantCourses",
                column: "CatalogCourseId",
                principalTable: "AppCourseCatalog",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppCatalogEnrollmentConditions_AppCourseCatalog_CatalogCourseId",
                table: "AppCatalogEnrollmentConditions");

            migrationBuilder.DropForeignKey(
                name: "FK_AppCourseCatalog_AppCourseFields_FieldId",
                table: "AppCourseCatalog");

            migrationBuilder.DropForeignKey(
                name: "FK_AppCourseProposals_AppCourseFields_FieldId",
                table: "AppCourseProposals");

            migrationBuilder.DropForeignKey(
                name: "FK_AppTenantCourseConditions_AppTenantCourses_TenantCourseId",
                table: "AppTenantCourseConditions");

            migrationBuilder.DropForeignKey(
                name: "FK_AppTenantCourses_AppCourseCatalog_CatalogCourseId",
                table: "AppTenantCourses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppTenantCourses",
                table: "AppTenantCourses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppTenantCourseConditions",
                table: "AppTenantCourseConditions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppCourseProposals",
                table: "AppCourseProposals");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppCourseFields",
                table: "AppCourseFields");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppCourseCatalog",
                table: "AppCourseCatalog");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppCountries",
                table: "AppCountries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppCatalogEnrollmentConditions",
                table: "AppCatalogEnrollmentConditions");

            migrationBuilder.RenameTable(
                name: "AppTenantCourses",
                newName: "TenantCourses");

            migrationBuilder.RenameTable(
                name: "AppTenantCourseConditions",
                newName: "TenantCourseConditions");

            migrationBuilder.RenameTable(
                name: "AppCourseProposals",
                newName: "CourseProposals");

            migrationBuilder.RenameTable(
                name: "AppCourseFields",
                newName: "CourseFields");

            migrationBuilder.RenameTable(
                name: "AppCourseCatalog",
                newName: "CourseCatalog");

            migrationBuilder.RenameTable(
                name: "AppCountries",
                newName: "Countries");

            migrationBuilder.RenameTable(
                name: "AppCatalogEnrollmentConditions",
                newName: "CatalogEnrollmentConditions");

            migrationBuilder.RenameIndex(
                name: "IX_AppTenantCourses_TenantId_CatalogCourseId",
                table: "TenantCourses",
                newName: "IX_TenantCourses_TenantId_CatalogCourseId");

            migrationBuilder.RenameIndex(
                name: "IX_AppTenantCourses_TenantId",
                table: "TenantCourses",
                newName: "IX_TenantCourses_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_AppTenantCourses_CatalogCourseId",
                table: "TenantCourses",
                newName: "IX_TenantCourses_CatalogCourseId");

            migrationBuilder.RenameIndex(
                name: "IX_AppTenantCourseConditions_TenantCourseId",
                table: "TenantCourseConditions",
                newName: "IX_TenantCourseConditions_TenantCourseId");

            migrationBuilder.RenameIndex(
                name: "IX_AppCourseProposals_Status",
                table: "CourseProposals",
                newName: "IX_CourseProposals_Status");

            migrationBuilder.RenameIndex(
                name: "IX_AppCourseProposals_ProposedById",
                table: "CourseProposals",
                newName: "IX_CourseProposals_ProposedById");

            migrationBuilder.RenameIndex(
                name: "IX_AppCourseProposals_FieldId",
                table: "CourseProposals",
                newName: "IX_CourseProposals_FieldId");

            migrationBuilder.RenameIndex(
                name: "IX_AppCourseCatalog_FieldId",
                table: "CourseCatalog",
                newName: "IX_CourseCatalog_FieldId");

            migrationBuilder.RenameIndex(
                name: "IX_AppCourseCatalog_CourseNameEn",
                table: "CourseCatalog",
                newName: "IX_CourseCatalog_CourseNameEn");

            migrationBuilder.RenameIndex(
                name: "IX_AppCourseCatalog_CourseNameAr",
                table: "CourseCatalog",
                newName: "IX_CourseCatalog_CourseNameAr");

            migrationBuilder.RenameIndex(
                name: "IX_AppCountries_Code",
                table: "Countries",
                newName: "IX_Countries_Code");

            migrationBuilder.RenameIndex(
                name: "IX_AppCatalogEnrollmentConditions_CatalogCourseId",
                table: "CatalogEnrollmentConditions",
                newName: "IX_CatalogEnrollmentConditions_CatalogCourseId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TenantCourses",
                table: "TenantCourses",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TenantCourseConditions",
                table: "TenantCourseConditions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CourseProposals",
                table: "CourseProposals",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CourseFields",
                table: "CourseFields",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CourseCatalog",
                table: "CourseCatalog",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Countries",
                table: "Countries",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CatalogEnrollmentConditions",
                table: "CatalogEnrollmentConditions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CatalogEnrollmentConditions_CourseCatalog_CatalogCourseId",
                table: "CatalogEnrollmentConditions",
                column: "CatalogCourseId",
                principalTable: "CourseCatalog",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseCatalog_CourseFields_FieldId",
                table: "CourseCatalog",
                column: "FieldId",
                principalTable: "CourseFields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseProposals_CourseFields_FieldId",
                table: "CourseProposals",
                column: "FieldId",
                principalTable: "CourseFields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TenantCourseConditions_TenantCourses_TenantCourseId",
                table: "TenantCourseConditions",
                column: "TenantCourseId",
                principalTable: "TenantCourses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TenantCourses_CourseCatalog_CatalogCourseId",
                table: "TenantCourses",
                column: "CatalogCourseId",
                principalTable: "CourseCatalog",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

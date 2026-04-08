using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class FoundationCoreSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldNameAr = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FieldNameEn = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseFields", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseCatalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseNameAr = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CourseNameEn = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nature = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResultType = table.Column<int>(type: "int", nullable: false),
                    RequiresEvaluation = table.Column<bool>(type: "bit", nullable: false),
                    RequiresProviderEvaluation = table.Column<bool>(type: "bit", nullable: false),
                    HasCertificate = table.Column<bool>(type: "bit", nullable: false),
                    EvaluationBlocksCertificate = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseCatalog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseCatalog_CourseFields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "CourseFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourseProposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CourseNameAr = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CourseNameEn = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nature = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ProposedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseProposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseProposals_CourseFields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "CourseFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CatalogEnrollmentConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConditionType = table.Column<int>(type: "int", nullable: false),
                    ConditionValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogEnrollmentConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogEnrollmentConditions_CourseCatalog_CatalogCourseId",
                        column: x => x.CatalogCourseId,
                        principalTable: "CourseCatalog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantCourses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CatalogCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefaultCapacity = table.Column<int>(type: "int", nullable: true),
                    DefaultDurationWeeks = table.Column<int>(type: "int", nullable: true),
                    ResultType = table.Column<int>(type: "int", nullable: false),
                    RequiresEvaluation = table.Column<bool>(type: "bit", nullable: false),
                    RequiresProviderEvaluation = table.Column<bool>(type: "bit", nullable: false),
                    HasCertificate = table.Column<bool>(type: "bit", nullable: false),
                    EvaluationBlocksCertificate = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AddedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantCourses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantCourses_CourseCatalog_CatalogCourseId",
                        column: x => x.CatalogCourseId,
                        principalTable: "CourseCatalog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantCourseConditions",
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
                    table.PrimaryKey("PK_TenantCourseConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantCourseConditions_TenantCourses_TenantCourseId",
                        column: x => x.TenantCourseId,
                        principalTable: "TenantCourses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogEnrollmentConditions_CatalogCourseId",
                table: "CatalogEnrollmentConditions",
                column: "CatalogCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Countries_Code",
                table: "Countries",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseCatalog_CourseNameAr",
                table: "CourseCatalog",
                column: "CourseNameAr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseCatalog_CourseNameEn",
                table: "CourseCatalog",
                column: "CourseNameEn",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseCatalog_FieldId",
                table: "CourseCatalog",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseProposals_FieldId",
                table: "CourseProposals",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseProposals_ProposedById",
                table: "CourseProposals",
                column: "ProposedById");

            migrationBuilder.CreateIndex(
                name: "IX_CourseProposals_Status",
                table: "CourseProposals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TenantCourseConditions_TenantCourseId",
                table: "TenantCourseConditions",
                column: "TenantCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantCourses_CatalogCourseId",
                table: "TenantCourses",
                column: "CatalogCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantCourses_TenantId",
                table: "TenantCourses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantCourses_TenantId_CatalogCourseId",
                table: "TenantCourses",
                columns: new[] { "TenantId", "CatalogCourseId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogEnrollmentConditions");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropTable(
                name: "CourseProposals");

            migrationBuilder.DropTable(
                name: "TenantCourseConditions");

            migrationBuilder.DropTable(
                name: "TenantCourses");

            migrationBuilder.DropTable(
                name: "CourseCatalog");

            migrationBuilder.DropTable(
                name: "CourseFields");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOD.Training.Migrations
{
    /// <inheritdoc />
    public partial class AddTravelManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrvAccommodationRuleAllowanceRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccommodationRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllowanceRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrvAccommodationRuleAllowanceRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrvAccommodationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PaymentPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_TrvAccommodationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrvAllowanceRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RankId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    AllowanceType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnnualPartialAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TicketClass = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_TrvAllowanceRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrvClothingAllowanceRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FullAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnnualPartialAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FullPaymentPeriodYears = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_TrvClothingAllowanceRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrvEmployeeClothingHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TravelRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClothingAllowanceRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsFullPayment = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_TrvEmployeeClothingHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrvFundingSourceVoteRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FundingSourceVoteCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_TrvFundingSourceVoteRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrvTravelTypeDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
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
                    table.PrimaryKey("PK_TrvTravelTypeDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrvClothingAllowanceRuleRanks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClothingAllowanceRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RankId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrvClothingAllowanceRuleRanks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrvClothingAllowanceRuleRanks_TrvClothingAllowanceRules_ClothingAllowanceRuleId",
                        column: x => x.ClothingAllowanceRuleId,
                        principalTable: "TrvClothingAllowanceRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrvAllowanceRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AllowanceType = table.Column<int>(type: "int", nullable: false),
                    TravelTypeDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Category = table.Column<int>(type: "int", nullable: true),
                    AppliesWhenAccommodationIncluded = table.Column<bool>(type: "bit", nullable: true),
                    AccommodationMultiplier = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_TrvAllowanceRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrvAllowanceRules_TrvTravelTypeDefinitions_TravelTypeDefinitionId",
                        column: x => x.TravelTypeDefinitionId,
                        principalTable: "TrvTravelTypeDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrvTravelRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TravelTypeDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DestinationCountry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DestinationCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    DailyAllowanceRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    NeedsPermission = table.Column<bool>(type: "bit", nullable: false),
                    NeedsAwareness = table.Column<bool>(type: "bit", nullable: false),
                    HasTicketCompensation = table.Column<bool>(type: "bit", nullable: false),
                    NeedsTransportation = table.Column<bool>(type: "bit", nullable: false),
                    IncludesAccommodation = table.Column<bool>(type: "bit", nullable: false),
                    UseHighestAllowance = table.Column<bool>(type: "bit", nullable: false),
                    AllowanceTiers = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequesterName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RequiresVisa = table.Column<bool>(type: "bit", nullable: false),
                    VisaCostPerEmployee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RequiresTravelInsurance = table.Column<bool>(type: "bit", nullable: false),
                    TravelInsuranceCostPerEmployee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TicketAirline = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TicketFlightNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TicketDepartureTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TicketArrivalTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TicketCostPerEmployee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TravelOfficeNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SelectedDepartureFlightOfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SelectedReturnFlightOfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceSystem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceTrainingCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceTrainingCourseName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    FundingSourceVoteCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TicketFundingSourceVoteCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VisaFundingSourceVoteCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HealthInsuranceFundingSourceVoteCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DailyAllowanceFundingSourceVoteCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClothingAllowanceFundingSourceVoteCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IntegrationWarnings = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AllowanceSnapshot_OverseasTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AllowanceSnapshot_ClothingTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AllowanceSnapshot_DeductionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AllowanceSnapshot_CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_TrvTravelRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrvTravelRequests_TrvTravelTypeDefinitions_TravelTypeDefinitionId",
                        column: x => x.TravelTypeDefinitionId,
                        principalTable: "TrvTravelTypeDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrvAllowanceRuleSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllowanceRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromDay = table.Column<int>(type: "int", nullable: false),
                    ToDay = table.Column<int>(type: "int", nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AppliesWhenTotalDaysFrom = table.Column<int>(type: "int", nullable: true),
                    AppliesWhenTotalDaysTo = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrvAllowanceRuleSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrvAllowanceRuleSegments_TrvAllowanceRules_AllowanceRuleId",
                        column: x => x.AllowanceRuleId,
                        principalTable: "TrvAllowanceRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrvTravelDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TravelRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileExtension = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    BlobName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrvTravelDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrvTravelDocuments_TrvTravelRequests_TravelRequestId",
                        column: x => x.TravelRequestId,
                        principalTable: "TrvTravelRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrvTravelEmployeeDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TravelRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TicketNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Pnr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TicketFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TicketBlobName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VisaFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    VisaBlobName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InsuranceFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    InsuranceBlobName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrvTravelEmployeeDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrvTravelEmployeeDocuments_TrvTravelRequests_TravelRequestId",
                        column: x => x.TravelRequestId,
                        principalTable: "TrvTravelRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrvTravelFlightOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TravelRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    TicketClass = table.Column<int>(type: "int", nullable: false),
                    Airline = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FlightNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DepartureTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArrivalTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Duration = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsSelected = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrvTravelFlightOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrvTravelFlightOffers_TrvTravelRequests_TravelRequestId",
                        column: x => x.TravelRequestId,
                        principalTable: "TrvTravelRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrvTravelRequestAllowanceDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TravelRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RankName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    DailyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CalculatedDays = table.Column<int>(type: "int", nullable: false),
                    AccommodationPaymentPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OverseasTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ClothingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DeductionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HasMatchingRule = table.Column<bool>(type: "bit", nullable: false),
                    SegmentsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    ClothingCalculationNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrvTravelRequestAllowanceDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrvTravelRequestAllowanceDetails_TrvTravelRequests_TravelRequestId",
                        column: x => x.TravelRequestId,
                        principalTable: "TrvTravelRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrvTravelRequestEmployees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TravelRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RankId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RankName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    DailyAllowanceRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TicketClass = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrvTravelRequestEmployees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrvTravelRequestEmployees_TrvTravelRequests_TravelRequestId",
                        column: x => x.TravelRequestId,
                        principalTable: "TrvTravelRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrvAccommodationRuleAllowanceRules_AccommodationRuleId",
                table: "TrvAccommodationRuleAllowanceRules",
                column: "AccommodationRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAccommodationRuleAllowanceRules_AccommodationRuleId_AllowanceRuleId",
                table: "TrvAccommodationRuleAllowanceRules",
                columns: new[] { "AccommodationRuleId", "AllowanceRuleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrvAccommodationRuleAllowanceRules_AllowanceRuleId",
                table: "TrvAccommodationRuleAllowanceRules",
                column: "AllowanceRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAccommodationRules_IsActive",
                table: "TrvAccommodationRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAccommodationRules_Priority",
                table: "TrvAccommodationRules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRates_AllowanceType",
                table: "TrvAllowanceRates",
                column: "AllowanceType");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRates_Category",
                table: "TrvAllowanceRates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRates_IsActive",
                table: "TrvAllowanceRates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRates_RankId",
                table: "TrvAllowanceRates",
                column: "RankId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRates_TicketClass",
                table: "TrvAllowanceRates",
                column: "TicketClass");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRules_AllowanceType",
                table: "TrvAllowanceRules",
                column: "AllowanceType");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRules_AppliesWhenAccommodationIncluded",
                table: "TrvAllowanceRules",
                column: "AppliesWhenAccommodationIncluded");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRules_Category",
                table: "TrvAllowanceRules",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRules_IsActive",
                table: "TrvAllowanceRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRules_Priority",
                table: "TrvAllowanceRules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRules_TravelTypeDefinitionId",
                table: "TrvAllowanceRules",
                column: "TravelTypeDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRuleSegments_AllowanceRuleId",
                table: "TrvAllowanceRuleSegments",
                column: "AllowanceRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRuleSegments_AppliesWhenTotalDaysFrom",
                table: "TrvAllowanceRuleSegments",
                column: "AppliesWhenTotalDaysFrom");

            migrationBuilder.CreateIndex(
                name: "IX_TrvAllowanceRuleSegments_AppliesWhenTotalDaysTo",
                table: "TrvAllowanceRuleSegments",
                column: "AppliesWhenTotalDaysTo");

            migrationBuilder.CreateIndex(
                name: "IX_TrvClothingAllowanceRuleRanks_ClothingAllowanceRuleId",
                table: "TrvClothingAllowanceRuleRanks",
                column: "ClothingAllowanceRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvClothingAllowanceRuleRanks_ClothingAllowanceRuleId_RankId",
                table: "TrvClothingAllowanceRuleRanks",
                columns: new[] { "ClothingAllowanceRuleId", "RankId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrvClothingAllowanceRuleRanks_RankId",
                table: "TrvClothingAllowanceRuleRanks",
                column: "RankId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvClothingAllowanceRules_IsActive",
                table: "TrvClothingAllowanceRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TrvClothingAllowanceRules_Priority",
                table: "TrvClothingAllowanceRules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_TrvEmployeeClothingHistories_ClothingAllowanceRuleId",
                table: "TrvEmployeeClothingHistories",
                column: "ClothingAllowanceRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvEmployeeClothingHistories_EmployeeId",
                table: "TrvEmployeeClothingHistories",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvEmployeeClothingHistories_TravelRequestId",
                table: "TrvEmployeeClothingHistories",
                column: "TravelRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvFundingSourceVoteRules_IsActive",
                table: "TrvFundingSourceVoteRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TrvFundingSourceVoteRules_PaymentType",
                table: "TrvFundingSourceVoteRules",
                column: "PaymentType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelDocuments_TravelRequestId",
                table: "TrvTravelDocuments",
                column: "TravelRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelEmployeeDocuments_EmployeeId",
                table: "TrvTravelEmployeeDocuments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelEmployeeDocuments_TravelRequestId",
                table: "TrvTravelEmployeeDocuments",
                column: "TravelRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelFlightOffers_Direction",
                table: "TrvTravelFlightOffers",
                column: "Direction");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelFlightOffers_IsSelected",
                table: "TrvTravelFlightOffers",
                column: "IsSelected");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelFlightOffers_TicketClass",
                table: "TrvTravelFlightOffers",
                column: "TicketClass");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelFlightOffers_TravelRequestId",
                table: "TrvTravelFlightOffers",
                column: "TravelRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequestAllowanceDetails_CalculatedAt",
                table: "TrvTravelRequestAllowanceDetails",
                column: "CalculatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequestAllowanceDetails_Category",
                table: "TrvTravelRequestAllowanceDetails",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequestAllowanceDetails_EmployeeId",
                table: "TrvTravelRequestAllowanceDetails",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequestAllowanceDetails_EmployeeNumber",
                table: "TrvTravelRequestAllowanceDetails",
                column: "EmployeeNumber");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequestAllowanceDetails_TravelRequestId",
                table: "TrvTravelRequestAllowanceDetails",
                column: "TravelRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequestEmployees_Category",
                table: "TrvTravelRequestEmployees",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequestEmployees_EmployeeId",
                table: "TrvTravelRequestEmployees",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequestEmployees_EmployeeNumber",
                table: "TrvTravelRequestEmployees",
                column: "EmployeeNumber");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequestEmployees_TicketClass",
                table: "TrvTravelRequestEmployees",
                column: "TicketClass");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequestEmployees_TravelRequestId",
                table: "TrvTravelRequestEmployees",
                column: "TravelRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequests_Category",
                table: "TrvTravelRequests",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequests_CreationTime",
                table: "TrvTravelRequests",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequests_Department",
                table: "TrvTravelRequests",
                column: "Department");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequests_RequesterId",
                table: "TrvTravelRequests",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequests_SelectedDepartureFlightOfferId",
                table: "TrvTravelRequests",
                column: "SelectedDepartureFlightOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequests_SelectedReturnFlightOfferId",
                table: "TrvTravelRequests",
                column: "SelectedReturnFlightOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequests_SourceTenantId",
                table: "TrvTravelRequests",
                column: "SourceTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequests_SourceTrainingCourseId",
                table: "TrvTravelRequests",
                column: "SourceTrainingCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequests_Status",
                table: "TrvTravelRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelRequests_TravelTypeDefinitionId",
                table: "TrvTravelRequests",
                column: "TravelTypeDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelTypeDefinitions_Code",
                table: "TrvTravelTypeDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrvTravelTypeDefinitions_IsActive",
                table: "TrvTravelTypeDefinitions",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrvAccommodationRuleAllowanceRules");

            migrationBuilder.DropTable(
                name: "TrvAccommodationRules");

            migrationBuilder.DropTable(
                name: "TrvAllowanceRates");

            migrationBuilder.DropTable(
                name: "TrvAllowanceRuleSegments");

            migrationBuilder.DropTable(
                name: "TrvClothingAllowanceRuleRanks");

            migrationBuilder.DropTable(
                name: "TrvEmployeeClothingHistories");

            migrationBuilder.DropTable(
                name: "TrvFundingSourceVoteRules");

            migrationBuilder.DropTable(
                name: "TrvTravelDocuments");

            migrationBuilder.DropTable(
                name: "TrvTravelEmployeeDocuments");

            migrationBuilder.DropTable(
                name: "TrvTravelFlightOffers");

            migrationBuilder.DropTable(
                name: "TrvTravelRequestAllowanceDetails");

            migrationBuilder.DropTable(
                name: "TrvTravelRequestEmployees");

            migrationBuilder.DropTable(
                name: "TrvAllowanceRules");

            migrationBuilder.DropTable(
                name: "TrvClothingAllowanceRules");

            migrationBuilder.DropTable(
                name: "TrvTravelRequests");

            migrationBuilder.DropTable(
                name: "TrvTravelTypeDefinitions");
        }
    }
}

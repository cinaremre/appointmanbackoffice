using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rezervise.Infrastructure.Migrations
{
    /// <inheritdoç />
    public partial class AddCommercialModules : Migration
    {
        /// <inheritdoç />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppointmentAttributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    TrackingLinkId = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    FirstTouchSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastTouchSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AttributionModel = table.Column<int>(type: "int", nullable: false),
                    AttributedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentAttributions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessPublicPages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ThemeSettings = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessPublicPages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessReengagementRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    TemplateMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IntervalDays = table.Column<int>(type: "int", nullable: false),
                    LeadDays = table.Column<int>(type: "int", nullable: false),
                    MaxMessagesPerCycle = table.Column<int>(type: "int", nullable: false),
                    CooldownDays = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessReengagementRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessReengagementRules_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessReengagementRules_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CampaignSpends",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    SpendDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    DataSource = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignSpends", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CashMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    CashSessionId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    MovementType = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReferenceId = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashMovements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CashRegisters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashRegisters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashRegisters_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommissionAccruals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    SaleLineId = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    RuleId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BaseAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionAccruals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommissionPayouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionPayouts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommissionRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    ServiceId = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    CalculationType = table.Column<int>(type: "int", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    FixedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationSuppressions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationSuppressions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunicationSuppressions_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunicationSuppressions_Users_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsentHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EvidenceReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerCommunicationPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    LegalBasis = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EvidenceReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsAllowed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerCommunicationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerCommunicationPreferences_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerCommunicationPreferences_Users_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerServiceHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    LastCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VisitCount = table.Column<int>(type: "int", nullable: false),
                    MedianIntervalDays = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerServiceHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerServiceHistories_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerServiceHistories_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerServiceHistories_Users_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "TRY"),
                    ExpenseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeatureDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ColumnMapping = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CsvContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    ImportedRows = table.Column<int>(type: "int", nullable: false),
                    FailedRows = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportRowResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    ImportJobId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    SourceRecordKey = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ErrorDetails = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportRowResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketingCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    ExternalCampaignId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingCampaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PackageUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    CustomerPackageId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    UsageType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageUsages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MinimumStock = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecoveredSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    CancelledAppointmentId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    ReplacementAppointmentId = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    ReleasedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RebookedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoveredSlots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    SoldAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "TRY"),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceMaterials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    QuantityPerService = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceMaterials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServicePackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ValidityDays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePackages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    MovementType = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReferenceId = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    BillingPeriod = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrackingEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    TrackingLinkId = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    AnonymousSessionId = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackingEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrackingLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TargetId = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Medium = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Campaign = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrackingCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    DestinationPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackingLinks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WaitlistEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    PreferredEmployeeId = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    EarliestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LatestDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PreferredTimeRange = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaitlistEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReengagementAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    TriggeredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ConvertedAppointmentId = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReengagementAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReengagementAttempts_BusinessReengagementRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "BusinessReengagementRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CashSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    CashRegisterId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    OpenedBy = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpeningCash = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ClosedBy = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpectedCash = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CountedCash = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Difference = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashSessions_CashRegisters_CashRegisterId",
                        column: x => x.CashRegisterId,
                        principalTable: "CashRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessFeatureOverrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    FeatureId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    QuotaOverride = table.Column<int>(type: "int", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GrantedBy = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessFeatureOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessFeatureOverrides_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessFeatureOverrides_FeatureDefinitions_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "FeatureDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FeatureUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    FeatureId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    UsagePeriod = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    UsedQuantity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureUsages_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeatureUsages_FeatureDefinitions_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "FeatureDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    SaleId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    CashSessionId = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "TRY"),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    ExternalTransactionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsRefund = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RefersToPaymentId = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    SaleId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    ItemType = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ServiceId = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuniqueidentifier", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleLines_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    ServicePackageId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerPackages_ServicePackages_ServicePackageId",
                        column: x => x.ServicePackageId,
                        principalTable: "ServicePackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServicePackageItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    ServicePackageId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    IncludedSessions = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePackageItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicePackageItems_ServicePackages_ServicePackageId",
                        column: x => x.ServicePackageId,
                        principalTable: "ServicePackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    StartAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessSubscriptions_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessSubscriptions_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanFeatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    FeatureId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IncludedQuota = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanFeatures_FeatureDefinitions_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "FeatureDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanFeatures_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WaitlistOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    WaitlistEntryId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuniqueidentifier", nullable: false),
                    SlotStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaitlistOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaitlistOffers_WaitlistEntries_WaitlistEntryId",
                        column: x => x.WaitlistEntryId,
                        principalTable: "WaitlistEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentAttributions_BusinessId",
                table: "AppointmentAttributions",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentAttributions_CampaignId",
                table: "AppointmentAttributions",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "UX_AppointmentAttributions_AppointmentId",
                table: "AppointmentAttributions",
                column: "AppointmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessFeatureOverrides_BusinessId_FeatureId",
                table: "BusinessFeatureOverrides",
                columns: new[] { "BusinessId", "FeatureId" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessFeatureOverrides_FeatureId",
                table: "BusinessFeatureOverrides",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessPublicPages_BusinessId",
                table: "BusinessPublicPages",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "UX_BusinessPublicPages_Slug",
                table: "BusinessPublicPages",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessReengagementRules_Business_Service",
                table: "BusinessReengagementRules",
                columns: new[] { "BusinessId", "ServiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessReengagementRules_ServiceId",
                table: "BusinessReengagementRules",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSubscriptions_BusinessId_Status",
                table: "BusinessSubscriptions",
                columns: new[] { "BusinessId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSubscriptions_PlanId",
                table: "BusinessSubscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignSpends_BusinessId_SpendDate",
                table: "CampaignSpends",
                columns: new[] { "BusinessId", "SpendDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignSpends_CampaignId",
                table: "CampaignSpends",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_BusinessId",
                table: "CashMovements",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_CashSessionId",
                table: "CashMovements",
                column: "CashSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_MovementType",
                table: "CashMovements",
                column: "MovementType");

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisters_BusinessId",
                table: "CashRegisters",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisters_IsActive",
                table: "CashRegisters",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_BusinessId",
                table: "CashSessions",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_Status",
                table: "CashSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_CashSessions_CashRegisterId_Open",
                table: "CashSessions",
                column: "CashRegisterId",
                unique: true,
                filter: "[Status] = 0 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionAccruals_BusinessId",
                table: "CommissionAccruals",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionAccruals_EmployeeId",
                table: "CommissionAccruals",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionAccruals_SaleLineId",
                table: "CommissionAccruals",
                column: "SaleLineId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionPayouts_BusinessId",
                table: "CommissionPayouts",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionPayouts_EmployeeId",
                table: "CommissionPayouts",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRules_Business_Employee_Service",
                table: "CommissionRules",
                columns: new[] { "BusinessId", "EmployeeId", "ServiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRules_BusinessId",
                table: "CommissionRules",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationSuppressions_CustomerId",
                table: "CommunicationSuppressions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "UX_CommunicationSuppressions_Business_Customer_Channel",
                table: "CommunicationSuppressions",
                columns: new[] { "BusinessId", "CustomerId", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsentHistories_Business_Customer",
                table: "ConsentHistories",
                columns: new[] { "BusinessId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCommunicationPreferences_CustomerId",
                table: "CustomerCommunicationPreferences",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "UX_CommunicationPreferences_Business_Customer_Channel_Purpose",
                table: "CustomerCommunicationPreferences",
                columns: new[] { "BusinessId", "CustomerId", "Channel", "Purpose" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPackages_Business_Customer",
                table: "CustomerPackages",
                columns: new[] { "BusinessId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPackages_ExpiresAt",
                table: "CustomerPackages",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPackages_ServicePackageId",
                table: "CustomerPackages",
                column: "ServicePackageId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerServiceHistories_CustomerId",
                table: "CustomerServiceHistories",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerServiceHistories_LastCompletedAt",
                table: "CustomerServiceHistories",
                column: "LastCompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerServiceHistories_ServiceId",
                table: "CustomerServiceHistories",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "UX_CustomerServiceHistories_Business_Customer_Service",
                table: "CustomerServiceHistories",
                columns: new[] { "BusinessId", "CustomerId", "ServiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_BusinessId",
                table: "Expenses",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_BusinessId_ExpenseDate",
                table: "Expenses",
                columns: new[] { "BusinessId", "ExpenseDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_CategoryId",
                table: "Expenses",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ExpenseDate",
                table: "Expenses",
                column: "ExpenseDate");

            migrationBuilder.CreateIndex(
                name: "UX_FeatureDefinitions_Code",
                table: "FeatureDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeatureUsages_FeatureId",
                table: "FeatureUsages",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "UX_FeatureUsages_Business_Feature_Period",
                table: "FeatureUsages",
                columns: new[] { "BusinessId", "FeatureId", "UsagePeriod" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_BusinessId",
                table: "ImportJobs",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportRowResults_ImportJobId",
                table: "ImportRowResults",
                column: "ImportJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportRowResults_ImportJobId_Status",
                table: "ImportRowResults",
                columns: new[] { "ImportJobId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketingCampaigns_BusinessId",
                table: "MarketingCampaigns",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageUsages_AppointmentId",
                table: "PackageUsages",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageUsages_CustomerPackageId",
                table: "PackageUsages",
                column: "CustomerPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BusinessId",
                table: "Payments",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CashSessionId",
                table: "Payments",
                column: "CashSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentMethod",
                table: "Payments",
                column: "PaymentMethod");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RefersToPaymentId",
                table: "Payments",
                column: "RefersToPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SaleId",
                table: "Payments",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanFeatures_FeatureId",
                table: "PlanFeatures",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "UX_PlanFeatures_PlanId_FeatureId",
                table: "PlanFeatures",
                columns: new[] { "PlanId", "FeatureId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_BusinessId",
                table: "Products",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "UX_Products_BusinessId_Sku",
                table: "Products",
                columns: new[] { "BusinessId", "Sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecoveredSlots_BusinessId",
                table: "RecoveredSlots",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveredSlots_Status",
                table: "RecoveredSlots",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ReengagementAttempts_Business_Customer_Service",
                table: "ReengagementAttempts",
                columns: new[] { "BusinessId", "CustomerId", "ServiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReengagementAttempts_RuleId",
                table: "ReengagementAttempts",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ReengagementAttempts_TriggeredAt",
                table: "ReengagementAttempts",
                column: "TriggeredAt");

            migrationBuilder.CreateIndex(
                name: "IX_SaleLines_EmployeeId",
                table: "SaleLines",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleLines_ProductId",
                table: "SaleLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleLines_SaleId",
                table: "SaleLines",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleLines_ServiceId",
                table: "SaleLines",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_AppointmentId",
                table: "Sales",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_BusinessId",
                table: "Sales",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_BusinessId_SoldAt",
                table: "Sales",
                columns: new[] { "BusinessId", "SoldAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CustomerId",
                table: "Sales",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_Status",
                table: "Sales",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMaterials_ServiceId",
                table: "ServiceMaterials",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "UX_ServiceMaterials_Business_Service_Product",
                table: "ServiceMaterials",
                columns: new[] { "BusinessId", "ServiceId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ServicePackageItems_Package_Service",
                table: "ServicePackageItems",
                columns: new[] { "ServicePackageId", "ServiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServicePackages_BusinessId",
                table: "ServicePackages",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_BusinessId",
                table: "StockMovements",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductId",
                table: "StockMovements",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "UX_StockMovements_Reference",
                table: "StockMovements",
                columns: new[] { "ReferenceType", "ReferenceId" },
                unique: true,
                filter: "[ReferenceType] IS NOT NULL AND [ReferenceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingEvents_BusinessId_OccurredAt",
                table: "TrackingEvents",
                columns: new[] { "BusinessId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TrackingEvents_TrackingLinkId",
                table: "TrackingEvents",
                column: "TrackingLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingLinks_BusinessId",
                table: "TrackingLinks",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "UX_TrackingLinks_TrackingCode",
                table: "TrackingLinks",
                column: "TrackingCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_Business_Customer_Service",
                table: "WaitlistEntries",
                columns: new[] { "BusinessId", "CustomerId", "ServiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_Status",
                table: "WaitlistEntries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistOffers_Status",
                table: "WaitlistOffers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistOffers_WaitlistEntryId",
                table: "WaitlistOffers",
                column: "WaitlistEntryId");
        }

        /// <inheritdoç />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentAttributions");

            migrationBuilder.DropTable(
                name: "BusinessFeatureOverrides");

            migrationBuilder.DropTable(
                name: "BusinessPublicPages");

            migrationBuilder.DropTable(
                name: "BusinessSubscriptions");

            migrationBuilder.DropTable(
                name: "CampaignSpends");

            migrationBuilder.DropTable(
                name: "CashMovements");

            migrationBuilder.DropTable(
                name: "CashSessions");

            migrationBuilder.DropTable(
                name: "CommissionAccruals");

            migrationBuilder.DropTable(
                name: "CommissionPayouts");

            migrationBuilder.DropTable(
                name: "CommissionRules");

            migrationBuilder.DropTable(
                name: "CommunicationSuppressions");

            migrationBuilder.DropTable(
                name: "ConsentHistories");

            migrationBuilder.DropTable(
                name: "CustomerCommunicationPreferences");

            migrationBuilder.DropTable(
                name: "CustomerPackages");

            migrationBuilder.DropTable(
                name: "CustomerServiceHistories");

            migrationBuilder.DropTable(
                name: "Expenses");

            migrationBuilder.DropTable(
                name: "FeatureUsages");

            migrationBuilder.DropTable(
                name: "ImportJobs");

            migrationBuilder.DropTable(
                name: "ImportRowResults");

            migrationBuilder.DropTable(
                name: "MarketingCampaigns");

            migrationBuilder.DropTable(
                name: "PackageUsages");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "PlanFeatures");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "RecoveredSlots");

            migrationBuilder.DropTable(
                name: "ReengagementAttempts");

            migrationBuilder.DropTable(
                name: "SaleLines");

            migrationBuilder.DropTable(
                name: "ServiceMaterials");

            migrationBuilder.DropTable(
                name: "ServicePackageItems");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "TrackingEvents");

            migrationBuilder.DropTable(
                name: "TrackingLinks");

            migrationBuilder.DropTable(
                name: "WaitlistOffers");

            migrationBuilder.DropTable(
                name: "CashRegisters");

            migrationBuilder.DropTable(
                name: "FeatureDefinitions");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropTable(
                name: "BusinessReengagementRules");

            migrationBuilder.DropTable(
                name: "Sales");

            migrationBuilder.DropTable(
                name: "ServicePackages");

            migrationBuilder.DropTable(
                name: "WaitlistEntries");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cat_legacy_dependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceAssetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetAssetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DependencyType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, defaultValue: ""),
                    DiscoveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_legacy_dependencies", x => x.Id);
                    table.CheckConstraint("CK_cat_legacy_dependencies_source_asset_type", "\"SourceAssetType\" IN ('System', 'Program', 'Copybook', 'Transaction', 'Job', 'Artifact', 'Binding')");
                    table.CheckConstraint("CK_cat_legacy_dependencies_target_asset_type", "\"TargetAssetType\" IN ('System', 'Program', 'Copybook', 'Transaction', 'Job', 'Artifact', 'Binding')");
                });

            migrationBuilder.CreateTable(
                name: "cat_legacy_outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_legacy_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_mainframe_systems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false, defaultValue: ""),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, defaultValue: ""),
                    SysplexName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LparName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RegionName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TeamName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Domain = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TechnicalOwner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    BusinessOwner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    Criticality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Medium"),
                    LifecycleStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    OperatingSystem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    MipsCapacity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: ""),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_mainframe_systems", x => x.Id);
                    table.CheckConstraint("CK_cat_mainframe_systems_criticality", "\"Criticality\" IN ('Critical', 'High', 'Medium', 'Low')");
                    table.CheckConstraint("CK_cat_mainframe_systems_lifecycle_status", "\"LifecycleStatus\" IN ('Planning', 'Development', 'Staging', 'Active', 'Deprecating', 'Deprecated', 'Retired')");
                });

            migrationBuilder.CreateTable(
                name: "cat_cics_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false, defaultValue: ""),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, defaultValue: ""),
                    SystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgramName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    TransactionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Online"),
                    RegionName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CicsVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RegionPort = table.Column<int>(type: "integer", nullable: true),
                    CommareaLength = table.Column<int>(type: "integer", nullable: true),
                    Criticality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Medium"),
                    LifecycleStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_cics_transactions", x => x.Id);
                    table.CheckConstraint("CK_cat_cics_transactions_criticality", "\"Criticality\" IN ('Critical', 'High', 'Medium', 'Low')");
                    table.CheckConstraint("CK_cat_cics_transactions_lifecycle_status", "\"LifecycleStatus\" IN ('Planning', 'Development', 'Staging', 'Active', 'Deprecating', 'Deprecated', 'Retired')");
                    table.CheckConstraint("CK_cat_cics_transactions_transaction_type", "\"TransactionType\" IN ('Online', 'Conversational', 'Pseudo', 'Web', 'Channel')");
                    table.ForeignKey(
                        name: "FK_cat_cics_transactions_cat_mainframe_systems_SystemId",
                        column: x => x.SystemId,
                        principalTable: "cat_mainframe_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cat_cobol_programs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false, defaultValue: ""),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, defaultValue: ""),
                    SystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Language = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "COBOL"),
                    CompilerVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    LastCompiled = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SourceLibrary = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false, defaultValue: ""),
                    LoadModule = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    Criticality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Medium"),
                    LifecycleStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_cobol_programs", x => x.Id);
                    table.CheckConstraint("CK_cat_cobol_programs_criticality", "\"Criticality\" IN ('Critical', 'High', 'Medium', 'Low')");
                    table.CheckConstraint("CK_cat_cobol_programs_lifecycle_status", "\"LifecycleStatus\" IN ('Planning', 'Development', 'Staging', 'Active', 'Deprecating', 'Deprecated', 'Retired')");
                    table.ForeignKey(
                        name: "FK_cat_cobol_programs_cat_mainframe_systems_SystemId",
                        column: x => x.SystemId,
                        principalTable: "cat_mainframe_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cat_copybooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false, defaultValue: ""),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, defaultValue: ""),
                    SystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: ""),
                    LayoutFieldCount = table.Column<int>(type: "integer", nullable: false),
                    LayoutTotalLength = table.Column<int>(type: "integer", nullable: false),
                    LayoutRecordFormat = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SourceLibrary = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false, defaultValue: ""),
                    RawContent = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    Criticality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Medium"),
                    LifecycleStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_copybooks", x => x.Id);
                    table.CheckConstraint("CK_cat_copybooks_criticality", "\"Criticality\" IN ('Critical', 'High', 'Medium', 'Low')");
                    table.CheckConstraint("CK_cat_copybooks_lifecycle_status", "\"LifecycleStatus\" IN ('Planning', 'Development', 'Staging', 'Active', 'Deprecating', 'Deprecated', 'Retired')");
                    table.ForeignKey(
                        name: "FK_cat_copybooks_cat_mainframe_systems_SystemId",
                        column: x => x.SystemId,
                        principalTable: "cat_mainframe_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cat_db2_artifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false, defaultValue: ""),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, defaultValue: ""),
                    SystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtifactType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Table"),
                    SchemaName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    TablespaceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    DatabaseName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    Criticality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Medium"),
                    LifecycleStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_db2_artifacts", x => x.Id);
                    table.CheckConstraint("CK_cat_db2_artifacts_artifact_type", "\"ArtifactType\" IN ('Table', 'View', 'StoredProcedure', 'Index', 'Tablespace', 'Package')");
                    table.CheckConstraint("CK_cat_db2_artifacts_criticality", "\"Criticality\" IN ('Critical', 'High', 'Medium', 'Low')");
                    table.CheckConstraint("CK_cat_db2_artifacts_lifecycle_status", "\"LifecycleStatus\" IN ('Planning', 'Development', 'Staging', 'Active', 'Deprecating', 'Deprecated', 'Retired')");
                    table.ForeignKey(
                        name: "FK_cat_db2_artifacts_cat_mainframe_systems_SystemId",
                        column: x => x.SystemId,
                        principalTable: "cat_mainframe_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cat_ims_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false, defaultValue: ""),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, defaultValue: ""),
                    SystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "MPP"),
                    PsbName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    DbdName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    Criticality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Medium"),
                    LifecycleStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_ims_transactions", x => x.Id);
                    table.CheckConstraint("CK_cat_ims_transactions_criticality", "\"Criticality\" IN ('Critical', 'High', 'Medium', 'Low')");
                    table.CheckConstraint("CK_cat_ims_transactions_lifecycle_status", "\"LifecycleStatus\" IN ('Planning', 'Development', 'Staging', 'Active', 'Deprecating', 'Deprecated', 'Retired')");
                    table.CheckConstraint("CK_cat_ims_transactions_transaction_type", "\"TransactionType\" IN ('MPP', 'BMP', 'FastPath', 'IFP')");
                    table.ForeignKey(
                        name: "FK_cat_ims_transactions_cat_mainframe_systems_SystemId",
                        column: x => x.SystemId,
                        principalTable: "cat_mainframe_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cat_mq_contracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QueueName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MessageFormat = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PayloadSchema = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CopybookReference = table.Column<Guid>(type: "uuid", nullable: true),
                    MaxMessageLength = table.Column<int>(type: "integer", nullable: true),
                    HeaderFormat = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EncodingScheme = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_mq_contracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cat_mq_contracts_cat_mainframe_systems_SystemId",
                        column: x => x.SystemId,
                        principalTable: "cat_mainframe_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cat_zos_connect_bindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false, defaultValue: ""),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, defaultValue: ""),
                    SystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    OperationName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    HttpMethod = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: ""),
                    BasePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    TargetTransaction = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    RequestSchema = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    ResponseSchema = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    Criticality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Medium"),
                    LifecycleStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_zos_connect_bindings", x => x.Id);
                    table.CheckConstraint("CK_cat_zos_connect_bindings_criticality", "\"Criticality\" IN ('Critical', 'High', 'Medium', 'Low')");
                    table.CheckConstraint("CK_cat_zos_connect_bindings_lifecycle_status", "\"LifecycleStatus\" IN ('Planning', 'Development', 'Staging', 'Active', 'Deprecating', 'Deprecated', 'Retired')");
                    table.ForeignKey(
                        name: "FK_cat_zos_connect_bindings_cat_mainframe_systems_SystemId",
                        column: x => x.SystemId,
                        principalTable: "cat_mainframe_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cat_copybook_contract_mappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CopybookId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MappingType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_copybook_contract_mappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cat_copybook_contract_mappings_cat_copybooks_CopybookId",
                        column: x => x.CopybookId,
                        principalTable: "cat_copybooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cat_copybook_fields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CopybookId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    PicClause = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    Offset = table.Column<int>(type: "integer", nullable: false),
                    Length = table.Column<int>(type: "integer", nullable: false),
                    DataType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: ""),
                    IsRedefines = table.Column<bool>(type: "boolean", nullable: false),
                    RedefinesField = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OccursCount = table.Column<int>(type: "integer", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_copybook_fields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cat_copybook_fields_cat_copybooks_CopybookId",
                        column: x => x.CopybookId,
                        principalTable: "cat_copybooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cat_copybook_program_usages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    CopybookId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsageType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "COPY"),
                    DiscoveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_copybook_program_usages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cat_copybook_program_usages_cat_cobol_programs_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "cat_cobol_programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cat_copybook_program_usages_cat_copybooks_CopybookId",
                        column: x => x.CopybookId,
                        principalTable: "cat_copybooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cat_copybook_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CopybookId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionLabel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RawContent = table.Column<string>(type: "text", nullable: false),
                    FieldCount = table.Column<int>(type: "integer", nullable: false),
                    TotalLength = table.Column<int>(type: "integer", nullable: false),
                    RecordFormat = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: ""),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_copybook_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cat_copybook_versions_cat_copybooks_CopybookId",
                        column: x => x.CopybookId,
                        principalTable: "cat_copybooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cat_copybook_diffs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CopybookId = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeLevel = table.Column<int>(type: "integer", nullable: false),
                    BreakingChangeCount = table.Column<int>(type: "integer", nullable: false),
                    AdditiveChangeCount = table.Column<int>(type: "integer", nullable: false),
                    NonBreakingChangeCount = table.Column<int>(type: "integer", nullable: false),
                    ChangesJson = table.Column<string>(type: "jsonb", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_copybook_diffs", x => x.Id);
                    table.CheckConstraint("CK_cat_copybook_diffs_change_level", "\"ChangeLevel\" >= 0 AND \"ChangeLevel\" <= 4");
                    table.ForeignKey(
                        name: "FK_cat_copybook_diffs_cat_copybook_versions_BaseVersionId",
                        column: x => x.BaseVersionId,
                        principalTable: "cat_copybook_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cat_copybook_diffs_cat_copybook_versions_TargetVersionId",
                        column: x => x.TargetVersionId,
                        principalTable: "cat_copybook_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cat_copybook_diffs_cat_copybooks_CopybookId",
                        column: x => x.CopybookId,
                        principalTable: "cat_copybooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cat_cics_transactions_Criticality",
                table: "cat_cics_transactions",
                column: "Criticality");

            migrationBuilder.CreateIndex(
                name: "IX_cat_cics_transactions_LifecycleStatus",
                table: "cat_cics_transactions",
                column: "LifecycleStatus");

            migrationBuilder.CreateIndex(
                name: "IX_cat_cics_transactions_SystemId",
                table: "cat_cics_transactions",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_cics_transactions_TransactionId_SystemId",
                table: "cat_cics_transactions",
                columns: new[] { "TransactionId", "SystemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_cobol_programs_Criticality",
                table: "cat_cobol_programs",
                column: "Criticality");

            migrationBuilder.CreateIndex(
                name: "IX_cat_cobol_programs_LifecycleStatus",
                table: "cat_cobol_programs",
                column: "LifecycleStatus");

            migrationBuilder.CreateIndex(
                name: "IX_cat_cobol_programs_Name_SystemId",
                table: "cat_cobol_programs",
                columns: new[] { "Name", "SystemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_cobol_programs_SystemId",
                table: "cat_cobol_programs",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_copybook_contract_mappings_CopybookId",
                table: "cat_copybook_contract_mappings",
                column: "CopybookId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_copybook_contract_mappings_CopybookId_ContractVersionId",
                table: "cat_copybook_contract_mappings",
                columns: new[] { "CopybookId", "ContractVersionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_copybook_diffs_BaseVersionId_TargetVersionId",
                table: "cat_copybook_diffs",
                columns: new[] { "BaseVersionId", "TargetVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_cat_copybook_diffs_CopybookId",
                table: "cat_copybook_diffs",
                column: "CopybookId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_copybook_diffs_TargetVersionId",
                table: "cat_copybook_diffs",
                column: "TargetVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_copybook_fields_CopybookId",
                table: "cat_copybook_fields",
                column: "CopybookId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_copybook_fields_CopybookId_SortOrder",
                table: "cat_copybook_fields",
                columns: new[] { "CopybookId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_cat_copybook_program_usages_CopybookId",
                table: "cat_copybook_program_usages",
                column: "CopybookId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_copybook_program_usages_ProgramId_CopybookId",
                table: "cat_copybook_program_usages",
                columns: new[] { "ProgramId", "CopybookId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_copybook_versions_CopybookId",
                table: "cat_copybook_versions",
                column: "CopybookId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_copybook_versions_CopybookId_VersionLabel",
                table: "cat_copybook_versions",
                columns: new[] { "CopybookId", "VersionLabel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_copybooks_Criticality",
                table: "cat_copybooks",
                column: "Criticality");

            migrationBuilder.CreateIndex(
                name: "IX_cat_copybooks_LifecycleStatus",
                table: "cat_copybooks",
                column: "LifecycleStatus");

            migrationBuilder.CreateIndex(
                name: "IX_cat_copybooks_Name_SystemId",
                table: "cat_copybooks",
                columns: new[] { "Name", "SystemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_copybooks_SystemId",
                table: "cat_copybooks",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_db2_artifacts_ArtifactType",
                table: "cat_db2_artifacts",
                column: "ArtifactType");

            migrationBuilder.CreateIndex(
                name: "IX_cat_db2_artifacts_Criticality",
                table: "cat_db2_artifacts",
                column: "Criticality");

            migrationBuilder.CreateIndex(
                name: "IX_cat_db2_artifacts_LifecycleStatus",
                table: "cat_db2_artifacts",
                column: "LifecycleStatus");

            migrationBuilder.CreateIndex(
                name: "IX_cat_db2_artifacts_Name_SystemId",
                table: "cat_db2_artifacts",
                columns: new[] { "Name", "SystemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_db2_artifacts_SystemId",
                table: "cat_db2_artifacts",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_ims_transactions_Criticality",
                table: "cat_ims_transactions",
                column: "Criticality");

            migrationBuilder.CreateIndex(
                name: "IX_cat_ims_transactions_LifecycleStatus",
                table: "cat_ims_transactions",
                column: "LifecycleStatus");

            migrationBuilder.CreateIndex(
                name: "IX_cat_ims_transactions_SystemId",
                table: "cat_ims_transactions",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_ims_transactions_TransactionCode_SystemId",
                table: "cat_ims_transactions",
                columns: new[] { "TransactionCode", "SystemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_legacy_dependencies_SourceAssetId",
                table: "cat_legacy_dependencies",
                column: "SourceAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_legacy_dependencies_SourceAssetId_TargetAssetId_Depende~",
                table: "cat_legacy_dependencies",
                columns: new[] { "SourceAssetId", "TargetAssetId", "DependencyType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_legacy_dependencies_TargetAssetId",
                table: "cat_legacy_dependencies",
                column: "TargetAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_legacy_outbox_messages_CreatedAt",
                table: "cat_legacy_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_cat_legacy_outbox_messages_IdempotencyKey",
                table: "cat_legacy_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_legacy_outbox_messages_ProcessedAt",
                table: "cat_legacy_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_cat_mainframe_systems_Criticality",
                table: "cat_mainframe_systems",
                column: "Criticality");

            migrationBuilder.CreateIndex(
                name: "IX_cat_mainframe_systems_Domain",
                table: "cat_mainframe_systems",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_cat_mainframe_systems_LifecycleStatus",
                table: "cat_mainframe_systems",
                column: "LifecycleStatus");

            migrationBuilder.CreateIndex(
                name: "IX_cat_mainframe_systems_Name",
                table: "cat_mainframe_systems",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_mainframe_systems_TeamName",
                table: "cat_mainframe_systems",
                column: "TeamName");

            migrationBuilder.CreateIndex(
                name: "IX_cat_mq_contracts_QueueName_SystemId",
                table: "cat_mq_contracts",
                columns: new[] { "QueueName", "SystemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_mq_contracts_SystemId",
                table: "cat_mq_contracts",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_zos_connect_bindings_Criticality",
                table: "cat_zos_connect_bindings",
                column: "Criticality");

            migrationBuilder.CreateIndex(
                name: "IX_cat_zos_connect_bindings_LifecycleStatus",
                table: "cat_zos_connect_bindings",
                column: "LifecycleStatus");

            migrationBuilder.CreateIndex(
                name: "IX_cat_zos_connect_bindings_Name_SystemId",
                table: "cat_zos_connect_bindings",
                columns: new[] { "Name", "SystemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_zos_connect_bindings_SystemId",
                table: "cat_zos_connect_bindings",
                column: "SystemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cat_cics_transactions");

            migrationBuilder.DropTable(
                name: "cat_copybook_contract_mappings");

            migrationBuilder.DropTable(
                name: "cat_copybook_diffs");

            migrationBuilder.DropTable(
                name: "cat_copybook_fields");

            migrationBuilder.DropTable(
                name: "cat_copybook_program_usages");

            migrationBuilder.DropTable(
                name: "cat_db2_artifacts");

            migrationBuilder.DropTable(
                name: "cat_ims_transactions");

            migrationBuilder.DropTable(
                name: "cat_legacy_dependencies");

            migrationBuilder.DropTable(
                name: "cat_legacy_outbox_messages");

            migrationBuilder.DropTable(
                name: "cat_mq_contracts");

            migrationBuilder.DropTable(
                name: "cat_zos_connect_bindings");

            migrationBuilder.DropTable(
                name: "cat_copybook_versions");

            migrationBuilder.DropTable(
                name: "cat_cobol_programs");

            migrationBuilder.DropTable(
                name: "cat_copybooks");

            migrationBuilder.DropTable(
                name: "cat_mainframe_systems");
        }
    }
}

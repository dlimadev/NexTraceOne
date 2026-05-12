using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cat_consumer_assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_consumer_assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_discovered_services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ServiceNamespace = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TraceCount = table.Column<long>(type: "bigint", nullable: false),
                    EndpointCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MatchedServiceAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    DiscoveryRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    IgnoreReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_discovered_services", x => x.Id);
                    table.CheckConstraint("CK_cat_discovered_services_Status", "\"Status\" IN ('Pending', 'Matched', 'Ignored', 'Registered')");
                });

            migrationBuilder.CreateTable(
                name: "cat_discovery_match_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Pattern = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TargetServiceAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_discovery_match_rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_discovery_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ServicesFound = table.Column<int>(type: "integer", nullable: false),
                    NewServicesFound = table.Column<int>(type: "integer", nullable: false),
                    ErrorCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_discovery_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_dx_scores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TeamName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CycleTimeHours = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    DeploymentFrequencyPerWeek = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    CognitiveLoadScore = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: false),
                    ToilPercentage = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    OverallScore = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    ScoreLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_dx_scores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_framework_asset_details",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Language = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PackageManager = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ArtifactRegistryUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    LatestVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    MinSupportedVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    TargetPlatform = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    LicenseType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    BuildPipelineUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    ChangelogUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    KnownConsumerCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_framework_asset_details", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_graph_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NodesJson = table.Column<string>(type: "text", nullable: false),
                    EdgesJson = table.Column<string>(type: "text", nullable: false),
                    NodeCount = table.Column<int>(type: "integer", nullable: false),
                    EdgeCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_graph_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_linked_references",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetType = table.Column<int>(type: "integer", maxLength: 50, nullable: false),
                    ReferenceType = table.Column<int>(type: "integer", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_linked_references", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_node_health_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OverlayMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Score = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    FactorsJson = table.Column<string>(type: "text", nullable: false),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SourceSystem = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_node_health_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_outbox_messages",
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
                    table.PrimaryKey("PK_cat_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_productivity_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeploymentCount = table.Column<int>(type: "integer", nullable: false),
                    AverageCycleTimeHours = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    IncidentCount = table.Column<int>(type: "integer", nullable: false),
                    ManualStepsCount = table.Column<int>(type: "integer", nullable: false),
                    SnapshotSource = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_productivity_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_saved_graph_views",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsShared = table.Column<bool>(type: "boolean", nullable: false),
                    FiltersJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_saved_graph_views", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cat_service_assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false, defaultValue: ""),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, defaultValue: ""),
                    ServiceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "RestApi"),
                    Domain = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SystemArea = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    TeamName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TechnicalOwner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    BusinessOwner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    Criticality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Medium"),
                    LifecycleStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    ExposureType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Internal"),
                    DocumentationUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    RepositoryUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    SubDomain = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Capability = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    GitRepository = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    CiPipelineUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    InfrastructureProvider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    HostingPlatform = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    RuntimeLanguage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    RuntimeVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    SloTarget = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: ""),
                    DataClassification = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    RegulatoryScope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    ChangeFrequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: ""),
                    ProductOwner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    ContactChannel = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    OnCallRotationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    Tier = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Standard"),
                    LastOwnershipReviewAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_service_assets", x => x.Id);
                    table.CheckConstraint("CK_cat_service_assets_criticality", "\"Criticality\" IN ('Critical', 'High', 'Medium', 'Low')");
                    table.CheckConstraint("CK_cat_service_assets_exposure_type", "\"ExposureType\" IN ('Internal', 'Partner', 'Public')");
                    table.CheckConstraint("CK_cat_service_assets_lifecycle_status", "\"LifecycleStatus\" IN ('Planning', 'Development', 'Staging', 'Active', 'Deprecating', 'Deprecated', 'Retired')");
                    table.CheckConstraint("CK_cat_service_assets_service_type", "\"ServiceType\" IN ('RestApi', 'SoapService', 'KafkaProducer', 'KafkaConsumer', 'BackgroundService', 'ScheduledProcess', 'IntegrationComponent', 'SharedPlatformService', 'GraphqlApi', 'GrpcService', 'LegacySystem', 'Gateway', 'ThirdParty', 'CobolProgram', 'CicsTransaction', 'ImsTransaction', 'BatchJob', 'MainframeSystem', 'MqQueueManager', 'ZosConnectApi', 'Framework')");
                });

            migrationBuilder.CreateTable(
                name: "cat_api_assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RoutePattern = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Visibility = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OwnerServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDecommissioned = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_api_assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cat_api_assets_cat_service_assets_OwnerServiceId",
                        column: x => x.OwnerServiceId,
                        principalTable: "cat_service_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cat_service_interfaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, defaultValue: ""),
                    InterfaceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    ExposureScope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Internal"),
                    BasePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    TopicName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    WsdlNamespace = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    GrpcServiceName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false, defaultValue: ""),
                    ScheduleCron = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    EnvironmentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    SloTarget = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: ""),
                    RequiresContract = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AuthScheme = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "None"),
                    RateLimitPolicy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    DocumentationUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    DeprecationDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SunsetDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeprecationNotice = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_service_interfaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cat_service_interfaces_cat_service_assets_ServiceAssetId",
                        column: x => x.ServiceAssetId,
                        principalTable: "cat_service_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cat_service_links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    IconHint = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_service_links", x => x.Id);
                    table.CheckConstraint("CK_cat_service_links_category", "\"Category\" IN ('Repository', 'Documentation', 'CiCd', 'Monitoring', 'Wiki', 'SwaggerUi', 'ApiPortal', 'Backstage', 'Adr', 'Runbook', 'Changelog', 'Dashboard', 'Other')");
                    table.ForeignKey(
                        name: "FK_cat_service_links_cat_service_assets_ServiceAssetId",
                        column: x => x.ServiceAssetId,
                        principalTable: "cat_service_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cat_consumer_relationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    FirstObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_consumer_relationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cat_consumer_relationships_cat_api_assets_ApiAssetId",
                        column: x => x.ApiAssetId,
                        principalTable: "cat_api_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cat_discovery_sources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExternalReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DiscoveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_discovery_sources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cat_discovery_sources_cat_api_assets_ApiAssetId",
                        column: x => x.ApiAssetId,
                        principalTable: "cat_api_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cat_contract_bindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceInterfaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    BindingEnvironment = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    IsDefaultVersion = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActivatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeactivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeactivatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MigrationNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, defaultValue: ""),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_contract_bindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cat_contract_bindings_cat_service_interfaces_ServiceInterfa~",
                        column: x => x.ServiceInterfaceId,
                        principalTable: "cat_service_interfaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cat_api_assets_OwnerServiceId",
                table: "cat_api_assets",
                column: "OwnerServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_consumer_relationships_ApiAssetId",
                table: "cat_consumer_relationships",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_bindings_ContractVersionId",
                table: "cat_contract_bindings",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_bindings_IsDeleted",
                table: "cat_contract_bindings",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_bindings_ServiceInterfaceId",
                table: "cat_contract_bindings",
                column: "ServiceInterfaceId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_bindings_ServiceInterfaceId_Status",
                table: "cat_contract_bindings",
                columns: new[] { "ServiceInterfaceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_cat_contract_bindings_Status",
                table: "cat_contract_bindings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_cat_discovered_services_Environment",
                table: "cat_discovered_services",
                column: "Environment");

            migrationBuilder.CreateIndex(
                name: "IX_cat_discovered_services_LastSeenAt",
                table: "cat_discovered_services",
                column: "LastSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_cat_discovered_services_ServiceName_Environment",
                table: "cat_discovered_services",
                columns: new[] { "ServiceName", "Environment" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_discovered_services_Status",
                table: "cat_discovered_services",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_cat_discovery_match_rules_IsActive",
                table: "cat_discovery_match_rules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_cat_discovery_match_rules_IsActive_Priority",
                table: "cat_discovery_match_rules",
                columns: new[] { "IsActive", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_cat_discovery_runs_Environment",
                table: "cat_discovery_runs",
                column: "Environment");

            migrationBuilder.CreateIndex(
                name: "IX_cat_discovery_runs_StartedAt",
                table: "cat_discovery_runs",
                column: "StartedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_cat_discovery_sources_ApiAssetId",
                table: "cat_discovery_sources",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_dx_scores_Period",
                table: "cat_dx_scores",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_cat_dx_scores_ScoreLevel",
                table: "cat_dx_scores",
                column: "ScoreLevel");

            migrationBuilder.CreateIndex(
                name: "IX_cat_dx_scores_TeamId",
                table: "cat_dx_scores",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_framework_asset_details_Language",
                table: "cat_framework_asset_details",
                column: "Language");

            migrationBuilder.CreateIndex(
                name: "IX_cat_framework_asset_details_PackageName",
                table: "cat_framework_asset_details",
                column: "PackageName");

            migrationBuilder.CreateIndex(
                name: "IX_cat_framework_asset_details_ServiceAssetId",
                table: "cat_framework_asset_details",
                column: "ServiceAssetId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_graph_snapshots_CapturedAt",
                table: "cat_graph_snapshots",
                column: "CapturedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_cat_linked_references_AssetId_AssetType",
                table: "cat_linked_references",
                columns: new[] { "AssetId", "AssetType" });

            migrationBuilder.CreateIndex(
                name: "IX_cat_linked_references_ReferenceType",
                table: "cat_linked_references",
                column: "ReferenceType");

            migrationBuilder.CreateIndex(
                name: "IX_cat_node_health_records_CalculatedAt",
                table: "cat_node_health_records",
                column: "CalculatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_cat_node_health_records_NodeId_OverlayMode",
                table: "cat_node_health_records",
                columns: new[] { "NodeId", "OverlayMode" });

            migrationBuilder.CreateIndex(
                name: "IX_cat_outbox_messages_CreatedAt",
                table: "cat_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_cat_outbox_messages_IdempotencyKey",
                table: "cat_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_outbox_messages_ProcessedAt",
                table: "cat_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_cat_productivity_snapshots_PeriodEnd",
                table: "cat_productivity_snapshots",
                column: "PeriodEnd");

            migrationBuilder.CreateIndex(
                name: "IX_cat_productivity_snapshots_PeriodStart",
                table: "cat_productivity_snapshots",
                column: "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_cat_productivity_snapshots_TeamId",
                table: "cat_productivity_snapshots",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_saved_graph_views_OwnerId",
                table: "cat_saved_graph_views",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_saved_graph_views_OwnerId_IsShared",
                table: "cat_saved_graph_views",
                columns: new[] { "OwnerId", "IsShared" });

            migrationBuilder.CreateIndex(
                name: "IX_cat_service_assets_Criticality",
                table: "cat_service_assets",
                column: "Criticality");

            migrationBuilder.CreateIndex(
                name: "IX_cat_service_assets_Domain",
                table: "cat_service_assets",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_cat_service_assets_LifecycleStatus",
                table: "cat_service_assets",
                column: "LifecycleStatus");

            migrationBuilder.CreateIndex(
                name: "IX_cat_service_assets_Name",
                table: "cat_service_assets",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_service_assets_ServiceType",
                table: "cat_service_assets",
                column: "ServiceType");

            migrationBuilder.CreateIndex(
                name: "IX_cat_service_assets_SubDomain",
                table: "cat_service_assets",
                column: "SubDomain");

            migrationBuilder.CreateIndex(
                name: "IX_cat_service_assets_TeamName",
                table: "cat_service_assets",
                column: "TeamName");

            migrationBuilder.CreateIndex(
                name: "IX_cat_service_interfaces_InterfaceType",
                table: "cat_service_interfaces",
                column: "InterfaceType");

            migrationBuilder.CreateIndex(
                name: "IX_cat_service_interfaces_IsDeleted",
                table: "cat_service_interfaces",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_cat_service_interfaces_ServiceAssetId",
                table: "cat_service_interfaces",
                column: "ServiceAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_service_interfaces_ServiceAssetId_Status_InterfaceType",
                table: "cat_service_interfaces",
                columns: new[] { "ServiceAssetId", "Status", "InterfaceType" });

            migrationBuilder.CreateIndex(
                name: "IX_cat_service_interfaces_Status",
                table: "cat_service_interfaces",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_cat_service_links_ServiceAssetId",
                table: "cat_service_links",
                column: "ServiceAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_service_links_ServiceAssetId_Category",
                table: "cat_service_links",
                columns: new[] { "ServiceAssetId", "Category" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cat_consumer_assets");

            migrationBuilder.DropTable(
                name: "cat_consumer_relationships");

            migrationBuilder.DropTable(
                name: "cat_contract_bindings");

            migrationBuilder.DropTable(
                name: "cat_discovered_services");

            migrationBuilder.DropTable(
                name: "cat_discovery_match_rules");

            migrationBuilder.DropTable(
                name: "cat_discovery_runs");

            migrationBuilder.DropTable(
                name: "cat_discovery_sources");

            migrationBuilder.DropTable(
                name: "cat_dx_scores");

            migrationBuilder.DropTable(
                name: "cat_framework_asset_details");

            migrationBuilder.DropTable(
                name: "cat_graph_snapshots");

            migrationBuilder.DropTable(
                name: "cat_linked_references");

            migrationBuilder.DropTable(
                name: "cat_node_health_records");

            migrationBuilder.DropTable(
                name: "cat_outbox_messages");

            migrationBuilder.DropTable(
                name: "cat_productivity_snapshots");

            migrationBuilder.DropTable(
                name: "cat_saved_graph_views");

            migrationBuilder.DropTable(
                name: "cat_service_links");

            migrationBuilder.DropTable(
                name: "cat_service_interfaces");

            migrationBuilder.DropTable(
                name: "cat_api_assets");

            migrationBuilder.DropTable(
                name: "cat_service_assets");
        }
    }
}

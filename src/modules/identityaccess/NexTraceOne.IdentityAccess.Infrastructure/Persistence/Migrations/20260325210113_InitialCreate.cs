using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "env_environments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Profile = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Criticality = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsProductionLike = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsPrimaryProduction = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_env_environments", x => x.Id);
                    table.CheckConstraint("CK_env_environments_criticality", "\"Criticality\" BETWEEN 1 AND 4");
                    table.CheckConstraint("CK_env_environments_profile", "\"Profile\" BETWEEN 1 AND 9");
                    table.CheckConstraint("CK_env_environments_sort_order", "\"SortOrder\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "iam_access_review_campaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Deadline = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InitiatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_access_review_campaigns", x => x.Id);
                    table.CheckConstraint("CK_iam_access_review_campaigns_Status", "\"Status\" IN ('Open', 'Completed')");
                });

            migrationBuilder.CreateTable(
                name: "iam_break_glass_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Justification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    PostMortemNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    PostMortemAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_break_glass_requests", x => x.Id);
                    table.CheckConstraint("CK_iam_break_glass_requests_Status", "\"Status\" IN ('Active', 'Expired', 'Revoked', 'PostMortemCompleted')");
                });

            migrationBuilder.CreateTable(
                name: "iam_delegations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DelegateeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DelegatedPermissions = table.Column<string>(type: "jsonb", nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ValidFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ValidUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_delegations", x => x.Id);
                    table.CheckConstraint("CK_iam_delegations_Status", "\"Status\" IN ('Active', 'Expired', 'Revoked')");
                });

            migrationBuilder.CreateTable(
                name: "iam_external_identities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExternalUserId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ExternalEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    ExternalGroupsJson = table.Column<string>(type: "jsonb", nullable: true),
                    LastSyncAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_external_identities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "iam_jit_access_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionCode = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Scope = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Justification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ApprovalDeadline = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DecidedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    GrantedFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    GrantedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_jit_access_requests", x => x.Id);
                    table.CheckConstraint("CK_iam_jit_access_requests_Status", "\"Status\" IN ('Pending', 'Approved', 'Rejected', 'Expired', 'Revoked')");
                });

            migrationBuilder.CreateTable(
                name: "iam_outbox_messages",
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
                    table.PrimaryKey("PK_iam_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "iam_permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "iam_roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "iam_security_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RiskScore = table.Column<int>(type: "integer", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsReviewed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_security_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "iam_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefreshToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "iam_sso_group_mappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExternalGroupId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ExternalGroupName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_sso_group_mappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "iam_tenant_memberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_tenant_memberships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "iam_tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "iam_users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "integer", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MfaEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    MfaMethod = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    MfaSecret = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FederationProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "env_environment_accesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GrantedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    GrantedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_env_environment_accesses", x => x.Id);
                    table.CheckConstraint("CK_env_environment_accesses_access_level", "\"AccessLevel\" IN ('read', 'write', 'admin', 'none')");
                    table.ForeignKey(
                        name: "FK_env_environment_accesses_env_environments_EnvironmentId",
                        column: x => x.EnvironmentId,
                        principalTable: "env_environments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "iam_access_review_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Decision = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ReviewerComment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_access_review_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_iam_access_review_items_iam_access_review_campaigns_Campaig~",
                        column: x => x.CampaignId,
                        principalTable: "iam_access_review_campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "iam_permissions",
                columns: new[] { "Id", "Code", "Module", "Name" },
                values: new object[,]
                {
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c001"), "identity:users:read", "Identity", "Read users" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c002"), "identity:users:write", "Identity", "Create and update users" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c003"), "identity:roles:assign", "Identity", "Assign roles to users" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c004"), "identity:sessions:revoke", "Identity", "Revoke user sessions" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c005"), "identity:roles:read", "Identity", "View available roles" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c006"), "identity:sessions:read", "Identity", "View active sessions" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c007"), "identity:permissions:read", "Identity", "View available permissions" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c008"), "identity:jit-access:decide", "Identity", "Approve or reject JIT access requests" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c009"), "identity:break-glass:decide", "Identity", "Approve, revoke or audit break glass requests" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c00a"), "identity:delegations:manage", "Identity", "Create and revoke delegations" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c010"), "catalog:assets:read", "Catalog", "View service and API assets" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c011"), "catalog:assets:write", "Catalog", "Create and update assets" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c020"), "contracts:read", "Contracts", "View contract versions and diffs" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c021"), "contracts:write", "Contracts", "Create and update contracts" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c022"), "contracts:import", "Contracts", "Import contract files" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c030"), "change-intelligence:releases:read", "ChangeIntelligence", "View releases" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c031"), "change-intelligence:releases:write", "ChangeIntelligence", "Create and manage releases" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c032"), "change-intelligence:blast-radius:read", "ChangeIntelligence", "View blast radius reports" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c034"), "operations:incidents:read", "Operations", "View operational incidents" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c035"), "operations:incidents:write", "Operations", "Create and manage operational incidents" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c040"), "workflow:read", "Workflow", "View workflow instances and templates" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c041"), "workflow:write", "Workflow", "Create and configure workflows" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c042"), "workflow:approve", "Workflow", "Approve or reject workflow stages" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c050"), "promotion:read", "Promotion", "View promotion requests and gates" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c051"), "promotion:write", "Promotion", "Create promotion requests" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c052"), "promotion:promote", "Promotion", "Execute environment promotions" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c060"), "ruleset-governance:read", "RulesetGovernance", "View rulesets and bindings" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c061"), "ruleset-governance:write", "RulesetGovernance", "Manage rulesets and bindings" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c070"), "audit:read", "Audit", "View audit trail" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c071"), "audit:export", "Audit", "Export audit data" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c090"), "platform:settings:read", "Platform", "View platform settings" },
                    { new Guid("2e91a557-fade-46df-b248-0f5f5899c091"), "platform:settings:write", "Platform", "Manage platform settings" }
                });

            migrationBuilder.InsertData(
                table: "iam_roles",
                columns: new[] { "Id", "Description", "IsSystem", "Name" },
                values: new object[,]
                {
                    { new Guid("1e91a557-fade-46df-b248-0f5f5899c001"), "Full platform administration access", true, "PlatformAdmin" },
                    { new Guid("1e91a557-fade-46df-b248-0f5f5899c002"), "Technical leadership with approval and governance", true, "TechLead" },
                    { new Guid("1e91a557-fade-46df-b248-0f5f5899c003"), "Development access with contract management", true, "Developer" },
                    { new Guid("1e91a557-fade-46df-b248-0f5f5899c004"), "Read-only access across modules", true, "Viewer" },
                    { new Guid("1e91a557-fade-46df-b248-0f5f5899c005"), "Audit and compliance access", true, "Auditor" },
                    { new Guid("1e91a557-fade-46df-b248-0f5f5899c006"), "Security review and session management", true, "SecurityReview" },
                    { new Guid("1e91a557-fade-46df-b248-0f5f5899c007"), "Restricted to workflow approvals only", true, "ApprovalOnly" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_env_environment_accesses_active_expires",
                table: "env_environment_accesses",
                columns: new[] { "IsActive", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_env_environment_accesses_env_id",
                table: "env_environment_accesses",
                column: "EnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_env_environment_accesses_unique_active",
                table: "env_environment_accesses",
                columns: new[] { "UserId", "EnvironmentId", "TenantId" },
                unique: true,
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_env_environment_accesses_user_active",
                table: "env_environment_accesses",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_env_environment_accesses_user_tenant_env",
                table: "env_environment_accesses",
                columns: new[] { "UserId", "TenantId", "EnvironmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_env_environments_not_deleted",
                table: "env_environments",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_env_environments_tenant_active",
                table: "env_environments",
                columns: new[] { "TenantId", "IsActive" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_env_environments_tenant_criticality",
                table: "env_environments",
                columns: new[] { "TenantId", "Criticality" });

            migrationBuilder.CreateIndex(
                name: "IX_env_environments_tenant_primary_production_unique",
                table: "env_environments",
                columns: new[] { "TenantId", "IsPrimaryProduction" },
                unique: true,
                filter: "\"IsPrimaryProduction\" = true AND \"IsActive\" = true AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_env_environments_tenant_profile",
                table: "env_environments",
                columns: new[] { "TenantId", "Profile" });

            migrationBuilder.CreateIndex(
                name: "IX_env_environments_tenant_slug",
                table: "env_environments",
                columns: new[] { "TenantId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_iam_access_review_campaigns_TenantId_Status",
                table: "iam_access_review_campaigns",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_iam_access_review_items_CampaignId",
                table: "iam_access_review_items",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_iam_access_review_items_Decision",
                table: "iam_access_review_items",
                column: "Decision");

            migrationBuilder.CreateIndex(
                name: "IX_iam_access_review_items_ReviewerId",
                table: "iam_access_review_items",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_iam_break_glass_requests_RequestedBy_Status",
                table: "iam_break_glass_requests",
                columns: new[] { "RequestedBy", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_iam_break_glass_requests_Status",
                table: "iam_break_glass_requests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_iam_break_glass_requests_TenantId",
                table: "iam_break_glass_requests",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_iam_delegations_DelegateeId",
                table: "iam_delegations",
                column: "DelegateeId");

            migrationBuilder.CreateIndex(
                name: "IX_iam_delegations_GrantorId",
                table: "iam_delegations",
                column: "GrantorId");

            migrationBuilder.CreateIndex(
                name: "IX_iam_delegations_TenantId_Status",
                table: "iam_delegations",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_iam_external_identities_Provider_ExternalUserId",
                table: "iam_external_identities",
                columns: new[] { "Provider", "ExternalUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_iam_external_identities_UserId",
                table: "iam_external_identities",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_iam_jit_access_requests_RequestedBy",
                table: "iam_jit_access_requests",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_iam_jit_access_requests_Status",
                table: "iam_jit_access_requests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_iam_jit_access_requests_TenantId_Status",
                table: "iam_jit_access_requests",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_iam_outbox_messages_CreatedAt",
                table: "iam_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_iam_outbox_messages_IdempotencyKey",
                table: "iam_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_iam_outbox_messages_ProcessedAt",
                table: "iam_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_iam_permissions_Code",
                table: "iam_permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_iam_roles_Name",
                table: "iam_roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_iam_security_events_RiskScore",
                table: "iam_security_events",
                column: "RiskScore");

            migrationBuilder.CreateIndex(
                name: "IX_iam_security_events_TenantId_EventType",
                table: "iam_security_events",
                columns: new[] { "TenantId", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_iam_security_events_TenantId_OccurredAt",
                table: "iam_security_events",
                columns: new[] { "TenantId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_iam_security_events_UserId",
                table: "iam_security_events",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_iam_sessions_RefreshToken",
                table: "iam_sessions",
                column: "RefreshToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_iam_sessions_UserId",
                table: "iam_sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_iam_sso_group_mappings_TenantId",
                table: "iam_sso_group_mappings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_iam_sso_group_mappings_TenantId_Provider_ExternalGroupId",
                table: "iam_sso_group_mappings",
                columns: new[] { "TenantId", "Provider", "ExternalGroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_iam_tenant_memberships_TenantId",
                table: "iam_tenant_memberships",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_iam_tenant_memberships_UserId_TenantId",
                table: "iam_tenant_memberships",
                columns: new[] { "UserId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_iam_tenants_slug",
                table: "iam_tenants",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_iam_users_Email",
                table: "iam_users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "env_environment_accesses");

            migrationBuilder.DropTable(
                name: "iam_access_review_items");

            migrationBuilder.DropTable(
                name: "iam_break_glass_requests");

            migrationBuilder.DropTable(
                name: "iam_delegations");

            migrationBuilder.DropTable(
                name: "iam_external_identities");

            migrationBuilder.DropTable(
                name: "iam_jit_access_requests");

            migrationBuilder.DropTable(
                name: "iam_outbox_messages");

            migrationBuilder.DropTable(
                name: "iam_permissions");

            migrationBuilder.DropTable(
                name: "iam_roles");

            migrationBuilder.DropTable(
                name: "iam_security_events");

            migrationBuilder.DropTable(
                name: "iam_sessions");

            migrationBuilder.DropTable(
                name: "iam_sso_group_mappings");

            migrationBuilder.DropTable(
                name: "iam_tenant_memberships");

            migrationBuilder.DropTable(
                name: "iam_tenants");

            migrationBuilder.DropTable(
                name: "iam_users");

            migrationBuilder.DropTable(
                name: "env_environments");

            migrationBuilder.DropTable(
                name: "iam_access_review_campaigns");
        }
    }
}

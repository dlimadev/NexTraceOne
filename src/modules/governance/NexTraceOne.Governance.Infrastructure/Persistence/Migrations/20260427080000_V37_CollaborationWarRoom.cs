using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Governance.Infrastructure.Persistence.Migrations;

/// <summary>
/// Wave V3.7 — Real-time Collaboration &amp; War Room.
/// Adiciona:
///   - Tabela gov_presence_sessions (sessões de presença em tempo real)
///   - Tabela gov_dashboard_comments (comentários ancorados em widgets — V3.7)
/// </summary>
public partial class V37_CollaborationWarRoom : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "gov_presence_sessions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ResourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                AvatarColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                LeftAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_gov_presence_sessions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_gov_presence_resource_active",
            table: "gov_presence_sessions",
            columns: new[] { "tenant_id", "ResourceType", "ResourceId", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "ix_gov_presence_user_active",
            table: "gov_presence_sessions",
            columns: new[] { "tenant_id", "UserId", "IsActive" });

        migrationBuilder.CreateTable(
            name: "gov_dashboard_comments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DashboardId = table.Column<Guid>(type: "uuid", nullable: false),
                WidgetId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                AuthorUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                MentionsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                ParentCommentId = table.Column<Guid>(type: "uuid", nullable: true),
                IsResolved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                ResolvedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                EditedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_gov_dashboard_comments", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_gov_comment_dashboard_tenant",
            table: "gov_dashboard_comments",
            columns: new[] { "DashboardId", "tenant_id" });

        migrationBuilder.CreateIndex(
            name: "ix_gov_comment_widget",
            table: "gov_dashboard_comments",
            columns: new[] { "DashboardId", "WidgetId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "gov_presence_sessions");
        migrationBuilder.DropTable(name: "gov_dashboard_comments");
    }
}

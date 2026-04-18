using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Governance.Infrastructure.Persistence.Migrations;

/// <summary>
/// Migra CustomDashboards para suportar widgets ricos com posição no grid e configuração contextual.
/// Substitui WidgetIds (string[]) por Widgets (DashboardWidget[] JSONB).
/// Adiciona TeamId e IsSystem para suporte a dashboards de equipa e de sistema.
/// </summary>
public partial class AddDashboardWidgetsAndTeamId : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Adiciona coluna Widgets (JSONB) com valor por defeito de array vazio
        migrationBuilder.AddColumn<string>(
            name: "Widgets",
            table: "gov_custom_dashboards",
            type: "jsonb",
            nullable: false,
            defaultValue: "[]");

        // Adiciona TeamId opcional
        migrationBuilder.AddColumn<string>(
            name: "TeamId",
            table: "gov_custom_dashboards",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        // Adiciona IsSystem (default false)
        migrationBuilder.AddColumn<bool>(
            name: "IsSystem",
            table: "gov_custom_dashboards",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        // Backfill: converte WidgetIds (string[]) em Widgets (DashboardWidget[]) com posições automáticas
        migrationBuilder.Sql(@"
            UPDATE gov_custom_dashboards
            SET ""Widgets"" = COALESCE(
                (
                    SELECT jsonb_agg(
                        jsonb_build_object(
                            'widgetId', gen_random_uuid()::text,
                            'type', widget_type,
                            'position', jsonb_build_object(
                                'x', 0,
                                'y', (row_num - 1) * 2,
                                'width', 2,
                                'height', 2
                            ),
                            'config', jsonb_build_object(
                                'serviceId', null,
                                'teamId', null,
                                'timeRange', '24h',
                                'customTitle', null
                            )
                        )
                    )
                    FROM (
                        SELECT value AS widget_type,
                               ROW_NUMBER() OVER () AS row_num
                        FROM jsonb_array_elements_text(""WidgetIds"")
                    ) sub
                ),
                '[]'::jsonb
            )
            WHERE ""WidgetIds"" IS NOT NULL AND ""WidgetIds"" != '[]'::jsonb;
        ");

        // Remove a coluna WidgetIds (substituída por Widgets)
        migrationBuilder.DropColumn(
            name: "WidgetIds",
            table: "gov_custom_dashboards");

        // Índice para suporte a dashboards de equipa
        migrationBuilder.CreateIndex(
            name: "IX_gov_custom_dashboards_TeamId",
            table: "gov_custom_dashboards",
            column: "TeamId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_gov_custom_dashboards_TeamId",
            table: "gov_custom_dashboards");

        migrationBuilder.DropColumn(
            name: "IsSystem",
            table: "gov_custom_dashboards");

        migrationBuilder.DropColumn(
            name: "TeamId",
            table: "gov_custom_dashboards");

        // Restaura coluna WidgetIds como array vazio
        migrationBuilder.AddColumn<string>(
            name: "WidgetIds",
            table: "gov_custom_dashboards",
            type: "jsonb",
            nullable: false,
            defaultValue: "[]");

        // Extrai tipos de widget da coluna Widgets para restaurar WidgetIds
        migrationBuilder.Sql(@"
            UPDATE gov_custom_dashboards
            SET ""WidgetIds"" = COALESCE(
                (SELECT jsonb_agg(w->>'type') FROM jsonb_array_elements(""Widgets"") AS w),
                '[]'::jsonb
            );
        ");

        migrationBuilder.DropColumn(
            name: "Widgets",
            table: "gov_custom_dashboards");
    }
}

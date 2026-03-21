using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandProviderAndModelEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthenticationMode",
                table: "AiProviders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HealthStatus",
                table: "AiProviders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "AiProviders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "SupportsChat",
                table: "AiProviders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsEmbeddings",
                table: "AiProviders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsStructuredOutput",
                table: "AiProviders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsTools",
                table: "AiProviders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsVision",
                table: "AiProviders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TimeoutSeconds",
                table: "AiProviders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "ai_gov_models",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ComplianceStatus",
                table: "ai_gov_models",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ContextWindow",
                table: "ai_gov_models",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalModelId",
                table: "ai_gov_models",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDefaultForChat",
                table: "ai_gov_models",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefaultForEmbeddings",
                table: "ai_gov_models",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefaultForReasoning",
                table: "ai_gov_models",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsInstalled",
                table: "ai_gov_models",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LicenseName",
                table: "ai_gov_models",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LicenseUrl",
                table: "ai_gov_models",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ProviderId",
                table: "ai_gov_models",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RecommendedRamGb",
                table: "ai_gov_models",
                type: "numeric(5,1)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresGpu",
                table: "ai_gov_models",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "ai_gov_models",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "SupportsEmbeddings",
                table: "ai_gov_models",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsStreaming",
                table: "ai_gov_models",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsStructuredOutput",
                table: "ai_gov_models",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsToolCalling",
                table: "ai_gov_models",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsVision",
                table: "ai_gov_models",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_AiProviders_Slug",
                table: "AiProviders",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_models_IsDefaultForChat",
                table: "ai_gov_models",
                column: "IsDefaultForChat");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_models_ProviderId",
                table: "ai_gov_models",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_gov_models_Slug",
                table: "ai_gov_models",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AiProviders_Slug",
                table: "AiProviders");

            migrationBuilder.DropIndex(
                name: "IX_ai_gov_models_IsDefaultForChat",
                table: "ai_gov_models");

            migrationBuilder.DropIndex(
                name: "IX_ai_gov_models_ProviderId",
                table: "ai_gov_models");

            migrationBuilder.DropIndex(
                name: "IX_ai_gov_models_Slug",
                table: "ai_gov_models");

            migrationBuilder.DropColumn(
                name: "AuthenticationMode",
                table: "AiProviders");

            migrationBuilder.DropColumn(
                name: "HealthStatus",
                table: "AiProviders");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "AiProviders");

            migrationBuilder.DropColumn(
                name: "SupportsChat",
                table: "AiProviders");

            migrationBuilder.DropColumn(
                name: "SupportsEmbeddings",
                table: "AiProviders");

            migrationBuilder.DropColumn(
                name: "SupportsStructuredOutput",
                table: "AiProviders");

            migrationBuilder.DropColumn(
                name: "SupportsTools",
                table: "AiProviders");

            migrationBuilder.DropColumn(
                name: "SupportsVision",
                table: "AiProviders");

            migrationBuilder.DropColumn(
                name: "TimeoutSeconds",
                table: "AiProviders");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "ai_gov_models");

            migrationBuilder.DropColumn(
                name: "ComplianceStatus",
                table: "ai_gov_models");

            migrationBuilder.DropColumn(
                name: "ContextWindow",
                table: "ai_gov_models");

            migrationBuilder.DropColumn(
                name: "ExternalModelId",
                table: "ai_gov_models");

            migrationBuilder.DropColumn(
                name: "IsDefaultForChat",
                table: "ai_gov_models");

            migrationBuilder.DropColumn(
                name: "IsDefaultForEmbeddings",
                table: "ai_gov_models");

            migrationBuilder.DropColumn(
                name: "IsDefaultForReasoning",
                table: "ai_gov_models");

            migrationBuilder.DropColumn(
                name: "IsInstalled",
                table: "ai_gov_models");

            migrationBuilder.DropColumn(
                name: "LicenseName",
                table: "ai_gov_models");

            migrationBuilder.DropColumn(
                name: "LicenseUrl",
                table: "ai_gov_models");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                table: "ai_gov_models");

            migrationBuilder.DropColumn(
                name: "RecommendedRamGb",
                table: "ai_gov_models");

            migrationBuilder.DropColumn(
                name: "RequiresGpu",
                table: "ai_gov_models");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "ai_gov_models");

            migrationBuilder.DropColumn(
                name: "SupportsEmbeddings",
                table: "ai_gov_models");

            migrationBuilder.DropColumn(
                name: "SupportsStreaming",
                table: "ai_gov_models");

            migrationBuilder.DropColumn(
                name: "SupportsStructuredOutput",
                table: "ai_gov_models");

            migrationBuilder.DropColumn(
                name: "SupportsToolCalling",
                table: "ai_gov_models");

            migrationBuilder.DropColumn(
                name: "SupportsVision",
                table: "ai_gov_models");
        }
    }
}

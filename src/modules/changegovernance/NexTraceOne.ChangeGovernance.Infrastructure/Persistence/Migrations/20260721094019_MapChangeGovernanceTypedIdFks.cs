using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.ChangeGovernance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MapChangeGovernanceTypedIdFks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReleaseId",
                table: "WorkItemAssociations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "WorkflowInstanceId",
                table: "WorkflowStages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "WorkflowTemplateId",
                table: "WorkflowInstances",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "WorkflowTemplateId",
                table: "SlaPolicies",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "RulesetId",
                table: "RulesetBindings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ReleaseId",
                table: "RollbackAssessments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ReleaseId",
                table: "ReleaseBaselines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SourceEnvironmentId",
                table: "PromotionRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TargetEnvironmentId",
                table: "PromotionRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "DeploymentEnvironmentId",
                table: "PromotionGates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "GateId",
                table: "PromotionGateEvaluations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ReleaseId",
                table: "PostReleaseReviews",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ReleaseId",
                table: "ObservationWindows",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "RulesetId",
                table: "LintResults",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "PromotionGateId",
                table: "GateEvaluations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "PromotionRequestId",
                table: "GateEvaluations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ReleaseId",
                table: "FeatureFlagStates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ReleaseId",
                table: "ExternalMarkers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ReleaseId",
                table: "ChangeScores",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ReleaseId",
                table: "ChangeEvents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ReleaseId",
                table: "ChangeConfidenceEvents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ReleaseId",
                table: "CanaryRollouts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ReleaseId",
                table: "BlastRadiusReports",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ReleaseId",
                table: "ApprovalRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemAssociations_ReleaseId",
                table: "WorkItemAssociations",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStages_WorkflowInstanceId",
                table: "WorkflowStages",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstances_WorkflowTemplateId",
                table: "WorkflowInstances",
                column: "WorkflowTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaPolicies_WorkflowTemplateId",
                table: "SlaPolicies",
                column: "WorkflowTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_RulesetBindings_RulesetId",
                table: "RulesetBindings",
                column: "RulesetId");

            migrationBuilder.CreateIndex(
                name: "IX_RollbackAssessments_ReleaseId",
                table: "RollbackAssessments",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseBaselines_ReleaseId",
                table: "ReleaseBaselines",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRequests_SourceEnvironmentId",
                table: "PromotionRequests",
                column: "SourceEnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRequests_TargetEnvironmentId",
                table: "PromotionRequests",
                column: "TargetEnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionGates_DeploymentEnvironmentId",
                table: "PromotionGates",
                column: "DeploymentEnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionGateEvaluations_GateId",
                table: "PromotionGateEvaluations",
                column: "GateId");

            migrationBuilder.CreateIndex(
                name: "IX_PostReleaseReviews_ReleaseId",
                table: "PostReleaseReviews",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ObservationWindows_ReleaseId",
                table: "ObservationWindows",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_LintResults_RulesetId",
                table: "LintResults",
                column: "RulesetId");

            migrationBuilder.CreateIndex(
                name: "IX_GateEvaluations_PromotionGateId",
                table: "GateEvaluations",
                column: "PromotionGateId");

            migrationBuilder.CreateIndex(
                name: "IX_GateEvaluations_PromotionRequestId",
                table: "GateEvaluations",
                column: "PromotionRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlagStates_ReleaseId",
                table: "FeatureFlagStates",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalMarkers_ReleaseId",
                table: "ExternalMarkers",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeScores_ReleaseId",
                table: "ChangeScores",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeEvents_ReleaseId",
                table: "ChangeEvents",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeConfidenceEvents_ReleaseId",
                table: "ChangeConfidenceEvents",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CanaryRollouts_ReleaseId",
                table: "CanaryRollouts",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_BlastRadiusReports_ReleaseId",
                table: "BlastRadiusReports",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_ReleaseId",
                table: "ApprovalRequests",
                column: "ReleaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkItemAssociations_ReleaseId",
                table: "WorkItemAssociations");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowStages_WorkflowInstanceId",
                table: "WorkflowStages");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowInstances_WorkflowTemplateId",
                table: "WorkflowInstances");

            migrationBuilder.DropIndex(
                name: "IX_SlaPolicies_WorkflowTemplateId",
                table: "SlaPolicies");

            migrationBuilder.DropIndex(
                name: "IX_RulesetBindings_RulesetId",
                table: "RulesetBindings");

            migrationBuilder.DropIndex(
                name: "IX_RollbackAssessments_ReleaseId",
                table: "RollbackAssessments");

            migrationBuilder.DropIndex(
                name: "IX_ReleaseBaselines_ReleaseId",
                table: "ReleaseBaselines");

            migrationBuilder.DropIndex(
                name: "IX_PromotionRequests_SourceEnvironmentId",
                table: "PromotionRequests");

            migrationBuilder.DropIndex(
                name: "IX_PromotionRequests_TargetEnvironmentId",
                table: "PromotionRequests");

            migrationBuilder.DropIndex(
                name: "IX_PromotionGates_DeploymentEnvironmentId",
                table: "PromotionGates");

            migrationBuilder.DropIndex(
                name: "IX_PromotionGateEvaluations_GateId",
                table: "PromotionGateEvaluations");

            migrationBuilder.DropIndex(
                name: "IX_PostReleaseReviews_ReleaseId",
                table: "PostReleaseReviews");

            migrationBuilder.DropIndex(
                name: "IX_ObservationWindows_ReleaseId",
                table: "ObservationWindows");

            migrationBuilder.DropIndex(
                name: "IX_LintResults_RulesetId",
                table: "LintResults");

            migrationBuilder.DropIndex(
                name: "IX_GateEvaluations_PromotionGateId",
                table: "GateEvaluations");

            migrationBuilder.DropIndex(
                name: "IX_GateEvaluations_PromotionRequestId",
                table: "GateEvaluations");

            migrationBuilder.DropIndex(
                name: "IX_FeatureFlagStates_ReleaseId",
                table: "FeatureFlagStates");

            migrationBuilder.DropIndex(
                name: "IX_ExternalMarkers_ReleaseId",
                table: "ExternalMarkers");

            migrationBuilder.DropIndex(
                name: "IX_ChangeScores_ReleaseId",
                table: "ChangeScores");

            migrationBuilder.DropIndex(
                name: "IX_ChangeEvents_ReleaseId",
                table: "ChangeEvents");

            migrationBuilder.DropIndex(
                name: "IX_ChangeConfidenceEvents_ReleaseId",
                table: "ChangeConfidenceEvents");

            migrationBuilder.DropIndex(
                name: "IX_CanaryRollouts_ReleaseId",
                table: "CanaryRollouts");

            migrationBuilder.DropIndex(
                name: "IX_BlastRadiusReports_ReleaseId",
                table: "BlastRadiusReports");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalRequests_ReleaseId",
                table: "ApprovalRequests");

            migrationBuilder.DropColumn(
                name: "ReleaseId",
                table: "WorkItemAssociations");

            migrationBuilder.DropColumn(
                name: "WorkflowInstanceId",
                table: "WorkflowStages");

            migrationBuilder.DropColumn(
                name: "WorkflowTemplateId",
                table: "WorkflowInstances");

            migrationBuilder.DropColumn(
                name: "WorkflowTemplateId",
                table: "SlaPolicies");

            migrationBuilder.DropColumn(
                name: "RulesetId",
                table: "RulesetBindings");

            migrationBuilder.DropColumn(
                name: "ReleaseId",
                table: "RollbackAssessments");

            migrationBuilder.DropColumn(
                name: "ReleaseId",
                table: "ReleaseBaselines");

            migrationBuilder.DropColumn(
                name: "SourceEnvironmentId",
                table: "PromotionRequests");

            migrationBuilder.DropColumn(
                name: "TargetEnvironmentId",
                table: "PromotionRequests");

            migrationBuilder.DropColumn(
                name: "DeploymentEnvironmentId",
                table: "PromotionGates");

            migrationBuilder.DropColumn(
                name: "GateId",
                table: "PromotionGateEvaluations");

            migrationBuilder.DropColumn(
                name: "ReleaseId",
                table: "PostReleaseReviews");

            migrationBuilder.DropColumn(
                name: "ReleaseId",
                table: "ObservationWindows");

            migrationBuilder.DropColumn(
                name: "RulesetId",
                table: "LintResults");

            migrationBuilder.DropColumn(
                name: "PromotionGateId",
                table: "GateEvaluations");

            migrationBuilder.DropColumn(
                name: "PromotionRequestId",
                table: "GateEvaluations");

            migrationBuilder.DropColumn(
                name: "ReleaseId",
                table: "FeatureFlagStates");

            migrationBuilder.DropColumn(
                name: "ReleaseId",
                table: "ExternalMarkers");

            migrationBuilder.DropColumn(
                name: "ReleaseId",
                table: "ChangeScores");

            migrationBuilder.DropColumn(
                name: "ReleaseId",
                table: "ChangeEvents");

            migrationBuilder.DropColumn(
                name: "ReleaseId",
                table: "ChangeConfidenceEvents");

            migrationBuilder.DropColumn(
                name: "ReleaseId",
                table: "CanaryRollouts");

            migrationBuilder.DropColumn(
                name: "ReleaseId",
                table: "BlastRadiusReports");

            migrationBuilder.DropColumn(
                name: "ReleaseId",
                table: "ApprovalRequests");
        }
    }
}

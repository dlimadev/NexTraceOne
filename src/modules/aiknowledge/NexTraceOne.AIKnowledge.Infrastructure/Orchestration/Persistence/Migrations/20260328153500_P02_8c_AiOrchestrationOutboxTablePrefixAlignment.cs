using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P02_8c_AiOrchestrationOutboxTablePrefixAlignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_aik_orch_outbox_messages",
                table: "aik_orch_outbox_messages");

            migrationBuilder.RenameTable(
                name: "aik_orch_outbox_messages",
                newName: "aik_outbox_messages");

            migrationBuilder.RenameIndex(
                name: "IX_aik_orch_outbox_messages_ProcessedAt",
                table: "aik_outbox_messages",
                newName: "IX_aik_outbox_messages_ProcessedAt");

            migrationBuilder.RenameIndex(
                name: "IX_aik_orch_outbox_messages_IdempotencyKey",
                table: "aik_outbox_messages",
                newName: "IX_aik_outbox_messages_IdempotencyKey");

            migrationBuilder.RenameIndex(
                name: "IX_aik_orch_outbox_messages_CreatedAt",
                table: "aik_outbox_messages",
                newName: "IX_aik_outbox_messages_CreatedAt");

            migrationBuilder.AddPrimaryKey(
                name: "PK_aik_outbox_messages",
                table: "aik_outbox_messages",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_aik_outbox_messages",
                table: "aik_outbox_messages");

            migrationBuilder.RenameTable(
                name: "aik_outbox_messages",
                newName: "aik_orch_outbox_messages");

            migrationBuilder.RenameIndex(
                name: "IX_aik_outbox_messages_ProcessedAt",
                table: "aik_orch_outbox_messages",
                newName: "IX_aik_orch_outbox_messages_ProcessedAt");

            migrationBuilder.RenameIndex(
                name: "IX_aik_outbox_messages_IdempotencyKey",
                table: "aik_orch_outbox_messages",
                newName: "IX_aik_orch_outbox_messages_IdempotencyKey");

            migrationBuilder.RenameIndex(
                name: "IX_aik_outbox_messages_CreatedAt",
                table: "aik_orch_outbox_messages",
                newName: "IX_aik_orch_outbox_messages_CreatedAt");

            migrationBuilder.AddPrimaryKey(
                name: "PK_aik_orch_outbox_messages",
                table: "aik_orch_outbox_messages",
                column: "Id");
        }
    }
}

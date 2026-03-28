using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P02_8b_CostOutboxTablePrefixAlignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ops_cost_outbox_messages",
                table: "ops_cost_outbox_messages");

            migrationBuilder.RenameTable(
                name: "ops_cost_outbox_messages",
                newName: "ops_outbox_messages");

            migrationBuilder.RenameIndex(
                name: "IX_ops_cost_outbox_messages_ProcessedAt",
                table: "ops_outbox_messages",
                newName: "IX_ops_outbox_messages_ProcessedAt");

            migrationBuilder.RenameIndex(
                name: "IX_ops_cost_outbox_messages_IdempotencyKey",
                table: "ops_outbox_messages",
                newName: "IX_ops_outbox_messages_IdempotencyKey");

            migrationBuilder.RenameIndex(
                name: "IX_ops_cost_outbox_messages_CreatedAt",
                table: "ops_outbox_messages",
                newName: "IX_ops_outbox_messages_CreatedAt");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ops_outbox_messages",
                table: "ops_outbox_messages",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ops_outbox_messages",
                table: "ops_outbox_messages");

            migrationBuilder.RenameTable(
                name: "ops_outbox_messages",
                newName: "ops_cost_outbox_messages");

            migrationBuilder.RenameIndex(
                name: "IX_ops_outbox_messages_ProcessedAt",
                table: "ops_cost_outbox_messages",
                newName: "IX_ops_cost_outbox_messages_ProcessedAt");

            migrationBuilder.RenameIndex(
                name: "IX_ops_outbox_messages_IdempotencyKey",
                table: "ops_cost_outbox_messages",
                newName: "IX_ops_cost_outbox_messages_IdempotencyKey");

            migrationBuilder.RenameIndex(
                name: "IX_ops_outbox_messages_CreatedAt",
                table: "ops_cost_outbox_messages",
                newName: "IX_ops_cost_outbox_messages_CreatedAt");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ops_cost_outbox_messages",
                table: "ops_cost_outbox_messages",
                column: "Id");
        }
    }
}

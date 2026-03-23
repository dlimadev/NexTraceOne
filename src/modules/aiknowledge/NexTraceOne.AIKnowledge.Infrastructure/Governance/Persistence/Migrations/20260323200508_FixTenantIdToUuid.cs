using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixTenantIdToUuid : Migration
    {
        /// <inheritdoc />
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                // A conversão de TenantId de varchar(200) para uuid já foi executada
                // via raw SQL na migração 20260322140000_StandardizeTenantIdToGuid.
                // Esta migração existe apenas para reconciliar o snapshot do EF Core
                // com o estado real do modelo (Guid TenantId), corrigindo o PendingModelChangesWarning.
            }

            /// <inheritdoc />
            protected override void Down(MigrationBuilder migrationBuilder)
            {
                // Sem operações DDL — reversão coberta por 20260322140000_StandardizeTenantIdToGuid.Down().
            }
    }
}

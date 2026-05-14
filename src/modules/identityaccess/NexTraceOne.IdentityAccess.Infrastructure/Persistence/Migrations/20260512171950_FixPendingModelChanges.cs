using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // As alterações de modelo serão aplicadas automaticamente pelo EF Core
            // quando o modelo for sincronizado com o banco de dados.
            // Esta migration está vazia porque as mudanças já foram capturadas
            // na migration 20260511090000_IAM_AddEnvironmentAccessPolicies.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reversão não necessária - modelo já está correto
        }
    }
}

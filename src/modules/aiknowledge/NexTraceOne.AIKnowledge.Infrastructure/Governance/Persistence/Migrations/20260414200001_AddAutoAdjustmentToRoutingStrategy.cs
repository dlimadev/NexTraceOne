using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations;

/// <summary>
/// E-M02: Adiciona colunas de ajuste automático de prioridade por feedback negativo
/// à tabela aik_routing_strategies.
/// Permite rastrear quando e porquê uma estratégia foi automaticamente desprioritizada.
/// </summary>
public partial class AddAutoAdjustmentToRoutingStrategy : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "AutoAdjustedAt",
            table: "aik_routing_strategies",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "AutoAdjustmentReason",
            table: "aik_routing_strategies",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AutoAdjustedAt",
            table: "aik_routing_strategies");

        migrationBuilder.DropColumn(
            name: "AutoAdjustmentReason",
            table: "aik_routing_strategies");
    }
}

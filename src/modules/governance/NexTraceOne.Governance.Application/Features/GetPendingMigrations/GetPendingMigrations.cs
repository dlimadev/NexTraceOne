using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.GetPendingMigrations;

/// <summary>
/// Feature: GetPendingMigrations — lista de migrations pendentes por DbContext.
/// Permite ao PlatformAdmin saber exactamente que alterações de schema serão aplicadas
/// antes de promover uma nova versão para produção.
/// </summary>
public static class GetPendingMigrations
{
    /// <summary>Query sem parâmetros — retorna todas as migrations pendentes de todos os contextos.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Handler que consulta migrations pendentes via IPendingMigrationsProvider.</summary>
    public sealed class Handler(IPendingMigrationsProvider migrationsProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var pending = await migrationsProvider.GetPendingMigrationsAsync(cancellationToken);

            // Classificação de risco por convenção de naming (heurística)
            var migrations = pending
                .Select(m => ClassifyMigration(m))
                .OrderBy(m => m.Context)
                .ThenBy(m => m.MigrationId)
                .ToList();

            var isSafeToApply = !migrations.Any(m =>
                m.RiskLevel == "High" || m.RiskLevel == "Critical");

            var response = new Response(
                TotalPending: migrations.Count,
                IsSafeToApply: isSafeToApply,
                Migrations: migrations,
                CheckedAt: DateTimeOffset.UtcNow);

            return Result<Response>.Success(response);
        }

        /// <summary>
        /// Classifica uma migration por risco com base em heurísticas de naming.
        /// Low: índices, colunas nullable, novas tabelas.
        /// Medium: particionamento, renaming, colunas non-nullable com default.
        /// High: drop de tabela/coluna, migrations marcadas explicitamente como destrutivas.
        /// </summary>
        private static MigrationDto ClassifyMigration(PendingMigrationInfo info)
        {
            var id = info.MigrationId;
            var lower = id.ToLowerInvariant();

            var (riskLevel, requiresDowntime, isReversible) = lower switch
            {
                _ when lower.Contains("drop") => ("High", false, false),
                _ when lower.Contains("delete") && lower.Contains("table") => ("High", false, false),
                _ when lower.Contains("truncate") => ("High", false, false),
                _ when lower.Contains("partition") => ("Medium", false, false),
                _ when lower.Contains("rename") => ("Medium", false, false),
                _ when lower.Contains("alter") && lower.Contains("column") && !lower.Contains("nullable") => ("Medium", false, true),
                _ when lower.Contains("index") => ("Low", false, true),
                _ when lower.Contains("addcolumn") || lower.Contains("add_column") => ("Low", false, true),
                _ when lower.Contains("nullable") => ("Low", false, true),
                _ => ("Low", false, true)
            };

            return new MigrationDto(
                MigrationId: id,
                Context: info.ContextName,
                RiskLevel: riskLevel,
                RequiresDowntime: requiresDowntime,
                IsReversible: isReversible);
        }
    }

    /// <summary>Resposta com a lista de migrations pendentes e avaliação de segurança.</summary>
    public sealed record Response(
        int TotalPending,
        bool IsSafeToApply,
        IReadOnlyList<MigrationDto> Migrations,
        DateTimeOffset CheckedAt);

    /// <summary>Informação individual de uma migration pendente com avaliação de risco.</summary>
    /// <param name="MigrationId">ID da migration (ex: "20260410_AddServiceHealthIndex").</param>
    /// <param name="Context">Nome do DbContext (ex: "CatalogGraph", "Identity").</param>
    /// <param name="RiskLevel">Nível de risco: Low, Medium, High ou Critical.</param>
    /// <param name="RequiresDowntime">true se a migration requer interrupção do serviço.</param>
    /// <param name="IsReversible">true se a migration pode ser revertida com Down().</param>
    public sealed record MigrationDto(
        string MigrationId,
        string Context,
        string RiskLevel,
        bool RequiresDowntime,
        bool IsReversible);
}

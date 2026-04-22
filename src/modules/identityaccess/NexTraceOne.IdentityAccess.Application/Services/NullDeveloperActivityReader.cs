using NexTraceOne.IdentityAccess.Application.Abstractions;

namespace NexTraceOne.IdentityAccess.Application.Services;

/// <summary>
/// Implementação nula de <see cref="IDeveloperActivityReader"/> com dados de teste codificados.
///
/// Devolve oito utilizadores com níveis de atividade variados para validar o comportamento
/// do handler <c>GetDeveloperActivityReport</c> sem necessidade de base de dados.
/// Substituir por implementação real baseada em EF/PostgreSQL em produção.
///
/// Wave AC.2 — GetDeveloperActivityReport.
/// </summary>
public sealed class NullDeveloperActivityReader : IDeveloperActivityReader
{
    /// <inheritdoc />
    public Task<IReadOnlyList<DeveloperActivityEntry>> ListByTenantAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken ct)
    {
        IReadOnlyList<DeveloperActivityEntry> entries =
        [
            // Utilizador altamente ativo — líder técnico com muitas ações
            new DeveloperActivityEntry(
                UserId: "user-001",
                UserName: "alice.santos",
                TeamName: "team-payments",
                ContractsCreated: 4,
                ContractsUpdated: 6,
                RunbooksCreated: 2,
                RunbooksUpdated: 3,
                ReleasesRegistered: 5,
                OperationalNotesCreated: 4),

            // Utilizador ativo — engineer regular
            new DeveloperActivityEntry(
                UserId: "user-002",
                UserName: "bob.silva",
                TeamName: "team-payments",
                ContractsCreated: 2,
                ContractsUpdated: 3,
                RunbooksCreated: 1,
                RunbooksUpdated: 1,
                ReleasesRegistered: 3,
                OperationalNotesCreated: 2),

            // Utilizador ativo — foco em runbooks
            new DeveloperActivityEntry(
                UserId: "user-003",
                UserName: "carol.lima",
                TeamName: "team-logistics",
                ContractsCreated: 1,
                ContractsUpdated: 2,
                RunbooksCreated: 3,
                RunbooksUpdated: 4,
                ReleasesRegistered: 2,
                OperationalNotesCreated: 1),

            // Utilizador ocasional — poucas ações
            new DeveloperActivityEntry(
                UserId: "user-004",
                UserName: "david.costa",
                TeamName: "team-logistics",
                ContractsCreated: 0,
                ContractsUpdated: 1,
                RunbooksCreated: 0,
                RunbooksUpdated: 0,
                ReleasesRegistered: 1,
                OperationalNotesCreated: 0),

            // Utilizador inativo — sem ações no período
            new DeveloperActivityEntry(
                UserId: "user-005",
                UserName: "eve.ferreira",
                TeamName: "team-platform",
                ContractsCreated: 0,
                ContractsUpdated: 0,
                RunbooksCreated: 0,
                RunbooksUpdated: 0,
                ReleasesRegistered: 0,
                OperationalNotesCreated: 0),

            // Utilizador inativo sem equipa
            new DeveloperActivityEntry(
                UserId: "user-006",
                UserName: "frank.oliveira",
                TeamName: null,
                ContractsCreated: 0,
                ContractsUpdated: 0,
                RunbooksCreated: 0,
                RunbooksUpdated: 0,
                ReleasesRegistered: 0,
                OperationalNotesCreated: 0),

            // Utilizador altamente ativo — foco em contratos
            new DeveloperActivityEntry(
                UserId: "user-007",
                UserName: "grace.mendes",
                TeamName: "team-platform",
                ContractsCreated: 5,
                ContractsUpdated: 7,
                RunbooksCreated: 1,
                RunbooksUpdated: 2,
                ReleasesRegistered: 4,
                OperationalNotesCreated: 3),

            // Utilizador ocasional — equipa de logística
            new DeveloperActivityEntry(
                UserId: "user-008",
                UserName: "henry.rocha",
                TeamName: "team-logistics",
                ContractsCreated: 0,
                ContractsUpdated: 0,
                RunbooksCreated: 1,
                RunbooksUpdated: 0,
                ReleasesRegistered: 0,
                OperationalNotesCreated: 1),
        ];

        return Task.FromResult(entries);
    }
}

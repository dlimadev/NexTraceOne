using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Audit.Application.Abstractions;
using NexTraceOne.Audit.Domain.Errors;

namespace NexTraceOne.Audit.Application.Features.VerifyChainIntegrity;

/// <summary>
/// Feature: VerifyChainIntegrity — verifica a integridade da cadeia de hash SHA-256.
/// </summary>
public static class VerifyChainIntegrity
{
    /// <summary>Query de verificação de integridade da cadeia.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Handler que percorre a cadeia verificando cada elo.</summary>
    public sealed class Handler(
        IAuditEventRepository auditEventRepository,
        IAuditChainRepository auditChainRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var links = await auditChainRepository.GetAllLinksAsync(cancellationToken);
            if (links.Count == 0)
            {
                return new Response(true, 0, []);
            }

            var violations = new List<ChainViolation>();
            var previousHash = string.Empty;

            foreach (var link in links.OrderBy(l => l.SequenceNumber))
            {
                if (!string.Equals(link.PreviousHash, previousHash, StringComparison.Ordinal))
                {
                    violations.Add(new ChainViolation(link.SequenceNumber, "Previous hash mismatch"));
                }

                previousHash = link.CurrentHash;
            }

            return new Response(violations.Count == 0, links.Count, violations);
        }
    }

    /// <summary>Resposta da verificação de integridade.</summary>
    public sealed record Response(bool IsIntact, int TotalLinks, IReadOnlyList<ChainViolation> Violations);

    /// <summary>Violação detectada na cadeia de hash.</summary>
    public sealed record ChainViolation(long SequenceNumber, string Reason);
}

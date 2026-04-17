using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;

namespace NexTraceOne.IdentityAccess.Application.Features.UpdateTenant;

/// <summary>
/// Feature: UpdateTenant — atualiza dados de um tenant existente (uso exclusivo de Platform Admin).
///
/// Permite atualizar nome, razão social e identificação fiscal.
/// O slug é imutável após a criação para preservar estabilidade de referências externas.
/// </summary>
public static class UpdateTenant
{
    /// <summary>Comando para atualização de um tenant.</summary>
    public sealed record Command(
        Guid TenantId,
        string Name,
        string? LegalName,
        string? TaxId) : ICommand<Response>;

    /// <summary>Resposta com os dados actualizados do tenant.</summary>
    public sealed record Response(Guid TenantId, string Name, string? LegalName, string? TaxId);

    /// <summary>Valida os parâmetros do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
            RuleFor(x => x.LegalName).MaximumLength(512).When(x => x.LegalName != null);
            RuleFor(x => x.TaxId).MaximumLength(50).When(x => x.TaxId != null);
        }
    }

    /// <summary>
    /// Handler que actualiza os dados do tenant.
    /// O commit é gerenciado pelo TransactionBehavior do pipeline.
    /// </summary>
    public sealed class Handler(
        ITenantRepository tenantRepository,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var tenant = await tenantRepository.GetByIdAsync(TenantId.From(request.TenantId), cancellationToken);
            if (tenant is null)
                return IdentityErrors.TenantNotFound(request.TenantId);

            var now = dateTimeProvider.UtcNow;
            tenant.UpdateName(request.Name, now);
            tenant.UpdateOrganizationInfo(request.LegalName, request.TaxId, now);

            return Result<Response>.Success(new Response(
                tenant.Id.Value,
                tenant.Name,
                tenant.LegalName,
                tenant.TaxId));
        }
    }
}

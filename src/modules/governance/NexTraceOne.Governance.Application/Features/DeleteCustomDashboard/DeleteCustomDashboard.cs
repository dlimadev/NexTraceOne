using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using MediatR;

namespace NexTraceOne.Governance.Application.Features.DeleteCustomDashboard;

/// <summary>
/// Feature: DeleteCustomDashboard — remove um dashboard customizado persistido.
/// Apenas o criador ou um PlatformAdmin pode eliminar; dashboards de sistema não são eliminados.
///
/// Owner: módulo Governance.
/// Pilar: Governance — Source of Truth para dashboards de governance por persona.
/// </summary>
public static class DeleteCustomDashboard
{
    /// <summary>Comando para eliminar um dashboard customizado.</summary>
    public sealed record Command(
        Guid DashboardId,
        string TenantId) : ICommand;

    /// <summary>Validação do comando de eliminação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DashboardId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    /// <summary>Handler que remove o dashboard da base de dados.</summary>
    public sealed class Handler(
        ICustomDashboardRepository repository,
        IGovernanceUnitOfWork unitOfWork) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            var dashboard = await repository.GetByIdAsync(
                new CustomDashboardId(request.DashboardId), cancellationToken);

            if (dashboard is null)
                return Error.NotFound(
                    "CustomDashboard.NotFound",
                    "Custom dashboard with ID '{0}' was not found.",
                    request.DashboardId);

            if (dashboard.TenantId != request.TenantId)
                return Error.Forbidden(
                    "CustomDashboard.Forbidden",
                    "Access to dashboard '{0}' is not allowed.",
                    request.DashboardId);

            if (dashboard.IsSystem)
                return Error.Business(
                    "CustomDashboard.SystemDashboardReadOnly",
                    "System dashboards cannot be deleted.");

            await repository.DeleteAsync(dashboard, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}

using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.DeleteCustomChart;

/// <summary>Feature: DeleteCustomChart — remove um gráfico customizado.</summary>
public static class DeleteCustomChart
{
    public sealed record Command(Guid ChartId, string TenantId) : ICommand<bool>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ChartId).NotEmpty();
        }
    }

    public sealed class Handler(ICustomChartRepository repository) : ICommandHandler<Command, bool>
    {
        public async Task<Result<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            var id = new CustomChartId(request.ChartId);
            var existing = await repository.GetByIdAsync(id, request.TenantId, cancellationToken);
            if (existing is null)
                return Error.NotFound("CustomChart.NotFound", "Chart {0} not found.", request.ChartId);

            await repository.DeleteAsync(id, cancellationToken);
            return Result<bool>.Success(true);
        }
    }
}

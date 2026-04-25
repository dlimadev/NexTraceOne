using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetContractConsumerInventory;

/// <summary>
/// Feature: GetContractConsumerInventory — lista consumidores reais de um contrato
/// derivados de traces OTel, com frequência e último acesso observado.
///
/// Referência: CC-04.
/// Ownership: módulo Catalog (Contracts).
/// </summary>
public static class GetContractConsumerInventory
{
    public sealed record Query(
        string TenantId,
        Guid ContractId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ContractId).NotEmpty();
        }
    }

    public sealed class Handler(IContractConsumerInventoryRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var records = await repository.ListByContractAsync(
                request.ContractId, request.TenantId, cancellationToken);

            var consumers = records.Select(r => new ConsumerDto(
                ConsumerService: r.ConsumerService,
                ConsumerEnvironment: r.ConsumerEnvironment,
                Version: r.Version,
                FrequencyPerDay: r.FrequencyPerDay,
                LastCalledAt: r.LastCalledAt,
                FirstCalledAt: r.FirstCalledAt)).ToList();

            return Result<Response>.Success(new Response(
                ContractId: request.ContractId,
                TotalConsumers: consumers.Count,
                ActiveConsumers: consumers.Count(c => c.FrequencyPerDay > 0),
                Consumers: consumers));
        }
    }

    public sealed record Response(
        Guid ContractId,
        int TotalConsumers,
        int ActiveConsumers,
        IReadOnlyList<ConsumerDto> Consumers);

    public sealed record ConsumerDto(
        string ConsumerService,
        string ConsumerEnvironment,
        string? Version,
        double FrequencyPerDay,
        DateTimeOffset LastCalledAt,
        DateTimeOffset FirstCalledAt);
}

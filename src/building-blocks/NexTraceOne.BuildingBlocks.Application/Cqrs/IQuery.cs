using MediatR;
using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.BuildingBlocks.Application.Cqrs;

/// <summary>
/// Marcador para Queries tipadas.
/// Queries são somente-leitura e não modificam estado.
/// REGRA CQRS: Queries nunca chamam repositórios de escrita nem disparam Domain Events.
/// </summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }

/// <summary>Handler para Queries tipadas.</summary>
public interface IQueryHandler<TQuery, TResponse>
    : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse> { }

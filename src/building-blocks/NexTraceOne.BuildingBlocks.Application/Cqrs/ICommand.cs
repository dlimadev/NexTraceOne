using MediatR;
using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.BuildingBlocks.Application.Cqrs;

/// <summary>
/// Marcador para Commands sem resposta tipada.
/// Commands representam intenções de mudar o estado do sistema.
/// REGRA CQRS: Commands modificam estado e não retornam dados de leitura.
/// </summary>
public interface ICommand : IRequest<Result<Unit>> { }

/// <summary>
/// Marcador para Commands com resposta tipada.
/// Usado quando o command precisa retornar o Id do aggregate criado.
/// </summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }

/// <summary>Handler para Commands sem resposta tipada.</summary>
public interface ICommandHandler<TCommand>
    : IRequestHandler<TCommand, Result<Unit>>
    where TCommand : ICommand { }

/// <summary>Handler para Commands com resposta tipada.</summary>
public interface ICommandHandler<TCommand, TResponse>
    : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse> { }

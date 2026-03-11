namespace NexTraceOne.BuildingBlocks.Application.Cqrs;

/// <summary>
/// Marcador para requests públicos que não exigem tenant ativo no pipeline.
/// Usado principalmente por endpoints de autenticação e handshakes externos.
/// </summary>
public interface IPublicRequest
{
}

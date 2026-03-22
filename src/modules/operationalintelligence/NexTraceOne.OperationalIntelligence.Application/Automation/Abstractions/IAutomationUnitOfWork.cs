using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Automation.Abstractions;

/// <summary>
/// Unidade de trabalho isolada para o subdomínio Automation.
/// Evita conflito de registro de IUnitOfWork entre módulos.
/// </summary>
public interface IAutomationUnitOfWork : IUnitOfWork
{
}

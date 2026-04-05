using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;

/// <summary>
/// Unidade de trabalho dedicada ao Developer Experience.
/// Permite persistência isolada sem ambiguidade de IUnitOfWork no container DI.
/// </summary>
public interface IDeveloperExperienceUnitOfWork : IUnitOfWork
{
}

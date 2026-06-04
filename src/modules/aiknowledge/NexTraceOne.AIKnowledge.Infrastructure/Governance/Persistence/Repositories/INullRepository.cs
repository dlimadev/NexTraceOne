namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

/// <summary>
/// Interface marcadora para identificar implementações nulas de repositórios.
/// Usada por health checks para detectar fallback quando um serviço externo
/// não está configurado, sem depender de reflexão frágil.
/// </summary>
public interface INullRepository { }

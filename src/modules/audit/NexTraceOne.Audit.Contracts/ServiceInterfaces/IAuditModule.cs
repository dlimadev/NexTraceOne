namespace NexTraceOne.Audit.Contracts.ServiceInterfaces;

/// <summary>
/// Interface pública do módulo Audit.
/// Outros módulos que precisarem de dados deste módulo devem usar
/// este contrato — nunca acessar o DbContext ou repositórios diretamente.
/// </summary>
public interface IAuditModule
{
    // TODO: Definir operações de consulta que outros módulos podem usar
}

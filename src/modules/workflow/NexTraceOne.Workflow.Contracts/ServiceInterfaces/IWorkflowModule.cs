namespace NexTraceOne.Workflow.Contracts.ServiceInterfaces;

/// <summary>
/// Interface pública do módulo Workflow.
/// Outros módulos que precisarem de dados deste módulo devem usar
/// este contrato — nunca acessar o DbContext ou repositórios diretamente.
/// </summary>
public interface IWorkflowModule
{
    // TODO: Definir operações de consulta que outros módulos podem usar
}

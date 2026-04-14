using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;

/// <summary>
/// Unit of work específico do sub-módulo Workflow para garantir commit no WorkflowDbContext.
/// </summary>
public interface IWorkflowUnitOfWork : IUnitOfWork;

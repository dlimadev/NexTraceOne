using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.Promotion.Abstractions;

/// <summary>
/// Unit of work específico do sub-módulo Promotion para garantir commit no PromotionDbContext.
/// </summary>
public interface IPromotionUnitOfWork : IUnitOfWork;

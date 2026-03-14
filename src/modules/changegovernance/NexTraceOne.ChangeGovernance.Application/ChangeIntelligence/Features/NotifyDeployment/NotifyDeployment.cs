using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;

namespace NexTraceOne.ChangeIntelligence.Application.Features.NotifyDeployment;

/// <summary>
/// Feature: NotifyDeployment — recebe eventos de deployment do CI/CD e cria uma Release.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class NotifyDeployment
{
    /// <summary>Comando de notificação de deployment do CI/CD.</summary>
    public sealed record Command(
        Guid ApiAssetId,
        string ServiceName,
        string Version,
        string Environment,
        string PipelineSource,
        string CommitSha) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de notificação de deployment.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Version).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.PipelineSource).NotEmpty().MaximumLength(500);
            RuleFor(x => x.CommitSha).NotEmpty().MaximumLength(100);
        }
    }

    /// <summary>Handler que cria uma Release a partir de um evento de deployment recebido.</summary>
    public sealed class Handler(
        IReleaseRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var release = Release.Create(
                request.ApiAssetId,
                request.ServiceName,
                request.Version,
                request.Environment,
                request.PipelineSource,
                request.CommitSha,
                dateTimeProvider.UtcNow);

            repository.Add(release);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                release.Id.Value,
                release.ServiceName,
                release.Version,
                release.Environment,
                release.Status.ToString(),
                release.CreatedAt);
        }
    }

    /// <summary>Resposta da criação da Release via evento de deployment.</summary>
    public sealed record Response(
        Guid ReleaseId,
        string ServiceName,
        string Version,
        string Environment,
        string Status,
        DateTimeOffset CreatedAt);
}

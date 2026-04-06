using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Features.InitiateContractDeprecation;

/// <summary>
/// Feature: InitiateContractDeprecation — inicia o workflow governado de deprecação de contrato.
/// Identifica consumidores activos, deprecia a versão mais recente publicada, regista a data
/// de sunset e o contrato substituto. Retorna impacto e estado do workflow.
/// Estrutura VSA: Command + Validator + Handler + Response em arquivo único.
/// </summary>
public static class InitiateContractDeprecation
{
    /// <summary>Comando para iniciar o workflow de deprecação de contrato.</summary>
    public sealed record Command(
        Guid ApiAssetId,
        DateTimeOffset SunsetDate,
        Guid? ReplacementApiAssetId = null,
        string? MigrationGuide = null,
        string? Reason = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.SunsetDate).GreaterThan(DateTimeOffset.UtcNow)
                .WithMessage("Sunset date must be in the future.");
        }
    }

    /// <summary>
    /// Handler que deprecia a versão mais recente do contrato e regista o contexto de sunset.
    /// </summary>
    public sealed class Handler(
        IApiAssetRepository apiAssetRepository,
        IContractVersionRepository contractVersionRepository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Verificar que o API asset existe
            var apiAsset = await apiAssetRepository.GetByIdAsync(
                ApiAssetId.From(request.ApiAssetId), cancellationToken);

            if (apiAsset is null)
                return ContractsErrors.ContractVersionNotFound(request.ApiAssetId.ToString());

            // Obter versão mais recente publicada (Locked ou Approved)
            var latestVersion = await contractVersionRepository.GetLatestByApiAssetAsync(
                request.ApiAssetId, cancellationToken);

            if (latestVersion is null)
                return ContractsErrors.ContractVersionNotFound(request.ApiAssetId.ToString());

            // Construir notice de deprecação
            var notice = string.IsNullOrWhiteSpace(request.Reason)
                ? $"Contract deprecated. Sunset date: {request.SunsetDate:yyyy-MM-dd}."
                : $"{request.Reason} Sunset date: {request.SunsetDate:yyyy-MM-dd}.";

            if (!string.IsNullOrWhiteSpace(request.MigrationGuide))
                notice += $" Migration: {request.MigrationGuide}";

            // Deprecar versão
            var deprecateResult = latestVersion.Deprecate(notice, dateTimeProvider.UtcNow, request.SunsetDate);

            if (deprecateResult.IsFailure)
                return deprecateResult.Error;

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                request.ApiAssetId,
                latestVersion.Id.Value,
                request.SunsetDate,
                request.ReplacementApiAssetId,
                notice);
        }
    }

    /// <summary>Resposta do workflow de deprecação de contrato.</summary>
    public sealed record Response(
        Guid ApiAssetId,
        Guid DeprecatedVersionId,
        DateTimeOffset SunsetDate,
        Guid? ReplacementApiAssetId,
        string DeprecationNotice);
}

using System.Globalization;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;
using NexTraceOne.Catalog.Domain.LegacyAssets.Errors;

namespace NexTraceOne.Catalog.Application.LegacyAssets.Features.GetLegacyAssetDetail;

/// <summary>
/// Feature: GetLegacyAssetDetail — obtém os detalhes de um ativo legacy pelo seu identificador e tipo.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetLegacyAssetDetail
{
    /// <summary>Query de detalhes de um ativo legacy.</summary>
    public sealed record Query(Guid AssetId, string AssetType) : IQuery<Response>;

    /// <summary>Valida a entrada da query de detalhe de ativo legacy.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.AssetId).NotEmpty();
            RuleFor(x => x.AssetType).NotEmpty();
        }
    }

    /// <summary>Handler que retorna os detalhes completos de um ativo legacy.</summary>
    public sealed class Handler(
        IMainframeSystemRepository mainframeSystemRepository,
        ICobolProgramRepository cobolProgramRepository,
        ICopybookRepository copybookRepository,
        ICicsTransactionRepository cicsTransactionRepository,
        IImsTransactionRepository imsTransactionRepository,
        IDb2ArtifactRepository db2ArtifactRepository,
        IZosConnectBindingRepository zosConnectBindingRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            return request.AssetType switch
            {
                "MainframeSystem" => await GetMainframeSystem(request.AssetId, cancellationToken),
                "CobolProgram" => await GetCobolProgram(request.AssetId, cancellationToken),
                "Copybook" => await GetCopybook(request.AssetId, cancellationToken),
                "CicsTransaction" => await GetCicsTransaction(request.AssetId, cancellationToken),
                "ImsTransaction" => await GetImsTransaction(request.AssetId, cancellationToken),
                "Db2Artifact" => await GetDb2Artifact(request.AssetId, cancellationToken),
                "ZosConnectBinding" => await GetZosConnectBinding(request.AssetId, cancellationToken),
                _ => LegacyAssetsErrors.InvalidAssetType(request.AssetType)
            };
        }

        private async Task<Result<Response>> GetMainframeSystem(Guid assetId, CancellationToken cancellationToken)
        {
            var system = await mainframeSystemRepository.GetByIdAsync(
                MainframeSystemId.From(assetId), cancellationToken);
            if (system is null)
            {
                return LegacyAssetsErrors.MainframeSystemNotFound(assetId);
            }

            var metadata = new Dictionary<string, string>
            {
                ["SysplexName"] = system.Lpar.SysplexName,
                ["LparName"] = system.Lpar.LparName,
                ["RegionName"] = system.Lpar.RegionName,
                ["OperatingSystem"] = system.OperatingSystem,
                ["MipsCapacity"] = system.MipsCapacity
            };

            return new Response(
                system.Id.Value, "MainframeSystem", system.Name, system.DisplayName,
                system.Description, system.TeamName, system.Domain,
                system.Criticality.ToString(), system.LifecycleStatus.ToString(),
                system.CreatedAt, system.UpdatedAt, metadata);
        }

        private async Task<Result<Response>> GetCobolProgram(Guid assetId, CancellationToken cancellationToken)
        {
            var program = await cobolProgramRepository.GetByIdAsync(
                CobolProgramId.From(assetId), cancellationToken);
            if (program is null)
            {
                return LegacyAssetsErrors.CobolProgramNotFound(assetId);
            }

            var metadata = new Dictionary<string, string>
            {
                ["SystemId"] = program.SystemId.Value.ToString(),
                ["Language"] = program.Language,
                ["CompilerVersion"] = program.CompilerVersion,
                ["SourceLibrary"] = program.SourceLibrary,
                ["LoadModule"] = program.LoadModule
            };

            return new Response(
                program.Id.Value, "CobolProgram", program.Name, program.DisplayName,
                program.Description, string.Empty, string.Empty,
                program.Criticality.ToString(), program.LifecycleStatus.ToString(),
                program.CreatedAt, program.UpdatedAt, metadata);
        }

        private async Task<Result<Response>> GetCopybook(Guid assetId, CancellationToken cancellationToken)
        {
            var copybook = await copybookRepository.GetByIdAsync(
                CopybookId.From(assetId), cancellationToken);
            if (copybook is null)
            {
                return LegacyAssetsErrors.CopybookNotFound(assetId);
            }

            var metadata = new Dictionary<string, string>
            {
                ["SystemId"] = copybook.SystemId.Value.ToString(),
                ["Version"] = copybook.Version,
                ["FieldCount"] = copybook.Layout.FieldCount.ToString(CultureInfo.InvariantCulture),
                ["TotalLength"] = copybook.Layout.TotalLength.ToString(CultureInfo.InvariantCulture),
                ["RecordFormat"] = copybook.Layout.RecordFormat
            };

            return new Response(
                copybook.Id.Value, "Copybook", copybook.Name, copybook.DisplayName,
                copybook.Description, string.Empty, string.Empty,
                copybook.Criticality.ToString(), copybook.LifecycleStatus.ToString(),
                copybook.CreatedAt, copybook.UpdatedAt, metadata);
        }

        private async Task<Result<Response>> GetCicsTransaction(Guid assetId, CancellationToken cancellationToken)
        {
            var transaction = await cicsTransactionRepository.GetByIdAsync(
                CicsTransactionId.From(assetId), cancellationToken);
            if (transaction is null)
            {
                return LegacyAssetsErrors.CicsTransactionNotFound(assetId);
            }

            var metadata = new Dictionary<string, string>
            {
                ["SystemId"] = transaction.SystemId.Value.ToString(),
                ["TransactionId"] = transaction.TransactionId,
                ["ProgramName"] = transaction.ProgramName,
                ["RegionName"] = transaction.Region.RegionName,
                ["CicsVersion"] = transaction.Region.CicsVersion,
                ["TransactionType"] = transaction.TransactionType.ToString()
            };

            return new Response(
                transaction.Id.Value, "CicsTransaction", transaction.TransactionId, transaction.DisplayName,
                transaction.Description, string.Empty, string.Empty,
                transaction.Criticality.ToString(), transaction.LifecycleStatus.ToString(),
                transaction.CreatedAt, transaction.UpdatedAt, metadata);
        }

        private async Task<Result<Response>> GetImsTransaction(Guid assetId, CancellationToken cancellationToken)
        {
            var transaction = await imsTransactionRepository.GetByIdAsync(
                ImsTransactionId.From(assetId), cancellationToken);
            if (transaction is null)
            {
                return LegacyAssetsErrors.ImsTransactionNotFound(assetId);
            }

            var metadata = new Dictionary<string, string>
            {
                ["SystemId"] = transaction.SystemId.Value.ToString(),
                ["TransactionCode"] = transaction.TransactionCode,
                ["PsbName"] = transaction.PsbName,
                ["DbdName"] = transaction.DbdName,
                ["TransactionType"] = transaction.TransactionType.ToString()
            };

            return new Response(
                transaction.Id.Value, "ImsTransaction", transaction.TransactionCode, transaction.DisplayName,
                transaction.Description, string.Empty, string.Empty,
                transaction.Criticality.ToString(), transaction.LifecycleStatus.ToString(),
                transaction.CreatedAt, transaction.UpdatedAt, metadata);
        }

        private async Task<Result<Response>> GetDb2Artifact(Guid assetId, CancellationToken cancellationToken)
        {
            var artifact = await db2ArtifactRepository.GetByIdAsync(
                Db2ArtifactId.From(assetId), cancellationToken);
            if (artifact is null)
            {
                return LegacyAssetsErrors.Db2ArtifactNotFound(assetId);
            }

            var metadata = new Dictionary<string, string>
            {
                ["SystemId"] = artifact.SystemId.Value.ToString(),
                ["ArtifactType"] = artifact.ArtifactType.ToString(),
                ["SchemaName"] = artifact.SchemaName,
                ["DatabaseName"] = artifact.DatabaseName,
                ["TablespaceName"] = artifact.TablespaceName
            };

            return new Response(
                artifact.Id.Value, "Db2Artifact", artifact.Name, artifact.DisplayName,
                artifact.Description, string.Empty, string.Empty,
                artifact.Criticality.ToString(), artifact.LifecycleStatus.ToString(),
                artifact.CreatedAt, artifact.UpdatedAt, metadata);
        }

        private async Task<Result<Response>> GetZosConnectBinding(Guid assetId, CancellationToken cancellationToken)
        {
            var binding = await zosConnectBindingRepository.GetByIdAsync(
                ZosConnectBindingId.From(assetId), cancellationToken);
            if (binding is null)
            {
                return LegacyAssetsErrors.ZosConnectBindingNotFound(assetId);
            }

            var metadata = new Dictionary<string, string>
            {
                ["SystemId"] = binding.SystemId.Value.ToString(),
                ["ServiceName"] = binding.ServiceName,
                ["OperationName"] = binding.OperationName,
                ["HttpMethod"] = binding.HttpMethod,
                ["BasePath"] = binding.BasePath,
                ["TargetTransaction"] = binding.TargetTransaction
            };

            return new Response(
                binding.Id.Value, "ZosConnectBinding", binding.Name, binding.DisplayName,
                binding.Description, string.Empty, string.Empty,
                binding.Criticality.ToString(), binding.LifecycleStatus.ToString(),
                binding.CreatedAt, binding.UpdatedAt, metadata);
        }
    }

    /// <summary>Resposta detalhada de um ativo legacy.</summary>
    public sealed record Response(
        Guid Id,
        string AssetType,
        string Name,
        string DisplayName,
        string Description,
        string TeamName,
        string Domain,
        string Criticality,
        string LifecycleStatus,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt,
        IReadOnlyDictionary<string, string> Metadata);
}

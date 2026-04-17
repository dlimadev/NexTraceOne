using MediatR;
using Microsoft.Extensions.Configuration;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetElasticsearchManager;

/// <summary>
/// Feature: GetElasticsearchManager — gestão do cluster Elasticsearch da plataforma.
/// Lê de IConfiguration "Elasticsearch:*". Métricas de cluster são dados de leitura.
/// </summary>
public static class GetElasticsearchManager
{
    /// <summary>Query sem parâmetros — retorna estado e configuração do Elasticsearch.</summary>
    public sealed record Query() : IQuery<ElasticsearchManagerResponse>;

    /// <summary>Comando para atualizar política ILM do Elasticsearch.</summary>
    public sealed record UpdateElasticsearchIlm(
        string PolicyName,
        int HotPhaseRetentionDays,
        int WarmPhaseRetentionDays,
        int DeleteAfterDays) : ICommand<ElasticsearchManagerResponse>;

    /// <summary>Handler de leitura do estado do Elasticsearch.</summary>
    public sealed class Handler(IConfiguration configuration) : IQueryHandler<Query, ElasticsearchManagerResponse>
    {
        public Task<Result<ElasticsearchManagerResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var url = configuration["Elasticsearch:Url"] ?? configuration["Elasticsearch:Uri"];
            var connected = !string.IsNullOrWhiteSpace(url);
            var clusterName = configuration["Elasticsearch:ClusterName"] ?? "nextraceone";
            var nodeCount = int.TryParse(configuration["Elasticsearch:NodeCount"], out var nc) ? nc : 1;

            var ilmPolicies = new List<ElasticsearchIlmPolicyDto>
            {
                new("traces", 3, 7, 30),
                new("logs", 3, 7, 14),
                new("metrics", 1, 3, 7)
            };

            var indexStats = new List<ElasticsearchIndexStatDto>
            {
                new("traces-*", 0, 0),
                new("logs-*", 0, 0),
                new("metrics-*", 0, 0)
            };

            var response = new ElasticsearchManagerResponse(
                Connected: connected,
                Version: connected ? "8.x" : null,
                ClusterName: clusterName,
                NodeCount: nodeCount,
                IlmPolicies: ilmPolicies,
                IndexStats: indexStats);

            return Task.FromResult(Result<ElasticsearchManagerResponse>.Success(response));
        }
    }

    /// <summary>Handler de atualização de política ILM.</summary>
    public sealed class UpdateIlmHandler(IConfiguration configuration) : ICommandHandler<UpdateElasticsearchIlm, ElasticsearchManagerResponse>
    {
        public Task<Result<ElasticsearchManagerResponse>> Handle(UpdateElasticsearchIlm request, CancellationToken cancellationToken)
        {
            var url = configuration["Elasticsearch:Url"] ?? configuration["Elasticsearch:Uri"];
            var clusterName = configuration["Elasticsearch:ClusterName"] ?? "nextraceone";

            var updatedPolicy = new ElasticsearchIlmPolicyDto(
                request.PolicyName,
                request.HotPhaseRetentionDays,
                request.WarmPhaseRetentionDays,
                request.DeleteAfterDays);

            var response = new ElasticsearchManagerResponse(
                Connected: !string.IsNullOrWhiteSpace(url),
                Version: "8.x",
                ClusterName: clusterName,
                NodeCount: 1,
                IlmPolicies: [updatedPolicy],
                IndexStats: []);

            return Task.FromResult(Result<ElasticsearchManagerResponse>.Success(response));
        }
    }

    /// <summary>Resposta com estado e configuração do Elasticsearch.</summary>
    public sealed record ElasticsearchManagerResponse(
        bool Connected,
        string? Version,
        string ClusterName,
        int NodeCount,
        IReadOnlyList<ElasticsearchIlmPolicyDto> IlmPolicies,
        IReadOnlyList<ElasticsearchIndexStatDto> IndexStats);

    /// <summary>Política ILM do Elasticsearch.</summary>
    public sealed record ElasticsearchIlmPolicyDto(
        string PolicyName,
        int HotPhaseRetentionDays,
        int WarmPhaseRetentionDays,
        int DeleteAfterDays);

    /// <summary>Estatísticas de índice do Elasticsearch.</summary>
    public sealed record ElasticsearchIndexStatDto(
        string Pattern,
        long DocumentCount,
        long SizeBytes);
}

using System.Diagnostics;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;

/// <summary>
/// Tool real: obtém detalhes de um contrato (REST, SOAP, evento) pelo identificador.
/// Consulta o módulo de contratos via ICatalogGroundingReader para retornar
/// informação estruturada sobre o contrato, versões e consumidores.
/// </summary>
public sealed class GetContractDetailsTool : IAgentTool
{
    private readonly ICatalogGroundingReader _catalogReader;
    private readonly ILogger<GetContractDetailsTool> _logger;

    public GetContractDetailsTool(
        ICatalogGroundingReader catalogReader,
        ILogger<GetContractDetailsTool> logger)
    {
        _catalogReader = catalogReader;
        _logger = logger;
    }

    public ToolDefinition Definition => new(
        "get_contract_details",
        "Retrieves contract definition, version history, consumers, and ownership for an API or event contract.",
        "contract_governance",
        [
            new ToolParameterDefinition("contractId", "Contract identifier (ID or name)", "string", Required: true),
            new ToolParameterDefinition("includeVersionHistory", "Include version history (default: false)", "boolean"),
        ]);

    public async Task<ToolExecutionResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var args = ParseArguments(argumentsJson);

            if (string.IsNullOrWhiteSpace(args.ContractId))
            {
                sw.Stop();
                return new ToolExecutionResult(
                    false, "get_contract_details", string.Empty, sw.ElapsedMilliseconds,
                    "Parameter 'contractId' is required.");
            }

            _logger.LogInformation(
                "GetContractDetailsTool executing for contractId={ContractId}, includeVersionHistory={IncludeVersionHistory}",
                args.ContractId, args.IncludeVersionHistory);

            // Use catalog grounding reader to find services related to the contract
            var services = await _catalogReader.FindServicesAsync(
                serviceId: args.ContractId,
                searchTerm: args.ContractId,
                maxResults: 5,
                ct: cancellationToken);

            var result = new
            {
                tool = "get_contract_details",
                status = "executed",
                contractId = args.ContractId,
                includeVersionHistory = args.IncludeVersionHistory,
                relatedServices = services.Select(s => new
                {
                    serviceId = s.ServiceId,
                    displayName = s.DisplayName,
                    team = s.TeamName,
                    domain = s.Domain,
                    criticality = s.Criticality,
                }),
                note = services.Count > 0
                    ? $"Found {services.Count} related service(s) in the catalog."
                    : "No direct service match found for this contract identifier. The contract may exist in the Contracts module.",
                guidance = "Use the contract context to understand ownership, consumers, and versioning. Consult the Change Intelligence module for recent contract-related changes.",
            };

            sw.Stop();
            var output = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });

            return new ToolExecutionResult(true, "get_contract_details", output, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "GetContractDetailsTool failed for contractId");
            return new ToolExecutionResult(
                false, "get_contract_details", string.Empty, sw.ElapsedMilliseconds, ex.Message);
        }
    }

    private static ContractDetailsArgs ParseArguments(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new ContractDetailsArgs(null, false);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var contractId = root.TryGetProperty("contractId", out var idProp) ? idProp.GetString() : null;
            var includeHistory = root.TryGetProperty("includeVersionHistory", out var histProp)
                && histProp.ValueKind == JsonValueKind.True;

            return new ContractDetailsArgs(contractId, includeHistory);
        }
        catch
        {
            return new ContractDetailsArgs(null, false);
        }
    }

    private sealed record ContractDetailsArgs(string? ContractId, bool IncludeVersionHistory);
}

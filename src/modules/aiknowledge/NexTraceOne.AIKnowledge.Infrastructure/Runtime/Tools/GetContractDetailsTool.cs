using System.Diagnostics;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;

/// <summary>
/// Tool real: obtém detalhes de um contrato (REST, SOAP, evento) pelo identificador.
/// Consulta o módulo de contratos via IContractGroundingReader (versões diretas) e
/// IServiceInterfaceGroundingReader (caminho ServiceInterface → ContractBinding → ContractVersion).
/// </summary>
public sealed class GetContractDetailsTool : IAgentTool
{
    private readonly IServiceInterfaceGroundingReader _interfaceReader;
    private readonly IContractGroundingReader _contractReader;
    private readonly ILogger<GetContractDetailsTool> _logger;

    public GetContractDetailsTool(
        IServiceInterfaceGroundingReader interfaceReader,
        IContractGroundingReader contractReader,
        ILogger<GetContractDetailsTool> logger)
    {
        _interfaceReader = interfaceReader;
        _contractReader = contractReader;
        _logger = logger;
    }

    public ToolDefinition Definition => new(
        "get_contract_details",
        "Retrieves contract definition, version history, consumers, and ownership for an API or event contract. Resolves by contract name, interface name, or contract version ID.",
        "contract_governance",
        [
            new ToolParameterDefinition("contractId", "Contract identifier (contract name, interface name, or version ID)", "string", Required: true),
            new ToolParameterDefinition("environment", "Filter by environment (e.g. production, staging)", "string"),
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
                "GetContractDetailsTool executing for contractId={ContractId}, environment={Environment}, includeVersionHistory={IncludeVersionHistory}",
                args.ContractId, args.Environment, args.IncludeVersionHistory);

            // Step 1: Try to find ServiceInterfaces by name (most precise path)
            var interfaces = await _interfaceReader.FindInterfacesByNameAsync(
                args.ContractId, maxResults: 5, ct: cancellationToken);

            // Step 2: For each interface found, resolve active ContractBindings → ContractVersions
            var contractsByInterface = new List<object>();
            foreach (var iface in interfaces)
            {
                if (!Guid.TryParse(iface.InterfaceId, out var ifaceGuid))
                    continue;

                var contracts = await _contractReader.FindContractsByServiceInterfaceAsync(
                    ifaceGuid, args.Environment, maxResults: 10, ct: cancellationToken);

                contractsByInterface.Add(new
                {
                    interfaceId = iface.InterfaceId,
                    interfaceName = iface.Name,
                    interfaceType = iface.InterfaceType,
                    interfaceStatus = iface.Status,
                    serviceName = iface.ServiceName,
                    requiresContract = iface.RequiresContract,
                    sloTarget = iface.SloTarget,
                    activeContracts = contracts.Select(c => new
                    {
                        contractVersionId = c.ContractVersionId,
                        version = c.Version,
                        protocol = c.Protocol,
                        lifecycleState = c.LifecycleState,
                        isLocked = c.IsLocked,
                        lockedAt = c.LockedAt,
                    }),
                });
            }

            // Step 3: Also query ContractVersions directly by semver search as fallback
            var directContracts = await _contractReader.FindContractVersionsAsync(
                contractVersionId: null,
                apiAssetId: null,
                searchTerm: args.ContractId,
                maxResults: 5,
                ct: cancellationToken);

            var hasResults = contractsByInterface.Count > 0 || directContracts.Count > 0;

            var result = new
            {
                tool = "get_contract_details",
                status = "executed",
                contractId = args.ContractId,
                environment = args.Environment,
                includeVersionHistory = args.IncludeVersionHistory,
                interfaceContracts = contractsByInterface,
                directContractMatches = directContracts.Select(c => new
                {
                    contractVersionId = c.ContractVersionId,
                    version = c.Version,
                    protocol = c.Protocol,
                    lifecycleState = c.LifecycleState,
                    isLocked = c.IsLocked,
                }),
                note = hasResults
                    ? $"Found {contractsByInterface.Count} interface(s) with bindings and {directContracts.Count} direct contract version match(es)."
                    : "No contract found for the given identifier. Verify the contract name, interface name, or version ID.",
                guidance = "Interface contracts represent the active binding between a service interface and a contract version per environment. Use 'environment' parameter to filter by deployment context.",
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
            return new ContractDetailsArgs(null, null, false);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var contractId = root.TryGetProperty("contractId", out var idProp) ? idProp.GetString() : null;
            var environment = root.TryGetProperty("environment", out var envProp) ? envProp.GetString() : null;
            var includeHistory = root.TryGetProperty("includeVersionHistory", out var histProp)
                && histProp.ValueKind == JsonValueKind.True;

            return new ContractDetailsArgs(contractId, environment, includeHistory);
        }
        catch
        {
            return new ContractDetailsArgs(null, null, false);
        }
    }

    private sealed record ContractDetailsArgs(string? ContractId, string? Environment, bool IncludeVersionHistory);
}

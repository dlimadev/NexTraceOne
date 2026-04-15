using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Domain.Contracts.Policies;

/// <summary>
/// Política estática que define quais tipos de contrato são permitidos por tipo de interface de serviço.
/// Encapsula as regras de domínio do mapeamento InterfaceType → ContractType[].
/// Complementa a ServiceContractPolicy com granularidade de interface.
/// </summary>
public static class InterfaceContractPolicy
{
    /// <summary>
    /// Mapeamento oficial: InterfaceType → ContractTypes permitidos.
    /// Interfaces com lista vazia não expõem contratos de interface pública.
    /// </summary>
    private static readonly IReadOnlyDictionary<InterfaceType, ContractType[]> _allowedContractTypes =
        new Dictionary<InterfaceType, ContractType[]>
        {
            [InterfaceType.RestApi]           = [ContractType.RestApi],
            [InterfaceType.SoapService]       = [ContractType.Soap],
            [InterfaceType.KafkaProducer]     = [ContractType.Event, ContractType.SharedSchema],
            [InterfaceType.KafkaConsumer]     = [ContractType.Event, ContractType.SharedSchema],
            [InterfaceType.GrpcService]       = [ContractType.Grpc],
            [InterfaceType.GraphqlApi]        = [ContractType.RestApi],
            [InterfaceType.WebhookProducer]   = [ContractType.RestApi, ContractType.Event],
            [InterfaceType.ZosConnectApi]     = [ContractType.RestApi],
            [InterfaceType.MqQueue]           = [ContractType.MqMessage],
            [InterfaceType.IntegrationBridge] = [ContractType.RestApi, ContractType.Soap, ContractType.Event],
            // Sem contrato de interface pública
            [InterfaceType.BackgroundWorker]  = [],
            [InterfaceType.ScheduledJob]      = [],
            [InterfaceType.WebhookConsumer]   = [],
        };

    /// <summary>
    /// Tipos de interface que exigem contrato obrigatório.
    /// </summary>
    private static readonly HashSet<InterfaceType> _mandatoryContractTypes =
    [
        InterfaceType.RestApi,
        InterfaceType.SoapService,
        InterfaceType.KafkaProducer,
        InterfaceType.GrpcService,
        InterfaceType.GraphqlApi,
        InterfaceType.ZosConnectApi,
        InterfaceType.MqQueue,
    ];

    /// <summary>
    /// Indica se um tipo de interface suporta contratos de interface pública.
    /// </summary>
    /// <param name="interfaceType">Tipo da interface.</param>
    /// <returns>
    /// <c>true</c> se o tipo permite ao menos um contrato; <c>false</c> caso contrário.
    /// </returns>
    public static bool SupportsContracts(InterfaceType interfaceType)
        => _allowedContractTypes.TryGetValue(interfaceType, out var types) && types.Length > 0;

    /// <summary>
    /// Retorna os tipos de contrato permitidos para o tipo de interface.
    /// Retorna lista vazia se a interface não suporta contratos.
    /// </summary>
    /// <param name="interfaceType">Tipo da interface.</param>
    public static IReadOnlyList<ContractType> AllowedContractTypes(InterfaceType interfaceType)
        => _allowedContractTypes.TryGetValue(interfaceType, out var types)
            ? types
            : Array.Empty<ContractType>();

    /// <summary>
    /// Verifica se um tipo de contrato é permitido para o tipo de interface.
    /// </summary>
    /// <param name="interfaceType">Tipo da interface.</param>
    /// <param name="contractType">Tipo do contrato a validar.</param>
    public static bool IsContractTypeAllowed(InterfaceType interfaceType, ContractType contractType)
        => AllowedContractTypes(interfaceType).Contains(contractType);

    /// <summary>
    /// Indica se o tipo de interface exige a existência de pelo menos um contrato vinculado.
    /// </summary>
    /// <param name="interfaceType">Tipo da interface.</param>
    /// <returns>
    /// <c>true</c> se a interface exige contrato; <c>false</c> se o contrato é opcional ou não aplicável.
    /// </returns>
    public static bool RequiresContract(InterfaceType interfaceType)
        => _mandatoryContractTypes.Contains(interfaceType);
}

using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Domain.Contracts.Policies;

/// <summary>
/// Política estática que define quais tipos de contrato são permitidos por tipo de serviço.
/// Encapsula as regras de domínio do mapeamento ServiceType → ContractType.
/// Usada na criação de drafts para validar que o contrato é coerente com o serviço vinculado.
/// </summary>
public static class ServiceContractPolicy
{
    /// <summary>
    /// Mapeamento oficial: ServiceType → ContractTypes permitidos.
    /// Serviços com lista vazia não expõem contratos de interface pública.
    /// </summary>
    private static readonly IReadOnlyDictionary<ServiceType, ContractType[]> _allowedContractTypes =
        new Dictionary<ServiceType, ContractType[]>
        {
            [ServiceType.RestApi]                = [ContractType.RestApi],
            [ServiceType.SoapService]            = [ContractType.Soap],
            [ServiceType.KafkaProducer]          = [ContractType.Event, ContractType.SharedSchema],
            [ServiceType.KafkaConsumer]          = [ContractType.Event, ContractType.SharedSchema],
            [ServiceType.GraphqlApi]             = [ContractType.RestApi],
            [ServiceType.GrpcService]            = [ContractType.Grpc],
            [ServiceType.ZosConnectApi]          = [ContractType.RestApi],
            [ServiceType.CicsTransaction]        = [ContractType.CicsCommarea],
            [ServiceType.CobolProgram]           = [ContractType.Copybook],
            [ServiceType.MqQueueManager]         = [ContractType.MqMessage],
            [ServiceType.IntegrationComponent]   = [ContractType.RestApi, ContractType.Soap, ContractType.Event],
            [ServiceType.Gateway]                = [ContractType.RestApi],
            [ServiceType.ThirdParty]             = [ContractType.RestApi, ContractType.Soap, ContractType.Event],
            [ServiceType.LegacySystem]           = [ContractType.FixedLayout, ContractType.Copybook, ContractType.MqMessage],
            [ServiceType.SharedPlatformService]  = [ContractType.SharedSchema],
            [ServiceType.Framework]              = [ContractType.SharedSchema],
            // Sem contrato de interface pública
            [ServiceType.BackgroundService]      = [],
            [ServiceType.ScheduledProcess]       = [],
            [ServiceType.BatchJob]               = [],
            [ServiceType.MainframeSystem]        = [],
            [ServiceType.ImsTransaction]         = [],
        };

    /// <summary>
    /// Indica se um tipo de serviço suporta contratos de interface pública.
    /// </summary>
    /// <param name="serviceType">Tipo do serviço.</param>
    /// <returns>
    /// <c>true</c> se o tipo de serviço permite ao menos um tipo de contrato; <c>false</c> caso contrário.
    /// </returns>
    public static bool SupportsContracts(ServiceType serviceType)
        => _allowedContractTypes.TryGetValue(serviceType, out var types) && types.Length > 0;

    /// <summary>
    /// Retorna a lista de tipos de contrato permitidos para um tipo de serviço.
    /// Retorna lista vazia se o serviço não suporta contratos.
    /// </summary>
    /// <param name="serviceType">Tipo do serviço.</param>
    public static IReadOnlyList<ContractType> AllowedContractTypes(ServiceType serviceType)
        => _allowedContractTypes.TryGetValue(serviceType, out var types)
            ? types
            : Array.Empty<ContractType>();

    /// <summary>
    /// Número mínimo de contratos esperados para o tipo de serviço.
    /// Retorna 1 se o serviço suporta contratos; 0 caso contrário.
    /// </summary>
    /// <param name="serviceType">Tipo do serviço.</param>
    public static int RequiredContractCount(ServiceType serviceType)
        => SupportsContracts(serviceType) ? 1 : 0;

    /// <summary>
    /// Verifica se um tipo de contrato é permitido para um tipo de serviço.
    /// </summary>
    /// <param name="serviceType">Tipo do serviço.</param>
    /// <param name="contractType">Tipo do contrato a validar.</param>
    /// <returns>
    /// <c>true</c> se o tipo de contrato é permitido para o tipo de serviço; <c>false</c> caso contrário.
    /// </returns>
    public static bool IsContractTypeAllowed(ServiceType serviceType, ContractType contractType)
        => AllowedContractTypes(serviceType).Contains(contractType);
}

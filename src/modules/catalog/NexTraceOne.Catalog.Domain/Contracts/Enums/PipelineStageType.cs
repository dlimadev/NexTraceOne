namespace NexTraceOne.Catalog.Domain.Contracts.Enums;

/// <summary>
/// Tipos de estágio disponíveis no pipeline de geração de código a partir de contratos.
/// Cada estágio representa uma capacidade de geração automatizada.
/// </summary>
public enum PipelineStageType
{
    /// <summary>Geração de stubs de servidor a partir da especificação do contrato.</summary>
    ServerStubs = 0,

    /// <summary>Geração de SDK de cliente para consumidores do contrato.</summary>
    ClientSdk = 1,

    /// <summary>Geração de servidor mock para testes de integração.</summary>
    MockServer = 2,

    /// <summary>Geração de colecção Postman para exploração da API.</summary>
    PostmanCollection = 3,

    /// <summary>Geração de testes de contrato automatizados.</summary>
    ContractTests = 4,

    /// <summary>Validação de fitness do contrato contra políticas e boas práticas.</summary>
    FitnessValidation = 5
}

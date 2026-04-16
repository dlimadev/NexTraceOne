using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

#pragma warning disable CA1031

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Dispatcher que constrói o modelo canônico interno (ContractCanonicalModel)
/// delegando ao builder específico do protocolo.
/// Ver: OpenApiCanonicalModelBuilder, SwaggerCanonicalModelBuilder,
/// AsyncApiCanonicalModelBuilder, WsdlCanonicalModelBuilder, WorkerServiceCanonicalModelBuilder.
/// </summary>
public static class CanonicalModelBuilder
{
    /// <summary>
    /// Constrói o modelo canônico a partir do conteúdo da spec e seu protocolo.
    /// Delega ao builder específico do protocolo.
    /// </summary>
    public static ContractCanonicalModel Build(string specContent, ContractProtocol protocol)
    {
        return protocol switch
        {
            ContractProtocol.OpenApi => OpenApiCanonicalModelBuilder.Build(specContent),
            ContractProtocol.Swagger => SwaggerCanonicalModelBuilder.Build(specContent),
            ContractProtocol.AsyncApi => AsyncApiCanonicalModelBuilder.Build(specContent),
            ContractProtocol.Wsdl => WsdlCanonicalModelBuilder.Build(specContent),
            ContractProtocol.WorkerService => WorkerServiceCanonicalModelBuilder.Build(specContent),
            _ => CanonicalModelHelpers.EmptyModel(protocol)
        };
    }
}

namespace NexTraceOne.Catalog.Application.Graph.ConfigurationKeys;

/// <summary>
/// Chaves de configuração do módulo Catalog — integração Backstage bridge.
/// Todos os valores são geridos via IConfigurationResolutionService + ConfigurationDefinitionSeeder.
/// </summary>
public static class BackstageBridgeConfigKeys
{
    /// <summary>URL da instância Backstage de destino para exportação e anotação de source-url.</summary>
    public const string InstanceUrl = "integrations.backstage.instanceUrl";

    /// <summary>Indica se a exportação para o Backstage está habilitada.</summary>
    public const string ExportEnabled = "integrations.backstage.exportEnabled";
}

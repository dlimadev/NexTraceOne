using System.Text.Json.Serialization;

namespace NexTraceOne.Catalog.Domain.Contracts.Enums;

/// <summary>
/// Estratégia de migração recomendada pelo Schema Evolution Advisor
/// quando uma evolução de contrato requer ação dos consumidores ou produtores.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MigrationStrategy
{
    /// <summary>Dual-write — produzir dados em ambos os formatos durante período de transição.</summary>
    DualWrite = 0,

    /// <summary>Versionamento — manter múltiplas versões ativas em paralelo.</summary>
    Versioning = 1,

    /// <summary>Transformação — aplicar camada de transformação entre versões.</summary>
    Transformation = 2,

    /// <summary>Depreciação de campo — marcar campos como deprecated com prazo de remoção.</summary>
    FieldDeprecation = 3,

    /// <summary>Migração lazy — consumidores migram ao seu próprio ritmo com suporte a ambos formatos.</summary>
    LazyMigration = 4
}

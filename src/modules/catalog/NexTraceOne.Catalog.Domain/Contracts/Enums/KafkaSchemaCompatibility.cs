namespace NexTraceOne.Contracts.Domain.Enums;

/// <summary>
/// Modos de compatibilidade de schema suportados pelo Kafka Schema Registry.
/// Define as regras de evolução permitidas para um subject, controlando quais
/// mudanças em schemas são aceitas automaticamente e quais requerem governança.
/// </summary>
public enum KafkaSchemaCompatibility
{
    /// <summary>Sem verificação de compatibilidade — qualquer mudança é aceita.</summary>
    None = 0,

    /// <summary>Compatível com versões anteriores — consumers antigos conseguem ler dados novos.</summary>
    Backward = 1,

    /// <summary>Compatível com versão imediatamente anterior apenas.</summary>
    BackwardTransitive = 2,

    /// <summary>Compatível com versões futuras — producers novos geram dados legíveis por consumers antigos.</summary>
    Forward = 3,

    /// <summary>Compatível com versão imediatamente posterior apenas.</summary>
    ForwardTransitive = 4,

    /// <summary>Compatível em ambas as direções — backward + forward.</summary>
    Full = 5,

    /// <summary>Compatível em ambas as direções com todas as versões anteriores.</summary>
    FullTransitive = 6
}

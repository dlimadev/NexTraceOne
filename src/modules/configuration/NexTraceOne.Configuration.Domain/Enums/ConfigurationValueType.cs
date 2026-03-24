namespace NexTraceOne.Configuration.Domain.Enums;

/// <summary>
/// Tipo de valor suportado por uma configuração.
/// Define a forma como o valor é armazenado, validado e apresentado na interface.
/// </summary>
public enum ConfigurationValueType
{
    /// <summary>Valor textual simples — sem restrições de formato.</summary>
    String = 0,

    /// <summary>Valor numérico inteiro — validado como número sem casas decimais.</summary>
    Integer = 1,

    /// <summary>Valor numérico decimal — validado como número com casas decimais.</summary>
    Decimal = 2,

    /// <summary>Valor booleano — verdadeiro ou falso.</summary>
    Boolean = 3,

    /// <summary>Valor em formato JSON — validado como JSON estruturado.</summary>
    Json = 4,

    /// <summary>Lista de strings — valores múltiplos separados e armazenados como coleção.</summary>
    StringList = 5
}

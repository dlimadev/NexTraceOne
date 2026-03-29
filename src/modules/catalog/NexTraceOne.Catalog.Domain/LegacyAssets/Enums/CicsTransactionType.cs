namespace NexTraceOne.Catalog.Domain.LegacyAssets.Enums;

/// <summary>
/// Tipo de transação CICS.
/// Classifica o modo de processamento da transação.
/// </summary>
public enum CicsTransactionType
{
    /// <summary>Transação online padrão (conversational ou pseudo-conversational).</summary>
    Online = 0,

    /// <summary>Transação conversational — mantém sessão ativa.</summary>
    Conversational = 1,

    /// <summary>Transação pseudo-conversational — liberta recursos entre interações.</summary>
    Pseudo = 2,

    /// <summary>Transação web — exposta via CICS Web Services.</summary>
    Web = 3,

    /// <summary>Transação channel — usa channels/containers CICS.</summary>
    Channel = 4
}

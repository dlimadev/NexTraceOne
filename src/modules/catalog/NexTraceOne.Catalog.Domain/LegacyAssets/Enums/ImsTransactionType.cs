namespace NexTraceOne.Catalog.Domain.LegacyAssets.Enums;

/// <summary>
/// Tipo de transação IMS.
/// Classifica o modo de processamento IMS/DB.
/// </summary>
public enum ImsTransactionType
{
    /// <summary>Message Processing Program — processamento de mensagens online.</summary>
    MPP = 0,

    /// <summary>Batch Message Processing — processamento batch com acesso DL/I.</summary>
    BMP = 1,

    /// <summary>Fast Path — processamento de alta performance.</summary>
    FastPath = 2,

    /// <summary>IMS Fast Path — processamento dedicado IFP.</summary>
    IFP = 3
}

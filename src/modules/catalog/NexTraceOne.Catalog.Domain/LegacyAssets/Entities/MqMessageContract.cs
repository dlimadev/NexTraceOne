using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

/// <summary>
/// Contrato de mensagem MQ — descriptor e formato de payload para filas IBM MQ.
/// </summary>
public sealed class MqMessageContract : Entity<MqMessageContractId>
{
    private MqMessageContract() { }

    /// <summary>Nome da fila MQ.</summary>
    public string QueueName { get; private set; } = string.Empty;

    /// <summary>Formato da mensagem (ex.: MQFMT_STRING, CCSID).</summary>
    public string MessageFormat { get; private set; } = string.Empty;

    /// <summary>Schema do payload (opcional).</summary>
    public string? PayloadSchema { get; private set; }

    /// <summary>Referência ao copybook COBOL associado (se aplicável).</summary>
    public CopybookId? CopybookReference { get; private set; }

    /// <summary>Comprimento máximo da mensagem.</summary>
    public int? MaxMessageLength { get; private set; }

    /// <summary>Formato do header da mensagem.</summary>
    public string? HeaderFormat { get; private set; }

    /// <summary>Esquema de codificação (ex.: EBCDIC, UTF-8).</summary>
    public string? EncodingScheme { get; private set; }

    /// <summary>Sistema mainframe ao qual o contrato MQ pertence.</summary>
    public MainframeSystemId SystemId { get; private set; } = null!;

    /// <summary>Data de criação do registo.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data da última atualização.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>Cria um novo contrato de mensagem MQ.</summary>
    public static MqMessageContract Create(
        string queueName, string messageFormat, MainframeSystemId systemId)
    {
        Guard.Against.NullOrWhiteSpace(queueName);
        Guard.Against.NullOrWhiteSpace(messageFormat);
        Guard.Against.Null(systemId);

        return new MqMessageContract
        {
            Id = MqMessageContractId.New(),
            QueueName = queueName.Trim(),
            MessageFormat = messageFormat.Trim(),
            SystemId = systemId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Associa um copybook de referência ao contrato MQ.</summary>
    public void SetCopybookReference(CopybookId copybookId)
    {
        CopybookReference = copybookId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Atualiza detalhes adicionais do contrato MQ.</summary>
    public void UpdateDetails(
        string? payloadSchema, int? maxMessageLength,
        string? headerFormat, string? encodingScheme)
    {
        PayloadSchema = payloadSchema;
        MaxMessageLength = maxMessageLength;
        HeaderFormat = headerFormat;
        EncodingScheme = encodingScheme;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>Identificador fortemente tipado de MqMessageContract.</summary>
public sealed record MqMessageContractId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static MqMessageContractId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static MqMessageContractId From(Guid id) => new(id);
}

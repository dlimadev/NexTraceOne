namespace NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

/// <summary>
/// Marcador para todos os identificadores fortemente tipados da plataforma.
/// Usando ITypedId nos genéricos, garantimos que métodos como GetByIdAsync
/// só aceitem o tipo de Id correto, evitando confusões entre, por exemplo,
/// ReleaseId e AssetId que seriam ambos Guid sem esta abstração.
/// </summary>
public interface ITypedId
{
    /// <summary>Valor bruto do identificador (normalmente Guid).</summary>
    Guid Value { get; }
}

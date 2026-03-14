namespace NexTraceOne.BuildingBlocks.Application.Cqrs;

/// <summary>
/// Marcador para requests que exigem uma capability ativa na licença.
/// Commands ou queries que implementam esta interface serão verificados
/// pelo LicenseCapabilityBehavior antes da execução.
///
/// Exemplo de uso:
/// <code>
/// public sealed record Command(...) : ICommand&lt;Guid&gt;, IRequiresCapability
/// {
///     public string RequiredCapability => "catalog:write";
/// }
/// </code>
/// </summary>
public interface IRequiresCapability
{
    /// <summary>
    /// Código da capability exigida pela licença para executar este request.
    /// Códigos seguem o formato "módulo:ação" (ex.: "catalog:write").
    /// </summary>
    string RequiredCapability { get; }
}

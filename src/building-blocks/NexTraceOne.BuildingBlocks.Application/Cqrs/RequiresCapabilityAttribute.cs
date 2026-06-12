namespace NexTraceOne.BuildingBlocks.Application.Cqrs;

/// <summary>
/// Declara que um Command/Query exige uma capability de licença do tenant.
/// Aplicado ao record do request e verificado pelo CapabilityEnforcementBehavior
/// no pipeline MediatR — handlers não precisam checar capability manualmente.
/// Múltiplos atributos exigem TODAS as capabilities declaradas.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class RequiresCapabilityAttribute(string capability) : Attribute
{
    /// <summary>Chave da capability exigida (ex.: "contract_studio", "ai_enabled").</summary>
    public string Capability { get; } = capability;
}

using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.RulesetGovernance.Domain.Entities;

/// <summary>
/// Value Object que representa um finding individual de uma execução de linting.
/// Contém regra violada, severidade, mensagem descritiva e caminho do arquivo/local.
/// </summary>
public sealed class Finding : ValueObject
{
    private Finding() { }

    /// <summary>Nome da regra violada.</summary>
    public string Rule { get; private set; } = string.Empty;

    /// <summary>Severidade do finding.</summary>
    public FindingSeverity Severity { get; private set; }

    /// <summary>Mensagem descritiva do finding.</summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>Caminho do elemento no documento onde a violação foi encontrada.</summary>
    public string Path { get; private set; } = string.Empty;

    /// <summary>Cria um novo finding de linting.</summary>
    public static Finding Create(string rule, FindingSeverity severity, string message, string path)
    {
        return new Finding
        {
            Rule = rule,
            Severity = severity,
            Message = message,
            Path = path
        };
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Rule;
        yield return Severity;
        yield return Message;
        yield return Path;
    }
}

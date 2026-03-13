namespace NexTraceOne.Contracts.Domain.ValueObjects;

/// <summary>
/// Value object que representa uma versão semântica no formato Major.Minor.Patch.
/// Imutável e comparável por valor.
/// </summary>
public sealed record SemanticVersion(int Major, int Minor, int Patch)
{
    /// <summary>
    /// Tenta fazer o parse de uma string no formato "Major.Minor.Patch".
    /// Retorna null se o formato for inválido.
    /// </summary>
    public static SemanticVersion? Parse(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return null;

        var parts = version.Trim().Split('.');
        if (parts.Length != 3)
            return null;

        if (!int.TryParse(parts[0], out var major) || major < 0)
            return null;

        if (!int.TryParse(parts[1], out var minor) || minor < 0)
            return null;

        if (!int.TryParse(parts[2], out var patch) || patch < 0)
            return null;

        return new SemanticVersion(major, minor, patch);
    }

    /// <summary>Incrementa a versão major e reseta minor e patch para zero.</summary>
    public SemanticVersion BumpMajor() => new(Major + 1, 0, 0);

    /// <summary>Incrementa a versão minor e reseta patch para zero.</summary>
    public SemanticVersion BumpMinor() => new(Major, Minor + 1, 0);

    /// <summary>Incrementa a versão patch.</summary>
    public SemanticVersion BumpPatch() => new(Major, Minor, Patch + 1);

    /// <summary>Retorna a representação textual no formato "Major.Minor.Patch".</summary>
    public override string ToString() => $"{Major}.{Minor}.{Patch}";
}

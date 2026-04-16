namespace NexTraceOne.ApiHost.Preflight;

/// <summary>
/// Resultado de um check individual do preflight.
/// </summary>
/// <param name="Name">Nome do check (ex: "PostgreSQL", "Disk", "RAM").</param>
/// <param name="Status">Estado: Ok, Warning ou Error.</param>
/// <param name="Message">Mensagem legível por humanos com o resultado do check.</param>
/// <param name="Suggestion">Sugestão de resolução quando o estado não é Ok. Pode ser nulo.</param>
/// <param name="IsRequired">Indica se este check é obrigatório — false bloqueia o startup apenas em avisos.</param>
public sealed record PreflightCheckResult(
    string Name,
    PreflightCheckStatus Status,
    string Message,
    string? Suggestion = null,
    bool IsRequired = true);

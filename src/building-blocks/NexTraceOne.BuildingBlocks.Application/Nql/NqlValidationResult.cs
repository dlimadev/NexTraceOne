namespace NexTraceOne.BuildingBlocks.Application.Nql;

/// <summary>
/// Resultado da validação de sintaxe e governance de uma query NQL.
/// </summary>
public sealed class NqlValidationResult
{
    private NqlValidationResult() { }

    public bool IsValid { get; private init; }
    public string? Error { get; private init; }
    public NqlPlan? Plan { get; private init; }

    public static NqlValidationResult Ok(NqlPlan plan) =>
        new() { IsValid = true, Plan = plan };

    public static NqlValidationResult Fail(string error) =>
        new() { IsValid = false, Error = error };
}

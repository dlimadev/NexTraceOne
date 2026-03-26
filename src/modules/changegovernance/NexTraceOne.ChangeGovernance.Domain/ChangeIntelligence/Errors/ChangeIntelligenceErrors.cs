using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

/// <summary>
/// Catálogo centralizado de erros do módulo ChangeIntelligence com códigos i18n.
/// Cada erro possui código único para rastreabilidade em logs e documentação.
/// Padrão: ChangeIntelligence.{Entidade}.{Descrição}
/// </summary>
public static class ChangeIntelligenceErrors
{
    /// <summary>Release não encontrada.</summary>
    public static Error ReleaseNotFound(string releaseId)
        => Error.NotFound("ChangeIntelligence.Release.NotFound", "Release '{0}' was not found.", releaseId);

    /// <summary>Score de mudança inválido — deve estar entre 0.0 e 1.0.</summary>
    public static Error InvalidChangeScore(decimal score)
        => Error.Validation("ChangeIntelligence.Release.InvalidChangeScore", "Change score '{0}' is invalid. Value must be between 0.0 and 1.0.", score);

    /// <summary>Esta release já está marcada como rollback.</summary>
    public static Error AlreadyRollback()
        => Error.Conflict("ChangeIntelligence.Release.AlreadyRollback", "This release is already registered as a rollback.");

    /// <summary>Transição de status inválida para o deployment.</summary>
    public static Error InvalidStatusTransition(string from, string to)
        => Error.Conflict("ChangeIntelligence.Release.InvalidStatusTransition", "Cannot transition deployment status from '{0}' to '{1}'.", from, to);

    /// <summary>Relatório de blast radius não encontrado para a release informada.</summary>
    public static Error BlastRadiusReportNotFound(string releaseId)
        => Error.NotFound("ChangeIntelligence.BlastRadiusReport.NotFound", "Blast radius report for release '{0}' was not found.", releaseId);

    /// <summary>Score de change intelligence não encontrado para a release informada.</summary>
    public static Error ChangeScoreNotFound(string releaseId)
        => Error.NotFound("ChangeIntelligence.ChangeIntelligenceScore.NotFound", "Change intelligence score for release '{0}' was not found.", releaseId);

    /// <summary>Status de deployment inválido.</summary>
    public static Error InvalidDeploymentStatus(string status)
        => Error.Validation("ChangeIntelligence.Release.InvalidDeploymentStatus", "'{0}' is not a valid deployment status.", status);

    /// <summary>Já existe uma review pós-release para esta release.</summary>
    public static Error PostReleaseReviewAlreadyExists(string releaseId)
        => Error.Conflict("ChangeIntelligence.PostReleaseReview.AlreadyExists", "A post-release review already exists for release '{0}'.", releaseId);

    /// <summary>Review pós-release não encontrada para a release informada.</summary>
    public static Error PostReleaseReviewNotFound(string releaseId)
        => Error.NotFound("ChangeIntelligence.PostReleaseReview.NotFound", "Post-release review for release '{0}' was not found.", releaseId);

    /// <summary>Baseline não encontrado para a release informada. Necessário para post-change verification.</summary>
    public static Error BaselineNotFound(string releaseId)
        => Error.NotFound("ChangeIntelligence.ReleaseBaseline.NotFound", "Baseline for release '{0}' was not found. Record a baseline before submitting observation metrics.", releaseId);
}

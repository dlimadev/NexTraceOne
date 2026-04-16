using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Catalog.Domain.Contracts.Errors;

/// <summary>
/// Catálogo centralizado de erros do módulo Contracts com códigos i18n.
/// Cada erro possui código único para rastreabilidade em logs e documentação.
/// Padrão: Contracts.{Entidade}.{Descrição}
/// </summary>
public static class ContractsErrors
{
    // ── ContractVersion ─────────────────────────────────────────────

    /// <summary>Versão de contrato não encontrada.</summary>
    public static Error ContractVersionNotFound(string id)
        => Error.NotFound("Contracts.ContractVersion.NotFound", "Contract version '{0}' was not found.", id);

    /// <summary>Versão semântica já existe para este ativo de API.</summary>
    public static Error AlreadyExists(string semVer, string apiAssetId)
        => Error.Conflict("Contracts.ContractVersion.AlreadyExists", "Contract version '{0}' already exists for API asset '{1}'.", semVer, apiAssetId);

    /// <summary>A versão de contrato já está bloqueada.</summary>
    public static Error AlreadyLocked(string semVer)
        => Error.Conflict("Contracts.ContractVersion.AlreadyLocked", "Contract version '{0}' is already locked.", semVer);

    /// <summary>Versão semântica inválida — deve seguir o formato Major.Minor.Patch.</summary>
    public static Error InvalidSemVer(string version)
        => Error.Validation("Contracts.ContractVersion.InvalidSemVer", "'{0}' is not a valid semantic version. Expected format: Major.Minor.Patch.", version);

    /// <summary>Conteúdo da especificação está vazio.</summary>
    public static Error EmptySpecContent()
        => Error.Validation("Contracts.ContractVersion.EmptySpecContent", "The spec content cannot be empty.");

    /// <summary>Nenhuma versão anterior encontrada para este ativo de API.</summary>
    public static Error NoPreviousVersion(string apiAssetId)
        => Error.Business("Contracts.ContractVersion.NoPreviousVersion", "No previous contract version found for API asset '{0}'.", apiAssetId);

    // ── Lifecycle ───────────────────────────────────────────────────

    /// <summary>Transição de lifecycle inválida entre estados.</summary>
    public static Error InvalidLifecycleTransition(string fromState, string toState)
        => Error.Business("Contracts.Lifecycle.InvalidTransition", "Cannot transition from '{0}' to '{1}'.", fromState, toState);

    /// <summary>Não é possível assinar o contrato no estado atual.</summary>
    public static Error CannotSignInCurrentState(string state)
        => Error.Business("Contracts.Signing.InvalidState", "Cannot sign contract in state '{0}'. Contract must be Approved or Locked.", state);

    /// <summary>Verificação de integridade da assinatura falhou.</summary>
    public static Error SignatureVerificationFailed(string contractVersionId)
        => Error.Business("Contracts.Signing.VerificationFailed", "Signature verification failed for contract version '{0}'.", contractVersionId);

    /// <summary>Data de sunset deve ser posterior à data de depreciação.</summary>
    public static Error SunsetDateMustBeAfterDeprecation(string deprecationDate, string sunsetDate)
        => Error.Validation("Contracts.Lifecycle.SunsetDateBeforeDeprecation", "Sunset date '{1}' must be after deprecation date '{0}'.", deprecationDate, sunsetDate);

    /// <summary>Protocolo incompatível entre versões base e alvo para cálculo de diff.</summary>
    public static Error ProtocolMismatchForDiff(string baseProtocol, string targetProtocol)
        => Error.Validation("Contracts.ContractDiff.ProtocolMismatch", "Cannot compute diff between different protocols: base='{0}', target='{1}'.", baseProtocol, targetProtocol);

    // ── ContractDiff ────────────────────────────────────────────────

    /// <summary>Falha ao computar o diff entre versões de contrato.</summary>
    public static Error DiffComputationFailed(string reason)
        => Error.Business("Contracts.ContractDiff.ComputationFailed", "Failed to compute contract diff: {0}", reason);

    /// <summary>Nenhum diff encontrado para a versão de contrato informada.</summary>
    public static Error DiffNotFound(string contractVersionId)
        => Error.NotFound("Contracts.ContractDiff.NotFound", "No diff found for contract version '{0}'.", contractVersionId);

    // ── Protocol ────────────────────────────────────────────────────

    /// <summary>Protocolo de contrato não suportado.</summary>
    public static Error UnsupportedProtocol(string protocol)
        => Error.Validation("Contracts.Protocol.Unsupported", "Protocol '{0}' is not supported.", protocol);

    // ── Artifacts ───────────────────────────────────────────────────

    /// <summary>Artefato não encontrado.</summary>
    public static Error ArtifactNotFound(string id)
        => Error.NotFound("Contracts.Artifact.NotFound", "Contract artifact '{0}' was not found.", id);

    // ── Size Limits ─────────────────────────────────────────────────

    /// <summary>Conteúdo da especificação excede o tamanho máximo permitido.</summary>
    public static Error SpecContentTooLarge(int maxSizeMb)
        => Error.Validation("Contracts.ContractVersion.SpecContentTooLarge", "Spec content exceeds maximum allowed size of {0}MB.", maxSizeMb);

    // ── Format Detection ────────────────────────────────────────────

    /// <summary>Formato de arquivo não reconhecido para detecção de protocolo.</summary>
    public static Error UnrecognizedFormat(string format)
        => Error.Validation("Contracts.Protocol.UnrecognizedFormat", "Could not recognize the contract format: '{0}'.", format);

    // ── Rulesets ────────────────────────────────────────────────────

    /// <summary>Nenhum ruleset encontrado para o protocolo informado.</summary>
    public static Error NoRulesetsForProtocol(string protocol)
        => Error.Business("Contracts.Ruleset.NotFound", "No rulesets found for protocol '{0}'.", protocol);

    // ── ContractDraft ───────────────────────────────────────────────

    /// <summary>Draft de contrato requer um serviço vinculado.</summary>
    public static Error DraftRequiresServiceId()
        => Error.Validation(
            "Contracts.Draft.RequiresServiceId",
            "A contract draft must be linked to a service.");

    /// <summary>Tipo de serviço não suporta contratos de interface pública.</summary>
    public static Error ServiceTypeDoesNotSupportContracts(string serviceType)
        => Error.Validation(
            "Contracts.Draft.ServiceTypeDoesNotSupportContracts",
            "Service type '{0}' does not support contracts.",
            serviceType);

    /// <summary>Tipo de contrato não é permitido para o tipo de serviço informado.</summary>
    public static Error ContractTypeNotAllowedForServiceType(string contractType, string serviceType)
        => Error.Validation(
            "Contracts.Draft.ContractTypeNotAllowedForServiceType",
            "Contract type '{0}' is not allowed for service type '{1}'.",
            contractType,
            serviceType);

    /// <summary>Draft de contrato não encontrado.</summary>
    public static Error DraftNotFound(string id)
        => Error.NotFound("Contracts.Draft.NotFound", "Contract draft '{0}' was not found.", id);

    /// <summary>Draft não pode ser editado no estado atual.</summary>
    public static Error DraftNotEditable(string id)
        => Error.Business("Contracts.Draft.NotEditable", "Contract draft '{0}' cannot be edited in its current state.", id);

    /// <summary>Transição de estado de draft inválida.</summary>
    public static Error InvalidDraftTransition(string fromStatus, string toStatus)
        => Error.Business("Contracts.Draft.InvalidTransition", "Cannot transition draft from '{0}' to '{1}'.", fromStatus, toStatus);

    /// <summary>Draft precisa estar ligado a um serviço ou API asset real antes da publicação.</summary>
    public static Error DraftMissingCatalogLink(string draftId)
        => Error.Validation(
            "Contracts.Draft.MissingCatalogLink",
            "Contract draft '{0}' must be linked to a real service or API asset before publication.",
            draftId);

    /// <summary>Referência de catálogo associada ao draft não foi encontrada.</summary>
    public static Error CatalogLinkNotFound(string linkedId)
        => Error.NotFound(
            "Contracts.Catalog.LinkNotFound",
            "No service or API asset was found for linked identifier '{0}'.",
            linkedId);

    // ── SOAP/WSDL ────────────────────────────────────────────────────

    /// <summary>Detalhe SOAP não encontrado para a versão de contrato informada.</summary>
    public static Error SoapDetailNotFound(string contractVersionId)
        => Error.NotFound("Contracts.Soap.DetailNotFound", "SOAP detail not found for contract version '{0}'.", contractVersionId);

    /// <summary>Detalhe SOAP já existe para a versão de contrato informada.</summary>
    public static Error SoapDetailAlreadyExists(string contractVersionId)
        => Error.Conflict("Contracts.Soap.DetailAlreadyExists", "SOAP detail already exists for contract version '{0}'.", contractVersionId);

    /// <summary>Metadado SOAP de draft não encontrado para o draft informado.</summary>
    public static Error SoapDraftMetadataNotFound(string draftId)
        => Error.NotFound("Contracts.Soap.DraftMetadataNotFound", "SOAP draft metadata not found for draft '{0}'.", draftId);

    /// <summary>Operação de importação WSDL requer conteúdo XML válido.</summary>
    public static Error InvalidWsdlContent()
        => Error.Validation("Contracts.Soap.InvalidWsdlContent", "The provided content is not a valid WSDL document. Expected XML with WSDL definitions.");

    // ── Event Contracts / AsyncAPI ────────────────────────────────────────────────

    /// <summary>Detalhe de Event Contract não encontrado para a versão de contrato informada.</summary>
    public static Error EventDetailNotFound(string contractVersionId)
        => Error.NotFound("Contracts.Event.DetailNotFound", "Event contract detail not found for contract version '{0}'.", contractVersionId);

    /// <summary>Metadado de Event Draft não encontrado para o draft informado.</summary>
    public static Error EventDraftMetadataNotFound(string draftId)
        => Error.NotFound("Contracts.Event.DraftMetadataNotFound", "Event draft metadata not found for draft '{0}'.", draftId);

    /// <summary>Operação de importação AsyncAPI requer conteúdo JSON válido com campo asyncapi.</summary>
    public static Error InvalidAsyncApiContent()
        => Error.Validation("Contracts.Event.InvalidAsyncApiContent", "The provided content is not a valid AsyncAPI document. Expected JSON with 'asyncapi' field.");

    // ── Background Service Contracts ─────────────────────────────────────────────

    /// <summary>Detalhe de Background Service Contract não encontrado para a versão de contrato informada.</summary>
    public static Error BackgroundServiceDetailNotFound(string contractVersionId)
        => Error.NotFound("Contracts.BackgroundService.DetailNotFound", "Background service contract detail not found for contract version '{0}'.", contractVersionId);

    /// <summary>Metadado de Background Service Draft não encontrado para o draft informado.</summary>
    public static Error BackgroundServiceDraftMetadataNotFound(string draftId)
        => Error.NotFound("Contracts.BackgroundService.DraftMetadataNotFound", "Background service draft metadata not found for draft '{0}'.", draftId);

    /// <summary>Nome do serviço em background é obrigatório para registro deste tipo contratual.</summary>
    public static Error BackgroundServiceNameRequired()
        => Error.Validation("Contracts.BackgroundService.ServiceNameRequired", "Service name is required for background service contracts.");

    // ── CanonicalEntity ─────────────────────────────────────────────

    /// <summary>Entidade canónica não encontrada.</summary>
    public static Error CanonicalEntityNotFound(string id)
        => Error.NotFound("Contracts.CanonicalEntity.NotFound", "Canonical entity '{0}' was not found.", id);

    /// <summary>Versão de entidade canónica não encontrada.</summary>
    public static Error CanonicalEntityVersionNotFound(string version)
        => Error.NotFound("Contracts.CanonicalEntityVersion.NotFound", "Canonical entity version '{0}' was not found.", version);

    // ── ContractExample ─────────────────────────────────────────────

    /// <summary>Exemplo de contrato não encontrado.</summary>
    public static Error ExampleNotFound(string id)
        => Error.NotFound("Contracts.Example.NotFound", "Contract example '{0}' was not found.", id);

    // ── ContractReview ──────────────────────────────────────────────

    /// <summary>Revisão de contrato não encontrada.</summary>
    public static Error ReviewNotFound(string id)
        => Error.NotFound("Contracts.Review.NotFound", "Contract review '{0}' was not found.", id);

    // ── ConsumerExpectation ─────────────────────────────────────────

    /// <summary>Expectativa de consumidor não encontrada.</summary>
    public static Error ConsumerExpectationNotFound(string id)
        => Error.NotFound("Contracts.ConsumerExpectation.NotFound", "Consumer expectation '{0}' was not found.", id);

    /// <summary>Score de saúde de contrato não encontrado para o API Asset informado.</summary>
    public static Error ContractHealthScoreNotFound(string apiAssetId)
        => Error.NotFound("Contracts.HealthScore.NotFound", "Contract health score not found for API asset '{0}'.", apiAssetId);

    /// <summary>Nenhuma versão de contrato encontrada para calcular o score de saúde.</summary>
    public static Error NoVersionsForHealthScore(string apiAssetId)
        => Error.NotFound("Contracts.HealthScore.NoVersions", "No contract versions found for API asset '{0}' to compute health score.", apiAssetId);

    /// <summary>Formato de exportação não suportado.</summary>
    public static Error UnsupportedExportFormat(string format)
        => Error.Validation("Contracts.Export.UnsupportedFormat", "Export format '{0}' is not supported.", format);

    // ── PipelineExecution ───────────────────────────────────────────

    /// <summary>Execução de pipeline não encontrada.</summary>
    public static Error PipelineExecutionNotFound(string id)
        => Error.NotFound("Contracts.PipelineExecution.NotFound", "Pipeline execution '{0}' was not found.", id);

    // ── ContractNegotiation ─────────────────────────────────────────

    /// <summary>Negociação de contrato não encontrada.</summary>
    public static Error ContractNegotiationNotFound(string id)
        => Error.NotFound("Contracts.Negotiation.NotFound", "Contract negotiation '{0}' was not found.", id);

    /// <summary>Transição de estado de negociação inválida.</summary>
    public static Error NegotiationInvalidStatusTransition(string from, string to)
        => Error.Business("Contracts.Negotiation.InvalidTransition", "Cannot transition negotiation from '{0}' to '{1}'.", from, to);

    // ── SchemaEvolutionAdvice ────────────────────────────────────────

    /// <summary>Análise de evolução de schema não encontrada.</summary>
    public static Error SchemaEvolutionAdviceNotFound(string id)
        => Error.NotFound("Contracts.SchemaEvolutionAdvice.NotFound", "Schema evolution advice '{0}' was not found.", id);

    // ── SemanticDiffResult ──────────────────────────────────────────

    /// <summary>Resultado de diff semântico não encontrado.</summary>
    public static Error SemanticDiffResultNotFound(string id)
        => Error.NotFound("Contracts.SemanticDiffResult.NotFound", "Semantic diff result '{0}' was not found.", id);

    // ── ContractComplianceGate ───────────────────────────────────────

    /// <summary>Gate de compliance contratual não encontrado.</summary>
    public static Error ComplianceGateNotFound(string id)
        => Error.NotFound("Contracts.ComplianceGate.NotFound", "Contract compliance gate '{0}' was not found.", id);

    /// <summary>Resultado de compliance contratual não encontrado.</summary>
    public static Error ComplianceResultNotFound(string id)
        => Error.NotFound("Contracts.ComplianceResult.NotFound", "Contract compliance result '{0}' was not found.", id);

    // ── ContractListing ─────────────────────────────────────────────

    /// <summary>Listagem de contrato no marketplace não encontrada.</summary>
    public static Error ContractListingNotFound(string id)
        => Error.NotFound("Contracts.Listing.NotFound", "Contract listing '{0}' was not found.", id);

    // ── MarketplaceReview ───────────────────────────────────────────

    /// <summary>Avaliação de contrato no marketplace não encontrada.</summary>
    public static Error MarketplaceReviewNotFound(string id)
        => Error.NotFound("Contracts.MarketplaceReview.NotFound", "Marketplace review '{0}' was not found.", id);

    // ── ImpactSimulation ────────────────────────────────────────────

    /// <summary>Simulação de impacto não encontrada.</summary>
    public static Error ImpactSimulationNotFound(string id)
        => Error.NotFound("Contracts.ImpactSimulation.NotFound", "Impact simulation '{0}' was not found.", id);

    // ── ContractVerification ──────────────────────────────────────────

    /// <summary>Verificação de contrato não encontrada.</summary>
    public static Error VerificationNotFound(string id)
        => Error.NotFound("Contracts.Verification.NotFound", "Contract verification '{0}' was not found.", id);

    /// <summary>Nenhuma versão de contrato aprovada ou bloqueada encontrada para o ativo de API.</summary>
    public static Error VerificationContractNotFound(string apiAssetId)
        => Error.NotFound("Contracts.Verification.ContractNotFound", "No approved or locked contract version found for API asset '{0}'.", apiAssetId);

    /// <summary>Conteúdo da especificação não pode estar vazio para verificação.</summary>
    public static Error VerificationSpecContentEmpty()
        => Error.Validation("Contracts.Verification.SpecContentEmpty", "Spec content cannot be empty for verification.");

    // ── ContractChangelog ─────────────────────────────────────────────

    /// <summary>Entrada de changelog de contrato não encontrada.</summary>
    public static Error ChangelogNotFound(string id)
        => Error.NotFound("Contracts.Changelog.NotFound", "Contract changelog '{0}' was not found.", id);

    /// <summary>Entrada de changelog já se encontra aprovada.</summary>
    public static Error ChangelogAlreadyApproved(string id)
        => Error.Conflict("Contracts.Changelog.AlreadyApproved", "Contract changelog '{0}' is already approved.", id);
}


using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Aggregate Root que representa uma versão versionada de um contrato multi-protocolo.
/// Gerencia o ciclo de vida completo da versão, incluindo importação, bloqueio, assinatura,
/// lifecycle states (Draft → InReview → Approved → Locked → Deprecated → Sunset → Retired),
/// proveniência, diffs e artefatos associados.
/// Suporta OpenAPI, Swagger, WSDL, AsyncAPI e formatos futuros (Protobuf, GraphQL).
/// </summary>
public sealed class ContractVersion : AuditableEntity<ContractVersionId>
{
    private readonly List<ContractDiff> _diffs = [];
    private readonly List<ContractRuleViolation> _ruleViolations = [];
    private readonly List<ContractArtifact> _artifacts = [];

    private ContractVersion() { }

    /// <summary>Identificador do ativo de API correspondente no módulo Catalog Graph.</summary>
    public Guid ApiAssetId { get; private set; }

    /// <summary>Versão semântica do contrato, ex: "1.2.3".</summary>
    public string SemVer { get; private set; } = string.Empty;

    /// <summary>Conteúdo bruto da especificação (JSON, YAML ou XML, máx. 1MB).</summary>
    public string SpecContent { get; private set; } = string.Empty;

    /// <summary>Formato da especificação: "json", "yaml" ou "xml".</summary>
    public string Format { get; private set; } = string.Empty;

    /// <summary>Protocolo do contrato (OpenAPI, WSDL, AsyncAPI, etc.).</summary>
    public ContractProtocol Protocol { get; private set; }

    /// <summary>Estado atual no ciclo de vida do contrato.</summary>
    public ContractLifecycleState LifecycleState { get; private set; }

    /// <summary>Origem do import: URL, "upload", "ai-generated" ou "migration".</summary>
    public string ImportedFrom { get; private set; } = string.Empty;

    /// <summary>Indica se esta versão está bloqueada contra novas alterações.</summary>
    public bool IsLocked { get; private set; }

    /// <summary>Data/hora em que a versão foi bloqueada, se aplicável.</summary>
    public DateTimeOffset? LockedAt { get; private set; }

    /// <summary>Usuário que bloqueou a versão, se aplicável.</summary>
    public string? LockedBy { get; private set; }

    /// <summary>Assinatura digital do contrato após promoção/locking.</summary>
    public ContractSignature? Signature { get; private set; }

    /// <summary>Proveniência (lineage) do contrato: origem, parser, padrão, importador.</summary>
    public ContractProvenance? Provenance { get; private set; }

    /// <summary>Data de depreciação, se o contrato estiver no estado Deprecated/Sunset.</summary>
    public DateTimeOffset? DeprecationDate { get; private set; }

    /// <summary>Data de sunset (encerramento definitivo) do contrato.</summary>
    public DateTimeOffset? SunsetDate { get; private set; }

    /// <summary>Mensagem de depreciação para consumers.</summary>
    public string? DeprecationNotice { get; private set; }

    /// <summary>
    /// Acordo de nível de serviço (SLA/SLO) associado a este contrato.
    /// Permite correlação com observabilidade e change intelligence.
    /// </summary>
    public ContractSla? Sla { get; private set; }

    /// <summary>
    /// Último score geral calculado para esta versão de contrato (0.0 a 1.0).
    /// Materializado na última chamada ao endpoint de scorecard para permitir
    /// exibição de badge de qualidade na listagem do catálogo sem recálculo.
    /// </summary>
    public decimal? LastOverallScore { get; private set; }

    /// <summary>
    /// Token de concorrência otimista (PostgreSQL xmin).
    /// Utilizado pelo EF Core para detetar conflitos de escrita concorrente.
    /// </summary>
    public uint RowVersion { get; set; }

    /// <summary>Diffs computados associados a esta versão.</summary>
    public IReadOnlyList<ContractDiff> Diffs => _diffs.AsReadOnly();

    /// <summary>Violações de ruleset detectadas nesta versão.</summary>
    public IReadOnlyList<ContractRuleViolation> RuleViolations => _ruleViolations.AsReadOnly();

    /// <summary>Artefatos gerados a partir desta versão (testes, scaffolds, evidências).</summary>
    public IReadOnlyList<ContractArtifact> Artifacts => _artifacts.AsReadOnly();

    /// <summary>
    /// Importa uma nova versão de contrato para o sistema.
    /// Suporta múltiplos protocolos (OpenAPI, Swagger, WSDL, AsyncAPI).
    /// Retorna falha se a versão semântica for inválida ou o conteúdo estiver vazio.
    /// </summary>
    public static Result<ContractVersion> Import(
        Guid apiAssetId,
        string semVer,
        string specContent,
        string format,
        string importedFrom,
        ContractProtocol protocol = ContractProtocol.OpenApi)
    {
        Guard.Against.Default(apiAssetId);
        Guard.Against.NullOrWhiteSpace(format);
        Guard.Against.NullOrWhiteSpace(importedFrom);

        if (string.IsNullOrWhiteSpace(semVer) || SemanticVersion.Parse(semVer) is null)
            return ContractsErrors.InvalidSemVer(semVer ?? string.Empty);

        if (string.IsNullOrWhiteSpace(specContent))
            return ContractsErrors.EmptySpecContent();

        return new ContractVersion
        {
            Id = ContractVersionId.New(),
            ApiAssetId = apiAssetId,
            SemVer = semVer,
            SpecContent = specContent,
            Format = format.ToLowerInvariant(),
            ImportedFrom = importedFrom,
            Protocol = protocol,
            LifecycleState = ContractLifecycleState.Draft,
            IsLocked = false
        };
    }

    /// <summary>
    /// Bloqueia esta versão do contrato, impedindo novas alterações.
    /// Retorna falha se a versão já estiver bloqueada.
    /// </summary>
    public Result<Unit> Lock(string lockedBy, DateTimeOffset lockedAt)
    {
        Guard.Against.NullOrWhiteSpace(lockedBy);

        if (IsLocked)
            return ContractsErrors.AlreadyLocked(SemVer);

        IsLocked = true;
        LockedAt = lockedAt;
        LockedBy = lockedBy;
        LifecycleState = ContractLifecycleState.Locked;
        return Unit.Value;
    }

    /// <summary>
    /// Transiciona o estado do ciclo de vida do contrato, validando transições permitidas.
    /// Transições inválidas retornam erro de domínio, garantindo integridade do fluxo.
    /// </summary>
    public Result<Unit> TransitionTo(ContractLifecycleState newState, DateTimeOffset at)
    {
        if (!IsValidTransition(LifecycleState, newState))
            return ContractsErrors.InvalidLifecycleTransition(LifecycleState.ToString(), newState.ToString());

        LifecycleState = newState;

        if (newState == ContractLifecycleState.Locked && !IsLocked)
        {
            IsLocked = true;
            LockedAt = at;
        }

        return Unit.Value;
    }

    /// <summary>
    /// Aplica assinatura digital ao contrato após canonicalização.
    /// Requer que o contrato esteja no estado Locked ou Approved.
    /// </summary>
    public Result<Unit> Sign(ContractSignature signature)
    {
        Guard.Against.Null(signature);

        if (LifecycleState is not (ContractLifecycleState.Locked or ContractLifecycleState.Approved))
            return ContractsErrors.CannotSignInCurrentState(LifecycleState.ToString());

        Signature = signature;
        return Unit.Value;
    }

    /// <summary>
    /// Deprecia o contrato com aviso para consumers e data de sunset opcional.
    /// </summary>
    public Result<Unit> Deprecate(string notice, DateTimeOffset deprecatedAt, DateTimeOffset? sunsetDate)
    {
        Guard.Against.NullOrWhiteSpace(notice);

        if (LifecycleState is ContractLifecycleState.Retired or ContractLifecycleState.Draft)
            return ContractsErrors.InvalidLifecycleTransition(LifecycleState.ToString(), ContractLifecycleState.Deprecated.ToString());

        LifecycleState = ContractLifecycleState.Deprecated;
        DeprecationDate = deprecatedAt;
        DeprecationNotice = notice;
        SunsetDate = sunsetDate;
        return Unit.Value;
    }

    /// <summary>
    /// Registra a proveniência (lineage) do contrato para rastreabilidade.
    /// </summary>
    public void SetProvenance(ContractProvenance provenance)
    {
        Guard.Against.Null(provenance);
        Provenance = provenance;
    }

    /// <summary>Associa um diff computado a esta versão do contrato.</summary>
    public void AddDiff(ContractDiff diff)
    {
        Guard.Against.Null(diff);
        _diffs.Add(diff);
    }

    /// <summary>Registra uma violação de ruleset detectada nesta versão.</summary>
    public void AddRuleViolation(ContractRuleViolation violation)
    {
        Guard.Against.Null(violation);
        _ruleViolations.Add(violation);
    }

    /// <summary>Associa um artefato gerado a esta versão do contrato.</summary>
    public void AddArtifact(ContractArtifact artifact)
    {
        Guard.Against.Null(artifact);
        _artifacts.Add(artifact);
    }

    /// <summary>
    /// Define ou actualiza o SLA/SLO associado a esta versão do contrato.
    /// Permite registar expectativas de disponibilidade, latência e throughput.
    /// </summary>
    public void SetSla(ContractSla sla)
    {
        Guard.Against.Null(sla);
        Sla = sla;
    }

    /// <summary>Remove o SLA associado a esta versão do contrato.</summary>
    public void ClearSla() => Sla = null;

    /// <summary>
    /// Actualiza o último score geral calculado para esta versão de contrato.
    /// Chamado após geração de scorecard para materializar o valor no catálogo.
    /// </summary>
    public void UpdateLastOverallScore(decimal score)
    {
        Guard.Against.OutOfRange(score, nameof(score), 0m, 1m);
        LastOverallScore = score;
    }

    /// <summary>
    /// Valida se a transição entre estados do lifecycle é permitida.
    /// Implementa a máquina de estados: Draft → InReview → Approved → Locked → Deprecated → Sunset → Retired.
    /// </summary>
    private static bool IsValidTransition(ContractLifecycleState from, ContractLifecycleState to) =>
        (from, to) switch
        {
            (ContractLifecycleState.Draft, ContractLifecycleState.InReview) => true,
            (ContractLifecycleState.InReview, ContractLifecycleState.Approved) => true,
            (ContractLifecycleState.InReview, ContractLifecycleState.Draft) => true,
            (ContractLifecycleState.Approved, ContractLifecycleState.Locked) => true,
            (ContractLifecycleState.Approved, ContractLifecycleState.InReview) => true,
            (ContractLifecycleState.Locked, ContractLifecycleState.Deprecated) => true,
            (ContractLifecycleState.Deprecated, ContractLifecycleState.Sunset) => true,
            (ContractLifecycleState.Sunset, ContractLifecycleState.Retired) => true,
            _ => false
        };
}

/// <summary>Identificador fortemente tipado de ContractVersion.</summary>
public sealed record ContractVersionId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ContractVersionId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ContractVersionId From(Guid id) => new(id);
}

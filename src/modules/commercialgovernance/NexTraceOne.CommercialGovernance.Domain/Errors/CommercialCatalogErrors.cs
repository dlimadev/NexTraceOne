using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.CommercialCatalog.Domain.Errors;

/// <summary>
/// Catálogo centralizado de erros do subdomínio CommercialCatalog com códigos i18n.
/// Cada código segue o padrão "CommercialCatalog.{Entidade}.{Condição}" para
/// mapeamento direto com chaves de tradução no frontend.
///
/// Decisão de design:
/// - Separado de LicensingErrors para manter responsabilidades distintas entre subdomínios.
/// - Códigos estáveis e versionados para compatibilidade com i18n.
/// - Mensagens técnicas em inglês; frontend resolve a mensagem final via i18n.
/// </summary>
public static class CommercialCatalogErrors
{
    // ─── Plan ────────────────────────────────────────────────────────

    /// <summary>Plano não encontrado pelo identificador.</summary>
    public static Error PlanNotFound(Guid planId)
        => Error.NotFound("CommercialCatalog.Plan.NotFound", "Plan '{0}' was not found.", planId);

    /// <summary>Código de plano já em uso por outro plano.</summary>
    public static Error PlanCodeAlreadyExists(string code)
        => Error.Conflict("CommercialCatalog.Plan.CodeAlreadyExists", "A plan with code '{0}' already exists.", code);

    // ─── FeaturePack ─────────────────────────────────────────────────

    /// <summary>Pacote de funcionalidades não encontrado pelo identificador.</summary>
    public static Error FeaturePackNotFound(Guid featurePackId)
        => Error.NotFound("CommercialCatalog.FeaturePack.NotFound", "Feature pack '{0}' was not found.", featurePackId);

    /// <summary>Código de pacote já em uso por outro pacote.</summary>
    public static Error FeaturePackCodeAlreadyExists(string code)
        => Error.Conflict("CommercialCatalog.FeaturePack.CodeAlreadyExists", "A feature pack with code '{0}' already exists.", code);

    // ─── License Key ─────────────────────────────────────────────────

    /// <summary>Licença não encontrada para geração de chave.</summary>
    public static Error LicenseNotFoundForKeyGeneration(Guid licenseId)
        => Error.NotFound("CommercialCatalog.LicenseKey.LicenseNotFound", "License '{0}' was not found for key generation.", licenseId);
}

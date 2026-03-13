namespace NexTraceOne.Licensing.Domain.Enums;

/// <summary>
/// Tipo de licença que define o modelo operacional e as regras de ciclo de vida.
/// Usado para distinguir licenças de avaliação, padrão e enterprise,
/// cada uma com diferentes políticas de expiração, limites e enforcement.
/// </summary>
public enum LicenseType
{
    /// <summary>Licença de avaliação com duração limitada e limites reduzidos.</summary>
    Trial = 0,

    /// <summary>Licença padrão com limites definidos pelo plano contratado.</summary>
    Standard = 1,

    /// <summary>Licença enterprise com limites estendidos e suporte avançado.</summary>
    Enterprise = 2
}

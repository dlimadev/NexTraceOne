namespace NexTraceOne.Licensing.Domain.Enums;

/// <summary>
/// Edição comercial da licença que determina o conjunto base de capabilities e limites.
/// Cada edição define um perfil de capacidades e quotas que pode ser refinado
/// por entitlements adicionais.
///
/// Decisão de design:
/// - Community é gratuito com limites básicos, para adoção inicial.
/// - Professional é o nível padrão comercial.
/// - Enterprise oferece limites estendidos e capabilities avançadas.
/// - Unlimited é reservado para parceiros estratégicos ou contratos especiais.
/// </summary>
public enum LicenseEdition
{
    /// <summary>Edição comunitária com limites básicos e funcionalidades essenciais.</summary>
    Community = 0,

    /// <summary>Edição profissional com limites intermediários e funcionalidades completas.</summary>
    Professional = 1,

    /// <summary>Edição enterprise com limites estendidos e funcionalidades avançadas.</summary>
    Enterprise = 2,

    /// <summary>Edição sem limites — reservada para parceiros e contratos especiais.</summary>
    Unlimited = 3
}

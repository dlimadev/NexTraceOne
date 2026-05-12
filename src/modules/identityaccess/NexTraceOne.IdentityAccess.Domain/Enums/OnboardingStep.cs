namespace NexTraceOne.IdentityAccess.Domain.Enums;

/// <summary>Passos do wizard de onboarding para novos tenants.</summary>
public enum OnboardingStep
{
    /// <summary>Instalação do NexTrace Agent com cópia de API key.</summary>
    Install,
    /// <summary>Aguardar primeiro heartbeat do agente.</summary>
    FirstSignal,
    /// <summary>Registar o primeiro serviço.</summary>
    RegisterService,
    /// <summary>Importar ou criar contrato OpenAPI.</summary>
    AddContract,
    /// <summary>Definir SLO básico de availability.</summary>
    SetupSlo,
}

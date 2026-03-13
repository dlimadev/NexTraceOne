namespace NexTraceOne.DeveloperPortal.Domain.Enums;

/// <summary>Nível de subscrição para notificações de alterações em API.</summary>
public enum SubscriptionLevel
{
    /// <summary>Apenas mudanças breaking (MAJOR version).</summary>
    BreakingChangesOnly = 0,

    /// <summary>Todas as mudanças (breaking, additive, non-breaking).</summary>
    AllChanges = 1,

    /// <summary>Avisos de depreciação de endpoints ou campos.</summary>
    DeprecationNotices = 2,

    /// <summary>Alertas de segurança e vulnerabilidades.</summary>
    SecurityAdvisories = 3
}

/// <summary>Canal de entrega de notificações ao subscritor.</summary>
public enum NotificationChannel
{
    /// <summary>Notificação por e-mail.</summary>
    Email = 0,

    /// <summary>Notificação por webhook HTTP.</summary>
    Webhook = 1
}

/// <summary>Tipo de geração de código solicitada pelo desenvolvedor.</summary>
public enum GenerationType
{
    /// <summary>Cliente SDK completo para consumir a API.</summary>
    SdkClient = 0,

    /// <summary>Exemplo de integração com a API.</summary>
    IntegrationExample = 1,

    /// <summary>Testes de contrato gerados automaticamente.</summary>
    ContractTest = 2,

    /// <summary>Modelos de dados (DTOs, records) derivados do contrato.</summary>
    DataModels = 3
}

/// <summary>Tipo de evento de analytics registrado pelo portal do desenvolvedor.</summary>
public enum PortalEventType
{
    /// <summary>Pesquisa executada no catálogo.</summary>
    Search = 0,

    /// <summary>Visualização de detalhes de uma API.</summary>
    ApiView = 1,

    /// <summary>Execução de chamada no playground.</summary>
    PlaygroundExecution = 2,

    /// <summary>Geração de código a partir de contrato.</summary>
    CodeGeneration = 3,

    /// <summary>Criação de subscrição para notificações.</summary>
    SubscriptionCreated = 4,

    /// <summary>Visualização de documentação de API.</summary>
    DocumentViewed = 5,

    /// <summary>Início do fluxo de onboarding.</summary>
    OnboardingStarted = 6,

    /// <summary>Conclusão do fluxo de onboarding.</summary>
    OnboardingCompleted = 7
}

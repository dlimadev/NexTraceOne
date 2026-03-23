namespace NexTraceOne.Notifications.Application.ExternalDelivery;

/// <summary>
/// Configuração dos canais externos de notificação.
/// Cada canal é configurado de forma independente, permitindo habilitação/desabilitação por ambiente.
/// Secrets e credenciais devem ser injetados via variáveis de ambiente ou secrets manager.
/// </summary>
public sealed class NotificationChannelOptions
{
    public const string SectionName = "Notifications:Channels";

    /// <summary>Configuração do canal de email.</summary>
    public EmailChannelSettings Email { get; set; } = new();

    /// <summary>Configuração do canal Microsoft Teams.</summary>
    public TeamsChannelSettings Teams { get; set; } = new();
}

/// <summary>
/// Configuração do canal de email para notificações externas.
/// </summary>
public sealed class EmailChannelSettings
{
    /// <summary>Se o canal de email está habilitado.</summary>
    public bool Enabled { get; set; }

    /// <summary>Host do servidor SMTP.</summary>
    public string? SmtpHost { get; set; }

    /// <summary>Porta do servidor SMTP (padrão: 587).</summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>Se deve usar SSL/TLS.</summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>Utilizador para autenticação SMTP.</summary>
    public string? Username { get; set; }

    /// <summary>Senha para autenticação SMTP.</summary>
    public string? Password { get; set; }

    /// <summary>Endereço de remetente.</summary>
    public string? FromAddress { get; set; }

    /// <summary>Nome de exibição do remetente.</summary>
    public string FromName { get; set; } = "NexTraceOne";

    /// <summary>URL base do produto para deep links em emails.</summary>
    public string BaseUrl { get; set; } = "https://app.nextraceone.com";
}

/// <summary>
/// Configuração do canal Microsoft Teams para notificações externas.
/// Utiliza Incoming Webhook para enviar mensagens / Adaptive Cards.
/// </summary>
public sealed class TeamsChannelSettings
{
    /// <summary>Se o canal Teams está habilitado.</summary>
    public bool Enabled { get; set; }

    /// <summary>URL do Incoming Webhook do Microsoft Teams.</summary>
    public string? WebhookUrl { get; set; }

    /// <summary>Timeout para chamadas HTTP ao webhook (segundos).</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>URL base do produto para deep links nos cards Teams.</summary>
    public string BaseUrl { get; set; } = "https://app.nextraceone.com";
}

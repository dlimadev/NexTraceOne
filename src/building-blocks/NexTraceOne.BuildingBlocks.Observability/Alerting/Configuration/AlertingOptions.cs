namespace NexTraceOne.BuildingBlocks.Observability.Alerting.Configuration;

/// <summary>
/// Opções de configuração do sistema de alertas da plataforma.
/// Mapeia a secção "Alerting" do appsettings.
/// </summary>
public sealed class AlertingOptions
{
    /// <summary>Nome da secção de configuração.</summary>
    public const string SectionName = "Alerting";

    /// <summary>Indica se o sistema de alertas está ativo.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Configurações do canal Webhook.</summary>
    public WebhookChannelOptions Webhook { get; set; } = new();

    /// <summary>Configurações do canal Email.</summary>
    public EmailChannelOptions Email { get; set; } = new();
}

/// <summary>
/// Opções de configuração do canal Webhook para envio de alertas via HTTP POST.
/// </summary>
public sealed class WebhookChannelOptions
{
    /// <summary>Indica se o canal Webhook está ativo.</summary>
    public bool Enabled { get; set; }

    /// <summary>URL de destino do webhook.</summary>
    public string? Url { get; set; }

    /// <summary>Headers HTTP adicionais a incluir no request.</summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>Timeout em segundos para o request HTTP.</summary>
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Opções de configuração do canal Email para envio de alertas via SMTP.
/// </summary>
public sealed class EmailChannelOptions
{
    /// <summary>Indica se o canal Email está ativo.</summary>
    public bool Enabled { get; set; }

    /// <summary>Host do servidor SMTP.</summary>
    public string? SmtpHost { get; set; }

    /// <summary>Porta do servidor SMTP.</summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>Indica se deve utilizar SSL/TLS.</summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>Utilizador para autenticação SMTP.</summary>
    public string? Username { get; set; }

    /// <summary>Password para autenticação SMTP.</summary>
    public string? Password { get; set; }

    /// <summary>Endereço de email do remetente.</summary>
    public string? FromAddress { get; set; }

    /// <summary>Nome de exibição do remetente.</summary>
    public string? FromName { get; set; } = "NexTraceOne Alerts";

    /// <summary>Lista de destinatários dos alertas.</summary>
    public List<string> Recipients { get; set; } = new();
}

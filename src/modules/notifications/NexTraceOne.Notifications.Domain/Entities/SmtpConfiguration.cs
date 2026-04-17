using NexTraceOne.BuildingBlocks.Core.Attributes;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Domain.Entities;

/// <summary>
/// Configuração SMTP persistida para envio de notificações por email.
/// Complementa a DeliveryChannelConfiguration com os campos específicos do protocolo SMTP,
/// permitindo gestão da configuração de email sem redeploy.
/// Cada tenant pode ter a sua própria configuração SMTP.
/// </summary>
public sealed class SmtpConfiguration : Entity<SmtpConfigurationId>
{
    private SmtpConfiguration() { } // EF Core

    private SmtpConfiguration(
        SmtpConfigurationId id,
        Guid tenantId,
        string host,
        int port,
        bool useSsl,
        string? username,
        string? encryptedPassword,
        string fromAddress,
        string fromName,
        string? baseUrl,
        bool isEnabled)
    {
        Id = id;
        TenantId = tenantId;
        Host = host;
        Port = port;
        UseSsl = useSsl;
        Username = username;
        EncryptedPassword = encryptedPassword;
        FromAddress = fromAddress;
        FromName = fromName;
        BaseUrl = baseUrl;
        IsEnabled = isEnabled;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Tenant ao qual a configuração pertence.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Hostname ou IP do servidor SMTP.</summary>
    public string Host { get; private set; } = default!;

    /// <summary>Porta do servidor SMTP (padrão: 587 para TLS, 465 para SSL, 25 para sem TLS).</summary>
    public int Port { get; private set; }

    /// <summary>Indica se deve usar SSL/TLS na ligação SMTP.</summary>
    public bool UseSsl { get; private set; }

    /// <summary>Nome de utilizador para autenticação SMTP. Null se não for necessária autenticação.</summary>
    public string? Username { get; private set; }

    /// <summary>
    /// Senha cifrada para autenticação SMTP.
    /// Encriptada em repouso via AES-256-GCM (EncryptedStringConverter — aplicado automaticamente pelo NexTraceDbContextBase).
    /// A camada de Application passa o valor em texto claro; o EF Core aplica cifra/decifra de forma transparente.
    /// </summary>
    [EncryptedField]
    public string? EncryptedPassword { get; private set; }

    /// <summary>Endereço de email do remetente (ex.: noreply@empresa.com).</summary>
    public string FromAddress { get; private set; } = default!;

    /// <summary>Nome de exibição do remetente (ex.: "NexTraceOne Notifications").</summary>
    public string FromName { get; private set; } = default!;

    /// <summary>
    /// URL base do produto para deep links em emails.
    /// Usado para construir links de ação dentro dos templates de email.
    /// </summary>
    public string? BaseUrl { get; private set; }

    /// <summary>Indica se a configuração SMTP está ativa e deve ser usada para envio.</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>Data/hora UTC de criação da configuração.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Data/hora UTC da última atualização.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Cria uma nova configuração SMTP para o tenant.
    /// </summary>
    public static SmtpConfiguration Create(
        Guid tenantId,
        string host,
        int port,
        bool useSsl,
        string fromAddress,
        string fromName,
        string? username = null,
        string? encryptedPassword = null,
        string? baseUrl = null,
        bool isEnabled = false)
    {
        return new SmtpConfiguration(
            new SmtpConfigurationId(Guid.NewGuid()),
            tenantId,
            host,
            port,
            useSsl,
            username,
            encryptedPassword,
            fromAddress,
            fromName,
            baseUrl,
            isEnabled);
    }

    /// <summary>Atualiza os parâmetros de conexão do servidor SMTP.</summary>
    public void UpdateServer(string host, int port, bool useSsl)
    {
        Host = host;
        Port = port;
        UseSsl = useSsl;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Atualiza as credenciais de autenticação SMTP.</summary>
    public void UpdateCredentials(string? username, string? encryptedPassword)
    {
        Username = username;
        EncryptedPassword = encryptedPassword;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Atualiza os dados de remetente e URL base.</summary>
    public void UpdateSender(string fromAddress, string fromName, string? baseUrl)
    {
        FromAddress = fromAddress;
        FromName = fromName;
        BaseUrl = baseUrl;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Habilita a configuração SMTP.</summary>
    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Desabilita a configuração SMTP.</summary>
    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

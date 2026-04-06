namespace NexTraceOne.Governance.Domain.SecurityGate.Enums;

/// <summary>Categoria de achado de segurança (OWASP Top 10 e extensões).</summary>
public enum SecurityCategory
{
    /// <summary>Injection (SQL, LDAP, etc.).</summary>
    Injection = 0,

    /// <summary>Autenticação e gestão de sessão inseguras.</summary>
    BrokenAuth = 1,

    /// <summary>Exposição de dados sensíveis.</summary>
    SensitiveDataExposure = 2,

    /// <summary>XML External Entities (XXE).</summary>
    XmlExternalEntities = 3,

    /// <summary>Controlo de acesso quebrado.</summary>
    BrokenAccessControl = 4,

    /// <summary>Configuração de segurança incorreta.</summary>
    SecurityMisconfiguration = 5,

    /// <summary>Cross-Site Scripting (XSS).</summary>
    Xss = 6,

    /// <summary>Desserialização insegura.</summary>
    InsecureDeserialization = 7,

    /// <summary>Logging e monitorização insuficientes.</summary>
    InsufficientLogging = 8,

    /// <summary>Server-Side Request Forgery (SSRF).</summary>
    Ssrf = 9,

    /// <summary>Segredos hardcoded no código.</summary>
    HardcodedSecrets = 10,

    /// <summary>Uso de criptografia insegura (MD5, SHA1, DES).</summary>
    InsecureCrypto = 11,

    /// <summary>Ausência de validação de input.</summary>
    MissingInputValidation = 12,

    /// <summary>Logging de dados sensíveis (PII, tokens).</summary>
    LoggingSensitiveData = 13
}

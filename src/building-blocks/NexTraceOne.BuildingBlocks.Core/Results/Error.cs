namespace NexTraceOne.BuildingBlocks.Core.Results;

/// <summary>
/// Representa um erro de domínio ou aplicação dentro do padrão Result.
/// Contém código, mensagem e tipo de erro para mapeamento correto em HTTP.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>Argumentos dinâmicos usados para interpolação e localização da mensagem.</summary>
    public object[] MessageArgs { get; init; } = [];

    /// <summary>Mensagem final formatada com os argumentos informados.</summary>
    public string FormattedMessage => MessageArgs.Length == 0
        ? Message
        : string.Format(Message, MessageArgs);

    /// <summary>Cria um erro NotFound com suporte a argumentos de formatação.</summary>
    public static Error NotFound(string code, string msg, params object[] args)
        => new(code, msg, ErrorType.NotFound) { MessageArgs = args };

    /// <summary>Cria um erro de validação com suporte a argumentos de formatação.</summary>
    public static Error Validation(string code, string msg, params object[] args)
        => new(code, msg, ErrorType.Validation) { MessageArgs = args };

    /// <summary>Cria um erro de conflito com suporte a argumentos de formatação.</summary>
    public static Error Conflict(string code, string msg, params object[] args)
        => new(code, msg, ErrorType.Conflict) { MessageArgs = args };

    /// <summary>Cria um erro de autenticação com suporte a argumentos de formatação.</summary>
    public static Error Unauthorized(string code, string msg, params object[] args)
        => new(code, msg, ErrorType.Unauthorized) { MessageArgs = args };

    /// <summary>Cria um erro de autorização com suporte a argumentos de formatação.</summary>
    public static Error Forbidden(string code, string msg, params object[] args)
        => new(code, msg, ErrorType.Forbidden) { MessageArgs = args };

    /// <summary>Cria um erro de segurança com suporte a argumentos de formatação.</summary>
    public static Error Security(string code, string msg, params object[] args)
        => new(code, msg, ErrorType.Security) { MessageArgs = args };

    /// <summary>Cria um erro de negócio com suporte a argumentos de formatação.</summary>
    public static Error Business(string code, string msg, params object[] args)
        => new(code, msg, ErrorType.Business) { MessageArgs = args };

    /// <summary>Erro nulo — representa ausência de erro (interno).</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);
}

/// <summary>Classificação dos tipos de erro para mapeamento HTTP automático.</summary>
public enum ErrorType
{
    /// <summary>Sem erro.</summary>
    None,
    /// <summary>Recurso não encontrado → HTTP 404.</summary>
    NotFound,
    /// <summary>Validação de entrada → HTTP 422.</summary>
    Validation,
    /// <summary>Conflito de estado → HTTP 409.</summary>
    Conflict,
    /// <summary>Não autenticado → HTTP 401.</summary>
    Unauthorized,
    /// <summary>Sem permissão → HTTP 403.</summary>
    Forbidden,
    /// <summary>Erro de segurança → HTTP 500 (sem detalhes).</summary>
    Security,
    /// <summary>Regra de negócio violada → HTTP 422.</summary>
    Business
}

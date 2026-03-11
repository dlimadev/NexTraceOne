namespace NexTraceOne.BuildingBlocks.Domain.Results;

/// <summary>
/// Representa um erro de domínio ou aplicação dentro do padrão Result.
/// Contém código, mensagem e tipo de erro para mapeamento correto em HTTP.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    // Factories para os tipos de erro mais comuns
    public static Error NotFound(string code, string msg)     => new(code, msg, ErrorType.NotFound);
    public static Error Validation(string code, string msg)   => new(code, msg, ErrorType.Validation);
    public static Error Conflict(string code, string msg)     => new(code, msg, ErrorType.Conflict);
    public static Error Unauthorized(string code, string msg) => new(code, msg, ErrorType.Unauthorized);
    public static Error Forbidden(string code, string msg)    => new(code, msg, ErrorType.Forbidden);
    public static Error Security(string code, string msg)     => new(code, msg, ErrorType.Security);
    public static Error Business(string code, string msg)     => new(code, msg, ErrorType.Business);

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

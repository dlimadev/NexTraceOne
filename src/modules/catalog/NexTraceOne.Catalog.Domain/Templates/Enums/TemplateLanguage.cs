namespace NexTraceOne.Catalog.Domain.Templates.Enums;

/// <summary>
/// Linguagem ou stack tecnológica alvo do template de serviço.
/// Determina o scaffolding gerado (estrutura de pastas, dependências, contratos base).
/// </summary>
public enum TemplateLanguage
{
    /// <summary>Serviço .NET (ASP.NET Core, Worker Service, etc.).</summary>
    DotNet = 1,

    /// <summary>Serviço Node.js (Express, Fastify, NestJS).</summary>
    NodeJs = 2,

    /// <summary>Serviço Java (Spring Boot, Quarkus).</summary>
    Java = 3,

    /// <summary>Serviço Go.</summary>
    Go = 4,

    /// <summary>Serviço Python (FastAPI, Django).</summary>
    Python = 5,

    /// <summary>Template neutro — sem geração de código, apenas governança.</summary>
    Agnostic = 6
}

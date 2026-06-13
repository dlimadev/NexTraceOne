using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Generation;

/// <summary>
/// Gerador determinístico de scaffold de aplicação por <see cref="ServiceType"/> (sem IA).
///
/// Produz o esqueleto adequado ao modelo do serviço — consumidor Kafka, produtor Kafka,
/// serviço SOAP, gRPC, worker/background, API REST (quando não há contrato OpenAPI para
/// gerar classes), ou um esqueleto genérico. Para APIs REST com contrato OpenAPI, a feature
/// usa antes o <see cref="DotNetCleanArchitectureCodeGenerator"/>.
///
/// Os templates usam tokens (__NS__, __NAME__, __SERVICE__) substituídos de forma simples,
/// evitando problemas de chavetas em C#. Lógica pura — totalmente testável.
/// </summary>
public static class ServiceScaffoldGenerator
{
    /// <summary>Gera o esqueleto da aplicação para o serviço/tipo indicado.</summary>
    public static IReadOnlyList<GeneratedCodeFile> Generate(string serviceName, ServiceType serviceType)
    {
        var ns = DotNetCleanArchitectureCodeGenerator.ToPascalCase(serviceName);

        return serviceType switch
        {
            ServiceType.KafkaConsumer => KafkaConsumer(serviceName, ns),
            ServiceType.KafkaProducer => KafkaProducer(serviceName, ns),
            ServiceType.SoapService => SoapService(serviceName, ns),
            ServiceType.GrpcService => GrpcService(serviceName, ns),
            ServiceType.BackgroundService
                or ServiceType.ScheduledProcess
                or ServiceType.BatchJob => Worker(serviceName, ns, serviceType),
            ServiceType.RestApi
                or ServiceType.GraphqlApi
                or ServiceType.Gateway => RestApiSkeleton(serviceName, ns),
            _ => Generic(serviceName, ns, serviceType)
        };
    }

    // ── Kafka consumer ───────────────────────────────────────────────────

    private static IReadOnlyList<GeneratedCodeFile> KafkaConsumer(string serviceName, string ns)
    {
        const string template = @"using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace __NS__.Consumers;

/// <summary>Consumidor Kafka gerado para o serviço '__SERVICE__'.</summary>
public sealed class __NAME__Consumer : BackgroundService
{
    private readonly ILogger<__NAME__Consumer> _logger;

    public __NAME__Consumer(ILogger<__NAME__Consumer> logger) => _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // TODO: subscrever ao tópico e processar as mensagens conforme o contrato AsyncAPI do serviço.
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
        }
    }
}
";
        return
        [
            new GeneratedCodeFile($"src/{serviceName}/Consumers/{ns}Consumer.cs", Fill(template, ns, serviceName)),
            Readme(serviceName, "Kafka consumer", "Consome mensagens de um tópico Kafka conforme o contrato AsyncAPI.")
        ];
    }

    // ── Kafka producer ───────────────────────────────────────────────────

    private static IReadOnlyList<GeneratedCodeFile> KafkaProducer(string serviceName, string ns)
    {
        const string template = @"using System.Threading;
using System.Threading.Tasks;

namespace __NS__.Producers;

/// <summary>Produtor Kafka gerado para o serviço '__SERVICE__'.</summary>
public sealed class __NAME__Producer
{
    /// <summary>Publica uma mensagem no tópico conforme o contrato AsyncAPI do serviço.</summary>
    public Task PublishAsync(object message, CancellationToken cancellationToken = default)
    {
        // TODO: serializar e publicar a mensagem no tópico Kafka.
        return Task.CompletedTask;
    }
}
";
        return
        [
            new GeneratedCodeFile($"src/{serviceName}/Producers/{ns}Producer.cs", Fill(template, ns, serviceName)),
            Readme(serviceName, "Kafka producer", "Publica mensagens num tópico Kafka conforme o contrato AsyncAPI.")
        ];
    }

    // ── SOAP ───────────────────────────────────────────────────────────────

    private static IReadOnlyList<GeneratedCodeFile> SoapService(string serviceName, string ns)
    {
        const string template = @"using System.Threading.Tasks;

namespace __NS__.Services;

/// <summary>Contrato do serviço SOAP '__SERVICE__' (derivar as operações do WSDL).</summary>
public interface I__NAME__Service
{
    // TODO: declarar as operações definidas no WSDL do serviço.
}

/// <summary>Implementação gerada do serviço SOAP '__SERVICE__'.</summary>
public sealed class __NAME__Service : I__NAME__Service
{
    // TODO: implementar as operações do contrato WSDL.
}
";
        return
        [
            new GeneratedCodeFile($"src/{serviceName}/Services/{ns}Service.cs", Fill(template, ns, serviceName)),
            Readme(serviceName, "SOAP service", "Serviço SOAP cujas operações derivam do contrato WSDL.")
        ];
    }

    // ── gRPC ───────────────────────────────────────────────────────────────

    private static IReadOnlyList<GeneratedCodeFile> GrpcService(string serviceName, string ns)
    {
        const string template = @"namespace __NS__.Services;

/// <summary>
/// Serviço gRPC gerado para '__SERVICE__'. Adicionar o ficheiro .proto do contrato Protobuf
/// e gerar a base a partir dele; depois herdar dela nesta classe.
/// </summary>
public sealed class __NAME__GrpcService
{
    // TODO: herdar de __NAME__.__NAME__Base (gerado a partir do .proto) e implementar os métodos.
}
";
        return
        [
            new GeneratedCodeFile($"src/{serviceName}/Services/{ns}GrpcService.cs", Fill(template, ns, serviceName)),
            Readme(serviceName, "gRPC service", "Serviço gRPC cujas mensagens/métodos derivam do contrato Protobuf (.proto).")
        ];
    }

    // ── Worker / background / scheduled / batch ──────────────────────────────

    private static IReadOnlyList<GeneratedCodeFile> Worker(string serviceName, string ns, ServiceType serviceType)
    {
        const string template = @"using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace __NS__;

/// <summary>Worker gerado para o serviço '__SERVICE__'.</summary>
public sealed class __NAME__Worker : BackgroundService
{
    private readonly ILogger<__NAME__Worker> _logger;

    public __NAME__Worker(ILogger<__NAME__Worker> logger) => _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // TODO: implementar a lógica de execução do serviço.
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
        }
    }
}
";
        return
        [
            new GeneratedCodeFile($"src/{serviceName}/{ns}Worker.cs", Fill(template, ns, serviceName)),
            Readme(serviceName, serviceType.ToString(), "Processo de fundo/agendado gerado a partir do modelo do serviço.")
        ];
    }

    // ── REST sem contrato OpenAPI ────────────────────────────────────────────

    private static IReadOnlyList<GeneratedCodeFile> RestApiSkeleton(string serviceName, string ns)
    {
        const string template = @"using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// TODO: registar os endpoints. Quando existir um contrato OpenAPI no NexTraceOne,
// as classes (DTOs + endpoints) são geradas automaticamente a partir dele.
app.MapGet(""/health"", () => Results.Ok(new { service = ""__SERVICE__"", status = ""ok"" }));

app.Run();
";
        return
        [
            new GeneratedCodeFile($"src/{serviceName}.Api/Program.cs", Fill(template, ns, serviceName)),
            Readme(serviceName, "REST API", "API REST. Com um contrato OpenAPI associado, os DTOs e endpoints são gerados automaticamente.")
        ];
    }

    // ── Genérico ─────────────────────────────────────────────────────────────

    private static IReadOnlyList<GeneratedCodeFile> Generic(string serviceName, string ns, ServiceType serviceType)
        => [Readme(serviceName, serviceType.ToString(),
            "Esqueleto mínimo. O modelo deste serviço ainda não tem um gerador dedicado.")];

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static string Fill(string template, string ns, string serviceName)
        => template
            .Replace("__NS__", ns, StringComparison.Ordinal)
            .Replace("__NAME__", ns, StringComparison.Ordinal)
            .Replace("__SERVICE__", serviceName, StringComparison.Ordinal);

    private static GeneratedCodeFile Readme(string serviceName, string model, string description)
    {
        var content = $"# {serviceName}\n\n> Modelo: **{model}**\n\n{description}\n\n"
            + "_Gerado pelo NexTraceOne (determinístico, sem IA)._\n";
        return new GeneratedCodeFile($"src/{serviceName}/README.md", content);
    }
}

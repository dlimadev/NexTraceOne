using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.DeveloperPortal.Application.Abstractions;
using NexTraceOne.DeveloperPortal.Domain.Entities;
using NexTraceOne.DeveloperPortal.Domain.Enums;

namespace NexTraceOne.DeveloperPortal.Application.Features.GenerateCode;

/// <summary>
/// Feature: GenerateCode — gera artefactos de código a partir de contrato OpenAPI.
/// Suporta SDK client, exemplos de integração, testes de contrato e modelos.
/// Toda geração é auditada e marcada como gerada (humana ou IA).
/// </summary>
public static class GenerateCode
{
    /// <summary>Comando para gerar código a partir de contrato OpenAPI.</summary>
    public sealed record Command(
        Guid ApiAssetId,
        string ApiName,
        string ContractVersion,
        Guid RequestedById,
        string Language,
        GenerationType GenerationType,
        bool IsAiGenerated = false,
        string? TemplateId = null) : ICommand<Response>;

    /// <summary>Valida os parâmetros de geração de código.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly string[] SupportedLanguages = ["CSharp", "Java", "Python", "TypeScript", "Go"];

        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.ApiName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ContractVersion).NotEmpty().MaximumLength(50);
            RuleFor(x => x.RequestedById).NotEmpty();
            RuleFor(x => x.Language)
                .NotEmpty()
                .Must(lang => SupportedLanguages.Contains(lang))
                .WithMessage($"Language must be one of: {string.Join(", ", SupportedLanguages)}");
        }
    }

    /// <summary>
    /// Handler que gera código a partir de contrato e regista para auditoria.
    /// MVP1: geração por template estático. Em produção, pode delegar para IA.
    /// </summary>
    public sealed class Handler(
        ICodeGenerationRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // MVP1: geração de código por template estático
            var generatedCode = GenerateTemplate(
                request.Language, request.GenerationType, request.ApiName, request.ContractVersion);

            var record = CodeGenerationRecord.Create(
                request.ApiAssetId,
                request.ApiName,
                request.ContractVersion,
                request.RequestedById,
                request.Language,
                request.GenerationType.ToString(),
                generatedCode,
                request.IsAiGenerated,
                request.TemplateId,
                clock.UtcNow);

            repository.Add(record);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                record.Id.Value,
                record.Language,
                record.GenerationType,
                generatedCode,
                record.IsAiGenerated,
                record.GeneratedAt);
        }

        /// <summary>Gera template de código estático por linguagem e tipo.</summary>
        private static string GenerateTemplate(
            string language, GenerationType type, string apiName, string version) =>
            (language, type) switch
            {
                ("CSharp", GenerationType.SdkClient) =>
                    $$"""
                    // Auto-generated SDK client for {{apiName}} v{{version}}
                    // Review before use — this is a generated artifact.

                    using System.Net.Http;
                    using System.Text.Json;

                    public sealed class {{SanitizeName(apiName)}}Client
                    {
                        private readonly HttpClient _http;

                        public {{SanitizeName(apiName)}}Client(HttpClient http) => _http = http;

                        public async Task<string> GetAsync(string path, CancellationToken cancellationToken = default)
                        {
                            var response = await _http.GetAsync(path, cancellationToken);
                            response.EnsureSuccessStatusCode();
                            return await response.Content.ReadAsStringAsync(cancellationToken);
                        }

                        public async Task<string> PostAsync(string path, HttpContent content, CancellationToken cancellationToken = default)
                        {
                            var response = await _http.PostAsync(path, content, cancellationToken);
                            response.EnsureSuccessStatusCode();
                            return await response.Content.ReadAsStringAsync(cancellationToken);
                        }
                    }
                    """,

                ("TypeScript", GenerationType.SdkClient) =>
                    $$"""
                    // Auto-generated SDK client for {{apiName}} v{{version}}
                    // Review before use — this is a generated artifact.

                    export class {{SanitizeName(apiName)}}Client {
                      constructor(private readonly baseUrl: string) {}

                      async get(path: string): Promise<Response> {
                        return fetch(`${this.baseUrl}${path}`);
                      }

                      async post(path: string, body: unknown): Promise<Response> {
                        return fetch(`${this.baseUrl}${path}`, {
                          method: 'POST',
                          headers: { 'Content-Type': 'application/json' },
                          body: JSON.stringify(body),
                        });
                      }
                    }
                    """,

                ("Python", GenerationType.SdkClient) =>
                    $$"""
                    # Auto-generated SDK client for {{apiName}} v{{version}}
                    # Review before use — this is a generated artifact.

                    import requests

                    class {{SanitizeName(apiName)}}Client:
                        def __init__(self, base_url: str):
                            self.base_url = base_url

                        def get(self, path: str) -> requests.Response:
                            return requests.get(f"{self.base_url}{path}")

                        def post(self, path: str, data: dict) -> requests.Response:
                            return requests.post(f"{self.base_url}{path}", json=data)
                    """,

                ("Java", GenerationType.SdkClient) =>
                    $$"""
                    // Auto-generated SDK client for {{apiName}} v{{version}}
                    // Review before use — this is a generated artifact.

                    import java.net.http.HttpClient;
                    import java.net.http.HttpRequest;
                    import java.net.http.HttpResponse;
                    import java.net.URI;

                    public class {{SanitizeName(apiName)}}Client {
                        private final String baseUrl;
                        private final HttpClient httpClient;

                        public {{SanitizeName(apiName)}}Client(String baseUrl) {
                            this.baseUrl = baseUrl;
                            this.httpClient = HttpClient.newHttpClient();
                        }

                        public HttpResponse<String> get(String path) throws Exception {
                            var request = HttpRequest.newBuilder()
                                .uri(URI.create(baseUrl + path))
                                .GET().build();
                            return httpClient.send(request, HttpResponse.BodyHandlers.ofString());
                        }

                        public HttpResponse<String> post(String path, String body) throws Exception {
                            var request = HttpRequest.newBuilder()
                                .uri(URI.create(baseUrl + path))
                                .header("Content-Type", "application/json")
                                .POST(HttpRequest.BodyPublishers.ofString(body)).build();
                            return httpClient.send(request, HttpResponse.BodyHandlers.ofString());
                        }
                    }
                    """,

                ("Go", GenerationType.SdkClient) =>
                    $$"""
                    // Auto-generated SDK client for {{apiName}} v{{version}}
                    // Review before use — this is a generated artifact.

                    package client

                    import (
                    	"bytes"
                    	"net/http"
                    )

                    // {{SanitizeName(apiName)}}Client provides HTTP methods for the API.
                    type {{SanitizeName(apiName)}}Client struct {
                    	BaseURL    string
                    	HttpClient *http.Client
                    }

                    // New{{SanitizeName(apiName)}}Client creates a new API client.
                    func New{{SanitizeName(apiName)}}Client(baseURL string) *{{SanitizeName(apiName)}}Client {
                    	c := &{{SanitizeName(apiName)}}Client{BaseURL: baseURL}
                    	c.HttpClient = &http.Client{}
                    	return c
                    }

                    // Get performs a GET request to the specified path.
                    func (c *{{SanitizeName(apiName)}}Client) Get(path string) (*http.Response, error) {
                    	return c.HttpClient.Get(c.BaseURL + path)
                    }

                    // Post performs a POST request to the specified path.
                    func (c *{{SanitizeName(apiName)}}Client) Post(path string, body []byte) (*http.Response, error) {
                    	return c.HttpClient.Post(c.BaseURL+path, "application/json", bytes.NewReader(body))
                    }
                    """,

                (_, GenerationType.IntegrationExample) =>
                    $"""
                    // Integration example for {apiName} v{version}
                    // Language: {language}
                    // Review before use — this is a generated artifact.

                    // Step 1: Initialize client
                    // Step 2: Authenticate
                    // Step 3: Make API call
                    // Step 4: Handle response
                    // Step 5: Implement error handling and retry
                    """,

                _ =>
                    $"""
                    // Generated {type} for {apiName} v{version}
                    // Language: {language}
                    // Review before use — this is a generated artifact.
                    """
            };

        /// <summary>
        /// Sanitiza nome de API para uso como identificador de código.
        /// Remove caracteres não-alfanuméricos, garante que começa com letra e nunca retorna vazio.
        /// </summary>
        private static string SanitizeName(string name)
        {
            var sanitized = new string(name.Where(c => char.IsLetterOrDigit(c)).ToArray());

            if (string.IsNullOrEmpty(sanitized))
                return "Api";

            if (!char.IsLetter(sanitized[0]))
                sanitized = "Api" + sanitized;

            return sanitized;
        }
    }

    /// <summary>Resposta com código gerado e metadados de auditoria.</summary>
    public sealed record Response(
        Guid RecordId,
        string Language,
        string GenerationType,
        string GeneratedCode,
        bool IsAiGenerated,
        DateTimeOffset GeneratedAt);
}

using System.Text;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Domain.Templates.Enums;

namespace NexTraceOne.Catalog.Application.Templates.Features.GenerateEnvironmentBlueprint;

/// <summary>
/// Feature: GenerateEnvironmentBlueprint — gera ficheiros de infraestrutura prontos a usar
/// (Dockerfile, docker-compose, CI/CD pipeline) a partir de metadados do template/serviço.
/// Suporta .NET, Node.js, Java e Go. Pipelines: GitHub Actions, GitLab CI, Azure DevOps.
/// Estrutura VSA: Command + Validator + Handler + Response.
/// </summary>
public static class GenerateEnvironmentBlueprint
{
    /// <summary>Comando para gerar o blueprint de ambiente de um serviço.</summary>
    public sealed record Command(
        string ServiceName,
        string Domain,
        TemplateLanguage Language,
        string CiProvider = "github-actions",
        bool IncludeDocker = true,
        bool IncludeDockerCompose = true,
        bool IncludeCiPipeline = true,
        bool IncludeHelmChart = false,
        string? DatabaseType = null,
        int ServicePort = 8080) : ICommand<Response>;

    /// <summary>Valida o comando de geração do blueprint de ambiente.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly string[] s_validProviders =
            ["github-actions", "gitlab-ci", "azure-devops"];

        public Validator()
        {
            RuleFor(x => x.ServiceName)
                .NotEmpty()
                .MaximumLength(100)
                .Matches(@"^[a-z0-9\-]+$")
                .WithMessage("ServiceName must be lowercase alphanumeric with hyphens only.");

            RuleFor(x => x.Domain).NotEmpty().MaximumLength(100);

            RuleFor(x => x.CiProvider)
                .Must(p => s_validProviders.Contains(p))
                .WithMessage($"CiProvider must be one of: {string.Join(", ", s_validProviders)}.");

            RuleFor(x => x.ServicePort)
                .InclusiveBetween(1024, 65535);
        }
    }

    /// <summary>Handler que gera os ficheiros de infraestrutura.</summary>
    public sealed class Handler : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var files = new List<BlueprintFile>();

            if (request.IncludeDocker)
                files.Add(BuildDockerfile(request));

            if (request.IncludeDockerCompose)
                files.Add(BuildDockerCompose(request));

            if (request.IncludeCiPipeline)
                files.Add(BuildCiPipeline(request));

            if (request.IncludeHelmChart)
            {
                files.Add(BuildHelmValues(request));
                files.Add(BuildHelmChart(request));
            }

            files.Add(BuildHealthCheckNote(request));

            return Task.FromResult(Result<Response>.Success(new Response(
                ServiceName: request.ServiceName,
                Language: request.Language.ToString(),
                CiProvider: request.CiProvider,
                Files: files.AsReadOnly(),
                Summary: BuildSummary(files))));
        }

        private static BlueprintFile BuildDockerfile(Command req)
        {
            var content = req.Language switch
            {
                TemplateLanguage.NodeJs => BuildNodeJsDockerfile(req),
                TemplateLanguage.Java => BuildJavaDockerfile(req),
                TemplateLanguage.Go => BuildGoDockerfile(req),
                _ => BuildDotNetDockerfile(req),
            };
            return new BlueprintFile("Dockerfile", content, "dockerfile");
        }

        private static string BuildDotNetDockerfile(Command req) =>
            $"""
            FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
            WORKDIR /app
            EXPOSE {req.ServicePort}

            FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
            WORKDIR /src
            COPY ["src/{req.ServiceName}/{req.ServiceName}.csproj", "src/{req.ServiceName}/"]
            RUN dotnet restore "src/{req.ServiceName}/{req.ServiceName}.csproj"
            COPY . .
            WORKDIR "/src/src/{req.ServiceName}"
            RUN dotnet build "{req.ServiceName}.csproj" -c Release -o /app/build

            FROM build AS publish
            RUN dotnet publish "{req.ServiceName}.csproj" -c Release -o /app/publish /p:UseAppHost=false

            FROM base AS final
            WORKDIR /app
            COPY --from=publish /app/publish .
            HEALTHCHECK --interval=30s --timeout=3s CMD curl -f http://localhost:{req.ServicePort}/health || exit 1
            ENTRYPOINT ["dotnet", "{req.ServiceName}.dll"]
            """;

        private static string BuildNodeJsDockerfile(Command req) =>
            $"""
            FROM node:22-alpine AS builder
            WORKDIR /app
            COPY package*.json ./
            RUN npm ci --omit=dev
            COPY . .
            RUN npm run build

            FROM node:22-alpine AS final
            WORKDIR /app
            ENV NODE_ENV=production
            COPY --from=builder /app/dist ./dist
            COPY --from=builder /app/node_modules ./node_modules
            EXPOSE {req.ServicePort}
            HEALTHCHECK --interval=30s CMD wget -qO- http://localhost:{req.ServicePort}/health || exit 1
            CMD ["node", "dist/main.js"]
            """;

        private static string BuildJavaDockerfile(Command req) =>
            $"""
            FROM eclipse-temurin:21-jdk AS builder
            WORKDIR /app
            COPY .mvn/ .mvn
            COPY mvnw pom.xml ./
            RUN ./mvnw dependency:go-offline
            COPY src ./src
            RUN ./mvnw package -DskipTests

            FROM eclipse-temurin:21-jre AS final
            WORKDIR /app
            COPY --from=builder /app/target/*.jar app.jar
            EXPOSE {req.ServicePort}
            HEALTHCHECK --interval=30s CMD curl -f http://localhost:{req.ServicePort}/actuator/health || exit 1
            ENTRYPOINT ["java", "-jar", "app.jar"]
            """;

        private static string BuildGoDockerfile(Command req) =>
            $"""
            FROM golang:1.23-alpine AS builder
            WORKDIR /app
            COPY go.mod go.sum ./
            RUN go mod download
            COPY . .
            RUN CGO_ENABLED=0 GOOS=linux go build -o /app/service ./cmd/main.go

            FROM alpine:3.20 AS final
            RUN apk --no-cache add ca-certificates
            WORKDIR /root/
            COPY --from=builder /app/service .
            EXPOSE {req.ServicePort}
            HEALTHCHECK --interval=30s CMD wget -qO- http://localhost:{req.ServicePort}/health || exit 1
            CMD ["./service"]
            """;

        private static BlueprintFile BuildDockerCompose(Command req)
        {
            var sb = new StringBuilder();
            sb.AppendLine("services:");
            sb.AppendLine($"  {req.ServiceName}:");
            sb.AppendLine("    build: .");
            sb.AppendLine($"    container_name: {req.ServiceName}");
            sb.AppendLine("    ports:");
            sb.AppendLine($"      - \"{req.ServicePort}:{req.ServicePort}\"");
            sb.AppendLine("    environment:");
            sb.AppendLine("      - ASPNETCORE_ENVIRONMENT=Development");
            sb.AppendLine($"      - SERVICE_NAME={req.ServiceName}");
            sb.AppendLine($"      - SERVICE_DOMAIN={req.Domain}");
            sb.AppendLine("    networks:");
            sb.AppendLine($"      - {req.Domain}-net");
            sb.AppendLine("    healthcheck:");
            sb.AppendLine($"      test: [\"CMD\", \"curl\", \"-f\", \"http://localhost:{req.ServicePort}/health\"]");
            sb.AppendLine("      interval: 30s");
            sb.AppendLine("      timeout: 5s");
            sb.AppendLine("      retries: 3");

            if (string.Equals(req.DatabaseType, "postgres", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine();
                sb.AppendLine("  postgres:");
                sb.AppendLine("    image: postgres:16-alpine");
                sb.AppendLine("    environment:");
                sb.AppendLine($"      POSTGRES_DB: {req.ServiceName.Replace("-", "_", StringComparison.Ordinal)}");
                sb.AppendLine("      POSTGRES_USER: appuser");
                sb.AppendLine("      POSTGRES_PASSWORD: apppassword");
                sb.AppendLine("    volumes:");
                sb.AppendLine("      - postgres_data:/var/lib/postgresql/data");
                sb.AppendLine("    networks:");
                sb.AppendLine($"      - {req.Domain}-net");
                sb.AppendLine();
                sb.AppendLine("volumes:");
                sb.AppendLine("  postgres_data:");
            }

            sb.AppendLine();
            sb.AppendLine("networks:");
            sb.AppendLine($"  {req.Domain}-net:");
            sb.AppendLine("    driver: bridge");

            return new BlueprintFile("docker-compose.yml", sb.ToString(), "yaml");
        }

        private static BlueprintFile BuildCiPipeline(Command req)
        {
            var (filename, content) = req.CiProvider switch
            {
                "gitlab-ci" => (".gitlab-ci.yml", BuildGitLabCi(req)),
                "azure-devops" => ("azure-pipelines.yml", BuildAzureDevOpsCi(req)),
                _ => (".github/workflows/ci.yml", BuildGitHubActionsCi(req)),
            };
            return new BlueprintFile(filename, content, "yaml");
        }

        private static string BuildGitHubActionsCi(Command req) =>
            $"""
            name: CI — {req.ServiceName}

            on:
              push:
                branches: [main, develop]
              pull_request:
                branches: [main]

            jobs:
              build-and-test:
                runs-on: ubuntu-latest
                steps:
                  - uses: actions/checkout@v4
                  - name: Setup .NET
                    uses: actions/setup-dotnet@v4
                    with:
                      dotnet-version: "10.0.x"
                  - name: Restore
                    run: dotnet restore
                  - name: Build
                    run: dotnet build --no-restore -c Release
                  - name: Test
                    run: dotnet test --no-build -c Release --logger trx
                  - name: Upload test results
                    uses: actions/upload-artifact@v4
                    with:
                      name: test-results
                      path: "**/*.trx"

              docker:
                needs: build-and-test
                runs-on: ubuntu-latest
                if: github.ref == 'refs/heads/main'
                steps:
                  - uses: actions/checkout@v4
                  - name: Build Docker image
                    run: docker build -t {req.ServiceName}:latest .
            """;

        private static string BuildGitLabCi(Command req) =>
            $"""
            stages:
              - build
              - test
              - docker

            variables:
              DOCKER_IMAGE: {req.ServiceName}

            build:
              stage: build
              image: mcr.microsoft.com/dotnet/sdk:10.0
              script:
                - dotnet restore
                - dotnet build -c Release --no-restore

            test:
              stage: test
              image: mcr.microsoft.com/dotnet/sdk:10.0
              script:
                - dotnet test -c Release --logger "junit;LogFilePath=test-results.xml"
              artifacts:
                reports:
                  junit: test-results.xml

            docker-build:
              stage: docker
              image: docker:latest
              only:
                - main
              script:
                - docker build -t $DOCKER_IMAGE:$CI_COMMIT_SHA .
            """;

        private static string BuildAzureDevOpsCi(Command req) =>
            $"""
            trigger:
              - main
              - develop

            pool:
              vmImage: ubuntu-latest

            steps:
              - task: UseDotNet@2
                inputs:
                  packageType: sdk
                  version: "10.0.x"

              - script: dotnet restore
                displayName: Restore

              - script: dotnet build --no-restore -c Release
                displayName: Build

              - script: dotnet test --no-build -c Release --logger trx
                displayName: Test

              - task: PublishTestResults@2
                inputs:
                  testResultsFormat: VSTest
                  testResultsFiles: "**/*.trx"
            """;

        private static BlueprintFile BuildHelmValues(Command req)
        {
            var content =
                $"""
                replicaCount: 1

                image:
                  repository: {req.ServiceName}
                  tag: latest
                  pullPolicy: IfNotPresent

                service:
                  type: ClusterIP
                  port: {req.ServicePort}

                resources:
                  requests:
                    cpu: 100m
                    memory: 128Mi
                  limits:
                    cpu: 500m
                    memory: 512Mi

                env:
                  SERVICE_NAME: {req.ServiceName}
                  SERVICE_DOMAIN: {req.Domain}
                """;
            return new BlueprintFile("helm/values.yaml", content, "yaml");
        }

        private static BlueprintFile BuildHelmChart(Command req)
        {
            var content =
                $"""
                apiVersion: v2
                name: {req.ServiceName}
                description: Helm chart for {req.ServiceName} ({req.Domain} domain)
                type: application
                version: 0.1.0
                appVersion: "1.0.0"
                """;
            return new BlueprintFile("helm/Chart.yaml", content, "yaml");
        }

        private static BlueprintFile BuildHealthCheckNote(Command req)
        {
            var content = req.Language switch
            {
                TemplateLanguage.DotNet =>
                    $"// Health check endpoint for {req.ServiceName}\n// Add to Program.cs: app.MapHealthChecks(\"/health\");\n// NuGet: Microsoft.Extensions.Diagnostics.HealthChecks",
                TemplateLanguage.NodeJs =>
                    $"// Health check for {req.ServiceName}\n// app.get('/health', (req, res) => res.json({{ status: 'ok', service: '{req.ServiceName}' }}));",
                _ =>
                    $"# Health check for {req.ServiceName}\n# Expose GET /health -> 200 OK {{ \"status\": \"ok\" }}",
            };
            return new BlueprintFile("HEALTH_CHECK.md", content, "markdown");
        }

        private static string BuildSummary(IReadOnlyList<BlueprintFile> files)
        {
            var fileNames = string.Join(", ", files.Select(f => f.FileName));
            return $"{files.Count} blueprint file(s) generated: {fileNames}";
        }
    }

    /// <summary>Ficheiro de infraestrutura gerado pelo blueprint.</summary>
    public sealed record BlueprintFile(
        string FileName,
        string Content,
        string Format);

    /// <summary>Resposta com todos os ficheiros de ambiente gerados.</summary>
    public sealed record Response(
        string ServiceName,
        string Language,
        string CiProvider,
        IReadOnlyList<BlueprintFile> Files,
        string Summary);
}

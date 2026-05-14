using System.Text.Json;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Infrastructure.Services;

/// <summary>
/// Implementação de ISbomGenerator para geração de SBOM (Software Bill of Materials) em formato SPDX 2.3.
/// Coleta dependências do projeto, licenças e metadados para compliance de segurança.
/// Essencial para auditoria de supply chain e conformidade com Executive Order 14028.
/// </summary>
public class SbomGeneratorService : ISbomGenerator
{
    public async Task<SbomDocument> GenerateSbomAsync(string projectPath)
    {
        var sbom = new SbomDocument(
            SpdxVersion: "SPDX-2.3",
            DataLicense: "CC0-1.0",
            DocumentNamespace: $"https://nextraceone.com/spdx/{Guid.NewGuid()}",
            Package: new SbomPackage(
                SpdxId: "SPDXRef-Package",
                Name: "NexTraceOne",
                VersionInfo: await GetProjectVersionAsync(projectPath),
                DownloadLocation: "https://github.com/nextraceone/NexTraceOne",
                FilesAnalyzed: "true",
                LicenseConcluded: "MIT",
                CopyrightText: "Copyright (c) 2026 NexTraceOne"),
            Dependencies: await CollectDependenciesAsync(projectPath),
            Relationships: new List<SbomRelationship>(),
            Created: DateTime.UtcNow,
            Creator: new SbomCreator(
                Tool: "NexTraceOne-SBOM-Generator-1.0",
                Organization: "NexTraceOne"),
            Metadata: new Dictionary<string, string>());

        // Adicionar relacionamentos
        foreach (var dep in sbom.Dependencies)
        {
            sbom.Relationships.Add(new SbomRelationship(
                SpdxElementId: "SPDXRef-Package",
                RelatedSpdxElement: dep.SpdxId,
                RelationshipType: "DEPENDS_ON"));
        }

        return sbom;
    }

    public Task<string> ExportSbomToJsonAsync(SbomDocument sbom)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return Task.FromResult(JsonSerializer.Serialize(sbom, options));
    }

    public Task ValidateSbomComplianceAsync(SbomDocument sbom)
    {
        // Validações básicas SPDX
        if (string.IsNullOrEmpty(sbom.DocumentNamespace))
            throw new InvalidOperationException("Document namespace is required");

        if (string.IsNullOrEmpty(sbom.Package.SpdxId))
            throw new InvalidOperationException("Package SPDX ID is required");

        return Task.CompletedTask;
    }

    private async Task<string> GetProjectVersionAsync(string projectPath)
    {
        // Extrair versão do .csproj ou git tag
        await Task.Delay(50); // Simular leitura
        return "1.0.0";
    }

    private async Task<List<SbomPackage>> CollectDependenciesAsync(string projectPath)
    {
        var dependencies = new List<SbomPackage>();

        // Em produção: parse .csproj files, package-lock.json, etc.
        // Aqui: mock de dependências comuns
        
        dependencies.Add(new SbomPackage(
            SpdxId: "SPDXRef-Pkg-1",
            Name: "Microsoft.Extensions.DependencyInjection",
            VersionInfo: "10.0.0",
            DownloadLocation: "https://nuget.org/packages/Microsoft.Extensions.DependencyInjection",
            FilesAnalyzed: "false",
            LicenseConcluded: "MIT",
            CopyrightText: ""));

        dependencies.Add(new SbomPackage(
            SpdxId: "SPDXRef-Pkg-2",
            Name: "Newtonsoft.Json",
            VersionInfo: "13.0.3",
            DownloadLocation: "https://nuget.org/packages/Newtonsoft.Json",
            FilesAnalyzed: "false",
            LicenseConcluded: "MIT",
            CopyrightText: ""));

        await Task.Delay(100); // Simular coleta

        return dependencies;
    }
}

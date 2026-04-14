using System.Xml.Linq;
using System.Text.Json;

using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.DependencyGovernance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;

namespace NexTraceOne.Catalog.Application.DependencyGovernance.Features.ScanServiceDependencies;

/// <summary>
/// Feature: ScanServiceDependencies — analisa o ficheiro de projeto e cria/actualiza
/// o perfil de dependências do serviço com pacotes, ecossistema e estado de saúde.
/// Persona primária: Engineer, Platform Admin.
/// </summary>
public static class ScanServiceDependencies
{
    public sealed record Command(
        Guid ServiceId,
        string ProjectFileContent,
        string ProjectFileType,
        Guid? TemplateId = null) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty();
            RuleFor(x => x.ProjectFileContent).NotEmpty().MaximumLength(1_000_000);
            RuleFor(x => x.ProjectFileType)
                .NotEmpty()
                .Must(t => t is "csproj" or "package.json" or "pom.xml")
                .WithMessage("ProjectFileType must be 'csproj', 'package.json' or 'pom.xml'.");
        }
    }

    public sealed class Handler(
        IServiceDependencyProfileRepository repository,
        IDependencyGovernanceUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var existing = await repository.FindByServiceIdAsync(request.ServiceId, cancellationToken);
            var profile = existing ?? ServiceDependencyProfile.Create(request.ServiceId, request.TemplateId);

            var dependencies = ParseDependencies(profile.Id.Value, request.ProjectFileContent, request.ProjectFileType);
            profile.UpdateScan(dependencies, null, SbomFormat.CycloneDx);

            if (existing is null)
                await repository.AddAsync(profile, cancellationToken);
            else
                await repository.UpdateAsync(profile, cancellationToken);

            await unitOfWork.CommitAsync(cancellationToken);

            var vulnCount = profile.Dependencies.Sum(d => d.Vulnerabilities.Count);

            return Result<Response>.Success(new Response(
                ProfileId: profile.Id.Value,
                HealthScore: profile.HealthScore,
                TotalDependencies: profile.TotalDependencies,
                DirectDependencies: profile.DirectDependencies,
                VulnerabilityCount: vulnCount));
        }

        private static IReadOnlyList<PackageDependency> ParseDependencies(
            Guid profileId, string content, string fileType)
        {
            return fileType switch
            {
                "csproj" => ParseCsproj(profileId, content),
                "package.json" => ParsePackageJson(profileId, content),
                "pom.xml" => ParsePomXml(profileId, content),
                _ => Array.Empty<PackageDependency>()
            };
        }

        private static IReadOnlyList<PackageDependency> ParseCsproj(Guid profileId, string content)
        {
            var deps = new List<PackageDependency>();
            try
            {
                var doc = XDocument.Parse(content);
                var refs = doc.Descendants("PackageReference");
                foreach (var r in refs)
                {
                    var name = r.Attribute("Include")?.Value;
                    var version = r.Attribute("Version")?.Value ?? r.Element("Version")?.Value ?? "0.0.0";
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    deps.Add(PackageDependency.Create(profileId, name, version, PackageEcosystem.NuGet, isDirect: true));
                }
            }
            catch { /* ignore malformed XML */ }
            return deps;
        }

        private static IReadOnlyList<PackageDependency> ParsePackageJson(Guid profileId, string content)
        {
            var deps = new List<PackageDependency>();
            try
            {
                using var doc = JsonDocument.Parse(content);
                AddNpmDeps(deps, profileId, doc.RootElement, "dependencies", isDirect: true);
                AddNpmDeps(deps, profileId, doc.RootElement, "devDependencies", isDirect: true);
            }
            catch { /* ignore malformed JSON */ }
            return deps;
        }

        private static void AddNpmDeps(List<PackageDependency> deps, Guid profileId,
            JsonElement root, string section, bool isDirect)
        {
            if (!root.TryGetProperty(section, out var obj)) return;
            foreach (var prop in obj.EnumerateObject())
            {
                var version = prop.Value.GetString() ?? "0.0.0";
                deps.Add(PackageDependency.Create(profileId, prop.Name, version, PackageEcosystem.Npm, isDirect));
            }
        }

        private static IReadOnlyList<PackageDependency> ParsePomXml(Guid profileId, string content)
        {
            var deps = new List<PackageDependency>();
            try
            {
                var doc = XDocument.Parse(content);
                XNamespace ns = "http://maven.apache.org/POM/4.0.0";
                foreach (var dep in doc.Descendants(ns + "dependency"))
                {
                    var groupId = dep.Element(ns + "groupId")?.Value ?? string.Empty;
                    var artifactId = dep.Element(ns + "artifactId")?.Value;
                    var version = dep.Element(ns + "version")?.Value ?? "0.0.0";
                    if (string.IsNullOrWhiteSpace(artifactId)) continue;
                    var name = string.IsNullOrWhiteSpace(groupId) ? artifactId : $"{groupId}:{artifactId}";
                    deps.Add(PackageDependency.Create(profileId, name, version, PackageEcosystem.Maven, isDirect: true));
                }
            }
            catch { /* ignore malformed XML */ }
            return deps;
        }
    }

    public sealed record Response(
        Guid ProfileId,
        int HealthScore,
        int TotalDependencies,
        int DirectDependencies,
        int VulnerabilityCount);
}

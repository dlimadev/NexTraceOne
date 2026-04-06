using NexTraceOne.Catalog.Application.DependencyGovernance.Features.ScanServiceDependencies;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;

namespace NexTraceOne.Catalog.Tests.DependencyGovernance;

public class ScanServiceDependenciesTests
{
    private static readonly Guid ServiceId = Guid.NewGuid();

    private static ScanServiceDependencies.Handler CreateHandler(
        InMemoryServiceDependencyProfileRepository? repo = null)
    {
        repo ??= new InMemoryServiceDependencyProfileRepository();
        return new ScanServiceDependencies.Handler(repo, new InMemoryUnitOfWork());
    }

    [Fact]
    public async Task Handle_CsprojFile_ParsesNuGetDependencies()
    {
        var handler = CreateHandler();
        var csproj = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
                <PackageReference Include="Serilog" Version="3.1.1" />
              </ItemGroup>
            </Project>
            """;

        var result = await handler.Handle(
            new ScanServiceDependencies.Command(ServiceId, csproj, "csproj"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalDependencies.Should().Be(2);
        result.Value.DirectDependencies.Should().Be(2);
    }

    [Fact]
    public async Task Handle_PackageJson_ParsesNpmDependencies()
    {
        var handler = CreateHandler();
        var packageJson = """
            {
                "dependencies": { "react": "18.0.0", "axios": "1.6.0" },
                "devDependencies": { "typescript": "5.0.0" }
            }
            """;

        var result = await handler.Handle(
            new ScanServiceDependencies.Command(ServiceId, packageJson, "package.json"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalDependencies.Should().Be(3);
    }

    [Fact]
    public async Task Handle_PomXml_ParsesMavenDependencies()
    {
        var handler = CreateHandler();
        var pom = """
            <?xml version="1.0" encoding="UTF-8"?>
            <project xmlns="http://maven.apache.org/POM/4.0.0">
              <dependencies>
                <dependency>
                  <groupId>org.springframework</groupId>
                  <artifactId>spring-core</artifactId>
                  <version>5.3.0</version>
                </dependency>
              </dependencies>
            </project>
            """;

        var result = await handler.Handle(
            new ScanServiceDependencies.Command(ServiceId, pom, "pom.xml"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalDependencies.Should().Be(1);
    }

    [Fact]
    public async Task Handle_CreatesNewProfile_WhenNotExists()
    {
        var repo = new InMemoryServiceDependencyProfileRepository();
        var handler = CreateHandler(repo);

        var result = await handler.Handle(
            new ScanServiceDependencies.Command(ServiceId, "<Project/>", "csproj"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var profile = await repo.FindByServiceIdAsync(ServiceId, CancellationToken.None);
        profile.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_UpdatesExistingProfile_WhenExists()
    {
        var repo = new InMemoryServiceDependencyProfileRepository();
        var handler = CreateHandler(repo);

        // First scan
        await handler.Handle(
            new ScanServiceDependencies.Command(ServiceId,
                """<Project><ItemGroup><PackageReference Include="A" Version="1.0"/></ItemGroup></Project>""",
                "csproj"),
            CancellationToken.None);

        // Second scan
        var result = await handler.Handle(
            new ScanServiceDependencies.Command(ServiceId,
                """<Project><ItemGroup><PackageReference Include="A" Version="1.0"/><PackageReference Include="B" Version="2.0"/></ItemGroup></Project>""",
                "csproj"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalDependencies.Should().Be(2);
    }

    [Fact]
    public async Task Handle_DefaultHealthScore_WhenNoDeps()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(
            new ScanServiceDependencies.Command(ServiceId, "<Project/>", "csproj"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HealthScore.Should().Be(100);
        result.Value.VulnerabilityCount.Should().Be(0);
    }
}

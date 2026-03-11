#Requires -Version 7.0
<#
.SYNOPSIS
    Script de scaffolding completo da plataforma NexTraceOne v2 — Archon Pattern.

.DESCRIPTION
    Cria toda a estrutura da solução NexTraceOne — Sovereign Change Intelligence Platform.
    Gera: solução .NET 10, todos os projetos, estrutura de diretórios VSA, stubs de classes
    com documentação XML em português, referências entre projetos e configurações base.

    Arquitetura v2 (Archon Pattern):
      - Building Blocks granulares (substituem SharedKernel monolítico)
      - Cada módulo com 5 projetos: API, Application, Contracts, Domain, Infrastructure
      - Platform: ApiHost (composição), BackgroundWorkers (jobs/outbox), CLI
      - Pronto para futura extração de microserviços sem refactoring

    Building Blocks: Domain, Application, Infrastructure, EventBus, Observability, Security
    Módulos: Identity, Licensing, EngineeringGraph, DeveloperPortal, Contracts,
             ChangeIntelligence, RulesetGovernance, Workflow, Promotion,
             RuntimeIntelligence, CostIntelligence, AiOrchestration, ExternalAi, Audit

.PARAMETER RootPath
    Diretório raiz onde a solução será criada. Padrão: diretório atual.

.PARAMETER SkipDotnetRestore
    Pula o restore do NuGet após a criação. Útil para ambientes sem internet.

.EXAMPLE
    .\New-NexTraceOne.ps1 -RootPath "C:\Projects"
    .\New-NexTraceOne.ps1 -RootPath "C:\Projects" -SkipDotnetRestore

.NOTES
    Pré-requisitos: .NET 10 SDK instalado e disponível no PATH.
    Versão: 2.0.0 | NexTraceOne MVP1 — Archon Pattern
#>

[CmdletBinding()]
param(
    [string]$RootPath = (Get-Location).Path,
    [switch]$SkipDotnetRestore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ═══════════════════════════════════════════════════════════════════════════════
# CONFIGURAÇÕES GLOBAIS
# ═══════════════════════════════════════════════════════════════════════════════

$Script:SolutionName   = "NexTraceOne"
$Script:SolutionRoot   = Join-Path $RootPath $SolutionName
$Script:SrcDir         = Join-Path $SolutionRoot "src"
$Script:BBDir          = Join-Path $SrcDir "building-blocks"
$Script:ModulesDir     = Join-Path $SrcDir "modules"
$Script:PlatformDir    = Join-Path $SrcDir "platform"
$Script:TestsDir       = Join-Path $SolutionRoot "tests"
$Script:TestBBDir      = Join-Path $TestsDir "building-blocks"
$Script:TestModDir     = Join-Path $TestsDir "modules"
$Script:TestPlatDir    = Join-Path $TestsDir "platform"
$Script:ToolsDir       = Join-Path $SolutionRoot "tools"
$Script:BuildDir       = Join-Path $SolutionRoot "build"
$Script:DocsDir        = Join-Path $SolutionRoot "docs"

# Prefixos de namespace
$Script:BB = "NexTraceOne.BuildingBlocks"
$Script:MOD = "NexTraceOne"

# Cores para output
function Write-Step   { param($msg) Write-Host "`n▶  $msg" -ForegroundColor Cyan }
function Write-OK     { param($msg) Write-Host "   ✔  $msg" -ForegroundColor Green }
function Write-Info   { param($msg) Write-Host "   ·  $msg" -ForegroundColor Gray }
function Write-Header { param($msg) Write-Host "`n$('═' * 70)`n  $msg`n$('═' * 70)" -ForegroundColor Yellow }

# ═══════════════════════════════════════════════════════════════════════════════
# HELPERS
# ═══════════════════════════════════════════════════════════════════════════════

function New-Directory {
    param([string]$Path)
    if (-not (Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function New-StubFile {
    param([string]$Path, [string]$Content)
    $dir = Split-Path $Path -Parent
    New-Directory $dir
    if (-not (Test-Path $Path)) {
        Set-Content -Path $Path -Value $Content -Encoding UTF8
    }
}

function Invoke-Dotnet {
    param([string[]]$DotnetArgs, [string]$WorkDir = $SolutionRoot)
    $prev = Get-Location
    if (-not (Test-Path $WorkDir)) { New-Item -ItemType Directory -Path $WorkDir -Force | Out-Null }
    Set-Location $WorkDir
    & dotnet @DotnetArgs
    if ($LASTEXITCODE -ne 0) {
        Set-Location $prev
        throw "dotnet $($DotnetArgs -join ' ') falhou com exit code $LASTEXITCODE"
    }
    Set-Location $prev
}

# ═══════════════════════════════════════════════════════════════════════════════
# CONTEÚDO DOS ARQUIVOS CSPROJ — BUILDING BLOCKS
# ═══════════════════════════════════════════════════════════════════════════════

function Get-BBDomainCsproj {
    return @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>NexTraceOne.BuildingBlocks.Domain</RootNamespace>
    <AssemblyName>NexTraceOne.BuildingBlocks.Domain</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MediatR" />
    <PackageReference Include="Ardalis.GuardClauses" />
  </ItemGroup>
</Project>
'@
}

function Get-BBApplicationCsproj {
    return @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>NexTraceOne.BuildingBlocks.Application</RootNamespace>
    <AssemblyName>NexTraceOne.BuildingBlocks.Application</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\NexTraceOne.BuildingBlocks.Domain\NexTraceOne.BuildingBlocks.Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="MediatR" />
    <PackageReference Include="FluentValidation" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
  </ItemGroup>
</Project>
'@
}

function Get-BBInfrastructureCsproj {
    return @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>NexTraceOne.BuildingBlocks.Infrastructure</RootNamespace>
    <AssemblyName>NexTraceOne.BuildingBlocks.Infrastructure</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\NexTraceOne.BuildingBlocks.Application\NexTraceOne.BuildingBlocks.Application.csproj" />
    <ProjectReference Include="..\NexTraceOne.BuildingBlocks.Domain\NexTraceOne.BuildingBlocks.Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
    <PackageReference Include="Mapster" />
  </ItemGroup>
</Project>
'@
}

function Get-BBEventBusCsproj {
    return @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>NexTraceOne.BuildingBlocks.EventBus</RootNamespace>
    <AssemblyName>NexTraceOne.BuildingBlocks.EventBus</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\NexTraceOne.BuildingBlocks.Application\NexTraceOne.BuildingBlocks.Application.csproj" />
    <ProjectReference Include="..\NexTraceOne.BuildingBlocks.Domain\NexTraceOne.BuildingBlocks.Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="MediatR" />
  </ItemGroup>
</Project>
'@
}

function Get-BBObservabilityCsproj {
    return @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>NexTraceOne.BuildingBlocks.Observability</RootNamespace>
    <AssemblyName>NexTraceOne.BuildingBlocks.Observability</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\NexTraceOne.BuildingBlocks.Domain\NexTraceOne.BuildingBlocks.Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="OpenTelemetry" />
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" />
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" />
    <PackageReference Include="Serilog" />
    <PackageReference Include="Serilog.AspNetCore" />
    <PackageReference Include="Serilog.Sinks.Console" />
    <PackageReference Include="Serilog.Sinks.File" />
    <PackageReference Include="Serilog.Enrichers.Environment" />
  </ItemGroup>
</Project>
'@
}

function Get-BBSecurityCsproj {
    return @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>NexTraceOne.BuildingBlocks.Security</RootNamespace>
    <AssemblyName>NexTraceOne.BuildingBlocks.Security</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\NexTraceOne.BuildingBlocks.Application\NexTraceOne.BuildingBlocks.Application.csproj" />
    <ProjectReference Include="..\NexTraceOne.BuildingBlocks.Domain\NexTraceOne.BuildingBlocks.Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
  </ItemGroup>
</Project>
'@
}

# ═══════════════════════════════════════════════════════════════════════════════
# CONTEÚDO DOS ARQUIVOS CSPROJ — MÓDULOS (5 camadas por módulo)
# ═══════════════════════════════════════════════════════════════════════════════

function Get-ModuleDomainCsproj {
    param([string]$ModuleName)
    return @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>NexTraceOne.$ModuleName.Domain</RootNamespace>
    <AssemblyName>NexTraceOne.$ModuleName.Domain</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\building-blocks\NexTraceOne.BuildingBlocks.Domain\NexTraceOne.BuildingBlocks.Domain.csproj" />
  </ItemGroup>
</Project>
"@
}

function Get-ModuleApplicationCsproj {
    param([string]$ModuleName)
    return @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>NexTraceOne.$ModuleName.Application</RootNamespace>
    <AssemblyName>NexTraceOne.$ModuleName.Application</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\NexTraceOne.$ModuleName.Domain\NexTraceOne.$ModuleName.Domain.csproj" />
    <ProjectReference Include="..\NexTraceOne.$ModuleName.Contracts\NexTraceOne.$ModuleName.Contracts.csproj" />
    <ProjectReference Include="..\..\..\building-blocks\NexTraceOne.BuildingBlocks.Application\NexTraceOne.BuildingBlocks.Application.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="MediatR" />
    <PackageReference Include="FluentValidation" />
  </ItemGroup>
</Project>
"@
}

function Get-ModuleContractsCsproj {
    param([string]$ModuleName)
    return @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>NexTraceOne.$ModuleName.Contracts</RootNamespace>
    <AssemblyName>NexTraceOne.$ModuleName.Contracts</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\building-blocks\NexTraceOne.BuildingBlocks.Domain\NexTraceOne.BuildingBlocks.Domain.csproj" />
  </ItemGroup>
</Project>
"@
}

function Get-ModuleInfrastructureCsproj {
    param([string]$ModuleName)
    return @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>NexTraceOne.$ModuleName.Infrastructure</RootNamespace>
    <AssemblyName>NexTraceOne.$ModuleName.Infrastructure</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\NexTraceOne.$ModuleName.Application\NexTraceOne.$ModuleName.Application.csproj" />
    <ProjectReference Include="..\NexTraceOne.$ModuleName.Domain\NexTraceOne.$ModuleName.Domain.csproj" />
    <ProjectReference Include="..\..\..\building-blocks\NexTraceOne.BuildingBlocks.Infrastructure\NexTraceOne.BuildingBlocks.Infrastructure.csproj" />
    <ProjectReference Include="..\..\..\building-blocks\NexTraceOne.BuildingBlocks.EventBus\NexTraceOne.BuildingBlocks.EventBus.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
    <PackageReference Include="Mapster" />
  </ItemGroup>
</Project>
"@
}

function Get-ModuleApiCsproj {
    param([string]$ModuleName)
    return @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>NexTraceOne.$ModuleName.API</RootNamespace>
    <AssemblyName>NexTraceOne.$ModuleName.API</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\NexTraceOne.$ModuleName.Application\NexTraceOne.$ModuleName.Application.csproj" />
    <ProjectReference Include="..\NexTraceOne.$ModuleName.Contracts\NexTraceOne.$ModuleName.Contracts.csproj" />
    <ProjectReference Include="..\..\..\building-blocks\NexTraceOne.BuildingBlocks.Security\NexTraceOne.BuildingBlocks.Security.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="MediatR" />
  </ItemGroup>
</Project>
"@
}

# ═══════════════════════════════════════════════════════════════════════════════
# CONTEÚDO DOS ARQUIVOS CSPROJ — PLATFORM
# ═══════════════════════════════════════════════════════════════════════════════

function Get-ApiHostCsproj {
    param([string[]]$ModuleNames)
    $moduleRefs = $ModuleNames | ForEach-Object {
        $lower = $_.ToLower()
        @"
    <ProjectReference Include="..\..\modules\$lower\NexTraceOne.$_.API\NexTraceOne.$_.API.csproj" />
    <ProjectReference Include="..\..\modules\$lower\NexTraceOne.$_.Infrastructure\NexTraceOne.$_.Infrastructure.csproj" />
"@
    }
    $allRefs = $moduleRefs -join "`n"

    return @"
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>NexTraceOne.ApiHost</RootNamespace>
    <AssemblyName>NexTraceOne.ApiHost</AssemblyName>
    <UserSecretsId>nextraceone-apihost-dev</UserSecretsId>
  </PropertyGroup>
  <ItemGroup Label="Building Blocks">
    <ProjectReference Include="..\..\building-blocks\NexTraceOne.BuildingBlocks.Infrastructure\NexTraceOne.BuildingBlocks.Infrastructure.csproj" />
    <ProjectReference Include="..\..\building-blocks\NexTraceOne.BuildingBlocks.EventBus\NexTraceOne.BuildingBlocks.EventBus.csproj" />
    <ProjectReference Include="..\..\building-blocks\NexTraceOne.BuildingBlocks.Observability\NexTraceOne.BuildingBlocks.Observability.csproj" />
    <ProjectReference Include="..\..\building-blocks\NexTraceOne.BuildingBlocks.Security\NexTraceOne.BuildingBlocks.Security.csproj" />
  </ItemGroup>
  <ItemGroup Label="Módulos (API + Infrastructure)">
$allRefs
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
    <PackageReference Include="Serilog.AspNetCore" />
    <PackageReference Include="Quartz" />
    <PackageReference Include="Quartz.Extensions.Hosting" />
  </ItemGroup>
</Project>
"@
}

function Get-BackgroundWorkersCsproj {
    param([string[]]$ModuleNames)
    $moduleRefs = $ModuleNames | ForEach-Object {
        $lower = $_.ToLower()
        "    <ProjectReference Include=`"..\..\modules\$lower\NexTraceOne.$_.Infrastructure\NexTraceOne.$_.Infrastructure.csproj`" />"
    }
    $allRefs = $moduleRefs -join "`n"

    return @"
<Project Sdk="Microsoft.NET.Sdk.Worker">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>NexTraceOne.BackgroundWorkers</RootNamespace>
    <AssemblyName>NexTraceOne.BackgroundWorkers</AssemblyName>
  </PropertyGroup>
  <ItemGroup Label="Building Blocks">
    <ProjectReference Include="..\..\building-blocks\NexTraceOne.BuildingBlocks.Infrastructure\NexTraceOne.BuildingBlocks.Infrastructure.csproj" />
    <ProjectReference Include="..\..\building-blocks\NexTraceOne.BuildingBlocks.EventBus\NexTraceOne.BuildingBlocks.EventBus.csproj" />
    <ProjectReference Include="..\..\building-blocks\NexTraceOne.BuildingBlocks.Observability\NexTraceOne.BuildingBlocks.Observability.csproj" />
  </ItemGroup>
  <ItemGroup Label="Módulos (Infrastructure)">
$allRefs
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Quartz" />
    <PackageReference Include="Quartz.Extensions.Hosting" />
    <PackageReference Include="Serilog.AspNetCore" />
  </ItemGroup>
</Project>
"@
}

function Get-CliCsproj {
    param([string[]]$ModuleNames)
    $moduleRefs = $ModuleNames | ForEach-Object {
        $lower = $_.ToLower()
        "    <ProjectReference Include=`"..\..\src\modules\$lower\NexTraceOne.$_.Contracts\NexTraceOne.$_.Contracts.csproj`" />"
    }
    $allRefs = $moduleRefs -join "`n"

    return @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>NexTraceOne.CLI</RootNamespace>
    <AssemblyName>nex</AssemblyName>
    <SelfContained>false</SelfContained>
  </PropertyGroup>
  <ItemGroup Label="Módulos (apenas Contracts — consumidor externo)">
$allRefs
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="System.CommandLine" />
    <PackageReference Include="Spectre.Console" />
    <PackageReference Include="Spectre.Console.Cli" />
  </ItemGroup>
</Project>
"@
}

# ═══════════════════════════════════════════════════════════════════════════════
# CONTEÚDO DOS ARQUIVOS CSPROJ — TESTES
# ═══════════════════════════════════════════════════════════════════════════════

function Get-TestCsproj {
    param([string[]]$ProjectRefs)
    $refs = $ProjectRefs | Where-Object { $_ } | ForEach-Object {
        "    <ProjectReference Include=`"$_`" />"
    }
    $refsBlock = if ($refs) { $refs -join "`n" } else { "" }
    return @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
$refsBlock
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="Bogus" />
  </ItemGroup>
</Project>
"@
}

# ═══════════════════════════════════════════════════════════════════════════════
# STUBS — BUILDING BLOCKS: DOMAIN
# ═══════════════════════════════════════════════════════════════════════════════

function New-BBDomainStubs {
    param([string]$Base)

    # ── Primitives ─────────────────────────────────────────────────────────────
    New-StubFile "$Base/Primitives/Entity.cs" @'
namespace NexTraceOne.BuildingBlocks.Domain.Primitives;

/// <summary>
/// Classe base para todas as entidades do domínio da plataforma NexTraceOne.
/// Implementa igualdade baseada em identidade (Id), não em referência de objeto.
/// Toda entidade possui um identificador fortemente tipado que garante que
/// Ids de tipos diferentes nunca sejam comparados acidentalmente.
/// </summary>
/// <typeparam name="TId">Tipo do identificador, deve implementar ITypedId.</typeparam>
public abstract class Entity<TId> where TId : ITypedId
{
    /// <summary>Identificador único e imutável desta entidade.</summary>
    public TId Id { get; protected init; } = default!;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (Id.Equals(default(TId)) || other.Id.Equals(default(TId))) return false;
        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
        => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
        => !Equals(left, right);
}
'@

    New-StubFile "$Base/Primitives/AggregateRoot.cs" @'
namespace NexTraceOne.BuildingBlocks.Domain.Primitives;

/// <summary>
/// Classe base para todos os Aggregate Roots do domínio.
/// Um Aggregate Root é a única entidade através da qual o lado de fora
/// pode interagir com o agregado. Ele é responsável por manter as invariantes
/// e a consistência transacional de todas as entidades do agregado.
/// Também é o único responsável por emitir Domain Events.
/// </summary>
/// <typeparam name="TId">Tipo do identificador fortemente tipado.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId> where TId : ITypedId
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Coleção imutável dos Domain Events emitidos por este aggregate
    /// durante a operação atual. Serão coletados pelo DbContext antes do commit.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Registra um Domain Event para ser publicado após o commit.</summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    /// <summary>Limpa a fila de eventos após coleta pelo DbContext.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
'@

    New-StubFile "$Base/Primitives/ValueObject.cs" @'
namespace NexTraceOne.BuildingBlocks.Domain.Primitives;

/// <summary>
/// Classe base para Value Objects do domínio.
/// Value Objects são imutáveis e sua igualdade é baseada nos valores
/// de suas propriedades, não em identidade. São descartados e recriados
/// quando precisam mudar — nunca modificados in-place.
/// Exemplos: SemanticVersion, TenantId, Money, Email, GitContext.
/// </summary>
public abstract class ValueObject
{
    /// <summary>
    /// Retorna os componentes que definem a igualdade deste Value Object.
    /// Toda subclasse DEVE sobrescrever este método retornando seus campos relevantes.
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
        => GetEqualityComponents()
            .Aggregate(default(int), HashCode.Combine);

    public static bool operator ==(ValueObject? left, ValueObject? right)
        => Equals(left, right);

    public static bool operator !=(ValueObject? left, ValueObject? right)
        => !Equals(left, right);
}
'@

    New-StubFile "$Base/Primitives/AuditableEntity.cs" @'
namespace NexTraceOne.BuildingBlocks.Domain.Primitives;

/// <summary>
/// Extensão de Entity com campos de auditoria automáticos.
/// Toda entidade que herdar desta classe terá CreatedAt, CreatedBy,
/// UpdatedAt e UpdatedBy preenchidos automaticamente pelo AuditInterceptor
/// do DbContext, antes de cada SaveChanges.
/// </summary>
public abstract class AuditableEntity<TId> : Entity<TId> where TId : ITypedId
{
    /// <summary>Data/hora UTC de criação do registro.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Id do usuário que criou o registro.</summary>
    public string CreatedBy { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC da última atualização.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Id do usuário que realizou a última atualização.</summary>
    public string UpdatedBy { get; private set; } = string.Empty;

    /// <summary>Indica se o registro foi removido logicamente (soft-delete).</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>Chamado pelo AuditInterceptor ao criar. Não chamar diretamente.</summary>
    public void SetCreated(DateTimeOffset at, string by) { CreatedAt = at; CreatedBy = by; }

    /// <summary>Chamado pelo AuditInterceptor ao atualizar. Não chamar diretamente.</summary>
    public void SetUpdated(DateTimeOffset at, string by) { UpdatedAt = at; UpdatedBy = by; }

    /// <summary>Marca o registro como removido logicamente. Não deleta do banco.</summary>
    public void SoftDelete() => IsDeleted = true;
}
'@

    # ── StronglyTypedIds ────────────────────────────────────────────────────
    New-StubFile "$Base/StronglyTypedIds/ITypedId.cs" @'
namespace NexTraceOne.BuildingBlocks.Domain;

/// <summary>
/// Marcador para todos os identificadores fortemente tipados da plataforma.
/// Usando ITypedId nos genéricos, garantimos que métodos como GetByIdAsync
/// só aceitem o tipo de Id correto, evitando confusões entre, por exemplo,
/// ReleaseId e AssetId que seriam ambos Guid sem esta abstração.
/// </summary>
public interface ITypedId
{
    /// <summary>Valor bruto do identificador (normalmente Guid).</summary>
    Guid Value { get; }
}
'@

    New-StubFile "$Base/StronglyTypedIds/TypedIdBase.cs" @'
namespace NexTraceOne.BuildingBlocks.Domain;

/// <summary>
/// Implementação base para identificadores fortemente tipados.
/// Cada módulo cria seus próprios Ids herdando desta classe.
/// Exemplo: public sealed record ReleaseId(Guid Value) : TypedIdBase(Value);
/// </summary>
public abstract record TypedIdBase(Guid Value) : ITypedId
{
    /// <summary>Cria um novo Id com Guid gerado automaticamente.</summary>
    public static Guid NewId() => Guid.NewGuid();

    /// <summary>Representação string do Id (formato UUID padrão).</summary>
    public override string ToString() => Value.ToString();
}
'@

    # ── Events ───────────────────────────────────────────────────────────────
    New-StubFile "$Base/Events/IDomainEvent.cs" @'
using MediatR;

namespace NexTraceOne.BuildingBlocks.Domain;

/// <summary>
/// Marcador para todos os Domain Events da plataforma.
/// Domain Events representam algo relevante que aconteceu dentro do domínio.
/// São emitidos pelo Aggregate Root e processados de forma assíncrona
/// pelo pipeline do Outbox após o commit da transação.
/// REGRA: Domain Events são intra-módulo. Para comunicação entre módulos,
/// use Integration Events.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>Identificador único deste evento (para idempotência e rastreio).</summary>
    Guid EventId { get; }

    /// <summary>Data/hora UTC em que o evento ocorreu.</summary>
    DateTimeOffset OccurredAt { get; }
}
'@

    New-StubFile "$Base/Events/IIntegrationEvent.cs" @'
namespace NexTraceOne.BuildingBlocks.Domain;

/// <summary>
/// Marcador para Integration Events — eventos publicados entre módulos distintos.
/// Ao contrário dos Domain Events (intra-módulo), os Integration Events
/// cruzam fronteiras de bounded context. São serializados como OutboxMessages
/// e consumidos de forma assíncrona por outros módulos via Outbox Processor.
/// REGRA: Módulos nunca acessam DbContext de outros módulos diretamente.
/// A comunicação é sempre via Integration Events ou contratos públicos.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>Identificador único do evento para garantia de idempotência.</summary>
    Guid EventId { get; }

    /// <summary>Data/hora UTC de ocorrência.</summary>
    DateTimeOffset OccurredAt { get; }

    /// <summary>Nome do módulo de origem (ex: "ChangeIntelligence").</summary>
    string SourceModule { get; }
}
'@

    New-StubFile "$Base/Events/DomainEventBase.cs" @'
namespace NexTraceOne.BuildingBlocks.Domain;

/// <summary>
/// Implementação base para Domain Events.
/// Preenche automaticamente EventId e OccurredAt.
/// Todo Domain Event da plataforma deve herdar desta classe.
/// Exemplo: public sealed record ReleaseCreatedDomainEvent(ReleaseId ReleaseId) : DomainEventBase;
/// </summary>
public abstract record DomainEventBase : IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc/>
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
'@

    # ── Results ──────────────────────────────────────────────────────────────
    New-StubFile "$Base/Results/Result.cs" @'
namespace NexTraceOne.BuildingBlocks.Domain.Results;

/// <summary>
/// Padrão Result para operações que podem falhar sem lançar exceção.
/// Evita o uso de exceções para controle de fluxo em casos esperados
/// (validação, not found, conflito) e força o caller a tratar o resultado.
/// Uso: return Result.Success(value); ou return Error.NotFound("...");
/// </summary>
public sealed class Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T value)     { _value = value; IsSuccess = true; }
    private Result(Error error) { _error = error; IsSuccess = false; }

    /// <summary>Indica se a operação foi bem-sucedida.</summary>
    public bool IsSuccess { get; }

    /// <summary>Indica se a operação falhou.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Valor da operação bem-sucedida. Lança se IsFailure.</summary>
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Result is failure. Access Error instead.");

    /// <summary>Erro da operação falha. Lança se IsSuccess.</summary>
    public Error Error => IsFailure ? _error! : throw new InvalidOperationException("Result is success. Access Value instead.");

    /// <summary>Cria um Result de sucesso com o valor informado.</summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>Cria um Result de falha com o erro informado.</summary>
    public static implicit operator Result<T>(Error error) => new(error);

    /// <summary>Conversão implícita de T para Result de sucesso.</summary>
    public static implicit operator Result<T>(T value) => new(value);

    /// <summary>Executa action caso o resultado seja sucesso.</summary>
    public Result<T> OnSuccess(Action<T> action) { if (IsSuccess) action(_value!); return this; }

    /// <summary>Executa action caso o resultado seja falha.</summary>
    public Result<T> OnFailure(Action<Error> action) { if (IsFailure) action(_error!); return this; }

    /// <summary>Projeta o valor de sucesso para outro tipo mantendo o Result.</summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
        => IsSuccess ? Result<TOut>.Success(mapper(_value!)) : new Error(_error!.Code, _error.Message, _error.Type);
}
'@

    New-StubFile "$Base/Results/Error.cs" @'
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
'@

    # ── Guards ───────────────────────────────────────────────────────────────
    New-StubFile "$Base/Guards/NexTraceGuards.cs" @'
using Ardalis.GuardClauses;

namespace NexTraceOne.BuildingBlocks.Domain.Guards;

/// <summary>
/// Extensões de Guard específicas do domínio NexTraceOne.
/// Complementam as guards genéricas do Ardalis.GuardClauses com
/// validações de negócio recorrentes na plataforma.
/// Uso: Guard.Against.InvalidSemanticVersion(version);
/// </summary>
public static class NexTraceGuards
{
    /// <summary>
    /// Verifica que a string representa uma versão semântica válida (SemVer 2.0).
    /// Lança ArgumentException se inválida, convertida para HTTP 400 pelo middleware.
    /// </summary>
    public static string InvalidSemanticVersion(this IGuardClause _, string version, string paramName = "version")
    {
        // TODO: Implementar validação SemVer 2.0 com regex
        throw new NotImplementedException();
    }

    /// <summary>
    /// Verifica que o ambiente informado é um ambiente governado válido.
    /// Ambientes não governados (ex: Development) são rejeitados.
    /// </summary>
    public static string UngovernedEnvironment(this IGuardClause _, string environment, string paramName = "environment")
    {
        // TODO: Implementar validação de ambiente governado
        throw new NotImplementedException();
    }

    /// <summary>
    /// Verifica que o TenantId não é Guid.Empty.
    /// Requisições sem tenant ativo são rejeitadas.
    /// </summary>
    public static Guid EmptyTenantId(this IGuardClause _, Guid tenantId, string paramName = "tenantId")
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId cannot be empty.", paramName);
        return tenantId;
    }
}
'@

    # ── Enums ────────────────────────────────────────────────────────────────
    New-StubFile "$Base/Enums/ChangeLevel.cs" @'
namespace NexTraceOne.BuildingBlocks.Domain.Enums;

/// <summary>
/// Taxonomia de níveis de mudança da plataforma NexTraceOne.
/// Define a gravidade e o fluxo de governança necessário para cada tipo de mudança.
/// </summary>
public enum ChangeLevel
{
    /// <summary>Nível 0 — Eventos operacionais. Sem versão, sem workflow.</summary>
    Operational = 0,

    /// <summary>Nível 1 — Mudança sem alteração de contrato. Patch version.</summary>
    NonBreaking = 1,

    /// <summary>Nível 2 — Mudança com contrato non-breaking. Minor version.</summary>
    Additive = 2,

    /// <summary>Nível 3 — Mudança com contrato breaking. MAJOR version.</summary>
    Breaking = 3,

    /// <summary>Nível 4 — Eventos de publicação. Sem nova versão.</summary>
    Publication = 4
}
'@

    New-StubFile "$Base/Enums/DiscoveryConfidence.cs" @'
namespace NexTraceOne.BuildingBlocks.Domain.Enums;

/// <summary>
/// Modelo de confiança para dependências descobertas entre serviços.
/// Usado pelo módulo EngineeringGraph para classificar a qualidade da descoberta.
/// </summary>
public enum DiscoveryConfidence
{
    /// <summary>Inferido por análise de código ou heurística. Pode ser falso positivo.</summary>
    Inferred = 0,

    /// <summary>Detectado em logs de gateway com baixo volume de tráfego.</summary>
    Low = 1,

    /// <summary>Detectado via análise estática de contratos ou configuração.</summary>
    Medium = 2,

    /// <summary>Confirmado por traces OpenTelemetry com volume significativo.</summary>
    High = 3,

    /// <summary>Confirmado manualmente por um humano ou importado de catálogo oficial.</summary>
    Confirmed = 4
}
'@

    Write-OK "BuildingBlocks.Domain — stubs completos"
}

# ═══════════════════════════════════════════════════════════════════════════════
# STUBS — BUILDING BLOCKS: APPLICATION
# ═══════════════════════════════════════════════════════════════════════════════

function New-BBApplicationStubs {
    param([string]$Base)

    # ── CQRS ─────────────────────────────────────────────────────────────────
    New-StubFile "$Base/Cqrs/ICommand.cs" @'
using MediatR;
using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.BuildingBlocks.Application.Cqrs;

/// <summary>
/// Marcador para Commands sem resposta tipada.
/// Commands representam intenções de mudar o estado do sistema.
/// REGRA CQRS: Commands modificam estado e não retornam dados de leitura.
/// </summary>
public interface ICommand : IRequest<Result<Unit>> { }

/// <summary>
/// Marcador para Commands com resposta tipada.
/// Usado quando o command precisa retornar o Id do aggregate criado.
/// </summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }

/// <summary>Handler para Commands sem resposta tipada.</summary>
public interface ICommandHandler<TCommand>
    : IRequestHandler<TCommand, Result<Unit>>
    where TCommand : ICommand { }

/// <summary>Handler para Commands com resposta tipada.</summary>
public interface ICommandHandler<TCommand, TResponse>
    : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse> { }
'@

    New-StubFile "$Base/Cqrs/IQuery.cs" @'
using MediatR;
using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.BuildingBlocks.Application.Cqrs;

/// <summary>
/// Marcador para Queries tipadas.
/// Queries são somente-leitura e não modificam estado.
/// REGRA CQRS: Queries nunca chamam repositórios de escrita nem disparam Domain Events.
/// </summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }

/// <summary>Handler para Queries tipadas.</summary>
public interface IQueryHandler<TQuery, TResponse>
    : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse> { }
'@

    New-StubFile "$Base/Cqrs/IPagedQuery.cs" @'
namespace NexTraceOne.BuildingBlocks.Application.Cqrs;

/// <summary>
/// Contrato para Queries com paginação padronizada.
/// Todos os endpoints de listagem devem implementar este contrato.
/// </summary>
public interface IPagedQuery
{
    /// <summary>Número da página atual. Começa em 1.</summary>
    int Page { get; }

    /// <summary>Itens por página. Máximo: 100. Padrão: 20.</summary>
    int PageSize { get; }
}
'@

    # ── Behaviors ────────────────────────────────────────────────────────────
    New-StubFile "$Base/Behaviors/ValidationBehavior.cs" @'
using FluentValidation;
using MediatR;

namespace NexTraceOne.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Pipeline behavior do MediatR que executa FluentValidation automaticamente
/// antes de qualquer Command Handler.
/// Se houver erros de validação, o handler NÃO é chamado — o Result de falha
/// com ErrorType.Validation é retornado diretamente.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // TODO: Implementar execução de validators e retorno de Result de falha
        throw new NotImplementedException();
    }
}
'@

    New-StubFile "$Base/Behaviors/LoggingBehavior.cs" @'
using MediatR;
using Microsoft.Extensions.Logging;

namespace NexTraceOne.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Pipeline behavior que loga entrada, saída e duração de cada request MediatR.
/// Log de entrada inclui: tipo do request e dados (sem informação sensível).
/// Log de saída inclui: sucesso/falha e tempo de execução em ms.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // TODO: Implementar logging com Stopwatch e log estruturado
        throw new NotImplementedException();
    }
}
'@

    New-StubFile "$Base/Behaviors/PerformanceBehavior.cs" @'
using MediatR;
using Microsoft.Extensions.Logging;

namespace NexTraceOne.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Pipeline behavior que detecta requests lentos e emite alerta de performance.
/// >500ms → Warning. >2000ms → Error com stack trace.
/// </summary>
public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int WarningThresholdMs = 500;
    private const int ErrorThresholdMs = 2000;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // TODO: Implementar medição com Stopwatch e alertas por threshold
        throw new NotImplementedException();
    }
}
'@

    New-StubFile "$Base/Behaviors/TenantIsolationBehavior.cs" @'
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Pipeline behavior que garante isolamento de tenant em cada request.
/// Verifica se o TenantId está presente no contexto antes de executar
/// qualquer Command ou Query. Requests sem tenant ativo são rejeitados
/// com erro de segurança, exceto para endpoints públicos marcados com IPublicRequest.
/// </summary>
public sealed class TenantIsolationBehavior<TRequest, TResponse>(
    ICurrentTenant currentTenant)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // TODO: Verificar tenant ativo e rejeitar se ausente (exceto IPublicRequest)
        throw new NotImplementedException();
    }
}
'@

    New-StubFile "$Base/Behaviors/TransactionBehavior.cs" @'
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Pipeline behavior que gerencia transações de banco de dados.
/// Abre transação antes do Command Handler, commit ao final.
/// Em caso de exceção, rollback automático.
/// NOTA: Apenas Commands recebem transação. Queries usam AsNoTracking.
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // TODO: Implementar transaction scope com commit/rollback
        throw new NotImplementedException();
    }
}
'@

    # ── Abstractions ─────────────────────────────────────────────────────────
    New-StubFile "$Base/Abstractions/ICurrentUser.cs" @'
namespace NexTraceOne.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Abstração para acesso ao usuário autenticado no contexto da requisição.
/// Implementada pelo projeto Security como HttpContextCurrentUser.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Id único do usuário autenticado.</summary>
    string Id { get; }
    /// <summary>Nome de exibição do usuário.</summary>
    string Name { get; }
    /// <summary>Email do usuário autenticado.</summary>
    string Email { get; }
    /// <summary>Indica se há um usuário autenticado no contexto atual.</summary>
    bool IsAuthenticated { get; }
    /// <summary>Verifica se o usuário possui a permissão especificada.</summary>
    bool HasPermission(string permission);
}
'@

    New-StubFile "$Base/Abstractions/ICurrentTenant.cs" @'
namespace NexTraceOne.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Abstração para acesso ao tenant ativo na requisição atual.
/// Resolvido pelo TenantResolutionMiddleware a partir do JWT, header ou subdomínio.
/// Usado pelo TenantRlsInterceptor para configurar o RLS no PostgreSQL.
/// </summary>
public interface ICurrentTenant
{
    /// <summary>Identificador único do tenant ativo.</summary>
    Guid Id { get; }
    /// <summary>Slug do tenant (ex: "banco-xyz").</summary>
    string Slug { get; }
    /// <summary>Nome de exibição do tenant.</summary>
    string Name { get; }
    /// <summary>Indica se o tenant está ativo e pode realizar operações.</summary>
    bool IsActive { get; }
    /// <summary>Verifica se o tenant possui uma capability de licença específica.</summary>
    bool HasCapability(string capability);
}
'@

    New-StubFile "$Base/Abstractions/IUnitOfWork.cs" @'
namespace NexTraceOne.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Abstração da Unidade de Trabalho para coordenar o commit de mudanças.
/// Implementada pelos DbContexts de cada módulo.
/// REGRA: Não chamar CommitAsync() manualmente — o base handler cuida disso.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Persiste todas as mudanças pendentes. Dispara interceptors.</summary>
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
'@

    New-StubFile "$Base/Abstractions/IDateTimeProvider.cs" @'
namespace NexTraceOne.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Abstração do provedor de data/hora para testes determinísticos.
/// Em produção: DateTimeOffset.UtcNow. Em testes: valor fixo.
/// REGRA: Nunca use DateTime.Now diretamente nos handlers.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>Data/hora atual em UTC.</summary>
    DateTimeOffset UtcNow { get; }
    /// <summary>Data atual em UTC (sem componente de hora).</summary>
    DateOnly UtcToday { get; }
}
'@

    New-StubFile "$Base/Abstractions/IEventBus.cs" @'
namespace NexTraceOne.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Abstração do barramento de eventos para Integration Events.
/// Integration Events cruzam fronteiras de módulo via Outbox Pattern.
/// REGRA: Use para eventos ENTRE módulos. Para eventos DENTRO do módulo, use DomainEvent.
/// </summary>
public interface IEventBus
{
    /// <summary>Publica um Integration Event via Outbox Pattern.</summary>
    Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default) where T : class;
}
'@

    # ── Pagination ───────────────────────────────────────────────────────────
    New-StubFile "$Base/Pagination/PagedList.cs" @'
namespace NexTraceOne.BuildingBlocks.Application.Pagination;

/// <summary>
/// Container padronizado para respostas paginadas em toda a plataforma.
/// Metadados de paginação calculados automaticamente.
/// </summary>
public sealed record PagedList<T>
{
    /// <summary>Itens da página atual.</summary>
    public IReadOnlyList<T> Items { get; init; } = [];
    /// <summary>Total de registros no banco (sem paginação).</summary>
    public int TotalCount { get; init; }
    /// <summary>Página atual (começa em 1).</summary>
    public int Page { get; init; }
    /// <summary>Itens por página.</summary>
    public int PageSize { get; init; }
    /// <summary>Total de páginas calculado automaticamente.</summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    /// <summary>Indica se existe página anterior.</summary>
    public bool HasPrevious => Page > 1;
    /// <summary>Indica se existe próxima página.</summary>
    public bool HasNext => Page < TotalPages;
    /// <summary>Indica se o resultado está vazio.</summary>
    public bool IsEmpty => !Items.Any();

    public static PagedList<T> Create(IReadOnlyList<T> items, int total, int page, int size)
        => new() { Items = items, TotalCount = total, Page = page, PageSize = size };

    public static PagedList<T> Empty(int page = 1, int size = 20)
        => Create([], 0, page, size);
}
'@

    # ── Extensions ───────────────────────────────────────────────────────────
    New-StubFile "$Base/Extensions/ResultExtensions.cs" @'
using Microsoft.AspNetCore.Http;
using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.BuildingBlocks.Application.Extensions;

/// <summary>
/// Extensões para conversão de Result em IResult (Minimal API).
/// Mapeamento: NotFound→404, Validation→422, Conflict→409, Unauthorized→401,
/// Forbidden→403, Security→500, Business→422, Success→200.
/// </summary>
public static class ResultExtensions
{
    /// <summary>Converte Result para IResult com mapeamento HTTP automático.</summary>
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        // TODO: Implementar mapeamento de ErrorType para IResult
        throw new NotImplementedException();
    }

    /// <summary>Converte Result para Created (201) com URL do recurso criado.</summary>
    public static IResult ToCreatedResult<TId>(this Result<TId> result, string routeTemplate)
    {
        // TODO: Implementar mapeamento para Results.Created()
        throw new NotImplementedException();
    }
}
'@

    New-StubFile "$Base/DependencyInjection.cs" @'
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.BuildingBlocks.Application;

/// <summary>
/// Registra serviços do BuildingBlocks.Application no DI.
/// Inclui: Pipeline Behaviors MediatR, DateTimeProvider, validators.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBuildingBlocksApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: Registrar MediatR behaviors, DateTimeProvider, validators
        return services;
    }
}
'@

    Write-OK "BuildingBlocks.Application — stubs completos"
}

# ═══════════════════════════════════════════════════════════════════════════════
# STUBS — BUILDING BLOCKS: INFRASTRUCTURE
# ═══════════════════════════════════════════════════════════════════════════════

function New-BBInfrastructureStubs {
    param([string]$Base)

    New-StubFile "$Base/Persistence/NexTraceDbContextBase.cs" @'
using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Classe base para todos os DbContexts dos módulos.
/// Configura automaticamente: TenantRlsInterceptor (RLS PostgreSQL),
/// AuditInterceptor (CreatedAt/By, UpdatedAt/By),
/// EncryptionInterceptor (AES-256-GCM), OutboxInterceptor (Domain Events → Outbox).
/// </summary>
public abstract class NexTraceDbContextBase(
    DbContextOptions options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock) : DbContext(options)
{
    /// <summary>Assembly com as configurações IEntityTypeConfiguration deste DbContext.</summary>
    protected abstract System.Reflection.Assembly ConfigurationsAssembly { get; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(ConfigurationsAssembly);
        // TODO: ApplyUtcDateTimeConvention, ApplyStronglyTypedIdConventions, ApplyGlobalSoftDeleteFilter
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // TODO: SetAuditFields(), CollectDomainEvents(), WriteToOutboxAsync()
        return await base.SaveChangesAsync(ct);
    }
}
'@

    New-StubFile "$Base/Persistence/RepositoryBase.cs" @'
using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Implementação base de repositório usando EF Core.
/// CRUD completo + Specification evaluation. Módulos só implementam métodos de negócio.
/// </summary>
public abstract class RepositoryBase<TEntity, TId>(DbContext context)
    where TEntity : AggregateRoot<TId>
    where TId     : ITypedId
{
    protected readonly DbSet<TEntity> DbSet = context.Set<TEntity>();

    public async Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct = default)
        => await DbSet.FindAsync([id], ct);

    public async Task<TEntity> GetByIdOrThrowAsync(TId id, CancellationToken ct = default)
    {
        // TODO: Implementar com lançamento de NexTraceNotFoundException
        throw new NotImplementedException();
    }

    public Task<bool> ExistsAsync(TId id, CancellationToken ct = default)
        => DbSet.AnyAsync(e => e.Id.Equals(id), ct);

    public void Add(TEntity entity) => DbSet.Add(entity);
    public void Update(TEntity entity) => DbSet.Update(entity);
    public void Remove(TEntity entity) => DbSet.Remove(entity);
}
'@

    New-StubFile "$Base/Outbox/OutboxMessage.cs" @'
namespace NexTraceOne.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Mensagem Outbox para publicação garantida de Integration Events.
/// Salva na mesma transação do aggregate — garantia de consistência.
/// O OutboxProcessorJob entrega de forma assíncrona após commit.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public Guid TenantId { get; init; }
}
'@

    New-StubFile "$Base/Interceptors/TenantRlsInterceptor.cs" @'
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;

/// <summary>
/// Interceptor EF Core que executa SET app.current_tenant_id antes de cada query.
/// Ativa o Row-Level Security do PostgreSQL para isolamento de dados multi-tenant.
/// </summary>
public sealed class TenantRlsInterceptor : DbCommandInterceptor
{
    // TODO: Implementar ReaderExecutingAsync para SET app.current_tenant_id
}
'@

    New-StubFile "$Base/Interceptors/AuditInterceptor.cs" @'
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;

/// <summary>
/// Interceptor que preenche CreatedAt/By e UpdatedAt/By automaticamente
/// em todas as AuditableEntity antes do SaveChanges.
/// </summary>
public sealed class AuditInterceptor : SaveChangesInterceptor
{
    // TODO: Implementar SavingChangesAsync para preencher campos de auditoria
}
'@

    New-StubFile "$Base/Converters/EncryptedStringConverter.cs" @'
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Converters;

/// <summary>
/// EF Core Value Converter que criptografa campos marcados com [Encrypted] usando AES-256-GCM.
/// Criptografa automaticamente ao salvar e descriptografa ao ler.
/// </summary>
public sealed class EncryptedStringConverter : ValueConverter<string, string>
{
    public EncryptedStringConverter()
        : base(
            plainText => Encrypt(plainText),
            cipherText => Decrypt(cipherText))
    { }

    private static string Encrypt(string plainText)
    {
        // TODO: Implementar AES-256-GCM encryption
        throw new NotImplementedException();
    }

    private static string Decrypt(string cipherText)
    {
        // TODO: Implementar AES-256-GCM decryption
        throw new NotImplementedException();
    }
}
'@

    New-StubFile "$Base/DependencyInjection.cs" @'
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.BuildingBlocks.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura compartilhados: Interceptors, Converters, Outbox.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBuildingBlocksInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: Registrar interceptors, converters, outbox processor
        return services;
    }
}
'@

    Write-OK "BuildingBlocks.Infrastructure — stubs completos"
}

# ═══════════════════════════════════════════════════════════════════════════════
# STUBS — BUILDING BLOCKS: EVENT BUS
# ═══════════════════════════════════════════════════════════════════════════════

function New-BBEventBusStubs {
    param([string]$Base)

    New-StubFile "$Base/Abstractions/IIntegrationEventHandler.cs" @'
namespace NexTraceOne.BuildingBlocks.EventBus.Abstractions;

/// <summary>
/// Handler para Integration Events recebidos de outros módulos.
/// Cada módulo implementa handlers para os eventos que deseja consumir.
/// </summary>
public interface IIntegrationEventHandler<in TEvent> where TEvent : class
{
    Task HandleAsync(TEvent @event, CancellationToken ct = default);
}
'@

    New-StubFile "$Base/InProcess/InProcessEventBus.cs" @'
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.BuildingBlocks.EventBus.InProcess;

/// <summary>
/// Implementação in-process do IEventBus usando MediatR.
/// Usada no modo Modular Monolith — todos os módulos no mesmo processo.
/// Na evolução para microserviços, será substituída por RabbitMQ/Kafka.
/// </summary>
public sealed class InProcessEventBus : IEventBus
{
    public Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default) where T : class
    {
        // TODO: Implementar publicação via MediatR Publish
        throw new NotImplementedException();
    }
}
'@

    New-StubFile "$Base/Outbox/OutboxEventBus.cs" @'
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.BuildingBlocks.EventBus.Outbox;

/// <summary>
/// Implementação do IEventBus que persiste eventos no Outbox.
/// Garante at-least-once delivery mesmo em caso de falha do processo.
/// O OutboxProcessorJob consome e entrega os eventos de forma assíncrona.
/// </summary>
public sealed class OutboxEventBus : IEventBus
{
    public Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default) where T : class
    {
        // TODO: Serializar evento e salvar como OutboxMessage na mesma transação
        throw new NotImplementedException();
    }
}
'@

    New-StubFile "$Base/DependencyInjection.cs" @'
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.BuildingBlocks.EventBus;

/// <summary>Registra implementação do EventBus (InProcess ou Outbox).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBuildingBlocksEventBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: Registrar IEventBus (InProcessEventBus ou OutboxEventBus conforme config)
        return services;
    }
}
'@

    Write-OK "BuildingBlocks.EventBus — stubs completos"
}

# ═══════════════════════════════════════════════════════════════════════════════
# STUBS — BUILDING BLOCKS: OBSERVABILITY
# ═══════════════════════════════════════════════════════════════════════════════

function New-BBObservabilityStubs {
    param([string]$Base)

    New-StubFile "$Base/Logging/SerilogConfiguration.cs" @'
namespace NexTraceOne.BuildingBlocks.Observability.Logging;

/// <summary>
/// Configuração centralizada do Serilog para toda a plataforma.
/// Inclui: enrichers (Environment, MachineName, ThreadId),
/// sinks (Console, File, PostgreSQL), destructuring de objetos de domínio.
/// </summary>
public static class SerilogConfiguration
{
    // TODO: Implementar ConfigureSerilog(IHostBuilder)
}
'@

    New-StubFile "$Base/Tracing/NexTraceActivitySources.cs" @'
using System.Diagnostics;

namespace NexTraceOne.BuildingBlocks.Observability.Tracing;

/// <summary>
/// Activity Sources centralizados para OpenTelemetry.
/// Cada módulo pode criar spans filhos destes sources principais.
/// </summary>
public static class NexTraceActivitySources
{
    /// <summary>Source para operações de command (escrita).</summary>
    public static readonly ActivitySource Commands = new("NexTraceOne.Commands");

    /// <summary>Source para operações de query (leitura).</summary>
    public static readonly ActivitySource Queries = new("NexTraceOne.Queries");

    /// <summary>Source para eventos de domínio e integração.</summary>
    public static readonly ActivitySource Events = new("NexTraceOne.Events");

    /// <summary>Source para chamadas HTTP externas (adapters).</summary>
    public static readonly ActivitySource ExternalHttp = new("NexTraceOne.ExternalHttp");
}
'@

    New-StubFile "$Base/Metrics/NexTraceMeters.cs" @'
using System.Diagnostics.Metrics;

namespace NexTraceOne.BuildingBlocks.Observability.Metrics;

/// <summary>
/// Meters customizados para métricas de negócio da plataforma.
/// </summary>
public static class NexTraceMeters
{
    private static readonly Meter Meter = new("NexTraceOne", "1.0.0");

    /// <summary>Contador de deploys notificados à plataforma.</summary>
    public static readonly Counter<long> DeploymentsNotified = Meter.CreateCounter<long>("nextraceone.deployments.notified");

    /// <summary>Contador de workflows iniciados.</summary>
    public static readonly Counter<long> WorkflowsInitiated = Meter.CreateCounter<long>("nextraceone.workflows.initiated");

    /// <summary>Histograma de duração de cálculo de blast radius (ms).</summary>
    public static readonly Histogram<double> BlastRadiusDuration = Meter.CreateHistogram<double>("nextraceone.blastradius.duration_ms");
}
'@

    New-StubFile "$Base/HealthChecks/NexTraceHealthChecks.cs" @'
namespace NexTraceOne.BuildingBlocks.Observability.HealthChecks;

/// <summary>
/// Health checks customizados: PostgreSQL connectivity, Outbox backlog,
/// License validity, Assembly integrity.
/// </summary>
public static class NexTraceHealthChecks
{
    // TODO: Implementar AddNexTraceHealthChecks(IServiceCollection)
}
'@

    New-StubFile "$Base/DependencyInjection.cs" @'
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.BuildingBlocks.Observability;

/// <summary>Registra Serilog, OpenTelemetry, Metrics e HealthChecks.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBuildingBlocksObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: ConfigureSerilog, AddOpenTelemetry, AddHealthChecks
        return services;
    }
}
'@

    Write-OK "BuildingBlocks.Observability — stubs completos"
}

# ═══════════════════════════════════════════════════════════════════════════════
# STUBS — BUILDING BLOCKS: SECURITY
# ═══════════════════════════════════════════════════════════════════════════════

function New-BBSecurityStubs {
    param([string]$Base)

    New-StubFile "$Base/Encryption/AesGcmEncryptor.cs" @'
namespace NexTraceOne.BuildingBlocks.Security.Encryption;

/// <summary>
/// Implementação de criptografia AES-256-GCM para campos sensíveis.
/// Usado pelo EncryptedStringConverter do EF Core.
/// </summary>
public sealed class AesGcmEncryptor
{
    // TODO: Implementar Encrypt/Decrypt com AES-256-GCM
}
'@

    New-StubFile "$Base/Integrity/AssemblyIntegrityChecker.cs" @'
namespace NexTraceOne.BuildingBlocks.Security.Integrity;

/// <summary>
/// Verifica a integridade dos assemblies no boot da aplicação.
/// Calcula SHA-256 do binário e compara com hash assinado.
/// Se falhar, recusa inicialização. Pipeline: build → obfuscate → AOT → sign.
/// </summary>
public sealed class AssemblyIntegrityChecker
{
    /// <summary>Verifica integridade. Chamado em Program.cs antes de qualquer serviço.</summary>
    public static void VerifyOrThrow()
    {
        // TODO: Implementar verificação com assinatura GPG
        // Bypass via NEXTRACE_SKIP_INTEGRITY=true em desenvolvimento
    }
}
'@

    New-StubFile "$Base/Licensing/HardwareFingerprint.cs" @'
namespace NexTraceOne.BuildingBlocks.Security.Licensing;

/// <summary>
/// Gera impressão digital do hardware: SHA-256(CPU ID | Motherboard UUID | MAC).
/// Usada para binding de licença ao hardware registrado.
/// Em ambientes virtualizados, usa identificadores do hypervisor.
/// </summary>
public sealed class HardwareFingerprint
{
    /// <summary>Gera fingerprint. Retorna hex 64 chars (SHA-256).</summary>
    public static string Generate()
    {
        // TODO: Implementar coleta de CPU ID, Motherboard UUID e MAC
        throw new NotImplementedException();
    }
}
'@

    New-StubFile "$Base/MultiTenancy/TenantResolutionMiddleware.cs" @'
using Microsoft.AspNetCore.Http;

namespace NexTraceOne.BuildingBlocks.Security.MultiTenancy;

/// <summary>
/// Middleware que resolve o tenant ativo para cada requisição HTTP.
/// Prioridade: 1) JWT claim "tenant_id" 2) Header "X-Tenant-Id" 3) Subdomínio.
/// Requisições sem tenant → 401 (exceto endpoints públicos).
/// </summary>
public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // TODO: Implementar resolução de tenant
        await next(context);
    }
}
'@

    New-StubFile "$Base/Authentication/JwtTokenService.cs" @'
namespace NexTraceOne.BuildingBlocks.Security.Authentication;

/// <summary>
/// Serviço de geração e validação de JWT tokens.
/// Suporta: access token (curta duração), refresh token (longa duração).
/// Claims incluídos: sub, email, name, tenant_id, permissions.
/// </summary>
public sealed class JwtTokenService
{
    // TODO: Implementar GenerateAccessToken, GenerateRefreshToken, ValidateToken
}
'@

    New-StubFile "$Base/DependencyInjection.cs" @'
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.BuildingBlocks.Security;

/// <summary>
/// Registra: JWT authentication, TenantResolutionMiddleware, EncryptionService,
/// AssemblyIntegrityChecker, HardwareFingerprint.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBuildingBlocksSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: AddAuthentication, AddAuthorization, TenantMiddleware
        return services;
    }
}
'@

    Write-OK "BuildingBlocks.Security — stubs completos"
}

# ═══════════════════════════════════════════════════════════════════════════════
# STUBS — MÓDULOS (5 camadas por módulo)
# ═══════════════════════════════════════════════════════════════════════════════

function New-ModuleStructure {
    param(
        [string]$ModuleDir,       # Diretório do módulo (ex: src/modules/identity)
        [string]$ModuleName,      # Nome do módulo (ex: "Identity")
        [string[]]$DomainEntities,
        [string[]]$Features
    )

    $domDir   = Join-Path $ModuleDir "NexTraceOne.$ModuleName.Domain"
    $appDir   = Join-Path $ModuleDir "NexTraceOne.$ModuleName.Application"
    $conDir   = Join-Path $ModuleDir "NexTraceOne.$ModuleName.Contracts"
    $infDir   = Join-Path $ModuleDir "NexTraceOne.$ModuleName.Infrastructure"
    $apiDir   = Join-Path $ModuleDir "NexTraceOne.$ModuleName.API"

    # ── Domain Layer ─────────────────────────────────────────────────────────
    New-Directory "$domDir/Entities"
    New-Directory "$domDir/ValueObjects"
    New-Directory "$domDir/Events"
    New-Directory "$domDir/Enums"
    New-Directory "$domDir/Errors"

    foreach ($entity in $DomainEntities) {
        New-StubFile "$domDir/Entities/$entity.cs" @"
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.$ModuleName.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo $ModuleName.
/// TODO: Implementar regras de domínio, invariantes e domain events de $entity.
/// </summary>
public sealed class $entity : AuditableEntity<${entity}Id>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private $entity() { }
}

/// <summary>Identificador fortemente tipado de $entity.</summary>
public sealed record ${entity}Id(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ${entity}Id New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ${entity}Id From(Guid id) => new(id);
}
"@
    }

    New-StubFile "$domDir/Errors/${ModuleName}Errors.cs" @"
using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.$ModuleName.Domain.Errors;

/// <summary>
/// Catálogo centralizado de erros do módulo $ModuleName.
/// Cada erro possui código único para rastreabilidade em logs e documentação.
/// Padrão: {Módulo}.{Entidade}.{Descrição}
/// </summary>
public static class ${ModuleName}Errors
{
    // TODO: Definir erros específicos do módulo
    // Exemplo: public static Error NotFound(string id) => Error.NotFound("${ModuleName}.NotFound", $"...");
}
"@

    # ── Application Layer ────────────────────────────────────────────────────
    New-Directory "$appDir/Features"
    New-Directory "$appDir/Abstractions"
    New-Directory "$appDir/Extensions"

    foreach ($feature in $Features) {
        $featurePath = "$appDir/Features/$feature"
        New-Directory $featurePath
        New-StubFile "$featurePath/$feature.cs" @"
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.$ModuleName.Application.Features.$feature;

/// <summary>
/// Feature: $feature — Módulo: $ModuleName.
/// Estrutura VSA: Command/Query + Handler + Validator + Response em um único arquivo.
/// TODO: Implementar lógica de negócio desta feature.
/// </summary>
public static class $feature
{
    // ── COMMAND / QUERY ───────────────────────────────────────────────────
    // TODO: Implementar record Command ou Query com dados de entrada

    // ── VALIDATOR ─────────────────────────────────────────────────────────
    // TODO: Implementar AbstractValidator<Command> com FluentValidation

    // ── HANDLER ───────────────────────────────────────────────────────────
    // TODO: Implementar handler herdando CommandHandlerBase ou QueryHandlerBase

    // ── RESPONSE ──────────────────────────────────────────────────────────
    // TODO: Implementar record Response com dados de saída
}
"@
    }

    New-StubFile "$appDir/DependencyInjection.cs" @"
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.$ModuleName.Application;

/// <summary>
/// Registra serviços da camada Application do módulo $ModuleName.
/// Inclui: MediatR handlers, FluentValidation validators.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection Add${ModuleName}Application(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: Registrar MediatR handlers e validators deste módulo
        return services;
    }
}
"@

    # ── Contracts Layer ──────────────────────────────────────────────────────
    New-Directory "$conDir/IntegrationEvents"
    New-Directory "$conDir/ServiceInterfaces"
    New-Directory "$conDir/DTOs"

    New-StubFile "$conDir/ServiceInterfaces/I${ModuleName}Module.cs" @"
namespace NexTraceOne.$ModuleName.Contracts.ServiceInterfaces;

/// <summary>
/// Interface pública do módulo $ModuleName.
/// Outros módulos que precisarem de dados deste módulo devem usar
/// este contrato — nunca acessar o DbContext ou repositórios diretamente.
/// </summary>
public interface I${ModuleName}Module
{
    // TODO: Definir operações de consulta que outros módulos podem usar
}
"@

    # ── Infrastructure Layer ─────────────────────────────────────────────────
    New-Directory "$infDir/Persistence/Configurations"
    New-Directory "$infDir/Persistence/Repositories"
    New-Directory "$infDir/Persistence/Migrations"
    New-Directory "$infDir/Adapters"
    New-Directory "$infDir/Services"

    New-StubFile "$infDir/Persistence/${ModuleName}DbContext.cs" @"
using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;

namespace NexTraceOne.$ModuleName.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo $ModuleName.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class ${ModuleName}DbContext(
    DbContextOptions<${ModuleName}DbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock)
{
    // TODO: Adicionar DbSet<T> para cada entidade do módulo

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(${ModuleName}DbContext).Assembly;
}
"@

    New-StubFile "$infDir/DependencyInjection.cs" @"
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.$ModuleName.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura do módulo $ModuleName.
/// Inclui: DbContext, Repositórios, Adapters externos, Quartz Jobs.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection Add${ModuleName}Infrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: Registrar DbContext, repositórios, adapters
        return services;
    }
}
"@

    # ── API Layer ────────────────────────────────────────────────────────────
    New-Directory "$apiDir/Endpoints"
    New-Directory "$apiDir/Middleware"
    New-Directory "$apiDir/Extensions"

    New-StubFile "$apiDir/Endpoints/${ModuleName}EndpointModule.cs" @"
using NexTraceOne.BuildingBlocks.Application.Extensions;

namespace NexTraceOne.$ModuleName.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo $ModuleName.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// </summary>
public sealed class ${ModuleName}EndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/${ModuleName.ToLower()}")
            .WithTags("$ModuleName");

        // TODO: Mapear endpoints de cada feature
    }
}
"@

    New-StubFile "$apiDir/DependencyInjection.cs" @"
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.$ModuleName.API;

/// <summary>
/// Registra serviços específicos da camada API do módulo $ModuleName.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection Add${ModuleName}Module(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Add${ModuleName}Application(configuration);
        services.Add${ModuleName}Infrastructure(configuration);
        return services;
    }
}
"@

    Write-OK "Módulo $ModuleName — 5 camadas criadas (Domain, Application, Contracts, Infrastructure, API)"
}

# ═══════════════════════════════════════════════════════════════════════════════
# STUBS — PLATFORM: API HOST
# ═══════════════════════════════════════════════════════════════════════════════

function New-ApiHostStubs {
    param([string]$Base)

    New-StubFile "$Base/Program.cs" @'
using NexTraceOne.BuildingBlocks.Security.Integrity;

// ═══════════════════════════════════════════════════════════════════════════════
// NEXTRACEONE — Sovereign Change Intelligence Platform
// Host de entrada: NexTraceOne.ApiHost
// Arquitetura v2: Archon Pattern — Modular Monolith + Building Blocks
// ═══════════════════════════════════════════════════════════════════════════════

// [1] Verificação de integridade dos assemblies antes de qualquer inicialização
// AssemblyIntegrityChecker.VerifyOrThrow(); // TODO: Habilitar em produção

var builder = WebApplication.CreateBuilder(args);

// [2] Serilog
// TODO: builder.Host.UseSerilog(...)

// [3] Building Blocks
// TODO: builder.Services.AddBuildingBlocksApplication(builder.Configuration);
// TODO: builder.Services.AddBuildingBlocksInfrastructure(builder.Configuration);
// TODO: builder.Services.AddBuildingBlocksEventBus(builder.Configuration);
// TODO: builder.Services.AddBuildingBlocksObservability(builder.Configuration);
// TODO: builder.Services.AddBuildingBlocksSecurity(builder.Configuration);

// [4] Módulos — cada um registra sua Application + Infrastructure
// TODO: builder.Services.AddIdentityModule(builder.Configuration);
// TODO: builder.Services.AddLicensingModule(builder.Configuration);
// TODO: ... (todos os módulos)

// [5] OpenAPI / Swagger
builder.Services.AddOpenApi();

// [6] Rate Limiting, CORS, Health Checks
// TODO: builder.Services.AddNexTraceRateLimiting(...);

// [7] Quartz.NET (Outbox Processor, SLA Escalation)
// TODO: builder.Services.AddNexTraceJobs(...);

var app = builder.Build();

// ── Middlewares ──
app.UseHttpsRedirection();
// TODO: app.UseMiddleware<TenantResolutionMiddleware>();
// TODO: app.UseNexTraceExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

// ── Endpoints de módulos (assembly scanning) ──
// TODO: app.MapAllModuleEndpoints();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// Health check público
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Version = "2.0.0" }))
   .WithTags("Platform")
   .AllowAnonymous();

app.Run();
'@

    New-StubFile "$Base/appsettings.json" @'
{
  "ConnectionStrings": {
    "NexTraceOne": "Host=localhost;Port=5432;Database=nextraceone;Username=nextraceone;Password=CHANGE_ME"
  },
  "NexTraceOne": {
    "LicenseKey": "",
    "LicenseMode": "Online",
    "IntegrityCheck": false,
    "PerformanceThresholdMs": 500,
    "Tenant": {
      "ResolutionStrategy": "JwtClaim"
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": { "Microsoft": "Warning", "System": "Warning" }
    },
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "logs/nextraceone-.log", "rollingInterval": "Day", "retainedFileCountLimit": 30 } }
    ]
  },
  "OpenTelemetry": {
    "Endpoint": "http://localhost:4317",
    "ServiceName": "nextraceone-apihost"
  }
}
'@

    New-StubFile "$Base/appsettings.Development.json" @'
{
  "NexTraceOne": { "IntegrityCheck": false },
  "Serilog": { "MinimumLevel": { "Default": "Debug" } }
}
'@

    Write-OK "ApiHost — Program.cs e appsettings criados"
}

# ═══════════════════════════════════════════════════════════════════════════════
# STUBS — PLATFORM: BACKGROUND WORKERS
# ═══════════════════════════════════════════════════════════════════════════════

function New-BackgroundWorkersStubs {
    param([string]$Base)

    New-StubFile "$Base/Program.cs" @'
// ═══════════════════════════════════════════════════════════════════════════════
// NEXTRACEONE — Background Workers
// Processa: Outbox Messages, Quartz Jobs, SLA Escalation, Cost Ingestion
// ═══════════════════════════════════════════════════════════════════════════════

var builder = Host.CreateApplicationBuilder(args);

// TODO: Registrar BuildingBlocks
// TODO: Registrar módulos (Infrastructure layer apenas)
// TODO: Registrar Quartz.NET com jobs agendados

var host = builder.Build();
host.Run();
'@

    New-StubFile "$Base/Jobs/OutboxProcessorJob.cs" @'
namespace NexTraceOne.BackgroundWorkers.Jobs;

/// <summary>
/// Job Quartz.NET que processa mensagens pendentes na tabela Outbox.
/// Executa a cada 5 segundos, lê batch de mensagens não processadas,
/// deserializa e entrega aos handlers de Integration Event correspondentes.
/// Garante at-least-once delivery com retry exponencial.
/// </summary>
public sealed class OutboxProcessorJob
{
    // TODO: Implementar IJob com lógica de processamento do Outbox
}
'@

    Write-OK "BackgroundWorkers — stubs criados"
}

# ═══════════════════════════════════════════════════════════════════════════════
# STUBS — CLI
# ═══════════════════════════════════════════════════════════════════════════════

function New-CliStubs {
    param([string]$Base)

    New-StubFile "$Base/Program.cs" @'
using System.CommandLine;
using Spectre.Console;

// ═══════════════════════════════════════════════════════════════════════════════
// NEX — NexTraceOne Command Line Interface
// Uso: nex <command> [options]
// Consome apenas a camada Contracts de cada módulo (consumidor externo)
// ═══════════════════════════════════════════════════════════════════════════════

AnsiConsole.Write(new FigletText("NexTraceOne CLI").Color(Color.Cyan1));

var rootCommand = new RootCommand("NexTraceOne CLI — Sovereign Change Intelligence Platform");

// TODO: nex validate   — valida contrato OpenAPI com ruleset
// TODO: nex release    — gerencia releases (status, health, history)
// TODO: nex promotion  — controla promoção entre ambientes
// TODO: nex approval   — submete e consulta aprovações de workflow
// TODO: nex impact     — analisa blast radius de uma mudança
// TODO: nex tests      — gera cenários de teste em Robot Framework
// TODO: nex catalog    — consulta catálogo de APIs e serviços

return await rootCommand.InvokeAsync(args);
'@

    Write-OK "CLI — stub criado"
}

# ═══════════════════════════════════════════════════════════════════════════════
# ARQUIVOS DE SUPORTE
# ═══════════════════════════════════════════════════════════════════════════════

function New-SupportFiles {

    New-StubFile "$SolutionRoot/.editorconfig" @'
root = true

[*]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

[*.{cs,vb}]
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = true
csharp_style_namespace_declarations = file_scoped:warning
csharp_style_var_for_built_in_types = false:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion

dotnet_naming_rule.private_fields_underscore.severity = warning
dotnet_naming_rule.private_fields_underscore.symbols = private_fields
dotnet_naming_rule.private_fields_underscore.style = underscore_prefix
dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private
dotnet_naming_style.underscore_prefix.required_prefix = _
dotnet_naming_style.underscore_prefix.capitalization = camel_case

[*.{csproj,props,targets}]
indent_size = 2

[*.{json,yml,yaml}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false
'@

    New-StubFile "$SolutionRoot/.gitignore" @'
# .NET
bin/
obj/
*.user
*.suo
.vs/
.vscode/
.idea/
TestResults/

# NexTraceOne
logs/
build/artifacts/
*.license
hardware.fingerprint
sign.pfx

# Docker
.env.local
'@

    New-StubFile "$SolutionRoot/global.json" @'
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestMinor",
    "allowPrerelease": true
  }
}
'@

    New-StubFile "$SolutionRoot/Directory.Build.props" @'
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>1591</NoWarn>
    <LangVersion>latest</LangVersion>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
  <PropertyGroup>
    <Authors>NexTraceOne</Authors>
    <Company>NexTraceOne</Company>
    <Product>NexTraceOne Platform</Product>
    <Copyright>Copyright © 2026 NexTraceOne. Todos os direitos reservados.</Copyright>
    <VersionPrefix>0.1.0</VersionPrefix>
  </PropertyGroup>
</Project>
'@

    New-StubFile "$SolutionRoot/Directory.Packages.props" @'
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup Label="ASP.NET Core e EF Core">
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.0" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
  </ItemGroup>
  <ItemGroup Label="CQRS e Validação">
    <PackageVersion Include="MediatR" Version="12.4.1" />
    <PackageVersion Include="FluentValidation" Version="11.11.0" />
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="11.11.0" />
  </ItemGroup>
  <ItemGroup Label="Observabilidade">
    <PackageVersion Include="Serilog" Version="4.2.0" />
    <PackageVersion Include="Serilog.AspNetCore" Version="9.0.0" />
    <PackageVersion Include="Serilog.Sinks.Console" Version="6.0.0" />
    <PackageVersion Include="Serilog.Sinks.File" Version="5.0.0" />
    <PackageVersion Include="Serilog.Sinks.PostgreSQL" Version="4.1.0" />
    <PackageVersion Include="Serilog.Enrichers.Environment" Version="3.0.1" />
    <PackageVersion Include="OpenTelemetry" Version="1.11.2" />
    <PackageVersion Include="OpenTelemetry.Api" Version="1.11.2" />
    <PackageVersion Include="OpenTelemetry.Extensions.Hosting" Version="1.11.2" />
    <PackageVersion Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.11.2" />
  </ItemGroup>
  <ItemGroup Label="Scheduling e Background">
    <PackageVersion Include="Quartz" Version="3.13.1" />
    <PackageVersion Include="Quartz.Extensions.Hosting" Version="3.13.1" />
  </ItemGroup>
  <ItemGroup Label="Utilitários">
    <PackageVersion Include="Ardalis.GuardClauses" Version="5.0.0" />
    <PackageVersion Include="Mapster" Version="7.4.1" />
    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
  </ItemGroup>
  <ItemGroup Label="CLI">
    <PackageVersion Include="System.CommandLine" Version="2.0.0-beta4.22272.1" />
    <PackageVersion Include="Spectre.Console" Version="0.50.0" />
    <PackageVersion Include="Spectre.Console.Cli" Version="0.50.0" />
  </ItemGroup>
  <ItemGroup Label="Testes">
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageVersion Include="FluentAssertions" Version="7.1.0" />
    <PackageVersion Include="NSubstitute" Version="5.3.0" />
    <PackageVersion Include="Testcontainers.PostgreSql" Version="4.3.0" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
    <PackageVersion Include="Bogus" Version="35.6.1" />
    <PackageVersion Include="Respawn" Version="6.2.1" />
  </ItemGroup>
</Project>
'@

    New-StubFile "$BuildDir/README.md" @'
# NexTraceOne — Pipeline de Build e Proteção de IP

## Pipeline oficial

```
1. dotnet build --configuration Release
2. dotnet-reactor --config obfuscate.xml       # Obfuscação IL
3. dotnet publish --runtime linux-x64 (AOT)    # Compilação nativa
4. sha256sum + gpg --sign                       # Assinatura de integridade
```

## Ambientes: linux-x64 (principal), win-x64 (IIS), linux-arm64 (futuro)
'@

    New-StubFile "$DocsDir/ARCHITECTURE.md" @'
# NexTraceOne — Arquitetura v2 (Archon Pattern)

## Visão Geral

Sovereign Change Intelligence Platform — enterprise, self-hosted, soberana e auditável.

## Estrutura

### Building Blocks (substituem SharedKernel monolítico)
6 projetos granulares: Domain, Application, Infrastructure, EventBus, Observability, Security.
Cada módulo referencia apenas os building blocks que precisa.

### Módulos de Negócio
Cada módulo possui 5 projetos: Domain, Application, Contracts, Infrastructure, API.
Prontos para extração como serviço independente — zero refactoring de boundaries.

### Platform
- **ApiHost** — compõe todos os módulos (modular monolith)
- **BackgroundWorkers** — Outbox processor, jobs agendados
- **CLI** — ferramenta `nex` para operações administrativas

## Regra de Dependência

```
Domain ← Application ← Infrastructure
              ↑              ↓
         Contracts ←── API
```

Nenhum módulo referencia diretamente outro. Comunicação cross-módulo via Integration Events (Outbox).

## Futura Extração para Microserviços

1. Cada módulo já tem seu próprio projeto API com endpoints isolados
2. Cada módulo já tem seu próprio DbContext isolado
3. O ApiHost compõe tudo hoje; amanhã cada API pode virar seu próprio host
4. Substituir InProcessEventBus por RabbitMQ/Kafka e pronto
'@

    New-StubFile "$DocsDir/ROADMAP.md" @'
# NexTraceOne — Roadmap MVP1

## Visão Geral
Metodologia: 1 módulo por vez · 1 camada por vez · 1 aggregate por vez.

## FASE 1 — Fundação (Sem 1–4)
- Sem 1–2: Building Blocks (Domain, Application, Infrastructure, EventBus, Observability, Security)
- Sem 3–4: Módulo Identity (User, Role, Permission, Session, TenantMembership)

## FASE 2 — Catálogo e Contratos (Sem 5–8)
- Sem 5–6: Licensing (License, HardwareBinding, Capabilities)
- Sem 7–8: EngineeringGraph + DeveloperPortal + Contracts

## FASE 3 — Change Intelligence (Sem 9–12) — CORE
- ChangeIntelligence (Release, ChangeEvent, BlastRadius, Score)
- RulesetGovernance (Ruleset, LintExecution, Findings)

## FASE 4 — Workflow e Promoção (Sem 13–16)
- Workflow (Templates, Instances, Approvals, EvidencePack)
- Promotion + Gates

## FASE 5 — Audit e IA (Sem 17–20)
- Audit (AuditEvent, HashChain, Retention)
- AiOrchestration + ExternalAi

## FASE 6 — Hardening e Entrega (Sem 21–24)
- E2E Tests, Performance, Docker Compose, Onboarding

## Regras
1. Um módulo por vez
2. Uma camada por vez: Domain → Application → Infrastructure → API
3. Um aggregate por vez
4. Aprovação entre fases
5. Código em inglês, comentários em português
6. XML docs em PT-BR em toda classe/método público
'@

    Write-OK "Arquivos de suporte criados"
}

# ═══════════════════════════════════════════════════════════════════════════════
# DEFINIÇÃO DOS MÓDULOS
# ═══════════════════════════════════════════════════════════════════════════════

$Script:Modules = @(
    @{
        Name     = "Identity"
        Entities = @("User","Role","Permission","Session","TenantMembership")
        Features = @("FederatedLogin","LocalLogin","RefreshToken","CreateUser","AssignRole","ListTenantUsers","GetUserProfile","RevokeSession")
    },
    @{
        Name     = "Licensing"
        Entities = @("License","LicenseCapability","HardwareBinding","UsageQuota","LicenseActivation")
        Features = @("ActivateLicense","VerifyLicenseOnStartup","CheckCapability","TrackUsageMetric","AlertLicenseThreshold","GetLicenseStatus")
    },
    @{
        Name     = "EngineeringGraph"
        Entities = @("ApiAsset","ServiceAsset","ConsumerAsset","ConsumerRelationship","DiscoverySource")
        Features = @("RegisterApiAsset","RegisterServiceAsset","MapConsumerRelationship","InferDependencyFromOtel","ImportFromBackstage","ImportFromKongGateway","ValidateDiscoveredDependency","GetAssetGraph","GetAssetDetail","SearchAssets","UpdateAssetMetadata","DecommissionAsset")
    },
    @{
        Name     = "DeveloperPortal"
        Entities = @()
        Features = @("SearchCatalog","GetApiDetail","GetMyApis","GetApisIConsume","RenderOpenApiContract","GetApiConsumers","GetAssetTimeline","GetApiHealth")
    },
    @{
        Name     = "Contracts"
        Entities = @("ContractVersion","ContractDiff","ContractLock","OpenApiSchema")
        Features = @("ImportContract","CreateContractVersion","ComputeSemanticDiff","ClassifyBreakingChange","LockContractVersion","SuggestSemanticVersion","ExportContract","GetContractHistory","ValidateContractIntegrity")
    },
    @{
        Name     = "ChangeIntelligence"
        Entities = @("Release","ChangeEvent","DeploymentState","BlastRadiusReport","ChangeIntelligenceScore")
        Features = @("NotifyDeployment","ClassifyChangeLevel","CalculateBlastRadius","ComputeChangeScore","AttachWorkItemContext","UpdateDeploymentState","RegisterRollback","SyncJiraWorkItems","GetRelease","GetReleaseHistory","GetBlastRadiusReport","GetChangeScore","ListReleases")
    },
    @{
        Name     = "RulesetGovernance"
        Entities = @("Ruleset","RulesetBinding","LintExecution","LintFinding","RulesetScore")
        Features = @("UploadRuleset","BindRulesetToAssetType","ExecuteLintForRelease","ComputeRulesetScore","InstallDefaultRulesets","GetRulesetFindings","GetRulesetScore","ListRulesets","ArchiveRuleset")
    },
    @{
        Name     = "Workflow"
        Entities = @("WorkflowTemplate","WorkflowInstance","WorkflowStage","ApprovalDecision","EvidencePack","SlaPolicy")
        Features = @("CreateWorkflowTemplate","InitiateWorkflow","ApproveStage","RejectWorkflow","RequestChanges","AddObservation","GenerateEvidencePack","ExportEvidencePackPdf","EscalateSlaViolation","GetWorkflowStatus","ListPendingApprovals","GetEvidencePack")
    },
    @{
        Name     = "Promotion"
        Entities = @("PromotionRequest","Environment","PromotionGate","GateEvaluation")
        Features = @("ConfigureEnvironment","CreatePromotionRequest","EvaluatePromotionGates","ApprovePromotion","BlockPromotion","GetPromotionStatus","ListPromotionRequests","GetGateEvaluation","OverrideGateWithJustification")
    },
    @{
        Name     = "RuntimeIntelligence"
        Entities = @("RuntimeSnapshot","DriftFinding","ObservabilityProfile","RuntimeBaseline")
        Features = @("IngestRuntimeSnapshot","DetectRuntimeDrift","ComputeObservabilityDebt","GetRuntimeHealth","GetDriftFindings","GetReleaseHealthTimeline","CompareReleaseRuntime","GetObservabilityScore")
    },
    @{
        Name     = "CostIntelligence"
        Entities = @("CostSnapshot","CostAttribution","CostTrend","ServiceCostProfile")
        Features = @("IngestCostSnapshot","AttributeCostToService","ComputeCostTrend","GetCostByRelease","GetCostByRoute","GetCostDelta","GetCostReport","AlertCostAnomaly")
    },
    @{
        Name     = "AiOrchestration"
        Entities = @("AiConversation","AiContext","GeneratedTestArtifact","KnowledgeCaptureEntry")
        Features = @("ClassifyChangeWithAI","SuggestSemanticVersionWithAI","SummarizeReleaseForApproval","GenerateTestScenarios","GenerateRobotFrameworkDraft","AskCatalogQuestion","GetAiConversationHistory","ValidateKnowledgeCapture")
    },
    @{
        Name     = "ExternalAi"
        Entities = @("ExternalAiConsultation","ExternalAiProvider","ExternalAiPolicy","KnowledgeCapture")
        Features = @("QueryExternalAISimple","QueryExternalAIAdvanced","CaptureExternalAIResponse","ReuseKnowledgeCapture","ConfigureExternalAIPolicy","GetExternalAIUsage","ListKnowledgeCaptures","ApproveKnowledgeCapture")
    },
    @{
        Name     = "Audit"
        Entities = @("AuditEvent","AuditChainLink","RetentionPolicy")
        Features = @("RecordAuditEvent","SearchAuditLog","VerifyChainIntegrity","ExportAuditReport","ConfigureRetention","GetAuditTrail","GetComplianceReport")
    }
)

$Script:ModuleNames = $Modules | ForEach-Object { $_.Name }

# ═══════════════════════════════════════════════════════════════════════════════
# ═══════════════════════════════════════════════════════════════════════════════
#                         EXECUÇÃO PRINCIPAL
# ═══════════════════════════════════════════════════════════════════════════════
# ═══════════════════════════════════════════════════════════════════════════════

Write-Header "NexTraceOne v2 — Scaffolding Archon Pattern"
Write-Info "Raiz: $SolutionRoot"
Write-Info "SDK: $(& dotnet --version 2>$null)"
Write-Info "Building Blocks: 6 | Módulos: $($Modules.Count) × 5 camadas | Platform: 3"

# ── [1] Estrutura de diretórios ──────────────────────────────────────────────
Write-Step "Criando estrutura de diretórios"
@($SolutionRoot, $SrcDir, $BBDir, $ModulesDir, $PlatformDir,
  $TestsDir, $TestBBDir, $TestModDir, $TestPlatDir,
  $ToolsDir, $BuildDir, $DocsDir) | ForEach-Object { New-Directory $_ }
Write-OK "Diretórios criados"

# ── [2] Solução ──────────────────────────────────────────────────────────────
Write-Step "Criando solução $SolutionName.sln"
Invoke-Dotnet @("new","sln","--name",$SolutionName,"--output",$SolutionRoot)
Write-OK "Solução criada"

# ── [3] Building Blocks ──────────────────────────────────────────────────────
Write-Header "Building Blocks (6 projetos)"

$bbProjects = @(
    @{ Name = "Domain";          CsprojFn = "Get-BBDomainCsproj";          StubFn = "New-BBDomainStubs" }
    @{ Name = "Application";     CsprojFn = "Get-BBApplicationCsproj";     StubFn = "New-BBApplicationStubs" }
    @{ Name = "Infrastructure";  CsprojFn = "Get-BBInfrastructureCsproj";  StubFn = "New-BBInfrastructureStubs" }
    @{ Name = "EventBus";        CsprojFn = "Get-BBEventBusCsproj";        StubFn = "New-BBEventBusStubs" }
    @{ Name = "Observability";   CsprojFn = "Get-BBObservabilityCsproj";   StubFn = "New-BBObservabilityStubs" }
    @{ Name = "Security";        CsprojFn = "Get-BBSecurityCsproj";        StubFn = "New-BBSecurityStubs" }
)

foreach ($bb in $bbProjects) {
    $projName = "$BB.$($bb.Name)"
    $projDir  = Join-Path $BBDir $projName
    New-Directory $projDir

    Write-Step "Building Block: $($bb.Name)"

    # Gerar .csproj
    $csprojContent = & $bb.CsprojFn
    Set-Content "$projDir/$projName.csproj" $csprojContent -Encoding UTF8

    # Gerar stubs
    & $bb.StubFn -Base $projDir

    # Adicionar à solução
    Invoke-Dotnet @("sln","add","$projDir/$projName.csproj","--solution-folder","src/building-blocks")

    # Projeto de teste
    $testName = "$projName.Tests"
    $testDir  = Join-Path $TestBBDir $testName
    New-Directory $testDir

    $relRef = "..\..\..\..\src\building-blocks\$projName\$projName.csproj"
    Set-Content "$testDir/$testName.csproj" (Get-TestCsproj @($relRef)) -Encoding UTF8

    New-StubFile "$testDir/PlaceholderTests.cs" @"
namespace $projName.Tests;

/// <summary>Testes unitários do building block $($bb.Name).</summary>
public sealed class PlaceholderTests
{
    [Fact]
    public void Placeholder_Should_Pass() => Assert.True(true);
}
"@

    Invoke-Dotnet @("sln","add","$testDir/$testName.csproj","--solution-folder","tests/building-blocks")
}

# ── [4] Módulos ──────────────────────────────────────────────────────────────
Write-Header "Módulos de Negócio ($($Modules.Count) × 5 camadas)"

foreach ($mod in $Modules) {
    $modName   = $mod.Name
    $modLower  = $modName.ToLower()
    $modDir    = Join-Path $ModulesDir $modLower
    $slnFolder = "src/modules/$modLower"

    Write-Step "Módulo: $modName"
    New-Directory $modDir

    # Gerar .csproj de cada camada
    $layers = @(
        @{ Suffix = "Domain";         Fn = "Get-ModuleDomainCsproj" }
        @{ Suffix = "Application";    Fn = "Get-ModuleApplicationCsproj" }
        @{ Suffix = "Contracts";      Fn = "Get-ModuleContractsCsproj" }
        @{ Suffix = "Infrastructure"; Fn = "Get-ModuleInfrastructureCsproj" }
        @{ Suffix = "API";            Fn = "Get-ModuleApiCsproj" }
    )

    foreach ($layer in $layers) {
        $projName = "NexTraceOne.$modName.$($layer.Suffix)"
        $projDir  = Join-Path $modDir $projName
        New-Directory $projDir

        $content = & $layer.Fn -ModuleName $modName
        Set-Content "$projDir/$projName.csproj" $content -Encoding UTF8
        Invoke-Dotnet @("sln","add","$projDir/$projName.csproj","--solution-folder",$slnFolder)
    }

    # Gerar stubs do módulo
    New-ModuleStructure -ModuleDir $modDir -ModuleName $modName `
        -DomainEntities $mod.Entities -Features $mod.Features

    # Projeto de teste do módulo
    $testName = "NexTraceOne.$modName.Tests"
    $testModFolder = Join-Path $TestModDir $modLower
    $testDir = Join-Path $testModFolder $testName
    New-Directory $testDir

    $domRef  = "..\..\..\..\src\modules\$modLower\NexTraceOne.$modName.Domain\NexTraceOne.$modName.Domain.csproj"
    $appRef  = "..\..\..\..\src\modules\$modLower\NexTraceOne.$modName.Application\NexTraceOne.$modName.Application.csproj"
    $infRef  = "..\..\..\..\src\modules\$modLower\NexTraceOne.$modName.Infrastructure\NexTraceOne.$modName.Infrastructure.csproj"
    Set-Content "$testDir/$testName.csproj" (Get-TestCsproj @($domRef, $appRef, $infRef)) -Encoding UTF8

    New-StubFile "$testDir/PlaceholderTests.cs" @"
namespace NexTraceOne.$modName.Tests;

/// <summary>Testes do módulo $modName.</summary>
public sealed class PlaceholderTests
{
    [Fact]
    public void Placeholder_Should_Pass() => Assert.True(true);
}
"@

    Invoke-Dotnet @("sln","add","$testDir/$testName.csproj","--solution-folder","tests/modules/$modLower")
}

# ── [5] Platform ─────────────────────────────────────────────────────────────
Write-Header "Platform (ApiHost, BackgroundWorkers, CLI)"

# ApiHost
Write-Step "ApiHost"
$apiHostDir = Join-Path $PlatformDir "NexTraceOne.ApiHost"
New-Directory $apiHostDir
Set-Content "$apiHostDir/NexTraceOne.ApiHost.csproj" (Get-ApiHostCsproj $ModuleNames) -Encoding UTF8
New-ApiHostStubs $apiHostDir
Invoke-Dotnet @("sln","add","$apiHostDir/NexTraceOne.ApiHost.csproj","--solution-folder","src/platform")

# BackgroundWorkers
Write-Step "BackgroundWorkers"
$workersDir = Join-Path $PlatformDir "NexTraceOne.BackgroundWorkers"
New-Directory $workersDir
Set-Content "$workersDir/NexTraceOne.BackgroundWorkers.csproj" (Get-BackgroundWorkersCsproj $ModuleNames) -Encoding UTF8
New-BackgroundWorkersStubs $workersDir
Invoke-Dotnet @("sln","add","$workersDir/NexTraceOne.BackgroundWorkers.csproj","--solution-folder","src/platform")

# CLI
Write-Step "CLI"
$cliDir = Join-Path $ToolsDir "NexTraceOne.CLI"
New-Directory $cliDir
Set-Content "$cliDir/NexTraceOne.CLI.csproj" (Get-CliCsproj $ModuleNames) -Encoding UTF8
New-CliStubs $cliDir
Invoke-Dotnet @("sln","add","$cliDir/NexTraceOne.CLI.csproj","--solution-folder","tools")

# Platform Tests
Write-Step "Testes de plataforma (Integration + E2E)"
$intTestDir = Join-Path $TestPlatDir "NexTraceOne.IntegrationTests"
New-Directory $intTestDir
$apiHostRef = "..\..\..\..\src\platform\NexTraceOne.ApiHost\NexTraceOne.ApiHost.csproj"
Set-Content "$intTestDir/NexTraceOne.IntegrationTests.csproj" (Get-TestCsproj @($apiHostRef)) -Encoding UTF8
New-StubFile "$intTestDir/PlaceholderTests.cs" @'
namespace NexTraceOne.IntegrationTests;

/// <summary>Testes de integração contra banco de dados real (Testcontainers).</summary>
public sealed class PlaceholderTests
{
    [Fact]
    public void Placeholder_Should_Pass() => Assert.True(true);
}
'@
Invoke-Dotnet @("sln","add","$intTestDir/NexTraceOne.IntegrationTests.csproj","--solution-folder","tests/platform")

$e2eDir = Join-Path $TestPlatDir "NexTraceOne.E2E.Tests"
New-Directory $e2eDir
Set-Content "$e2eDir/NexTraceOne.E2E.Tests.csproj" (Get-TestCsproj @($apiHostRef)) -Encoding UTF8
New-StubFile "$e2eDir/PlaceholderTests.cs" @'
namespace NexTraceOne.E2E.Tests;

/// <summary>Testes End-to-End.</summary>
public sealed class PlaceholderTests
{
    [Fact]
    public void Placeholder_Should_Pass() => Assert.True(true);
}
'@
Invoke-Dotnet @("sln","add","$e2eDir/NexTraceOne.E2E.Tests.csproj","--solution-folder","tests/platform")

# ── [6] Arquivos de suporte ──────────────────────────────────────────────────
Write-Step "Criando arquivos de suporte"
New-SupportFiles

# ── [7] Restore ──────────────────────────────────────────────────────────────
if (-not $SkipDotnetRestore) {
    Write-Step "Restaurando pacotes NuGet"
    Invoke-Dotnet @("restore","$SolutionRoot/$SolutionName.sln")
    Write-OK "Pacotes restaurados"
} else {
    Write-Info "Restore pulado (--SkipDotnetRestore)"
}

# ═══════════════════════════════════════════════════════════════════════════════
# SUMÁRIO FINAL
# ═══════════════════════════════════════════════════════════════════════════════

$bbCount      = 6
$bbTestCount  = 6
$modCount     = $Modules.Count
$modProjCount = $modCount * 5
$modTestCount = $modCount
$platCount    = 3
$platTestCount = 2
$totalProjects = $bbCount + $bbTestCount + $modProjCount + $modTestCount + $platCount + $platTestCount

Write-Header "Scaffolding Concluído com Sucesso"

Write-Host @"

  SOLUÇÃO:     $SolutionName.sln (Archon Pattern v2)
  LOCALIZAÇÃO: $SolutionRoot

  PROJETOS CRIADOS: $totalProjects
  ─────────────────────────────────────────────────────────────
  ┌ src/
  │  ├── building-blocks/ ($bbCount projetos)
  │  │    ├── NexTraceOne.BuildingBlocks.Domain
  │  │    ├── NexTraceOne.BuildingBlocks.Application
  │  │    ├── NexTraceOne.BuildingBlocks.Infrastructure
  │  │    ├── NexTraceOne.BuildingBlocks.EventBus
  │  │    ├── NexTraceOne.BuildingBlocks.Observability
  │  │    └── NexTraceOne.BuildingBlocks.Security
  │  ├── modules/ ($modCount módulos × 5 camadas = $modProjCount projetos)
  │  │    ├── identity/          (Domain, Application, Contracts, Infrastructure, API)
  │  │    ├── licensing/
  │  │    ├── engineeringgraph/
  │  │    ├── developerportal/
  │  │    ├── contracts/
  │  │    ├── changeintelligence/ ← CORE
  │  │    ├── rulesetgovernance/
  │  │    ├── workflow/
  │  │    ├── promotion/
  │  │    ├── runtimeintelligence/
  │  │    ├── costintelligence/
  │  │    ├── aiorchestration/
  │  │    ├── externalai/
  │  │    └── audit/
  │  └── platform/ ($platCount projetos)
  │       ├── NexTraceOne.ApiHost           (compõe todos os módulos)
  │       ├── NexTraceOne.BackgroundWorkers (Outbox, Jobs)
  │       └── (CLI em tools/)
  ├ tools/
  │  └── NexTraceOne.CLI                    (consome apenas Contracts)
  └ tests/ ($($bbTestCount + $modTestCount + $platTestCount) projetos)
       ├── building-blocks/ ($bbTestCount)
       ├── modules/         ($modTestCount)
       └── platform/        ($platTestCount — Integration + E2E)

  PRÓXIMOS PASSOS:
  ─────────────────────────────────────────────────────────────
  1. Abrir $SolutionName.sln no Visual Studio ou Rider
  2. Implementar BuildingBlocks.Domain (Entity, ValueObject, Result)
  3. Seguir roadmap: docs/ROADMAP.md
  4. Fase 1: Building Blocks → Identity → Licensing

  Arquitetura: docs/ARCHITECTURE.md
  Roadmap:     docs/ROADMAP.md

"@ -ForegroundColor Green

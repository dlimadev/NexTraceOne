# Guia de Implementação de Validators FluentValidation — NexTraceOne

## Índice

1. [Visão geral](#visão-geral)
2. [Template — Command com Validator](#1-template--command-com-validator)
3. [Template — Query com Validator](#2-template--query-com-validator-parâmetros-de-pesquisa--filtro)
4. [Regras de validação comuns](#3-regras-de-validação-comuns)
5. [Template de teste unitário](#4-template-de-teste-unitário-do-validator)
6. [Prioridade de implementação](#5-prioridade-de-implementação-de-validators)
7. [Estrutura de ficheiros](#6-comandos-para-gerar-o-scaffold)
8. [Checklist de revisão de código](#7-checklist-de-revisão-de-código)

---

## Visão geral

Todos os `Command` e `Query` que recebam parâmetros de entrada devem ter um `Validator`.
O validator é descoberto automaticamente pelo `MediatR` pipeline através do
`ValidationBehavior` registado no DI.

Regras que **dispensam** validator:
- Comandos sem propriedades (ex.: `MarkAllNotificationsRead.Command` vazio)
- Comandos com apenas um parâmetro que vem de identidade autenticada (ex.: `Logout.Command(UserId)` preenchido pelo middleware)
- Seeds e bootstraps executados em startup com parâmetros internos validados por código

---

## 1. Template — Command com Validator

```csharp
using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.{Module}.Application.Abstractions;
using NexTraceOne.{Module}.Domain.Entities;

namespace NexTraceOne.{Module}.Application.Features.{FeatureName};

/// <summary>
/// Feature: {FeatureName} — breve descrição do que este command faz.
/// </summary>
public static class {FeatureName}
{
    /// <summary>Dados de entrada do command.</summary>
    public sealed record Command(
        string ResourceId,
        string Name,
        string? Description,
        int MaxItems) : ICommand<Response>;

    /// <summary>Valida a entrada do command.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            // Strings obrigatórias
            RuleFor(x => x.ResourceId)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(200);

            // Strings opcionais — validar somente quando não nulas
            RuleFor(x => x.Description)
                .MaximumLength(2000)
                .When(x => x.Description is not null);

            // Numéricos
            RuleFor(x => x.MaxItems)
                .InclusiveBetween(1, 1000);
        }
    }

    /// <summary>Response retornada pelo command.</summary>
    public sealed record Response(string ResourceId);

    /// <summary>Handler do command.</summary>
    public sealed class Handler(
        I{Module}Repository repository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            // guard: parsear IDs string → strongly-typed
            if (!Guid.TryParse(request.ResourceId, out var resourceGuid))
                return Error.Validation("INVALID_RESOURCE_ID",
                    "Resource ID '{0}' is not a valid GUID.", request.ResourceId);

            // ... lógica de domínio ...

            await unitOfWork.CommitAsync(cancellationToken);
            return new Response(ResourceId: resourceGuid.ToString());
        }
    }
}
```

---

## 2. Template — Query com Validator (parâmetros de pesquisa / filtro)

Queries que aceitam filtros opcionais ou paginação devem validar os parâmetros.

```csharp
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.{Module}.Application.Features.List{Resource}s;

public static class List{Resource}s
{
    /// <summary>Parâmetros de pesquisa/filtro/paginação.</summary>
    public sealed record Query(
        string? Status,
        string? OwnerId,
        int Page,
        int PageSize) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        private static readonly string[] ValidStatuses = ["Active", "Inactive", "Archived"];

        public Validator()
        {
            // Paginação — sempre validar
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100);

            // Filtros opcionais — validar somente quando presentes
            RuleFor(x => x.Status)
                .MaximumLength(50)
                .Must(s => ValidStatuses.Contains(s))
                .WithMessage("Status must be one of: Active, Inactive, Archived.")
                .When(x => x.Status is not null);

            RuleFor(x => x.OwnerId)
                .MaximumLength(50)
                .When(x => x.OwnerId is not null);
        }
    }

    public sealed record Response(IReadOnlyList<{Resource}Dto> Items, int TotalCount);

    // Handler omitido — segue padrão normal
}
```

---

## 3. Regras de validação comuns

### 3.1 String ID (GUID como string)

```csharp
// No validator: comprimento apenas (a validade do GUID é verificada no handler)
RuleFor(x => x.ResourceId).NotEmpty().MaximumLength(50);

// No handler: parse e guard clause
if (!Guid.TryParse(request.ResourceId, out var guid))
    return Error.Validation("INVALID_{RESOURCE}_ID",
        "{Resource} ID '{0}' is not a valid GUID.", request.ResourceId);
```

### 3.2 Enum como string

```csharp
// No validator: aceitar qualquer string curta (parsing no handler)
RuleFor(x => x.Status).NotEmpty().MaximumLength(50);

// No handler: parse e guard
if (!Enum.TryParse<{EnumType}>(request.Status, ignoreCase: true, out var status))
    return Error.Validation("INVALID_STATUS",
        "Status '{0}' is not valid. Use {values}.", request.Status);
```

### 3.3 Coleções

```csharp
// Coleção obrigatória com pelo menos 1 item
RuleFor(x => x.Tags)
    .NotEmpty()
    .Must(t => t.Count <= 20).WithMessage("Maximum 20 tags allowed.");

// Regras para cada elemento
RuleForEach(x => x.Tags)
    .NotEmpty()
    .MaximumLength(100);
```

### 3.4 Regras condicionais

```csharp
// Apenas quando outro campo tem um valor
RuleFor(x => x.ExpiresAt)
    .GreaterThan(DateTimeOffset.UtcNow)
    .When(x => x.ExpiresAt.HasValue);

// Excluir quando outro campo é verdadeiro
RuleFor(x => x.Reason)
    .NotEmpty().MaximumLength(500)
    .Unless(x => x.IsSystemAction);
```

### 3.5 Dependência entre campos

```csharp
// Quando StartAt é fornecido, EndAt deve ser depois
RuleFor(x => x.EndAt)
    .GreaterThan(x => x.StartAt)
    .WithMessage("EndAt must be after StartAt.")
    .When(x => x.StartAt.HasValue && x.EndAt.HasValue);
```

---

## 4. Template de teste unitário do Validator

```csharp
using FluentAssertions;
using NexTraceOne.{Module}.Application.Features.{FeatureName};
using Xunit;

namespace NexTraceOne.{Module}.Tests.Application.Features.{FeatureName};

/// <summary>
/// Testes unitários do Validator de {FeatureName}.
/// Cobre: comando válido, campos obrigatórios, limites de comprimento,
/// enums inválidos e combinações condicionais.
/// </summary>
public sealed class {FeatureName}ValidatorTests
{
    private readonly {FeatureName}.Validator _sut = new();

    // ── Factory para comando válido ──────────────────────────────────────────

    private static {FeatureName}.Command ValidCommand() =>
        new(ResourceId: Guid.NewGuid().ToString(),
            Name: "Valid Name",
            Description: null,
            MaxItems: 10);

    // ── Comando válido deve passar ───────────────────────────────────────────

    [Fact]
    public void Valid_Command_Should_Pass()
    {
        _sut.Validate(ValidCommand()).IsValid.Should().BeTrue();
    }

    // ── Campos obrigatórios ──────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_ResourceId_Should_Fail(string id)
    {
        var result = _sut.Validate(ValidCommand() with { ResourceId = id });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ResourceId");
    }

    [Fact]
    public void Empty_Name_Should_Fail()
    {
        var result = _sut.Validate(ValidCommand() with { Name = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    // ── Limites de comprimento ───────────────────────────────────────────────

    [Fact]
    public void Name_Exceeding_MaxLength_Should_Fail()
    {
        var result = _sut.Validate(ValidCommand() with { Name = new string('A', 201) });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Description_At_MaxLength_Should_Pass()
    {
        var result = _sut.Validate(ValidCommand() with { Description = new string('B', 2000) });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Description_Exceeding_MaxLength_Should_Fail()
    {
        var result = _sut.Validate(ValidCommand() with { Description = new string('B', 2001) });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    // ── Numéricos / limites ──────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void MaxItems_Out_Of_Range_Should_Fail(int value)
    {
        var result = _sut.Validate(ValidCommand() with { MaxItems = value });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MaxItems");
    }

    // ── Campos opcionais nulos devem passar sem erro ─────────────────────────

    [Fact]
    public void Null_Description_Should_Pass()
    {
        var result = _sut.Validate(ValidCommand() with { Description = null });
        result.IsValid.Should().BeTrue();
    }
}
```

---

## 5. Prioridade de implementação de validators

| Prioridade | Tipo | Motivo |
|-----------|------|--------|
| 🔴 **Alta** | Commands de escrita (Create/Update/Delete) | Risco de dados inválidos em BD |
| 🟡 **Média** | Queries com filtros e paginação | Risco de query injection / performance |
| 🟢 **Baixa** | Queries de leitura por ID | Validação do GUID já ocorre no handler |
| ⚪ **N/A** | Commands sem parâmetros, seeds internos | Sem input externo a validar |

---

## 6. Comandos para gerar o scaffold

Criar os ficheiros manualmente seguindo o template acima. O projeto não usa T4 ou Roslyn generators — todos os validators são ficheiros `.cs` normais dentro da feature correspondente, no namespace `NexTraceOne.{Module}.Application.Features.{FeatureName}`.

Estrutura esperada:
```
src/modules/{module}/NexTraceOne.{Module}.Application/
  Features/
    {FeatureName}/
      {FeatureName}.cs          ← Command/Query + Validator + Handler + Response (tudo junto)
tests/modules/{module}/
  NexTraceOne.{Module}.Tests/
    Application/
      Features/
        {FeatureName}/
          {FeatureName}ValidatorTests.cs   ← apenas os testes do validator
```

---

## 7. Checklist de revisão de código

Ao rever um PR que adiciona ou altera um Command/Query:

- [ ] O Command/Query tem parâmetros de entrada externos?
- [ ] Existe um `Validator : AbstractValidator<Command>`?
- [ ] Campos obrigatórios têm `.NotEmpty()`?
- [ ] Strings têm `.MaximumLength(n)` com limite razoável?
- [ ] Campos opcionais têm `.When(x => x.Field is not null)`?
- [ ] Campos numéricos têm `.InclusiveBetween(min, max)` ou `.GreaterThan(n)`?
- [ ] Existem testes unitários para o validator?
- [ ] Os testes cobrem: comando válido, campo obrigatório vazio, excesso de comprimento?

# NexTraceOne — Sovereign Change Intelligence Platform

## Identidade

NexTraceOne é uma **Sovereign Change Intelligence Platform** — enterprise, self-hosted, soberana e auditável.
Conecta contrato → mudança → impacto → aprovação → runtime → custo em um único sistema auditável.
Segmento: Bancos, seguradoras, telecom, governo, grandes enterprises.

## Stack

- **Backend:** .NET 10, ASP.NET Core 10 (Minimal APIs, REPR Pattern), EF Core 10, PostgreSQL 16
- **Libs:** MediatR 12, FluentValidation 11, Quartz.NET 3, OpenTelemetry 1.x, Serilog 4, Ardalis.GuardClauses, Mapster (source-gen)
- **Arquitetura:** Modular Monolith + Vertical Slice (VSA) + DDD tático + CQRS + Outbox Pattern
- **Infra MVP1:** PostgreSQL apenas (sem Redis, sem OpenSearch, sem Temporal)

## Estrutura do Projeto (Archon Pattern v2)

```
NexTraceOne/
├── src/
│   ├── building-blocks/          ← 6 projetos granulares (substituem SharedKernel)
│   │   ├── NexTraceOne.BuildingBlocks.Domain
│   │   ├── NexTraceOne.BuildingBlocks.Application
│   │   ├── NexTraceOne.BuildingBlocks.Infrastructure
│   │   ├── NexTraceOne.BuildingBlocks.EventBus
│   │   ├── NexTraceOne.BuildingBlocks.Observability
│   │   └── NexTraceOne.BuildingBlocks.Security
│   ├── modules/                  ← 14 módulos × 5 camadas cada
│   │   └── {nome}/
│   │       ├── NexTraceOne.{Nome}.Domain
│   │       ├── NexTraceOne.{Nome}.Application
│   │       ├── NexTraceOne.{Nome}.Contracts
│   │       ├── NexTraceOne.{Nome}.Infrastructure
│   │       └── NexTraceOne.{Nome}.API
│   └── platform/
│       ├── NexTraceOne.ApiHost           ← Compõe todos os módulos
│       └── NexTraceOne.BackgroundWorkers ← Outbox, Jobs
├── tools/NexTraceOne.CLI                 ← CLI 'nex' (consome só Contracts)
├── tests/
│   ├── building-blocks/
│   ├── modules/
│   └── platform/
└── docs/
```

## Referências Detalhadas

See @docs/ARCHITECTURE.md for arquitetura completa, Building Blocks, módulos e regras de dependência.
See @docs/CONVENTIONS.md for padrões de código, idioma, documentação XML e regras inegociáveis.
See @docs/ROADMAP.md for estado atual do projeto, fases e próximos passos.
See @docs/DOMAIN.md for taxonomia de mudanças, discovery de dependências e domínio de negócio.
See @docs/SECURITY.md for pilares de segurança, RLS, encryption, integrity e licensing.

## Regras Críticas (SEMPRE seguir)

1. **Idioma:** Código/logs/nomes em INGLÊS. Comentários XML (`<summary>`) e inline em PORTUGUÊS.
2. **Nunca** usar `DateTime.Now` — sempre `IDateTimeProvider`.
3. **Nunca** acessar DbContext de outro módulo — comunicação via Integration Events ou Contracts.
4. **Nunca** publicar Integration Events sem Outbox Pattern.
5. **Um módulo por vez**, uma camada por vez (Domain → Application → Infrastructure → API), um aggregate por vez.
6. **Toda classe pública** com `<summary>` XML em português.
7. **Todo método público** com `<summary>` XML em português.
8. **Result Pattern** para operações que podem falhar — sem exceções para controle de fluxo.
9. **Testes unitários** adjacentes a cada implementação.

## Comandos

```bash
# Build
dotnet build NexTraceOne.sln

# Testes
dotnet test NexTraceOne.sln

# Executar API
dotnet run --project src/platform/NexTraceOne.ApiHost

# Executar Workers
dotnet run --project src/platform/NexTraceOne.BackgroundWorkers

# Executar CLI
dotnet run --project tools/NexTraceOne.CLI
```

## Estado Atual

- ✅ Script de scaffold v2 (Archon Pattern) pronto para execução
- 🔲 Próximo passo: Executar scaffold e implementar BuildingBlocks.Domain (Entity, ValueObject, Result)
- 🔲 Fase 1: Building Blocks → Identity → Licensing

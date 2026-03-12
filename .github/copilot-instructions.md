# Copilot Instructions

## Project Guidelines
- Neste repositório, usar código/logs/nomes em inglês, comentários XML em português, classes finais como sealed, CancellationToken em toda async, construtores privados em aggregates com factory methods, Result<T> para falhas, guard clauses no início, error codes como chaves i18n, strongly typed IDs, nunca DateTime.Now, nunca acessar DbContext de outro módulo e seguir docs/CONVENTIONS.md e docs/ARCHITECTURE.md.

---

## Critérios de Done do MVP1 — Por Perfil de Usuário

O MVP1 está pronto quando:

### DESENVOLVEDOR consegue:
- Importar uma API em menos de 5 minutos
- Ver exatamente o que mudou entre versões em 30 segundos
- Entender quais consumers serão afetados antes de submeter
- Submeter para workflow em menos de 2 minutos
- Receber feedback claro sobre o que bloqueia sua release

### TECH LEAD consegue:
- Aprovar uma mudança com contexto completo
- Ver o evidence pack completo de uma release
- Entender o Blast Radius sem abrir outra ferramenta
- Rejeitar com feedback estruturado
- Promover para Pre-Production e Production com gates reais

### PLATFORM ADMIN consegue:
- Configurar workflow templates por tipo de mudança
- Configurar rulesets e bindings
- Configurar ambientes e gates de promoção
- Gerenciar usuários e roles
- Validar a trilha de auditoria completa

### AUDITOR consegue:
- Encontrar qualquer mudança por quem/quando/o quê
- Verificar que o processo foi seguido para cada release
- Exportar evidências de um período em JSON ou CSV
- Verificar integridade criptográfica da trilha

---

## Métricas de Qualidade do MVP1

| Métrica | Alvo |
|---------|------|
| **Time to First Value** | < 30 minutos (do install à primeira API importada com diff visível) |
| **Time to Core Value** | < 7 dias (do install ao primeiro workflow completo aprovado) |
| **Cobertura de auditoria** | 100% (toda ação relevante gera AuditEvent) |
| **Disponibilidade alvo** | 99.5% (self-hosted, depende da infra do cliente) |

### Performance

| Operação | Alvo |
|----------|------|
| Import de contrato | < 5 segundos |
| Diff semântico | < 10 segundos |
| Cálculo de Blast Radius | < 5 segundos |
| Busca no catálogo | < 1 segundo |
| Audit search (90 dias) | < 3 segundos |

---

## Stack Técnica do MVP1

### Backend
- .NET 10 / ASP.NET Core 10
- Entity Framework Core 10 + Npgsql
- PostgreSQL 16 (dados principais + audit + search)
- MediatR (CQRS + pipeline behaviors)
- FluentValidation
- Quartz.NET (jobs: outbox processor, SLA checker, fingerprint capture, license validation)
- OpenTelemetry (tracing, metrics, logging)
- Serilog → OpenTelemetry exporter

### Frontend
- React 18 + TypeScript
- Vite
- TanStack Router
- TanStack Query
- Zustand
- Tailwind CSS
- Radix UI
- Apache ECharts (grafos e métricas)
- Playwright (testes e2e)

### Infraestrutura MVP1
- PostgreSQL 16 (único banco de dados)
- Sem Redis no MVP1 (cache em memória onde necessário)
- Sem OpenSearch no MVP1 (PostgreSQL FTS suficiente)
- Sem Temporal no MVP1 (workflow em state machine com Quartz.NET + PostgreSQL)
- SMTP para notificações de email

### Distribuição
- Docker Compose (para POC e avaliação)
- Instalador Windows (MSI) para produção self-hosted
- CLI distribuída como binário único (win-x64, linux-x64)

---

## Ordem de Desenvolvimento do MVP1

### FASE 1 — Fundação (Semanas 1–4)
- BuildingBlocks.Domain
- BuildingBlocks.Application
- BuildingBlocks.Infrastructure
- Identity & Access (básico)
- Licensing & Entitlements (básico)

### FASE 2 — Catálogo e Contratos (Semanas 5–8)
- Engineering Graph (Asset Catalog)
- Developer Portal (catálogo navegável)
- Contracts & Interoperability (import/export/diff)
- Ruleset Governance (upload + execução básica)

### FASE 3 — Inteligência de Mudança (Semanas 9–12)
- Releases & Change Intelligence (core)
- Deployment Notification Endpoint
- Blast Radius Engine (básico)
- Change Intelligence Score

### FASE 4 — Governança (Semanas 13–16)
- Workflow & Approval Engine
- Evidence Pack automático
- Promotion Governance
- Promotion Gates

### FASE 5 — Auditoria e IA Básica (Semanas 17–20)
- Audit & Traceability Layer completo
- IA básica (classificação + resumo de aprovação)
- External AI Consultation (Simple Mode)
- NexTrace CLI

### FASE 6 — Hardening e Lançamento (Semanas 21–24)
- Testes end-to-end completos
- Performance e otimização
- Documentação operacional
- Docker Compose e instalador
- Onboarding wizard e tour guiado

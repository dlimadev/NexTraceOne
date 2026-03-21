# Phase 1 — Security and Production Baseline Report

**Data:** 2026-03-21  
**Fase:** FASE 1 — SEGURANÇA CRÍTICA + BASELINE DE PRODUÇÃO  
**Responsável:** Principal Security Engineer / Platform Architect  
**Status:** ✅ Concluída

---

## 1. Resumo Executivo

### Riscos críticos tratados

| # | Risco | Severidade Anterior | Status |
|---|---|---|---|
| 1 | JWT Secret sem validação de comprimento mínimo | 🔴 CRÍTICO | ✅ Corrigido |
| 2 | 32 endpoints ChangeGovernance sem autorização | 🔴 CRÍTICO | ✅ Corrigido |
| 3 | Fallback `DefaultConnection` em todos os DI files | 🟠 ALTO | ✅ Corrigido |
| 4 | Ausência de documentação operacional de secrets | 🟠 ALTO | ✅ Documentado |
| 5 | Ausência de documentação de autorização de endpoints | 🟠 ALTO | ✅ Documentado |

### O que impedia produção antes

1. **JWT Secret**: existia validação de presença, mas não de comprimento mínimo — um secret de 1 char passava a validação
2. **Endpoints expostos**: 32 endpoints do módulo ChangeGovernance (releases, workflow, freeze) estavam sem RequirePermission, acessíveis a qualquer request autenticado
3. **Fallback inseguro**: todos os 16 DI files aceitavam `DefaultConnection` como fallback de terceiro nível — risco de usar connection string incorrecta silenciosamente
4. **Sem documentação operacional**: operadores não tinham guia claro de quais secrets configurar, em qual formato, e o que bloquearia o startup

### O que muda agora

- Startup falha explicitamente se JWT secret tiver menos de 32 caracteres em ambientes não-Development
- Todos os endpoints do sistema têm autorização explícita
- Connection strings usam helper `GetRequiredConnectionString` sem fallback para `DefaultConnection`
- Existe documentação completa de secrets, configuração e auditoria de endpoints

---

## 2. Código Alterado

### Ficheiros modificados

| Ficheiro | Natureza da Alteração | Impacto |
|---|---|---|
| `src/platform/NexTraceOne.ApiHost/StartupValidation.cs` | Adicionada validação de comprimento mínimo JWT (32 chars) | Alto — startup bloqueado com secret fraco |
| `src/building-blocks/NexTraceOne.BuildingBlocks.Infrastructure/Configuration/ConfigurationExtensions.cs` | **Novo ficheiro** — helper `GetRequiredConnectionString` | Médio — centraliza resolução segura de connection strings |
| `src/modules/*/DependencyInjection.cs` (16 ficheiros) | Substituída cadeia fallback por `GetRequiredConnectionString` | Alto — eliminado fallback `DefaultConnection` |
| `src/modules/changegovernance/.../AnalysisEndpoints.cs` | Adicionado `RequirePermission` a 6 endpoints | Alto — endpoints protegidos |
| `src/modules/changegovernance/.../DeploymentEndpoints.cs` | Adicionado `RequirePermission` a 3 endpoints | Alto — endpoints protegidos |
| `src/modules/changegovernance/.../FreezeEndpoints.cs` | Adicionado `RequirePermission` a 2 endpoints | Alto — endpoints protegidos |
| `src/modules/changegovernance/.../IntelligenceEndpoints.cs` | Adicionado `RequirePermission` a 6 endpoints | Alto — endpoints protegidos |
| `src/modules/changegovernance/.../ReleaseQueryEndpoints.cs` | Adicionado `RequirePermission` a 3 endpoints | Alto — endpoints protegidos |
| `src/modules/changegovernance/.../ApprovalEndpoints.cs` | Adicionado `RequirePermission` a 5 endpoints | Alto — endpoints protegidos |
| `src/modules/changegovernance/.../StatusEndpoints.cs` | Adicionado `RequirePermission` a 3 endpoints | Alto — endpoints protegidos |
| `src/modules/changegovernance/.../EvidencePackEndpoints.cs` | Adicionado `RequirePermission` a 3 endpoints | Alto — endpoints protegidos |
| `src/modules/changegovernance/.../TemplateEndpoints.cs` | Adicionado `RequirePermission` a 1 endpoint | Alto — endpoint protegido |

### Ficheiros de documentação criados

| Ficheiro | Conteúdo |
|---|---|
| `docs/security/PHASE-1-SECRETS-BASELINE.md` | Classificação de secrets, política por ambiente, convenção de env vars |
| `docs/security/REQUIRED-ENVIRONMENT-CONFIGURATION.md` | Tabela completa de variáveis obrigatórias por ambiente, exemplos Docker |
| `docs/security/BACKEND-ENDPOINT-AUTH-AUDIT.md` | Auditoria completa de todos os endpoints e suas políticas de autorização |
| `docs/security/PHASE-1-PRODUCTION-BASELINE-CHECKLIST.md` | Checklist operacional de 7 blocos para deploy seguro |
| `docs/audits/PHASE-1-SECURITY-AND-PRODUCTION-BASELINE-REPORT.md` | Este documento |

---

## 3. Segurança Endurecida

### JWT

**Antes:**
```csharp
// Apenas verificava se estava vazio — secret de 1 char passava!
if (string.IsNullOrWhiteSpace(jwtSecret))
    throw new InvalidOperationException("...");
```

**Depois:**
```csharp
// Verifica presença + comprimento mínimo de 32 caracteres (256 bits equivalente)
private const int MinimumJwtSecretLength = 32;

if (string.IsNullOrWhiteSpace(jwtSecret))
    throw new InvalidOperationException($"... minimum {MinimumJwtSecretLength} characters required");

if (jwtSecret.Length < MinimumJwtSecretLength)
    throw new InvalidOperationException($"... must be at least {MinimumJwtSecretLength} characters long");
```

**Comportamento por ambiente:**
- Development: warning se ausente, warning se curto — não bloqueia
- Staging/Production: falha crítica se ausente OU se `< 32 caracteres`

### Connection Strings

**Antes:**
```csharp
var connectionString = configuration.GetConnectionString("SpecificDatabase")
    ?? configuration.GetConnectionString("NexTraceOne")
    ?? configuration.GetConnectionString("DefaultConnection")  // ← RISCO
    ?? throw new InvalidOperationException("...");
```

**Depois:**
```csharp
// Helper centralizado, sem fallback para DefaultConnection
var connectionString = configuration.GetRequiredConnectionString("SpecificDatabase", "NexTraceOne");
```

O helper `GetRequiredConnectionString` aceita apenas os fallbacks explicitamente declarados (o fallback "NexTraceOne" é arquitetural, parte do ADR-001 de consolidação de bases de dados). A cadeia `?? DefaultConnection` foi eliminada de todos os 16 módulos.

### Endpoint Authorization — ChangeGovernance

**Antes:** 32 endpoints sem `RequirePermission` — acessíveis por qualquer utilizador com token válido.

**Depois:** Todos os 32 endpoints têm permissão granular explícita:

| Grupo | Permissões | Semântica |
|---|---|---|
| Releases / ChangeIntelligence | `change-intelligence:read/write` | Alinhado com ChangeConfidenceEndpoints |
| Workflow / Approval | `workflow:instances:write` | Operações de aprovação/rejeição |
| Workflow / Status | `workflow:instances:read/write` | Consulta e escalação |
| Workflow / EvidencePacks | `workflow:instances:read/write` | Geração e consulta |
| Workflow / Templates | `workflow:templates:write` | Gestão de templates |

### Frontend — ReactQueryDevtools

Já estava correctamente protegido com `import.meta.env.DEV`:
```tsx
{import.meta.env.DEV && <ReactQueryDevtoolsDev />}
```
O Vite tree-shake this branch em builds de produção. Confirmado — nenhuma alteração necessária.

### Integrity Check e Security Headers

**AssemblyIntegrityChecker:** já existia e funcionava. Documentado em `PHASE-1-PRODUCTION-BASELINE-CHECKLIST.md`.

**Security Headers:** já implementados em `WebApplicationExtensions.cs`:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Strict-Transport-Security` (apenas non-Development)
- `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'`
- `Cache-Control: no-store`
- `Permissions-Policy`
- `Referrer-Policy`

---

## 4. Evidências

### Onde estavam os problemas

1. **JWT fraco:** `StartupValidation.cs` linha 64-70 — apenas `IsNullOrWhiteSpace`, sem validação de comprimento
2. **DefaultConnection fallback:** em todos os 16 módulos Infrastructure — linha de código `?? configuration.GetConnectionString("DefaultConnection")`
3. **Endpoints expostos:** 9 ficheiros em ChangeGovernance sem `using NexTraceOne.BuildingBlocks.Security.Extensions;` e sem `RequirePermission`

### Como foram corrigidos

1. **JWT:** `StartupValidation.cs` refactored com método `ValidateJwtSecret` separado, com lógica de ambiente e comprimento mínimo
2. **DefaultConnection:** criado `ConfigurationExtensions.GetRequiredConnectionString` em BuildingBlocks.Infrastructure; 16 DI files actualizados via script Python
3. **Endpoints:** script Python adicionou `using` directive e `.RequirePermission(...)` a cada endpoint — verificado por re-auditoria que mostra `auth ≥ maps` para todos os ficheiros

---

## 5. Comportamento por Ambiente

### Development
```
✅ Startup: warning se Jwt:Secret ausente/curto, não bloqueia
✅ DB: warning se connection string vazia, não bloqueia
✅ IntegrityCheck: desactivado por defeito (appsettings.Development.json)
✅ HSTS: desactivado (não HTTPS local)
✅ ReactQueryDevtools: visível
⚠️  Jwt:Secret: pode ter menos de 32 chars (aviso emitido)
```

### Staging
```
🚫 Startup BLOQUEADO se: Jwt:Secret ausente, Jwt:Secret < 32 chars, qualquer connection string vazia
✅ HSTS: activado
✅ Cookies: RequireSecureCookies = true
✅ Endpoints: todos protegidos
✅ IntegrityCheck: recomendado activado
```

### Production
```
🚫 Startup BLOQUEADO se: Jwt:Secret ausente, Jwt:Secret < 32 chars, qualquer connection string vazia
🚫 NEXTRACE_SKIP_INTEGRITY nunca deve ser true
✅ HSTS com preload activado
✅ Cookies: RequireSecureCookies = true
✅ Todos os endpoints com autorização explícita
✅ ReactQueryDevtools: não presente no bundle
✅ Security headers: completos
```

---

## 6. Pendências Remanescentes

### Fora do escopo da Fase 1 (documentadas para Fase 2)

| Item | Prioridade | Notas |
|---|---|---|
| Fallback authorization policy global (`deny by default`) | Alta | Requer testes de impacto nos endpoints públicos |
| Integração com secrets manager externo (Vault / AWS SM) | Alta | Requer infra setup |
| Rotation automática de JWT Secret | Média | Depende de secrets manager |
| Scanner de secrets no pipeline CI | Alta | git-secrets ou trufflehog |
| Testes de penetração OWASP Top 10 | Alta | Fase de qualidade |
| Geração de ficheiros `.sha256` no build pipeline | Média | Requer pipeline ajuste |
| CSP granular para frontend SPA | Baixa | Requer análise de compatibilidade |
| Auditoria de `Security:ApiKeys` configuração | Média | Keys sistema-a-sistema |

---

## 7. Próximos Passos Recomendados — Fase 2

A Fase 1 estabeleceu o baseline de segurança. A Fase 2 deve focar em:

### 7.1 Secrets Management Real
- Integrar com HashiCorp Vault ou AWS Secrets Manager
- Configurar rotation automática de JWT Secret e passwords de DB
- Eliminar qualquer dependência de variáveis de ambiente em texto claro em produção

### 7.2 Hardening de Autorização
- Configurar fallback policy global `RequireAuthenticatedUser()`
- Migrar endpoints com `RequireAuthorization()` genérico para `RequirePermission` granular
- Rever permissões de `change-intelligence:write` — considerar granularidade por operação

### 7.3 Pipeline de Segurança
- Adicionar scanner de secrets ao pre-commit hook e CI
- Configurar dependabot ou similar para alertas de vulnerabilidades em dependências
- Gerar e verificar ficheiros `.sha256` no pipeline de build

### 7.4 Observabilidade de Segurança
- Audit trail para operações sensíveis (rotação de secrets, alterações de configuração)
- Alertas para falhas de autenticação em volume (brute force)
- Métricas de rate limiting

### 7.5 Testes de Segurança
- Testes de integração para validação de startup com configuração inválida
- Testes unitários para `GetRequiredConnectionString`
- Scan de segurança automatizado (SAST/DAST)

---

## Apêndice — Verificação de Build

**Status:** ✅ Build 0 errors após todas as alterações

```
dotnet build src/platform/NexTraceOne.ApiHost/NexTraceOne.ApiHost.csproj
  12 Warning(s) [warnings pré-existentes, não introduzidos nesta fase]
  0 Error(s)
```

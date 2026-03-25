# Relatório de Configuração e Parametrização — NexTraceOne

**Data:** 25 de março de 2026

---

## 1. Objectivo

Auditar o estado de configuração e parametrização do sistema: o que está hardcoded vs parametrizado, o que deveria ser configurável por admin funcional vs o que deve ser técnico, e o que representa riscos de configuração.

---

## 2. Estado Actual da Configuração

### 2.1 Ficheiros de Configuração

| Ficheiro | Propósito | Estado |
|---------|-----------|--------|
| `appsettings.json` | Config principal — CRÍTICO | BROKEN |
| `appsettings.Development.json` | Overrides de desenvolvimento | PARTIAL |
| `.env.example` | Template de variáveis de ambiente | READY |
| `docker-compose.yml` | Stack completa para POC | READY |
| `Directory.Build.props` | Build global | READY |
| `Directory.Packages.props` | Versões NuGet centralizadas | READY |

---

## 3. Problemas Críticos de Configuração

### 3.1 appsettings.json — BROKEN

**Localização:** `src/platform/NexTraceOne.ApiHost/appsettings.json`

**Problemas:**

#### Credenciais hardcoded (21 connection strings):
```json
"ConnectionStrings": {
  "IdentityDb": "...;Password=ouro18;...",
  "ContractsDb": "...;Password=ouro18;...",
  // ...19 mais com a mesma senha
}
```
**Regra violada:** Segredos nunca devem estar em ficheiros de configuração commitados.

#### JWT Secret vazio:
```json
"Jwt": {
  "Secret": ""
}
```
**Regra violada:** Configuração crítica sem valor — dependente de env var sem falha explícita.

#### CORS de desenvolvimento no config base:
```json
"Cors": {
  "AllowedOrigins": ["http://localhost:5173", "http://localhost:3000"]
}
```
**Regra violada:** Origens de desenvolvimento devem estar apenas em `appsettings.Development.json`.

#### OIDC com segredos vazios:
```json
"Oidc": {
  "ClientId": "",
  "ClientSecret": ""
}
```
**Estado:** Vazio é aceitável (SSO opcional) mas deve ser documentado explicitamente.

---

### 3.2 .env.example — READY (mas não enforçado)

```env
POSTGRES_PASSWORD=change-me-in-production
JWT_SECRET=...
NEXTRACE_ENCRYPTION_KEY=...
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
OBSERVABILITY_PROVIDER=ClickHouse
OLLAMA_ENDPOINT=http://localhost:11434
OPENAI_API_KEY=
```

**Positivo:** Template bem documentado com todas as variáveis necessárias.
**Problema:** Não existe validação de startup que garanta que estas variáveis foram configuradas em produção.

---

## 4. O que está Hardcoded e Não Deveria Estar

| Configuração | Localização | Tipo de Problema |
|-------------|-------------|-----------------|
| Senha PostgreSQL "ouro18" | `appsettings.json` | SEGREDO — CRITICAL |
| JWT secret (fallback) | `JwtTokenService.cs:48` | SEGREDO — CRITICAL |
| AES key (fallback) | `AesGcmEncryptor.cs:113` | SEGREDO — HIGH |
| Rate limits (20/10/30/50/100) | `Program.cs:97-209` | OPERACIONAL — MEDIUM |
| Log retention (30 dias) | `appsettings.json` | OPERACIONAL — LOW |
| CORS localhost | `appsettings.json` | CONFIG — HIGH |
| NEXTRACE_AUTO_MIGRATE=false | `.env.example` | OPERACIONAL — OK |
| NEXTRACE_SKIP_INTEGRITY | `security.yml` | SEGURANÇA — HIGH |

---

## 5. O que Deveria Virar Parametrização Persistida

Aplicando a regra: **"Se precisa ser alterado por admin funcional sem redeploy → parametrização persistida"**

| Configuração | Candidato a | Justificativa |
|-------------|-------------|---------------|
| Janelas de deploy (freeze windows) | `chg_freeze_windows` (já existe) | Admin funcional altera sem IT |
| Thresholds de blast radius | ConfigurationEntry | Varia por ambiente/risco |
| SLA policies de workflow | `chg_wf_sla_policies` (já existe) | PM/Operations altera |
| Token budgets de IA | `aik_gov_ai_budgets` (já existe) | Finance/IT Admin altera |
| Rate limit de IA por utilizador | ConfigurationEntry | Admin IA altera |
| Regras de governance | `gov_governance_packs` (já existe) | Compliance altera |
| Retention policies | `aud_retention_policies` (já existe) | Legal/Compliance |
| Feature flags | **ConfigurationEntry** (a criar) | Product altera |
| Thresholds de anomaly detection | ConfigurationEntry | Ops altera |
| Aprovadores de promotion por ambiente | `chg_prm_promotion_gates` (já existe) | Platform Admin altera |

---

## 6. O que Deve Permanecer Configuração Técnica

| Configuração | Justificativa |
|-------------|---------------|
| Connection strings | Infra — alterado por IT/DevOps |
| JWT secret, AES key | Segredo — gerido por secrets manager |
| SMTP server | Infra — alterado por IT |
| OIDC provider URLs | Infra — alterado por IT/Security |
| OTel collector endpoint | Infra — alterado por Platform |
| ClickHouse endpoint | Infra — alterado por Platform |
| Docker compose ports | Infra — ambiente dev/staging |

---

## 7. Configurações Ausentes ou Incompletas

### 7.1 Feature Flags

O sistema não tem um mecanismo formal de feature flags:
- `releaseScope.ts` no frontend faz gating por rota
- Sem backend para feature flags configuráveis
- `ConfigurationModule` existe mas sem entidade de feature flag

**Recomendação:** Adicionar `FeatureFlagDefinition` e `FeatureFlagEntry` ao `ConfigurationDbContext`

### 7.2 Parâmetros de IA Ausentes

Em `appsettings.json` e `ConfigurationModule` não foram encontrados:
- Model selection policy (algoritmo de roteamento padrão)
- Maximum context window por tenant
- AI response timeout
- Knowledge source weights (hardcoded em `ListKnowledgeSourceWeightsQuery`)
- Suggested prompts (hardcoded em `ListSuggestedPromptsQuery`)

### 7.3 Ambiente-Awareness

O sistema tem `ICurrentEnvironment` mas sem parametrização por ambiente:
- Sem thresholds específicos por ambiente (dev vs staging vs prod)
- Sem politicas de aprovação diferentes por ambiente

---

## 8. Configuração do docker-compose.yml

**Estado: READY para POC/avaliação**

```yaml
services:
  postgres:      # PostgreSQL 16-alpine
  clickhouse:    # ClickHouse 24.8-alpine
  otel-collector: # 0.115.0
  apihost:       # NexTraceOne.ApiHost
  workers:       # NexTraceOne.BackgroundWorkers
  ingestion:     # NexTraceOne.Ingestion.Api
  frontend:      # React SPA
```

**Notas:**
- Health checks configurados em todos os serviços
- Dependências correctas entre serviços
- Sem Ollama container — IA local requer instalação manual
- Volumes para PostgreSQL e ClickHouse correctamente configurados

---

## 9. Configuração de Build

### Directory.Build.props
```xml
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
```

**Estado: EXCELENTE** — nullable habilitado e warnings como erros

### Directory.Packages.props

Versões centralizadas para todos os pacotes NuGet — sem conflitos de versão entre módulos.

**Estado: READY**

---

## 10. Configuração de Observabilidade

**Build/otel-collector/otel-collector.yaml:**
- Receivers: OTLP (gRPC 4317, HTTP 4318), Prometheus
- Processors: batch, memory_limiter
- Exporters: ClickHouse para traces/logs/metrics, Prometheus para metrics, debug
- Pipeline: traces, metrics, logs todos configurados

**Estado: READY** — pipeline configurado adequadamente

---

## 11. Resumo de Achados

| Categoria | Quantidade | Estado Predominante |
|-----------|-----------|---------------------|
| Segredos hardcoded | 3 | CRITICAL |
| Config dev em base | 2 | HIGH |
| Config operacional hardcoded | 5 | MEDIUM |
| Config ausente/incompleta | 4 áreas | MEDIUM |
| Config técnica correcta | 8+ | READY |

---

## 12. Recomendações

| Prioridade | Acção |
|-----------|-------|
| P0 | Remover todos os segredos de `appsettings.json` |
| P0 | Adicionar validação de startup para JWT_SECRET e NEXTRACE_ENCRYPTION_KEY |
| P1 | Mover CORS localhost para appsettings.Development.json |
| P1 | Externalizar rate limits para appsettings (não hardcoded no Program.cs) |
| P1 | Adicionar Ollama container ao docker-compose.yml |
| P2 | Adicionar FeatureFlagDefinition/FeatureFlagEntry ao ConfigurationDbContext |
| P2 | Mover ListKnowledgeSourceWeights para ConfigurationEntry |
| P2 | Mover ListSuggestedPrompts para ConfigurationEntry por persona |
| P2 | Adicionar parametrização de thresholds de AI por tenant |
| P3 | Criar hierarquia de configuração tenant/environment/module no ConfigurationDbContext |

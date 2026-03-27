# Relatório de Configuração e Parametrização — NexTraceOne
**Auditoria Forense | Março 2026**

---

## 1. Inventário de Configuração

| Arquivo | Localização | Propósito |
|---|---|---|
| appsettings.json | `src/platform/NexTraceOne.ApiHost/` | Defaults de produção |
| appsettings.Development.json | `src/platform/NexTraceOne.ApiHost/` | Overrides de desenvolvimento |
| appsettings.json | `src/platform/NexTraceOne.Ingestion.Api/` | Ingestão — defaults |
| appsettings.Development.json | `src/platform/NexTraceOne.Ingestion.Api/` | Ingestão — desenvolvimento |
| .env.example | Raiz do repositório | Template de variáveis de ambiente |
| docker-compose.yml | Raiz | Configuração Docker principal |
| docker-compose.override.yml | Raiz | Overrides locais |
| Directory.Build.props | Raiz | Propriedades centrais .NET |
| Directory.Packages.props | Raiz | Versões de pacotes NuGet |
| global.json | Raiz | Versão do SDK .NET |

---

## 2. Análise de appsettings.json (Produção)

### Segurança — Estado: CORRETO

| Configuração | Valor | Avaliação |
|---|---|---|
| `Jwt:ExpirationMinutes` | 60 | Razoável |
| `Security:Cookie:Secure` | `true` | Correto — requer HTTPS |
| `Security:CsrfProtection:Enabled` | `true` | Correto |
| `Security:IntegrityCheck:Enabled` | `true` | Correto para produção |
| `Security:ApiKeys` | Array vazio | Correto — preenchido por admin |
| `Cors:AllowedOrigins` | Array vazio | **Correto** — requer config explícita em produção |

### Rate Limiting — Estado: BEM CONFIGURADO

| Policy | Limite | Janela |
|---|---|---|
| Global | 100/IP (20 não-resolvidos) | 1 min |
| Auth | 20/IP | 1 min |
| AuthSensitive | 10/IP | 1 min |
| AI | 30/IP | 1 min |
| DataIntensive | 50/IP | 1 min |
| Operations | 40/IP | 1 min |

### AI Providers — Estado: DEFAULTS SEGUROS

```json
"AiRuntime": {
  "Ollama": { "BaseUrl": "http://localhost:11434", "Enabled": true },
  "OpenAI": { "ApiKey": "", "Enabled": false },
  "Routing": { "PreferredProvider": "ollama" }
}
```

**Avaliação:** Ollama como padrão local; OpenAI desabilitado por padrão. Correto.

### Observabilidade — Estado: REQUIRES CONFIGURATION

- `"Endpoint": "http://localhost:4317"` para OpenTelemetry
- Em produção, requer substituição por endpoint real do coletor
- ClickHouse password vazia em desenvolvimento — requer configuração em produção

---

## 3. Análise de appsettings.Development.json

| Diferença | Valor Dev | Avaliação |
|---|---|---|
| `IntegrityCheck:Enabled` | `false` | Correto para dev |
| `Cors:AllowedOrigins` | `["localhost:5173", "localhost:3000"]` | Correto para dev |
| `Cookie:Secure` | `false` | Correto para HTTP local |
| Serilog MinimumLevel | `Debug` | Correto para dev |

**Avaliação:** Separação correta entre dev e produção.

---

## 4. Análise de .env.example

### Sem segredos hardcoded — CONFIRMADO

| Variável | Valor no Template | Avaliação |
|---|---|---|
| `POSTGRES_PASSWORD` | `change-me-in-production` | Correto — placeholder explícito |
| `JWT_SECRET` | `REPLACE-WITH-AT-LEAST-32-CHAR-SECRET-KEY` | Correto — obrigatório substituir |
| `NEXTRACE_ENCRYPTION_KEY` | `REPLACE-WITH-BASE64-32-BYTE-KEY` | Correto — obrigatório substituir |
| `NEXTRACE_SKIP_INTEGRITY` | `false` | Correto — default seguro |
| `NEXTRACE_AUTO_MIGRATE` | `false` | Correto — previne auto-migração em produção |
| `OLLAMA_ENDPOINT` | `http://localhost:11434` | Correto — local por padrão |
| `OPENAI_API_KEY` | vazio | Correto — opcional |

---

## 5. O que Está Hardcoded e Não Deveria

| Item | Localização | Recomendação |
|---|---|---|
| API Keys armazenadas em appsettings | `appsettings.json "ApiKeys"` | Migrar para armazenamento criptografado em banco para produção (anotado no código como MVP1) |
| ClickHouse password vazia | docker-compose/appsettings dev | Configurar credencial em produção |
| OpenTelemetry endpoint = localhost | appsettings.json | Configurar endpoint real por ambiente via env var |
| Modelos de IA hardcoded no config | `DefaultChatModel: "qwen3.5:9b"` | Migrar para Model Registry (que já existe) |
| Thresholds de blast radius | Verificar handlers de ChangeGovernance | Candidatos a parametrização persistida |
| Approval rules | Verificar WorkflowDbContext | Já persistidos via rulesets — OK |

---

## 6. O que Deve Virar Parametrização Persistida

Aplicando a regra: *"Se a configuração precisa ser alterada por admin funcional sem redeploy, é forte candidata a parametrização persistida."*

| Configuração | Estado Atual | Recomendação |
|---|---|---|
| Feature flags | Database-driven (ConfigurationDbContext) | Já correto |
| Thresholds de change score | Verificar handlers | Candidato a configuração persistida |
| Deploy windows / freeze windows | WorkflowDbContext (FreezeWindow entity) | Já persistido — correto |
| Retention policies | AuditDbContext | Verificar se é configurável por tenant |
| Rate limit overrides por tenant | appsettings.json | Candidato a override por tenant via ConfigurationDbContext |
| AI token budgets | AiGovernanceDbContext | Já persistido — correto |
| AI access policies | AiGovernanceDbContext | Já persistido — correto |

---

## 7. O que Deve Permanecer Configuração Técnica

| Item | Justificativa |
|---|---|
| Strings de conexão | Infraestrutura — não funcional |
| JWT secret | Segredo de segurança — env var |
| Encryption key | Segredo de segurança — env var |
| CORS origins | Infraestrutura — env var/appsettings por ambiente |
| Rate limiting global | Infraestrutura — appsettings |
| OpenTelemetry endpoint | Infraestrutura — env var |
| SDK e versão .NET | global.json |

---

## 8. O que Está Obsoleto ou Inconsistente

| Item | Estado | Recomendação |
|---|---|---|
| Seção de Commercial Governance no config (se existir) | Módulo removido no PR-17 | Verificar e remover |
| Configurações de tecnologias removidas | — | Confirmar limpeza |
| Feature flags para módulos mock | Config pode referenciar módulos mock | Sinalizar como PREVIEW explicitamente |

---

## 9. Feature Flags — Estado

**Status: IMPLEMENTADO via ConfigurationDbContext**

- Feature flags database-driven com override por tenant
- `SetFeatureFlagOverride` — override por tenant
- `GetEffectiveFeatureFlag` — resolução de valor efetivo
- `ConfigurationDefinitionSeeder` — valores padrão em desenvolvimento

**Gap:** Não há inventário documentado de todas as feature flags ativas no sistema. Recomendado criar lista canônica de flags por módulo.

---

## 10. Configuração de Licenciamento

**Status: AUSENTE**

O módulo de Commercial Governance (licenciamento) foi removido no PR-17. Não há seção de licensing no appsettings.json atual.

**Risco:** Se licenciamento for requisito para self-hosted enterprise, precisa ser reimplementado com abordagem diferente da original removida.

**Evidência:** `docs/REBASELINE.md` — "~~Commercial Governance~~ — REMOVIDO (PR-17)"

---

## 11. Avaliação Self-Hosted Readiness

| Critério | Estado | Evidência |
|---|---|---|
| Sem segredos hardcoded | Sim | .env.example com placeholders |
| Configuração por env vars | Sim | Todas as configs sensíveis via env |
| Docker Compose funcional | Sim | docker-compose.yml com todos os serviços |
| IIS support | Verificar | Documentado mas não verificado em detalhe |
| Auto-migrate desabilitado por padrão | Sim | `NEXTRACE_AUTO_MIGRATE=false` |
| Script de migração manual | Sim | `scripts/db/apply-migrations.sh` e `.ps1` |
| Sem dependências proprietárias | Sim (verificado) | PostgreSQL, Ollama, OpenTelemetry — open-source |
| Sem Redis obrigatório | Sim | Não detectado no stack |
| Sem Temporal obrigatório | Sim | Quartz.NET no lugar |

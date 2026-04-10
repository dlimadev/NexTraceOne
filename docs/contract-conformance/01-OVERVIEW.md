# Contract Conformance & Validation — Plano de Implementação

> **Módulo:** Contracts  
> **Versão do plano:** 1.0  
> **Data:** 2026-04-10  
> **Branch:** `claude/api-contract-validation-C1GSU`

---

## Índice deste plano

| Ficheiro | Conteúdo |
|----------|----------|
| [01-OVERVIEW.md](01-OVERVIEW.md) | Este ficheiro — contexto, problema, visão geral |
| [02-DOMAIN-MODEL.md](02-DOMAIN-MODEL.md) | Novas entidades de domínio e modelo de dados |
| [03-CONFIGURATION-PARAMETERS.md](03-CONFIGURATION-PARAMETERS.md) | Parametrização e configuração por tenant/serviço/ambiente |
| [04-API-ENDPOINTS.md](04-API-ENDPOINTS.md) | Novos endpoints de API |
| [05-CI-INTEGRATION.md](05-CI-INTEGRATION.md) | Integração com CI/CD — tokens, `.nextraceone.yaml`, GitHub Actions |
| [06-CHANGELOG.md](06-CHANGELOG.md) | Plano de implementação do changelog de contratos |
| [07-GAP-ANALYSIS.md](07-GAP-ANALYSIS.md) | Análise completa de gaps no módulo de contratos |
| [08-IMPLEMENTATION-PHASES.md](08-IMPLEMENTATION-PHASES.md) | Fases de implementação com ordem de prioridade |

---

## 1. Contexto e Problema

O NexTraceOne já suporta o ciclo completo de **design de contratos** (Draft → Review → Approved → Locked). O gap actual é:

> **Não existe nenhum mecanismo que valide se o contrato implementado pela equipa de desenvolvimento corresponde ao contrato desenhado no NexTraceOne.**

### Fluxo actual (com o gap assinalado)

```
[NexTraceOne] Contrato desenhado e aprovado
       ↓
[Developer] Implementa o serviço
       ↓
  ❌ NENHUM GATE DE VALIDAÇÃO
       ↓
[CI/CD] Deploy sem conformance check
       ↓
[Produção] Contrato real pode divergir do desenhado
```

### Fluxo alvo (após implementação)

```
[NexTraceOne] Contrato desenhado e aprovado (Locked)
       ↓ webhook notifica equipa
[Developer] Implementa guiado pelo contrato do NexTraceOne
       ↓
[CI/CD] Extrai spec implementada
       ↓ POST /contracts/validate-implementation
[NexTraceOne] Diff semântico + avaliação de política
       ↓
  ✅ Conforme → deploy avança
  ⚠️  Drift → warning, deploy continua (se política = warn)
  ❌ Breaking → pipeline bloqueado (se política = block)
       ↓
[NexTraceOne] Registo de ConformanceCheck como evidência
       ↓
[Promoção PRE→PROD] Gate avalia conformance do ambiente origem
       ↓
[Runtime] DetectContractDrift compara traces OTel com spec
       ↓
[Changelog] Linha do tempo auditável de todas as alterações
```

---

## 2. Pilares do produto reforçados

Este plano reforça directamente:

- **Contract Governance** — source of truth para o comportamento esperado
- **Change Intelligence** — conformance como gate de promoção
- **Production Change Confidence** — evidência de conformance antes do deploy
- **Source of Truth** — o NexTraceOne é a referência, não a implementação

---

## 3. Componentes a implementar

### 3.1 Domínio (novas entidades)

| Entidade | Propósito |
|----------|-----------|
| `ContractConformanceCheck` | Resultado de uma validação CI — persiste evidência |
| `ContractChangelogEntry` | Linha do tempo auditável de eventos de contrato |
| `ContractCiToken` | Token de CI com binding a serviço — resolve sem GUID manual |
| `ContractConformancePolicy` | Política de conformance por tenant/equipa/serviço/ambiente |

### 3.2 Configuração (parâmetros persistidos)

Reutilizar o módulo `Configuration` existente (`cfg_*`) com novos `ConfigurationDefinition`:

| Chave | Propósito |
|-------|-----------|
| `contracts.conformance.resolution_strategy` | Como o CI resolve o contrato activo |
| `contracts.conformance.blocking_policy` | Quando bloquear o pipeline |
| `contracts.conformance.score_threshold` | Score mínimo de conformance (0–100) |
| `contracts.conformance.required_environments` | Ambientes onde a validação é obrigatória |
| `contracts.conformance.allow_additional_endpoints` | Permite endpoints extra na implementação |
| `contracts.changelog.auto_generate` | Gera changelog automaticamente em eventos de contrato |
| `contracts.changelog.retention_days` | Retenção do changelog em dias |
| `contracts.notifications.breaking_change_alert` | Notifica consumers em breaking change |
| `contracts.notifications.channels` | Canais de notificação (email, slack, teams, webhook) |
| `contracts.ci_token.max_per_service` | Limite de tokens CI por serviço |
| `contracts.ci_token.default_expiry_days` | Expiração padrão dos tokens CI |

### 3.3 Novos endpoints

| Método | Path | Propósito |
|--------|------|-----------|
| `POST` | `/contracts/validate-implementation` | Gate principal de conformance |
| `GET` | `/contracts/{id}/conformance-history` | Histórico de checks por contrato |
| `GET` | `/services/{serviceId}/conformance-status` | Status de conformance por serviço |
| `POST` | `/contracts/ci-tokens` | Criar token CI com binding a serviço |
| `GET` | `/contracts/ci-tokens` | Listar tokens CI |
| `DELETE` | `/contracts/ci-tokens/{id}` | Revogar token CI |
| `GET` | `/contracts/{assetId}/changelog` | Changelog auditável de um contrato |
| `GET` | `/contracts/changelog/feed` | Feed global de eventos de contratos |

### 3.4 Integração CI/CD

- Convenção de ficheiro `.nextraceone.yaml` na raiz do repositório do serviço
- GitHub Action `nextraceone/contract-gate@v1`
- Suporte a Jenkins, GitLab CI, Azure DevOps via API directa

---

## 4. Decisão arquitectural principal

O endpoint de conformance **não requer ID de versão na path**. A resolução do contrato activo é feita internamente pelo `IActiveContractResolver` com a seguinte hierarquia:

```
1. CI Token com binding → resolve serviceId automaticamente
2. serviceSlug + environmentName no body → lookup por slug
3. apiAssetId explícito no body → resolve versão activa do asset
4. contractVersionId explícito → acesso directo (fallback administrativo)
```

Isto elimina o problema de equipas de CI não terem acesso a GUIDs internos do NexTraceOne.

---

## 5. Impacto nos módulos existentes

| Módulo | Impacto |
|--------|---------|
| `Contracts` | Novas features CQRS, entidades, endpoints |
| `ChangeGovernance` | `EvaluateContractComplianceGate` extendido com conformance |
| `Configuration` | Novos `ConfigurationDefinition` para parâmetros de conformance |
| `Integrations` | Webhook delivery para notificação de consumers |
| `Notifications` | Alertas de breaking change e drift |
| `Frontend` | Nova tab "Conformance" + página de gestão de CI Tokens |

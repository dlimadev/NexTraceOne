# P12.2 — Self-Hosted Enterprise Residue Removal Report

> Data: 2026-03-27 | Fase: P12.2 — Remoção definitiva de resíduos de self-hosted enterprise

---

## 1. Objetivo

Remover definitivamente todos os resíduos ativos de self-hosted enterprise do NexTraceOne — código, testes, documentação e roadmap — alinhando o repositório ao escopo atual do produto após a decisão de eliminar self-hosted enterprise como modo de entrega.

Esta fase sucede o P12.1 (remoção de Licensing) e foca exclusivamente nos resíduos ligados a self-hosted, on-prem enterprise, IIS installation, hardening self-hosted e equivalentes.

---

## 2. Inventário de Resíduos Encontrados

### 2.1 Código Backend — ValueObjects

| ID | Ficheiro | Tipo | Resíduo | Classificação |
|----|----------|------|---------|---------------|
| SH-01 | `DeploymentModel.cs` | ValueObject | Classe docs descreviam "verificação de licença online" e "Licenciamento offline obrigatório" como comportamentos diferenciadores entre SelfHosted/OnPremise | 🟡 REWRITE |
| SH-02 | `MfaPolicy.cs` | ValueObject | Métodos `ForSelfHosted()` e `ForOnPremise()` com comentários referenciando self-hosted enterprise e on-premise como deployment models | 🟡 REWRITE |
| SH-03 | `AuthenticationPolicy.cs` | ValueObject | Método `ForSelfHosted()` com comentário "para instalações self-hosted" | 🟡 REWRITE |
| SH-04 | `SessionPolicy.cs` | ValueObject | Métodos `ForSelfHosted()` e `ForOnPremise()` com comentários self-hosted/on-premise; class doc mencionava "deployment model (SaaS, self-hosted, on-premise)" | 🟡 REWRITE |
| SH-05 | `AuthenticationMode.cs` | ValueObject | Comentários mencionando "instalações on-premise air-gapped" e "instalações self-hosted que têm conectividade" | 🟡 REWRITE |
| SH-06 | `DeploymentReadinessLevel.cs` | Enum | Comentário "Utilizado para avaliação de readiness em self-hosted e on-prem" | 🟡 REWRITE |

### 2.2 Testes

| ID | Ficheiro | Tipo | Resíduo |
|----|----------|------|---------|
| SH-07 | `MfaPolicyTests.cs` | Teste | Métodos de teste `ForSelfHosted_*` e `ForOnPremise_*` chamando os métodos agora renomeados; nome de método `ForSaaS_ShouldRequireMfaForVendorOps` com "VendorOps" (resíduo P12.1 remanescente) |
| SH-08 | `AuthenticationPolicyTests.cs` | Teste | Método `ForSelfHosted_CreatesCorrectPolicy` e chamadas a `ForSelfHosted()` |
| SH-09 | `SessionPolicyTests.cs` | Teste | Métodos `ForSelfHosted_ShouldHaveRelaxedTimeouts` e `ForOnPremise_ShouldHaveMostRelaxedTimeouts` |

### 2.3 Documentação Ativa

| ID | Ficheiro | Tipo | Resíduo |
|----|----------|------|---------|
| SH-10 | `docs/ROADMAP.md` | Roadmap ativo | Onda 3 listava "Self-hosted / on-prem readiness" como item de entrega |
| SH-11 | `docs/PRODUCT-SCOPE.md` | Escopo ativo | Onda 3 "Hardening" listava "Self-hosted readiness" como item |
| SH-12 | `docs/DEPLOYMENT-ARCHITECTURE.md` | Arquitetura | Princípio declarado como "Self-hosted enterprise. On-premise first." e descrição de plataforma como "sovereign" e "on-premise" |

### 2.4 Fora de Escopo (Mantidos Intencionalmente)

| Ficheiro | Motivo de Manutenção |
|---------|---------------------|
| `docs/observability/collection/iis-clr-profiler.md` | Capacidade de **coletar telemetria de aplicações hospedadas em IIS** (observabilidade de apps de cliente), não relacionado a deployment do NexTraceOne em IIS |
| `docs/observability/collection/kubernetes-otel-collector.md` | Referências a IIS são sobre alternativas de coleta de telemetria para apps monitoradas, não sobre deployment do NexTraceOne |
| `src/building-blocks/NexTraceOne.BuildingBlocks.Observability/` | CLR Profiler é capacidade de observabilidade de apps externas, não deployment do NexTraceOne |
| `docker-compose.yml` | Compose é arquivo de deployment legítimo para dev/avaliação; PostgreSQL/ClickHouse não são ligados exclusivamente a self-hosted enterprise |
| `docs/runbooks/` | Runbooks são operacionais gerais, não específicos a self-hosted enterprise |
| `DeploymentModel.cs` (estrutura geral) | Os valores "SaaS", "SelfHosted", "OnPremise" são usados para configurar políticas de tenant; a estrutura é mantida mas as referências a licensing foram limpas |

---

## 3. Alterações Implementadas

### 3.1 Backend — Renomeação de Factory Methods

#### `MfaPolicy.cs`
- **Caminho:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/ValueObjects/MfaPolicy.cs`
- **Alterações:**
  - `ForSelfHosted()` → `ForStandardDeployment()` — comentário atualizado para "Política MFA equilibrada" sem referência a deployment
  - `ForOnPremise()` → `ForRestrictedConnectivityDeployment()` — comentário atualizado para "Política MFA permissiva"

#### `AuthenticationPolicy.cs`
- **Caminho:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/ValueObjects/AuthenticationPolicy.cs`
- **Alterações:**
  - `ForSelfHosted()` → `ForStandardDeployment()` — comentário reescrito de "para instalações self-hosted" para "para deployments com conectividade externa"

#### `SessionPolicy.cs`
- **Caminho:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/ValueObjects/SessionPolicy.cs`
- **Alterações:**
  - `ForSelfHosted()` → `ForStandardDeployment()` — comentário reescrito
  - `ForOnPremise()` → `ForRestrictedConnectivityDeployment()` — comentário reescrito
  - Classe doc: removida referência a "deployment model (SaaS, self-hosted, on-premise)"

### 3.2 Backend — Reescrita de Comentários

#### `DeploymentModel.cs`
- **Alterações:**
  - Classe doc: removidas referências a "verificação de licença online" e "Licenciamento offline obrigatório"
  - `SelfHosted` static: comentário reescrito para descrever "ambiente com conectividade externa disponível"
  - `OnPremise` static: comentário reescrito para "ambiente air-gapped ou altamente restrito, sem telemetria remota"
  - `AllowsExternalConnectivity`: removida menção a "verificação de licença"

#### `AuthenticationMode.cs`
- **Alterações:**
  - Classe doc `Local`: removida referência a "on-premise air-gapped"
  - Classe doc `Hybrid`: removida referência a "instalações self-hosted"
  - Static `Local`: comentário reescrito para "contextos sem acesso a provedores de identidade externos"

#### `DeploymentReadinessLevel.cs`
- **Alterações:**
  - Comentário reescrito de "Utilizado para avaliação de readiness em self-hosted e on-prem" para "Utilizado para avaliação de readiness em ambientes gerenciados"

### 3.3 Testes — Renomeação e Atualização

#### `MfaPolicyTests.cs`
- `ForSaaS_ShouldRequireMfaForVendorOps` → `ForSaaS_ShouldRequireMfaForSensitiveExternalOps` **(fix P12.1 remanescente)**
- `ForSelfHosted_ShouldNotRequireMfaOnLogin` → `ForStandardDeployment_ShouldNotRequireMfaOnLogin`
- `ForSelfHosted_ShouldRequireMfaForPrivilegedOps` → `ForStandardDeployment_ShouldRequireMfaForPrivilegedOps`
- `ForOnPremise_ShouldNotRequireAnyMfa` → `ForRestrictedConnectivityDeployment_ShouldNotRequireAnyMfa`
- Chamadas a `MfaPolicy.ForSelfHosted()` → `MfaPolicy.ForStandardDeployment()`
- Chamadas a `MfaPolicy.ForOnPremise()` → `MfaPolicy.ForRestrictedConnectivityDeployment()`
- Comentário de classe atualizado

#### `AuthenticationPolicyTests.cs`
- `ForSelfHosted_CreatesCorrectPolicy` → `ForStandardDeployment_CreatesCorrectPolicy`
- Chamadas a `AuthenticationPolicy.ForSelfHosted()` → `AuthenticationPolicy.ForStandardDeployment()`
- Comentário de classe atualizado

#### `SessionPolicyTests.cs`
- `ForSelfHosted_ShouldHaveRelaxedTimeouts` → `ForStandardDeployment_ShouldHaveBalancedTimeouts`
- `ForOnPremise_ShouldHaveMostRelaxedTimeouts` → `ForRestrictedConnectivityDeployment_ShouldHavePermissiveTimeouts`
- Chamadas a `SessionPolicy.ForSelfHosted()` → `SessionPolicy.ForStandardDeployment()`
- Chamadas a `SessionPolicy.ForOnPremise()` → `SessionPolicy.ForRestrictedConnectivityDeployment()`
- Comentário de classe atualizado

### 3.4 Documentação Ativa

#### `docs/ROADMAP.md`
- **Alteração:** Onda 3 removido "Self-hosted / on-prem readiness" da lista de entregas
- **Título da Onda:** "Hardening e operação enterprise" → "Hardening e operação"

#### `docs/PRODUCT-SCOPE.md`
- **Alteração:** Onda 3 removido "Self-hosted readiness" da lista de itens de Hardening

#### `docs/DEPLOYMENT-ARCHITECTURE.md`
- **Alteração:**
  - Princípio do documento: "Self-hosted enterprise. On-premise first." → "Independência de serviços cloud externos obrigatórios."
  - Descrição "sovereign" e "on-premise" reescrita para focar em independência de cloud externos
  - "servir via nginx ou CDN on-premise" → "servir via nginx ou CDN"

---

## 4. Validação Funcional

### 4.1 Compilação

- `src/modules/identityaccess/` → ✅ Build succeeded (0 errors)
- `src/modules/governance/NexTraceOne.Governance.Domain/` → ✅ Build succeeded (0 errors)

### 4.2 Testes

- `NexTraceOne.IdentityAccess.Tests`: **315 testes — 0 falhas** ✅
  - `MfaPolicyTests`: Todos os testes passam com novos nomes
  - `AuthenticationPolicyTests`: Todos os testes passam com novos nomes
  - `SessionPolicyTests`: Todos os testes passam com novos nomes
  - `DeploymentModelTests`: Todos os testes passam (os valores "SaaS", "SelfHosted", "OnPremise" como strings de configuração de tenant foram mantidos)

### 4.3 Consistência de API

- Nenhum método renomeado era utilizado em código de produção (verificado por `grep -rn "\.ForSelfHosted\|\.ForOnPremise"` em `src/`)
- Apenas usados em testes — zero impacto em endpoints, handlers ou DI

---

## 5. Ficheiros Alterados

| Ficheiro | Tipo | Ação |
|---------|------|------|
| `src/modules/identityaccess/.../ValueObjects/DeploymentModel.cs` | ValueObject | Reescrita de comentários (remove licensing/self-hosted enterprise) |
| `src/modules/identityaccess/.../ValueObjects/MfaPolicy.cs` | ValueObject | Rename ForSelfHosted→ForStandardDeployment, ForOnPremise→ForRestrictedConnectivityDeployment |
| `src/modules/identityaccess/.../ValueObjects/AuthenticationPolicy.cs` | ValueObject | Rename ForSelfHosted→ForStandardDeployment |
| `src/modules/identityaccess/.../ValueObjects/SessionPolicy.cs` | ValueObject | Rename factory methods + class doc rewrite |
| `src/modules/identityaccess/.../ValueObjects/AuthenticationMode.cs` | ValueObject | Reescrita de comentários (remove "air-gapped", "self-hosted") |
| `src/modules/governance/.../Enums/DeploymentReadinessLevel.cs` | Enum | Reescrita de comentário |
| `tests/.../ValueObjects/MfaPolicyTests.cs` | Teste | Update nomes de métodos e chamadas |
| `tests/.../ValueObjects/AuthenticationPolicyTests.cs` | Teste | Update nomes de métodos e chamadas |
| `tests/.../ValueObjects/SessionPolicyTests.cs` | Teste | Update nomes de métodos e chamadas |
| `docs/ROADMAP.md` | Documentação ativa | Remove "Self-hosted / on-prem readiness" de Onda 3 |
| `docs/PRODUCT-SCOPE.md` | Documentação ativa | Remove "Self-hosted readiness" de Onda 3 |
| `docs/DEPLOYMENT-ARCHITECTURE.md` | Documentação ativa | Reescrita do posicionamento "sovereign/on-premise first" |

---

## 6. Separação de Escopo — P12.1 vs P12.2

| Categoria | P12.1 (Licensing) | P12.2 (Self-Hosted Enterprise) |
|-----------|-------------------|-------------------------------|
| Permissões `licensing:*` | ✅ Removidas | — |
| `RequiredForVendorOps` → `RequiredForSensitiveExternalOps` | ✅ Renomeado | — |
| Breadcrumbs/navigation licensing | ✅ Removidos | — |
| i18n licensing | ✅ Limpo | — |
| Factory methods `ForSelfHosted/ForOnPremise` | — | ✅ Renomeados |
| Comments "on-premise air-gapped", "self-hosted" | — | ✅ Reescritos |
| ROADMAP "Self-hosted readiness" | — | ✅ Removido |
| DEPLOYMENT-ARCHITECTURE "sovereign/on-premise first" | — | ✅ Reescrito |
| Comentários de licença em DeploymentModel | — | ✅ Reescritos |

---

## 7. Conclusão

Todos os resíduos ativos de self-hosted enterprise identificados foram removidos ou reescritos. O código compila sem erros e todos os 315 testes de Identity Access passam. A documentação ativa não promove mais self-hosted enterprise como modo de entrega ou roadmap válido.

Material de observabilidade relacionado a IIS (coleta de telemetria de apps .NET hospedadas em IIS) foi conscientemente mantido pois representa capacidade de observabilidade de apps de cliente, não deployment do NexTraceOne em IIS.

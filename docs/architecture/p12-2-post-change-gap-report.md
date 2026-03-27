# P12.2 — Post-Change Gap Report

> Data: 2026-03-27 | Fase: P12.2 — Remoção de resíduos de self-hosted enterprise

---

## 1. O Que Foi Resolvido

### 1.1 Código Backend
- ✅ Factory methods `ForSelfHosted()` e `ForOnPremise()` renomeados em todos os 3 ValueObjects afetados (MfaPolicy, AuthenticationPolicy, SessionPolicy)
- ✅ Comentários de classe e de método reescritos em MfaPolicy, AuthenticationPolicy, SessionPolicy, AuthenticationMode, DeploymentModel e DeploymentReadinessLevel
- ✅ Referências a "verificação de licença online" e "Licenciamento offline obrigatório" removidas de DeploymentModel
- ✅ Referências a "on-premise air-gapped" e "instalações self-hosted" removidas de AuthenticationMode

### 1.2 Testes
- ✅ 8 métodos de teste renomeados para eliminar "ForSelfHosted", "ForOnPremise" e "VendorOps" nos nomes
- ✅ Chamadas aos métodos renomeados atualizadas em 3 ficheiros de teste
- ✅ 315 testes Identity Access passam sem falhas

### 1.3 Documentação Ativa
- ✅ `docs/ROADMAP.md` — removido "Self-hosted / on-prem readiness" e "enterprise" do título de Onda 3
- ✅ `docs/PRODUCT-SCOPE.md` — removido "Self-hosted readiness" da Onda 3
- ✅ `docs/DEPLOYMENT-ARCHITECTURE.md` — princípio "Self-hosted enterprise. On-premise first." e descrição "sovereign/on-premise" reescritos

### 1.4 Resíduo P12.1 Remanescente
- ✅ Método de teste `ForSaaS_ShouldRequireMfaForVendorOps` renomeado para `ForSaaS_ShouldRequireMfaForSensitiveExternalOps` (fix pendente do P12.1 tratado nesta fase)

---

## 2. O Que Ficou Pendente

### 2.1 Documentação de Observabilidade IIS — Mantida Intencionalmente

**Ficheiros:**
- `docs/observability/collection/iis-clr-profiler.md`
- `docs/observability/collection/kubernetes-otel-collector.md` (menção a IIS como alternativa)

**Motivo para manutenção:** Estes documentos descrevem a capacidade de **coletar telemetria de aplicações .NET hospedadas em IIS** — ou seja, monitorar apps de cliente que correm em IIS. Não descrevem deployment do NexTraceOne em IIS. Esta é uma capacidade de observabilidade legítima.

**Decisão:** Mantidos fora do escopo desta fase. Se a capacidade de coletar telemetria de apps IIS for descontinuada, os documentos devem ser tratados numa fase separada de revisão de capacidades de observabilidade.

### 2.2 DeploymentModel — Estrutura Mantida

**Ficheiro:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/ValueObjects/DeploymentModel.cs`

**Estado:** Os valores `SelfHosted` e `OnPremise` foram mantidos como configurações válidas de tenant. Os comentários foram reescritos para remover ligações a licensing e self-hosted enterprise como modo de entrega.

**Pendência:** Se no futuro for decidido que o conceito de "deployment model" por tenant também não faz sentido, o ValueObject pode ser simplificado ou removido. Decisão fora do escopo do P12.2.

### 2.3 Scripts PowerShell de Migração

**Ficheiro:** `scripts/db/apply-migrations.ps1`

**Estado:** Script existe com suporte explícito a Windows/PowerShell. Não foi alterado pois scripts de migração de base de dados são utilitários técnicos válidos independentemente do modelo de deployment.

**Pendência:** Se for decidido descontinuar suporte a Windows como ambiente de execução, o script pode ser removido. Fora do escopo do P12.2.

---

## 3. O Que Fica Para P12.3

### 3.1 Limpeza de Documentação Histórica (Se Necessário)

Os ficheiros de auditoria em `docs/audits/2026-03-25/` (incluindo `licensing-selfhosted-readiness-report.md`) são relatórios históricos de auditoria, não documentação ativa de produto. Devem permanecer como registo histórico a menos que a decisão seja arquivá-los explicitamente.

### 3.2 Revisão de Documentação de Deployment para Alinhamento Completo

A documentação em `docs/deployment/` cobre Docker, ambientes, CI/CD. Não foi identificado conteúdo explicitamente ligado a self-hosted enterprise que precisasse de remoção, mas uma revisão mais profunda pode identificar fraseamentos que assumam esse modo de operação.

### 3.3 Validação de Consistência Cross-Modular

Verificar se os novos nomes de factory methods (`ForStandardDeployment`, `ForRestrictedConnectivityDeployment`) são usados de forma consistente quando novos módulos ou features criem políticas de tenant por omissão.

---

## 4. Limitações Residuais

| Área | Limitação | Impacto |
|------|-----------|---------|
| DeploymentModel string values | "SelfHosted" e "OnPremise" continuam como valores válidos para configuração de tenant | Baixo — é configuração interna, não promessa de produto |
| Scripts de migration PowerShell | `apply-migrations.ps1` ainda existe | Baixo — utilitário técnico neutro |
| Docs IIS CLR Profiler | `iis-clr-profiler.md` mantido como capacidade de observabilidade | Zero — é capacidade de observabilidade de apps externas, não deployment |

---

## 5. Estado do Repositório Após P12.2

| Categoria | Estado |
|-----------|--------|
| Referências ativas a "self-hosted enterprise" no código | ✅ Removidas |
| Referências ativas a "on-prem enterprise" no código | ✅ Removidas |
| ROADMAP com "self-hosted readiness" | ✅ Removido |
| PRODUCT-SCOPE com "self-hosted readiness" | ✅ Removido |
| DEPLOYMENT-ARCHITECTURE "sovereign/on-premise first" | ✅ Reescrito |
| Factory methods com nomes de deployment model | ✅ Renomeados para nomes neutros |
| Comentários com "air-gapped", "instalações self-hosted" | ✅ Reescritos |
| Build sem erros | ✅ Confirmado |
| Testes passando | ✅ 315 testes — 0 falhas |

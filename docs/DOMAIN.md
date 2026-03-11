# NexTraceOne — Domínio de Negócio

## 1. Taxonomia de Mudanças

A plataforma classifica TODA mudança em 5 níveis. O nível determina o fluxo de governança.

### Nível 0 — Eventos Operacionais

Mudanças sem impacto em contrato ou comportamento.

- `ScalingEvent` — auto-scaling, réplicas
- `InfrastructureChange` — mudança de infra sem impacto funcional
- `ConfigurationChange` — feature flags, parâmetros
- `CertificateRenewal` — renovação de certificados

**Governança:** Sem versão, sem workflow. Apenas AuditEvent registrado.

### Nível 1 — Mudança sem alteração de contrato

Correções e melhorias internas que não alteram a interface pública.

- `BugFixChange` — correção de bug
- `PerformanceChange` — otimização de performance
- `DependencyUpdate` — atualização de dependência
- `LoggingChange` — mudança em logging
- `SecurityPatchChange` — patch de segurança

**Governança:** Patch version (x.x.PATCH). Workflow leve.

### Nível 2 — Mudança com contrato Non-Breaking

Adições ao contrato que não quebram consumidores existentes.

- `AdditiveChange` — novo endpoint, campo opcional adicionado
- `DocumentationChange` — mudança em documentação do contrato
- `DeprecationMark` — marcação de deprecação (sem remoção)
- `EnhancementChange` — melhoria funcional compatível

**Governança:** Minor version (x.MINOR.0). Workflow padrão. Notificação informativa.

### Nível 3 — Mudança com contrato Breaking

Mudanças que QUEBRAM consumidores existentes. Máxima governança.

- `BreakingStructuralChange` — campo removido, tipo alterado
- `BreakingBehavioralChange` — semântica diferente no mesmo endpoint
- `BreakingSecurityChange` — novo requisito de autenticação
- `BreakingContractualChange` — SLA alterado, rate limit diferente

**Governança:** MAJOR version (MAJOR.0.0). Workflow COMPLETO. Comunicação formal obrigatória a todos os consumidores afetados.

### Nível 4 — Eventos de Publicação

Eventos de ciclo de vida de deploy.

- `EnvironmentPromotion` — promoção entre ambientes (dev → staging → prod)
- `Redeployment` — redeploy sem mudança de versão
- `RollbackEvent` — rollback para versão anterior
- `InitialDeployment` — primeiro deploy de um serviço

**Governança:** Sem nova versão (exceto InitialDeployment). Workflow de promoção.

---

## 2. Discovery de Dependências (5 camadas)

A plataforma descobre dependências entre serviços por 5 métodos complementares:

### Camada 1 — Contrato (Estático)

Análise de especificações OpenAPI:
- `servers` → URLs base de dependência
- `securitySchemes` → requisitos de autenticação
- `$ref` externos → referências cross-API
- `x-nextraceone.consumers` → extensão custom declarando consumidores

### Camada 2 — Runtime (OpenTelemetry)

OTLP endpoint próprio que recebe traces:
- Análise de spans → identifica chamadas HTTP entre serviços
- `DiscoveryConfidence: High` — evidência real de tráfego
- Detecção de dependências não declaradas

### Camada 3 — Sinal de Borda/Gateway

Análise de gateways e proxies:
- Kong Admin API
- AWS API Gateway
- Azure APIM
- Nginx/IIS access logs

### Camada 4 — Importação de Plataformas

Integração com catálogos existentes:
- Backstage Catalog API
- Consul service registry
- Kubernetes API (Service/Ingress)
- Dynatrace topology

### Camada 5 — Análise de Código (Estático)

Scan de repositórios:
- `appsettings.json` → URLs de serviços
- `HttpClient` declarations → dependências HTTP
- `docker-compose.yml` → serviços colocados
- Pact contracts → consumer-driven contracts

### Modelo de Confiança

| Nível | Significado |
|-------|------------|
| **Confirmed** | Validado manualmente por humano |
| **High** | Detectado via OpenTelemetry com volume significativo |
| **Medium** | Detectado via análise estática de contratos |
| **Low** | Detectado em logs de gateway com baixo volume |
| **Inferred** | Inferido por análise de código ou heurística |

---

## 3. Integrações CI/CD

### CI/CD → NexTraceOne (webhook de deploy)

```
POST /api/v1/change-intelligence/deployments/notify
```

```json
{
  "serviceName": "order-api",
  "releaseVersion": "2.4.0",
  "environment": "pre-production",
  "status": "Succeeded",
  "gitCommitSha": "abc123def456",
  "pipelineId": "pipe-456",
  "workItems": [
    { "id": "NXT-123", "type": "Story" },
    { "id": "NXT-456", "type": "Bug" }
  ]
}
```

**Plugins nativos:** Jenkins, GitHub Actions, GitLab CI, Azure DevOps + CLI universal (`nex deploy notify`).

### Task Management → NexTraceOne

Plataformas suportadas: Jira, Azure DevOps, GitHub Issues, Linear, ClickUp.

`WorkItemContext` como Value Object dentro de `Release`:
- ID do work item
- Tipo (Story, Bug, Task, Epic)
- URL do item na plataforma de origem
- Status sincronizado

### NexTraceOne → Task Management (write-back)

- Comentário automático ao aprovar workflow
- Transição de status (ex: "In Review" → "Approved")
- Criação de bug quando breaking change detectada sem história associada

---

## 4. Fluxo Principal (Happy Path)

```
[CI/CD Pipeline]
      │
      ▼ POST /deployments/notify
[ChangeIntelligence]
      │
      ├─ ClassifyChangeLevel() → Determina nível 0-4
      │
      ├─ CalculateBlastRadius() → Consulta EngineeringGraph
      │                           para consumidores afetados
      │
      ├─ ComputeChangeScore() → Score de risco normalizado
      │
      ├─ Se nível ≥ 2:
      │   └─ [Workflow] InitiateWorkflow()
      │       ├─ Coleta evidências (EvidencePack)
      │       ├─ Encaminha para aprovadores
      │       └─ Monitora SLA de aprovação
      │
      ├─ Se aprovado:
      │   └─ [Promotion] CreatePromotionRequest()
      │       ├─ EvaluatePromotionGates()
      │       └─ Se gates passam → Promoção autorizada
      │
      └─ [Audit] RecordAuditEvent()
          └─ Hash chain para integridade
```

---

## 5. Conceitos-Chave

### Blast Radius

O "raio de explosão" de uma mudança — quantos e quais consumidores são afetados.
Calculado percorrendo o grafo de dependências (EngineeringGraph) de forma transitiva.
Score normalizado de 0.0 (sem impacto) a 1.0 (todos os consumidores afetados).

### Evidence Pack

Pacote de evidências montado automaticamente para aprovadores:
- Diff do contrato (o que mudou)
- Blast radius report (quem é afetado)
- Change score (nível de risco)
- Work items associados
- Resultados de linting (RulesetGovernance)
- Health metrics do runtime (se disponível)

### Promotion Gates

Checkpoints automáticos que devem passar antes de promover para o próximo ambiente:
- Linting passou? (RulesetGovernance)
- Testes passaram? (CI/CD webhook)
- Blast radius aceitável?
- Workflow aprovado?
- SLA de observabilidade cumprido?
- Budget de custo respeitado?

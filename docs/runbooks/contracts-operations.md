# Runbook Operacional: Gestão de Contratos

**Módulo:** Contracts (Catalog)  
**Versão:** 1.0  
**Última atualização:** Abril 2026  
**Responsável:** Platform Engineering / Tech Leads

---

## 1. Emergência: Correção de Contrato Bloqueado

### Contexto

Um contrato no estado `Locked` está protegido contra alterações. Em situações excepcionais (ex: erro crítico de segurança, vulnerabilidade no schema publicado), pode ser necessário desbloquear e corrigir o contrato em produção.

### Pré-requisitos

- Acesso com role `Platform Admin` ou `Break Glass`
- Aprovação de pelo menos um Tech Lead do domínio afetado
- Ticket de incidente aberto e correlacionado à mudança

### Passos

1. **Abrir ticket de incidente** no sistema de gestão de incidentes com justificativa clara.
2. **Ativar Break Glass Access** via endpoint administrativo:
   ```
   POST /api/v1/admin/access/break-glass
   { "justification": "Correção crítica de segurança no contrato X", "ttlMinutes": 60 }
   ```
3. **Verificar o estado atual** do contrato:
   ```
   GET /api/v1/catalog/contracts/{contractVersionId}
   ```
4. **Criar nova versão de contrato** com a correção (não editar a versão bloqueada diretamente):
   - Criar draft com `POST /api/v1/catalog/contracts/drafts`
   - Aplicar a correção no conteúdo
   - Submeter para revisão imediata com aprovação de emergência
5. **Publicar a nova versão** via fluxo de aprovação comprimido (Emergency Approval).
6. **Registar evidência** no Evidence Pack da nova versão com referência ao incidente.
7. **Revogar Break Glass Access** imediatamente após conclusão.
8. **Notificar consumers** conhecidos via canal de comunicação do serviço afetado.

### Riscos

- Consumers podem estar a utilizar a versão bloqueada; a nova versão pode quebrar compatibilidade.
- Verificar blast radius antes de publicar:
  ```
  GET /api/v1/catalog/contracts/{contractVersionId}/blast-radius
  ```

---

## 2. Emergência: Rollback de Versão de Contrato

### Contexto

Após uma publicação, foi detetado um problema crítico (regressão, breaking change não prevista, falha de conformidade). É necessário reverter para a versão anterior do contrato.

### Pré-requisitos

- Existir uma versão anterior válida (`Locked` ou `Approved`) no histórico do ativo
- Acesso com role `Tech Lead` ou superior no domínio
- Incidente correlacionado aberto

### Passos

1. **Identificar a versão anterior** estável do contrato:
   ```
   GET /api/v1/catalog/contracts?apiAssetId={apiAssetId}&orderBy=createdAt&direction=desc
   ```
2. **Verificar compatibilidade** da versão anterior com consumers ativos:
   ```
   GET /api/v1/catalog/contracts/{previousVersionId}/consumer-expectations
   ```
3. **Criar nova versão** copiando o conteúdo da versão anterior (não reutilizar diretamente):
   ```
   POST /api/v1/catalog/contracts/drafts
   { "title": "Rollback para v{N}", "specContent": "{conteúdo da versão anterior}", ... }
   ```
4. **Registar proveniência** com `importedFrom: "rollback-from-{previousVersionId}"` para rastreabilidade.
5. **Executar fluxo de aprovação de emergência** (ver secção 3).
6. **Publicar** a versão de rollback.
7. **Deprecar a versão problemática** com aviso de rollback:
   ```
   POST /api/v1/catalog/contracts/{problemVersionId}/deprecate
   { "notice": "Revertido para v{N} devido a [incidente]. Ver ticket #XXX." }
   ```
8. **Registar correlação** entre a versão problemática e o incidente no Change Intelligence.

### Notas

- Nunca eliminar versões de contrato — apenas deprecar.
- Preservar trilha de auditoria completa para conformidade.

---

## 3. Rotina: Gestão do Ciclo de Vida de Contratos

### Fluxo completo: Draft → InReview → Approved → Locked

#### 3.1 Criar Draft

```
POST /api/v1/catalog/contracts/drafts
{
  "title": "User Management API v2",
  "author": "engineer@company.com",
  "contractType": "RestApi",
  "protocol": "OpenApi"
}
```

#### 3.2 Adicionar conteúdo ao Draft

```
PUT /api/v1/catalog/contracts/drafts/{draftId}/content
{
  "specContent": "openapi: 3.1.0 ...",
  "format": "yaml",
  "updatedBy": "engineer@company.com"
}
```

#### 3.3 Submeter para revisão

```
POST /api/v1/catalog/contracts/drafts/{draftId}/submit
{ "submittedBy": "engineer@company.com" }
```

#### 3.4 Aprovar (Tech Lead / Reviewer)

```
POST /api/v1/catalog/contracts/drafts/{draftId}/approve
{ "approvedBy": "techlead@company.com", "notes": "Aprovado após revisão de segurança." }
```

#### 3.5 Publicar (criar ContractVersion)

```
POST /api/v1/catalog/contracts/drafts/{draftId}/publish
{ "publishedBy": "techlead@company.com" }
```

#### 3.6 Bloquear versão publicada (imutabilidade)

```
POST /api/v1/catalog/contracts/{contractVersionId}/lock
{ "lockedBy": "admin@company.com" }
```

### Checklist pré-publicação

- [ ] Conteúdo da especificação validado (sem erros de linting)
- [ ] Exemplos adicionados para endpoints principais
- [ ] Ownership e equipa confirmados no ativo de API
- [ ] Consumer expectations registadas pelos consumidores conhecidos
- [ ] SLA definido se o contrato for crítico

---

## 4. Incidente: Breaking Change Detetada

### Contexto

O sistema de diff semântico detetou uma breaking change entre a versão atual e uma versão anterior. Consumers podem estar em risco.

### Passos

1. **Verificar o diff** que acionou o alerta:
   ```
   GET /api/v1/catalog/contracts/{contractVersionId}/diffs
   ```
2. **Identificar consumers afetados**:
   ```
   GET /api/v1/catalog/contracts/{contractVersionId}/consumer-expectations
   GET /api/v1/catalog/graph/apis/{apiAssetId}/consumers
   ```
3. **Calcular blast radius**:
   ```
   GET /api/v1/catalog/contracts/{contractVersionId}/blast-radius
   ```
4. **Notificar imediatamente** os Tech Leads dos serviços consumidores identificados.
5. **Avaliar opções**:
   - **Opção A**: Rollback (ver secção 2) se a breaking change não foi intencional.
   - **Opção B**: Manter a nova versão e coordenar migração com consumers.
   - **Opção C**: Criar versão de compatibilidade paralela (versioning por URL: `/api/v2/...`).
6. **Registar decisão** como Operational Note no contrato afetado.
7. **Correlacionar com mudanças recentes** no Change Intelligence:
   ```
   GET /api/v1/changes?serviceId={serviceId}&environment=production&from={timestamp}
   ```
8. **Acompanhar** a verificação pós-change nos ambientes não produtivos antes de promover correção.

### Critérios de severidade

| Tipo de Breaking Change | Severidade | Ação Recomendada |
|------------------------|-----------|-----------------|
| Remoção de campo obrigatório | Crítica | Rollback imediato |
| Mudança de tipo de campo | Alta | Notificar + avaliar rollback |
| Remoção de endpoint | Alta | Notificar consumers e coordenar |
| Mudança de schema de resposta | Média | Notificar + janela de migração |
| Alteração de enum values | Média | Notificar consumers |

---

## 5. Manutenção: Auditoria de Consumer Expectations

### Objetivo

Verificar periodicamente se as expectativas registadas pelos consumers ainda são válidas face ao contrato publicado atual.

### Frequência recomendada

- Antes de cada publicação de nova versão principal
- Mensalmente para contratos de alta criticidade
- Após qualquer breaking change detetada

### Passos

1. **Listar todas as expectativas ativas** para o contrato:
   ```
   GET /api/v1/catalog/contracts/{apiAssetId}/consumer-expectations?isActive=true
   ```

2. **Para cada expectativa**, verificar compatibilidade:
   - Comparar `ExpectedSubsetJson` com o schema atual da versão publicada
   - Identificar campos removidos, renomeados ou com tipo alterado

3. **Executar validação automática** (se disponível no ambiente):
   ```
   POST /api/v1/catalog/contracts/{contractVersionId}/validate-expectations
   ```

4. **Contactar owners dos consumers** para expectativas desatualizadas:
   - Usar `ConsumerServiceName` e `ConsumerDomain` para identificar equipa responsável
   - Fornecer diff entre expectativa e contrato atual

5. **Marcar expectativas obsoletas** como inativas:
   ```
   PATCH /api/v1/catalog/contracts/consumer-expectations/{id}
   { "isActive": false, "deactivationReason": "Contrato v2 publicado em {data}" }
   ```

6. **Registar resultado da auditoria** como Operational Note no Knowledge Hub.

### Indicadores de alerta

- Expectativa sem atualização há mais de 90 dias para contrato em evolução ativa
- Consumer com `ConsumerDomain` que não existe no Service Catalog
- Expectativa referencia campos que foram removidos na versão atual

---

## Referências

- [Contract Governance — Visão geral](../CONTRACT-STUDIO-VISION.md)
- [Service Governance](../SERVICE-CONTRACT-GOVERNANCE.md)
- [Change Intelligence](../CHANGE-CONFIDENCE.md)
- [ADR-004 — Consumer-Driven Contract Testing](../adr/004-consumer-driven-contract-testing.md)
- [Runbook de Rollback geral](./ROLLBACK-RUNBOOK.md)
- [Runbook de Resposta a Incidentes](./INCIDENT-RESPONSE-PLAYBOOK.md)

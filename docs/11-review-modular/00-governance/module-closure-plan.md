# Plano de Fecho por Módulo — NexTraceOne

> **Classificação:** PLANO DE FECHO MODULAR  
> **Data de referência:** Julho 2025  
> **Escopo:** Mini-plano de fecho para cada módulo prioritário  
> **Metodologia:** 7 etapas padronizadas por módulo

---

## 1. Metodologia de Fecho

Cada módulo segue a mesma sequência de 7 etapas:

```
1. Fix Security/Permission ──► Segurança primeiro
2. Fix Persistence/Backend ──► Dados e lógica
3. Fix Frontend Integration ──► UI conectada a dados reais
4. Fix UX/i18n ──────────────► Experiência localizada
5. Fix Documentation ────────► Docs alinhados com código
6. Validate AI/Agents ───────► (se aplicável)
7. Close Final Checklist ────► Validação completa
```

---

## 2. Módulo Contracts — PRIORIDADE MÁXIMA (P0)

**Maturidade actual:** 68% | **Alvo:** 85%+

### Etapa 1: Security/Permission
- [ ] Verificar que permissões de leitura/escrita de contratos estão enforced
- [ ] Validar que Contract Studio respeita roles
- [ ] Confirmar que versionamento respeita ownership

### Etapa 2: Persistence/Backend
- [ ] Completar handlers de Contract Studio (create, edit, version)
- [ ] Implementar validação de compatibilidade (breaking change detection)
- [ ] Completar fluxo de approval workflow
- [ ] Garantir soft delete e audit em todas as entidades

### Etapa 3: Frontend Integration
- [ ] **P0: Corrigir 3 rotas partidas (governance, spectral, canonical)**
- [ ] Conectar Contract Studio a endpoints reais
- [ ] Implementar diff view funcional
- [ ] Conectar approval workflow UI a backend

### Etapa 4: UX/i18n
- [ ] Verificar i18n completo em todas as páginas de Contracts
- [ ] Implementar empty states com mensagens i18n
- [ ] Garantir loading states em todas as views

### Etapa 5: Documentation
- [ ] Criar README do módulo Contracts
- [ ] Documentar modelo de domínio (entidades, value objects, aggregates)
- [ ] Documentar fluxos de aprovação e versionamento

### Etapa 6: AI/Agents
- [ ] Verificar que agente de contratos consegue gerar sugestões
- [ ] Testar geração assistida de schemas

### Etapa 7: Checklist Final
- [ ] Todas as rotas navegáveis sem erro
- [ ] CRUD completo para REST, SOAP, Kafka contracts
- [ ] Versionamento funcional
- [ ] Diff view funcional
- [ ] Testes ≥80%
- [ ] Maturidade ≥85%

**Critério de aceitação mínimo:** Rotas corrigidas, Contract Studio funcional para REST APIs, versionamento básico.

---

## 3. Módulo Identity & Access — PRIORIDADE ALTA

**Maturidade actual:** 82% | **Alvo:** 90%+

### Etapa 1: Security/Permission
- [ ] **Implementar MFA enforcement em runtime**
- [ ] **Migrar API key storage para BD encriptada**
- [ ] Validar JIT Access, Break Glass, Delegation em cenários reais
- [ ] Testar Access Review workflow completo

### Etapa 2: Persistence/Backend
- [ ] Adicionar RowVersion em User, Tenant, Role entities
- [ ] Garantir seed completo de roles e permissões de produção
- [ ] Validar OIDC flow end-to-end

### Etapa 3: Frontend Integration
- [ ] Verificar que MFA setup UI funciona
- [ ] Confirmar API key management UI conectada a novo storage
- [ ] Testar fluxo completo de convite de utilizador

### Etapa 4: UX/i18n
- [ ] Verificar i18n em login, MFA, settings
- [ ] Garantir mensagens de erro de autenticação localizadas
- [ ] Empty states em listas de users, roles, permissions

### Etapa 5: Documentation
- [ ] Criar README do módulo Identity & Access
- [ ] Documentar fluxos de autenticação (JWT, OIDC, API Key)
- [ ] Documentar modelo de permissões

### Etapa 6: AI/Agents
- [ ] N/A para este módulo (segurança manual obrigatória)

### Etapa 7: Checklist Final
- [ ] MFA enforced para utilizadores configurados
- [ ] API keys em BD encriptada
- [ ] Todos os fluxos de autenticação testados
- [ ] Testes ≥85%
- [ ] Maturidade ≥90%

**Critério de aceitação mínimo:** MFA enforced, API key migrada, OIDC funcional.

---

## 4. Módulo Configuration — PRIORIDADE MÉDIA-ALTA

**Maturidade actual:** 77% | **Alvo:** 85%+

### Etapa 1: Security/Permission
- [ ] Verificar que permissões de leitura/escrita de configurações estão granulares
- [ ] Validar isolamento por environment

### Etapa 2: Persistence/Backend
- [ ] **Criar migrações EF Core para o módulo**
- [ ] Adicionar RowVersion em entidades de configuração
- [ ] Validar que feature flags persistem correctamente

### Etapa 3: Frontend Integration
- [ ] Verificar que UI de configuração lê dados via API (não hardcoded)
- [ ] Confirmar que alterações via UI persistem

### Etapa 4: UX/i18n
- [ ] Verificar i18n em todas as páginas de configuração
- [ ] Garantir confirmação antes de alterações críticas

### Etapa 5: Documentation
- [ ] **Consolidar 35 ficheiros fragmentados num README + sub-docs organizados**
- [ ] Documentar feature flags disponíveis
- [ ] Documentar variáveis por environment

### Etapa 6: AI/Agents
- [ ] N/A

### Etapa 7: Checklist Final
- [ ] Migrações existem e aplicam-se correctamente
- [ ] Feature flags funcionais end-to-end
- [ ] Docs consolidados e navegáveis
- [ ] Testes ≥95% (manter)
- [ ] Maturidade ≥85%

**Critério de aceitação mínimo:** Migrações criadas, docs consolidados.

---

## 5. Módulo Audit & Compliance — PRIORIDADE ALTA

**Maturidade actual:** 53% | **Alvo:** 70%+

### Etapa 1: Security/Permission
- [ ] Verificar que audit trail não é editável
- [ ] Confirmar que acesso a logs de auditoria requer permissão específica
- [ ] Garantir que dados sensíveis são mascarados nos logs

### Etapa 2: Persistence/Backend
- [ ] Completar handlers de relatórios de compliance
- [ ] Garantir retenção configurável de audit trail
- [ ] Adicionar RowVersion em entidades de compliance

### Etapa 3: Frontend Integration
- [ ] **Elevar frontend de 40% para 70%+**
- [ ] Implementar visualização de audit trail com filtros
- [ ] Implementar dashboard de compliance
- [ ] Conectar relatórios a endpoints backend

### Etapa 4: UX/i18n
- [ ] Verificar i18n em todas as novas páginas
- [ ] Implementar export de relatórios (CSV, PDF)
- [ ] Empty states informativos

### Etapa 5: Documentation
- [ ] Criar README do módulo
- [ ] Documentar tipos de eventos auditados
- [ ] Documentar relatórios de compliance disponíveis

### Etapa 6: AI/Agents
- [ ] Verificar se agente consegue consultar audit trail
- [ ] Testar queries de compliance via AI

### Etapa 7: Checklist Final
- [ ] Audit trail visível e filtrável no frontend
- [ ] Relatórios de compliance funcionais
- [ ] Frontend ≥70%
- [ ] Testes ≥65%
- [ ] Maturidade ≥70%

**Critério de aceitação mínimo:** Frontend funcional com visualização de audit trail.

---

## 6. Módulo Governance — PRIORIDADE ALTA (Refactor)

**Maturidade actual:** 64% | **Alvo:** 75%+

### Etapa 1: Security/Permission
- [ ] Verificar permissões por sub-domínio (Reports, Risk, Compliance, FinOps)
- [ ] Garantir que Executive Views respeitam persona

### Etapa 2: Persistence/Backend
- [ ] Identificar sub-domínios dentro das 19 endpoint modules
- [ ] Planear extracção de sub-módulos (Reports, Risk, FinOps)
- [ ] Contextualizar FinOps por serviço/equipa/domínio

### Etapa 3: Frontend Integration
- [ ] Avaliar quais das 25 páginas pertencem realmente a Governance
- [ ] Mover páginas para módulos correctos se identificados
- [ ] Simplificar navegação dentro do módulo

### Etapa 4: UX/i18n
- [ ] Verificar i18n em todas as páginas
- [ ] Garantir que Executive Views usam linguagem adequada à persona

### Etapa 5: Documentation
- [ ] Criar README do módulo
- [ ] Documentar sub-domínios e fronteiras
- [ ] Documentar decisão de reestruturação

### Etapa 6: AI/Agents
- [ ] N/A nesta fase

### Etapa 7: Checklist Final
- [ ] Sub-domínios identificados e documentados
- [ ] FinOps contextualizado (não genérico)
- [ ] Testes ≥70%
- [ ] Maturidade ≥75%

**Critério de aceitação mínimo:** Sub-domínios identificados, FinOps com contexto.

---

## 7. Módulo Integrations — PRIORIDADE MÉDIA

**Maturidade actual:** 41% | **Alvo:** 60%+

### Etapa 1: Security/Permission
- [ ] Verificar que credenciais de integração são encriptadas
- [ ] Confirmar isolamento por tenant
- [ ] Validar rate limiting para chamadas externas

### Etapa 2: Persistence/Backend
- [ ] Completar adapters para conectores core
- [ ] Implementar retry/circuit breaker para chamadas externas
- [ ] Garantir logging de todas as chamadas externas

### Etapa 3: Frontend Integration
- [ ] Verificar que UI de gestão de integrações funciona
- [ ] Implementar estado de saúde por integração

### Etapa 4: UX/i18n
- [ ] Verificar i18n
- [ ] Empty states para integrações não configuradas

### Etapa 5: Documentation
- [ ] **Criar README e docs (actualmente 0%)**
- [ ] Documentar conectores disponíveis
- [ ] Documentar como criar novo conector

### Etapa 6: AI/Agents
- [ ] N/A nesta fase

### Etapa 7: Checklist Final
- [ ] Conectores core funcionais
- [ ] Docs existentes (0% → 40%+)
- [ ] Testes ≥40%
- [ ] Maturidade ≥60%

**Critério de aceitação mínimo:** Docs criados, conectores core testados.

---

## 8. Módulo AI Knowledge — PRIORIDADE MÉDIA

**Maturidade actual:** 43% | **Alvo:** 65%+

### Etapa 1: Security/Permission
- [ ] Verificar que acesso a AI respeita permissões por persona
- [ ] Confirmar que token budgets são enforced
- [ ] Garantir audit de prompts e respostas

### Etapa 2: Persistence/Backend
- [ ] **Elevar backend de 25% para 50%+**
- [ ] Implementar handlers reais para tools
- [ ] Conectar providers (Ollama, OpenAI, Azure AI)
- [ ] Implementar model registry no backend

### Etapa 3: Frontend Integration
- [ ] **Implementar streaming UI**
- [ ] Conectar tool results à UI
- [ ] Mostrar estado real dos providers

### Etapa 4: UX/i18n
- [ ] Verificar i18n em chat, settings, catalog
- [ ] Empty states informativos para chat sem provider
- [ ] Loading states adequados para streaming

### Etapa 5: Documentation
- [ ] **Alinhar docs com realidade (actualmente otimista)**
- [ ] Documentar providers suportados (estado real)
- [ ] Documentar tools disponíveis e limitações

### Etapa 6: AI/Agents (específico deste módulo)
- [ ] **Conectar tools em runtime**
- [ ] Implementar RAG básico
- [ ] Testar pelo menos 3 agentes com execução real

### Etapa 7: Checklist Final
- [ ] Chat funcional com streaming
- [ ] Tools executam acções reais
- [ ] Pelo menos 2 providers activos
- [ ] Docs honestos e alinhados
- [ ] Testes ≥40%
- [ ] Maturidade ≥65%

**Critério de aceitação mínimo:** Tools funcionais, streaming, docs honestos.

---

## 9. Módulo Product Analytics — PRIORIDADE BAIXA

**Maturidade actual:** 30% | **Alvo:** 50%+

### Etapa 1: Security/Permission
- [ ] Verificar que acesso a analytics respeita permissões

### Etapa 2: Persistence/Backend
- [ ] Completar modelo de domínio de analytics
- [ ] Implementar handlers de ingestão e consulta

### Etapa 3: Frontend Integration
- [ ] **Implementar ProductAnalyticsOverviewPage.tsx (0 bytes)**
- [ ] Conectar dashboards a endpoints

### Etapa 4: UX/i18n
- [ ] i18n em todas as páginas
- [ ] Empty states

### Etapa 5: Documentation
- [ ] **Criar docs (actualmente 0%)**

### Etapa 6: AI/Agents
- [ ] N/A nesta fase

### Etapa 7: Checklist Final
- [ ] Overview page funcional
- [ ] Docs existentes
- [ ] Testes ≥30%
- [ ] Maturidade ≥50%

**Critério de aceitação mínimo:** Página overview funcional, docs mínimos.

---

## 10. Módulos Estáveis (Change Governance, Catalog, OpIntel, Notifications)

Estes módulos estão acima de 63% e necessitam apenas de ajustes pontuais:

### Change Governance (81%)
- [ ] README do módulo
- [ ] Completar blast radius calculation
- [ ] Manter testes ≥80%

### Catalog (81%)
- [ ] README do módulo
- [ ] Completar topologia visual
- [ ] Manter testes ≥90%

### Operational Intelligence (74%)
- [ ] README do módulo
- [ ] Dashboard com dados reais
- [ ] Documentar domínio

### Notifications (63%)
- [ ] **Criar migrações**
- [ ] README do módulo
- [ ] Completar canais webhook e SMS
- [ ] Testes ≥70%

---

## 11. Dashboard de Progresso

| Módulo | Actual | Alvo | Etapas Necessárias | Status |
|--------|--------|------|-------------------|--------|
| Contracts | 68% | 85% | 7/7 | 🔴 Não iniciado |
| Identity | 82% | 90% | 5/7 | 🔴 Não iniciado |
| Configuration | 77% | 85% | 5/7 | 🔴 Não iniciado |
| Audit & Compliance | 53% | 70% | 7/7 | 🔴 Não iniciado |
| Governance | 64% | 75% | 7/7 | 🔴 Não iniciado |
| Integrations | 41% | 60% | 7/7 | 🔴 Não iniciado |
| AI Knowledge | 43% | 65% | 7/7 | 🔴 Não iniciado |
| Product Analytics | 30% | 50% | 7/7 | 🔴 Não iniciado |
| Change Governance | 81% | 85% | 3/7 | 🟡 Ajustes |
| Catalog | 81% | 85% | 3/7 | 🟡 Ajustes |
| OpIntel | 74% | 80% | 3/7 | 🟡 Ajustes |
| Notifications | 63% | 75% | 4/7 | 🟡 Ajustes |

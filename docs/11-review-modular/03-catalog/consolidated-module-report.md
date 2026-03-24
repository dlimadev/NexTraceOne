# Catalog — Consolidated Module Report

> Gerado a partir da consolidação de todos os relatórios de auditoria e revisão modular do NexTraceOne.
> Última atualização: 2026-03-24

---

## 1. Visão Geral do Módulo

O módulo **Catalog** é o coração do NexTraceOne e a principal **fonte de verdade** do produto. É responsável pelo registo centralizado de serviços, gestão de ownership, mapeamento de dependências, visualização de topologia, governança de contratos, Developer Portal e exploração unificada de informação (Source of Truth).

Abrange 4 subdomínios distintos:

| Subdomínio | Responsabilidade | DbContext |
|------------|-----------------|-----------|
| **Graph** | Topologia de serviços, APIs, consumers, dependências, health records, snapshots | `CatalogGraphDbContext` (8 DbSets) |
| **Contracts** | Drafts, reviews, scorecards, exemplos, rulesets Spectral, versionamento | `ContractsDbContext` (7 DbSets) |
| **Portal** | Developer Portal com pesquisa, playground, sessões guardadas | `DeveloperPortalDbContext` (5 DbSets) |
| **Source of Truth** | Referências linkadas, explorador unificado de verdade | Partilha `CatalogGraphDb` + `ContractsDb` |

**Código-fonte:**

- Backend: `src/modules/catalog/` (256 ficheiros C#, 4 endpoint modules)
- Frontend: `src/frontend/src/features/catalog/` (23 ficheiros, 12 páginas)
- Base de dados: `nextraceone_catalog` (3 DbContexts, 20 DbSets, ~82 entidades de domínio)

---

## 2. Estado Atual

| Dimensão | Maturidade | Indicador |
|----------|-----------|-----------|
| **Backend** | 🟢 95% | Modelo de domínio robusto, 82 entidades, CQRS, DDD |
| **Frontend** | 🟡 75% | Funcional (9 páginas roteadas), gaps em topologia, 3 páginas órfãs |
| **Documentação** | 🟡 65% | Parcial — sem README, sem documentação do modelo de domínio |
| **Testes** | 🟢 90% | 430+ testes — cobertura excelente |
| **Maturidade global** | 🟢 **81%** | |
| **Prioridade** | **P2** | Pilar Core — Módulo Mais Maduro |
| **Status** | ✅ Funcional com ajustes pontuais | |

**Posição no produto:** Segundo módulo mais maduro (empatado com Change Governance a 81%), atrás apenas de Identity & Access (82%). É o módulo com mais entidades de domínio (82) e o modelo mais rico do sistema.

---

## 3. Problemas Críticos e Bloqueadores

Não existem bloqueadores absolutos neste módulo. Os problemas são de completude e higiene de código, não de funcionamento base.

| # | Problema | Severidade | Impacto |
|---|---------|-----------|---------|
| 1 | Visualização de topologia/dependências incompleta no frontend | 🟠 Alto | Funcionalidade core do Service Catalog parcialmente entregue — utilizadores não conseguem navegar o grafo de serviços de forma completa |
| 2 | 3 páginas órfãs legacy sem rota nem referência | 🟠 Alto | Código morto causa confusão para developers, risco de manutenção indevida |
| 3 | Ausência de README e documentação do modelo de domínio | 🟡 Médio | Impede onboarding eficaz; 82 entidades sem documentação estruturada |

**Causa raiz comum:** O módulo Catalog foi implementado com foco prioritário no backend e testes, deixando a completude do frontend e a documentação para segundo plano. As páginas órfãs resultam da migração de funcionalidades de contratos para o módulo `contracts` dedicado, sem limpeza do código residual.

---

## 4. Problemas por Camada

### 4.1 Frontend

| # | Problema | Severidade | Ficheiro/Rota | Detalhe |
|---|---------|-----------|--------------|---------|
| 1 | Páginas órfãs legacy | 🟠 Alto | `catalog/pages/ContractDetailPage.tsx`, `ContractListPage.tsx`, `ContractsPage.tsx` | Sem rota, sem referência — resíduos da migração para o módulo `contracts`. Substituídas por `ContractWorkspacePage` e `ContractCatalogPage` no módulo contracts |
| 2 | Topologia visual incompleta | 🟠 Alto | `ServiceCatalogPage.tsx` (`/services/graph`, 1010 linhas) | O componente de grafo existe mas a navegação e interação com dependências não está totalmente funcional |
| 3 | `CatalogContractsConfigurationPage` com rota admin | 🟡 Médio | `catalog/pages/CatalogContractsConfigurationPage.tsx` | Acessível apenas via admin (`/platform/configuration/catalog-contracts`), mas a permissão associada (`platform:admin:read`) não é granular ao módulo |
| 4 | Documentação de ações e páginas frontend não preenchida | 🟡 Médio | `docs/11-review-modular/06-service-catalog/frontend/` | Templates de revisão existem mas estão inteiramente vazios (`NOT_STARTED`) |

**Páginas roteadas e funcionais (9):**

| Página | Rota | Permissão |
|--------|------|-----------|
| ServiceCatalogListPage | `/services` | `catalog:assets:read` |
| ServiceCatalogPage | `/services/graph` | `catalog:assets:read` |
| ServiceDetailPage | `/services/:serviceId` | `catalog:assets:read` |
| SourceOfTruthExplorerPage | `/source-of-truth` | `catalog:assets:read` |
| ServiceSourceOfTruthPage | `/source-of-truth/services/:serviceId` | `catalog:assets:read` |
| ContractSourceOfTruthPage | `/source-of-truth/contracts/:contractVersionId` | `catalog:assets:read` |
| DeveloperPortalPage | `/portal/*` | `developer-portal:read` |
| GlobalSearchPage | `/search` | `catalog:assets:read` |
| CatalogContractsConfigurationPage | `/platform/configuration/catalog-contracts` | `platform:admin:read` |

### 4.2 Backend

| # | Problema | Severidade | Detalhe |
|---|---------|-----------|--------|
| 1 | Revisão detalhada de application services não realizada | 🟡 Médio | Templates em `06-service-catalog/backend/` estão `NOT_STARTED` — padrões CQRS, Result pattern, CancellationToken não foram formalmente validados |
| 2 | Documentação de endpoints por subdomínio ausente | 🟡 Médio | 5 endpoint modules existem e funcionam, mas não têm documentação estruturada de request/response |
| 3 | Validações frontend ↔ backend não verificadas formalmente | 🟡 Médio | Template de validation-rules está vazio; não há confirmação de consistência entre camadas |

**Endpoints inventariados (5 módulos, ~19 rotas):**

| Módulo | Rotas | Persistência |
|--------|-------|-------------|
| `ContractsEndpointModule` | 7 rotas (`/api/contracts/*`) | ContractsDb |
| `ContractStudioEndpointModule` | 4 rotas (`/api/contracts/studio/*`) | ContractsDb |
| `ServiceCatalogEndpointModule` | 6 rotas (`/api/catalog/services/*`) | CatalogGraphDb |
| `DeveloperPortalEndpointModule` | 4 rotas (`/api/portal/*`) | DeveloperPortalDb |
| `SourceOfTruthEndpointModule` | 2 rotas (`/api/source-of-truth/*`) | CatalogGraphDb + ContractsDb |

**Pontos fortes do backend:**

- Modelo de domínio classificado como ✅ COERENTE com 82 entidades
- Aggregate roots bem definidos: `ServiceAggregate`, `ContractAggregate`
- Value objects para schemas, versões e estados
- Domain services de compatibilidade, validação e diff
- Acoplamento com infraestrutura BAIXO — domínio limpo
- Regras de negócio implementadas: impact analysis, health records, contract lifecycle (Draft → Review → Published), Spectral validation, compliance scoring, consumer tracking

### 4.3 Database

| # | Problema | Severidade | Detalhe |
|---|---------|-----------|--------|
| 1 | Schema review não preenchida | 🟡 Médio | Template em `06-service-catalog/database/schema-review.md` está `NOT_STARTED` — tabelas, índices e constraints não foram formalmente auditados |
| 2 | Migrations review não preenchida | 🟡 Médio | 6 migrations existem mas não foram revistas quanto a reversibilidade e consolidação |
| 3 | Seed data review não preenchida | 🟡 Médio | `seed-catalog.sql` (172 linhas) com 9 services, 6 APIs e contracts — idempotência não validada |

**Estado actual da persistência:**

| DbContext | Entidades (est.) | Entity Configs (est.) | RLS | Audit | Soft Delete |
|-----------|------------------|----------------------|-----|-------|-------------|
| ContractsDb | ~35 | ~20 | ✅ | ✅ | ✅ |
| CatalogGraphDb | ~30 | ~15 | ✅ | ✅ | ✅ |
| DeveloperPortalDb | ~17 | ~10 | ✅ | ✅ | ✅ |

Todas as 3 DbContexts residem na base de dados lógica `nextraceone_catalog`, com isolamento total de tenant via RLS, audit interceptors e soft deletes activos.

### 4.4 Segurança

| Aspecto | Estado | Observação |
|---------|--------|------------|
| Autenticação | ✅ JWT em todos os endpoints | |
| Permissões granulares | ✅ | `catalog:assets:read/write`, `contracts:import/read/write`, `developer-portal:read/write` |
| RLS (multi-tenancy) | ✅ 100% dos DbContexts | |
| Rate limiting | ✅ | Global na maioria; `data-intensive` para topologia e search |
| Autorização formal review | 🟡 Não preenchida | Template `authorization-rules.md` está `NOT_STARTED` |

Não foram identificados problemas de segurança activos. A revisão formal de autorização por página e por acção continua pendente.

### 4.5 IA e Agentes

| Aspecto | Estado |
|---------|--------|
| Capacidades de IA definidas | ❌ `NOT_STARTED` |
| Agents definidos e registados | ❌ `NOT_STARTED` |
| Model Registry | ❌ Não configurado para este módulo |
| Auditoria de IA | ❌ Não aplicável actualmente |

Toda a área de IA para o módulo Catalog está por definir. Os templates de `ai-capabilities.md` e `agents-review.md` contêm placeholders genéricos copiados do módulo Identity, não reflectindo as necessidades reais do Catalog.

**Capacidades de IA esperadas (não implementadas):**

- Sugestão automática de dependências entre serviços
- Geração assistida de contratos (já parcialmente disponível via `ContractStudioEndpointModule` com `/api/contracts/studio/generate`)
- Análise de impacto assistida por IA
- Detecção de anomalias em topologia

### 4.6 Documentação

| Aspecto | Estado | Severidade |
|---------|--------|-----------|
| README do módulo | ❌ Inexistente | 🟠 Alto |
| Documentação do modelo de domínio (82 entidades) | ❌ Inexistente | 🟠 Alto |
| Module overview (06-service-catalog) | ⚠️ Template vazio | 🟡 Médio |
| Revisão detalhada (06-service-catalog/) | ⚠️ 100% dos ficheiros `NOT_STARTED` | 🟡 Médio |
| Developer onboarding notes | ⚠️ Template com placeholders incorrectos (copia texto de Identity) | 🟡 Médio |
| Comentários de código | ⚠️ Não revistos | 🟡 Médio |
| Documentação inline de endpoints | ⚠️ Parcial | 🟡 Médio |

---

## 5. Dependências

### Dependências de entrada (este módulo depende de)

| Módulo | Tipo | Descrição |
|--------|------|-----------|
| Identity & Access | Obrigatória | Autenticação, autorização, ownership de serviços baseado em identidade e equipas |

### Dependências de saída (outros módulos dependem deste)

| Módulo | Tipo | Descrição |
|--------|------|-----------|
| Contracts | Consumidor | Contratos de API associados a serviços do catálogo |
| Change Governance | Fornecedor | Serviços afetados por mudanças requerem contexto do catálogo |
| Operational Intelligence | Fornecedor | Dados de topologia e dependências para análise operacional |
| Audit & Compliance | Dependente | Alterações no catálogo geram eventos de auditoria |
| Governance | Dependente | Catálogo fornece contexto de serviços para políticas e domínios |

**Impacto:** O Catalog é um módulo fundacional — qualquer regressão aqui propaga-se para 5+ módulos dependentes.

---

## 6. Quick Wins

| # | Acção | Esforço | Impacto | Severidade |
|---|-------|---------|---------|-----------|
| 1 | Remover 3 páginas órfãs legacy (`ContractDetailPage.tsx`, `ContractListPage.tsx`, `ContractsPage.tsx`) | 1h | Elimina código morto e confusão | 🟠 Alto |
| 2 | Criar README do módulo (`src/modules/catalog/README.md`) com visão geral, subdomínios e entry points | 2h | Desbloqueia onboarding | 🟠 Alto |
| 3 | Documentar os 5 endpoint modules com request/response mínimos | 2h | Facilita integração frontend ↔ backend | 🟡 Médio |
| 4 | Corrigir developer onboarding notes (actualmente copia texto do Identity) | 1h | Evita desinformação | 🟡 Médio |
| 5 | Validar Developer Portal (`/portal/*`) — confirmar que search e playground funcionam end-to-end | 2h | Garante funcionalidade de portal | 🟡 Médio |

---

## 7. Refactors Estruturais

| # | Refactor | Esforço | Impacto | Severidade |
|---|---------|---------|---------|-----------|
| 1 | Completar visualização de topologia no frontend (`ServiceCatalogPage.tsx`, 1010 linhas) — navegação de grafo, interacção com dependências, drill-down por serviço | 3-5 dias | Funcionalidade diferenciadora do produto | 🟠 Alto |
| 2 | Avaliar separação do `ContractsDbContext` para módulo contracts dedicado — actualmente o backend de contratos vive no módulo catalog mas o frontend já foi migrado para o módulo contracts | 2-3 dias | Alinhamento arquitetural entre frontend e backend |  🟡 Médio |
| 3 | Documentar modelo de domínio completo (82 entidades, agregados, value objects, invariantes) | 3 dias | Base para manutenção sustentável | 🟡 Médio |
| 4 | Preencher toda a revisão detalhada em `06-service-catalog/` (15 ficheiros `NOT_STARTED`) | 5 dias | Completude da auditoria modular | 🟡 Médio |
| 5 | Definir e implementar capacidades de IA específicas para o Catalog (sugestão de dependências, análise de impacto, geração de contratos governada) | 5-10 dias | Diferenciação via AIOps | 🟢 Baixo |

---

## 8. Critérios de Fecho

Para o módulo Catalog ser considerado **DONE**, devem ser cumpridos:

### Obrigatórios (gate de produção)

- [ ] Topologia visual funcional e navegável no frontend
- [ ] Zero páginas órfãs — todas conectadas a rotas ou explicitamente removidas
- [ ] README do módulo criado e completo
- [ ] Documentação do modelo de domínio publicada
- [ ] Todos os 430+ testes passam (manter cobertura ≥90%)
- [ ] Endpoints documentados com request/response por subdomínio
- [ ] i18n verificado em todas as 9 páginas roteadas
- [ ] Revisão de autorização concluída (permissões por página e por acção)

### Desejáveis (meta 85%+)

- [ ] Revisão detalhada `06-service-catalog/` preenchida (≥80% dos ficheiros)
- [ ] Capacidades de IA definidas e priorizadas
- [ ] Developer onboarding notes corrigidas e completas
- [ ] Schema de base de dados formalmente auditado
- [ ] Seeds validados quanto a idempotência e suporte multi-tenant
- [ ] Comentários de código revistos em classes de domínio principais
- [ ] Acceptance checklist preenchido e validado

---

## 9. Plano de Ação Priorizado

### Wave 1 — Quick wins imediatos (1-2 dias)

| # | Acção | Responsável | Esforço |
|---|-------|------------|---------|
| 1.1 | Remover `ContractDetailPage.tsx`, `ContractListPage.tsx`, `ContractsPage.tsx` | Frontend | 1h |
| 1.2 | Criar README do módulo catalog | Backend | 2h |
| 1.3 | Corrigir developer onboarding notes (remover conteúdo copiado do Identity) | Docs | 1h |

### Wave 2 — Completude funcional (3-5 dias)

| # | Acção | Responsável | Esforço |
|---|-------|------------|---------|
| 2.1 | Completar topologia visual — navegação de grafo e drill-down | Frontend | 3-5 dias |
| 2.2 | Validar fluxos de Graph (service registration, dependency mapping, impact analysis) | QA | 3h |
| 2.3 | Validar Developer Portal end-to-end (search, playground) | QA | 2h |
| 2.4 | Documentar endpoints por subdomínio (5 módulos, ~19 rotas) | Backend | 2h |

### Wave 3 — Documentação e auditoria (5-8 dias)

| # | Acção | Responsável | Esforço |
|---|-------|------------|---------|
| 3.1 | Documentar modelo de domínio (82 entidades, agregados, value objects) | Architect | 3 dias |
| 3.2 | Preencher revisão detalhada em `06-service-catalog/` | Equipa | 5 dias |
| 3.3 | Completar revisão de autorização formal | Security | 1 dia |
| 3.4 | Auditar schema, migrations e seeds | DBA | 1 dia |

### Wave 4 — IA e diferenciação (5-10 dias)

| # | Acção | Responsável | Esforço |
|---|-------|------------|---------|
| 4.1 | Definir capacidades de IA para o Catalog | Product + AI | 2 dias |
| 4.2 | Avaliar separação ContractsDbContext → módulo contracts | Architect | 2-3 dias |
| 4.3 | Implementar IA assistida para topologia e análise de impacto | AI + Backend | 5-10 dias |

**Meta:** Elevar maturidade de 81% → 85%+ após Waves 1-3.

---

## 10. Inconsistências entre Relatórios

| # | Inconsistência | Fonte A | Fonte B | Impacto |
|---|---------------|---------|---------|---------|
| 1 | **Estado da revisão detalhada vs. simples.** A revisão simples (`03-catalog/module-review.md`) contém dados concretos e detalhados (entidades, endpoints, páginas, testes). A revisão detalhada (`06-service-catalog/`) está 100% `NOT_STARTED` com todos os campos a `[A PREENCHER]`. | `module-review.md` | `06-service-catalog/*` | 🟠 Alto — a revisão detalhada é inútil no estado actual; todo o conhecimento está na revisão simples e nos relatórios de governança |
| 2 | **Onboarding notes copiam conteúdo do módulo Identity.** O ficheiro `developer-onboarding-notes.md` em `06-service-catalog/` refere "Autenticação de utilizadores (OIDC, SAML, MFA)", "gestão de sessões com refresh token rotation" e "break glass" — funcionalidades do Identity, não do Catalog. | `developer-onboarding-notes.md` | `module-review.md` | 🟡 Médio — documento de onboarding é enganoso |
| 3 | **Número de páginas frontend.** A revisão simples indica 12 páginas (9 roteadas + 3 órfãs). O inventário de frontend de governança indica 12 páginas mas lista diferenças subtis na rota de `ServiceSourceOfTruthPage` (`/source-of-truth/service/:serviceId` vs. `/source-of-truth/services/:serviceId`). | `module-review.md` | `frontend-pages-and-routes-report.md` | 🟢 Baixo — possível typo no singular/plural da rota |
| 4 | **Estado do module overview.** O `README.md` em `06-service-catalog/` diz estado `NOT_STARTED`, enquanto a revisão simples e os relatórios de governança consideram o módulo funcional e a 81%. | `06-service-catalog/README.md` | `modular-review-summary.md` | 🟡 Médio — o estado `NOT_STARTED` refere-se à revisão detalhada, não ao módulo em si, mas é confuso |
| 5 | **Prioridade de fecho.** O `module-consolidation-report.md` coloca o Catalog na Wave 2 com 6ª posição de prioridade, enquanto o `module-closure-plan.md` o trata como "módulo estável" que necessita apenas "ajustes pontuais". | `module-consolidation-report.md` | `module-closure-plan.md` | 🟢 Baixo — ambas as perspectivas são válidas; o módulo é estável mas precisa de completude documental |
| 6 | **Templates de IA e agents.** Os templates em `06-service-catalog/ai/` referem agents de Identity (Identity Security Agent, Access Review Agent, Permission Analyzer Agent), não agents relevantes para o Catalog. | `agents-review.md` | Contexto do módulo | 🟡 Médio — templates genéricos não adaptados ao módulo |

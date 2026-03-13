# NexTraceOne — Revisão Completa dos Módulos 1 a 4

> **Data:** Março 2026
> **Escopo:** Módulos 1 (Identity & Access), 2 (Licensing & Entitlements), 3 (Engineering Graph), 4 (Developer Portal)
> **Base:** Análise do código real do repositório

---

## 1. RESUMO EXECUTIVO

### Visão Geral
O projeto NexTraceOne está em estado de maturidade mista. Os módulos 1-3 (Identity, Licensing, Engineering Graph) estão substancialmente implementados com Domain, Application, Infrastructure e API funcionais. O módulo 4 (Developer Portal) está em fase inicial — Domain e parte da Application existem, mas Infrastructure, API e testes estão em scaffold.

### Estado Geral
- **Build:** ✅ Compila sem erros (149 warnings)
- **Testes:** ✅ 370 testes passando, 0 falhas
- **Módulos registrados no ApiHost:** 9 de 14 (Identity, Licensing, EngineeringGraph, Contracts, ChangeIntelligence, RulesetGovernance, Workflow, Promotion, Audit)
- **DeveloperPortal:** ❌ Não registrado no ApiHost

### Principais Pontos Prontos
- Building Blocks (6/6) completamente funcionais
- Identity com 35 features, 111 testes, RBAC + multi-tenancy + OIDC + enterprise features
- Licensing com modelo rico de licenciamento (trial, capabilities, quotas, hardware binding)
- Engineering Graph com 21 features, 37 testes, blast radius, integração inbound (SyncConsumers)
- Contracts com 9 features, 42 testes, diff semântico, classificação de breaking changes

### Principais Lacunas
- Developer Portal: Infrastructure, API, testes e DI estão em scaffold
- i18n: en, pt-BR, pt-PT e es disponíveis; pt-PT e es necessitam registo em i18n.ts
- Identity: Sem migrations EF Core (auto-migration apenas)
- Licensing: Apenas 8 testes (baixa cobertura para a complexidade do domínio)
- API: Sem versionamento formal, sem Swagger UI, OpenAPI apenas em Development
- Frontend: Sem página dedicada para Developer Portal ou Licensing

---

## 2. REVISÃO POR MÓDULO

### 2.1 MÓDULO 1 — Identity & Access

#### Visão Geral
Módulo mais maduro do sistema. Implementa autenticação, autorização, multi-tenancy, OIDC, sessões, delegação, acesso emergencial, revisão de acesso e eventos de segurança. 35 features CQRS com handlers completos.

#### ✅ Itens Prontos
| Item | Detalhe |
|------|---------|
| Domain (18 entities) | User, Role, Session, Tenant, TenantMembership, Permission, ExternalIdentity, SsoGroupMapping, BreakGlass, JitAccess, Delegation, AccessReview, SecurityEvent, Environment, EnvironmentAccess |
| Value Objects (4) | Email, FullName, HashedPassword, RefreshTokenHash |
| Application (35 features) | LocalLogin, FederatedLogin, OidcCallback, RefreshToken, Logout, CreateUser, AssignRole, ChangePassword, ActivateUser, DeactivateUser, ListTenantUsers, GetCurrentUser, GetUserProfile, ListMyTenants, SelectTenant, ListRoles, ListPermissions, RequestBreakGlass, RevokeBreakGlass, RequestJitAccess, DecideJitAccess, CreateDelegation, RevokeDelegation, ListDelegations, StartAccessReviewCampaign, GetAccessReviewCampaign, DecideAccessReviewItem, ListAccessReviewCampaigns, ListActiveSessions, ListEnvironments, ListJitAccessRequests, ListBreakGlassRequests, GrantEnvironmentAccess, RevokeSession |
| Abstractions (20 interfaces) | Repositórios, JWT, PasswordHasher, OIDC Provider, LoginSessionCreator, SecurityAuditRecorder, etc. |
| Infrastructure | IdentityDbContext (16 DbSets), 16 Entity Configurations, 12 Repository implementations, 7 Services (JWT, OIDC, PasswordHasher, AuditBridge, etc.) |
| API (9 endpoint files) | ~40 endpoints sob /api/v1/identity/ (Auth, Users, Roles, BreakGlass, JIT, Delegation, Tenant, AccessReview, Environment) |
| Contracts | 2 Integration Events (UserCreated, UserRoleChanged), 2 DTOs, IIdentityModule |
| Testes (111) | 15 feature tests + 10 domain tests + 2 value object tests + 3 infrastructure tests |
| Frontend | LoginPage, TenantSelectionPage, UsersPage com i18n completo |
| DI wiring | ✅ Completamente registrado no ApiHost |

#### 🟡 Itens Parciais
| Item | O que existe | O que falta | Impacto | Blocker? |
|------|-------------|-------------|---------|----------|
| Migrations EF Core | DbContext com 16 entities configuradas | Ficheiros de migration ausentes — depende de auto-migration em runtime | Risco em produção: auto-migration não é recomendado | Não é blocker para desenvolvimento, mas P1 para produção |
| OIDC real | Handler OidcCallback + StartOidcLogin implementados, IOidcProvider com HttpClient | Sem testes de integração com IDP real | Sem validação funcional end-to-end | Não blocker |
| Audit bridge | ISecurityAuditBridge implementado | Integração real com módulo Audit não testada end-to-end | Eventos de segurança podem não chegar ao Audit em runtime | Não blocker |
| Enterprise features | BreakGlass, JIT, Delegation, AccessReview — domain + application + endpoints | Sem testes de integração; sem UI no frontend | Features enterprise existem mas não foram exercitadas em fluxo completo | Não blocker |

#### 🔲 Itens Pendentes
| Item | Detalhe | Próximo passo |
|------|---------|---------------|
| Frontend para enterprise features | Páginas para BreakGlass, JIT, Delegation, AccessReview | Criar páginas com i18n |
| Testes de integração | Fluxo completo login→tenant→action | Criar testes E2E |
| Frontend Licensing awareness | Verificação de capabilities no frontend | Integrar com Licensing API |

#### ⚠️ Itens que Precisam Revisão
- Auto-migration em produção é um risco: criar migrations explícitas é recomendado
- Warnings CS8632 (nullable) nos testes devem ser corrigidos

#### Próximos Passos
1. Gerar migrations EF Core explícitas (P1)
2. Adicionar testes de integração OIDC (P2)
3. Criar UI frontend para features enterprise (P2)

---

### 2.2 MÓDULO 2 — Licensing & Entitlements

#### Visão Geral
Modelo de licenciamento completo e rico, com trial, conversão, capabilities, quotas com enforcement progressivo, hardware binding e health score. Todas as camadas implementadas.

#### ✅ Itens Prontos
| Item | Detalhe |
|------|---------|
| Domain (5 entities) | License (Aggregate Root), LicenseCapability, LicenseActivation, HardwareBinding, UsageQuota |
| Enums (4) | LicenseType, LicenseEdition, EnforcementLevel, WarningLevel |
| Application (10 features) | ActivateLicense, VerifyLicenseOnStartup, CheckCapability, TrackUsageMetric, GetLicenseStatus, GetLicenseHealth, AlertLicenseThreshold, StartTrial, ExtendTrial, ConvertTrial |
| Abstractions (3) | ILicenseRepository, IHardwareFingerprintProvider, IHardwareBindingRepository |
| Infrastructure | LicensingDbContext + IUnitOfWork, 2 Repositories, 5 Entity Configurations, Migrations presentes |
| API (10 endpoints) | /api/v1/licensing/ (activate, verify, status, capabilities, usage, thresholds, trial/start, trial/extend, trial/convert, health) |
| Contracts | 3 DTOs, 2 Integration Events, ILicensingModule |
| DI wiring | ✅ Completamente registrado no ApiHost |

#### 🟡 Itens Parciais
| Item | O que existe | O que falta | Impacto | Blocker? |
|------|-------------|-------------|---------|----------|
| Testes | 8 testes unitários | Cobertura baixa para a complexidade do domínio (trial lifecycle, quotas, hardware binding) | Risco de regressão em mudanças futuras | Não blocker, mas P1 |
| Frontend | Nenhuma página de Licensing | Dashboard de licenças, status, alertas de quota | Administrador não consegue visualizar estado das licenças | Não blocker para backend |
| Integração cross-module | ILicensingModule interface definida | Nenhum módulo consome licensing validation ativamente | Capabilities não são enforced em runtime nos outros módulos | P2 |
| Offline mode | HardwareBinding implementado, VerifyLicenseOnStartup existe | Sem mecanismo de cache offline ou grace period real para desconexão | Licenciamento requer DB sempre | P2 |

#### 🔲 Itens Pendentes
| Item | Detalhe | Próximo passo |
|------|---------|---------------|
| Frontend Licensing page | Dashboard de licença com status, quotas, capabilities | Criar LicensingPage.tsx + API client |
| Mais testes | Trial lifecycle, quota enforcement, hardware binding | Expandir para ≥30 testes |
| Enforcement no pipeline | TenantIsolationBehavior ou LicenseBehavior para verificar capability antes de cada command | Implementar MediatR behavior |
| API client frontend | Não existe api/licensing.ts | Criar módulo de API frontend |

#### Próximos Passos
1. Expandir cobertura de testes (P1)
2. Criar frontend page e API client (P2)
3. Implementar enforcement via MediatR behavior (P2)

---

### 2.3 MÓDULO 3 — Engineering Graph

#### Visão Geral
Módulo de referência do projeto. Implementação completa com Domain, Application, Infrastructure e API. Inclui grafo de dependências, blast radius, integração inbound (SyncConsumers), temporalidade, overlays e busca.

#### ✅ Itens Prontos
| Item | Detalhe |
|------|---------|
| Domain (8 entities) | ApiAsset, ServiceAsset, ConsumerRelationship, ConsumerAsset, DiscoverySource, GraphSnapshot, NodeHealthRecord, SavedGraphView |
| Enums (5) | EdgeType, HealthStatus, NodeType, OverlayMode, RelationshipSemantic |
| Application (21 features) | RegisterServiceAsset, RegisterApiAsset, MapConsumerRelationship, DecommissionAsset, UpdateAssetMetadata, CreateGraphSnapshot, CreateSavedView, SyncConsumers, InferDependencyFromOtel, ImportFromBackstage, ImportFromKongGateway, ValidateDiscoveredDependency, GetAssetDetail, GetAssetGraph, SearchAssets, GetSubgraph, GetImpactPropagation, GetTemporalDiff, GetNodeHealth, ListSnapshots, ListSavedViews |
| Infrastructure | EngineeringGraphDbContext, 5 Repositories, 8 Entity Configurations, Migrations presentes |
| API (14+ endpoints) | Incluindo /api/v1/engineeringgraph/integration/v1/consumers/sync (inbound) |
| Contracts | IEngineeringGraphModule |
| Testes (37) | Domínio + aplicação + SyncConsumers + blast radius + subgraph |
| Frontend | EngineeringGraphPage.tsx com 5 abas e i18n completo |
| Integração inbound | SyncConsumers: batch upsert com idempotência, até 100 itens, correlationId, sourceSystem |
| Blast radius | GetImpactPropagation: traversal recursivo com profundidade configurável |
| Temporalidade | Snapshots + GetTemporalDiff |
| Overlays | Health, ChangeVelocity, Risk, Cost, ObservabilityDebt |
| Documentação | docs/engineering-graph/EXTERNAL-INTEGRATION-API.md + docs/engineering-graph/ROADMAP.md |

#### 🟡 Itens Parciais
| Item | O que existe | O que falta | Impacto | Blocker? |
|------|-------------|-------------|---------|----------|
| ImportFromBackstage | Handler com parsing básico | Integração real com Backstage catalog API | Funcionalidade de importação limitada a payloads manuais | Não blocker |
| ImportFromKongGateway | Handler com parsing básico | Integração real com Kong Admin API | Idem | Não blocker |
| Frontend graph visualization | Tab de Graph com lista de nós e arestas | Visualização gráfica real (Apache ECharts) | UX limitada para grafos grandes | P2 |
| InferDependencyFromOtel | Handler implementado | Sem receptor real de traces OpenTelemetry | Discovery automático não funciona ainda | P2 |

#### Próximos Passos
1. Integrar visualização gráfica com Apache ECharts (P2)
2. Implementar receptor real de OTel traces (P3)
3. Refinar importações Backstage/Kong com testes de integração (P3)

---

### 2.4 MÓDULO 4 — Developer Portal

#### Visão Geral
Módulo em estágio inicial. Domain Layer está completo com 5 aggregates e lógica de negócio real. Application Layer tem 16 features, mas 8 são stubs. Infrastructure, API e testes estão em scaffold. **Não está registrado no ApiHost.**

#### ✅ Itens Prontos
| Item | Detalhe |
|------|---------|
| Domain (5 aggregates) | CodeGenerationRecord, PlaygroundSession, PortalAnalyticsEvent, SavedSearch, Subscription |
| Enums (4) | SubscriptionLevel, NotificationChannel, GenerationType, PortalEventType |
| Error catalog (11 errors) | Com códigos i18n padronizados (DeveloperPortal.*.NotFound, etc.) |
| Application features (8 implementados) | CreateSubscription, DeleteSubscription, GenerateCode, GetSubscriptions, GetApiConsumers, GetPlaygroundHistory, GetPortalAnalytics, RecordAnalyticsEvent |

#### 🟡 Itens Parciais
| Item | O que existe | O que falta | Impacto | Blocker? |
|------|-------------|-------------|---------|----------|
| Application features (8 stubs) | ExecutePlayground (mock), SearchCatalog (vazio), GetApiDetail (stub), GetApiHealth (stub), GetMyApis (stub), GetApisIConsume (stub), GetAssetTimeline (stub), RenderOpenApiContract (stub) | Integração real com EngineeringGraph, Contracts, RuntimeIntelligence | Features core do portal não funcionam | Blocker para funcionalidade do portal |
| DI wiring Application | DependencyInjection.cs existe | TODO: MediatR handlers e validators não registrados | Handlers não são descobertos pelo MediatR | Blocker |

#### 🔲 Itens Pendentes (Críticos)
| Item | Detalhe | Impacto | Próximo passo |
|------|---------|---------|---------------|
| Infrastructure completa | DbContext vazio, sem DbSets, sem repos, sem configs, sem migrations | Nenhuma feature persiste dados | Implementar DbContext + Repos + Configs + Migrations |
| API endpoints | Endpoint module sem rotas mapeadas | Portal não tem endpoints HTTP | Mapear 16 endpoints |
| DI completo | Application e Infrastructure DI são stubs | Módulo não funciona em runtime | Registrar MediatR, validators, repos |
| Registro no ApiHost | Módulo não está em Program.cs | Portal não é carregado | Adicionar AddDeveloperPortalModule() |
| Testes reais | Apenas 1 placeholder test | Sem cobertura | Criar testes para domain + features implementadas |
| Frontend | Sem página de Developer Portal | Sem UI para o portal | Criar DeveloperPortalPage.tsx |
| Contracts | IDeveloperPortalModule vazio | Sem operações cross-module | Definir métodos públicos |

#### ⚠️ Itens Incorretos / Frágeis
- Application DependencyInjection.cs não registra MediatR nem validators — handlers nunca são chamados
- Infrastructure DependencyInjection.cs não registra DbContext nem repos — persistência não existe
- 8 features retornam stubs/mocks sem indicação clara ao consumidor de que são placeholders

#### Blockers para Avanço
1. **Blocker P0:** DI wiring (Application + Infrastructure) precisa ser implementado
2. **Blocker P0:** Infrastructure (DbContext, repos, configs, migrations) precisa existir
3. **Blocker P0:** API endpoints precisam ser mapeados
4. **Blocker P0:** Registro no ApiHost (Program.cs)

#### Próximos Passos
1. Implementar Infrastructure completa (DbContext, repos, configs, migrations) — P0
2. Registrar DI em Application e Infrastructure — P0
3. Mapear endpoints no API layer — P0
4. Registrar no ApiHost — P0
5. Criar testes para domain e features — P1
6. Implementar features stub com integração real — P1
7. Criar frontend page — P2

---

## 3. ANÁLISE TRANSVERSAL

### 3.1 Arquitetura
- **Separação de responsabilidades:** ✅ Excelente. Cada módulo tem 5 camadas independentes.
- **SOLID/SRP:** ✅ Boa aderência nos módulos implementados. Features seguem VSA (1 arquivo por caso de uso).
- **DDD:** ✅ Aggregates com factory methods, value objects, domain events, error catalogs.
- **CQRS:** ✅ MediatR com Command/Query separação consistente.
- **Consistência entre módulos:** ✅ EngineeringGraph, Contracts, ChangeIntelligence seguem o mesmo padrão. DeveloperPortal está atrasado.

### 3.2 Segurança
- **Autorização real:** ✅ RBAC com permissões granulares, RequireAuthorization() nos endpoints
- **Multi-tenancy:** ✅ TenantResolutionMiddleware + TenantRlsInterceptor (PostgreSQL RLS)
- **Token management:** ✅ JWT com RS256, refresh token rotation, session tracking
- **Security headers:** ✅ CSP, X-Frame-Options, HSTS via UseSecurityHeaders()
- **Frontend:** ✅ ProtectedRoute, usePermissions(), sessionStorage para tokens
- **Gaps:** Sem rate limiting explícito nas APIs; sem API key para integrações sistema-a-sistema

### 3.3 Testabilidade
- **Testes existentes:** 370 testes, 0 falhas
- **Cobertura por módulo:**
  - Identity: 111 testes ✅ Excelente
  - EngineeringGraph: 37 testes ✅ Boa
  - Contracts: 42 testes ✅ Boa
  - ChangeIntelligence: 18 testes ✅ Adequada
  - Workflow: 40 testes ✅ Boa
  - RulesetGovernance: 26 testes ✅ Boa
  - Promotion: 29 testes ✅ Boa
  - Licensing: 8 testes ⚠️ Baixa (precisa expandir)
  - DeveloperPortal: 1 placeholder ❌ Sem cobertura
  - Building Blocks: 49 testes ✅ Boa
- **Testes E2E:** Apenas placeholders (1 teste)
- **Massa de teste:** Scripts de seed existem para EngineeringGraph

### 3.4 Auditabilidade
- **SecurityEvent entity:** ✅ 40+ event types com risk scoring
- **AuditInterceptor:** ✅ CreatedAt/By, UpdatedAt/By automático
- **Integration com Audit module:** ✅ ISecurityAuditBridge implementado
- **Hash chain:** ✅ AuditChainLink com SHA-256 no módulo Audit
- **Gap:** Integração end-to-end Identity→Audit não testada

### 3.5 Estado do Frontend
- **Páginas funcionais:** 11 (Login, TenantSelection, Dashboard, Releases, EngineeringGraph, Contracts, Users, Workflow, Promotion, Audit, Unauthorized)
- **i18n:** ✅ Implementado com i18next, 175+ chaves por idioma
- **Loading/Error states:** ✅ Skeleton components, ErrorBoundary global
- **API clients:** 9 módulos (identity, changeIntelligence, engineeringGraph, contracts, workflow, promotion, audit, + client base)
- **Gaps:** Sem página para DeveloperPortal, sem página para Licensing, sem API client para licensing

### 3.6 Integração entre Módulos
- **Contracts layer:** ✅ Cada módulo expõe interface (IIdentityModule, ILicensingModule, IEngineeringGraphModule, etc.)
- **Integration Events:** ✅ UserCreated, UserRoleChanged, LicenseActivated, LicenseThresholdReached via Outbox
- **Cross-module queries:** ✅ Via service interfaces (nunca via DbContext direto)
- **Gap:** DeveloperPortal não consome IEngineeringGraphModule nem IContractsModule ainda

---

## 4. I18N STATUS

### 4.1 O que está pronto
- ✅ Infraestrutura i18next + react-i18next configurada
- ✅ Detecção automática de idioma do browser
- ✅ Troca de idioma no AppHeader (toggle)
- ✅ 175+ chaves traduzidas organizadas por namespace
- ✅ XSS protection (escapeValue: true)
- ✅ Fallback para inglês configurado
- ✅ Backend retorna error codes (chaves i18n) via Result<T>

### 4.2 Cobertura por Idioma
| Idioma | Status | Ficheiro | Chaves |
|--------|--------|----------|--------|
| English (en) | ✅ Completo | src/frontend/src/locales/en.json | 175+ |
| Português Brasil (pt-BR) | ✅ Completo | src/frontend/src/locales/pt-BR.json | 175+ |
| Português Portugal (pt-PT) | ✅ Completo | src/frontend/src/locales/pt-PT.json | 316 |
| Espanhol (es) | ✅ Completo | src/frontend/src/locales/es.json | 316 |

### 4.3 O que falta
1. ~~Criar locale pt-PT.json com adaptações para português europeu~~ ✅ Concluído
2. ~~Criar locale es.json com traduções para espanhol~~ ✅ Concluído
3. Atualizar i18n.ts para registrar pt-PT e es
4. Atualizar AppHeader para oferecer seletor de 4 idiomas (não apenas toggle)
5. Adicionar chaves i18n para DeveloperPortal (namespace developerPortal.*)
6. Adicionar chaves i18n para Licensing (namespace licensing.*)
7. Backend: SharedMessages.resx existe mas sem variantes pt-PT e es

### 4.4 Gaps por Módulo
| Módulo | en | pt-BR | pt-PT | es |
|--------|-----|-------|-------|-----|
| Common/Auth/Tenants | ✅ | ✅ | ✅ | ✅ |
| Dashboard | ✅ | ✅ | ✅ | ✅ |
| Identity/Users | ✅ | ✅ | ✅ | ✅ |
| Engineering Graph | ✅ | ✅ | ✅ | ✅ |
| Contracts | ✅ | ✅ | ✅ | ✅ |
| Releases/ChangeIntelligence | ✅ | ✅ | ✅ | ✅ |
| Workflow | ✅ | ✅ | ✅ | ✅ |
| Promotion | ✅ | ✅ | ✅ | ✅ |
| Audit | ✅ | ✅ | ✅ | ✅ |
| Developer Portal | ❌ | ❌ | ❌ | ❌ |
| Licensing | ❌ | ❌ | ❌ | ❌ |

---

## 5. API EXTERNAL INTEGRATION STATUS

### 5.1 O que está pronto
- ✅ Todas as APIs sob /api/v1/ (versionamento manual via path)
- ✅ SyncConsumers como referência de integração inbound (batch upsert, idempotência, correlationId)
- ✅ OpenAPI nativo (.NET 10) em /openapi/v1.json (Development)
- ✅ Result<T>.ToHttpResult() padroniza respostas HTTP
- ✅ Error codes padronizados (chaves i18n) em todas as respostas de erro
- ✅ Contratos estáveis via Contracts layer (DTOs, Integration Events)
- ✅ Multi-tenancy via header/JWT para isolamento por tenant
- ✅ Postman collection existente (docs/postman/NexTraceOne-API.postman_collection.json)

### 5.2 O que falta
| Item | Status | Impacto | Prioridade |
|------|--------|---------|------------|
| API versioning formal | /api/v1/ hardcoded, sem middleware de versionamento | Dificuldade em manter v1 e v2 simultaneamente | P2 |
| Swagger UI | OpenAPI JSON existe mas sem UI interativa | Desenvolvedores externos não conseguem explorar a API visualmente | P1 |
| API key / Client credentials | Sem autenticação sistema-a-sistema | Integrações externas precisam de JWT pessoal | P1 |
| Rate limiting | Sem rate limiting | Risco de abuso em APIs públicas | P1 |
| Idempotency keys | Apenas SyncConsumers tem idempotência | Outros endpoints de criação podem gerar duplicatas | P2 |
| Webhook outbound | Subscription entity existe no DeveloperPortal mas sem dispatcher | Notificações de mudança não são enviadas | P2 |
| Documentação de integração | EXTERNAL-INTEGRATION-API.md existe para EngineeringGraph | Falta para outros módulos | P2 |
| CORS para integrações | Configuração restritiva existe | Pode precisar de ajuste para origens de parceiros | P3 |

### 5.3 Readiness por Módulo
| Módulo | Inbound Ready | Outbound Ready | Auth Sistema-a-Sistema | Documentação |
|--------|--------------|----------------|----------------------|-------------|
| Identity | ✅ (40 endpoints) | 🟡 (Integration Events via Outbox) | 🟡 (JWT apenas) | ❌ |
| Licensing | ✅ (10 endpoints) | 🟡 (Integration Events) | 🟡 (JWT apenas) | ❌ |
| Engineering Graph | ✅ (14+ endpoints + SyncConsumers) | 🟡 (Integration Events) | 🟡 (JWT apenas) | ✅ |
| Developer Portal | ❌ (0 endpoints mapeados) | ❌ | ❌ | ❌ |

---

## 6. ROADMAP ATUALIZADO

### 6.1 Status Real por Módulo

| Módulo | Domain | Application | Infrastructure | API | Testes | Frontend | DI/ApiHost | Status Geral |
|--------|--------|-------------|---------------|-----|--------|----------|-----------|-------------|
| Identity | ✅ 100% | ✅ 100% | ✅ 100% (sem migrations) | ✅ 100% | ✅ 111 | ✅ 3 páginas | ✅ | 95% |
| Licensing | ✅ 100% | ✅ 100% | ✅ 100% | ✅ 100% | ⚠️ 8 | ❌ 0 páginas | ✅ | 80% |
| Engineering Graph | ✅ 100% | ✅ 100% | ✅ 100% | ✅ 100% | ✅ 37 | ✅ 1 página | ✅ | 98% |
| Developer Portal | ✅ 100% | 🟡 50% | ❌ Scaffold | ❌ Scaffold | ❌ 1 placeholder | ❌ 0 páginas | ❌ Não registrado | 30% |

### 6.2 Priorização — Now / Next / Later

#### NOW (P0 — Blockers / Críticos)
1. **DeveloperPortal Infrastructure:** DbContext + DbSets + Entity Configurations + Repositories + Migrations
2. **DeveloperPortal DI wiring:** Registrar MediatR, validators, repositories em Application e Infrastructure
3. **DeveloperPortal API endpoints:** Mapear 16 endpoints no EndpointModule
4. **DeveloperPortal registro no ApiHost:** Adicionar AddDeveloperPortalModule() em Program.cs
5. **Identity Migrations:** Gerar migrations EF Core explícitas

#### NEXT (P1 — Importante)
1. **DeveloperPortal testes:** Criar testes unitários para domain + features implementadas
2. **Licensing testes:** Expandir de 8 para ≥30 testes (trial, quotas, hardware)
3. **i18n pt-PT:** Criar locale com adaptações para português europeu
4. **i18n es:** Criar locale com traduções para espanhol
5. **Swagger UI:** Habilitar interface interativa para exploração das APIs
6. **API key / Client credentials:** Implementar autenticação sistema-a-sistema
7. **Rate limiting:** Implementar throttling nas APIs públicas
8. **DeveloperPortal features stub:** Integrar com EngineeringGraph e Contracts
9. **Frontend Licensing page:** Criar dashboard de licenciamento
10. **Frontend DeveloperPortal page:** Criar UI do portal

#### LATER (P2/P3 — Evolução)
1. **API versioning middleware:** Implementar versionamento formal com Asp.Versioning
2. **Idempotency keys:** Generalizar padrão do SyncConsumers para outros endpoints
3. **Webhook dispatcher:** Implementar envio de notificações via webhook
4. **Documentação de integração:** Criar EXTERNAL-INTEGRATION-API.md para cada módulo
5. **Frontend graph visualization:** Apache ECharts para visualização de grafos
6. **OTel receptor real:** Implementar discovery automático via OpenTelemetry
7. **Offline licensing:** Implementar cache local para validação offline
8. **Licensing enforcement behavior:** MediatR behavior para verificar capabilities
9. **Testes E2E:** Expandir para fluxos completos de ponta a ponta
10. **Seletor de 4 idiomas:** Atualizar AppHeader com dropdown para en, pt-BR, pt-PT, es

### 6.3 Dependências e Riscos

#### Dependências entre Módulos
- DeveloperPortal → EngineeringGraph (via IEngineeringGraphModule para dados de API)
- DeveloperPortal → Contracts (via IContractsModule para specs OpenAPI)
- DeveloperPortal → RuntimeIntelligence (para health — módulo ainda não implementado)
- ChangeIntelligence → EngineeringGraph (para blast radius — ✅ já implementado)
- Workflow → ChangeIntelligence (para evidence pack — ✅ já implementado)

#### Riscos Técnicos
1. **Identity sem migrations:** Auto-migration em produção é arriscado
2. **DeveloperPortal incompleto:** Precisa de Infrastructure antes de ser funcional
3. **Licensing baixa cobertura de testes:** Risco de regressão em domínio complexo

#### Riscos de Produto
1. **Portal sem funcionalidade real:** Impede onboarding de desenvolvedores
2. **Sem Swagger UI:** Dificulta adoção por equipes externas
3. **Sem rate limiting:** Risco de abuso

#### Riscos Frontend/Backend
1. **DeveloperPortal sem endpoints:** Frontend não tem APIs para consumir
2. **Licensing sem frontend:** Administrador não visualiza licenças

### 6.4 Roadmap de i18n

| Tarefa | Prioridade | Estimativa |
|--------|------------|------------|
| Criar pt-PT.json (adaptação de pt-BR) | P1 | 1 dia |
| Criar es.json (tradução completa) | P1 | 2 dias |
| Atualizar i18n.ts para 4 idiomas | P1 | 0.5 dia |
| Atualizar AppHeader (seletor de idioma) | P2 | 0.5 dia |
| Adicionar namespace developerPortal.* | P1 | 0.5 dia |
| Adicionar namespace licensing.* | P2 | 0.5 dia |
| Backend SharedMessages pt-PT e es | P2 | 1 dia |

### 6.5 Roadmap de Integração Externa

| Tarefa | Prioridade | Estimativa |
|--------|------------|------------|
| Swagger UI (Development + Staging) | P1 | 0.5 dia |
| API key / Client credentials auth | P1 | 3 dias |
| Rate limiting middleware | P1 | 1 dia |
| Documentação de integração por módulo | P2 | 3 dias |
| API versioning middleware | P2 | 1 dia |
| Idempotency middleware | P2 | 2 dias |
| Webhook outbound dispatcher | P2 | 3 dias |
| CORS configuração para parceiros | P3 | 0.5 dia |

---

## 7. PENDÊNCIAS CRÍTICAS

### O que impede avanço seguro
1. DeveloperPortal Infrastructure + DI + API endpoints (P0)
2. DeveloperPortal registro no ApiHost (P0)

### O que impede testes funcionais
1. DeveloperPortal sem endpoints = impossível testar via HTTP
2. Identity sem migrations = risco em ambientes limpos
3. DeveloperPortal sem testes = sem baseline de qualidade

### O que deve ser tratado antes do próximo módulo
1. Completar DeveloperPortal Infrastructure (mínimo viável)
2. Registrar DeveloperPortal no ApiHost
3. Expandir testes de Licensing
4. Criar locales pt-PT e es (infraestrutura base)

---

## 8. RESUMO PARA STATUS REPORT

Revisão completa dos módulos 1-4 do NexTraceOne realizada com base no código real do repositório.

**Pronto:**
- Identity & Access (95%): 35 features, 111 testes, RBAC + multi-tenancy + OIDC completo
- Licensing & Entitlements (80%): modelo rico com trial, quotas, hardware binding; falta frontend e mais testes
- Engineering Graph (98%): módulo referência com 21 features, 37 testes, blast radius, integração inbound
- Contracts (100%): diff semântico completo, 42 testes

**Parcial:**
- Developer Portal (30%): Domain e parte da Application prontos; Infrastructure, API, testes e frontend em scaffold

**Faltando:**
- DeveloperPortal: Infrastructure, API endpoints, DI wiring, registo no ApiHost, testes, frontend
- i18n: pt-PT e es criados (falta registo em i18n.ts e seletor de 4 idiomas no AppHeader)
- APIs: sem Swagger UI, sem API key para integrações, sem rate limiting

**Roadmap atualizado** para refletir estado real. Próxima prioridade: completar DeveloperPortal Infrastructure e expandir suporte i18n.

# NexTraceOne — Estado Real do Produto
**Data da análise:** 2026-04-18  
**Branch analisada:** `claude/analyze-nextraceone-project-8JhlQ`  
**Modo:** Analysis — avaliação honesta, sem otimismo  
**Escopo:** Codebase completo — Backend, Frontend, Base de Dados, Testes, Documentação

---

## Aviso Editorial

Este documento é uma análise independente do estado real do produto.  
Não é um relatório de marketing. Não é um sumário de roadmap.  
É uma leitura honesta e direta do que existe, do que falta e do que está quebrado.  
Onde há progresso real, é dito. Onde há dívida técnica ou lacuna funcional, é nomeado.

---

## 1. Síntese Executiva

O NexTraceOne é uma plataforma enterprise de **governança de serviços, contratos e mudanças**, com ambição de ser Source of Truth operacional para equipas de engenharia e produto.

Após análise do codebase (estimado em **3.800+ ficheiros**, **27 DbContexts**, **12 módulos**, **113 rotas frontend**, **4+ idiomas**, **97 ficheiros de documentação**):

### Veredicto geral: **Produto em estado beta avançado, mas não apto para produção sem remediação**

| Dimensão | Estado | Nota |
|----------|--------|------|
| Arquitectura | Sólida | Monólito modular, bounded contexts claros |
| Completude funcional (backend) | ~82% | Módulos reais, mas stubs e endpoints em falta |
| Completude funcional (frontend) | ~78% | Muitas páginas reais, mas UX crítica quebrada em 4 ecrãs |
| Base de dados | ~88% | RLS implementado, 1 colisão crítica não resolvida |
| Testes (backend) | ~75% | Volume alto, mas cobertura de integração insuficiente |
| Testes (frontend) | ~55% | Vitest presente, mas maioria são mocks — testes reais escassos |
| Segurança | ~68% | Fundação boa, mas 3 críticos activos desde Abril 10 |
| Documentação | ~80% | Quantidade excelente, mas parte desalinhada com código real |
| i18n | ~85% | Estrutura sólida, 4 idiomas; 4 ficheiros ainda com strings hardcoded |
| Observabilidade | ~70% | Infra pronta; integração real com ClickHouse/pgvector ausente |

---

## 2. O que está genuinamente bem construído

Antes de listar problemas, é justo reconhecer o que funciona com qualidade:

- **Arquitectura modular**: 12 bounded contexts com separação real de responsabilidades. Nenhum DbContext cruza fronteiras de módulo. DDD aplicado com coerência.
- **Building blocks**: `Result<T>`, guard clauses, strongly typed IDs, CQRS via MediatR, outbox pattern — tudo production-grade.
- **Segurança (fundação)**: JWT, CSRF, PBKDF2, AES-256-GCM, rate limiting com 6 políticas, hash chain SHA-256 no audit trail.
- **Catálogo de contratos**: 10 tipos de contrato (REST, SOAP, Evento, Background, Webhook, Copybook, MqMessage, FixedLayout, CicsCommarea, SharedSchema) com Contract Studio visual — é diferenciador real.
- **IA governada**: LLM real (Ollama/OpenAI), 5 guardrails de segurança, ferramentas de domínio, auditoria completa — não é chat genérico.
- **Suite de testes**: ~4.000 testes backend, ~1.700 testes frontend. Volume sério.
- **i18n**: 4 idiomas, estrutura de chaves consistente, textos de produto bem traduzidos.
- **Documentação estratégica**: 97 ficheiros, ADRs, runbooks, guias de segurança, planos de implantação on-prem.
- **CI/CD**: 5 pipelines no GitHub Actions (CI, security, staging, production, E2E).

---

## 3. Problemas Críticos — estado actual

> Estes problemas existiam na auditoria de 2026-04-10 e **permanecem não resolvidos** à data desta análise.

### [C-01] Colisão de tabela no ChangeGovernance ❌ NÃO RESOLVIDO
- **Impacto**: Migrações EF Core falham. Dados de promoção corrompidos.
- **Detalhe**: `ChangeIntelligenceDbContext` e `PromotionDbContext` mapeiam entidades diferentes para `chg_promotion_gates`.
- **Ver**: [GAPS-DATABASE-2026-04-18.md](./GAPS-DATABASE-2026-04-18.md#c-01)

### [C-02] Chave JWT hardcoded no código-fonte ❌ NÃO RESOLVIDO
- **Impacto**: Se deployed em produção tal como está, todos os tokens são comprometidos.
- **Detalhe**: `devFallbackKey` em `BuildingBlocks.Security/DependencyInjection.cs` — visível em binários compilados.
- **Ver**: [GAPS-BACKEND-2026-04-18.md](./GAPS-BACKEND-2026-04-18.md#c-02)

### [C-03] API Keys em memória sem encriptação ❌ NÃO RESOLVIDO
- **Impacto**: Chaves expostas em dumps de processo. Ausência de rotação e auditoria de uso.
- **Ver**: [GAPS-BACKEND-2026-04-18.md](./GAPS-BACKEND-2026-04-18.md#c-03)

### [C-04] 6 endpoints de autenticação em falta no backend ❌ NÃO RESOLVIDO
- **Impacto**: Activação de conta, recuperação de password e fluxo de convite estão completamente quebrados.
- **Endpoints em falta**: `activateAccount`, `forgotPassword`, `resetPassword`, `resendMfaCode`, `getInvitationDetails`, `acceptInvitation`
- **Ver**: [GAPS-BACKEND-2026-04-18.md](./GAPS-BACKEND-2026-04-18.md#c-04)

### [C-05] Export endpoint — stub sem implementação ❌ NÃO RESOLVIDO
- **Impacto**: Exportação de dados retorna status `"queued"` hardcoded. Nenhum job Quartz real.
- **Ver**: [GAPS-BACKEND-2026-04-18.md](./GAPS-BACKEND-2026-04-18.md#c-05)

### [C-06] OnCall Intelligence com dados pseudo-aleatórios ❌ NÃO RESOLVIDO
- **Impacto**: Indicadores de fadiga calculados com `Math.Min(20m + (seed % 30), 60m)`. Dados apresentados como métricas reais — são ficção.
- **Ver**: [GAPS-BACKEND-2026-04-18.md](./GAPS-BACKEND-2026-04-18.md#c-06)

### [C-07 a C-10] Campos GUID expostos na UI (4 ecrãs) ❌ NÃO RESOLVIDO
- **Impacto**: Utilizadores obrigados a introduzir UUIDs brutos. Violação do princípio básico de UX enterprise.
- **Ecrãs afectados**: `CanonicalEntityImpactCascadePage`, `ContractHealthTimelinePage`, `DependencyDashboardPage`, `LicenseCompliancePage`
- **Ver**: [GAPS-FRONTEND-2026-04-18.md](./GAPS-FRONTEND-2026-04-18.md#c-07)

---

## 4. Problemas de Alta Prioridade — estado actual

| ID | Área | Descrição | Estado |
|----|------|-----------|--------|
| A-01 | BD | ~30 configurações de entidade sem `TenantId.IsRequired()` — bypass de RLS possível | ❌ Não resolvido |
| A-02 | Segurança | Break Glass sem workflow de aprovação | ❌ Não resolvido |
| A-03 | Segurança | Operações em produção sem autorização adicional (MFA step-up) | ❌ Não resolvido |
| A-04 | Backend | Silent exception handling (catch blocks vazios em 3 handlers) | ❌ Não resolvido |
| A-05 | Backend | Null result sem validação em `IncidentCorrelationService` | ❌ Não resolvido |
| A-06 | API | DTO mismatch no endpoint de correlação (Guid vs string) | ❌ Não resolvido |
| A-07 | Frontend | Strings hardcoded sem i18n em 4 ficheiros de features críticas | ❌ Não resolvido |

---

## 5. Lacunas funcionais estruturais

Estas não são bugs — são funcionalidades previstas na visão do produto que **ainda não existem**:

| Capacidade | Estado | Impacto no produto |
|------------|--------|-------------------|
| GraphQL Federation | Ausente | HotChocolate 14.3 instalado mas nenhum endpoint GraphQL exposto |
| SAML 2.0 | Ausente | Apenas OIDC/JWT suportados; SSO enterprise incompleto |
| Vector search / RAG real | Ausente | pgvector instalado mas não usado para retrieval semântico |
| Correlação incidente↔mudança avançada | Parcial | Apenas timestamp + nome de serviço; sem ML ou heurísticas reais |
| Conectores reais Mainframe | Ausente | APIs de importação existem; conectores CICS/IMS/DB2 não existem |
| Event-driven entre módulos | Parcial | Outbox e processadores existem; eventos de integração escassos no módulo Configuration |
| Protobuf/gRPC | Ausente | ADR-006 planeado; não implementado |
| Segurança a nível de coluna (RLS granular) | Ausente | RLS por tenant existe; sem controlo por coluna |
| IDE Extensions (VS Code / Visual Studio) | Ausente | Mencionado em CLAUDE.md como escopo; inexistente no repositório |

---

## 6. Estado por módulo

| Módulo | Backend | Frontend | Testes | Estado geral |
|--------|---------|----------|--------|--------------|
| Building Blocks | ✅ Completo | N/A | ✅ 400+ testes | **PRONTO** |
| Configuration | ✅ Completo | ✅ Completo | ✅ 451 testes | **PRONTO** (C-05 export stub) |
| Identity Access | ⚠️ 6 endpoints em falta | ⚠️ Fluxos quebrados | ✅ 150+ testes | **BLOQUEADO** (C-04) |
| Catalog | ✅ 90 features reais | ⚠️ 2 ecrãs GUID | ✅ 1179+ testes | **QUASE PRONTO** |
| Change Governance | ⚠️ Colisão BD | ✅ Completo | ✅ 307 testes | **BLOQUEADO** (C-01) |
| Operational Intelligence | ⚠️ OnCall fake | ✅ Completo | ✅ 639 testes | **PARCIAL** (C-06) |
| Audit Compliance | ✅ Completo | ✅ Completo | ✅ 80+ testes | **PRONTO** |
| Governance | ✅ Completo | ✅ 25/26 ecrãs | ✅ 233+ testes | **PRONTO** |
| AI Knowledge | ✅ LLM real | ✅ Completo | ✅ 819+ testes | **PRONTO** |
| Knowledge | ✅ Completo | ✅ Completo | ✅ 60+ testes | **PRONTO** |
| Notifications | ✅ Completo | ✅ Completo | ✅ 70+ testes | **PRONTO** |
| Integrations | ✅ Completo | ⚠️ Parcial | ✅ 50+ testes | **PARCIAL** |

---

## 7. Dívida técnica acumulada

### Dívida de segurança (alta urgência)
1. JWT fallback key em código → remover e forçar variável de ambiente obrigatória
2. API Keys em plaintext → encriptar e implementar rotação
3. Break Glass sem aprovação → adicionar policy de aprovação obrigatória
4. MFA step-up para produção → implementar `require_mfa` por scope de ambiente

### Dívida de domínio
1. OnCall Intelligence com dados simulados → integrar com fonte real de on-call
2. Export endpoint sem implementação → implementar job Quartz com entrega real
3. Correlação incidente↔mudança muito básica → ampliar com heurísticas de contexto
4. SearchCatalog stub intencional → concluir integração cross-module

### Dívida de UX
1. 4 ecrãs com GUID raw input → substituir por search/dropdown contextual
2. 4 ficheiros com strings hardcoded → migrar para i18n
3. ContractHealthTimeline sem loading state → adicionar skeleton

### Dívida de arquitectura
1. GraphQL HotChocolate instalado mas não exposto → decidir: implementar ou remover dependência
2. pgvector presente mas sem uso real → implementar pipeline de embedding/retrieval ou remover
3. Eventos de integração ausentes no módulo Configuration → publicar eventos em handlers críticos

---

## 8. Estimativa de esforço para produção

| Categoria | Story Points estimados | Sprints (equipa 3 devs) |
|-----------|----------------------|------------------------|
| Críticos (C-01 a C-10) | 12–16 SP | 1 sprint |
| Alta prioridade (A-01 a A-07) | 10–14 SP | 1 sprint |
| Médios (M-01 a M-14) | 14–20 SP | 1–2 sprints |
| Lacunas funcionais estruturais | 40–60 SP | 3–4 sprints |
| **Total para production-ready** | **76–110 SP** | **4–6 sprints** |

---

## 9. Documentos específicos desta análise

| Documento | Conteúdo |
|-----------|---------|
| [GAPS-BACKEND-2026-04-18.md](./GAPS-BACKEND-2026-04-18.md) | Análise detalhada do backend: stubs, endpoints em falta, erros de domínio |
| [GAPS-FRONTEND-2026-04-18.md](./GAPS-FRONTEND-2026-04-18.md) | Análise detalhada do frontend: UX, i18n, páginas incompletas |
| [GAPS-DATABASE-2026-04-18.md](./GAPS-DATABASE-2026-04-18.md) | Colisões, RLS gaps, modelos de dados problemáticos |
| [GAPS-TESTS-2026-04-18.md](./GAPS-TESTS-2026-04-18.md) | Cobertura de testes, qualidade, cenários em falta |
| [GAPS-DOCUMENTATION-2026-04-18.md](./GAPS-DOCUMENTATION-2026-04-18.md) | Documentação desalinhada, ausente ou desactualizada |
| [INNOVATION-ROADMAP-2026-04-18.md](./INNOVATION-ROADMAP-2026-04-18.md) | Novas funcionalidades sugeridas com base no estado real do produto |

---

## 10. Conclusão

O NexTraceOne tem uma **base arquitectural sólida** e um **volume de código real** que poucos produtos enterprise em fase equivalente conseguem apresentar. O produto é ambicioso e coerente com a sua visão.

**O problema não é a visão. É a execução parcial de componentes críticos.**

Existem 11 problemas críticos e de alta prioridade **abertos desde 2026-04-10** que não foram resolvidos. Enquanto estes não forem fechados:

- O fluxo de onboarding de novos utilizadores está quebrado (C-04)
- As migrações de base de dados podem falhar em ambientes com ChangeGovernance activo (C-01)
- O produto não deve ser considerado pronto para produção com as chaves JWT actuais (C-02)
- 4 ecrãs exigem que utilizadores enterprise introduzam UUIDs manualmente (C-07 a C-10)

O produto precisa de **1–2 sprints de estabilização focada** antes de qualquer demonstração em ambiente de produção real.

---

*Análise gerada em 2026-04-18. Para comparação com auditoria anterior, ver [AUDIT-MASTER-2026-04-10.md](../audit/AUDIT-MASTER-2026-04-10.md).*

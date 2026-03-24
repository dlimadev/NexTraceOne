# Standards de Documentação Propostos — NexTraceOne

> **Tipo:** Proposta de standards — documentação de código e produto  
> **Escopo:** Ficheiros .md, XML docs (C#), comentários inline, JSDoc (TypeScript), onboarding  
> **Data de referência:** Junho 2025  
> **Classificação:** Interno — Governança de Documentação

---

## 1. Objetivo

Este documento propõe **standards de documentação unificados** para o NexTraceOne, abrangendo:

1. Ficheiros Markdown (.md) — o quê, onde, profundidade, formato
2. XML Docs (C#) — quais classes, tipo de conteúdo, o que evitar
3. Comentários Inline — quando, o quê, o que não comentar
4. JSDoc/TSDoc (TypeScript/React) — onde obrigatório, formato
5. Onboarding — guias mínimos, informação por módulo, navegação

Estes standards baseiam-se na análise do estado atual do repositório (ver `documentation-and-onboarding-audit.md`) e visam preservar os pontos fortes existentes enquanto colmatam as lacunas identificadas.

---

## 2. Standard para Ficheiros Markdown (.md)

### 2.1 Tipos de documentos e localização

| Tipo | Localização | Quem escreve | Quando atualiza |
|---|---|---|---|
| README de módulo | `src/modules/{module}/README.md` | Dono do módulo | Quando a estrutura muda |
| README raiz | `/README.md` | Tech Lead | Quando a arquitetura evolui |
| Getting Started | `docs/getting-started/` | Tech Lead + DevOps | Quando o setup muda |
| Architecture Decision Record | `docs/adr/` | Quem toma a decisão | Quando a decisão é tomada |
| Guia de contribuição | `/CONTRIBUTING.md` | Tech Lead | Quando o processo muda |
| Auditoria/Review | `docs/11-review-modular/` | Equipa de governança | A cada ciclo de review |
| Plano de execução | `docs/execution/` | Gestão de projeto | A cada sprint |
| Observabilidade | `docs/observability/` | Equipa de plataforma | Quando infra muda |
| Design System | `docs/DESIGN-SYSTEM.md` | Frontend Lead | Quando o DS evolui |
| Guidelines | `docs/GUIDELINE.md` | Equipa | Quando as regras mudam |

### 2.2 Formato obrigatório para ficheiros .md

| Elemento | Regra |
|---|---|
| Título (H1) | Sempre presente, descritivo |
| Metadata block | Tipo, escopo, data, classificação (ver blocos `>` nos relatórios existentes) |
| Secções (H2) | Numeradas para documentos longos |
| Tabelas | Preferir tabelas a listas para dados estruturados |
| Código | Usar blocos de código com indicação de linguagem |
| Links | Relativos dentro do repositório, absolutos para externos |
| Idioma | **Português** (padrão do repositório) |
| Comprimento | Sem limite rígido, mas prefira documentos focados a monolíticos |

### 2.3 Profundidade por tipo

| Tipo de documento | Profundidade | Audiência |
|---|---|---|
| README de módulo | Visão geral + referência rápida | Qualquer programador |
| Getting Started | Passo-a-passo detalhado | Novos membros |
| ADR | Contexto + decisão + consequências | Equipa técnica |
| Auditoria | Análise profunda com evidências | Governança |
| Guidelines | Regras claras com exemplos | Todos |

### 2.4 O que NÃO deve ser ficheiro .md

| Tipo | Onde colocar |
|---|---|
| Especificações de API | OpenAPI/Swagger (gerado automaticamente) |
| Schema de BD | Diagrama ER gerado ou comentários nas migrations |
| Changelog de código | Git log + release notes |
| Notas pessoais ou rascunhos | Fora do repositório |

---

## 3. Standard para XML Docs (Backend C#)

### 3.1 Onde é obrigatório

| Tipo de artefacto | XML Doc obrigatório? | Conteúdo mínimo |
|---|---|---|
| Classes públicas | ✅ Sim | `<summary>` com propósito da classe |
| Interfaces públicas | ✅ Sim | `<summary>` com contrato descrito |
| Métodos públicos | ✅ Sim | `<summary>`, `<param>`, `<returns>` |
| Domain Entities | ✅ Sim | `<summary>` com descrição do conceito de negócio |
| Value Objects | ✅ Sim | `<summary>` com significado do valor |
| Commands/Queries | ✅ Sim | `<summary>` com propósito da operação |
| Handlers | ✅ Sim | `<summary>` com descrição do fluxo |
| DTOs | ✅ Sim | `<summary>` com contexto de uso |
| Endpoints | ✅ Sim | `<summary>` com descrição REST |
| DbContext | ✅ Sim | `<summary>` com módulo e responsabilidade |
| Entity Configuration | ✅ Sim | `<summary>` com mapeamento descrito |
| Métodos privados | ⚠️ Recomendado | Apenas se complexos |
| Classes internas | ⚠️ Recomendado | Quando propósito não é óbvio |

### 3.2 Formato do `<summary>`

#### Bom exemplo — Entity

```csharp
/// <summary>
/// Representa um serviço registado no catálogo do NexTraceOne.
/// Um serviço é a unidade fundamental de organização, com ownership,
/// contratos e dependências associadas.
/// </summary>
public class Service : AggregateRoot
```

#### Bom exemplo — Command Handler

```csharp
/// <summary>
/// Processa a criação de um novo serviço no catálogo.
/// Valida unicidade do nome dentro do tenant, cria a entidade
/// e publica o evento ServiceCreatedEvent.
/// </summary>
/// <param name="command">Dados do serviço a criar.</param>
/// <param name="cancellationToken">Token de cancelamento.</param>
/// <returns>Identificador do serviço criado.</returns>
public class CreateServiceCommandHandler : ICommandHandler<CreateServiceCommand, ServiceId>
```

#### Bom exemplo — Endpoint

```csharp
/// <summary>
/// Lista os serviços do catálogo com suporte a paginação e filtros.
/// Requer permissão catalog:read.
/// </summary>
/// <remarks>
/// GET /api/catalog/services?page=1&amp;pageSize=20&amp;search=nome
/// </remarks>
public class GetServicesEndpoint : Endpoint<GetServicesRequest, PagedResult<ServiceDto>>
```

### 3.3 O que evitar nos XML docs

| Anti-padrão | Exemplo | Porquê |
|---|---|---|
| Repetir o nome da classe | `/// <summary>Classe Service.</summary>` | Não acrescenta informação |
| Apenas tipos | `/// <summary>String.</summary>` | Descrever o significado, não o tipo |
| Documentação genérica | `/// <summary>Handler do comando.</summary>` | Especificar o que o handler faz |
| Copiar signature | `/// <summary>Cria com nome e id.</summary>` | Descrever o propósito de negócio |
| Inglês (neste projeto) | `/// <summary>Represents a service.</summary>` | Manter consistência em português |

### 3.4 Configuração recomendada

```xml
<!-- Directory.Build.props -->
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <!-- Suprimir inicialmente; remover gradualmente -->
  <NoWarn>$(NoWarn);CS1591</NoWarn>
</PropertyGroup>
```

**Meta:** Remover `CS1591` da supressão quando a cobertura atingir 100%, tornando XML docs obrigatórios no build.

---

## 4. Standard para Comentários Inline

### 4.1 Quando comentar

| Situação | Comentar? | Exemplo |
|---|---|---|
| Lógica de negócio não óbvia | ✅ Sim | `// Aplica desconto apenas se o contrato estiver ativo há mais de 30 dias` |
| Decisão técnica não trivial | ✅ Sim | `// Usa session scope em vez de local para suportar connection pooling` |
| Workaround temporário | ✅ Sim | `// Workaround para bug no EF Core 8 com owned types — remover quando atualizar` |
| SQL/query complexa | ✅ Sim | `// CTE recursiva para resolver hierarquia de dependências` |
| Cross-cutting concern | ✅ Sim | `// RLS: define tenant_id na sessão PostgreSQL antes de qualquer query` |
| Integração externa | ✅ Sim | `// A API externa retorna 429 se exceder 100 req/min — implementar retry com backoff` |

### 4.2 Quando NÃO comentar

| Situação | Comentar? | Razão |
|---|---|---|
| Código auto-explicativo | ❌ Não | `var user = await repository.GetByIdAsync(userId); // busca o usuário` — redundante |
| Nomes descritivos | ❌ Não | Se o nome explica, o comentário é ruído |
| CRUD simples | ❌ Não | Padrão universalmente conhecido |
| Getters/setters triviais | ❌ Não | Convenção da linguagem |
| Blocos `try/catch` padrão | ❌ Não | A menos que o tratamento seja não trivial |

### 4.3 Formato

| Regra | Detalhe |
|---|---|
| Idioma — Backend | **Português** (100% consistente atualmente) |
| Idioma — Frontend JSDoc | **Inglês** (standard técnico da comunidade React) |
| Idioma — Frontend inline | **Português** (explicações de negócio) |
| Posição | Acima da linha que explica, nunca no final da linha (exceto para inline curtos) |
| Comprimento | Máximo 2–3 linhas; se precisar de mais, usar `<remarks>` ou doc separada |
| Marcadores | Não usar TODO/HACK/FIXME no código — registar em issues |

---

## 5. Standard para JSDoc/TSDoc (Frontend)

### 5.1 Onde é obrigatório

| Tipo de artefacto | JSDoc obrigatório? | Conteúdo mínimo |
|---|---|---|
| Page Components | ✅ Sim | Descrição da página, props, rota |
| Custom Hooks | ✅ Sim | Propósito, parâmetros, retorno |
| API Services | ✅ Sim | Endpoints consumidos, tipos de request/response |
| Componentes partilhados (shared/) | ✅ Sim | Propósito, props, exemplos de uso |
| Componentes de feature | ⚠️ Recomendado | Quando complexos |
| Tipos/Interfaces | ⚠️ Recomendado | Quando propósito não é óbvio |
| Utilitários | ⚠️ Recomendado | Quando a função não é trivial |
| Constantes/Enums | ⚠️ Recomendado | Quando o significado não é claro |

### 5.2 Formatos de JSDoc por tipo

#### Page Component

```typescript
/**
 * Service Catalog page — displays the list of registered services
 * with filtering, sorting and detail navigation.
 *
 * Route: /catalog/services
 * Permissions: catalog:read
 *
 * @remarks
 * Uses useCatalogServices hook for data fetching.
 * Supports persona-based content filtering.
 */
export function ServiceCatalogPage(): JSX.Element {
```

#### Custom Hook

```typescript
/**
 * Manages contract diff state and comparison logic.
 *
 * @param contractId - The contract to compare
 * @param baseVersion - Base version for comparison
 * @param targetVersion - Target version for comparison
 * @returns Diff result with additions, removals and modifications
 *
 * @example
 * const { diff, isLoading } = useContractDiff(contractId, 'v1.0', 'v2.0');
 */
export function useContractDiff(
  contractId: string,
  baseVersion: string,
  targetVersion: string
): ContractDiffResult {
```

#### API Service

```typescript
/**
 * API integration service for the Catalog module.
 *
 * Endpoints:
 * - GET  /api/catalog/services       — List services (paginated)
 * - GET  /api/catalog/services/:id   — Get service details
 * - POST /api/catalog/services       — Create new service
 * - PUT  /api/catalog/services/:id   — Update service
 *
 * @module CatalogApiService
 */
```

### 5.3 O que evitar

| Anti-padrão | Porquê |
|---|---|
| JSDoc que apenas repete o nome da função | Não acrescenta informação |
| JSDoc sem descrição dos parâmetros em hooks complexos | Parâmetros devem ser descritos |
| JSDoc em inglês com gramática incorreta | Melhor sem JSDoc do que com inglês errado |
| JSDoc desatualizado (descreve comportamento anterior) | Pior que não ter JSDoc |

---

## 6. Standard para Documentação de Onboarding

### 6.1 Guias mínimos obrigatórios

| Guia | Localização | Conteúdo | Audiência |
|---|---|---|---|
| **README raiz** | `/README.md` | Visão geral, setup rápido, links | Todos |
| **Getting Started** | `docs/getting-started/README.md` | Setup completo, primeiros passos | Novos membros |
| **Codebase Map** | `docs/getting-started/codebase-map.md` | O que está onde | Novos membros |
| **Contributing** | `/CONTRIBUTING.md` | Processo de contribuição | Todos |
| **Architecture Overview** | `src/frontend/ARCHITECTURE.md` ✅ | Já existe — manter atualizado | Todos |

### 6.2 Informação mínima por módulo

Cada módulo (`src/modules/{module}/`) deve ter um README com:

| Secção | Obrigatória? | Descrição |
|---|---|---|
| Visão geral | ✅ | O que faz e porquê existe |
| Entidades principais | ✅ | Tabela com entidades e descrição |
| Endpoints | ✅ | Tabela com rotas e métodos |
| Dependências | ✅ | De quem depende e quem depende dele |
| Contratos/Eventos | ⚠️ | Se o módulo publica ou consome |
| Como testar | ⚠️ | Instruções para testes do módulo |
| Notas de desenvolvimento | ⚠️ | Decisões específicas, caveats |

### 6.3 Guia de navegação de documentação

O ficheiro `docs/README.md` deve servir como **índice principal** com:

```markdown
# Documentação — NexTraceOne

## Para novos membros da equipa
- [Getting Started](getting-started/README.md)
- [Mapa da Codebase](getting-started/codebase-map.md)
- [Guia de Contribuição](/CONTRIBUTING.md)

## Arquitetura e design
- [Análise Crítica Arquitetural](ANALISE-CRITICA-ARQUITETURAL.md)
- [Guidelines do Projeto](GUIDELINE.md)
- [Design System](DESIGN-SYSTEM.md)
- [Arquitetura Frontend](../src/frontend/ARCHITECTURE.md)
- [Decisões Arquiteturais (ADRs)](adr/)

## Módulos
- [Foundation](../src/modules/foundation/README.md)
- [Catalog](../src/modules/catalog/README.md)
- ...

## Operações
- [Observabilidade](observability/README.md)
- [Troubleshooting](observability/troubleshooting.md)

## Auditorias e governança
- [Review Modular](11-review-modular/)
- [Auditorias](audits/)
```

### 6.4 Caminho de onboarding recomendado

| Semana | Documentos a ler | Atividade |
|---|---|---|
| 1 | README raiz → Getting Started → Codebase Map | Setup + orientação |
| 1 | GUIDELINE.md → CONTRIBUTING.md | Convenções e processo |
| 1 | Module README (Notifications ou Audit) | Entender um módulo simples |
| 2 | ARCHITECTURE.md (frontend) → Design System | Entender frontend |
| 2 | Module README (Catalog) | Entender um módulo central |
| 3+ | ADRs → Observability docs → Restantes módulos | Aprofundamento |

---

## 7. Matriz de Responsabilidades

### RACI para documentação

| Tipo de documento | Responsible | Accountable | Consulted | Informed |
|---|---|---|---|---|
| README raiz | Tech Lead | CTO/Architect | Equipa | Todos |
| Getting Started | Tech Lead | Tech Lead | DevOps | Novos membros |
| Module README | Dono do módulo | Tech Lead | Equipa do módulo | Todos |
| ADR | Decisor técnico | Architect | Equipa afetada | Todos |
| XML docs (código) | Autor do código | Reviewer | — | — |
| JSDoc (código) | Autor do código | Reviewer | — | — |
| Auditorias | Equipa de governança | Tech Lead | Architects | Gestão |
| Design System | Frontend Lead | Tech Lead | Designers | Frontend devs |

---

## 8. Checklist de Verificação

### Para code review — Backend

- [ ] Novas classes públicas têm `/// <summary>`?
- [ ] Novos métodos públicos têm `<param>` e `<returns>`?
- [ ] Comentários inline em lógica não trivial?
- [ ] Comentários em português?
- [ ] Sem TODO/HACK/FIXME (usar issues)?

### Para code review — Frontend

- [ ] Novas páginas têm JSDoc?
- [ ] Novos hooks têm JSDoc com `@param` e `@returns`?
- [ ] Novos API services têm JSDoc com endpoints listados?
- [ ] ESLint disable tem justificação no comentário?
- [ ] Todo texto visível está em i18n?

### Para PR de documentação

- [ ] Metadata block presente (tipo, escopo, data)?
- [ ] Idioma em português?
- [ ] Tabelas para dados estruturados?
- [ ] Links relativos corretos?
- [ ] Secções numeradas em documentos longos?

### Para novo módulo

- [ ] README.md criado com todas as secções obrigatórias?
- [ ] Adicionado ao índice em `docs/README.md`?
- [ ] XML docs em todas as classes públicas?
- [ ] Endpoints documentados?

---

## 9. Métricas e Metas

### Métricas de cobertura

| Métrica | Meta Sprint 2 | Meta Trimestral | Meta Anual |
|---|---|---|---|
| XML docs backend | 99% | 100% | 100% |
| JSDoc frontend — Páginas | 80% | 90% | 95% |
| JSDoc frontend — API Services | 90% | 100% | 100% |
| JSDoc frontend — Hooks | 100% | 100% | 100% |
| READMEs em módulos | 9/9 | 9/9 | 9/9 |
| Pontuação legibilidade frontend | 70/100 | 75/100 | 80/100 |

### Métricas de processo

| Métrica | Meta |
|---|---|
| PRs com verificação de documentação | 100% |
| Novos módulos sem README | 0 |
| ADRs para decisões significativas | 100% |
| Getting Started atualizado e funcional | Sempre |

---

## 10. Adoção dos Standards

### Estratégia de adoção

| Fase | Ação | Prazo |
|---|---|---|
| 1 — Publicação | Publicar este documento e obter aprovação da equipa | Semana 1 |
| 2 — Comunicação | Apresentar standards em reunião de equipa | Semana 1 |
| 3 — Aplicação em novos PRs | Começar a verificar standards em code review | Semana 2 |
| 4 — Retroativa gradual | Aplicar standards ao código existente em sprints dedicados | Contínuo |
| 5 — Automação | Configurar linting/CI para verificar cobertura | Mês 2 |
| 6 — Revisão | Revisar e ajustar standards com base na experiência | Trimestral |

### Exceções

| Situação | Tratamento |
|---|---|
| Código auto-gerado | Excluído de XML docs/JSDoc obrigatórios |
| Protótipos/POCs | Standard relaxado, mas README obrigatório |
| Ficheiros de teste | JSDoc recomendado mas não obrigatório |
| Migrations de BD | Comentários inline recomendados em migrations complexas |

---

> **Nota:** Este documento deve ser revisto trimestralmente e atualizado conforme a equipa adota e refina as práticas de documentação. Complementa o relatório principal `documentation-and-onboarding-audit.md`.

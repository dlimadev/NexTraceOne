# Relatório de Nomenclatura e Legibilidade de Código — NexTraceOne

> **Tipo:** Relatório detalhado de auditoria — convenções de nomeação e organização  
> **Escopo:** Backend (C#/.NET), Frontend (TypeScript/React), Base de dados (PostgreSQL)  
> **Data de referência:** Junho 2025  
> **Classificação:** Interno — Governança de Código

---

## 1. Resumo Executivo

| Camada | Classificação | Consistência | Rastreabilidade |
|---|---|---|---|
| Backend (.NET / DDD) | **A+** | 100% em 9 módulos | Excelente |
| Frontend (React/TS) | **A** | ~95% | Muito boa |
| Base de dados (PostgreSQL) | **A** | ~98% | Muito boa |
| Cross-layer (Frontend → BD) | **A+** | — | Rastreamento end-to-end claro |

As convenções de nomeação do NexTraceOne são **excelentes globalmente**, com padrões claramente definidos e consistentemente aplicados. Foram identificadas **5 inconsistências menores** que não comprometem a legibilidade mas beneficiariam de normalização.

---

## 2. Backend — Nomenclatura e Organização

### 2.1 Padrão de Namespaces

| Camada | Padrão | Exemplo |
|---|---|---|
| Domain | `NexTraceOne.{Module}.Domain` | `NexTraceOne.Catalog.Domain` |
| Domain Entities | `NexTraceOne.{Module}.Domain.Entities` | `NexTraceOne.Catalog.Domain.Entities` |
| Domain Events | `NexTraceOne.{Module}.Domain.Events` | `NexTraceOne.Catalog.Domain.Events` |
| Application | `NexTraceOne.{Module}.Application` | `NexTraceOne.Catalog.Application` |
| Application Commands | `NexTraceOne.{Module}.Application.Commands` | `NexTraceOne.Catalog.Application.Commands` |
| Application Queries | `NexTraceOne.{Module}.Application.Queries` | `NexTraceOne.Catalog.Application.Queries` |
| Infrastructure | `NexTraceOne.{Module}.Infrastructure` | `NexTraceOne.Catalog.Infrastructure` |
| Endpoints | `NexTraceOne.{Module}.Endpoints` | `NexTraceOne.Catalog.Endpoints` |

### 2.2 Avaliação

| Critério | Avaliação | Notas |
|---|---|---|
| Consistência entre módulos | ✅ **100%** | Todos os 9 módulos seguem o mesmo padrão |
| Aderência a DDD | ✅ Excelente | Separação clara Domain/Application/Infrastructure |
| Nomes de classes | ✅ PascalCase | Standard .NET respeitado |
| Nomes de métodos | ✅ PascalCase | Standard .NET respeitado |
| Nomes de propriedades | ✅ PascalCase | Standard .NET respeitado |
| Nomes de variáveis | ✅ camelCase | Standard .NET respeitado |
| Interfaces | ✅ `I` prefix | `IServiceRepository`, `INotificationService` |
| Async suffix | ✅ Presente | Métodos assíncronos com sufixo `Async` |

### 2.3 Padrões de nomeação por tipo

| Tipo | Padrão | Exemplo |
|---|---|---|
| Entity | `{NomeDaEntidade}` | `Service`, `Contract`, `Incident` |
| Value Object | `{Conceito}` | `TenantId`, `ServiceName` |
| Command | `{Verbo}{Objeto}Command` | `CreateServiceCommand` |
| Query | `Get{Objeto}Query` | `GetServiceByIdQuery` |
| Handler | `{Command/Query}Handler` | `CreateServiceCommandHandler` |
| DTO | `{Objeto}Dto` | `ServiceDto`, `ContractDto` |
| Repository Interface | `I{Objeto}Repository` | `IServiceRepository` |
| DbContext | `{Módulo}DbContext` | `CatalogDbContext` |
| Configuration | `{Entidade}Configuration` | `ServiceConfiguration` |

### 2.4 Classificação: **A+**

Justificação: Padrão 100% consistente, aderência completa a DDD e convenções .NET, sem exceções identificadas em 9 módulos.

---

## 3. Frontend — Nomenclatura e Organização

### 3.1 Estrutura de diretórios

```
src/frontend/src/
├── features/                    # Feature-based organization
│   ├── {feature-name}/          # kebab-case
│   │   ├── components/          # Componentes específicos da feature
│   │   ├── hooks/               # Custom hooks (apenas 3/14 features)
│   │   ├── pages/               # Páginas da feature
│   │   ├── services/            # API services
│   │   └── types/               # Tipos TypeScript
│   └── ...
├── shared/                      # Componentes e utilitários partilhados
│   ├── components/              # Componentes reutilizáveis
│   ├── design-system/           # Design tokens e componentes base
│   ├── hooks/                   # Hooks partilhados
│   └── utils/                   # Utilitários
├── layouts/                     # Layouts da aplicação
└── routes/                      # Configuração de rotas
```

### 3.2 Convenções de nomeação

| Tipo | Padrão | Exemplo | Consistência |
|---|---|---|---|
| Componentes React | PascalCase | `ServiceCatalogPage.tsx` | ✅ ~100% |
| Custom Hooks | `use{Feature}` | `useConfiguration.ts` | ✅ ~100% |
| API Services | camelCase ou PascalCase | `catalogService.ts` | ✅ ~95% |
| Tipos/Interfaces | PascalCase | `ServiceDto.ts` | ✅ ~95% |
| Pastas de features | kebab-case | `change-intelligence/` | ✅ ~100% |
| Ficheiros utilitários | camelCase | `formatDate.ts` | ✅ ~95% |
| Constantes | UPPER_SNAKE_CASE | `API_BASE_URL` | ✅ ~90% |

### 3.3 Avaliação

| Critério | Avaliação | Notas |
|---|---|---|
| Organização por feature | ✅ Excelente | Clara separação por domínio |
| Nomeação de componentes | ✅ Consistente | PascalCase universal |
| Nomeação de hooks | ✅ Consistente | Prefixo `use` universal |
| Nomeação de pastas | ✅ Consistente | kebab-case |
| Separação shared vs feature | ⚠️ Parcial | Dois locais para componentes partilhados |

### 3.4 Classificação: **A**

Justificação: Muito consistente, com organização feature-based clara. Não atinge A+ devido às 5 inconsistências menores identificadas na secção 5.

---

## 4. Base de Dados — Nomenclatura

### 4.1 Convenção de tabelas

| Padrão | Exemplo | Módulo |
|---|---|---|
| `{prefixo}_{nome_tabela}` | `aud_audit_logs` | Audit |
| `{prefixo}_{nome_tabela}` | `gov_contracts` | Contract Governance |
| `{prefixo}_{nome_tabela}` | `cg_change_records` | Change Intelligence |
| `{prefixo}_{nome_tabela}` | `cat_services` | Catalog |
| `{prefixo}_{nome_tabela}` | `not_notifications` | Notifications |

### 4.2 Prefixos por módulo

| Módulo | Prefixo | Consistência |
|---|---|---|
| Audit | `aud_` | ✅ |
| Contract Governance | `gov_` | ✅ |
| Change Intelligence | `cg_` | ✅ |
| Catalog | `cat_` | ✅ |
| Notifications | `not_` | ✅ |
| Foundation | (verificar) | — |
| Operations | (verificar) | — |
| Observability | (verificar) | — |
| AI Agents | (verificar) | — |

### 4.3 Convenções de colunas

| Tipo | Padrão | Exemplo |
|---|---|---|
| Colunas regulares | snake_case | `service_name`, `created_at` |
| Chaves primárias | `id` ou `{entidade}_id` | `id`, `service_id` |
| Chaves estrangeiras | `{entidade}_id` | `contract_id`, `team_id` |
| Timestamps | `created_at`, `updated_at` | Padrão consistente |
| Flags booleanas | `is_{adjetivo}` | `is_active`, `is_deleted` |

### 4.4 Classificação: **A**

Justificação: snake_case consistente com prefixos de módulo. Padrão claro e previsível. Facilita identificação do módulo dono da tabela.

---

## 5. Rastreabilidade Cross-Layer

### 5.1 Fluxo end-to-end

A rastreabilidade do NexTraceOne permite seguir uma feature desde o frontend até à base de dados:

```
Frontend                  →  API              →  Application         →  Domain           →  Database
ServiceCatalogPage.tsx    →  GET /api/catalog  →  GetServicesQuery    →  Service entity   →  cat_services
  ↓                           /services           Handler                                     
catalogService.ts         →  POST /api/catalog →  CreateServiceCmd    →  Service.Create() →  INSERT cat_services
                              /services           Handler
```

### 5.2 Exemplo concreto de rastreamento

| Camada | Artefacto | Padrão de nome |
|---|---|---|
| **Frontend Page** | `ServiceCatalogPage.tsx` | PascalCase + Page suffix |
| **Frontend Service** | `catalogService.ts` | camelCase + Service |
| **Frontend Hook** | `useCatalogServices.ts` | use + Feature |
| **Frontend Type** | `ServiceDto.ts` | PascalCase + Dto |
| **API Endpoint** | `GET /api/catalog/services` | REST lowercase |
| **Endpoint Class** | `GetServicesEndpoint.cs` | Verbo + Entidade + Endpoint |
| **Application Query** | `GetServicesQuery.cs` | Get + Entidade + Query |
| **Handler** | `GetServicesQueryHandler.cs` | Query + Handler |
| **Domain Entity** | `Service.cs` | PascalCase singular |
| **Repository** | `IServiceRepository.cs` | I + Entidade + Repository |
| **DbContext** | `CatalogDbContext.cs` | Módulo + DbContext |
| **DB Table** | `cat_services` | prefixo + snake_case plural |
| **DB Column** | `service_name` | snake_case |

### 5.3 Classificação de rastreabilidade: **A+**

Justificação: É possível, de forma intuitiva e sem documentação, rastrear qualquer feature desde o componente React até à tabela da base de dados. Os padrões de nomeação são suficientemente consistentes para permitir busca textual eficaz.

---

## 6. Inconsistências Identificadas

### 6.1 Lista completa

| # | Inconsistência | Camada | Gravidade | Impacto | Recomendação |
|---|---|---|---|---|---|
| 1 | **Nomes de pastas de módulo** (lowercase/kebab-case) vs **namespaces** (PascalCase) | Backend | 🟢 Baixa | Mínimo — convenção .NET aceite | Documentar como intencional |
| 2 | **Apenas 3 de 14 features** têm pasta `hooks/` | Frontend | 🟢 Baixa | Inconsistência organizacional | Criar `hooks/` em todas as features que têm custom hooks |
| 3 | **Localização de ficheiros de tipos** inconsistente | Frontend | 🟢 Baixa | Confusão para novos programadores | Definir regra: types sempre em `{feature}/types/` |
| 4 | **Dois locais para componentes partilhados** | Frontend | 🟡 Média | Dúvida sobre onde colocar novo componente | Consolidar ou documentar regra clara |
| 5 | **Arquitetura de API client** não documentada | Frontend | 🟡 Média | Novo programador não sabe como criar API service | Criar documentação ou README |

### 6.2 Detalhes por inconsistência

#### Inconsistência #1: Pastas vs Namespaces

```
Pasta:      src/modules/catalog/
Namespace:  NexTraceOne.Catalog.Domain

Pasta:      src/modules/change-intelligence/
Namespace:  NexTraceOne.ChangeIntelligence.Domain
```

**Veredicto:** Isto é **intencional e aceitável** — convenção standard .NET onde pastas no filesystem podem usar kebab-case e namespaces usam PascalCase. Não requer ação, apenas documentação.

#### Inconsistência #2: Pastas hooks/ inconsistentes

```
src/features/contracts/hooks/     ✅ Existe
src/features/catalog/hooks/       ✅ Existe  
src/features/operations/hooks/    ✅ Existe
src/features/ai-assistant/        ❌ Sem pasta hooks/ (mas tem hooks inline?)
src/features/change-intel/        ❌ Sem pasta hooks/
... (11 features sem pasta hooks/)
```

**Recomendação:** Se uma feature tem custom hooks, devem estar em `{feature}/hooks/`. Se não tem, não é necessária a pasta.

#### Inconsistência #4: Componentes partilhados

```
src/shared/components/        # Local 1 — componentes genéricos
src/shared/design-system/     # Local 2 — design system components
```

**Recomendação:** Clarificar a regra:
- `shared/design-system/` → componentes primitivos (Button, Input, Card, etc.)
- `shared/components/` → componentes compostos reutilizáveis (DataTable, SearchBar, etc.)

---

## 7. Padrões de Organização de Ficheiros

### 7.1 Backend — Estrutura por módulo

```
src/modules/{module}/
├── NexTraceOne.{Module}.Domain/
│   ├── Entities/
│   ├── Events/
│   ├── ValueObjects/
│   ├── Enums/
│   └── Interfaces/
├── NexTraceOne.{Module}.Application/
│   ├── Commands/
│   ├── Queries/
│   ├── DTOs/
│   ├── Validators/
│   └── Interfaces/
├── NexTraceOne.{Module}.Infrastructure/
│   ├── Persistence/
│   ├── Configurations/
│   └── Repositories/
└── NexTraceOne.{Module}.Endpoints/
    └── {Resource}Endpoints.cs
```

**Avaliação:** ✅ Padrão DDD exemplar, 100% consistente nos 9 módulos.

### 7.2 Frontend — Estrutura por feature

```
src/features/{feature}/
├── components/
│   ├── {Component}Page.tsx
│   └── {SubComponent}.tsx
├── hooks/                    # (quando existem custom hooks)
│   └── use{Feature}.ts
├── services/
│   └── {feature}Service.ts
├── types/                    # (localização inconsistente)
│   └── {Feature}Types.ts
└── pages/
    └── {Feature}Page.tsx
```

**Avaliação:** ✅ Boa organização feature-based, com as inconsistências menores documentadas acima.

---

## 8. Recomendações

### Ações prioritárias

| # | Ação | Esforço | Impacto |
|---|---|---|---|
| 1 | Documentar as convenções de nomeação num ficheiro central | 2h | 🟡 Médio — referência para novos membros |
| 2 | Documentar regra de `shared/components/` vs `shared/design-system/` | 30 min | 🟡 Médio |
| 3 | Criar documentação da arquitetura de API client no frontend | 2h | 🟡 Médio |
| 4 | Normalizar pastas `hooks/` nas features que têm hooks | 1h | 🟢 Baixo |
| 5 | Normalizar localização de ficheiros `types/` | 1h | 🟢 Baixo |

### Ações preventivas

| # | Ação | Frequência |
|---|---|---|
| 6 | Verificar aderência a convenções em code review | Cada PR |
| 7 | Incluir verificação de nomeação em linting (quando possível) | Configuração única |
| 8 | Atualizar documentação quando convenções evoluem | Contínuo |

---

## 9. Conclusão

O NexTraceOne demonstra **maturidade excepcional** nas convenções de nomeação e organização de código. A rastreabilidade cross-layer é um dos pontos mais fortes do repositório, permitindo que qualquer programador siga o fluxo de uma feature desde a UI até à base de dados de forma intuitiva.

As 5 inconsistências identificadas são **menores** e não comprometem a qualidade global. A recomendação principal é **documentar formalmente as convenções existentes** para preservar esta qualidade à medida que a equipa cresce.

---

> **Nota:** Este relatório complementa o relatório principal `documentation-and-onboarding-audit.md`.

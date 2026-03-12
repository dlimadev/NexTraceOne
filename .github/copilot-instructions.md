# Copilot Instructions

## Project Guidelines
- Neste repositório, usar código/logs/nomes em inglês, comentários XML em português, classes finais como sealed, CancellationToken em toda async, construtores privados em aggregates com factory methods, Result<T> para falhas, guard clauses no início, error codes como chaves i18n, strongly typed IDs, nunca DateTime.Now, nunca acessar DbContext de outro módulo e seguir docs/CONVENTIONS.md e docs/ARCHITECTURE.md.

---

## Critérios de Done do MVP1 — Por Perfil de Usuário

O MVP1 está pronto quando:

### DESENVOLVEDOR consegue:
- Importar uma API em menos de 5 minutos
- Ver exatamente o que mudou entre versões em 30 segundos
- Entender quais consumers serão afetados antes de submeter
- Submeter para workflow em menos de 2 minutos
- Receber feedback claro sobre o que bloqueia sua release

### TECH LEAD consegue:
- Aprovar uma mudança com contexto completo
- Ver o evidence pack completo de uma release
- Entender o Blast Radius sem abrir outra ferramenta
- Rejeitar com feedback estruturado
- Promover para Pre-Production e Production com gates reais

### PLATFORM ADMIN consegue:
- Configurar workflow templates por tipo de mudança
- Configurar rulesets e bindings
- Configurar ambientes e gates de promoção
- Gerenciar usuários e roles
- Validar a trilha de auditoria completa

### AUDITOR consegue:
- Encontrar qualquer mudança por quem/quando/o quê
- Verificar que o processo foi seguido para cada release
- Exportar evidências de um período em JSON ou CSV
- Verificar integridade criptográfica da trilha

---

## Métricas de Qualidade do MVP1

| Métrica | Alvo |
|---------|------|
| **Time to First Value** | < 30 minutos (do install à primeira API importada com diff visível) |
| **Time to Core Value** | < 7 dias (do install ao primeiro workflow completo aprovado) |
| **Cobertura de auditoria** | 100% (toda ação relevante gera AuditEvent) |
| **Disponibilidade alvo** | 99.5% (self-hosted, depende da infra do cliente) |

### Performance

| Operação | Alvo |
|----------|------|
| Import de contrato | < 5 segundos |
| Diff semântico | < 10 segundos |
| Cálculo de Blast Radius | < 5 segundos |
| Busca no catálogo | < 1 segundo |
| Audit search (90 dias) | < 3 segundos |

---

## Stack Técnica do MVP1

### Backend
- .NET 10 / ASP.NET Core 10
- Entity Framework Core 10 + Npgsql
- PostgreSQL 16 (dados principais + audit + search)
- MediatR (CQRS + pipeline behaviors)
- FluentValidation
- Quartz.NET (jobs: outbox processor, SLA checker, fingerprint capture, license validation)
- OpenTelemetry (tracing, metrics, logging)
- Serilog → OpenTelemetry exporter

### Frontend
- React 18 + TypeScript
- Vite
- TanStack Router
- TanStack Query
- Zustand
- Tailwind CSS
- Radix UI
- Apache ECharts (grafos e métricas)
- Playwright (testes e2e)

### Infraestrutura MVP1
- PostgreSQL 16 (único banco de dados)
- Sem Redis no MVP1 (cache em memória onde necessário)
- Sem OpenSearch no MVP1 (PostgreSQL FTS suficiente)
- Sem Temporal no MVP1 (workflow em state machine com Quartz.NET + PostgreSQL)
- SMTP para notificações de email

### Distribuição
- Docker Compose (para POC e avaliação)
- Instalador Windows (MSI) para produção self-hosted
- CLI distribuída como binário único (win-x64, linux-x64)

---

## Ordem de Desenvolvimento do MVP1

### FASE 1 — Fundação (Semanas 1–4)
- BuildingBlocks.Domain
- BuildingBlocks.Application
- BuildingBlocks.Infrastructure
- Identity & Access (básico)
- Licensing & Entitlements (básico)

### FASE 2 — Catálogo e Contratos (Semanas 5–8)
- Engineering Graph (Asset Catalog)
- Developer Portal (catálogo navegável)
- Contracts & Interoperability (import/export/diff)
- Ruleset Governance (upload + execução básica)

### FASE 3 — Inteligência de Mudança (Semanas 9–12)
- Releases & Change Intelligence (core)
- Deployment Notification Endpoint
- Blast Radius Engine (básico)
- Change Intelligence Score

### FASE 4 — Governança (Semanas 13–16)
- Workflow & Approval Engine
- Evidence Pack automático
- Promotion Governance
- Promotion Gates

### FASE 5 — Auditoria e IA Básica (Semanas 17–20)
- Audit & Traceability Layer completo
- IA básica (classificação + resumo de aprovação)
- External AI Consultation (Simple Mode)
- NexTrace CLI

### FASE 6 — Hardening e Lançamento (Semanas 21–24)
- Testes end-to-end completos
- Performance e otimização
- Documentação operacional
- Docker Compose e instalador
- Onboarding wizard e tour guiado

---

## Diretrizes Obrigatórias de Documentação, Idioma e Qualidade de Código

Estas regras são **obrigatórias** em toda alteração realizada no repositório. Todo contribuidor (humano ou IA) deve segui-las rigorosamente.

### 1. Documentação de Código em Português

- Todo código novo deve ser documentado em português.
- Sempre que tocar em código existente, revise comentários e documentação e:
  - traduza para português quando fizer sentido
  - melhore a clareza e a qualidade técnica da explicação
  - remova comentários pobres, óbvios, redundantes ou desatualizados
- Documente com nível sênior, explicando **intenção, contexto, regras e decisões**, não apenas o "o que".
- A documentação deve cobrir, sempre que aplicável:
  - classes, interfaces, enums, records / DTOs relevantes
  - métodos públicos e métodos privados complexos
  - fluxos críticos e regras de negócio
  - validações e decisões de segurança
  - contratos entre camadas
  - integrações com SSO/OIDC/SAML/SCIM
  - políticas de autorização
  - mecanismos de auditoria
  - comportamento multi-tenant
  - pontos de extensão enterprise
- Em .NET, prefira XML Documentation Comments (`///`) bem escritas em português para elementos públicos e relevantes.
- Em frontend, documente componentes, hooks, services, guards e trechos complexos em português.
- Não gere documentação superficial — a documentação deve ser útil para onboarding, manutenção e auditoria técnica.

### 2. Comentários Devem Explicar a Intenção

Evite comentários do tipo:
- "set variable"
- "loop through items"
- "check if null"

Prefira comentários que expliquem:
- por que a regra existe
- por que a abordagem foi escolhida
- riscos evitados
- impactos de segurança
- restrições de contrato
- comportamento em cenários edge cases
- relação com multi-tenancy, auditoria, autorização ou compliance

### 3. Melhorar o Código sem Quebrar Contratos

Ao alterar código existente:
- melhore legibilidade, coesão, naming e estrutura interna
- reduza complexidade acidental
- remova duplicações quando for seguro
- fortaleça validações e tratamento de erro
- melhore separação de responsabilidades
- aumente clareza das abstrações
- **preserve contratos públicos e integrações existentes**
- evite breaking changes desnecessárias
- se uma quebra de contrato for realmente inevitável, minimize impacto e documente explicitamente:
  - o motivo
  - os impactos
  - a alternativa considerada
  - o que precisa ser ajustado nos consumidores

### 4. Logs Sempre em Inglês

Todos os logs devem estar em inglês, incluindo:
- information, warning, error, critical
- audit/security logs
- observability messages
- operational diagnostics

Os logs devem ser: objetivos, consistentes, estruturados, úteis para operação e investigação, sem vazamento de segredo, token, senha ou dado sensível.

### 5. Exceptions Sempre em Inglês

Toda exception lançada no backend deve ter mensagem em inglês.

- ✅ correto: `"Tenant context is required for this operation."`
- ✅ correto: `"User is not eligible to request privileged access."`
- ❌ incorreto: `"Contexto do tenant é obrigatório."`
- ❌ incorreto: `"Usuário não pode solicitar acesso."`

A mensagem técnica da exception é para engenharia/operação e deve permanecer em inglês.

### 6. i18n para Todas as Mensagens Enviadas ao Frontend

Toda mensagem destinada ao frontend deve usar i18n. Não deixar textos hardcoded para exibição direta ao usuário.

Aplicar isso em:
- responses de erro para UI, mensagens de validação, mensagens de sucesso/aviso/confirmação
- títulos, labels, placeholders e textos de tela
- mensagens de access denied e sessão
- fluxos de break glass, JIT, delegação, access review
- notificações e banners

Regras:
- Backend deve preferir retornar **códigos/chaves estáveis de mensagem** quando fizer sentido.
- Frontend deve resolver a exibição via i18n/localização.
- Evitar acoplar texto final de UX à lógica de domínio.
- Não espalhar strings de UI pelo código.
- Centralizar catálogos/chaves/mapeamentos de tradução.
- Preparar o sistema para múltiplos idiomas, mesmo que inicialmente use apenas pt-PT/pt-BR e en.

### 7. Separação entre Mensagem Técnica e Mensagem de UX

Manter claramente separado:
- **Mensagem técnica interna** → em inglês, usada em exception/log
- **Mensagem exibida ao usuário** → via i18n

Exemplo esperado:
- exception interna: `"The requested delegation scope exceeds the grantor permissions."`
- código/chave para frontend: `"identity.delegation.scope_exceeds_grantor_permissions"`
- tradução exibida pela UI: definida no catálogo i18n

### 8. Padrão para Erros de API

Sempre que possível, padronize respostas de erro para o frontend com estrutura consistente:
- `code` — código do erro (chave i18n)
- `messageKey` — chave de tradução
- `params` — parâmetros dinâmicos para interpolação
- `correlationId` — para rastreabilidade
- `details` — quando apropriado e seguro

Objetivo: facilitar i18n, facilitar troubleshooting, evitar exposição de mensagens internas, manter UX consistente.

### 9. Revisão de Código Existente Inclui Documentação

Na revisão do módulo, tratar como gap também:
- ausência de documentação
- comentários fracos
- documentação inconsistente com o comportamento real
- comentários em inglês quando deveriam estar em português
- ausência de explicação em fluxos críticos
- falta de documentação de segurança e tenant isolation

### 10. Documentação Técnica de Apoio

Além da documentação inline no código, atualizar quando fizer sentido:
- README do módulo
- ADRs curtos, se o projeto usar
- notas de arquitetura
- docs de fluxo de autenticação/autorização
- docs de integração frontend/backend
- docs de configuração por ambiente/tenant
- docs de OIDC, grupos SSO, JIT, Break Glass, Delegação e Access Review

Toda documentação técnica complementar deve ser escrita em português.

### 11. Critério de Qualidade da Documentação

Considere a documentação boa apenas se outro desenvolvedor conseguir:
- entender rapidamente o propósito do módulo
- localizar pontos de extensão
- entender regras de autorização
- entender impactos multi-tenant
- entender as decisões de segurança
- manter o código com segurança
- evoluir o módulo sem quebrar contratos

### 12. Critério de Conclusão de Tarefa

A tarefa só estará completa se:
- funcionalidade estiver implementada
- testes estiverem adequados
- documentação inline estiver madura
- mensagens estiverem corretamente separadas entre técnico e UX
- logs e exceptions estiverem em inglês
- frontend estiver preparado para i18n
- código estiver mais claro e melhor do que antes, sem quebra de contrato desnecessária

### 13. i18n Obrigatório no Frontend

Além de usar i18n para mensagens vindas do backend, o frontend **também deve obrigatoriamente usar i18n em toda a interface**.

Regras:
- Não deixar textos hardcoded em componentes de frontend.
- Todo texto visível ao usuário deve vir de i18n.
- Isso inclui:
  - títulos, labels, menus, botões, placeholders
  - mensagens de erro, sucesso, avisos, tooltips
  - modais, banners, estados vazios, textos de loading
  - access denied, textos de sessão/autenticação/autorização
  - telas de seleção de tenant e ambiente
  - fluxos administrativos
- Revisar o frontend existente e substituir textos hardcoded por chaves de tradução.
- Organizar namespaces de tradução de forma clara, por exemplo:
  - `auth.*`
  - `authorization.*`
  - `tenant.*`
  - `environment.*`
  - `common.*`
  - `validation.*`
  - `errors.*`
- Garantir consistência entre backend e frontend:
  - backend retorna `code`/`messageKey`/`params` quando aplicável
  - frontend resolve a mensagem final via i18n
- Evitar espalhar strings literais pela UI.
- Se já existir uma estrutura de i18n no projeto, reutilizá-la e padronizá-la.
- Se a estrutura estiver incompleta, completar sem quebrar o padrão existente.
- Preparar o sistema para múltiplos idiomas (pt-BR, en) desde o início.

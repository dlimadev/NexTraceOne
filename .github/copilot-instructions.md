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

---

## Diretrizes Obrigatórias de Segurança, Privacidade e On-Premise

Estas regras são **obrigatórias** e aplicam-se a todo código novo ou alterado, frontend e backend.
Referência: `docs/security/` para detalhes completos.

### 14. Segurança como Regra Obrigatória

- Todo código deve ser **secure by default** — nenhuma configuração insegura por padrão.
- Seguir princípios de **defense in depth**, **least privilege** e **privacy by design**.
- Nunca confiar exclusivamente no frontend para enforcement de segurança — o backend é a fonte de verdade.
- Toda validação de entrada deve ocorrer no backend, independentemente do frontend.
- Tratar toda entrada como potencialmente maliciosa (query params, headers, body, uploads, URLs).

### 15. Renderização Segura no Frontend

- **Nunca** usar `dangerouslySetInnerHTML` com conteúdo não sanitizado.
- **Nunca** usar `innerHTML` fora de React sem sanitização.
- Se for necessário renderizar Markdown ou HTML externo, usar DOMPurify ou equivalente.
- i18n com `escapeValue: true` (padrão seguro) está ativo — não desativar.
- Validar URLs antes de usar em atributos `href`, `src`, `action` — usar `isSafeUrl()` do módulo `utils/sanitize.ts`.
- Bloquear esquemas `javascript:`, `data:`, `vbscript:` em URLs dinâmicas.

### 16. Navegação e Redirecionamento Seguros

- Usar `isSafeRedirectPath()` de `utils/navigation.ts` para todo redirecionamento baseado em input externo.
- Allowlist de rotas internas para prevenção de open redirect.
- Nunca redirecionar diretamente para URLs vindas de query string, localStorage ou API sem validação.
- URLs externas devem abrir em nova aba (`target="_blank"`) com `rel="noopener noreferrer"`.

### 17. Sessão, Tokens e Dados Sensíveis

- **Refresh token**: exclusivamente em memória (closure) — nunca em localStorage ou sessionStorage.
- **Access token**: sessionStorage (escopo de aba) — nunca em localStorage.
- **Nenhum token em localStorage** — localStorage é acessível entre abas e persiste após fechar o browser.
- Limpar todos os dados de sessão em logout e session expired.
- Nunca logar tokens, passwords ou credenciais — nem no console, nem em logs do servidor.
- Usar `clearAllTokens()` para garantir limpeza completa.

### 18. Autorização Coerente entre Frontend e Backend

- Frontend usa permissões recebidas do backend (via `/me`) apenas para controle visual (UX).
- Backend faz enforcement real — toda API deve verificar autenticação e autorização.
- Não manter mapeamento client-side de role→permissões; obter do servidor.
- Usar o hook `usePermissions()` e `<ProtectedRoute>` para controle visual no frontend.
- Ocultar/desabilitar ações não autorizadas — nunca confiar que ocultação visual é suficiente.

### 19. API Client e Tratamento Seguro de Erros

- Usar o API client centralizado (`api/client.ts`) para todas as requisições.
- Nunca montar URLs concatenando strings sem validação.
- Nunca exibir mensagens técnicas ou stack traces ao usuário — usar `resolveApiError()` + i18n.
- ErrorBoundary global captura erros não tratados sem expor detalhes em produção.
- `console.log` e `debugger` são removidos automaticamente em builds de produção via terser.

### 20. Dependências e Supply Chain

- Manter `package-lock.json` versionado no repositório.
- Executar `npm audit` antes de releases.
- Executar `dotnet list package --vulnerable` antes de releases.
- Não instalar dependências sem verificar advisory databases.
- Preferir dependências com manutenção ativa e licenças compatíveis.

### 21. Compatibilidade com CSP, Headers, CORS

- Frontend deve ser compatível com CSP strict (sem `unsafe-eval`, sem `unsafe-inline` para scripts).
- Não usar `eval()`, `new Function()`, ou `setTimeout` com strings.
- Backend define security headers completos (CSP, X-Frame-Options, HSTS, etc.).
- CORS restritivo com origens explícitas — nunca wildcard com credentials.
- Meta tag CSP no `index.html` como defense-in-depth.

### 22. LGPD / GDPR-RGPD / Minimização de Dados / Masking / Logs

- **Minimização de dados**: não carregar, persistir, logar ou exibir dados pessoais além do necessário.
- **Frontend**: não persistir dados pessoais em localStorage; limpar sessionStorage em logout.
- **Backend**: não retornar dados pessoais desnecessários em respostas API.
- **Logs**: nunca registrar passwords, tokens, emails completos ou outros PII em logs.
- **Exports/Downloads**: garantir que exports não incluem dados pessoais desnecessários.
- **Masking**: considerar masking parcial de emails, IPs e identificadores na UI quando completo não é necessário.
- **Retenção**: definir e aplicar políticas de retenção para dados com PII.
- Referência: `docs/security/application-privacy-lgpd-gdpr-notes.md`.

### 23. Execução On-Premise

- **Nunca empacotar ou distribuir código-fonte** (.cs, .tsx, .ts, .csproj, .sln, testes).
- Distribuir apenas artefatos compilados/publicados (assemblies .NET, JS/CSS minificados).
- **Source maps desativados** em produção (`sourcemap: false`).
- **Debug symbols não incluídos** em distribuição de produção (`DebugType=none`).
- **console.log removido** em builds de produção via terser (`drop_console: true`).
- Nomes de assets com hash (sem exposição de estrutura interna).
- **Externalizar segredos**: connection strings, JWT secret, license key via variáveis de ambiente.
- Nunca incluir valores sensíveis em `appsettings.json` distribuído.
- Verificação de integridade de assemblies no boot da aplicação.
- Referência: `docs/security/application-onprem-hardening-notes.md`.

### 24. ErrorBoundary e Observabilidade Segura

- Manter `ErrorBoundary` global envolvendo `<App />` no `main.tsx`.
- ErrorBoundary nunca expõe stack traces ou detalhes técnicos em produção.
- Registrar erros no console apenas em desenvolvimento (`import.meta.env.DEV`).
- Usar Serilog com logger estruturado no backend — logs em inglês, sem PII.
- OpenTelemetry para tracing e métricas — não incluir dados pessoais em spans/metrics.

---

## Diretrizes Obrigatórias de Arquitetura, SOLID e Separação de Responsabilidades

Estas regras são **obrigatórias** em toda alteração realizada no repositório. Todo contribuidor (humano ou IA) deve segui-las rigorosamente.

### 25. SOLID e SRP como Padrão Obrigatório

- Toda nova classe deve ter **responsabilidade clara e limitada** — uma única razão para mudar.
- Evitar classes "faz-tudo" (God Objects) que centralizam regras, persistência, validação e integração.
- Evitar controllers/endpoints gordos — devem apenas delegar para MediatR handlers.
- Evitar services inflados que misturam autorização, validação, logging, persistência e regra de negócio.
- Evitar helpers/utils genéricos que escondem regra de negócio importante — regras devem estar no layer correto.
- Código duplicado entre behaviors, interceptors ou services deve ser extraído para utilitários compartilhados (exemplo: `ResultResponseFactory` para lógica de reflection sobre `Result<T>`).
- Preferir classes pequenas (~50-150 linhas) com responsabilidade bem definida.

### 26. Separação Clara entre Layers

- **Domain** — entidades, value objects, aggregates, domain events, domain services, specifications. Nunca depende de infraestrutura.
- **Application** — handlers (CQRS), validators, behaviors, application services. Orquestra casos de uso. Não contém detalhes de persistência.
- **Infrastructure** — repositories, DbContext, interceptors, serviços externos, integrações. Detalhes técnicos isolados aqui.
- **API** — endpoints Minimal API que apenas delegam para MediatR. Sem regra de negócio, sem acesso direto a repositório.
- **Contracts** — DTOs, integration events, interfaces públicas entre módulos. Único ponto de comunicação cross-module.

Regras inegociáveis:
- Domain **nunca** referencia Infrastructure, Application ou API.
- Application **nunca** referencia Infrastructure ou API.
- API **nunca** contém regra de negócio.
- Módulos comunicam via Integration Events (Outbox Pattern) ou interfaces definidas em Contracts.

### 27. Regras para Entidades de Domínio

- Entidades devem **preservar invariantes** via métodos de domínio e factory methods.
- Entidades **não devem** conter lógica de infraestrutura, persistência, logging ou integração.
- Entidades **não devem** ser God Objects com centenas de linhas de switch/if para catálogos de dados — extrair para services ou catálogos dedicados (exemplo: `RolePermissionCatalog` separado de `Role`).
- Usar **Value Objects** para encapsular validação e igualdade por valor (Email, SemanticVersion, etc.).
- Usar **Domain Services** para operações que envolvem múltiplos aggregates ou lógica complexa que não pertence a um único entity.
- Evitar mutabilidade excessiva — preferir private setters com métodos de domínio controlados.
- Constantes e catálogos extensos devem ficar em classes separadas (exemplo: `SecurityEventType` em arquivo próprio).

### 28. Regras para Services, Handlers e Behaviors

- Handlers devem focar em **um único caso de uso** — seguir o padrão Vertical Slice Architecture (VSA).
- Cada feature deve conter: Command/Query + Validator + Handler + Response em um único arquivo.
- Services não devem concentrar múltiplas responsabilidades — se precisar de mais de 200 linhas, considerar extração.
- Validações devem estar em FluentValidation validators, não espalhadas no handler.
- Lógica de parsing, cálculo ou transformação complexa deve ser extraída para **domain services** (exemplo: `OpenApiDiffCalculator` separado do handler `ComputeSemanticDiff`).
- Behaviors de pipeline (MediatR) devem ser focados e reutilizar utilitários compartilhados (exemplo: `ResultResponseFactory`).

### 29. Regras para Endpoints e API

- Endpoint modules devem ser organizados por **domínio/grupo lógico** — não concentrar 30+ endpoints em um único arquivo.
- Padrão: um arquivo de endpoints por grupo funcional (AuthEndpoints, UserEndpoints, etc.) com um orquestrador principal.
- Cada endpoint deve: receber request → enviar para MediatR → retornar resultado via `ToHttpResult()`.
- **Nunca** colocar regra de negócio em endpoint — toda lógica fica no handler.
- Autorização deve ser declarativa via `.RequirePermission()` ou `.RequireAuthorization()`.

### 30. Regras para Frontend

- Componentes não devem concentrar fetch, autorização, transformação pesada e renderização no mesmo lugar.
- Extrair hooks/services/helpers específicos quando necessário.
- Toda mensagem enviada ao usuário deve usar i18n — nunca textos hardcoded.
- Componentes devem ter responsabilidade clara: apresentação, ou lógica, ou data fetching — não todos juntos.

### 31. Regras de Evolução Segura e Refatoração

- **Preservar contratos públicos** sempre que possível — interfaces, DTOs, endpoints.
- Evitar breaking changes desnecessárias.
- Preferir **refatoração incremental e cirúrgica** — não reescrever módulos inteiros sem necessidade.
- Toda refatoração relevante deve vir com **testes que protejam o comportamento**.
- Se precisar quebrar contrato, documentar: motivo, impacto, alternativa, ajustes nos consumidores.
- Ao extrair responsabilidades, manter delegação no ponto original para backward compatibility quando possível.

### 32. Sinais de Alerta para Revisão

Código que apresente estes sinais deve ser refatorado:
- Classe com mais de 300 linhas
- Mais de 5 dependências injetadas
- Mais de 3 razões distintas para mudar
- Switch/if extenso baseado em tipo/role/status que poderia ser polimorfismo
- Lógica duplicada entre behaviors, interceptors ou handlers
- Handler com mais de 150 linhas de lógica
- Endpoint module com mais de 15 endpoints
- Entidade com métodos que acessam infraestrutura
- Utilitário genérico que esconde regra de negócio
- Background job orquestrando múltiplos tipos de entidade no mesmo arquivo
- Program.cs com mais de 100 linhas de lógica (extrair extension methods)

### 33. Regras para Background Jobs e Workers

- Cada tipo de processamento batch deve ser isolado em um **handler especializado** com interface dedicada.
- Jobs de orquestração devem apenas coordenar handlers — não conter lógica de negócio, queries ou persistência.
- Exemplo: `IdentityExpirationJob` orquestra `IExpirationHandler[]`, cada handler processa um único tipo de entidade.
- Falhas em um handler não devem impactar outros — isolamento por try/catch obrigatório.
- Padrão de interface para handlers de batch: receber DbContext, timestamp, batchSize e CancellationToken.
- Registro de novos handlers no DI é suficiente para estendê-los — Open/Closed Principle aplicado.
- Cada handler deve gerar seus próprios eventos de auditoria (SecurityEvent) quando aplicável.
- Logs devem identificar o handler por nome para facilitar troubleshooting.

### 34. Regras para Composition Root (Program.cs / Host)

- Program.cs deve ser **fino e declarativo** — apenas orquestra a composição, não implementa lógica.
- Extrair responsabilidades complexas em **extension methods** nomeados e documentados.
- Padrão: `builder.AddCorsConfiguration()`, `app.ApplyDatabaseMigrationsAsync()`, `app.UseSecurityHeaders()`.
- Cada extension method deve ter uma única responsabilidade configuracional.
- Lógica de migração de banco deve ser extraída para extension method dedicado com logging estruturado.
- Security headers devem estar em extension method próprio para reutilização e testabilidade.
- Validações de configuração (ex: CORS wildcard check) devem ficar no extension method correspondente.

### 35. Regras para Domain Services Puros (Sem I/O)

- Domain services estáticos que fazem cálculos puros devem separar **parsing de dados** de **lógica de comparação/cálculo**.
- Exemplo: `OpenApiSpecParser` (parsing JSON) separado de `OpenApiDiffCalculator` (detecção de mudanças).
- Parser retorna estruturas intermediárias; Calculator opera sobre essas estruturas.
- Benefícios: testabilidade isolada, reutilização do parser, Calculator independente do formato de entrada.
- Em caso de specs malformadas ou dados inválidos, parsers devem retornar estruturas vazias (não lançar exceções) para não bloquear o processamento.

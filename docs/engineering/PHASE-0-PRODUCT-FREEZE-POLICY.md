# PHASE-0 — NexTraceOne Product Freeze Policy

**Status:** ACTIVE — Effective immediately  
**Owner:** Engineering Leadership  
**Version:** 1.0.0  
**Date:** 2026-03-21

---

## 1. Princípio Mestre

> **"NexTraceOne será fechado como produto real. Não aceitaremos demo, preview, MVP, mocks operacionais, stubs de negócio ou comportamento fake em fluxos core."**

A partir desta política, o NexTraceOne deve operar sob a diretriz:

**Nenhuma funcionalidade será considerada pronta se depender de mock local, stub funcional, hardcode operacional, retorno simulado, banner demo, fallback inseguro ou comportamento que não represente uso enterprise real.**

---

## 2. Definições Formais

### 2.1 Mock

| Situação | Classificação | Permitido |
|---|---|---|
| `mockData` em arquivo de teste (`.test.ts`, `.spec.ts`, `__tests__/`) | Mock de teste | ✅ Aceitável |
| `const mockServices = [...]` em página operacional de produto | Mock proibido em runtime | ❌ Proibido |
| `mockHandlers` em setup de MSW para testes | Mock de teste | ✅ Aceitável |
| `mockDetails`, `mockJobs`, `mockQueues`, `mockEvents` em páginas core | Mock proibido em runtime | ❌ Proibido |

### 2.2 Stub

| Situação | Classificação | Permitido |
|---|---|---|
| Stub em camada de teste unitário isolado | Stub de teste | ✅ Aceitável |
| Handler CQRS com corpo `// TODO: Implementar` exposto via endpoint | Stub proibido em handler | ❌ Proibido |
| Interface de contrato sem implementação marcada como `// IMPLEMENTATION STATUS: Planned` | Stub documentado | ✅ Aceitável com rastreamento |
| Handler retornando `return Result.Success(new Response())` sem persistência | Stub proibido em handler | ❌ Proibido |

### 2.3 Hardcode

| Situação | Classificação | Permitido |
|---|---|---|
| `"Password=nextraceone_dev"` apenas em `appsettings.Development.json` | Hardcode de dev | ✅ Aceitável |
| `"Password=postgres"` em código de produção ou configuração não-dev | Hardcode proibido | ❌ Proibido |
| `Environment: "Production"` fixo em DTO de handler operacional | Hardcode proibido em fluxo | ❌ Proibido |
| `RuleCount: 0` em handler sem implementação de contagem real | Hardcode proibido em fluxo | ❌ Proibido |
| Constantes de configuração fixas em `appsettings.json` sem override | Hardcode aceitável | ✅ Aceitável |

### 2.4 IsSimulated

| Situação | Classificação | Permitido |
|---|---|---|
| `IsSimulated = true` em Response DTO de handler core com dados reais ausentes | Backend fake catalogado | ⚠️ Catalogado — deve ser removido em Fase 1+ |
| `IsSimulated = true` em Response DTO sem correspondente `DemoBanner` no frontend | Backend fake sem sinalização | ❌ Proibido |
| `GenerateSimulatedItems()` em handler de produção | Backend fake | ❌ Proibido em Fase 1+ |

### 2.5 Estado de Implementação

| Situação | Classificação |
|---|---|
| **Tela parcialmente pronta** | Frontend conectado ao backend, mas backend retorna dados parciais com `IsSimulated = true` declarado explicitamente |
| **Funcionalidade não pronta** | Backend com `// TODO: Implementar`, handler vazio, ou frontend com mock local — sem sinalizaçao ao utilizador |
| **Pronto para produção** | Backend real, persistência implementada, frontend conectado, sem mock/stub/hardcode funcional, testes passando |
| **Bloqueador absoluto** | Item P0 que impede qualquer deploy seguro (credencial hardcoded, devtool exposto, secret ausente, auto-migration em produção) |
| **Fake implementation** | Qualquer combinação de: mock local em página operacional + IsSimulated + GenerateSimulated + TODO crítico + fallback inseguro |

---

## 3. Regras Obrigatórias de Engenharia

### 3.1 Regras de Backend

1. **Nenhum handler core** pode retornar `IsSimulated = true` em ambiente de produção real sem que o dado seja sinalizados como indisponível.
2. **Nenhum handler** pode ser registrado em endpoint exposto com corpo `// TODO: Implementar` sem ao menos retornar erro explícito (`NotImplemented` ou `PreviewOnly`).
3. **Nenhum DTO** pode conter valores hardcoded para campos operacionais como `Environment`, `RuleCount`, `AuthenticationMode` em handlers de produção.
4. **Nenhum método** `GenerateSimulated*` pode persistir em handlers de production path após a conclusão da Fase 1.
5. **Nenhum fallback de credencial** default (ex: `Password=postgres`) pode existir em configuração não-dev.
6. **Toda feature** que não esteja totalmente implementada deve retornar `Result.Failure` com código de erro documentado, não retorno vazio ou simulado.

### 3.2 Regras de Frontend

1. **Nenhuma página operacional** pode usar `const mock*` arrays locais para alimentar dados visíveis ao utilizador em build não-dev.
2. **Nenhuma página** que consuma handler com `IsSimulated = true` pode existir sem `<DemoBanner />` visível.
3. **`ReactQueryDevtools`** deve ser carregado exclusivamente em build DEV via guard `import.meta.env.DEV`.
4. **Nenhum banner** de "Demo Data", "Preview Data" ou "Sample Data" pode aparecer em builds de produção sem que o dado seja explicitamente marcado como não-real.
5. **Toda página** deve implementar estados reais de loading, error e empty — não apenas o estado de sucesso com dados fake.

### 3.3 Regras de Infraestrutura e Segurança

1. **Auto-migrations** não podem executar em ambiente de produção (`ApplyDatabaseMigrationsAsync` bloqueia startup se `NEXTRACE_AUTO_MIGRATE=true` em Production).
2. **`IntegrityCheck: false`** não pode ser o valor padrão em `appsettings.json` — deve ser `true` exceto em ambientes explicitamente documentados.
3. **Nenhum devtool** de debug pode ser exposto em builds de produção.
4. **Todo endpoint** deve ter autenticação e autorização validadas e testadas.
5. **Secrets** devem ser injetados via variáveis de ambiente ou secrets manager — nunca commitados.

### 3.4 Regra de Não-Remoção

> **Não remova módulos, páginas ou capacidades apenas porque estão incompletos.**

- Se algo existir e estiver incompleto: manter visível, registrar como pendência, marcar como backlog de finalização.
- Não ocultar menus ou módulos para "aumentar sensação de completude".
- Não remover capacidade de produto sem justificativa arquitetural forte e relatório documentado.

---

## 4. Critério de Pronto Padrão do Produto (Definition of Done)

Uma feature só está **pronta** quando todos os critérios abaixo são atendidos:

### 4.1 Backend
- [ ] Handler implementado com lógica real (não simulada)
- [ ] Persistência implementada quando a feature requer estado
- [ ] Validação de entrada (FluentValidation ou equivalente)
- [ ] Retorno de erros tipados e rastreáveis
- [ ] CancellationToken propagado em toda async
- [ ] Multi-tenancy respeitado quando aplicável
- [ ] Sem `IsSimulated = true` em resultado final

### 4.2 Frontend
- [ ] Página conectada ao endpoint real (não a dados locais)
- [ ] Estado de loading implementado
- [ ] Estado de erro implementado
- [ ] Estado vazio (empty state) implementado
- [ ] `DemoBanner` removido quando backend real está disponível
- [ ] i18n aplicado em todo texto visível
- [ ] Sem `const mock*` arrays locais

### 4.3 Integração e Testes
- [ ] Teste unitário do handler
- [ ] Teste de integração do endpoint (quando aplicável)
- [ ] Teste de componente frontend (quando aplicável)
- [ ] Build sem erros de TypeScript
- [ ] Sem warnings de lint relevantes

### 4.4 Segurança e Configuração
- [ ] Nenhum secret hardcoded
- [ ] Autenticação e autorização verificadas
- [ ] Configuração via variáveis de ambiente para valores sensíveis
- [ ] Observabilidade mínima (logs estruturados no handler)

### 4.5 Documentação
- [ ] Comportamento documentado (inline ou em docs/)
- [ ] Breaking changes documentados
- [ ] Critérios de aceite atendidos

---

## 5. O que é Proibido a Partir desta Política

| # | Proibido | Consequência |
|---|---|---|
| P-01 | `const mock*` em páginas operacionais fora de testes | Bloqueio de merge |
| P-02 | `ReactQueryDevtools` sem guard `import.meta.env.DEV` | Bloqueio de merge |
| P-03 | `Password=postgres` em código ou config não-dev | Bloqueio de merge |
| P-04 | Handler exposto com `TODO: Implementar` sem retorno de erro | Bloqueio de merge |
| P-05 | Auto-migration ativa em produção | Bloqueio de startup |
| P-06 | `IsSimulated = true` sem `DemoBanner` correspondente no frontend | Bloqueio de merge |
| P-07 | Remover módulo/menu para "aumentar completude" | Proibido — requer ADR |
| P-08 | Feature marcada como concluída sem backend real | Proibido |
| P-09 | Secret ou credencial commitada em qualquer branch | Bloqueio imediato |
| P-10 | Devtool de debug exposto em build de produção | Bloqueio de merge |

---

## 6. Processo de Exceção

Quando uma exceção a esta política for necessária:

1. Criar issue no repositório com label `phase0-exception`
2. Documentar: razão técnica, prazo de resolução, risco associado
3. Obter aprovação de pelo menos um Tech Lead ou Architect
4. Registrar no inventário de dívida (`PHASE-0-DEMO-DEBT-INVENTORY.md`)
5. Criar ticket de fechamento na fase adequada

---

## 7. Responsabilidades

| Papel | Responsabilidade |
|---|---|
| **Engineer** | Não introduzir novos padrões proibidos; corrigir ao encontrar |
| **Tech Lead** | Revisar PRs sob esta política; aprovar exceções |
| **Architect** | Manter política atualizada; validar conformidade arquitetural |
| **Platform Admin** | Garantir que guardrails de CI/CD aplicam esta política |

---

## 8. Referências

- `docs/audits/PHASE-0-DEMO-DEBT-INVENTORY.md` — Inventário técnico completo
- `docs/roadmap/PHASE-0-FINALIZATION-BACKLOG.md` — Backlog de fechamento
- `docs/engineering/PRODUCT-DEFINITION-OF-DONE.md` — DoD oficial
- `docs/engineering/ANTI-DEMO-REGRESSION-CHECKLIST.md` — Checklist anti-regressão
- `scripts/quality/check-no-demo-artifacts.sh` — Script de verificação automatizada
- `docs/architecture/ADR-004-simulated-data-policy.md` — ADR de política de dados simulados

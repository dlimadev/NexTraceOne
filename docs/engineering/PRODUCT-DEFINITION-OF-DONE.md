# NexTraceOne — Product Definition of Done

**Status:** ACTIVE — Política oficial de conclusão de trabalho  
**Owner:** Engineering Leadership  
**Version:** 1.0.0  
**Date:** 2026-03-21

---

## Princípio

> **Uma feature só está pronta quando representa comportamento real, seguro, testado e documentado — nunca quando parece pronta.**

Este documento é a autoridade sobre o que significa "concluído" no NexTraceOne.  
Nenhuma feature, story, task ou bugfix deve ser marcada como concluída sem satisfazer os critérios aplicáveis.

---

## DoD 1 — Backend

Uma feature de backend está pronta quando:

- [ ] **Handler implementado** com lógica real — sem `TODO: Implementar`, sem `GenerateSimulated*`, sem retorno estático de dados fictícios
- [ ] **Persistência implementada** quando a feature requer estado (CRUD via DbContext real, migration aplicada)
- [ ] **Validação de entrada** implementada (FluentValidation ou equivalente) e testada
- [ ] **Erros tipados** retornados — `Result.Failure(ErrorCode, message)` para casos de negócio; sem excepções silenciosas
- [ ] **CancellationToken** propagado em todos os métodos async
- [ ] **Multi-tenancy** respeitado: queries filtradas por TenantId quando aplicável
- [ ] **Autorização** verificada via atributo ou policy — endpoint não é anónimo sem intenção explícita
- [ ] **IsSimulated ausente** do Response DTO de produção — ou se presente, declarado explicitamente e o frontend exibe `DemoBanner`
- [ ] **Sem hardcodes** de valores operacionais no handler (environments, role names, team names, etc.)
- [ ] **Logs estruturados** presentes nos pontos relevantes (erros, operações críticas)
- [ ] **Testes unitários** do handler (pelo menos caminho feliz + caminho de erro principal)

---

## DoD 2 — Frontend

Uma feature de frontend está pronta quando:

- [ ] **Conectada ao endpoint real** — sem `const mock*` arrays locais para dados operacionais
- [ ] **Estado de loading** implementado (`PageLoadingState` ou equivalente)
- [ ] **Estado de erro** implementado (`PageErrorState` ou equivalente) com mensagem i18n
- [ ] **Estado vazio (empty state)** implementado quando a lista pode ser vazia
- [ ] **`DemoBanner`** ausente quando o backend está real — ou presente e correcto quando o backend ainda retorna `IsSimulated = true`
- [ ] **i18n aplicado** em absolutamente todo texto visível: títulos, labels, botões, placeholders, tooltips, mensagens de erro, estados vazios
- [ ] **Acessibilidade mínima**: labels com `htmlFor`, roles para elementos clicáveis não-button, tipos de botão explícitos
- [ ] **Responsividade** básica com breakpoints (`md:`, `lg:` etc.) para componentes de grid
- [ ] **ReactQueryDevtools** não presente sem guard `import.meta.env.DEV`
- [ ] **TypeScript** sem erros (`tsc --noEmit` passa)
- [ ] **Testes de componente** para páginas e componentes complexos

---

## DoD 3 — Integração entre Módulos

Uma integração entre módulos está pronta quando:

- [ ] **Comunicação via contrato**: módulos comunicam por eventos de domínio ou DTOs tipados — não via dependência direta de implementação
- [ ] **Evento publicado** quando o módulo origem altera estado relevante para outros módulos
- [ ] **Handler de evento** implementado no módulo destino (não apenas o evento publicado)
- [ ] **IdempotencyKey** verificado em handlers de evento para prevenir processamento duplicado
- [ ] **Erro de integração** tratado explicitamente — falha em módulo destino não propaga para origem
- [ ] **Teste de integração** validando o fluxo end-to-end da integração

---

## DoD 4 — Segurança

Uma feature está pronta em termos de segurança quando:

- [ ] **Autenticação** verificada — endpoint requer JWT válido ou tem isenção explícita documentada
- [ ] **Autorização** verificada — acesso restrito por role/policy quando necessário
- [ ] **Entrada sanitizada** — sem injecção de SQL, XSS, ou path traversal possível
- [ ] **Sem secrets hardcoded** — credenciais, chaves de API, connection strings sensíveis via variáveis de ambiente
- [ ] **HTTPS enforced** — `UseHttpsRedirection` activo; sem comunicação não-encriptada em produção
- [ ] **Rate limiting** considerado para endpoints expostos publicamente
- [ ] **Headers de segurança** activos (`UseSecurityHeaders` presente no pipeline)

---

## DoD 5 — Banco de Dados / Migrations

Uma alteração de banco de dados está pronta quando:

- [ ] **Migration criada** com `dotnet ef migrations add` — nunca modificação directa do schema
- [ ] **Migration revisada** — verificar que não há perda de dados acidental; colunas nullable para novos campos
- [ ] **Migration reversível** quando possível — `Down()` implementado correctamente
- [ ] **Índices** criados para queries frequentes (foreign keys, filtros de tenant, filtros temporais)
- [ ] **Migration testada** localmente — `dotnet ef database update` executa sem erros
- [ ] **Sem auto-migration em produção** — `NEXTRACE_AUTO_MIGRATE=true` não configurado em produção
- [ ] **Nomenclatura de tabela** segue prefixo de módulo (ex: `cat_`, `oi_`, `gov_`, `ia_`)

---

## DoD 6 — Observabilidade

Uma feature tem observabilidade adequada quando:

- [ ] **Log de erro** registado com contexto (operationId, tenantId, requestId quando disponível)
- [ ] **Log de operação crítica** registado (ex: contrato publicado, incidente criado, change aprovado)
- [ ] **Span OpenTelemetry** presente para operações de longa duração ou alta frequência (quando configurado)
- [ ] **Métrica** registada para operações que precisam de monitoramento (ex: taxa de erros, latência)
- [ ] **Health check** actualizado se a feature introduz nova dependência externa

---

## DoD 7 — Testes

Uma feature tem cobertura adequada quando:

- [ ] **Testes unitários** cobrem: caminho feliz, validação inválida, erros de negócio
- [ ] **Testes de integração** cobrem endpoints críticos (quando infraestrutura de teste disponível)
- [ ] **Testes de componente React** cobrem: render, interacção, estado de loading/error/empty
- [ ] **Testes não quebram** funcionalidade existente — `dotnet test` e `vitest run` passam sem regressões
- [ ] **Testes não testam mocks** — testes de honestidade funcional (`SimulatedDataHonestyTests`) devem ser actualizados quando backend for real
- [ ] **Nenhum teste** consolida comportamento fake como correcto

---

## DoD 8 — Documentação

Uma feature está documentada quando:

- [ ] **Comportamento documentado** inline (XML doc no handler, JSDoc no componente) para lógica não-óbvia
- [ ] **Breaking changes** documentados em CHANGELOG ou ADR se alteram contrato público
- [ ] **Endpoint documentado** no OpenAPI spec (summary, description, responses)
- [ ] **Configuração nova** documentada em `appsettings.json` com comentário ou README actualizado
- [ ] **Decisão arquitetural relevante** registada como ADR em `docs/architecture/adr/`

---

## DoD 9 — Production Readiness

Uma feature está pronta para produção quando todos os DoDs acima são satisfeitos **e**:

- [ ] **Nenhum `IsSimulated = true`** no Response DTO produzido em runtime de produção
- [ ] **Nenhum `const mock*`** em página operacional fora de testes
- [ ] **Nenhum `TODO: Implementar`** em path de execução exposto
- [ ] **Nenhuma credencial hardcoded** fora de `appsettings.Development.json`
- [ ] **`ReactQueryDevtools`** não presente sem guard de ambiente
- [ ] **Build de produção** (`dotnet publish`, `npm run build`) executa sem erros ou warnings relevantes
- [ ] **Smoke test** executado contra ambiente staging confirma comportamento esperado

---

## Aplicação do DoD

### Em PRs
O reviewer deve verificar os critérios aplicáveis antes de aprovar.  
PRs que introduzam padrões proibidos (ver `PHASE-0-PRODUCT-FREEZE-POLICY.md`) devem ser rejeitados.

### Em Features Parciais
Se uma feature está parcialmente implementada mas deve ser entregue:
1. Backend retorna `Result.Failure` com código `NotImplemented` ou `PreviewOnly`
2. Frontend exibe estado vazio ou `DemoBanner` adequado
3. Issue de fechamento criado e linkado
4. Item registado no inventário de dívida (`PHASE-0-DEMO-DEBT-INVENTORY.md`)

### Em Excepções
Seguir processo documentado em `PHASE-0-PRODUCT-FREEZE-POLICY.md` §6.

---

## Referências

- `docs/engineering/PHASE-0-PRODUCT-FREEZE-POLICY.md`
- `docs/engineering/ANTI-DEMO-REGRESSION-CHECKLIST.md`
- `docs/audits/PHASE-0-DEMO-DEBT-INVENTORY.md`
- `scripts/quality/check-no-demo-artifacts.sh`

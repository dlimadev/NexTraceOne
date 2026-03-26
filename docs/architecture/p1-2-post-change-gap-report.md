# P1.2 — Post-Change Gap Report

**Data de execução:** 2026-03-26  
**Fase:** P1.2 — Security Pipeline Integrity Fix  
**Estado:** CONCLUÍDO COM LIMITAÇÕES RESIDUAIS DOCUMENTADAS

---

## 1. O que foi resolvido

| Item | Detalhe |
|---|---|
| `--build-arg NEXTRACE_SKIP_INTEGRITY=true` removido de `security.yml` | Pipeline de segurança já não documenta nem transmite intenção de bypass |
| Todos os outros workflows verificados | Nenhum outro workflow usa o bypass |
| Análise técnica do bypass documentada | Confirmado que o `--build-arg` era ignorado pelos Dockerfiles mas era problemático por intenção |

---

## 2. Limitações residuais identificadas

### 2.1 Pipeline de assinatura de assemblies não existe (CRÍTICO)

**Descrição:** O `AssemblyIntegrityChecker` está implementado e activo, mas os ficheiros `.sha256`
que ele valida **nunca são gerados** pelo pipeline de CI actual. O checker só verifica assemblies
que têm ficheiro `.sha256` correspondente — sem esses ficheiros, passa silenciosamente sem validar
nada.

**Impacto:** O mecanismo de anti-tamper existe no código mas não tem efeito prático porque a etapa
de `build → obfuscate → AOT → sign` (referenciada no comentário do checker) ainda não está
implementada no pipeline.

**Estado:** LIMITAÇÃO ARQUITECTURAL — não é escopo desta fase corrigir, mas deve ser endereçado.

**O que seria necessário:**
- Um step de pipeline que gere os ficheiros `.sha256` para cada `NexTraceOne*.dll`
- Esses ficheiros precisam de ser incluídos na imagem Docker ou no artefacto publicado
- Sem isso, o checker funciona mas não valida nada em produção

---

### 2.2 `NEXTRACE_SKIP_INTEGRITY=true` em `docker-compose.override.yml`

**Descrição:** O ficheiro `docker-compose.override.yml` define `NEXTRACE_SKIP_INTEGRITY: "true"`
para todos os serviços de backend. Isto é aceitável para desenvolvimento local, mas deve ser
documentado explicitamente como **exclusivo do ambiente local**.

**Impacto:** Baixo — só afecta ambientes locais com `docker compose up --override`; produção não
usa o override.

**Estado:** ACEITE como comportamento de desenvolvimento, sem ação imediata necessária.

---

### 2.3 `NEXTRACE_SKIP_INTEGRITY` em fixtures de testes

**Descrição:** `ApiHostPostgreSqlFixture.cs` e `ApiE2EFixture.cs` definem
`Environment.SetEnvironmentVariable("NEXTRACE_SKIP_INTEGRITY", "true")` programaticamente.

**Impacto:** Nenhum em produção. Aceitável para testes de integração/E2E onde os assemblies
não têm `.sha256` gerados.

**Estado:** ACEITE como comportamento de teste.

---

### 2.4 Documentação de deployment com referências ao bypass

**Descrição:** Alguns ficheiros de documentação (`ENVIRONMENT-CONFIGURATION.md`,
`DOCKER-AND-COMPOSE.md`, `LOCAL-SETUP.md`) mencionam `NEXTRACE_SKIP_INTEGRITY=true` como
aceitável em CI. Com esta fase concluída, essa referência deve ser actualizada para deixar
claro que **CI/CD de segurança não deve usar o bypass**.

**Impacto:** Risco de confusão futura se alguém re-introduzir o bypass baseado na documentação
antiga.

**Estado:** PENDENTE — actualização de documentação recomendada mas não é bloqueante.

---

## 3. O que deve ser tratado em fases seguintes

| Item | Prioridade | Fase sugerida |
|---|---|---|
| Implementar geração de ficheiros `.sha256` no pipeline de CI | HIGH | P1.3 ou sprint dedicado |
| Actualizar documentação de deployment para remover referências a `NEXTRACE_SKIP_INTEGRITY=true` em CI | MEDIUM | P1.3 ou manutenção documental |
| Validar que imagens Docker incluem `.sha256` antes do scan Trivy | HIGH | Com implementação da assinatura |
| Adicionar verificação no workflow que confirme presença de `.sha256` (fail if missing) | MEDIUM | Após implementação da assinatura |

---

## 4. Classificação final da fase P1.2

| Dimensão | Estado |
|---|---|
| Bypass removido do pipeline de segurança | ✅ RESOLVIDO |
| Outros workflows sem bypass indevido | ✅ CONFIRMADO |
| Pipeline de segurança continua funcional | ✅ SIM |
| Bypass equivalente escondido | ✅ NÃO |
| Mecanismo de integridade activo e eficaz end-to-end | ⚠️ LIMITADO — checker existe mas `.sha256` não são gerados |
| Documentação gerada | ✅ SIM |

---

## 5. Classificação do risco residual

O risco mais relevante é a **ausência do pipeline de assinatura de assemblies**. Com ou sem o bypass
no `security.yml`, a verificação de integridade não teria detectado tamper porque não existem
ficheiros `.sha256`. A remoção do bypass é correcta e necessária para preservar a postura de
segurança do pipeline, mas o valor real do mecanismo só será atingido quando a geração de
`.sha256` for implementada e integrada no pipeline de build.

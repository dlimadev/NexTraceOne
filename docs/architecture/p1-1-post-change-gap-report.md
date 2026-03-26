# P1.1 — Post-Change Gap Report

**Data de execução:** 2026-03-26  
**Fase:** P1.1 — CORS Base Cleanup  
**Estado:** CONCLUÍDO COM GAPS CONTROLADOS

---

## 1. O que foi resolvido

| Item | Detalhe |
|---|---|
| Origens localhost removidas do config base | `appsettings.json` → `AllowedOrigins: []` |
| Origens localhost confinadas ao ficheiro de Development | `appsettings.Development.json` → `AllowedOrigins: [localhost:5173, localhost:3000]` |
| Fallback de Development corrigido para cobrir array vazio | `WebApplicationBuilderExtensions.cs` actualizado |
| Protecção de startup em não-Development activa | Lança `InvalidOperationException` se origens estão vazias fora de Development/CI |
| Wildcard nunca introduzido | Validação activa em startup |

---

## 2. O que ficou pendente (fora do escopo P1.1)

| Item | Classificação | Responsável |
|---|---|---|
| `RequireSecureCookies: false` em `appsettings.Development.json` | Aceite por ser config de Development | P1.2 ou revisão de configuração |
| `NEXTRACE_SKIP_INTEGRITY` ainda documentado como variável de bypass | Risco operacional identificado anteriormente | P1.2 ou E19 |
| Rate limits hardcoded em `Program.cs` (não externalizados) | Configuração operacional inflexível | Backlog de configuração |
| Autenticação JWT sem validação de chave secreta forte em produção | Risco de segurança identificado em E18 | P1.2 — JWT hardening |
| Headers de tenant/environment não validados por permissão explícita | Risco de escalada lateral | Backlog de segurança |

---

## 3. Riscos residuais

### Risco 1 — Ambientes sem `appsettings.Development.json`

**Descrição:** Se o processo for iniciado com `ASPNETCORE_ENVIRONMENT=Development` sem o ficheiro
`appsettings.Development.json` presente (por exemplo, em containers ou CI que não copiam o ficheiro),
o fallback de `WebApplicationBuilderExtensions` entra em acção e usa `localhost:5173` e `localhost:3000`.

**Impacto:** Baixo — só afecta ambientes de Development/CI; não afecta Production/Staging (que lançam exceção).

**Mitigação:** O fallback é explícito no código e limitado a ambientes de desenvolvimento. Documentado como comportamento intencional.

---

### Risco 2 — Origens de produção ainda não configuradas

**Descrição:** `appsettings.json` tem agora `AllowedOrigins: []`. Qualquer ambiente não-Development
(Staging, Production) que não configure `Cors:AllowedOrigins` via variável de ambiente ou overlay
específico irá falhar no startup.

**Impacto:** Startup bloqueado (comportamento intencional e seguro).

**Mitigação:** A mensagem de erro de startup é clara e orienta para configurar `Cors__AllowedOrigins__0`, etc.
A documentação de deployment deve mencionar esta obrigatoriedade.

---

### Risco 3 — Ausência de testes automatizados para validação de CORS por ambiente

**Descrição:** Não existem testes de integração que validem explicitamente que a inicialização falha
em Production com origens vazias ou que Development aceita as origens esperadas.

**Impacto:** Regressão silenciosa possível se a lógica de `AddCorsConfiguration` for alterada.

**Mitigação:** Recomendada a adição de testes unitários para `WebApplicationBuilderExtensions` num ciclo futuro.

---

## 4. O que deve ser tratado no P1.2

Com base nos gaps identificados acima e nos relatórios anteriores, o P1.2 deve endereçar:

1. **JWT Secret Hardening** — garantir que a chave JWT em produção é configurada via variável de ambiente e tem entropia mínima.
2. **NEXTRACE_SKIP_INTEGRITY** — rever se a variável de bypass de integridade está devidamente controlada e documentada.
3. **Deployment documentation** — documentar explicitamente que `Cors:AllowedOrigins` deve ser configurado via variável de ambiente em Staging/Production.
4. **Testes de configuração CORS** — adicionar testes unitários para `AddCorsConfiguration` que cobram os cenários por ambiente.

---

## 5. Classificação final da fase P1.1

| Dimensão | Estado |
|---|---|
| Segurança de configuração CORS | ✅ RESOLVIDO |
| Comportamento em Development | ✅ FUNCIONAL |
| Comportamento em Production/Staging | ✅ SEGURO (startup bloqueado sem config explícita) |
| Wildcard introduzido | ✅ NÃO |
| Scope creep para outros P1 | ✅ NÃO |
| Documentação gerada | ✅ SIM |

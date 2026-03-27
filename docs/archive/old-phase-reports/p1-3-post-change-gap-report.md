# P1.3 — Post-Change Gap Report

**Data de execução:** 2026-03-26  
**Fase:** P1.3 — Secure Cookies Startup Validation  
**Estado:** CONCLUÍDO COM LIMITAÇÕES RESIDUAIS DOCUMENTADAS

---

## 1. O que foi resolvido

| Item | Detalhe |
|---|---|
| Validação de startup para `RequireSecureCookies` adicionada | `ValidateSecureCookiesPolicy` em `StartupValidation.cs` |
| Startup aborta explicitamente se `RequireSecureCookies=false` fora de Development | `InvalidOperationException` com mensagem clara |
| Development local não afectado | `RequireSecureCookies=false` continua permitido em Development com `LogWarning` |
| `appsettings.json` base verificado como seguro por defeito | `RequireSecureCookies: true` confirmado |
| Teste pré-existente corrigido | `StartupValidation_ValidatesEncryptionKeyConfiguration` estava a falhar antes desta fase |
| 2 novos testes adicionados | Cobertura da nova validação e da configuração base |

---

## 2. Limitações residuais identificadas

### 2.1 Validação só é executada no startup

**Descrição:** A validação de `RequireSecureCookies` (como todas as outras validações de startup)
só corre uma vez durante a inicialização da aplicação. Se a configuração for alterada em tempo
de execução (e.g., via config reload dinâmico), a protecção não actua novamente.

**Impacto:** Baixo — a configuração é carregada de ficheiros JSON e variáveis de ambiente, não
tem reload dinâmico automático no padrão actual do produto.

**Estado:** ACEITE como limitação arquitectural comum a todas as validações de startup.

---

### 2.2 `CookieSession.Enabled=false` por defeito oculta o risco em produção

**Descrição:** A feature de cookie session está desabilitada por defeito (`Enabled=false`). Se
`RequireSecureCookies=false` estiver presente numa configuração de produção mas `Enabled=false`,
o comportamento inseguro nunca é executado — mas a configuração indevida persiste sem detecção.

**Impacto:** Moderado — a validação adicionada detecta e bloqueia independentemente do valor de
`Enabled`, o que é a postura correcta (falhar rápido na má configuração independentemente de a
feature estar activa).

**Estado:** RESOLVIDO — a validação actual bloqueia `RequireSecureCookies=false` em produção
mesmo que `Enabled=false`, o que é conservador e correcto.

---

### 2.3 Restante política de cookies não validada no startup

**Descrição:** Outros parâmetros de `CookieSessionOptions` (`AccessTokenCookieName`,
`CsrfCookieName`, etc.) não têm validação de startup equivalente. Valores inválidos ou
conflituantes só seriam detectados em tempo de execução.

**Impacto:** Baixo — os nomes de cookie têm valores razoáveis por defeito e não representam
risco de segurança directo.

**Estado:** PENDENTE — pode ser endereçado futuramente como parte de validação de configuração
mais abrangente.

---

## 3. O que deve ser tratado em fases seguintes

| Item | Prioridade | Fase sugerida |
|---|---|---|
| Validar outros parâmetros críticos de cookies em startup (SameSite, HttpOnly) | LOW | Manutenção futura |
| Rever demais itens P1 do roadmap (rate limits, CORS avançado, etc.) | HIGH | P1.4+ |
| Validar que `SameSite=Strict` é aplicado em produção | MEDIUM | P1.4 ou sprint de hardening |

---

## 4. Classificação final da fase P1.3

| Dimensão | Estado |
|---|---|
| Validação de startup para `RequireSecureCookies` implementada | ✅ RESOLVIDO |
| Startup falha explicitamente fora de Development com valor inseguro | ✅ CONFIRMADO |
| Development local funcional | ✅ SIM |
| Teste de regressão adicionado | ✅ SIM |
| Teste pré-existente corrigido | ✅ SIM |
| Documentação gerada | ✅ SIM |

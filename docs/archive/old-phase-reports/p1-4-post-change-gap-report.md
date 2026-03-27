# P1.4 — Post-Change Gap Report

**Data de execução:** 2026-03-26  
**Fase:** P1.4 — Rate Limit Configuration Externalization  
**Estado:** CONCLUÍDO COM LIMITAÇÕES RESIDUAIS DOCUMENTADAS

---

## 1. O que foi resolvido

| Item | Detalhe |
|---|---|
| Todos os valores hardcoded de rate limiting removidos de `Program.cs` | 19 valores numéricos substituídos por referências a opções de configuração |
| Nova classe `RateLimitingOptions` criada | Binding fortemente tipado para todas as 6 políticas |
| Secção `RateLimiting` adicionada a `appsettings.json` | Todos os valores baseline auditados explicitamente documentados |
| Valores baseline preservados sem alteração silenciosa | global:100, auth:20, auth-sensitive:10, ai:30, data-intensive:50, operations:40 |
| Override por ambiente viável sem recompilação | Via `appsettings.{Env}.json` ou variáveis de ambiente |

---

## 2. Limitações residuais

### 2.1 Sem validação de startup para valores inválidos de rate limit

**Descrição:** Valores como `PermitLimit=0` ou negativos não são rejeitados no startup.
O ASP.NET Core `FixedWindowRateLimiter` aceita estes valores e pode produzir comportamento
inesperado (PermitLimit=0 bloqueia todos os pedidos).

**Impacto:** Médio — requer configuração intencional de um valor inválido, que seria
facilmente detectado em testes funcionais básicos.

**Estado:** PENDENTE — pode ser resolvido adicionando validação na sequência do startup
(ex: `ValidateRateLimitingOptions` em `StartupValidation.cs`).

---

### 2.2 `WindowMinutes` fixo em 1 para todas as políticas (por defeito)

**Descrição:** A janela temporal foi parametrizada (campo `WindowMinutes`), mas todos os
defaults são `1 minuto`. Não foi adicionada validação que garanta valores positivos.

**Impacto:** Baixo — `WindowMinutes=0` causaria exceção em runtime do `TimeSpan.FromMinutes`,
não comportamento silencioso.

**Estado:** ACEITE — comportamento de falha explícita em runtime é preferível a falha silenciosa.

---

### 2.3 Políticas não são dinâmicas em runtime

**Descrição:** Os valores são lidos uma única vez no startup. Alterações à configuração
requerem restart da aplicação.

**Impacto:** Baixo — consistente com a arquitetura actual de configuração do produto.
Rate limiting dinâmico requereria Redis ou outro mecanismo distribuído (fora do escopo).

**Estado:** ACEITE como limitação arquitectural conhecida.

---

### 2.4 Sem `appsettings.Development.json` override para rate limiting

**Descrição:** `appsettings.Development.json` não tem override da secção `RateLimiting`,
pelo que Development usa os mesmos limites que Production.

**Impacto:** Desenvolvimento local pode ser afectado por rate limiting durante testes de
stress ou desenvolvimento activo de features que fazem muitas chamadas.

**Estado:** PENDENTE — pode ser resolvido opcionalmente adicionando valores mais permissivos
no `appsettings.Development.json` se identificada necessidade operacional.

---

## 3. O que deve ser tratado em fases seguintes

| Item | Prioridade | Fase sugerida |
|---|---|---|
| Adicionar validação de startup para `RateLimiting` (valores inválidos/zero) | LOW | Manutenção futura |
| Avaliar override de Development para testes locais de stress | LOW | Opcional, conforme necessidade |
| Avaliar rate limiting distribuído (Redis) para deploys multi-instância | LOW | Fase futura de hardening |
| Rever outros itens P1 do roadmap (rever CORS avançado, etc.) | HIGH | P1.5+ |

---

## 4. Classificação final da fase P1.4

| Dimensão | Estado |
|---|---|
| Valores hardcoded removidos de `Program.cs` | ✅ RESOLVIDO |
| Nova secção `RateLimiting` em `appsettings.json` | ✅ RESOLVIDO |
| Binding tipado via `RateLimitingOptions` | ✅ RESOLVIDO |
| Valores baseline preservados | ✅ CONFIRMADO |
| Override por ambiente sem recompilação | ✅ DISPONÍVEL |
| Build sem erros | ✅ CONFIRMADO |
| Testes sem regressão (68 passam) | ✅ CONFIRMADO |
| Documentação gerada | ✅ SIM |

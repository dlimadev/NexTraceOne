# P1.4 — Rate Limit Configuration Report

**Data de execução:** 2026-03-26  
**Classificação:** MEDIUM / P2 — Flexibilidade e parametrização de rate limiting  
**Estado:** CONCLUÍDO

---

## 1. Contexto

Os rate limits do NexTraceOne estavam implementados e funcionais, porém todos os valores
numéricos estavam hardcoded diretamente no `Program.cs`. Isto impedia ajuste por ambiente sem
recompilação e tornava a parametrização operacional impossível.

---

## 2. Ficheiros alterados

| Ficheiro | Tipo de alteração |
|---|---|
| `src/platform/NexTraceOne.ApiHost/RateLimitingOptions.cs` | **Criado** — classe de opções com todas as políticas |
| `src/platform/NexTraceOne.ApiHost/appsettings.json` | **Alterado** — nova secção `RateLimiting` com valores baseline |
| `src/platform/NexTraceOne.ApiHost/Program.cs` | **Alterado** — binding da configuração, remoção de todos os números hardcoded |

---

## 3. Valores hardcoded existentes (antes da alteração)

| Localização | Política | Valor hardcoded |
|---|---|---|
| `Program.cs:110` | global (IP resolvido) | `PermitLimit = 100` |
| `Program.cs:110` | global (IP não resolvido) | `PermitLimit = 20` (ternário inline) |
| `Program.cs:117` | global | `Window = TimeSpan.FromMinutes(1)` |
| `Program.cs:119` | global | `QueueLimit = 5` |
| `Program.cs:134` | auth | `PermitLimit = 20` |
| `Program.cs:135` | auth | `Window = TimeSpan.FromMinutes(1)` |
| `Program.cs:137` | auth | `QueueLimit = 2` |
| `Program.cs:152` | auth-sensitive | `PermitLimit = 10` |
| `Program.cs:153` | auth-sensitive | `Window = TimeSpan.FromMinutes(1)` |
| `Program.cs:155` | auth-sensitive | `QueueLimit = 2` |
| `Program.cs:170` | ai | `PermitLimit = 30` |
| `Program.cs:171` | ai | `Window = TimeSpan.FromMinutes(1)` |
| `Program.cs:173` | ai | `QueueLimit = 3` |
| `Program.cs:188` | data-intensive | `PermitLimit = 50` |
| `Program.cs:189` | data-intensive | `Window = TimeSpan.FromMinutes(1)` |
| `Program.cs:191` | data-intensive | `QueueLimit = 3` |
| `Program.cs:206` | operations | `PermitLimit = 40` |
| `Program.cs:207` | operations | `Window = TimeSpan.FromMinutes(1)` |
| `Program.cs:209` | operations | `QueueLimit = 3` |

---

## 4. Nova estrutura de configuração

### Classe `RateLimitingOptions`

Criada em `src/platform/NexTraceOne.ApiHost/RateLimitingOptions.cs` com:

- `SectionName = "RateLimiting"` — secção de configuração
- `GlobalPolicyOptions Global` — política global com `PermitLimit`, `UnresolvedIpPermitLimit`, `WindowMinutes`, `QueueLimit`
- `PolicyOptions Auth` — política de autenticação
- `PolicyOptions AuthSensitive` — política de operações sensíveis
- `PolicyOptions Ai` — política de IA
- `PolicyOptions DataIntensive` — política de dados intensivos
- `PolicyOptions Operations` — política operacional

Os defaults inline das propriedades preservam os valores baseline auditados, garantindo
comportamento correto mesmo se a secção estiver ausente da configuração.

### Secção em `appsettings.json`

```json
"RateLimiting": {
  "Global": {
    "PermitLimit": 100,
    "UnresolvedIpPermitLimit": 20,
    "WindowMinutes": 1,
    "QueueLimit": 5
  },
  "Auth": { "PermitLimit": 20, "WindowMinutes": 1, "QueueLimit": 2 },
  "AuthSensitive": { "PermitLimit": 10, "WindowMinutes": 1, "QueueLimit": 2 },
  "Ai": { "PermitLimit": 30, "WindowMinutes": 1, "QueueLimit": 3 },
  "DataIntensive": { "PermitLimit": 50, "WindowMinutes": 1, "QueueLimit": 3 },
  "Operations": { "PermitLimit": 40, "WindowMinutes": 1, "QueueLimit": 3 }
}
```

---

## 5. Binding no startup

`Program.cs` lê a configuração antes de registar o `AddRateLimiter`:

```csharp
var rateLimitingOptions = builder.Configuration
    .GetSection(RateLimitingOptions.SectionName)
    .Get<RateLimitingOptions>() ?? new RateLimitingOptions();
```

O `?? new RateLimitingOptions()` garante que, mesmo com a secção ausente, os defaults
seguros dos objetos de opções são usados (comportamento explícito, não bypass silencioso).

Cada política usa a instância de opções correspondente:
```csharp
var authOpts = rateLimitingOptions.Auth;
// ...
PermitLimit = authOpts.PermitLimit,
Window = TimeSpan.FromMinutes(authOpts.WindowMinutes),
QueueLimit = authOpts.QueueLimit
```

---

## 6. Políticas parametrizadas

| Política | PermitLimit | UnresolvedIpPermitLimit | WindowMinutes | QueueLimit |
|---|---|---|---|---|
| global | 100 | 20 | 1 | 5 |
| auth | 20 | — | 1 | 2 |
| auth-sensitive | 10 | — | 1 | 2 |
| ai | 30 | — | 1 | 3 |
| data-intensive | 50 | — | 1 | 3 |
| operations | 40 | — | 1 | 3 |

---

## 7. Valores baseline preservados

Todos os valores baseline identificados na auditoria foram preservados como valores por defeito
tanto nos defaults inline das classes quanto na secção `appsettings.json`:

- global: 100/min (IP resolvido), 20/min (IP não resolvido) ✅
- auth: 20/min ✅
- auth-sensitive: 10/min ✅
- ai: 30/min ✅
- data-intensive: 50/min ✅
- operations: 40/min ✅

Não foram feitas alterações silenciosas de política de proteção.

---

## 8. Override por ambiente

Os valores podem ser ajustados por ambiente sem recompilação:

**Via `appsettings.{Environment}.json`:**
```json
"RateLimiting": {
  "Auth": { "PermitLimit": 50 }
}
```

**Via variável de ambiente (suportado pelo ASP.NET Core config system):**
```
RateLimiting__Auth__PermitLimit=50
RateLimiting__Ai__PermitLimit=60
```

---

## 9. Validação funcional

- Build `Release` concluído sem erros (791 warnings pré-existentes, 0 erros)
- 68 testes de building-blocks passam após as alterações
- Nenhum número mágico de rate limit permanece hardcoded no `Program.cs`

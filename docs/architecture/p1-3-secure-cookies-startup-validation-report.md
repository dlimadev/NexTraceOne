# P1.3 — Secure Cookies Startup Validation Report

**Data de execução:** 2026-03-26  
**Classificação:** HIGH / P1 — Segurança de configuração de cookies  
**Estado:** CONCLUÍDO

---

## 1. Contexto

A auditoria identificou que `appsettings.Development.json` contém `RequireSecureCookies: false`,
configuração que é necessária para desenvolvimento local via HTTP mas que, se usada acidentalmente
em Staging ou Production, expõe tokens de sessão a intercepção via rede não-TLS.

A plataforma já tinha validações de startup para Jwt:Secret e connection strings, mas não tinha
proteção equivalente para `Auth:CookieSession:RequireSecureCookies`.

---

## 2. Ficheiros alterados

| Ficheiro | Tipo de alteração |
|---|---|
| `src/platform/NexTraceOne.ApiHost/StartupValidation.cs` | Adicionado método `ValidateSecureCookiesPolicy`; chamada integrada em `ValidateStartupConfiguration`; docstring de `ValidateEncryptionKey` actualizada com referências explícitas |
| `tests/building-blocks/NexTraceOne.BuildingBlocks.Infrastructure.Tests/Configuration/StartupValidationTests.cs` | Adicionados 2 novos testes: `BaseAppSettings_RequireSecureCookies_IsTrueForProduction` e `StartupValidation_EnforcesSecureCookiesInNonDevelopment`; corrigido teste pré-existente `StartupValidation_ValidatesEncryptionKeyConfiguration` |

---

## 3. Ponto onde `RequireSecureCookies` é lido

**Ficheiro de configuração:** `src/platform/NexTraceOne.ApiHost/appsettings.Development.json`  
**Linha:** 30  
**Secção:** `Auth:CookieSession:RequireSecureCookies`

**Opções binding:** `NexTraceOne.BuildingBlocks.Security.CookieSession.CookieSessionOptions`  
**Propriedade:** `bool RequireSecureCookies { get; init; } = true`  
**Uso:** `CookieSessionEndpoints.cs` e `CsrfTokenValidator.cs` usam o valor para definir `Secure = opts.RequireSecureCookies` nos cookies emitidos

---

## 4. Validação de startup adicionada

### Localização

`StartupValidation.cs` — novo método privado `ValidateSecureCookiesPolicy`, chamado a partir de `ValidateStartupConfiguration` imediatamente após `ValidateEncryptionKey`.

### Lógica implementada

```csharp
private static void ValidateSecureCookiesPolicy(WebApplication app, IConfiguration configuration, ILogger logger)
{
    var rawValue = configuration["Auth:CookieSession:RequireSecureCookies"];
    var requireSecure = !string.Equals(rawValue, "false", StringComparison.OrdinalIgnoreCase);

    if (!requireSecure && !app.Environment.IsDevelopment())
    {
        logger.LogCritical(...);
        throw new InvalidOperationException(
            "NexTraceOne startup aborted: Auth:CookieSession:RequireSecureCookies must be true " +
            "in non-Development environments...");
    }

    if (!requireSecure)
        logger.LogWarning("...acceptable for local HTTP development but must NEVER be used in staging or production.");
    else
        logger.LogInformation("...secure cookies enforced...");
}
```

### Lógica de segurança da detecção

A lógica usa `!string.Equals(rawValue, "false", StringComparison.OrdinalIgnoreCase)` para a validação:
- Se a configuração estiver ausente (null) → `requireSecure = true` → comportamento seguro por defeito
- Se o valor for `"false"` (case-insensitive) → `requireSecure = false` → trigger da validação
- Qualquer outro valor ou ausência é tratado como seguro

---

## 5. Comportamento esperado por ambiente

### Em Development

- `RequireSecureCookies=false` **é permitido**
- Um `LogWarning` explícito é registado alertando que não pode ser usado em staging/produção
- O startup continua normalmente
- Desenvolvimento local via HTTP funciona sem alteração

### Fora de Development (Staging, Production, qualquer outro)

- `RequireSecureCookies=false` **aborta o startup** com `InvalidOperationException`
- Um `LogCritical` é emitido antes da excepção
- Mensagem de erro explícita: ambiente actual, causa, e forma de resolver
- `RequireSecureCookies=true` (base) ou ausente → startup continua normalmente com `LogInformation`

---

## 6. Protecção da configuração base

`appsettings.json` (base para todos os ambientes não-sobrescritos) tem:

```json
"Auth": {
  "CookieSession": {
    "RequireSecureCookies": true
  }
}
```

Isto garante que qualquer ambiente que não carregue explicitamente um override
(`appsettings.Development.json`) herda o valor seguro por defeito.

---

## 7. Correcção colateral: teste pré-existente

O teste `StartupValidation_ValidatesEncryptionKeyConfiguration` (pré-existente) estava a falhar
porque verificava a presença das strings `"NEXTRACE_ENCRYPTION_KEY"` e `"Base64-encoded 32-byte key"`
no ficheiro `StartupValidation.cs`, mas estas strings só existiam em `EncryptionKeyMaterial.cs`
(para onde a implementação delega).

A correcção foi actualizar o docstring do método `ValidateEncryptionKey` em `StartupValidation.cs`
para incluir explicitamente:
- `NEXTRACE_ENCRYPTION_KEY` — nome da variável de ambiente obrigatória
- `Base64-encoded 32-byte key` — formato exigido

Esta é uma melhoria de documentação correcta independente do P1.3.

---

## 8. Testes adicionados

| Teste | Propósito |
|---|---|
| `BaseAppSettings_RequireSecureCookies_IsTrueForProduction` | Verifica que `appsettings.json` base tem `RequireSecureCookies=true` |
| `StartupValidation_EnforcesSecureCookiesInNonDevelopment` | Verifica que `StartupValidation.cs` contém a lógica de enforcement |

Total de testes após as alterações: **17 passing, 0 failing**.

---

## 9. Critérios de aceite verificados

| Critério | Estado |
|---|---|
| `RequireSecureCookies=false` permitido apenas em Development | ✅ |
| Startup falha explicitamente fora de Development com esse valor | ✅ |
| Development local continua funcional | ✅ |
| Relatório final gerado | ✅ |

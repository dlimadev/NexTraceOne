# P0.2 — JWT Secret Hardening Report

**Data de execução:** 2026-03-25  
**Fase:** P0.2 — Remoção do Jwt:Secret vazio e eliminação do fallback JWT hardcoded  
**Estado:** CONCLUÍDO

---

## 1. Contexto

A auditoria de segurança identificou dois problemas CRITICAL ligados ao JWT:

1. `appsettings.json` continha `"Secret": ""` — campo vazio que sinaliza ausência de configuração, mas que o código resolvia silenciosamente via fallback hardcoded.
2. Três ficheiros de código continham o fallback `"development-signing-key-development-signing-key-1234567890"` que era usado em ambientes Development quando a chave não estava configurada — expondo uma chave previsível e pública no repositório.
3. `appsettings.Development.json` continha uma chave real commitada: `"NexTraceOne-Development-SecretKey-AtLeast32BytesLong-2024!"`.

Esta fase resolve todos estes problemas tornando o `Jwt:Secret` obrigatório em **todos os ambientes**.

---

## 2. Ficheiros alterados

### 2.1 `src/platform/NexTraceOne.ApiHost/appsettings.json`

**Problema:** `"Secret": ""` — campo vazio presente no ficheiro base commitado.

**Alteração:** Removida a linha `"Secret": ""` da secção `Jwt`.

A secção `Jwt` passa a ter apenas:
```json
"Jwt": {
  "Issuer": "NexTraceOne",
  "Audience": "nextraceone-api",
  "AccessTokenExpirationMinutes": 60,
  "RefreshTokenExpirationDays": 7
}
```

`Jwt:Secret` deve ser fornecido exclusivamente via variável de ambiente `Jwt__Secret`, `dotnet user-secrets` ou gestor de segredos.

---

### 2.2 `src/platform/NexTraceOne.ApiHost/appsettings.Development.json`

**Problema:** `"Secret": "NexTraceOne-Development-SecretKey-AtLeast32BytesLong-2024!"` — chave real commitada no repositório.

**Alteração:** Removida integralmente a secção `Jwt` do ficheiro Development (era a única chave nessa secção). O programador deve configurar o segredo localmente.

---

### 2.3 `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Authentication/JwtTokenService.cs`

**Problema:** `ResolveSigningKey()` retornava `"development-signing-key-development-signing-key-1234567890"` quando o ambiente era Development e a chave não estava configurada.

**Alteração:** Removida a lógica de fallback. O método lança `InvalidOperationException` em **todos os ambientes** se a chave estiver ausente ou vazia:

```csharp
throw new InvalidOperationException(
    "JWT signing key is not configured. Set 'Jwt:Secret' via environment variable (Jwt__Secret), " +
    "dotnet user-secrets, or a secrets manager. " +
    "A signing key is mandatory in all environments. " +
    "Generate a strong key with: openssl rand -base64 48");
```

---

### 2.4 `src/building-blocks/NexTraceOne.BuildingBlocks.Security/DependencyInjection.cs`

**Problema:** `AddBuildingBlocksSecurity()` usava o fallback `"development-signing-key-..."` em Development.

**Alteração:** Removida a ramificação condicional por ambiente. A chave é agora obrigatória em todos os ambientes — lança `InvalidOperationException` imediatamente se ausente.

---

### 2.5 `src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/Services/JwtTokenGenerator.cs`

**Problema:** `_signingKey` tinha fallback incondicional `?? "development-signing-key-development-signing-key-1234567890"` — sem verificação de ambiente.

**Alteração:** Substituído pelo operador `??` com `throw`:
```csharp
private readonly string _signingKey = configuration["Jwt:Secret"]
    ?? configuration["Security:Jwt:SigningKey"]
    ?? throw new InvalidOperationException(
        "JWT signing key is not configured. ...");
```

---

### 2.6 `src/platform/NexTraceOne.ApiHost/StartupValidation.cs`

**Problema:** `ValidateJwtSecret()` apenas avisava em Development e só falhava em Staging/Production.

**Alteração:** Removida a ramificação `IsDevelopment()` para JWT. A validação falha em **todos os ambientes** se a chave estiver ausente, vazia ou tiver menos de 32 caracteres.

Mensagem de erro:
```
NexTraceOne startup aborted: Jwt:Secret must be configured in all environments.
Set the 'Jwt__Secret' environment variable, use dotnet user-secrets, or provision it via a secrets manager.
Minimum 32 characters required for HS256 key material.
Generate a strong key with: openssl rand -base64 48
```

---

## 3. Testes actualizados

### 3.1 `JwtTokenServiceTests.cs`

- Removido: `Constructor_InDevelopment_WithNoKey_UsesFallbackKey` (testava o comportamento que foi eliminado)
- Adicionado: `Constructor_WithNoKey_Throws` — verifica que a ausência de chave lança `InvalidOperationException` em qualquer ambiente

### 3.2 `SecurityDependencyInjectionTests.cs`

- Removido: `AddBuildingBlocksSecurity_InDevelopment_WithNoKey_UsesFallback` (testava o fallback eliminado)
- Adicionado: `AddBuildingBlocksSecurity_WithNoKey_AlwaysThrows` — verifica que a ausência de chave lança sempre excepção

### 3.3 `StartupValidationTests.cs`

- Actualizado: `DevAppSettings_JwtSecret_IsSetForDevelopment` → `DevAppSettings_JwtSecret_IsAbsentOrPlaceholder`
  — agora verifica que o Development config **não** contém uma chave real commitada

### 3.4 `AppSettingsSecurityTests.cs`

- Actualizado: `BaseAppSettings_JwtSecret_ShouldBeEmpty` → `BaseAppSettings_JwtSecret_ShouldBeAbsent`
  — verifica que a propriedade `"Secret"` não existe no ficheiro base (em vez de verificar que está vazia)
- Actualizado: `DevAppSettings_JwtSecret_ShouldBeExplicitlyDevelopmentOnly` → `DevAppSettings_JwtSecret_ShouldNotBeHardcoded`
  — verifica que o dev config não tem uma chave real

---

## 4. Estratégia de configuração para desenvolvimento local

Para arrancar a aplicação localmente após esta fase, o programador deve configurar o JWT secret explicitamente:

**Opção A — dotnet user-secrets (recomendado):**
```bash
dotnet user-secrets set "Jwt:Secret" "$(openssl rand -base64 48)" \
  --project src/platform/NexTraceOne.ApiHost
```

**Opção B — variável de ambiente:**
```bash
export Jwt__Secret=$(openssl rand -base64 48)
dotnet run --project src/platform/NexTraceOne.ApiHost
```

**Opção C — ficheiro `.env` (se o bootstrap carregar .env):**
```
JWT_SECRET=<chave gerada com openssl rand -base64 48>
```

---

## 5. Alinhamento com `.env.example`

O ficheiro `.env.example` já contém:
```
JWT_SECRET=REPLACE-WITH-AT-LEAST-32-CHAR-SECRET-KEY
```

Nenhuma alteração foi necessária no `.env.example`.

---

## 6. Validação funcional

- `grep -rn "development-signing-key" src/ tests/` → **CLEAN** — zero ocorrências
- `grep -n '"Secret":' appsettings.json appsettings.Development.json` → **CLEAN** — nenhuma chave presente
- `dotnet test` nos projetos afectados: **100/100 Security, 65/65 Infrastructure, 290/290 Identity**

---

## 7. Sumário de ficheiros alterados

| Ficheiro | Tipo de alteração |
|---|---|
| `appsettings.json` | Removida linha `"Secret": ""` da secção Jwt |
| `appsettings.Development.json` | Removida secção `Jwt` completa (tinha `"Secret": "..."`) |
| `JwtTokenService.cs` | Removido fallback Development; sempre lança se chave ausente |
| `DependencyInjection.cs` (Security) | Removido fallback Development; sempre lança se chave ausente |
| `JwtTokenGenerator.cs` (IdentityAccess) | Removido `?? "development-signing-key-..."` |
| `StartupValidation.cs` | `ValidateJwtSecret` falha em todos os ambientes |
| `JwtTokenServiceTests.cs` | Teste de fallback substituído por teste de throw |
| `SecurityDependencyInjectionTests.cs` | Teste de fallback substituído por teste de throw |
| `StartupValidationTests.cs` | Teste de dev secret actualizado |
| `AppSettingsSecurityTests.cs` | 2 testes actualizados para nova realidade |

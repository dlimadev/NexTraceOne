# NexTraceOne — Relatório de Auditoria: Segurança
**Data:** 2026-04-10  
**Escopo:** Building blocks de segurança, módulo identityaccess, API host, frontend auth  
**Ficheiros analisados:** Security building block, IdentityAccess module, WebApplicationExtensions

---

## Contexto Positivo

O NexTraceOne demonstra uma postura de segurança madura em várias áreas:

- JWT validation robusto: HMAC-SHA256, audience/issuer validation, clock skew configurável
- CSRF protection correcta: double-submit cookie, SameSite=Strict, constant-time comparison
- Password hashing: PBKDF2-SHA256, 100.000 iterações, salt aleatório de 16 bytes
- Tenant isolation via JWT claims (JWT > header > subdomain) com validação no backend
- Rate limiting por categoria: auth (20/min), auth-sensitive (10/min), global (100/min)
- Security headers: X-Content-Type-Options, X-Frame-Options, Referrer-Policy, HSTS
- Security Event audit trail com risk scores e contexto completo
- Authorization em cascata: JWT claims → DB permissions → Module policies → JIT grants → Explicit deny

---

## CRÍTICO

### [C-02] Chave JWT de Fallback Hardcoded no Código
**Ficheiro:** `src/building-blocks/NexTraceOne.BuildingBlocks.Security/DependencyInjection.cs` (linhas 64–77)  

**Problema:**  
```csharp
const string devFallbackKey = "NexTraceOne-Dev-Only-FallbackKey-NOT-FOR-NonDev-Or-Production!!";
```

Esta constante está visível no código-fonte, compilada nos binários, e acessível via decompilação ou strings. Embora o código throw em ambientes não-dev se a chave real não estiver configurada, a constante expõe um vector de ataque:

1. Se o código dev for acidentalmente deployado em produção sem configuração de chave real, o fallback activa
2. A chave pode ser usada para forjar tokens JWT em qualquer ambiente de desenvolvimento com acesso à constante

**Impacto:** Se executado em produção sem configuração correcta, todos os tokens JWT são forjáveis.

**Correcção:**
```csharp
// REMOVER a constante hardcoded:
// const string devFallbackKey = "...";

// SUBSTITUIR por falha clara em todos os ambientes sem chave configurada:
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        $"JWT signing key is not configured. Set 'Security:Jwt:SigningKey' in configuration. " +
        $"Environment: {environment.EnvironmentName}");
}
```

Se for absolutamente necessário suporte dev sem configuração, gerar uma chave aleatória em runtime (não persistida):
```csharp
if (isDevelopment && string.IsNullOrWhiteSpace(jwtKey))
{
    // Gerar chave efémera — não persistida entre restarts
    var ephemeralKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    logger.LogWarning("Using ephemeral JWT key for development. Tokens will be invalidated on restart.");
    jwtKey = ephemeralKey;
}
```

---

### [C-03] API Keys Armazenadas em Plain Text em Memória
**Ficheiros:**
- `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Authentication/ApiKeyAuthenticationHandler.cs` (linhas 23–24)
- `src/building-blocks/NexTraceOne.BuildingBlocks.Security/DependencyInjection.cs` (linhas 163–164)

**Problema:**  
API keys são carregadas de `appsettings.json` e armazenadas em memória sem encriptação:
```csharp
var apiKeysSection = configuration.GetSection("Security:ApiKeys");
options.ConfiguredKeys = apiKeysSection.Get<List<ApiKeyConfiguration>>() ?? [];
```

Problemas:
1. API keys em ficheiros de configuração podem ser expostas em logs, dumps de memória ou repositórios git
2. Não há mecanismo de rotação sem redeploy
3. Não há auditoria de uso por API key
4. Não há hash — a chave raw é comparada directamente

O comentário no código refere "MVP1 limitation" — mas sem tracking de quando será resolvido.

**Correcção:**

**Fase 1 (imediata):** Passar a armazenar hash das chaves:
```csharp
// Armazenar apenas hash SHA-256 da chave no config
// Comparar HMAC do input com hash armazenado
public bool ValidateKey(string providedKey, string storedHash)
{
    var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(providedKey)));
    return CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(hash),
        Encoding.UTF8.GetBytes(storedHash));
}
```

**Fase 2 (sprint seguinte):** Migrar para DB com encriptação:
1. Criar entidade `ApiKey` no `IdentityDbContext`
2. Armazenar `KeyHash`, `CreatedAt`, `ExpiresAt`, `LastUsedAt`, `CreatedBy`
3. Criar endpoint admin para geração e revogação de chaves
4. Emitir `SecurityEvent` em cada uso de API key

---

## ALTO

### [A-02] Break Glass sem Workflow de Aprovação
**Ficheiro:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.API/Endpoints/Endpoints/BreakGlassEndpoints.cs` (linhas 29–40)  

**Problema:**  
Qualquer utilizador autenticado pode solicitar e (aparentemente) obter acesso de break glass sem aprovação prévia. O endpoint apenas requer `RequireAuthorization()` sem validação adicional.

**Impacto:** Break glass é um mecanismo de acesso de emergência que, por definição, bypassa controlos normais. Sem aprovação, qualquer utilizador comprometido pode escalar privilégios.

**Correcção:**
1. Adicionar estado `Pending` às solicitações de break glass
2. Implementar notificação imediata à equipa de segurança/ops quando solicitado
3. Requerer aprovação de segundo utilizador com permissão `security:break-glass:approve`
4. Adicionar expiração automática curta (2-4 horas máximo)
5. Criar audit log especial com categoria `BreakGlassSession` para todas as acções durante o acesso
6. Considerar MFA adicional antes de confirmar a solicitação

```csharp
// Exemplo de fluxo correcto:
// 1. POST /break-glass -> cria BG request com status Pending
// 2. Notifica security team via notification service
// 3. POST /break-glass/{id}/approve (requer permissão especial)
// 4. Só após aprovação o acesso é concedido com expiração
```

---

### [A-03] Operações em Ambiente de Produção sem Autorização Adicional
**Ficheiro:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.API/Endpoints/Endpoints/EnvironmentEndpoints.cs` (linhas 86–95)  

**Problema:**  
Definir um ambiente como "primary-production" requer apenas `env:environments:admin` — a mesma permissão usada para operações em ambientes não-produtivos. Não há:
- Distinção de permissão entre produção e não-produção
- Aprovação adicional para operações críticas em produção
- Autenticação step-up (re-verificar identidade)

**Correcção:**
```csharp
// Criar política específica para produção:
.RequirePermission("env:environments:production:admin")

// Ou implementar EnvironmentAccessRequirement:
.RequireAuthorization(policy => policy
    .AddRequirements(new ProductionEnvironmentRequirement()));
```

Criar `ProductionEnvironmentRequirement` que valida:
- Utilizador tem permissão explícita em produção
- Operação está dentro de janela de mudança aprovada (se aplicável)
- Registo de auditoria com categoria `ProductionCritical`

---

## MÉDIO

### [M-12] Delegação de Permissões Acessível a Qualquer Utilizador Autenticado
**Ficheiro:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.API/Endpoints/Endpoints/DelegationEndpoints.cs` (linhas 29–39)  

**Problema:**  
`RequireAuthorization()` sem permissão específica. Qualquer utilizador autenticado pode criar delegações (embora com a limitação de não poder delegar mais do que possui).

**Correcção:**
```csharp
.RequirePermission("identity:delegations:manage")
```

---

### [M-13] Sem Rate Limiting Específico para Falhas de API Key
**Ficheiro:** `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Authentication/ApiKeyAuthenticationHandler.cs` (linhas 53–56)  

**Problema:**  
Tentativas inválidas de API key são logadas mas não têm rate limiting específico por IP para este tipo de falha. O rate limiting global (100/min) pode não ser suficiente para prevenir brute force distribuído.

**Correcção:**
```csharp
// Incrementar contador de falhas por IP
// Bloquear temporariamente após N falhas consecutivas
// Emitir SecurityEvent com severidade crescente
```

---

### [M-14] Validação de State OIDC não Verificável no Código
**Ficheiro:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.API/Endpoints/Endpoints/AuthEndpoints.cs` (linhas 123–140)  

**Problema:**  
O parâmetro `state` no callback OIDC é aceite do query string mas não há evidência clara de validação contra o valor armazenado na sessão/cookie. O state OIDC é o mecanismo de protecção contra CSRF no fluxo OAuth.

**Acção:** Verificar se `OidcCallbackFeature` valida o state token contra o valor gerado no `initiate`. Se não, implementar:
```csharp
// No handler:
var storedState = await stateStore.GetAndRemoveAsync(command.State);
if (storedState is null || storedState != command.State)
    return Result.Failure("oidc.state.invalid");
```

---

## BAIXO

### [L-06] Validação de Chave de Encriptação não Verificável Inline
**Ficheiro:** `src/building-blocks/NexTraceOne.BuildingBlocks.Security/DependencyInjection.cs` (linha 89)  

`EncryptionKeyMaterial.ValidateRequiredEnvironmentVariable(isDevelopment)` — verificar que lança excepção clara se a chave não estiver configurada em ambientes não-dev.

---

### [L-07] Permissão de Leitura de Utilizadores muito Abrangente
**Ficheiro:** `src/modules/identityaccess/.../UserEndpoints.cs` (linhas 43–51)  

`identity:users:read` permite ler qualquer perfil de utilizador. Verificar que o handler enforce isolamento de tenant e não permite leitura cross-tenant.

---

### [L-08] X-XSS-Protection Desactivado
**Ficheiro:** `src/platform/NexTraceOne.ApiHost/WebApplicationExtensions.cs` (linha 224)  

```csharp
headers["X-XSS-Protection"] = "0";
```

Esta é a abordagem correcta em browsers modernos (que usam CSP em vez de X-XSS-Protection), mas deve estar documentada. Garantir que CSP está configurado no frontend para a SPA.

---

## Tabela de Remediação

| ID | Severidade | Issue | Esforço |
|----|-----------|-------|---------|
| C-02 | Crítico | Remover constante JWT hardcoded | 1h |
| C-03 | Crítico | Hash de API keys + migração para DB | 2 sprints |
| A-02 | Alto | Break Glass approval workflow | 1 sprint |
| A-03 | Alto | Autorização específica para produção | 3 dias |
| M-12 | Médio | Permissão para criar delegações | 2h |
| M-13 | Médio | Rate limiting por API key | 4h |
| M-14 | Médio | Verificar validação de state OIDC | 1 dia |
| L-06 | Baixo | Verificar validação de chave de encriptação | 1h |
| L-07 | Baixo | Verificar isolamento de tenant em users:read | 2h |
| L-08 | Baixo | Documentar decisão X-XSS-Protection | 30min |

# Segurança Transversal — Cenários de Teste Funcionais

> Cobertura: Multi-tenancy, JWT, RLS, Break Glass, Capabilities, Planos, MFA, Sessões, Campos Criptografados, Soft Delete

---

## Multi-Tenancy e Row-Level Security

### TC-SEC-001 — RLS bloqueia leitura cross-tenant

| Campo | Valor |
|-------|-------|
| **Módulo** | Transversal |
| **Feature** | TenantRlsInterceptor |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |

**Pré-condições:**
- Tenant A com 10 contratos; Tenant B com 20 contratos.
- `TenantRlsInterceptor` ativo em ambos os contextos.

**Passos:**
1. Autenticar como Tenant A.
2. Executar qualquer query que acesse a tabela `cat_contracts`.
3. `TenantRlsInterceptor.ReaderExecutedAsync` chama `SELECT set_config('app.current_tenant_id', '<tenantA>', false)`.

**Resultado Esperado:**
- PostgreSQL aplica política RLS; apenas registros de Tenant A retornados.
- Tentativa de manipular `tenantId` no payload não sobrescreve o valor do JWT.

**Critério de Aceite:** `result.Value.Count == 10`; zero registros de Tenant B.

---

### TC-SEC-002 — Filtro de repositório como segunda camada

| Campo | Valor |
|-------|-------|
| **Módulo** | Transversal |
| **Feature** | Repository TenantId filter |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |

**Pré-condições:** Repositório com `.Where(e => e.TenantId == currentTenant.Id)`.

**Passos:**
1. Desabilitar RLS artificialmente (teste de defense-in-depth).
2. Executar consulta via repositório.

**Resultado Esperado:**
- Filtro de repositório ainda exclui registros de outros tenants.

**Critério de Aceite:** Dados de outros tenants jamais retornados, mesmo sem RLS.

---

### TC-SEC-003 — Background job bypassa tenant (legítimo)

| Campo | Valor |
|-------|-------|
| **Módulo** | BackgroundWorkers |
| **Feature** | LicenseRecalculationJob |
| **Tipo** | Segurança |
| **Prioridade** | Alta |

**Pré-condições:** Job executa sem contexto de tenant (admin cross-tenant).

**Passos:**
1. `LicenseRecalculationJob` executa query sem `TenantId` filter intencional.
2. Acessa todos os tenants para calcular host units.

**Resultado Esperado:**
- Job legítimo consegue acesso cross-tenant.
- Resultado isolado por tenant no output (não mistura dados).

**Critério de Aceite:** Cada tenant recebe seu cálculo independente.

---

## JWT e Autenticação

### TC-SEC-004 — Token JWT com assinatura inválida

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | JWT Validation |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |

**Passos:**
1. Enviar request com JWT assinado com chave diferente da configurada.

**Resultado Esperado:**
- HTTP 401; log de segurança registrado.

**Critério de Aceite:** Nenhum dado retornado; `SecurityEvent` de token inválido gerado.

---

### TC-SEC-005 — Token JWT expirado

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | JWT Validation |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |

**Passos:**
1. Usar token com `exp` no passado.

**Resultado Esperado:**
- HTTP 401; erro `token_expired`.

**Critério de Aceite:** Sem acesso a dados; usuário deve refazer login ou usar refresh token.

---

### TC-SEC-006 — Refresh token válido emite novo JWT

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | RefreshToken |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** Refresh token válido (não expirado, não revogado).

**Passos:**
1. `RefreshToken.Command(refreshToken)`.
2. Handler valida token e emite novo par (access + refresh).

**Resultado Esperado:**
- Novos tokens emitidos; refresh token anterior invalidado (rotação).

**Critério de Aceite:** HTTP 200; tokens novos funcionam; token antigo retorna 401.

---

### TC-SEC-007 — Refresh token revogado (logout forçado)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | RevokeSession |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |

**Passos:**
1. Admin revoga sessão do usuário via `RevokeSession`.
2. Usuário tenta usar refresh token.

**Resultado Esperado:**
- HTTP 401; sessão não renovável.

**Critério de Aceite:** `ListActiveSessions` não mostra sessão revogada.

---

### TC-SEC-008 — Manipulação de claims no payload JWT

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | JWT Validation |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |

**Passos:**
1. Decodificar JWT válido, alterar `tenantId` para outro tenant, recodificar (sem re-assinar).

**Resultado Esperado:**
- Assinatura inválida → HTTP 401.

**Critério de Aceite:** Impossível trocar de tenant via manipulação de token.

---

## Capabilities e Planos

### TC-SEC-009 — Bloquear feature por capability ausente

| Campo | Valor |
|-------|-------|
| **Módulo** | Transversal |
| **Feature** | HasCapability check |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |

**Pré-condições:** Tenant no plano `Starter` sem capability `contract_studio`.

**Passos:**
1. Chamar qualquer endpoint que verifica `HasCapability("contract_studio")`.

**Resultado Esperado:**
- `Error.Forbidden("CapabilityRequired", "This feature requires the Contract Studio plan.")`.
- HTTP 403.

**Critério de Aceite:** Funcionalidade bloqueada sem exposição de dados.

---

### TC-SEC-010 — Plano Enterprise habilita todas as capabilities

| Campo | Valor |
|-------|-------|
| **Módulo** | Transversal |
| **Feature** | TenantCapabilities.ForPlan |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** Tenant com plano `Enterprise`.

**Passos:**
1. Verificar `HasCapability("contract_studio")`, `HasCapability("ai_governance")`, `HasCapability("chaos_engineering")`.

**Resultado Esperado:**
- Todos retornam `true`.

**Critério de Aceite:** Acesso liberado a todas as features.

---

### TC-SEC-011 — Downgrade de plano bloqueia features ativas

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | ProvisionTenantLicense |
| **Tipo** | Segurança |
| **Prioridade** | Alta |

**Passos:**
1. Tenant está em `Professional`; contrato expirado → downgrade para `Starter`.
2. `ProvisionTenantLicense` atualiza capabilities no JWT.
3. Usuário tenta acessar feature de Professional.

**Resultado Esperado:**
- Feature bloqueada após novo login (claims atualizados).

**Critério de Aceite:** HTTP 403; log de downgrade registrado.

---

### TC-SEC-012 — Plano Trial com teasers Enterprise bloqueados

| Campo | Valor |
|-------|-------|
| **Módulo** | Transversal |
| **Feature** | TenantCapabilities.ForPlan(Trial) |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Pré-condições:** Tenant em plano `Trial`.

**Passos:**
1. Verificar `HasCapability("multi_region")` e `HasCapability("air_gapped")`.

**Resultado Esperado:**
- `multi_region = false`; `air_gapped = false`.
- Professional + 4 teasers Enterprise habilitados; multi_region/air_gapped bloqueados.

**Critério de Aceite:** Comportamento conforme `TenantCapabilities.ForPlan(Trial)`.

---

## Break Glass

### TC-SEC-013 — Fluxo completo de Break Glass

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | RequestBreakGlass → uso → RevokeBreakGlass |
| **Tipo** | E2E |
| **Prioridade** | Crítica |

**Passos:**
1. `RequestBreakGlass.Command(reason: "Incidente crítico P0 — acesso emergencial")`.
2. Aprovação automática (ou por segundo admin).
3. Usuário usa acesso elevado.
4. `RevokeBreakGlass.Command(breakGlassId)`.
5. Verificar `ListBreakGlassRequests`.

**Resultado Esperado:**
- Acesso elevado concedido durante período; auto-revogado após expiração.
- `SecurityEvent` registrado para cada ação.

**Critério de Aceite:** Trilha auditável completa; sem permissões residuais após revogação.

---

### TC-SEC-014 — Break Glass sem justificativa

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | RequestBreakGlass |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Passos:**
1. `RequestBreakGlass.Command(reason: "")`.

**Resultado Esperado:**
- `ErrorType = Validation`; justificativa obrigatória.

**Critério de Aceite:** HTTP 422.

---

### TC-SEC-015 — Break Glass audita todos os acessos

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | Break Glass audit trail |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |

**Pré-condições:** Usuário com Break Glass ativo executa 5 operações.

**Passos:**
1. Executar 5 ações diferentes com token de Break Glass.
2. Revogar Break Glass.
3. Consultar `ListSecurityEvents`.

**Resultado Esperado:**
- 5 eventos de segurança com `IsBreakGlass = true` registrados.

**Critério de Aceite:** Trilha completa; cada ação rastreável com `breakGlassId`.

---

## MFA

### TC-SEC-016 — Login com MFA obrigatório

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | VerifyMfaChallenge |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |

**Pré-condições:** Tenant com `MfaRequired = true`; usuário com TOTP configurado.

**Passos:**
1. `LocalLogin` com credenciais corretas → retorna `RequiresMfa = true` e `mfaChallengeToken`.
2. `VerifyMfaChallenge.Command(mfaChallengeToken, code: "123456")`.

**Resultado Esperado:**
- JWT completo emitido apenas após MFA bem-sucedido.

**Critério de Aceite:** Token sem MFA não concede acesso a endpoints protegidos.

---

### TC-SEC-017 — Código MFA inválido

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | VerifyMfaChallenge |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |

**Passos:**
1. `VerifyMfaChallenge.Command(mfaChallengeToken, code: "000000")` (código errado).

**Resultado Esperado:**
- `ErrorType = Unauthorized`.
- Contador de tentativas incrementado; bloqueio após 5 falhas.

**Critério de Aceite:** HTTP 401; conta bloqueada temporariamente.

---

### TC-SEC-018 — Reenvio de código MFA

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | ResendMfaCode |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Passos:**
1. `ResendMfaCode.Command(mfaChallengeToken)`.

**Resultado Esperado:**
- Novo código enviado ao canal configurado (email/SMS).
- Token anterior invalidado.

**Critério de Aceite:** HTTP 200; log de reenvio registrado.

---

## JIT Access

### TC-SEC-019 — Fluxo JIT: solicitar → aprovar → usar → expirar

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | RequestJitAccess → DecideJitAccess |
| **Tipo** | E2E |
| **Prioridade** | Crítica |

**Passos:**
1. Usuário solicita acesso JIT: `RequestJitAccess(resource: "prod-database", duration: 30min, reason: "debug urgente")`.
2. Aprovador decide: `DecideJitAccess(decision: Approved, jitId)`.
3. Usuário usa acesso por 30 min.
4. Após 30 min: acesso expirado automaticamente.

**Resultado Esperado:**
- Acesso concedido apenas durante janela; expirado automaticamente.

**Critério de Aceite:** `SecurityEvent` com `JitAccessExpired` após 30 min.

---

### TC-SEC-020 — Acesso JIT negado

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | DecideJitAccess |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** Solicitação JIT pendente.

**Passos:**
1. `DecideJitAccess(decision: Denied, reason: "Não autorizado para produção")`.

**Resultado Esperado:**
- Solicitante notificado da negação.
- Acesso não concedido.

**Critério de Aceite:** HTTP 200; usuário não recebe permissões adicionais.

---

## Campos Criptografados

### TC-SEC-021 — Campo [EncryptedField] salvo cifrado

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | EncryptedStringConverter (AES-256-GCM) |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |

**Pré-condições:** Entidade com propriedade `[EncryptedField] string ApiKey`.

**Passos:**
1. Salvar entidade com `ApiKey = "sk_live_abc123"`.
2. Consultar diretamente na tabela (raw SQL).

**Resultado Esperado:**
- Valor na tabela é o ciphertext (não legível); começa com IV concatenado.
- `entity.ApiKey` via EF retorna `"sk_live_abc123"` (decriptado automaticamente).

**Critério de Aceite:** Raw SQL não retorna plaintext; EF retorna plaintext.

---

### TC-SEC-022 — Senhas SMTP armazenadas cifradas

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | UpsertSmtpConfiguration |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |

**Passos:**
1. Salvar configuração SMTP com senha `"S3cr3t!"`.
2. Verificar storage direto na tabela.

**Resultado Esperado:**
- Senha cifrada na tabela; API nunca retorna o campo `password` em GET.

**Critério de Aceite:** GET SMTP config não inclui `password` na resposta.

---

## Soft Delete

### TC-SEC-023 — Registro soft-deleted invisível em consultas padrão

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | Global soft-delete filter (IsDeleted == false) |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |

**Pré-condições:**
- Entidade `AuditableEntity` com `IsDeleted = true`.

**Passos:**
1. Executar `repository.GetAll()` ou qualquer consulta LINQ padrão.

**Resultado Esperado:**
- Entidades com `IsDeleted = true` **não** retornadas.
- Filtro global `WHERE IsDeleted = false` aplicado automaticamente pelo EF.

**Critério de Aceite:** `result.Count` não inclui entidades deletadas.

---

### TC-SEC-024 — Não é possível bypassar soft-delete via manipulação de parâmetros

| Campo | Valor |
|-------|-------|
| **Módulo** | Transversal |
| **Feature** | Global query filter |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |

**Passos:**
1. Tentar passar `includeDeleted=true` em endpoint público.

**Resultado Esperado:**
- Endpoint não expõe parâmetro `includeDeleted`; registros deletados nunca visíveis via API.

**Critério de Aceite:** 0 registros deletados retornados via API pública.

---

## IPublicRequest e Pipeline

### TC-SEC-025 — Endpoint público bypassa TenantIsolationBehavior

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | TenantIsolationBehavior + IPublicRequest |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |

**Pré-condições:**
- `LocalLogin.Command` implementa `IPublicRequest`.

**Passos:**
1. Chamar `LocalLogin.Command` sem token de tenant.
2. `TenantIsolationBehavior.Handle` verifica: se `IPublicRequest` → passa sem verificar tenant.

**Resultado Esperado:**
- Login executado sem erro de tenant.

**Critério de Aceite:** HTTP 200 com JWT.

---

### TC-SEC-026 — Endpoint privado sem tenant rejeitado

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | TenantIsolationBehavior |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |

**Pré-condições:**
- Request sem tenant no contexto (token sem `tenantId` claim).

**Passos:**
1. Chamar qualquer Command que **não** implementa `IPublicRequest`.
2. `TenantIsolationBehavior` verifica `ICurrentTenant.Id`.

**Resultado Esperado:**
- `Error.Unauthorized("TenantRequired", "...")`.

**Critério de Aceite:** HTTP 401.

---

### TC-SEC-027 — Limite de sessões simultâneas

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | ListActiveSessions / RevokeSession |
| **Tipo** | Segurança |
| **Prioridade** | Alta |

**Pré-condições:** Plano com limite de 5 sessões simultâneas por usuário.

**Passos:**
1. Fazer login 5 vezes (diferentes dispositivos).
2. Tentar 6ª sessão.

**Resultado Esperado:**
- 6ª sessão rejeitada ou sessão mais antiga revogada automaticamente (conforme política).

**Critério de Aceite:** Nunca mais de 5 sessões ativas simultâneas.

---

### TC-SEC-028 — API Token com escopo restrito

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | CreatePlatformApiToken |
| **Tipo** | Segurança |
| **Prioridade** | Alta |

**Passos:**
1. Criar API token com escopo `catalog:read`.
2. Usar token para `POST /catalog/drafts` (escopo de escrita).

**Resultado Esperado:**
- HTTP 403; escopo insuficiente.

**Critério de Aceite:** Token não autoriza operações fora do escopo declarado.

---

### TC-SEC-029 — Revogar API Token

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | RevokePlatformApiToken |
| **Tipo** | Segurança |
| **Prioridade** | Alta |

**Passos:**
1. Revogar token `T1` via `RevokePlatformApiToken`.
2. Tentar usar `T1`.

**Resultado Esperado:**
- HTTP 401; token revogado não aceito.

**Critério de Aceite:** Log de revogação registrado em `SecurityEvents`.

---

### TC-SEC-030 — AuditInterceptor preenche CreatedBy/UpdatedBy

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | AuditInterceptor |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Passos:**
1. Criar entidade `AuditableEntity` com usuário autenticado `user1@empresa.com`.
2. Atualizar a entidade com `user2@empresa.com`.

**Resultado Esperado:**
- `CreatedBy = "user1@empresa.com"`; `CreatedAt` preenchido.
- `UpdatedBy = "user2@empresa.com"`; `UpdatedAt` atualizado.

**Critério de Aceite:** Campos de auditoria preenchidos automaticamente sem atribuição manual.

---

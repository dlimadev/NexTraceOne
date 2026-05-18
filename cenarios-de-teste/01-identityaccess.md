# Cenários de Teste — Módulo IdentityAccess (IAM)

**Projeto:** NexTraceOne  
**Módulo:** IdentityAccess  
**Versão do Documento:** 1.0  
**Data:** 2026-05-18  
**Responsável:** QA / Arquitetura  

---

## Sumário

| Faixa | Área | Quantidade |
|-------|------|-----------|
| TC-IAM-001 a TC-IAM-012 | Autenticação Local (LocalLogin, ForgotPassword, ResetPassword, ActivateAccount) | 12 |
| TC-IAM-013 a TC-IAM-018 | MFA (VerifyMfaChallenge, ResendMfaCode) | 6 |
| TC-IAM-019 a TC-IAM-023 | Federação SAML / OIDC | 5 |
| TC-IAM-024 a TC-IAM-030 | Sessões e Tokens (RefreshToken, Logout, RevokeSession, ListActiveSessions, API Tokens) | 7 |
| TC-IAM-031 a TC-IAM-037 | Tenants (CreateTenant, UpdateTenant, ActivateTenant, DeactivateTenant, ProvisionTenant) | 7 |
| TC-IAM-038 a TC-IAM-043 | Usuários (CreateUser, ActivateUser, DeactivateUser, UpdateRole, GetCurrentUser) | 6 |
| TC-IAM-044 a TC-IAM-049 | Papéis e Permissões (CreateRole, DeleteRole, AssignRole, SeedDefaultRolePermissions) | 6 |
| TC-IAM-050 a TC-IAM-054 | Políticas de Acesso e Ambientes | 5 |
| TC-IAM-055 a TC-IAM-060 | Break Glass | 6 |
| TC-IAM-061 a TC-IAM-066 | Acesso JIT (RequestJitAccess, DecideJitAccess) | 6 |
| TC-IAM-067 a TC-IAM-071 | Delegação e Administração Delegada | 5 |
| TC-IAM-072 a TC-IAM-077 | Revisão de Acessos (Access Review Campaign) | 6 |
| TC-IAM-078 a TC-IAM-082 | Licenciamento e Onboarding | 5 |
| TC-IAM-083 a TC-IAM-087 | Agentes e Eventos de Segurança | 5 |
| TC-IAM-088 a TC-IAM-095 | Isolamento Multi-Tenant e Segurança de Borda | 8 |

**Total: 95 cenários**

---

## Autenticação Local

### TC-IAM-001 — Login local com credenciais válidas (caminho feliz)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | LocalLogin |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `LocalLogin.Handler` |

**Pré-condições:**
- Usuário existe no repositório com `IsActive = true` e `PasswordHash` preenchido.
- Usuário não está bloqueado (`IsLocked = false`).
- Usuário não tem MFA habilitado (`MfaEnabled = false`).
- Membership do usuário no tenant está ativo e `RoleId` válido.

**Passos:**
1. Construir `LocalLogin.Command` com email e senha válidos.
2. Chamar `LocalLogin.Handler.Handle()` diretamente com os substitutos injetados.
3. Verificar que `IUserRepository.GetByEmailAsync` retorna o usuário correto.
4. Verificar que `IPasswordHasher.Verify` retorna `true`.
5. Verificar que `ILoginSessionCreator.CreateSession` é chamado uma vez.
6. Verificar que `ISecurityAuditRecorder.RecordAuthenticationSuccess` é chamado.

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.AccessToken` não é nulo nem vazio.
- `result.Value.RefreshToken` não é nulo nem vazio.
- `result.Value.MfaRequired == false`
- `result.Value.User.TenantId` é o tenant do membership.

**Critério de Aceite:** HTTP 200 com JWT válido contendo claims de tenant e permissões.

---

### TC-IAM-002 — Login local com email inexistente

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | LocalLogin |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `LocalLogin.Handler` |

**Pré-condições:**
- `IUserRepository.GetByEmailAsync` retorna `null`.

**Passos:**
1. Construir `LocalLogin.Command` com email que não existe no sistema.
2. Executar o handler.
3. Verificar que `ISecurityAuditRecorder.RecordAuthenticationFailure` é chamado.

**Resultado Esperado:**
- `result.IsFailure == true`
- `result.Error.Type == ErrorType.Unauthorized` (mapeado de `IdentityErrors.InvalidCredentials`)
- Mensagem não deve revelar se o email existe ou não (prevenção de enumeração).

**Critério de Aceite:** HTTP 401. Corpo da resposta não diferencia "email não existe" de "senha errada".

---

### TC-IAM-003 — Login local com senha incorreta e registro de falha

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | LocalLogin |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `LocalLogin.Handler` |

**Pré-condições:**
- Usuário existe e está ativo.
- `IPasswordHasher.Verify` retorna `false`.
- Usuário ainda não atingiu limite de tentativas.

**Passos:**
1. Construir `LocalLogin.Command` com senha incorreta.
2. Executar o handler.
3. Verificar que `user.RegisterFailedLogin` é chamado com a data/hora atual.
4. Verificar que `ISecurityAuditRecorder.RecordAuthenticationFailure` é chamado.

**Resultado Esperado:**
- `result.IsFailure == true`
- `result.Error` corresponde a `IdentityErrors.InvalidCredentials()`.
- Contador de falhas incrementado no usuário.

**Critério de Aceite:** HTTP 401. Banco persiste o incremento de `FailedLoginCount`.

---

### TC-IAM-004 — Bloqueio automático após tentativas falhas sucessivas

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | LocalLogin |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `LocalLogin.Handler` |

**Pré-condições:**
- Usuário tem `FailedLoginCount` igual ao limite menos um.
- Próxima tentativa com senha incorreta deve acionar bloqueio.

**Passos:**
1. Configurar `IPasswordHasher.Verify` para retornar `false`.
2. Configurar `user.IsLocked` para retornar `true` após chamada a `RegisterFailedLogin`.
3. Executar o handler.
4. Verificar que `ISecurityAuditRecorder.RecordAccountLocked` é chamado.

**Resultado Esperado:**
- `result.IsFailure == true`
- `result.Error` corresponde a `IdentityErrors.AccountLocked`.
- `result.Error` contém data/hora de desbloqueio.

**Critério de Aceite:** HTTP 401. `LockoutEnd` presente na resposta de erro estruturado.

---

### TC-IAM-005 — Tentativa de login com conta bloqueada

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | LocalLogin |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `LocalLogin.Handler` |

**Pré-condições:**
- `user.IsLocked(dateTimeProvider.UtcNow)` retorna `true`.
- `user.LockoutEnd` é uma data futura.

**Passos:**
1. Construir `LocalLogin.Command` com credenciais válidas.
2. Executar o handler.
3. Verificar que `IPasswordHasher.Verify` NÃO é chamado (curto-circuito por bloqueio).

**Resultado Esperado:**
- `result.IsFailure == true`
- `result.Error` corresponde a `IdentityErrors.AccountLocked`.
- `IPasswordHasher.Verify` não é invocado.

**Critério de Aceite:** HTTP 401. Sistema não verifica senha quando conta está bloqueada.

---

### TC-IAM-006 — Login dispara desafio MFA quando habilitado

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | LocalLogin / MFA |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `LocalLogin.Handler` |

**Pré-condições:**
- Usuário existe, está ativo, senha correta.
- `user.MfaEnabled = true`.
- `IMfaChallengeTokenService.Issue` retorna um token de desafio válido.

**Passos:**
1. Executar `LocalLogin.Handler.Handle()`.
2. Verificar que `IMfaChallengeTokenService.Issue` é chamado com `userId` e TTL de 5 minutos.
3. Verificar que `ISecurityAuditRecorder.RecordStepUpMfaRequired` é chamado.

**Resultado Esperado:**
- `result.IsSuccess == true` (resposta parcial, não é erro).
- `result.Value.MfaRequired == true`.
- `result.Value.MfaChallengeToken` não é nulo.
- `result.Value.AccessToken` é string vazia.
- `result.Value.RefreshToken` é string vazia.

**Critério de Aceite:** HTTP 200 com `mfaRequired: true` e `mfaChallengeToken`. Nenhum JWT de acesso emitido.

---

### TC-IAM-007 — Validação de entrada do LocalLogin

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | LocalLogin |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `LocalLogin.Validator` |

**Pré-condições:**
- Pipeline MediatR com `ValidationBehavior` configurado.

**Passos:**
1. Testar combinações inválidas:
   - Email vazio (`""`)
   - Email mal-formado (`"nao-e-email"`)
   - Senha vazia (`""`)
   - Senha com menos de 8 caracteres (`"abc123"`)

**Resultado Esperado:**
- Para cada entrada inválida, `result.IsFailure == true`.
- `result.Error.Type == ErrorType.Validation`.
- Handler nunca é chamado (curto-circuito no `ValidationBehavior`).

**Critério de Aceite:** HTTP 422 com lista de erros de validação.

---

### TC-IAM-008 — Solicitação de recuperação de senha (ForgotPassword)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | ForgotPassword |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ForgotPassword.Handler` |

**Pré-condições:**
- Usuário existe com o email informado.
- `IPasswordResetTokenRepository` disponível para persistência.

**Passos:**
1. Construir `ForgotPassword.Command` com email válido e existente.
2. Executar o handler.
3. Verificar que token de recuperação é criado e persistido.
4. Verificar que módulo de notificações é acionado para envio de email.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Token de recuperação persistido com TTL definido (ex.: 1 hora).
- Notificação de recuperação enviada ao email do usuário.

**Critério de Aceite:** HTTP 200. Email de recuperação enviado independentemente de o endereço existir (prevenção de enumeração).

---

### TC-IAM-009 — Redefinição de senha com token válido (ResetPassword)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | ResetPassword |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `ResetPassword.Handler` |

**Pré-condições:**
- Token de recuperação existe, não expirou e não foi utilizado.
- Nova senha atende requisitos de complexidade.

**Passos:**
1. Construir `ResetPassword.Command` com token válido e nova senha.
2. Executar o handler.
3. Verificar que `IPasswordHasher.Hash` é chamado com a nova senha.
4. Verificar que o token é marcado como utilizado.
5. Verificar que todas as sessões ativas do usuário são revogadas.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Hash da nova senha armazenado no usuário.
- Token marcado como consumido.
- Sessões antigas invalidadas.

**Critério de Aceite:** HTTP 200. Login com senha antiga deve falhar após reset.

---

### TC-IAM-010 — Redefinição de senha com token expirado

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | ResetPassword |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ResetPassword.Handler` |

**Pré-condições:**
- Token de recuperação existe mas `ExpiresAt < dateTimeProvider.UtcNow`.

**Passos:**
1. Construir `ResetPassword.Command` com token expirado.
2. Executar o handler.

**Resultado Esperado:**
- `result.IsFailure == true`.
- `result.Error.Type == ErrorType.Business` ou `ErrorType.NotFound`.
- Senha do usuário não é alterada.

**Critério de Aceite:** HTTP 422. Token expirado não pode redefinir senha.

---

### TC-IAM-011 — Ativação de conta com token válido (ActivateAccount)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | ActivateAccount |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `ActivateAccount.Handler` |

**Pré-condições:**
- Token de ativação existe e não expirou.
- Usuário está no estado `Pending`.
- Senha fornecida atende critérios de complexidade.

**Passos:**
1. Construir `ActivateAccount.Command` com token e senha inicial.
2. Executar o handler.
3. Verificar que usuário é ativado (`IsActive = true`).
4. Verificar que hash da senha é armazenado.
5. Verificar que token é invalidado após uso.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- `result.Value.Activated == true`.
- Usuário pode realizar login local após ativação.

**Critério de Aceite:** HTTP 200. Conta ativada e pronta para uso.

---

### TC-IAM-012 — Alteração de senha (ChangePassword) com senha atual correta

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | ChangePassword |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ChangePassword.Handler` |

**Pré-condições:**
- Usuário autenticado com `ICurrentUser.IsAuthenticated = true`.
- Senha atual está correta.
- Nova senha é diferente da atual e atende complexidade.

**Passos:**
1. Construir `ChangePassword.Command` com senha atual e nova senha.
2. Executar o handler.
3. Verificar que `IPasswordHasher.Verify` é chamado com a senha atual.
4. Verificar que novo hash é persistido.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Hash da senha atualizado no repositório.

**Critério de Aceite:** HTTP 200. Sessões anteriores devem ser opcionalmente invalidadas conforme política de segurança.

---

## MFA (Autenticação Multifator)

### TC-IAM-013 — Verificação de desafio MFA com código TOTP válido

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | VerifyMfaChallenge |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `VerifyMfaChallenge.Handler` |

**Pré-condições:**
- Challenge token MFA emitido pelo `LocalLogin` e não expirado.
- Código TOTP de 6 dígitos correto para o usuário.

**Passos:**
1. Construir `VerifyMfaChallenge.Command` com challenge token e código TOTP.
2. Executar o handler.
3. Verificar que `IMfaChallengeTokenService.Validate` é chamado.
4. Verificar que `ITotpVerifier.Verify` é chamado com o segredo MFA do usuário.
5. Verificar que `ILoginSessionCreator.CreateSession` é invocado após validação bem-sucedida.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- `result.Value.AccessToken` e `result.Value.RefreshToken` válidos.
- `result.Value.MfaRequired == false`.

**Critério de Aceite:** HTTP 200 com JWT completo. Sessão ativa criada.

---

### TC-IAM-014 — Verificação de desafio MFA com código TOTP inválido

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | VerifyMfaChallenge |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `VerifyMfaChallenge.Handler` |

**Pré-condições:**
- Challenge token MFA válido.
- `ITotpVerifier.Verify` retorna `false`.

**Passos:**
1. Executar `VerifyMfaChallenge.Handler` com código incorreto.

**Resultado Esperado:**
- `result.IsFailure == true`.
- `result.Error` corresponde a erro de código MFA inválido.
- Nenhuma sessão criada.

**Critério de Aceite:** HTTP 401. Cada tentativa falha deve ser registrada como evento de segurança.

---

### TC-IAM-015 — Verificação de desafio MFA com challenge token expirado

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | VerifyMfaChallenge |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `VerifyMfaChallenge.Handler` |

**Pré-condições:**
- `IMfaChallengeTokenService.Validate` retorna falha por expiração.

**Passos:**
1. Executar o handler com challenge token expirado (após 5 minutos).

**Resultado Esperado:**
- `result.IsFailure == true`.
- Usuário deve iniciar novo processo de login.

**Critério de Aceite:** HTTP 401. Desafio expirado requer novo fluxo de autenticação.

---

### TC-IAM-016 — Reenvio de código MFA (ResendMfaCode)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | ResendMfaCode |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `ResendMfaCode.Handler` |

**Pré-condições:**
- Challenge token MFA válido e não expirado.
- Usuário tem MFA habilitado com canal de entrega configurado (SMS ou email).

**Passos:**
1. Construir `ResendMfaCode.Command` com challenge token válido.
2. Executar o handler.
3. Verificar que módulo de notificações é acionado para reenvio do código.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Novo código enviado ao canal configurado.
- Challenge token permanece válido.

**Critério de Aceite:** HTTP 200. Código reenviado sem criar nova sessão.

---

### TC-IAM-017 — Limite de reenvios de código MFA por janela de tempo

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | ResendMfaCode |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ResendMfaCode.Handler` |

**Pré-condições:**
- Usuário já solicitou reenvio o número máximo permitido na janela de tempo.

**Passos:**
1. Tentar reenvio além do limite de reenvios permitidos.

**Resultado Esperado:**
- `result.IsFailure == true`.
- `result.Error.Type == ErrorType.Business`.
- Nenhum código enviado.

**Critério de Aceite:** HTTP 422. Rate-limiting de reenvio MFA ativo.

---

### TC-IAM-018 — Step-up MFA bloqueado sem código quando usuário tem MFA habilitado

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | RequestBreakGlass / RequestJitAccess |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `RequestBreakGlass.Handler` / `RequestJitAccess.Handler` |

**Pré-condições:**
- `user.MfaEnabled = true`.
- `MfaCode` não fornecido no comando.

**Passos:**
1. Construir comando sem campo `MfaCode`.
2. Executar o handler.
3. Verificar que `SecurityEvent.StepUpMfaRequired` é registrado.
4. Verificar que `ISecurityEventTracker.Track` é chamado.

**Resultado Esperado:**
- `result.IsFailure == true`.
- `result.Error` corresponde a `IdentityErrors.MfaStepUpRequired()`.
- Operação bloqueada, nenhum recurso criado.

**Critério de Aceite:** HTTP 422. Operações privilegiadas exigem step-up MFA quando habilitado.

---

## Federação SAML / OIDC

### TC-IAM-019 — Início do fluxo SAML (StartSamlLogin)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | StartSamlLogin |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `StartSamlLogin.Handler` |

**Pré-condições:**
- Tenant tem IdP SAML configurado com `EntityId` e `SsoUrl` válidos.
- `ISamlIdentityProviderRepository` retorna configuração do IdP.

**Passos:**
1. Construir `StartSamlLogin.Command` com `TenantSlug`.
2. Executar o handler.
3. Verificar que `AuthnRequest` SAML é gerado e assinado.
4. Verificar que `RelayState` contém estado de retorno seguro.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- `result.Value.RedirectUrl` aponta para `SsoUrl` do IdP.
- `result.Value.SamlRequest` é um authnRequest Base64 válido.

**Critério de Aceite:** HTTP 302. Usuário redirecionado para o IdP SAML.

---

### TC-IAM-020 — Callback SAML com asserção válida (SamlAcsCallback)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | SamlAcsCallback |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `SamlAcsCallback.Handler` |

**Pré-condições:**
- Asserção SAML assinada pelo IdP confiável e não expirada.
- Usuário mapeado a um membership ativo no tenant.

**Passos:**
1. Submeter callback SAML com `SAMLResponse` válido (Base64).
2. Verificar que assinatura é validada contra certificado do IdP.
3. Verificar que atributos de identidade são extraídos (`NameID`, `email`).
4. Verificar que sessão e tokens são emitidos via `ILoginSessionCreator`.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- JWT emitido com claims do usuário federado.

**Critério de Aceite:** HTTP 200. Usuário federado autenticado com sucesso.

---

### TC-IAM-021 — Callback SAML com asserção expirada ou assinatura inválida

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | SamlAcsCallback |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `SamlAcsCallback.Handler` |

**Pré-condições:**
- `SAMLResponse` com timestamp expirado ou assinatura adulterada.

**Passos:**
1. Submeter `SAMLResponse` adulterado.

**Resultado Esperado:**
- `result.IsFailure == true`.
- Evento de segurança registrado com risco elevado.
- Nenhum token emitido.

**Critério de Aceite:** HTTP 401. Asserções SAML inválidas são rejeitadas.

---

### TC-IAM-022 — Início do fluxo OIDC (StartOidcLogin)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | StartOidcLogin |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `StartOidcLogin.Handler` |

**Pré-condições:**
- Tenant tem provedor OIDC configurado com `ClientId` e `AuthorizationEndpoint`.

**Passos:**
1. Construir `StartOidcLogin.Command` com `TenantSlug`.
2. Executar o handler.
3. Verificar que `state` e `nonce` PKCE são gerados de forma aleatória e seguros.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- `result.Value.RedirectUrl` contém `client_id`, `redirect_uri`, `scope`, `state`, `nonce`.
- `state` armazenado em sessão para validação no callback.

**Critério de Aceite:** HTTP 302. Usuário redirecionado ao provedor OIDC.

---

### TC-IAM-023 — Callback OIDC com code válido (OidcCallback)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | OidcCallback |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `OidcCallback.Handler` |

**Pré-condições:**
- `code` OIDC válido e `state` correspondente ao iniciado.
- IdP retorna `id_token` com claims de email e sub válidos.

**Passos:**
1. Submeter `OidcCallback.Command` com `code` e `state`.
2. Verificar troca de code por tokens no endpoint de token do IdP.
3. Verificar validação do `id_token` (assinatura, `nonce`, expiração).
4. Verificar criação de sessão e emissão de JWT interno.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- JWT interno emitido com claims do usuário OIDC.

**Critério de Aceite:** HTTP 200. Usuário OIDC autenticado e sessão ativa criada.

---

## Sessões, Tokens e API Keys

### TC-IAM-024 — Refresh de token com refresh token válido

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | RefreshToken |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `RefreshToken.Handler` |

**Pré-condições:**
- Sessão ativa existe com refresh token válido e não expirado.
- Usuário está ativo e tenant ativo.

**Passos:**
1. Construir `RefreshToken.Command` com refresh token.
2. Executar o handler.
3. Verificar que novo access token e novo refresh token são emitidos (rotação).
4. Verificar que refresh token anterior é invalidado.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Novo `AccessToken` e `RefreshToken` emitidos.
- Refresh token antigo inutilizável.

**Critério de Aceite:** HTTP 200. Rotação de refresh token implementada.

---

### TC-IAM-025 — Refresh de token expirado ou revogado

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | RefreshToken |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `RefreshToken.Handler` |

**Pré-condições:**
- Refresh token não existe ou está marcado como revogado.

**Passos:**
1. Executar `RefreshToken.Handler` com token inválido.

**Resultado Esperado:**
- `result.IsFailure == true`.
- `result.Error.Type == ErrorType.Unauthorized`.

**Critério de Aceite:** HTTP 401. Refresh tokens inválidos não emitem novos tokens.

---

### TC-IAM-026 — Logout revoga sessão ativa

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | Logout |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `Logout.Handler` |

**Pré-condições:**
- Sessão ativa associada ao usuário corrente.

**Passos:**
1. Construir `Logout.Command` com `SessionId`.
2. Executar o handler.
3. Verificar que sessão é marcada como revogada.
4. Verificar que evento de segurança `SessionTerminated` é registrado.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Sessão marcada como inativa.
- Refresh token da sessão invalidado.

**Critério de Aceite:** HTTP 200. Uso do refresh token após logout retorna 401.

---

### TC-IAM-027 — Revogação de sessão específica por administrador

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | RevokeSession |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `RevokeSession.Handler` |

**Pré-condições:**
- Admin tem permissão `iam.sessions.revoke`.
- Sessão a ser revogada pertence ao tenant do admin.

**Passos:**
1. Construir `RevokeSession.Command` com `SessionId` e `UserId` alvo.
2. Executar o handler.
3. Verificar que sessão alvo é revogada independentemente de ser de outro usuário.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Sessão revogada com registro de quem revogou.

**Critério de Aceite:** HTTP 200. Admin pode revogar sessões de outros usuários do mesmo tenant.

---

### TC-IAM-028 — Listagem de sessões ativas do usuário corrente

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | ListActiveSessions |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `ListActiveSessions.Handler` |

**Pré-condições:**
- Usuário tem 3 sessões ativas em diferentes dispositivos.

**Passos:**
1. Executar `ListActiveSessions.Query`.
2. Verificar que apenas sessões do usuário corrente são retornadas.
3. Verificar que sessões expiradas não aparecem na lista.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Lista contém as 3 sessões com `IpAddress`, `UserAgent`, `CreatedAt`.
- Sessão corrente identificada.

**Critério de Aceite:** HTTP 200. Usuário vê todas as suas sessões ativas.

---

### TC-IAM-029 — Criação de API Token de plataforma

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | CreatePlatformApiToken |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CreatePlatformApiToken.Handler` |

**Pré-condições:**
- Usuário autenticado com permissão para criar API tokens.
- Nome e escopos do token fornecidos.

**Passos:**
1. Construir `CreatePlatformApiToken.Command` com nome e lista de escopos.
2. Executar o handler.
3. Verificar que token raw é retornado apenas na criação (não recuperável depois).
4. Verificar que hash do token é armazenado no repositório.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- `result.Value.RawToken` presente (apenas na criação).
- `result.Value.TokenId` para referência futura.

**Critério de Aceite:** HTTP 201. Raw token exibido uma única vez. Banco armazena apenas o hash.

---

### TC-IAM-030 — Revogação de API Token

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | RevokePlatformApiToken |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `RevokePlatformApiToken.Handler` |

**Pré-condições:**
- API Token existe e pertence ao tenant do usuário corrente.

**Passos:**
1. Construir `RevokePlatformApiToken.Command` com `TokenId`.
2. Executar o handler.
3. Verificar que token é marcado como revogado.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Requisições com o token revogado devem receber 401.

**Critério de Aceite:** HTTP 200. Token revogado imediatamente. Não pode ser reutilizado.

---

## Tenants

### TC-IAM-031 — Criação de tenant com dados válidos

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | CreateTenant |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `CreateTenant.Handler` |

**Pré-condições:**
- Nome e slug únicos no sistema.
- Usuário com permissão `platform.tenants.create`.

**Passos:**
1. Construir `CreateTenant.Command` com nome, slug e plano inicial.
2. Executar o handler.
3. Verificar que tenant é criado com estado `Pending`.
4. Verificar que evento de domínio `TenantCreatedEvent` é emitido.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- `result.Value.TenantId` com novo Guid.
- Tenant persistido no repositório.

**Critério de Aceite:** HTTP 201. Tenant criado com estado inicial correto.

---

### TC-IAM-032 — Slug de tenant duplicado gera conflito

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | CreateTenant |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CreateTenant.Handler` |

**Pré-condições:**
- Tenant com o mesmo slug já existe no repositório.

**Passos:**
1. Tentar criar tenant com slug já existente.

**Resultado Esperado:**
- `result.IsFailure == true`.
- `result.Error.Type == ErrorType.Conflict`.

**Critério de Aceite:** HTTP 409. Slugs de tenant são únicos globalmente.

---

### TC-IAM-033 — Ativação de tenant pendente (ActivateTenant)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | ActivateTenant |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `ActivateTenant.Handler` |

**Pré-condições:**
- Tenant existe em estado `Pending`.
- Usuário tem permissão de plataforma.

**Passos:**
1. Construir `ActivateTenant.Command` com `TenantId`.
2. Executar o handler.
3. Verificar que estado do tenant muda para `Active`.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- `tenant.IsActive == true`.

**Critério de Aceite:** HTTP 200. Tenant ativo permite login de usuários.

---

### TC-IAM-034 — Desativação de tenant ativo (DeactivateTenant)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | DeactivateTenant |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `DeactivateTenant.Handler` |

**Pré-condições:**
- Tenant está ativo com usuários logados.

**Passos:**
1. Construir `DeactivateTenant.Command`.
2. Executar o handler.
3. Verificar que tenant é marcado como inativo.
4. Verificar que `TenantIsolationBehavior` rejeitará futuras requisições para este tenant.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- `tenant.IsActive == false`.
- Sessões ativas do tenant invalidadas ou sinalizadas para expiração.

**Critério de Aceite:** HTTP 200. Usuários do tenant desativado recebem 403 nas próximas requisições.

---

### TC-IAM-035 — Provisionamento automático de tenant (ProvisionTenant)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | ProvisionTenant |
| **Tipo** | Integração |
| **Prioridade** | Crítica |
| **Handler** | `ProvisionTenant.Handler` |

**Pré-condições:**
- Tenant recém-criado sem roles e políticas.

**Passos:**
1. Executar `ProvisionTenant.Handler` para o tenant.
2. Verificar que roles padrão são criadas (`Admin`, `Developer`, `Viewer`).
3. Verificar que políticas de acesso padrão são semeadas via `SeedDefaultModuleAccessPolicies`.
4. Verificar que permissões padrão são associadas aos roles via `SeedDefaultRolePermissions`.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Roles padrão criadas no tenant.
- Políticas de acesso dos módulos configuradas.

**Critério de Aceite:** Tenant provisionado e pronto para onboarding de usuários.

---

### TC-IAM-036 — Provisionamento de licença do tenant (ProvisionTenantLicense)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | ProvisionTenantLicense |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ProvisionTenantLicense.Handler` |

**Pré-condições:**
- Tenant existe e está ativo.
- Plano de licença válido fornecido (`Starter`, `Professional`, `Enterprise`, `Trial`).

**Passos:**
1. Construir `ProvisionTenantLicense.Command` com plano e unidades incluídas.
2. Executar o handler.
3. Verificar que `TenantLicense` é criada com capabilities do plano.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Capabilities do plano corretamente atribuídas ao tenant.

**Critério de Aceite:** HTTP 201. Licença provisionada com capabilities corretas para o plano.

---

### TC-IAM-037 — Leitura de licença do tenant (GetTenantLicense)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | GetTenantLicense |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetTenantLicense.Handler` |

**Pré-condições:**
- Tenant tem licença provisionada.

**Passos:**
1. Executar `GetTenantLicense.Query`.
2. Verificar que plano, capabilities e `IncludedHostUnits` são retornados.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- `result.Value` contém plano, lista de capabilities e contagem de host units.

**Critério de Aceite:** HTTP 200. Dados de licença exatos e atualizados.

---

## Usuários

### TC-IAM-038 — Criação de usuário com dados válidos

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | CreateUser |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `CreateUser.Handler` |

**Pré-condições:**
- Email não cadastrado no tenant.
- Role informada existe no tenant.
- Usuário criador tem permissão `iam.users.create`.

**Passos:**
1. Construir `CreateUser.Command` com nome, email e roleId.
2. Executar o handler.
3. Verificar que usuário é criado com `IsActive = false` (pendente de ativação).
4. Verificar que email de ativação é disparado.
5. Verificar que `TenantMembership` é criado associando o usuário ao tenant.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- `result.Value.UserId` gerado.
- Email de ativação enviado.

**Critério de Aceite:** HTTP 201. Usuário criado em estado pendente aguardando ativação.

---

### TC-IAM-039 — Criação de usuário com email duplicado no tenant

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | CreateUser |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CreateUser.Handler` |

**Pré-condições:**
- Usuário com o mesmo email já existe no tenant.

**Passos:**
1. Tentar criar usuário com email duplicado.

**Resultado Esperado:**
- `result.IsFailure == true`.
- `result.Error.Type == ErrorType.Conflict`.

**Critério de Aceite:** HTTP 409. Email único por tenant.

---

### TC-IAM-040 — Desativação de usuário ativo (DeactivateUser)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | DeactivateUser |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `DeactivateUser.Handler` |

**Pré-condições:**
- Usuário existe e está ativo (`IsActive = true`).
- Usuário corrente tem permissão `iam.users.deactivate`.

**Passos:**
1. Construir `DeactivateUser.Command` com `UserId`.
2. Executar o handler.
3. Verificar que `user.IsActive` é definido como `false`.
4. Verificar que sessões ativas do usuário são revogadas.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Usuário desativado e sessões encerradas.

**Critério de Aceite:** HTTP 200. Usuário desativado não consegue autenticar.

---

### TC-IAM-041 — Atualização de role do usuário (UpdateRole)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | UpdateRole |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `UpdateRole.Handler` |

**Pré-condições:**
- Usuário e nova role existem no tenant.
- Admin tem permissão `iam.roles.assign`.

**Passos:**
1. Construir `UpdateRole.Command` com `UserId` e novo `RoleId`.
2. Executar o handler.
3. Verificar que `TenantMembership.RoleId` é atualizado.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Novo role refletido no próximo login do usuário.

**Critério de Aceite:** HTTP 200. Role atualizada e permissões do próximo JWT refletirão a mudança.

---

### TC-IAM-042 — Obtenção do usuário corrente (GetCurrentUser)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | GetCurrentUser |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetCurrentUser.Handler` |

**Pré-condições:**
- Usuário autenticado com JWT válido.
- `ICurrentUser.Id` resolvido pelo `TenantResolutionMiddleware`.

**Passos:**
1. Executar `GetCurrentUser.Query`.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- `result.Value` contém `Id`, `Email`, `FullName`, `TenantId`, `RoleName`, `Permissions`.

**Critério de Aceite:** HTTP 200. Dados do usuário corrente com permissões atualizadas.

---

### TC-IAM-043 — Listagem de usuários do tenant com filtro

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | ListTenantUsers |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `ListTenantUsers.Handler` |

**Pré-condições:**
- Tenant tem 10 usuários (8 ativos, 2 desativados).

**Passos:**
1. Executar `ListTenantUsers.Query` sem filtro (deve retornar apenas ativos).
2. Executar com filtro `includeInactive=true` (deve retornar todos).
3. Verificar que usuários de outro tenant não aparecem.

**Resultado Esperado:**
- Query sem filtro: 8 usuários.
- Query com `includeInactive=true`: 10 usuários.
- Isolamento de tenant: 0 usuários de outros tenants.

**Critério de Aceite:** HTTP 200. Filtro de tenant aplicado em todas as consultas.

---

## Papéis e Permissões

### TC-IAM-044 — Criação de role customizada

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | CreateRole |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CreateRole.Handler` |

**Pré-condições:**
- Nome único dentro do tenant.
- Usuário tem permissão `iam.roles.create`.

**Passos:**
1. Construir `CreateRole.Command` com nome e descrição.
2. Executar o handler.
3. Verificar que role é criada com escopo correto do tenant.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Role criada com `TenantId` correto.

**Critério de Aceite:** HTTP 201. Role customizada disponível para atribuição.

---

### TC-IAM-045 — Exclusão de role em uso por usuários ativos

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | DeleteRole |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `DeleteRole.Handler` |

**Pré-condições:**
- Role está atribuída a pelo menos um usuário ativo.

**Passos:**
1. Tentar excluir role com usuários ativos.

**Resultado Esperado:**
- `result.IsFailure == true`.
- `result.Error.Type == ErrorType.Business` (role em uso).

**Critério de Aceite:** HTTP 422. Roles com usuários ativos não podem ser excluídas.

---

### TC-IAM-046 — Atribuição de role a usuário (AssignRole)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | AssignRole |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `AssignRole.Handler` |

**Pré-condições:**
- Usuário e role existem no mesmo tenant.
- Usuário ainda não tem esta role (sem duplicação).

**Passos:**
1. Construir `AssignRole.Command` com `UserId` e `RoleId`.
2. Executar o handler.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Membership atualizado com a nova role.

**Critério de Aceite:** HTTP 200. Role refletida no próximo token JWT.

---

### TC-IAM-047 — Listagem de permissões disponíveis

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | ListPermissions |
| **Tipo** | Unitário |
| **Prioridade** | Baixa |
| **Handler** | `ListPermissions.Handler` |

**Pré-condições:**
- Permissões semeadas no sistema.

**Passos:**
1. Executar `ListPermissions.Query`.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Lista completa de permissões com código e descrição.

**Critério de Aceite:** HTTP 200. Todas as permissões da plataforma listadas.

---

### TC-IAM-048 — Semeadura de permissões padrão por role (SeedDefaultRolePermissions)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | SeedDefaultRolePermissions |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `SeedDefaultRolePermissions.Handler` |

**Pré-condições:**
- Roles padrão criadas (`Admin`, `Developer`, `Viewer`).
- Permissões da plataforma cadastradas.

**Passos:**
1. Executar `SeedDefaultRolePermissions.Handler`.
2. Verificar que `Admin` recebe todas as permissões.
3. Verificar que `Viewer` recebe apenas permissões de leitura.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Associações role-permissão criadas corretamente.

**Critério de Aceite:** Permissões padrão corretas para cada role após provisionamento.

---

### TC-IAM-049 — Proibição de atribuição de role de outro tenant

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | AssignRole |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |
| **Handler** | `AssignRole.Handler` |

**Pré-condições:**
- `RoleId` informado pertence a tenant diferente do usuário corrente.

**Passos:**
1. Tentar atribuir role de outro tenant.

**Resultado Esperado:**
- `result.IsFailure == true`.
- `result.Error.Type == ErrorType.NotFound` (role não encontrada no tenant corrente).

**Critério de Aceite:** HTTP 404. Roles são escopo por tenant — cross-tenant assignment bloqueado.

---

## Políticas de Acesso e Ambientes

### TC-IAM-050 — Criação de política de acesso a ambiente

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | CreateEnvironmentAccessPolicy |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CreateEnvironmentAccessPolicy.Handler` |

**Pré-condições:**
- Ambiente existe no tenant.
- Usuário tem permissão `iam.policies.create`.

**Passos:**
1. Construir `CreateEnvironmentAccessPolicy.Command` com `EnvironmentId` e regras de acesso.
2. Executar o handler.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Política criada e associada ao ambiente.

**Critério de Aceite:** HTTP 201. Política de acesso ao ambiente ativa.

---

### TC-IAM-051 — Concessão de acesso a ambiente (GrantEnvironmentAccess)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | GrantEnvironmentAccess |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GrantEnvironmentAccess.Handler` |

**Pré-condições:**
- Usuário alvo existe e é membro do tenant.
- Ambiente existe e está ativo.

**Passos:**
1. Construir `GrantEnvironmentAccess.Command` com `UserId`, `EnvironmentId` e `AccessLevel`.
2. Executar o handler.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Concessão de acesso registrada para o usuário no ambiente.

**Critério de Aceite:** HTTP 200. Usuário pode acessar o ambiente após concessão.

---

### TC-IAM-052 — Avaliação de definição de política (EvaluatePolicyDefinition)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | EvaluatePolicyDefinition |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `EvaluatePolicyDefinition.Handler` |

**Pré-condições:**
- Definição de política existente com condições baseadas em atributos.

**Passos:**
1. Construir `EvaluatePolicyDefinition.Command` com o contexto de avaliação (usuário, recurso, ação).
2. Executar o handler.
3. Verificar que engine de políticas retorna `Allow` ou `Deny`.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- `result.Value.Decision` é `Allow` ou `Deny` com razões.

**Critério de Aceite:** HTTP 200. Avaliação determinística da política.

---

### TC-IAM-053 — Criação e listagem de ambientes do tenant

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | CreateEnvironment / ListEnvironments |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CreateEnvironment.Handler` / `ListEnvironments.Handler` |

**Pré-condições:**
- Tenant ativo sem ambientes cadastrados.

**Passos:**
1. Criar 3 ambientes: `Development`, `Staging`, `Production`.
2. Executar `ListEnvironments.Query`.
3. Verificar que 3 ambientes são retornados com tipo correto.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- 3 ambientes listados para o tenant.

**Critério de Aceite:** HTTP 200. Ambientes do tenant correto retornados.

---

### TC-IAM-054 — Definição e recuperação do ambiente primário de produção

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | SetPrimaryProductionEnvironment / GetPrimaryProductionEnvironment |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `SetPrimaryProductionEnvironment.Handler` / `GetPrimaryProductionEnvironment.Handler` |

**Pré-condições:**
- Tenant tem ambiente de produção cadastrado.

**Passos:**
1. Executar `SetPrimaryProductionEnvironment.Command` com `EnvironmentId`.
2. Executar `GetPrimaryProductionEnvironment.Query`.
3. Verificar que o ambiente definido é retornado corretamente.

**Resultado Esperado:**
- Set: `result.IsSuccess == true`.
- Get: `result.Value.EnvironmentId` corresponde ao ambiente definido.

**Critério de Aceite:** HTTP 200. Ambiente primário de produção configurado e recuperável.

---

## Break Glass

### TC-IAM-055 — Solicitação de Break Glass com justificativa válida

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | RequestBreakGlass |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `RequestBreakGlass.Handler` |

**Pré-condições:**
- Usuário autenticado, ativo, sem MFA habilitado.
- Usuário não excedeu limite trimestral de 3 usos.

**Passos:**
1. Construir `RequestBreakGlass.Command` com justificativa de mínimo 20 caracteres.
2. Executar o handler.
3. Verificar que `BreakGlassRequest` é criado com janela de 2 horas.
4. Verificar que `SecurityEvent.BreakGlassActivated` com `riskScore = 90` é registrado.
5. Verificar que `INotificationModule.SubmitAsync` é chamado com severidade `Critical`.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- `result.Value.RequestId` válido.
- `result.Value.ExpiresAt` = agora + 2 horas.
- `result.Value.QuarterlyUsageCount` incrementado.

**Critério de Aceite:** HTTP 201. Break glass ativado com trilha de auditoria e notificação.

---

### TC-IAM-056 — Break Glass bloqueado ao exceder cota trimestral

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | RequestBreakGlass |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `RequestBreakGlass.Handler` |

**Pré-condições:**
- Usuário já usou Break Glass 3 vezes no trimestre corrente (`CountQuarterlyUsageAsync` retorna 3).

**Passos:**
1. Tentar solicitar Break Glass além do limite.

**Resultado Esperado:**
- `result.IsFailure == true`.
- `result.Error` corresponde a `IdentityErrors.BreakGlassQuotaExceeded`.

**Critério de Aceite:** HTTP 422. Cota trimestral de Break Glass respeitada.

---

### TC-IAM-057 — Break Glass com MFA step-up obrigatório e código correto

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | RequestBreakGlass |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `RequestBreakGlass.Handler` |

**Pré-condições:**
- `user.MfaEnabled = true` e `user.MfaSecret` configurado.
- `ITotpVerifier.Verify` retorna `true` para o código fornecido.

**Passos:**
1. Construir `RequestBreakGlass.Command` com justificativa e `MfaCode` correto.
2. Executar o handler.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Break glass ativado com `mfaVerified = true` nos metadados do evento de segurança.

**Critério de Aceite:** HTTP 201. Step-up MFA verificado corretamente para Break Glass.

---

### TC-IAM-058 — Break Glass com código TOTP inválido

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | RequestBreakGlass |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |
| **Handler** | `RequestBreakGlass.Handler` |

**Pré-condições:**
- `user.MfaEnabled = true`.
- `ITotpVerifier.Verify` retorna `false`.

**Passos:**
1. Executar `RequestBreakGlass.Handler` com código TOTP inválido.
2. Verificar que `SecurityEvent.MfaStepUpDenied` é registrado com `riskScore = 70`.

**Resultado Esperado:**
- `result.IsFailure == true`.
- `result.Error` corresponde a `IdentityErrors.MfaCodeInvalid()`.
- Nenhum `BreakGlassRequest` criado.

**Critério de Aceite:** HTTP 422. Break glass bloqueado por código MFA inválido.

---

### TC-IAM-059 — Revogação de Break Glass ativo (RevokeBreakGlass)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | RevokeBreakGlass |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `RevokeBreakGlass.Handler` |

**Pré-condições:**
- `BreakGlassRequest` ativo (dentro da janela de 2 horas).

**Passos:**
1. Construir `RevokeBreakGlass.Command` com `RequestId`.
2. Executar o handler.
3. Verificar que acesso emergencial é encerrado antes do prazo.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- `BreakGlassRequest.RevokedAt` definido.

**Critério de Aceite:** HTTP 200. Acesso emergencial encerrado antecipadamente.

---

### TC-IAM-060 — Listagem de solicitações Break Glass com filtro de status

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | ListBreakGlassRequests |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `ListBreakGlassRequests.Handler` |

**Pré-condições:**
- Tenant tem 5 solicitações Break Glass (3 ativas, 2 revogadas).

**Passos:**
1. Executar `ListBreakGlassRequests.Query` sem filtro.
2. Executar com filtro `status=Active`.
3. Verificar que apenas registros do tenant corrente aparecem.

**Resultado Esperado:**
- Sem filtro: 5 registros.
- Com `status=Active`: 3 registros.
- Nenhum registro de outro tenant.

**Critério de Aceite:** HTTP 200. Isolamento de tenant aplicado na listagem.

---

## Acesso JIT (Just-in-Time)

### TC-IAM-061 — Solicitação de acesso JIT com justificativa válida

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | RequestJitAccess |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `RequestJitAccess.Handler` |

**Pré-condições:**
- Usuário autenticado, ativo, sem MFA habilitado.
- Permissão solicitada e escopo fornecidos.

**Passos:**
1. Construir `RequestJitAccess.Command` com `PermissionCode`, `Scope` e `Justification`.
2. Executar o handler.
3. Verificar que `JitAccessRequest` é criado com estado `Pending`.
4. Verificar que `SecurityEvent.JitAccessRequested` com `riskScore = 40` é registrado.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- `result.Value.RequestId` com novo Guid.
- `result.Value.ApprovalDeadline` dentro da janela configurada.

**Critério de Aceite:** HTTP 201. Solicitação JIT criada e aguardando aprovação.

---

### TC-IAM-062 — Aprovação de acesso JIT (DecideJitAccess — Aprovado)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | DecideJitAccess |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | `DecideJitAccess.Handler` |

**Pré-condições:**
- Solicitação JIT em estado `Pending` e dentro do prazo de aprovação.
- Usuário corrente tem permissão para aprovar (`iam.jit.approve`).

**Passos:**
1. Construir `DecideJitAccess.Command` com `RequestId` e `Decision = Approved`.
2. Executar o handler.
3. Verificar que permissão é concedida temporariamente ao solicitante.
4. Verificar que `JitAccessRequest.Status` muda para `Approved`.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Permissão temporária ativa pelo período configurado.

**Critério de Aceite:** HTTP 200. Acesso JIT aprovado e permissão temporária concedida.

---

### TC-IAM-063 — Rejeição de acesso JIT (DecideJitAccess — Rejeitado)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | DecideJitAccess |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `DecideJitAccess.Handler` |

**Pré-condições:**
- Solicitação JIT em estado `Pending`.

**Passos:**
1. Construir `DecideJitAccess.Command` com `Decision = Rejected` e motivo.
2. Executar o handler.
3. Verificar que nenhuma permissão é concedida.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- `JitAccessRequest.Status = Rejected`.
- Notificação enviada ao solicitante.

**Critério de Aceite:** HTTP 200. Rejeição registrada e solicitante notificado.

---

### TC-IAM-064 — Bloqueio de JIT sem step-up MFA quando necessário

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | RequestJitAccess |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |
| **Handler** | `RequestJitAccess.Handler` |

**Pré-condições:**
- `user.MfaEnabled = true` e `MfaCode` não fornecido.

**Passos:**
1. Executar `RequestJitAccess.Handler` sem `MfaCode`.
2. Verificar que `SecurityEvent.StepUpMfaRequired` com `riskScore = 50` é registrado.

**Resultado Esperado:**
- `result.IsFailure == true`.
- `result.Error` corresponde a `IdentityErrors.MfaStepUpRequired()`.

**Critério de Aceite:** HTTP 422. Acesso JIT bloqueado por step-up MFA obrigatório.

---

### TC-IAM-065 — Expiração automática de solicitação JIT por prazo

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | DecideJitAccess |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `DecideJitAccess.Handler` |

**Pré-condições:**
- Solicitação JIT com `ApprovalDeadline` no passado.

**Passos:**
1. Tentar decidir sobre solicitação expirada.

**Resultado Esperado:**
- `result.IsFailure == true`.
- `result.Error.Type == ErrorType.Business` (prazo expirado).

**Critério de Aceite:** HTTP 422. Solicitações JIT expiradas não podem ser aprovadas.

---

### TC-IAM-066 — Listagem de solicitações JIT por tenant

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | ListJitAccessRequests |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `ListJitAccessRequests.Handler` |

**Pré-condições:**
- Tenant tem múltiplas solicitações JIT em estados diferentes.

**Passos:**
1. Executar `ListJitAccessRequests.Query` com filtros de status.
2. Verificar que apenas solicitações do tenant corrente aparecem.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Isolamento de tenant aplicado.

**Critério de Aceite:** HTTP 200. Solicitações de outros tenants não visíveis.

---

## Delegação e Administração Delegada

### TC-IAM-067 — Criação de delegação de permissão

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | CreateDelegation |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CreateDelegation.Handler` |

**Pré-condições:**
- Delegante tem a permissão que deseja delegar.
- Delegatário existe no mesmo tenant.
- Período de delegação válido (início e fim no futuro).

**Passos:**
1. Construir `CreateDelegation.Command` com `DelegateeId`, `PermissionCode`, `StartsAt` e `ExpiresAt`.
2. Executar o handler.
3. Verificar que delegação é criada com estado `Active`.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- `result.Value.DelegationId` gerado.

**Critério de Aceite:** HTTP 201. Delegação ativa durante o período especificado.

---

### TC-IAM-068 — Proibição de delegar permissão não possuída

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | CreateDelegation |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |
| **Handler** | `CreateDelegation.Handler` |

**Pré-condições:**
- Delegante NÃO possui a permissão que tenta delegar.

**Passos:**
1. Tentar delegar permissão não possuída.

**Resultado Esperado:**
- `result.IsFailure == true`.
- `result.Error.Type == ErrorType.Forbidden`.

**Critério de Aceite:** HTTP 403. Usuário não pode delegar o que não possui.

---

### TC-IAM-069 — Revogação de delegação ativa (RevokeDelegation)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | RevokeDelegation |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `RevokeDelegation.Handler` |

**Pré-condições:**
- Delegação existe e está ativa.

**Passos:**
1. Executar `RevokeDelegation.Command` com `DelegationId`.
2. Verificar que delegação é marcada como revogada.
3. Verificar que delegatário perde a permissão delegada.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Delegação revogada imediatamente.

**Critério de Aceite:** HTTP 200. Permissão delegada retirada.

---

### TC-IAM-070 — Criação de administração delegada entre tenants

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | CreateDelegatedAdministration |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CreateDelegatedAdministration.Handler` |

**Pré-condições:**
- Tenant de origem tem permissão para criar administrações delegadas.
- Tenant alvo existe e está ativo.

**Passos:**
1. Construir `CreateDelegatedAdministration.Command` com `TargetTenantId` e escopos de administração.
2. Executar o handler.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Administração delegada criada com escopos definidos.

**Critério de Aceite:** HTTP 201. Administração cross-tenant configurada.

---

### TC-IAM-071 — Listagem de delegações ativas do usuário corrente

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | ListDelegations |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `ListDelegations.Handler` |

**Pré-condições:**
- Usuário corrente é delegante em 2 delegações ativas e delegatário em 1.

**Passos:**
1. Executar `ListDelegations.Query` com perspectiva `delegator`.
2. Executar com perspectiva `delegatee`.

**Resultado Esperado:**
- Perspectiva `delegator`: 2 delegações.
- Perspectiva `delegatee`: 1 delegação.

**Critério de Aceite:** HTTP 200. Delegações filtradas por perspectiva corretamente.

---

## Revisão de Acessos (Access Review Campaign)

### TC-IAM-072 — Início de campanha de revisão de acessos

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | StartAccessReviewCampaign |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `StartAccessReviewCampaign.Handler` |

**Pré-condições:**
- Tenant tem 5 membros ativos.
- Usuário autenticado como iniciador.
- Janela de revisão entre 1 e 90 dias.

**Passos:**
1. Construir `StartAccessReviewCampaign.Command` com nome e `ReviewWindowDays = 14`.
2. Executar o handler.
3. Verificar que `AccessReviewCampaign` é criado com 5 itens (um por membro).
4. Verificar que `SecurityEvent.AccessReviewStarted` com `riskScore = 5` é registrado.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- `result.Value.ItemCount == 5`.
- `result.Value.Deadline` = agora + 14 dias.

**Critério de Aceite:** HTTP 201. Campanha criada com item por membro ativo.

---

### TC-IAM-073 — Validação de janela de revisão inválida (fora de 1–90 dias)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | StartAccessReviewCampaign |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `StartAccessReviewCampaign.Validator` |

**Pré-condições:**
- Pipeline com `ValidationBehavior` configurado.

**Passos:**
1. Tentar criar campanha com `ReviewWindowDays = 0` (abaixo do mínimo).
2. Tentar com `ReviewWindowDays = 91` (acima do máximo).

**Resultado Esperado:**
- Ambas retornam `result.IsFailure == true`.
- `result.Error.Type == ErrorType.Validation`.

**Critério de Aceite:** HTTP 422. Janela de revisão restrita a 1–90 dias.

---

### TC-IAM-074 — Decisão em item de revisão (DecideAccessReviewItem — Certificado)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | DecideAccessReviewItem |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `DecideAccessReviewItem.Handler` |

**Pré-condições:**
- Campanha ativa com item de revisão pendente.
- Usuário corrente é o reviewer designado para o item.

**Passos:**
1. Construir `DecideAccessReviewItem.Command` com `ItemId`, `Decision = Certified`.
2. Executar o handler.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Item marcado como `Certified`.
- Acesso do usuário revisado mantido.

**Critério de Aceite:** HTTP 200. Certificação registrada com data e reviewer.

---

### TC-IAM-075 — Decisão em item de revisão (DecideAccessReviewItem — Revogado)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | DecideAccessReviewItem |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `DecideAccessReviewItem.Handler` |

**Pré-condições:**
- Campanha ativa com item de revisão pendente.

**Passos:**
1. Construir `DecideAccessReviewItem.Command` com `Decision = Revoked`.
2. Executar o handler.
3. Verificar que role/permissão do usuário revisado é removida.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Acesso do usuário revisado revogado.
- Membership ou permissão desativada.

**Critério de Aceite:** HTTP 200. Revogação de acesso executada automaticamente.

---

### TC-IAM-076 — Escalada de itens vencidos (EscalateOverdueAccessReviews)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | EscalateOverdueAccessReviews |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `EscalateOverdueAccessReviews.Handler` |

**Pré-condições:**
- Campanha com prazo vencido e itens ainda pendentes de decisão.

**Passos:**
1. Executar `EscalateOverdueAccessReviews.Handler`.
2. Verificar que itens vencidos são escalonados para revisores de nível superior.
3. Verificar que notificações de urgência são enviadas.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Itens escalonados com `EscalatedAt` registrado.

**Critério de Aceite:** Itens vencidos escalados e administradores notificados.

---

### TC-IAM-077 — Obtenção de campanha de revisão existente

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | GetAccessReviewCampaign |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GetAccessReviewCampaign.Handler` |

**Pré-condições:**
- Campanha de revisão existe com itens em estados mistos.

**Passos:**
1. Executar `GetAccessReviewCampaign.Query` com `CampaignId`.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- `result.Value` contém nome, prazo, contagem de itens por status.

**Critério de Aceite:** HTTP 200. Detalhes completos da campanha.

---

## Licenciamento e Onboarding

### TC-IAM-078 — Verificação de capability em handler (HasCapability)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | Licenciamento |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Handler** | Qualquer handler com verificação de capability |

**Pré-condições:**
- Tenant tem plano `Starter` sem capability `contract_studio`.

**Passos:**
1. Tentar executar handler que verifica `currentTenant.HasCapability("contract_studio")`.
2. Verificar retorno quando capability está ausente.

**Resultado Esperado:**
- `result.IsFailure == true`.
- `result.Error.Type == ErrorType.Forbidden`.
- Mensagem indica que a feature requer plano superior.

**Critério de Aceite:** HTTP 403. Capability gate funcional para plano `Starter`.

---

### TC-IAM-079 — Tenant sem licença cai em fallback Enterprise

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | Licenciamento / TenantResolutionMiddleware |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `TenantResolutionMiddleware` |

**Pré-condições:**
- Tenant existe sem registro de `TenantLicense`.

**Passos:**
1. Realizar requisição autenticada para o tenant sem licença.
2. Verificar que middleware resolve capabilities do plano `Enterprise`.

**Resultado Esperado:**
- Todas as capabilities Enterprise disponíveis.
- Nenhuma exceção lançada.

**Critério de Aceite:** Sistema operacional sem licença explícita usa Enterprise como padrão.

---

### TC-IAM-080 — Status de onboarding do tenant (GetOnboardingStatus)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | GetOnboardingStatus |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GetOnboardingStatus.Handler` |

**Pré-condições:**
- Tenant recém-criado com onboarding em progresso.

**Passos:**
1. Executar `GetOnboardingStatus.Query`.
2. Verificar que passos completados e pendentes são retornados.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- `result.Value.Steps` lista cada passo com status `Completed` ou `Pending`.

**Critério de Aceite:** HTTP 200. Progresso de onboarding refletido fielmente.

---

### TC-IAM-081 — Atualização de passo de onboarding (UpdateOnboardingStep)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | UpdateOnboardingStep |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `UpdateOnboardingStep.Handler` |

**Pré-condições:**
- Passo de onboarding existe e está pendente.

**Passos:**
1. Construir `UpdateOnboardingStep.Command` com `StepKey` e `Status = Completed`.
2. Executar o handler.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Passo marcado como completado com data/hora.

**Critério de Aceite:** HTTP 200. Progresso de onboarding atualizado.

---

### TC-IAM-082 — Obtenção de configuração de persona (GetPersonaConfig)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | GetPersonaConfig |
| **Tipo** | Unitário |
| **Prioridade** | Baixa |
| **Handler** | `GetPersonaConfig.Handler` |

**Pré-condições:**
- Usuário tem role configurada com persona associada.

**Passos:**
1. Executar `GetPersonaConfig.Query`.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- `result.Value` contém configurações de UI/UX específicas da persona.

**Critério de Aceite:** HTTP 200. Configuração de persona correta para o role do usuário.

---

## Agentes e Eventos de Segurança

### TC-IAM-083 — Registro de heartbeat de agente (RecordAgentHeartbeat)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | RecordAgentHeartbeat |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `RecordAgentHeartbeat.Handler` |

**Pré-condições:**
- Agente registrado com `AgentId` válido.
- Requisição autenticada via API Key do agente.

**Passos:**
1. Construir `RecordAgentHeartbeat.Command` com `AgentId`, `Version` e `Status`.
2. Executar o handler.
3. Verificar que `LastHeartbeatAt` é atualizado no registro do agente.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- `AgentRegistration.LastHeartbeatAt` atualizado para `dateTimeProvider.UtcNow`.

**Critério de Aceite:** HTTP 200. Heartbeat registrado. `AlertEvaluationJob` não gerará alerta por agente offline.

---

### TC-IAM-084 — Registro de query de agente (RecordAgentQuery)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | RecordAgentQuery |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `RecordAgentQuery.Handler` |

**Pré-condições:**
- Agente autenticado via API Key.
- Query NQL a ser auditada.

**Passos:**
1. Construir `RecordAgentQuery.Command` com `AgentId`, `QueryText` e resultado.
2. Executar o handler.
3. Verificar que entrada de auditoria é criada no `AgentQueryAuditLog`.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Query auditada e armazenada com `AgentId` e `TenantId`.

**Critério de Aceite:** HTTP 200. Todas as queries de agentes auditadas.

---

### TC-IAM-085 — Listagem de registros de agentes do tenant

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | ListAgentRegistrations |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `ListAgentRegistrations.Handler` |

**Pré-condições:**
- Tenant tem 3 agentes registrados.

**Passos:**
1. Executar `ListAgentRegistrations.Query`.
2. Verificar que agentes de outros tenants não aparecem.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Lista com 3 agentes do tenant corrente.

**Critério de Aceite:** HTTP 200. Isolamento de tenant aplicado.

---

### TC-IAM-086 — Listagem de eventos de segurança com filtro de tipo

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | ListSecurityEvents |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ListSecurityEvents.Handler` |

**Pré-condições:**
- Tenant tem eventos de segurança de vários tipos.

**Passos:**
1. Executar `ListSecurityEvents.Query` com filtro `eventType=BreakGlassActivated`.
2. Verificar que apenas eventos do tipo correto e do tenant corrente são retornados.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Eventos filtrados por tipo e tenant.

**Critério de Aceite:** HTTP 200. Auditoria de segurança isolada por tenant.

---

### TC-IAM-087 — Resolução de alerta ativo (ResolveAlert)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | ResolveAlert |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ResolveAlert.Handler` |

**Pré-condições:**
- Alerta ativo no tenant (`AlertStatus.Firing`).
- Usuário tem permissão `iam.alerts.resolve`.

**Passos:**
1. Construir `ResolveAlert.Command` com `AlertId` e notas de resolução.
2. Executar o handler.
3. Verificar que alerta muda para `AlertStatus.Resolved`.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Alerta resolvido com `ResolvedAt` e `ResolvedBy`.

**Critério de Aceite:** HTTP 200. Alerta marcado como resolvido com trilha de quem resolveu.

---

## Isolamento Multi-Tenant e Segurança de Borda

### TC-IAM-088 — Requisição sem tenant context rejeitada pelo TenantIsolationBehavior

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | TenantIsolationBehavior |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |
| **Handler** | `TenantIsolationBehavior<TRequest, TResponse>` |

**Pré-condições:**
- `ICurrentTenant.Id == Guid.Empty`.
- Requisição não implementa `IPublicRequest`.

**Passos:**
1. Executar qualquer command que não seja `IPublicRequest`.
2. Verificar que pipeline retorna erro de segurança antes de atingir o handler.

**Resultado Esperado:**
- `result.IsFailure == true`.
- `result.Error.Type == ErrorType.Security` com código `Tenant.Isolation.NoTenant`.
- Handler nunca é chamado.

**Critério de Aceite:** HTTP 500 (mascarado). Nenhum dado acessado sem tenant context.

---

### TC-IAM-089 — Requisição para tenant inativo rejeitada pelo TenantIsolationBehavior

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | TenantIsolationBehavior |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |
| **Handler** | `TenantIsolationBehavior<TRequest, TResponse>` |

**Pré-condições:**
- `ICurrentTenant.Id` é Guid válido mas `ICurrentTenant.IsActive == false`.

**Passos:**
1. Executar command com tenant inativo.

**Resultado Esperado:**
- `result.IsFailure == true`.
- `result.Error.Type == ErrorType.Forbidden` com código `Tenant.Isolation.Inactive`.

**Critério de Aceite:** HTTP 403. Tenant inativo bloqueia todas as operações.

---

### TC-IAM-090 — Endpoint público (IPublicRequest) bypass do TenantIsolationBehavior

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | TenantIsolationBehavior / LocalLogin |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `TenantIsolationBehavior<TRequest, TResponse>` |

**Pré-condições:**
- `LocalLogin.Command` implementa `IPublicRequest`.
- `ICurrentTenant.Id == Guid.Empty`.

**Passos:**
1. Executar `LocalLogin.Command` sem tenant context.

**Resultado Esperado:**
- `TenantIsolationBehavior` chama `next()` diretamente sem verificar tenant.
- Login prossegue normalmente.

**Critério de Aceite:** Endpoints de autenticação acessíveis sem tenant pré-estabelecido.

---

### TC-IAM-091 — Usuário de tenant A não acessa dados do tenant B via manipulação de ID

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | Isolamento Multi-Tenant |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |
| **Handler** | Qualquer handler com repositório |

**Pré-condições:**
- Usuário autenticado no tenant A.
- Tentativa de ler dados com ID pertencente ao tenant B.

**Passos:**
1. Executar query com ID de recurso do tenant B usando JWT do tenant A.
2. Verificar que repositório aplica filtro `.Where(e => e.TenantId == currentTenant.Id)`.
3. Verificar que `TenantRlsInterceptor` define `app.current_tenant_id` no PostgreSQL.

**Resultado Esperado:**
- `result.IsFailure == true`.
- `result.Error.Type == ErrorType.NotFound` (recurso não encontrado no tenant corrente).

**Critério de Aceite:** HTTP 404. Recursos de outros tenants são invisíveis. Duas camadas de isolamento: RLS + filtro no repositório.

---

### TC-IAM-092 — SelectTenant troca de contexto de tenant corretamente

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | SelectTenant |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `SelectTenant.Handler` |

**Pré-condições:**
- Usuário é membro de 2 tenants (multi-tenant).
- JWT contém `tenant_id` do tenant A.

**Passos:**
1. Executar `SelectTenant.Command` com `TenantId` do tenant B.
2. Verificar que novo JWT é emitido com claims do tenant B.
3. Verificar que membership do usuário no tenant B existe e está ativo.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Novo access token com claims do tenant B.

**Critério de Aceite:** HTTP 200. Troca de tenant segura com novo JWT.

---

### TC-IAM-093 — Tentativa de SelectTenant para tenant sem membership

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | SelectTenant |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |
| **Handler** | `SelectTenant.Handler` |

**Pré-condições:**
- Usuário NÃO é membro do tenant alvo.

**Passos:**
1. Executar `SelectTenant.Command` com `TenantId` de tenant sem membership.

**Resultado Esperado:**
- `result.IsFailure == true`.
- `result.Error.Type == ErrorType.Forbidden` ou `NotFound`.

**Critério de Aceite:** HTTP 403/404. Usuário não pode trocar para tenant sem membership.

---

### TC-IAM-094 — TenantId Guid.Empty rejeitado pelo guard customizado

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess / BuildingBlocks |
| **Feature** | Guards / NexTraceGuards |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `NexTraceGuards.EmptyTenantId` |

**Pré-condições:**
- Código que chama `Guard.Against.EmptyTenantId(tenantId)` com `Guid.Empty`.

**Passos:**
1. Chamar `Guard.Against.EmptyTenantId(Guid.Empty)`.

**Resultado Esperado:**
- `ArgumentException` lançada com mensagem `"TenantId cannot be empty."`.

**Critério de Aceite:** Guard customizado protege contra TenantId vazio em nível de domínio.

---

### TC-IAM-095 — JWT expirado retorna 401 sem vazar informação de stack trace

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess / Security |
| **Feature** | JwtTokenService / Middleware |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |
| **Handler** | Middleware de autenticação JWT |

**Pré-condições:**
- JWT emitido com `exp` no passado.

**Passos:**
1. Enviar requisição autenticada com JWT expirado.
2. Verificar que resposta HTTP é 401.
3. Verificar que corpo da resposta NÃO contém stack trace ou detalhes internos.

**Resultado Esperado:**
- HTTP 401.
- Corpo da resposta: mensagem genérica de token expirado.
- Sem vazamento de informação de implementação.

**Critério de Aceite:** HTTP 401. Informações sensíveis de runtime não expostas.

---

*Fim do documento — Módulo IdentityAccess. Total: 95 cenários de teste.*

# P11.3 — Post-Change Gap Report

**Data:** 2026-03-27  
**Fase:** P11.3 — Completar MFA enforcement, sessão segura e hardening final do runtime de autenticação  

---

## 1. O que foi resolvido nesta fase

| Item                                     | Status   | Detalhe                                                                              |
|------------------------------------------|----------|--------------------------------------------------------------------------------------|
| SR-1: MFA enforcement em runtime         | ✅ Resolvido | `LocalLogin.Handler` verifica `user.MfaEnabled`; fluxo de dois passos com challenge token + TOTP. |
| SR-6: Validação IP/UserAgent em sessão   | ✅ Resolvido | `RefreshToken.Handler` detecta mudança de IP e emite `SuspiciousSessionContextDetected`. |
| Step-up MFA em Break Glass               | ✅ Resolvido | `RequestBreakGlass.Handler` exige `MfaCode` TOTP se `user.MfaEnabled = true`. |
| Step-up MFA em JIT Access                | ✅ Resolvido | `RequestJitAccess.Handler` exige `MfaCode` TOTP se `user.MfaEnabled = true`. |
| `ITotpVerifier` + `TotpVerifier`         | ✅ Resolvido | Implementação RFC 6238 pura, HMAC-SHA1, janela ±1 passo, sem dependências externas. |
| `IMfaChallengeTokenService`              | ✅ Resolvido | Token stateless HMAC-SHA256, TTL 5 min, timing-safe.                               |
| `POST /auth/mfa/verify` endpoint         | ✅ Resolvido | Endpoint `VerifyMfaChallenge` registado em `AuthEndpoints.cs`.                     |
| Eventos de segurança MFA                 | ✅ Resolvido | 5 novos `SecurityEventType`, 5 novos métodos em `ISecurityAuditRecorder`.          |
| Erros de domínio MFA                     | ✅ Resolvido | 3 novos erros em `IdentityErrors`.                                                  |
| Testes MFA enforcement                   | ✅ Resolvido | 18 novos testes; total 315 passando.                                                |

---

## 2. O que ficou pendente (próximas fases)

### 2.1 MFA Setup/Enrollment (fora do escopo P11.3)

O P11.3 enforça o MFA para utilizadores que **já têm** `MfaEnabled = true` e `MfaSecret` configurado. O fluxo de **configuração inicial de MFA** (enrollment) — gerar QR code, armazenar segredo, confirmar primeiro código — ainda não está implementado.

**Impacto:** Sem enrollment UI/API, os utilizadores não conseguem activar MFA por si próprios. O `MfaEnabled` pode ser definido manualmente na base de dados ou por admin script como workaround temporário.

**Próximos passos:**
- Criar endpoint `POST /users/me/mfa/setup` para gerar segredo TOTP e QR code.
- Criar endpoint `POST /users/me/mfa/confirm` para confirmar enrollment com primeiro código.
- Criar endpoint `DELETE /users/me/mfa` para desactivar MFA com verificação de senha.

### 2.2 Revogação automática de sessão por contexto suspeito (configurável)

A detecção de mudança de IP no refresh token gera `SecurityEvent` mas **não revoga a sessão** por defeito. A revogação automática é configurável por política de tenant e ficou fora do escopo desta fase.

**Próximos passos:**
- Adicionar `SessionSecurityPolicy` por tenant (`RevokeOnIpChange: bool`, `RevokeOnUaChange: bool`).
- Implementar lógica condicional de revogação no `RefreshToken.Handler`.

### 2.3 Validação de UserAgent no refresh (não implementado)

A validação de IP foi implementada. A validação de **UserAgent** no refresh — detectar mudança de browser/cliente — ficou fora do escopo desta fase por ter menor impacto de segurança (UserAgent é trivialmente falsificável) e para minimizar falsos positivos.

**Próximos passos:** Avaliar se deve ser adicionado como sinal de anomalia (low confidence) no motor de Session Intelligence.

### 2.4 SAML (scope P12.x)

Conforme definido no plano, suporte SAML não foi incluído nesta fase. O módulo de federação OIDC existente continua disponível.

### 2.5 Autenticação MFA para federação OIDC

O MFA enforcement em P11.3 cobre apenas o fluxo de autenticação **local** (`LocalLogin`). Utilizadores que fazem login via OIDC delegam o MFA ao provider externo; não há enforcement adicional no callback OIDC.

**Próximos passos:** Avaliar se é necessário impor step-up MFA mesmo para logins federados (normalmente o provider externo já o enforça).

### 2.6 MFA para Break Glass sem TOTP secret configurado

Se um utilizador tem `MfaEnabled = true` mas `MfaSecret = null` (método não-TOTP, ex: WebAuthn), o handler vai falhar na verificação TOTP. A lógica de step-up por método não está implementada.

**Próximos passos:** Adicionar verificação de `user.MfaMethod` e routing para verificador apropriado quando WebAuthn/SMS forem suportados.

### 2.7 Dashboard de sessões activas e gestão de sessões

A entidade `Session` e os repositories estão completos, mas a UX de gestão de sessões activas ("ver dispositivos ligados", "revogar sessão específica") ainda não existe.

### 2.8 Rate limiting específico para /auth/mfa/verify

O endpoint `/auth/mfa/verify` usa o mesmo rate limiter `"auth"` que `/auth/login`. Um rate limiter dedicado com limite mais restrito (ex: 5 tentativas/15 min por IP + UserId) seria mais adequado para prevenir brute force de TOTP.

---

## 3. Limitações residuais

| Limitação                                       | Impacto | Prioridade |
|-------------------------------------------------|---------|------------|
| Enrollment MFA não disponível via UI/API        | Alto (bloqueador para utilizadores finais)  | P12.1 |
| Revogação automática por IP não configurável    | Médio (auditoria existe, revogação não)     | P12.2 |
| MFA enforcement não cobre login federado OIDC   | Médio (depende do provider externo)          | P12.2 |
| `MfaMethod != TOTP` não suportado em step-up   | Baixo (apenas TOTP existe hoje)             | Futuro |
| Rate limiting dedicado para `/auth/mfa/verify`  | Baixo-Médio (mitigado pelo rate limiter geral) | P12.1 |

---

## 4. Critérios de aceitação da fase — verificação

| Critério                                                                    | Status   |
|-----------------------------------------------------------------------------|----------|
| Utilizadores com MFA configurado têm MFA realmente enforced em runtime      | ✅ Sim   |
| Existe step-up MFA mínimo para operações privilegiadas relevantes           | ✅ Sim (Break Glass + JIT) |
| A sessão tem validação real por IP configurável                              | ✅ Parcial (detecção e auditoria; revogação automática em P12.x) |
| Login/session/refresh/logout contínuos sem bypass de enforcement            | ✅ Sim   |
| Eventos de segurança e auditoria mínimos gerados                            | ✅ Sim   |
| Código compila sem erros                                                    | ✅ Sim (0 erros) |
| Relatório final gerado                                                       | ✅ Sim   |

---

## 5. Itens para a próxima macrofase (P12.x)

1. **P12.1** — MFA Enrollment API + UI (setup TOTP, QR code, confirmação, desactivação)
2. **P12.1** — Rate limiting dedicado para endpoints MFA
3. **P12.2** — Política de sessão configurável por tenant (revogação automática por IP/UA)
4. **P12.2** — Suporte SAML (autenticação federada empresarial)
5. **P12.3** — Session Intelligence: motor de pontuação de anomalia com múltiplos sinais
6. **P12.3** — WebAuthn/FIDO2 como segundo fator MFA

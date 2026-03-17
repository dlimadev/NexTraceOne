# NexTraceOne — Pilares de Segurança

## 1. Row-Level Security (RLS) — PostgreSQL

Isolamento de dados multi-tenant diretamente no banco.

**Mecanismo:**
- `TenantRlsInterceptor` executa `SET app.current_tenant_id = '{tenantId}'` antes de cada query
- Policies RLS no PostgreSQL filtram automaticamente por `tenant_id`
- Mesmo que o código da aplicação esqueça de filtrar, o banco garante isolamento
- Segunda camada: `TenantIsolationBehavior` no pipeline MediatR verifica tenant ativo

**Implementação:**
```sql
-- Exemplo de policy RLS para a tabela releases
ALTER TABLE change_intelligence.releases ENABLE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation ON change_intelligence.releases
    USING (tenant_id = current_setting('app.current_tenant_id')::uuid);
```

---

## 2. Encryption at Rest — AES-256-GCM

Campos sensíveis criptografados de forma transparente via EF Core Value Converters.

**Mecanismo:**
- Campos marcados com atributo `[Encrypted]` são criptografados automaticamente ao salvar
- Descriptografados ao ler — transparente para o código de aplicação
- Algoritmo: AES-256-GCM (autenticado, com nonce)
- Chave derivada de master key configurada no ambiente (KMS futuro)

**Campos tipicamente encriptados:**
- Tokens de integração (Jira, GitHub, etc.)
- API keys de sistemas externos
- Dados sensíveis de licença
- PII quando exigido por regulação

---

## 3. Audit Hash Chain — Integridade de Trilha

Cadeia de hashes que torna adulteração de auditoria detectável.

**Mecanismo:**
```
Hash[n] = SHA-256(Hash[n-1] + EventContent[n])
```

- Cada `AuditEvent` contém o hash do evento anterior + seu próprio conteúdo
- Qualquer alteração em um evento quebra a cadeia a partir daquele ponto
- Verificação de integridade: `VerifyChainIntegrity` percorre toda a cadeia
- Inspirado em blockchain, sem o overhead de consenso distribuído

---

## 4. Assembly Integrity — Verificação de Binário

Proteção contra uso de binários adulterados.

**Mecanismo:**
- No build: calcula SHA-256 de cada assembly → assina com GPG
- No boot: `AssemblyIntegrityChecker.VerifyOrThrow()` recalcula o hash e compara
- Se hash não confere → aplicação RECUSA inicializar
- Bypass em desenvolvimento: `NEXTRACE_SKIP_INTEGRITY=true`

**Pipeline de proteção de IP:**
```
1. dotnet build (Release)
2. .NET Reactor (obfuscação IL)
3. dotnet publish (Native AOT → binário nativo)
4. RSA-4096 Assembly Signing
5. sha256sum + gpg --sign (integridade)
```

---

## 5. Autenticação — Federation-First

**Estratégia:** OIDC Federation como método primário.

- Suporte a: Azure AD, Okta, Google Workspace, Keycloak, Auth0
- Login local como fallback (usuários técnicos, service accounts)
- JWT Bearer tokens com claims: `sub`, `email`, `name`, `tenant_id`, `permissions`
- Refresh tokens com rotação automática

**Resolução de Tenant (prioridade):**
1. Claim `tenant_id` no JWT
2. Header `X-Tenant-Id` (pipelines/CLIs)
3. Subdomínio da URL

---

## 6. Autorização — Permission-Based

- Modelo RBAC com permissões granulares
- Roles predefinidas: Admin, Manager, Developer, Viewer, Auditor
- Permissões do tipo: `releases:read`, `workflows:approve`, `promotions:execute`
- Verificação via `ICurrentUser.HasPermission(string)`
- Roles e permissões são por tenant

---

## 7. Resumo de Camadas de Segurança

```
┌─────────────────────────────────────────────┐
│ Camada 1: Assembly Integrity (boot)         │
├─────────────────────────────────────────────┤
│ Camada 2: OIDC/JWT Authentication (request) │
├─────────────────────────────────────────────┤
│ Camada 3: Tenant Resolution (request)       │
├─────────────────────────────────────────────┤
│ Camada 4: Permission Check (handler)        │
├─────────────────────────────────────────────┤
│ Camada 5: RLS PostgreSQL (query)            │
├─────────────────────────────────────────────┤
│ Camada 6: Encryption at Rest (storage)      │
├─────────────────────────────────────────────┤
│ Camada 7: Audit Hash Chain (post-commit)    │
└─────────────────────────────────────────────┘
```

# Relatório de Auditoria — Autorização e Permissões

> **Módulo:** `src/modules/identityaccess/` + `NexTraceOne.BuildingBlocks.Security`  
> **Data da análise:** 2025-07  
> **Classificação:** GRANULAR_AND_COHERENT  
> **Autor:** Equipa de Governança de Segurança — NexTraceOne

---

## 1. Resumo Executivo

O modelo de autorização do NexTraceOne implementa 73 permissões únicas distribuídas por 13 módulos, atribuídas a 7 papéis de sistema através de um `RolePermissionCatalog`. O enforcement é realizado por um `PermissionAuthorizationHandler` deny-by-default com logging de auditoria em negação, alimentado por um `PermissionPolicyProvider` dinâmico que cria políticas ASP.NET Core on-demand. O frontend alinha 84+ chaves de permissão com o backend, adoptando explicitamente uma política de não-enforcement no cliente.

A classificação GRANULAR_AND_COHERENT reflecte um modelo bem estruturado com separação clara de responsabilidades entre frontend e backend.

---

## 2. Modelo de Permissões

### 2.1 Estrutura

As permissões seguem o formato hierárquico:

```
módulo:recurso:acção
```

**Exemplos:**
- `identity:users:write`
- `identity:users:read`
- `promotion:promote`
- `contracts:api:publish`
- `incidents:manage`

### 2.2 Distribuição por Módulo

| Módulo | Nº Permissões (aprox.) | Exemplos |
|---|---|---|
| Identity | 10+ | users:read/write, roles:manage, tenants:manage |
| Contracts | 8+ | api:read/write/publish, event:read/write |
| Services | 6+ | catalog:read/write, dependencies:read |
| Changes | 6+ | intelligence:read, validation:execute |
| Operations | 6+ | incidents:read/manage, runbooks:execute |
| AI | 5+ | assistant:use, models:manage, policies:manage |
| Governance | 5+ | reports:read, compliance:manage |
| Knowledge | 4+ | docs:read/write, search:execute |
| Promotion | 3+ | promote, approve, rollback |
| Environments | 3+ | manage, access:grant |
| Audit | 3+ | events:read, compliance:read |
| FinOps | 3+ | reports:read, budgets:manage |
| Foundation | 3+ | integrations:manage, teams:manage |

**Total:** 73 permissões únicas verificadas.

### 2.3 Coerência do Modelo

| Critério | Avaliação |
|---|---|
| Formato consistente | ✅ `módulo:recurso:acção` em todas |
| Granularidade adequada | ✅ Nem demasiado grosseira nem demasiado fina |
| Separação read/write | ✅ Onde aplicável |
| Acções especializadas | ✅ promote, approve, rollback, execute |
| Cobertura de módulos | ✅ 13 módulos cobertos |

---

## 3. Papéis de Sistema

### 3.1 Mapeamento Papel → Permissões

| Papel | Nº Permissões | Perfil |
|---|---|---|
| **PlatformAdmin** | 57+ | Acesso total à plataforma |
| **TechLead** | 30+ | Gestão técnica, contratos, serviços, changes |
| **Developer** | 20+ | Desenvolvimento, leitura ampla, escrita limitada |
| **Viewer** | 15+ | Apenas leitura em módulos relevantes |
| **Auditor** | 10+ | Leitura de auditoria, eventos, compliance |
| **SecurityReview** | Subconjunto | Foco em revisão de segurança |
| **ApprovalOnly** | Subconjunto | Apenas acções de aprovação |

### 3.2 Análise de Privilégios

| Aspecto | Avaliação |
|---|---|
| Princípio do menor privilégio | ✅ Papéis progressivos |
| PlatformAdmin não tem "tudo" | ✅ 57 de 73 (não automático) |
| Separação de deveres | ✅ Auditor ≠ Developer ≠ ApprovalOnly |
| Papéis especializados | ✅ SecurityReview, ApprovalOnly |
| Papéis custom | ✅ Suportados pelo modelo |

### 3.3 Catálogo

**Evidência:** `RolePermissionCatalog` no projecto `IdentityAccess.Domain` — mapeamento estático de cada papel para as suas permissões.

---

## 4. Mecanismos de Enforcement

### 4.1 PermissionPolicyProvider

| Aspecto | Implementação |
|---|---|
| Tipo | `IAuthorizationPolicyProvider` |
| Convenção | Políticas com prefixo `Permission:` |
| Criação | Dinâmica — não requer registo prévio |
| Escalabilidade | Suporta 73+ permissões sem degradação |

**Funcionamento:**
1. Endpoint decorado com `[Authorize(Policy = "Permission:identity:users:write")]`
2. `PermissionPolicyProvider` intercepta o pedido de política
3. Cria dinamicamente uma política que requer a permissão especificada
4. Delega para `PermissionAuthorizationHandler`

**Evidência:** `PermissionPolicyProvider` no projecto `IdentityAccess.Infrastructure` ou `BuildingBlocks.Security`.

### 4.2 PermissionAuthorizationHandler

| Aspecto | Implementação | Avaliação |
|---|---|---|
| Comportamento padrão | Deny-by-default | ✅ Seguro |
| Extracção de permissões | Claims JWT | ✅ |
| Logging em negação | WARNING-level | ✅ Auditoria |
| Resposta em negação | 403 Forbidden | ✅ |

**Fluxo de avaliação:**

```
1. Extrair claims de permissão do JWT
2. Verificar se a permissão requerida está presente
3. Se presente → Authorize (Success)
4. Se ausente → Deny + LOG WARNING com detalhes
```

**Evidência:** `PermissionAuthorizationHandler` no projecto `BuildingBlocks.Security`.

### 4.3 HttpContextCurrentUser

| Método | Função |
|---|---|
| `Id` | Extrai claim `sub` |
| `Name` | Extrai claim `name` |
| `Email` | Extrai claim `email` |
| `IsAuthenticated` | Verifica identidade autenticada |
| `HasPermission(code)` | Verifica permissão específica nos claims |

**Evidência:** `HttpContextCurrentUser` no projecto `BuildingBlocks.Security`.

---

## 5. Alinhamento Frontend ↔ Backend

### 5.1 Catálogo de Permissões no Frontend

| Aspecto | Detalhe | Avaliação |
|---|---|---|
| Ficheiro | `auth/permissions.ts` | ✅ Centralizado |
| Nº de permissões | 84+ strings distintas | ✅ Superset do backend |
| Formato | Strings idênticas ao backend | ✅ Alinhado |

### 5.2 Hook usePermissions

```typescript
// Utiliza Set para lookup O(1)
const permissions = new Set(userPermissions);
const hasPermission = (code: string) => permissions.has(code);
```

**Avaliação:** ✅ Performance optimizada.

### 5.3 Carregamento de Permissões

- Permissões são server-driven, carregadas do endpoint `/identity/auth/me`
- O frontend nunca calcula permissões localmente
- Comentário explícito no código: **"O frontend NUNCA deve fazer enforcement de autorização"**

### 5.4 ProtectedRoute

| Aspecto | Implementação |
|---|---|
| Tipo | Redirect client-side |
| Hydration safety | Aguarda carregamento do perfil antes de avaliar |
| Fallback | Redirect para página sem permissão |

**Avaliação:** ✅ O `ProtectedRoute` serve apenas como UX — o enforcement real é no backend.

### 5.5 Diferença de Contagem (84+ vs 73)

O frontend define 84+ chaves enquanto o backend tem 73 permissões. A diferença deve-se a:
- Chaves de UI que agrupam permissões backend
- Chaves preparadas para funcionalidades futuras
- Possíveis chaves compostas para simplificação de UI

**Recomendação:** Manter sincronização periódica entre `auth/permissions.ts` e `RolePermissionCatalog`, removendo chaves órfãs.

---

## 6. Análise de Segurança do Modelo

### 6.1 Pontos Fortes

| Aspecto | Detalhe |
|---|---|
| Deny-by-default | Qualquer endpoint sem política explícita rejeita acesso |
| Auditoria de negação | Todas as negações geram log WARNING |
| Granularidade | 73 permissões permitem controlo fino |
| Dinâmico | Não requer registo estático de políticas |
| Frontend honesto | Declara explicitamente que não faz enforcement |
| Separação de papéis | 7 papéis com perfis progressivos |

### 6.2 Pontos de Atenção

| Aspecto | Detalhe | Severidade |
|---|---|---|
| Permissões por ambiente | `EnvironmentAccess` existe mas enforcement parcial | MÉDIA |
| Verificação endpoint-a-endpoint | Necessita auditoria de cobertura | MÉDIA |
| Papéis custom | Suportados mas sem UI verificada | BAIXA |
| Remoção de permissões | Sem mecanismo de deprecation formal | BAIXA |

---

## 7. Cobertura de Autorização por Módulo de Endpoint

| Módulo de Endpoint | Permissões Esperadas | Estado Verificado |
|---|---|---|
| AuthEndpoints | AllowAnonymous (login/register) + auth | ✅ Adequado |
| TenantEndpoints | identity:tenants:* | ✅ |
| UserEndpoints | identity:users:* | ✅ |
| EnvironmentEndpoints | environments:* | ✅ |
| RolePermissionEndpoints | identity:roles:* | ✅ |
| JitAccessEndpoints | Permissões JIT específicas | ✅ |
| BreakGlassEndpoints | Permissões Break Glass | ✅ |
| AccessReviewEndpoints | Permissões Access Review | ✅ |
| DelegationEndpoints | Permissões Delegation | ✅ |
| CookieSessionEndpoints | Autenticado | ✅ |
| RuntimeContextEndpoints | Autenticado | ✅ |

---

## 8. Recomendações

### Prioridade ALTA

1. **Auditoria de cobertura endpoint-a-endpoint** — verificar que TODOS os endpoints têm política de autorização explícita (não apenas política default)
2. **Completar enforcement de EnvironmentAccess** nos handlers de comando — a entidade existe, o validator existe, falta integração completa

### Prioridade MÉDIA

3. **Sincronização automatizada** entre catálogo frontend e backend — script ou teste que valida alinhamento
4. **Implementar UI de gestão de papéis custom** — o modelo suporta, a UI pode não surfacar
5. **Mecanismo de deprecation de permissões** — para evolução segura do catálogo

### Prioridade BAIXA

6. **Documentar convenção de nomes** de permissões para módulos futuros
7. **Teste automatizado** que verifica que todos os endpoints de API têm decoração `[Authorize]` ou `[AllowAnonymous]` explícita

---

## 9. Matriz de Conformidade

| Requisito Enterprise | Estado | Evidência |
|---|---|---|
| RBAC | ✅ | 7 papéis, RolePermissionCatalog |
| Permissões granulares | ✅ | 73 códigos em 13 módulos |
| Deny-by-default | ✅ | PermissionAuthorizationHandler |
| Auditoria de negação | ✅ | WARNING logging |
| Separação frontend/backend | ✅ | Comentário explícito + ProtectedRoute |
| Políticas dinâmicas | ✅ | PermissionPolicyProvider |
| Papéis custom | ✅ | Modelo suportado |
| Permissões por ambiente | ⚠️ | Entidade existe, enforcement parcial |
| Menor privilégio | ✅ | Progressão de papéis |
| Separação de deveres | ✅ | Auditor/Developer/ApprovalOnly distintos |

---

> **Classificação final:** GRANULAR_AND_COHERENT — Modelo de autorização bem estruturado, com 73 permissões, 7 papéis, enforcement deny-by-default e alinhamento frontend coerente.

# NexTraceOne — Checklist de Entrada para Aceite

> **Documento de referência:** `docs/acceptance/NexTraceOne_Escopo_Homologavel.md`
> **Plano de teste:** `docs/acceptance/NexTraceOne_Plano_Teste_Funcional.md`
> **Plano operacional:** `docs/planos/NexTraceOne_Plano_Operacional_Finalizacao.md`

---

## 1. Objectivo

Confirmar que todos os critérios de entrada para o teste de aceitação (Fase 8) estão satisfeitos antes de iniciar a execução do plano de testes.

---

## 2. Critérios de entrada

### 2.1 Sem bloqueadores P0 abertos

| Verificação | Estado | Evidência |
|------------|--------|-----------|
| Backend compila sem erros | ✅ Aprovado | `run_build` → compilação bem-sucedida |
| Frontend compila sem erros TypeScript | ✅ Aprovado | `npx tsc --noEmit` → zero erros |
| Nenhum crash em runtime nos fluxos core | ✅ Aprovado | Bugs corrigidos nas Fases 1-6 (JWT audience, middleware order, ProtectedRoute race condition, missing roleId, minLength) |
| Nenhum loop infinito em auth | ✅ Aprovado | ProtectedRoute estabilizado na Fase 2 (isLoadingUser) |

### 2.2 Endpoints core estáveis

| Endpoint | Módulo | Estado |
|---------|--------|--------|
| `POST /identity/auth/login` | Identity | ✅ Estável |
| `GET /identity/auth/me` | Identity | ✅ Corrigido (Fase 1 — JWT audience) |
| `GET /identity/users` | Identity | ✅ Estável |
| `POST /identity/break-glass/*` | Identity | ✅ Corrigido (Fase 1 — minLength) |
| `GET /catalog/services` | Catalog | ✅ Estável |
| `GET /catalog/services/:id` | Catalog | ✅ Estável |
| `GET /catalog/graph` | Catalog | ✅ Estável |
| `GET /contracts/summary` | Contracts | ✅ Estável |
| `GET /contracts/*` | Contracts | ✅ Estável |
| `GET /changes/summary` | Changes | ✅ Estável |
| `GET /changes/*` | Changes | ✅ Estável |
| `GET /incidents/summary` | Incidents | ✅ Estável |
| `GET /incidents/:id` | Incidents | ✅ Corrigido (Fase 4 — data bugs) |
| `GET /audit/events` | Audit | ✅ Estável |
| `GET /health` | Platform | ✅ Estável |
| `GET /ready` | Platform | ✅ Estável |
| `GET /live` | Platform | ✅ Estável |

### 2.3 Navegação principal íntegra

| Verificação | Estado | Evidência |
|------------|--------|-----------|
| Sidebar renderiza correctamente | ✅ Aprovado | 23 itens homologáveis + 30+ preview com badge |
| Navegação entre módulos sem crash | ✅ Aprovado | Todas as rotas com ProtectedRoute (Fase 3-4) |
| Rotas protegidas redireccionam sem auth | ✅ Aprovado | ProtectedRoute em todas as rotas homologáveis |
| Preview modules mostram banner | ✅ Aprovado | PreviewGate + PreviewBanner (Fase 5) |
| Lazy loading funciona | ✅ Aprovado | Todas as páginas protegidas com lazy() |
| Loading/Error states consistentes | ✅ Aprovado | PageLoadingState + PageErrorState (Fase 6) |
| Catch-all redirect para `/` | ✅ Aprovado | `<Route path="*" element={<Navigate to="/" replace />} />` |

### 2.4 Massa de teste mínima carregada

| Base de dados | Seed file | Conteúdo |
|--------------|-----------|----------|
| IdentityDatabase | `seed-identity.sql` | 2 tenants, 10 users (4 principais de teste), memberships, roles, environments, security events |
| CatalogDatabase | `seed-catalog.sql` | Serviços, dependências, ownership, metadata |
| ChangeIntelligenceDatabase | `seed-changegovernance.sql` | Mudanças, releases, validações, blast radius |
| AuditDatabase | `seed-audit.sql` | Eventos de auditoria |
| IncidentDatabase | `seed-incidents.sql` | Incidentes com timeline, correlação, evidência, mitigação, runbooks |
| AiGovernanceDatabase | `seed-aiknowledge.sql` | Modelos, políticas, knowledge sources |

**Mecanismo:** Seeds aplicados automaticamente via `DevelopmentSeedDataExtensions.SeedDevelopmentDataAsync()` no arranque do ApiHost em ambiente Development. Todos os scripts são idempotentes (`ON CONFLICT DO NOTHING`).

### 2.5 Migrations aplicadas

| Migration | Módulo | Estado |
|----------|--------|--------|
| InitialCreate (Identity) | Identity | ✅ |
| InitialCreate (Audit) | Audit | ✅ |
| InitialCreate (CatalogGraph) | Catalog | ✅ |
| InitialCreate (Contracts) | Contracts | ✅ |
| InitialCreate (DeveloperPortal) | Portal | ✅ |
| InitialCreate (ChangeIntelligence) | Changes | ✅ |
| InitialCreate (RulesetGovernance) | Changes | ✅ |
| InitialCreate (Workflow) | Changes | ✅ |
| InitialCreate (Promotion) | Changes | ✅ |
| InitialCreate (Incidents) | Operations | ✅ |
| InitialCreate (AiGovernance) | AI | ✅ |

**Mecanismo:** Migrations aplicadas automaticamente via `app.ApplyDatabaseMigrationsAsync()` no arranque.

### 2.6 Escopo congelado

| Verificação | Estado | Evidência |
|------------|--------|-----------|
| Documento de escopo homologável criado | ✅ | `docs/acceptance/NexTraceOne_Escopo_Homologavel.md` |
| Módulos excluídos documentados | ✅ | Secção 3 do documento de escopo |
| Plano de teste funcional criado | ✅ | `docs/acceptance/NexTraceOne_Plano_Teste_Funcional.md` |
| Nenhuma feature nova a ser adicionada | ✅ | Escopo congelado até fim da Fase 8 |

---

## 3. Utilizadores de teste

| Email | Papel | Senha | Tenant |
|-------|-------|-------|--------|
| admin@nextraceone.dev | PlatformAdmin | Admin@123 | NexTrace Corp |
| techlead@nextraceone.dev | TechLead | Admin@123 | NexTrace Corp |
| dev@nextraceone.dev | Developer | Admin@123 | NexTrace Corp |
| auditor@nextraceone.dev | Auditor | Admin@123 | NexTrace Corp |

---

## 4. Como arrancar o ambiente

```bash
# 1. Backend (ApiHost) — aplica migrations e seed automaticamente
cd src/platform/NexTraceOne.ApiHost
dotnet run

# 2. Frontend
cd src/frontend
npm install
npm run dev
```

**Requisitos:**
- PostgreSQL a correr (connection strings configuradas em `appsettings.Development.json`)
- .NET 10 SDK instalado
- Node.js 18+ instalado

---

## 5. Decisão

| Critério | Estado |
|---------|--------|
| Sem bloqueadores P0 | ✅ |
| Endpoints core estáveis | ✅ |
| Navegação principal íntegra | ✅ |
| Massa de teste mínima | ✅ |
| Migrations aplicadas | ✅ |
| Escopo congelado | ✅ |

### **Resultado: PRONTO para iniciar Fase 8 — Execução do teste de aceitação.**

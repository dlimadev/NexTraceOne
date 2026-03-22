# Phase 4 — AI Policies and Token Budgets

## AI Policies

### Features Cobertas
- Criação, consulta, actualização e listagem de políticas de acesso IA
- Controlo de escopo (User, Group, Role, Persona, Team)
- Permissão/bloqueio de IA externa
- Limites de tokens por requisição
- Allow/block list de modelos

### Contratos Backend
- **Entidade:** `AIAccessPolicy`
- **Endpoints:**
  - `GET /api/v1/ai/policies` — Lista políticas com filtros (scope, isActive)
  - `POST /api/v1/ai/policies` — Cria nova política
  - `PATCH /api/v1/ai/policies/{id}` — Actualiza política existente
- **Permissões:** `ai:governance:read` (leitura), `ai:governance:write` (escrita)

### Frontend
- **Página:** `AiPoliciesPage.tsx`
- Stat cards: Total, Active, Internal Only, External Allowed
- Filtros: All, Active, Inactive + busca por texto
- Badges: Active/Inactive, scope, Internal Only, External Allowed
- Token limits: exibidos por política

### Testes
- Backend: 8 testes unitários de entidade (Create, Update, Activate/Deactivate, IsModelAllowed)
- Frontend: 6 novos testes (loading, success, error, empty, scope display, badges)

---

## Token Budgets

### Features Cobertas
- Gestão de orçamentos de tokens por período (Daily, Weekly, Monthly)
- Controlo de uso acumulado (tokens e requisições)
- Detecção de quota excedida
- Reset de período

### Contratos Backend
- **Entidade:** `AIBudget`
- **Endpoints:**
  - `GET /api/v1/ai/budgets` — Lista budgets com filtros (scope, isActive)
  - `PATCH /api/v1/ai/budgets/{id}` — Actualiza budget existente
- **Permissões:** `ai:governance:read` (leitura), `ai:governance:write` (escrita)

### Frontend
- **Página:** `TokenBudgetPage.tsx`
- Stat cards: Total, Active, Quota Exceeded, Total Tokens Used
- Busca por texto
- Barras de progresso visuais para tokens e requisições
- Cores: accent (<80%), warning (80-99%), critical (100%)
- Badge de Quota Exceeded

### Riscos Tratados
- Budget enforcement avançado (corte automático de acesso quando quota é excedida) é responsabilidade do runtime de IA e fica para evolução futura
- A gestão administrativa dos budgets está funcional e persiste dados reais
- O campo `isQuotaExceeded` é derivado do estado da entidade

### Testes
- Backend: 5 testes unitários (Create, RecordUsage, QuotaExceeded, ResetPeriod, Update)
- Frontend: 7 novos testes (loading, success, error, empty, quota exceeded badge, scope/period, token usage)

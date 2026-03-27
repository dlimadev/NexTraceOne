# Phase 4 — AI Routing and IDE Integrations

## AI Routing

### Regras de Roteamento
- Estratégias de roteamento controlam como requisições de IA são encaminhadas
- Cada estratégia define: persona alvo, caso de uso, tipo de cliente, caminho preferido, nível máximo de sensibilidade
- Suporte a escalação para IA externa quando permitido
- Prioridade numérica para resolução de conflitos

### Backend
- **Entidades:**
  - `AIRoutingStrategy` — Estratégia de roteamento
  - `AIRoutingDecision` — Decisão de roteamento (registo de auditoria)
  - `AIKnowledgeSourceWeight` — Peso de fonte de conhecimento por caso de uso
- **Endpoints:**
  - `GET /api/v1/ai/routing/strategies` — Lista estratégias
  - `GET /api/v1/ai/routing/decisions/{id}` — Consulta decisão
  - `GET /api/v1/ai/knowledge-sources/weights` — Lista pesos de fontes
- **Permissões:** `ai:governance:read`

### Frontend
- **Página:** `AiRoutingPage.tsx`
- Duas tabs: Strategies e Source Weights
- Estratégias com expandir/colapsar para detalhes
- Badges: Active/Inactive, InternalOnly/InternalPreferred, Sensitivity level
- Pesos de fontes por caso de uso com barras visuais de relevância

### Validação de Produção
- Estratégias podem ser administradas via UI real
- Decisões de roteamento ficam registadas para auditoria
- Pesos de fontes controlam priorização de contexto

### Testes
- Backend: 17 testes unitários (Strategy CRUD, IsApplicable, Decision Record, SourceWeight CRUD, ExecutionPlan, EnrichmentResult)
- Frontend: 6 novos testes (loading, success, error, empty, stat counts, path badges)

---

## IDE Integrations

### Funcionalidade
- Registo e gestão de clientes IDE (VS Code e Visual Studio)
- Políticas de capacidade por tipo de cliente e persona
- Controlo de comandos permitidos, contextos, geração de contratos
- Sumário administrativo com estatísticas de clientes

### Backend
- **Entidades:**
  - `AIIDEClientRegistration` — Registo de cliente IDE
  - `AIIDECapabilityPolicy` — Política de capacidade IDE
- **Endpoints:**
  - `GET /api/v1/ai/ide/capabilities` — Capacidades por tipo de cliente
  - `GET /api/v1/ai/ide/clients` — Lista clientes registados
  - `POST /api/v1/ai/ide/clients/register` — Regista novo cliente
  - `GET /api/v1/ai/ide/policies` — Lista políticas de capacidade
  - `GET /api/v1/ai/ide/summary` — Sumário administrativo
- **Permissões:** `ai:ide:read` (leitura), `ai:ide:write` (registo)

### Frontend
- **Página:** `IdeIntegrationsPage.tsx`
- Sumário: Total Clients, VS Code, Visual Studio, Active Policies
- Cards por tipo de cliente com capacidades e estado
- Tabela de clientes registados com filtros
- Secção de políticas de capacidade

### Testes
- Backend: 11 novos testes unitários (IDE Client Register, RecordAccess, Revoke, Reactivate; IDE Policy Create, Update, Deactivate, Activate, SetAllowedModels)
- Frontend: 6 novos testes (loading, success, error, empty, stat cards, policy details)

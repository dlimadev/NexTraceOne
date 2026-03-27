# Wave 3 — Rate Limiting and CORS

## Rate Limiting

### Políticas Definidas

| Política | Limite | Janela | Queue | Aplicação |
|----------|--------|--------|-------|-----------|
| **Global** | 100 req (20 sem IP) | 60s | 5 | Todos os endpoints (default) |
| **auth** | 20 req | 60s | 2 | Login, refresh, federated, OIDC callback |
| **auth-sensitive** | 10 req | 60s | 2 | Registration, OIDC start, session creation |
| **ai** | 30 req | 60s | 3 | Chat, orchestration, external AI, geração |
| **data-intensive** | 50 req | 60s | 3 | Catálogo, reports, FinOps, analytics |
| **operations** | 40 req | 60s | 3 | Incidentes, reliability, runtime intelligence |

### Rationale

- **ai (30/min)**: endpoints de IA são computacionalmente custosos e podem ter custo financeiro (API keys externas). Limite mais restritivo que global para prevenir abuso.
- **data-intensive (50/min)**: endpoints que executam queries complexas contra BD. Limite equilibra usabilidade com proteção contra scraping.
- **operations (40/min)**: endpoints de operação são sensíveis e não devem ser abusados por automações mal configuradas.

### Grupos de Endpoint Protegidos

| Grupo | Política | Endpoints |
|-------|----------|-----------|
| AI Runtime | `ai` | `/api/v1/ai/*` (chat, providers, models, tokens) |
| AI Orchestration | `ai` | `/api/v1/aiorchestration/*` (catalog, changes, contracts, analysis, generate) |
| External AI | `ai` | `/api/v1/externalai/*` (query, knowledge) |
| Service Catalog | `data-intensive` | `/api/v1/catalog/*` |
| Reports | `data-intensive` | `/api/v1/reports/*` |
| FinOps | `data-intensive` | `/api/v1/finops/*` |
| Incidents | `operations` | `/api/v1/incidents/*` |
| Reliability | `operations` | `/api/v1/reliability/*` |
| Runtime Intelligence | `operations` | `/api/v1/runtime/*` |

### Configuração por Ambiente

Os limites são configuráveis via código. Em produção, os limites atuais são conservadores e podem ser ajustados via feature flags ou configuração se necessário.

---

## CORS

### Configuração por Ambiente

| Ambiente | Comportamento |
|----------|--------------|
| **Development** | Fallback para `localhost:5173` e `localhost:3000` se nenhuma origem configurada |
| **CI** | Mesmo comportamento de Development |
| **Staging** | **Requer** `Cors:AllowedOrigins` explícito — sem fallback |
| **Production** | **Requer** `Cors:AllowedOrigins` explícito — sem fallback |

### Como Configurar

**Via appsettings:**
```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://app.nextraceone.com",
      "https://staging.nextraceone.com"
    ]
  }
}
```

**Via environment variables:**
```bash
Cors__AllowedOrigins__0=https://app.nextraceone.com
Cors__AllowedOrigins__1=https://staging.nextraceone.com
```

### Regras de Segurança

1. Wildcard (`*`) é **proibido** (AllowCredentials está ativo)
2. Production e Staging não aceitam fallback para localhost
3. Headers permitidos são explicitamente listados
4. Credentials são permitidos (cookies de sessão)

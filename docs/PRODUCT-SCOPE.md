# PRODUCT-SCOPE.md — Escopo do produto NexTraceOne

> **Última atualização:** Março 2026 (pós-finalização Onda 1)
>
> Documento complementar: [ROADMAP.md](./ROADMAP.md) — plano de execução por ondas e sprints.
> Documento complementar: [REBASELINE.md](./REBASELINE.md) — inventário real do estado atual.

---

## Core (Onda 1 — Fechamento em progresso)

Fluxos centrais que definem o NexTraceOne como produto:

- **Contract Governance & Source of Truth** — cadastro, versionamento, diff, compatibilidade, ownership, busca, Contract Studio
  - ✅ Backend real com persistência; Frontend conectado; i18n completo; 5 testes SoT Explorer
- **Change Confidence** — submissão, evidence, blast radius, advisory, approval, trilha de decisão
  - ✅ Backend real com persistência (195 testes); Frontend conectado; i18n completo; 16 testes frontend
- **Incident Correlation & Mitigation** — correlação, troubleshooting, mitigação guiada, runbooks, validação pós-ação
  - ⚠️ Domain/Application/API prontos (164 testes backend); Frontend conectado via API; handlers retornam mock; 13 testes frontend
- **AI Assistant grounded** — grounding em contratos/serviços/incidents/changes, explicação de fontes, prompts por persona
  - ⚠️ Governance completa; UI funcional com 4 contextos; grounding depende de modelo real; 5 testes frontend

### Ajustes transversais concluídos (finalização Onda 1)

- ✅ i18n — 3 strings hardcoded corrigidas; chaves adicionadas em 4 locales
- ✅ Error states — adicionados em ChangeCatalogPage, SourceOfTruthExplorer, DashboardPage
- ✅ Navegação cross-entity — links bidirecionais entre serviço ↔ contrato ↔ change ↔ incident
- ✅ Breadcrumbs — ativos em todas as rotas (já implementado em AppLayout)
- ✅ 23 novos testes frontend para os 4 fluxos centrais

### Módulos de suporte ao core (prontos)

- Service Catalog & Ownership (✅ produção)
- Identity & Access (✅ produção)
- Audit Compliance (✅ produção)
- ~~Licensing~~ (removido — fora do núcleo do produto)

---

## Productização (Onda 2 — Consolidar depois)

Tornar o produto utilizável em escala:

- UX por persona (Engineer, Tech Lead, Architect, Product, Executive, Platform Admin, Auditor)
- Product polish restante (componentes de loading mais ricos, mobile responsividade)
- Product analytics validado
- Integration Hub pragmático (conectores estáveis, freshness visível)
- **Persistência real dos Incidents** — migrar handlers mock para EF stores

> **Nota:** Empty states, error states, breadcrumbs, e navegação cross-entity
> foram antecipados e concluídos na finalização da Onda 1.

---

## Hardening (Onda 3 — Endurecer)

Tornar a plataforma confiável para uso sério:

- Performance de endpoints críticos
- Testes E2E dos fluxos centrais
- Health/readiness
- Packaging/deployment
- Self-hosted readiness
- Revisão de permissões e escopos

---

## Evolução futura (Onda 4 — Seletivo)

Só após fechar ondas 0-3:

- Change Advisory Intelligence pragmático
- Operational Knowledge Memory
- Connector SDK
- Previsão seletiva com dados reais

### Congelado por agora

- FinOps com dados reais (depende de dados de custo)
- Advanced compliance packs
- Governance fabrics avançados
- Multi-org governance
- AI routing avançado
- Digital twins e orchestrators

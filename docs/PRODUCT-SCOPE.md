# PRODUCT-SCOPE.md — Escopo do produto NexTraceOne

> **Última atualização:** Março 2026
>
> Documento complementar: [ROADMAP.md](./ROADMAP.md) — plano de execução por ondas e sprints.
> Documento complementar: [REBASELINE.md](./REBASELINE.md) — inventário real do estado atual.

---

## Core (Onda 1 — Fechar agora)

Fluxos centrais que definem o NexTraceOne como produto:

- **Contract Governance & Source of Truth** — cadastro, versionamento, diff, compatibilidade, ownership, busca, Contract Studio
- **Change Confidence** — submissão, evidence, blast radius, advisory, approval, trilha de decisão
- **Incident Correlation & Mitigation** — correlação, troubleshooting, mitigação guiada, runbooks, validação pós-ação
- **AI Assistant grounded** — grounding em contratos/serviços/incidents/changes, explicação de fontes, prompts por persona

### Módulos de suporte ao core (prontos)

- Service Catalog & Ownership (✅ produção)
- Identity & Access (✅ produção)
- Audit Compliance (✅ produção)
- Licensing (✅ produção)

---

## Productização (Onda 2 — Consolidar depois)

Tornar o produto utilizável em escala:

- UX por persona (Engineer, Tech Lead, Architect, Product, Executive, Platform Admin, Auditor)
- Product polish (empty states, loading, errors, breadcrumbs, navegação cross-entity)
- Product analytics validado
- Integration Hub pragmático (conectores estáveis, freshness visível)

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

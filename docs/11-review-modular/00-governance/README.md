# 00-governance — Governança da Revisão Modular

## Objetivo

Esta pasta contém os **relatórios transversais, metodologia e ferramentas de acompanhamento** da revisão modular do NexTraceOne. É o ponto central de coordenação para toda a revisão.

---

## Metodologia de Revisão

A revisão modular segue uma metodologia estruturada e reprodutível:

1. **Código como fonte principal de verdade** — toda análise parte do código real, não de documentação ou suposições
2. **Análise por módulo** — cada domínio funcional é revisto independentemente
3. **Cruzamento entre camadas** — frontend, backend, banco, IA e documentação são cruzados para identificar inconsistências
4. **Classificação padronizada** — gaps e funcionalidades são classificados com vocabulário comum
5. **Priorização orientada por produto** — a ordem de revisão e correção segue a visão oficial do NexTraceOne

Para detalhes completos, consultar: [review-methodology.md](./review-methodology.md)

---

## Relatórios Transversais Existentes

Os relatórios abaixo fornecem uma visão consolidada do estado do produto antes e durante a revisão:

| Relatório                                                                     | Descrição                                                              |
|-------------------------------------------------------------------------------|------------------------------------------------------------------------|
| [modular-review-summary.md](./modular-review-summary.md)                     | Resumo executivo da revisão modular                                    |
| [module-inventory-report.md](./module-inventory-report.md)                    | Inventário completo de módulos identificados no produto                |
| [repository-structural-audit.md](./repository-structural-audit.md)            | Auditoria da estrutura do repositório                                  |
| [frontend-pages-and-routes-report.md](./frontend-pages-and-routes-report.md)  | Mapeamento de páginas e rotas do frontend                              |
| [menu-structure-report.md](./menu-structure-report.md)                        | Análise da estrutura de menus e navegação                              |
| [markdown-inventory-report.md](./markdown-inventory-report.md)                | Inventário de toda a documentação markdown existente                   |
| [documentation-vs-code-gap-report.md](./documentation-vs-code-gap-report.md)  | Gaps entre documentação existente e código real                        |
| [review-priority-recommendation.md](./review-priority-recommendation.md)      | Recomendação de prioridade para revisão dos módulos                    |

---

## Ferramentas de Acompanhamento

Além dos relatórios, esta pasta contém ferramentas para gerir a revisão:

| Ficheiro                                                           | Finalidade                                                      |
|--------------------------------------------------------------------|-----------------------------------------------------------------|
| [review-methodology.md](./review-methodology.md)                  | Metodologia detalhada de revisão                                |
| [review-status-overview.md](./review-status-overview.md)          | Tabela de status de todos os módulos                            |
| [review-checklist-global.md](./review-checklist-global.md)        | Checklist global aplicável a todos os módulos                   |
| [module-priority-matrix.md](./module-priority-matrix.md)          | Matriz de prioridade com critérios de decisão                   |

---

## Priorização de Módulos

A priorização segue os **pilares oficiais do produto**:

### Prioridade máxima (núcleo do produto)

- **Service Catalog & Ownership** — o NexTraceOne é fonte de verdade para serviços
- **Contract Governance** — contratos são first-class citizens no produto
- **Identity & Access** — fundação de segurança e persona-awareness

### Prioridade alta (diferenciadores)

- **Change Intelligence / Change Confidence** — diferenciador central do produto
- **Operational Intelligence** — incidentes, runbooks, AIOps

### Prioridade média (governança e IA)

- **AI & Knowledge** — IA governada como capacidade transversal
- **Governance, Audit & Compliance** — reports, risk, compliance

### Prioridade base (suporte e extensão)

- **Configuration & Integrations** — ambientes, integrações externas
- **Notifications & Analytics** — alertas e métricas de produto

Para detalhes completos, consultar: [module-priority-matrix.md](./module-priority-matrix.md)

---

## Visão Consolidada da Revisão

### Estado atual

A revisão modular está em fase de **estruturação e análise inicial**. Os relatórios transversais já foram gerados e fornecem a base para a revisão módulo a módulo.

### Próximos passos

1. Completar a análise de cada módulo usando o template `module-review.md`
2. Atualizar o status de cada módulo em [review-status-overview.md](./review-status-overview.md)
3. Priorizar gaps identificados na [module-priority-matrix.md](./module-priority-matrix.md)
4. Iniciar correções pelos módulos de maior prioridade
5. Re-testar e validar módulos corrigidos
6. Atualizar este README conforme a revisão avança

### Princípio orientador

> Toda decisão de revisão e correção deve aproximar o produto da visão oficial do NexTraceOne como **plataforma unificada de governança de serviços e contratos, confiança em mudanças de produção, consistência operacional e inteligência assistida por IA**.

# Revisão Modular — NexTraceOne

## Objetivo

Esta pasta contém a **revisão modular completa** do NexTraceOne, organizada por domínio funcional. O objetivo é garantir que cada módulo do produto está alinhado com a visão oficial, que os gaps são identificados e priorizados, e que a evolução do produto segue uma estratégia incremental orientada por produto.

A revisão modular é o processo central para consolidar o NexTraceOne como **fonte de verdade para serviços, contratos, mudanças e conhecimento operacional**.

---

## Escopo da Revisão

A revisão cobre **todas as camadas do produto**:

| Camada              | O que é analisado                                                        |
|---------------------|--------------------------------------------------------------------------|
| **Frontend**        | Páginas, rotas, componentes, menus, i18n, UX por persona                 |
| **Backend**         | Endpoints, serviços de aplicação, domínio, contratos públicos            |
| **Banco de Dados**  | Entidades, migrações, integridade referencial, índices                   |
| **IA**              | Modelos, prompts, governança, auditoria, integração com módulos          |
| **Agents**          | Workers, background services, event handlers, scheduled jobs             |
| **Documentação**    | Docs inline, docs técnicos, runbooks, knowledge hub                      |

---

## Organização da Pasta

A pasta está organizada por **domínio funcional**, cada um com o seu próprio diretório:

```
docs/11-review-modular/
├── 00-governance/               # Relatórios transversais, metodologia e acompanhamento
├── 01-contracts/                # Revisão do módulo de contratos
├── 02-identity-access/          # Revisão de identidade e acesso
├── 03-catalog/                  # Revisão do catálogo de serviços
├── 04-change-governance/        # Revisão de change intelligence / change confidence
├── 05-operational-intelligence/ # Revisão de operações, incidentes, AIOps
├── 06-governance/               # Revisão de governança, reports, compliance
├── 07-configuration/            # Revisão de configuração, ambientes, integrações
├── 08-ai-knowledge/             # Revisão de IA, knowledge hub, model registry
├── 09-audit-compliance/         # Revisão de auditoria e compliance
├── 10-notifications/            # Revisão de notificações e alertas
├── 11-integrations/             # Revisão de integrações externas
├── 12-product-analytics/        # Revisão de analytics e métricas de produto
└── README.md                    # Este ficheiro
```

---

## Como Usar os Templates

Cada módulo segue a **mesma estrutura de revisão**, documentada no ficheiro `module-review.md` dentro de cada pasta. A estrutura padrão inclui:

1. **Visão geral do módulo** — descrição, papel no produto, pilares associados
2. **Inventário de funcionalidades** — o que existe, o que funciona, o que falta
3. **Análise de gaps** — funcionalidades incompletas, órfãs, quebradas ou ausentes
4. **Cruzamento entre camadas** — frontend ↔ backend ↔ banco ↔ IA ↔ docs
5. **Checklist de conformidade** — i18n, segurança, persona, UX, testes
6. **Recomendações** — ações priorizadas para alinhar com a visão oficial
7. **Status e tracking** — estado atual, responsável, próximos passos

### Para iniciar a revisão de um módulo

1. Abrir o ficheiro `module-review.md` do módulo
2. Preencher cada secção com base na análise do código e da documentação
3. Classificar os gaps encontrados (ver definições abaixo)
4. Atualizar o status no ficheiro `00-governance/review-status-overview.md`
5. Seguir a ordem de trabalho recomendada

---

## Ordem de Trabalho Recomendada

A revisão deve seguir a **ordem de prioridade do produto**, não a ordem numérica das pastas:

| Fase | Módulos                                         | Justificação                                         |
|------|--------------------------------------------------|------------------------------------------------------|
| 1    | 03-catalog, 01-contracts                         | Núcleo central: serviços e contratos como pilares    |
| 2    | 02-identity-access                               | Fundação: sem identidade, nada funciona              |
| 3    | 04-change-governance                             | Diferenciador: change confidence é core do produto   |
| 4    | 05-operational-intelligence                      | Operações: incidentes, runbooks, AIOps               |
| 5    | 08-ai-knowledge                                  | IA governada e knowledge hub                         |
| 6    | 06-governance, 09-audit-compliance               | Governança, reports, compliance                      |
| 7    | 07-configuration, 11-integrations                | Configuração e integrações                           |
| 8    | 10-notifications, 12-product-analytics           | Notificações e analytics                             |

---

## Definições de Status

Cada módulo é classificado com um dos seguintes status:

| Status               | Descrição                                                                 |
|----------------------|---------------------------------------------------------------------------|
| `NOT_STARTED`        | Revisão ainda não iniciada                                                |
| `IN_ANALYSIS`        | Revisão em curso, análise do código e documentação em andamento           |
| `GAP_IDENTIFIED`     | Gaps identificados e documentados, aguardando priorização                 |
| `IN_FIX`            | Correções em curso para os gaps identificados                             |
| `BLOCKED`           | Revisão ou correção bloqueada por dependência externa ou decisão pendente |
| `READY_FOR_RETEST`  | Correções aplicadas, aguardando re-teste e validação                      |
| `APPROVED`          | Revisão completa, módulo aprovado e conforme com a visão do produto       |
| `DONE`              | Módulo totalmente revisto, corrigido, testado e documentado               |

### Fluxo de status

```
NOT_STARTED → IN_ANALYSIS → GAP_IDENTIFIED → IN_FIX → READY_FOR_RETEST → APPROVED → DONE
                   ↕               ↕              ↕
                BLOCKED         BLOCKED         BLOCKED
```

> **Nota:** `BLOCKED` pode ocorrer em qualquer fase ativa da revisão. Quando desbloqueado, o módulo retorna ao estado em que estava antes do bloqueio (IN_ANALYSIS, GAP_IDENTIFIED ou IN_FIX).

---

## Definições de Prioridade

Os gaps e módulos são classificados por prioridade:

| Prioridade  | Descrição                                                                        |
|-------------|----------------------------------------------------------------------------------|
| `CRITICAL`  | Bloqueia o uso do produto ou compromete segurança/integridade de dados           |
| `HIGH`      | Funcionalidade core incompleta ou inconsistente, impacto direto na experiência   |
| `MEDIUM`    | Funcionalidade secundária com gaps, impacto parcial na experiência               |
| `LOW`       | Melhorias cosméticas, otimizações, documentação complementar                     |

---

## Quem Deve Preencher Cada Secção

| Secção                           | Responsável principal          | Colaboradores                  |
|----------------------------------|--------------------------------|--------------------------------|
| Inventário de funcionalidades    | Engineer / Tech Lead           | —                              |
| Análise de gaps                  | Tech Lead / Architect          | Engineer                       |
| Cruzamento entre camadas         | Architect                      | Tech Lead, Engineer            |
| Checklist de conformidade        | Tech Lead                      | Engineer, Product              |
| Recomendações                    | Architect / Tech Lead          | Product, Platform Admin        |
| Status e tracking                | Tech Lead                      | Project Manager                |
| Validação final                  | Product / Architect            | Tech Lead                      |

---

## Como Esta Revisão Consolida o Produto

A revisão modular é essencial para:

1. **Garantir alinhamento com a visão oficial** — cada módulo é avaliado contra os pilares do produto
2. **Identificar gaps reais** — baseado no código, não em suposições
3. **Priorizar evolução** — foco no que mais impacta o produto como Source of Truth
4. **Evitar regressão** — checklist e re-teste garantem qualidade contínua
5. **Documentar decisões** — cada gap identificado tem justificação e recomendação
6. **Acelerar onboarding** — novos membros da equipa entendem rapidamente o estado de cada módulo
7. **Suportar governança** — executives e auditors têm visibilidade sobre o estado do produto

### Princípio fundamental

> **O código é a fonte principal de verdade.** A documentação é referência secundária. A revisão modular cruza ambas para garantir que o produto real corresponde à visão oficial do NexTraceOne.

---

## Ficheiros de Referência

- [Metodologia de Revisão](./00-governance/review-methodology.md)
- [Status Geral da Revisão](./00-governance/review-status-overview.md)
- [Checklist Global](./00-governance/review-checklist-global.md)
- [Matriz de Prioridade](./00-governance/module-priority-matrix.md)
- [Resumo da Revisão Modular](./00-governance/modular-review-summary.md)
- [Inventário de Módulos](./00-governance/module-inventory-report.md)
- [Auditoria Estrutural](./00-governance/repository-structural-audit.md)

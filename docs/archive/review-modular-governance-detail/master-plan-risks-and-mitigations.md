# Riscos de Execução e Mitigações — Plano Mestre NexTraceOne

> **Classificação:** ANÁLISE DE RISCOS  
> **Data de referência:** Julho 2025  
> **Escopo:** Riscos identificados na execução do plano de remediação  
> **Objetivo:** Antecipar problemas e definir mitigações preventivas

---

## 1. Resumo de Riscos

| Nível | Quantidade | Descrição |
|-------|-----------|-----------|
| CRÍTICO | 2 | Podem causar falha completa do plano |
| ALTO | 4 | Podem atrasar significativamente |
| MÉDIO | 5 | Podem causar retrabalho |
| BAIXO | 3 | Inconvenientes gerenciáveis |
| **Total** | **14** | |

---

## 2. Riscos CRÍTICOS

### R-1: Expandir AI antes de Segurança Estar Sólida

| Dimensão | Detalhe |
|----------|---------|
| **Probabilidade** | MÉDIA |
| **Impacto** | CRÍTICO |
| **Descrição** | Implementar funcionalidades de AI (tools, streaming, RAG) antes de ter MFA enforced, API keys em BD encriptada e permissões de AI governance completas. IA com acesso a dados sensíveis sem segurança adequada. |
| **Consequência** | Exposição de dados sensíveis via AI, prompts que exfiltram informação, acesso não autorizado a dados de outros tenants via queries AI. |
| **Mitigação** | Wave 1 (segurança) é **obrigatória** antes de Wave 4 (AI). Não iniciar trabalho em AI tools sem MFA enforced e permissões validadas. |
| **Indicador** | Se alguém começa a trabalhar em AI antes de Wave 1 estar fechada → flag imediata. |
| **Plano B** | Se segurança atrasa, implementar AI em modo read-only (sem tools que modifiquem dados). |

### R-2: Fechar Frontend antes de Backend/DB Estarem Prontos

| Dimensão | Detalhe |
|----------|---------|
| **Probabilidade** | ALTA |
| **Impacto** | CRÍTICO |
| **Descrição** | Implementar páginas frontend e UX antes de endpoints e schema estarem estáveis. Causa raiz #1 identificada na auditoria — é exactamente este padrão que criou os problemas actuais (rotas partidas, páginas órfãs). |
| **Consequência** | Repetir o mesmo padrão que causou os problemas originais. Frontend terá de ser refeito quando backend mudar. Desperdício de 2-3 semanas de trabalho. |
| **Mitigação** | Abordagem **backend-first**: endpoints estáveis antes de UI. Wave 2 (backend/DB) antes de Wave 3 (frontend). Excepção: Wave 0 quick wins de frontend (rotas partidas). |
| **Indicador** | Se frontend começa a criar páginas para endpoints que ainda não existem → flag imediata. |
| **Plano B** | Se backend atrasa, frontend pode trabalhar com mocks bem definidos (contratos API estáveis, implementação mock). |

---

## 3. Riscos ALTOS

### R-3: Corrigir Menu antes de Compreender Módulos

| Dimensão | Detalhe |
|----------|---------|
| **Probabilidade** | ALTA |
| **Impacto** | ALTO |
| **Descrição** | Alterar a sidebar e a estrutura de navegação antes de ter clareza sobre a estrutura modular final (especialmente Governance que precisa de divisão). |
| **Consequência** | Menu alterado terá de ser refeito quando módulo Governance for dividido. Utilizadores habituam-se a navegação que vai mudar. |
| **Mitigação** | Revisão modular **antes** de alterar menu. Wave 0 corrige apenas rotas partidas sem reestruturar menu. Reestruturação de menu apenas após Wave 2 (quando Governance estiver planeado). |
| **Indicador** | Se alguém reestrutura menu sidebar sem documento de módulos aprovado → flag. |
| **Plano B** | Criar menu com secções genéricas (e.g., "Governance & Reports") que acomodem divisão futura. |

### R-4: Consolidar Migrações Tarde Demais

| Dimensão | Detalhe |
|----------|---------|
| **Probabilidade** | MÉDIA |
| **Impacto** | ALTO |
| **Descrição** | Adiar a criação de migrações para Configuration e Notifications (P3-3, P3-4) e a adição de RowVersion (P3-2), resultando em schema drift e conflitos difíceis de resolver. |
| **Consequência** | Quando as migrações forem finalmente criadas, podem conflitar com alterações feitas em waves posteriores. RowVersion adicionado tardiamente causa breaking changes massivos em handlers. |
| **Mitigação** | Wave 2 inclui migrações e RowVersion **cedo** no plano. Não deixar para Wave 4 ou 5. |
| **Indicador** | Se Wave 2 termina sem migrações de Config/Notifications criadas → flag. |
| **Plano B** | Se migrações conflitam, criar migration "squash" que consolida estado. |

### R-5: Corrigir Documentação sem Alinhar Código

| Dimensão | Detalhe |
|----------|---------|
| **Probabilidade** | ALTA |
| **Impacto** | ALTO |
| **Descrição** | Escrever documentação detalhada (READMEs, onboarding, architecture) que descreve o estado desejado em vez do estado real. Já aconteceu com a documentação do módulo AI (65% docs vs 25% backend). |
| **Consequência** | Documentação deceptiva que confunde developers. Onboarding guide que não funciona. README que descreve features inexistentes. Perda de confiança na documentação. |
| **Mitigação** | Princípio **code-first, docs-follow**: documentar o que existe, marcar o que está planeado com tag `[PLANEADO]`. Toda a documentação deve ser verificável. |
| **Indicador** | Se um README descreve features que não passam em testes → flag imediata. |
| **Plano B** | Revisão cruzada: developer diferente tenta seguir a documentação e reporta discrepâncias. |

### R-6: Trabalhar Módulos Fora de Ordem Causando Retrabalho

| Dimensão | Detalhe |
|----------|---------|
| **Probabilidade** | MÉDIA |
| **Impacto** | ALTO |
| **Descrição** | Trabalhar em módulos dependentes antes dos módulos fundacionais. Por exemplo, trabalhar em AI Knowledge antes de Identity & Access estar completo, ou trabalhar em Governance antes de Catalog estar estável. |
| **Consequência** | Retrabalho quando módulo fundacional muda. Interfaces instáveis propagam instabilidade. |
| **Mitigação** | Respeitar dependências entre waves e entre módulos. Identity → Catalog → Contracts → restantes. |
| **Indicador** | Se alguém trabalha em módulo X antes de dependência Y estar ≥70% → avaliar risco. |
| **Plano B** | Se dependência atrasa, trabalhar em interfaces/contratos estáveis (interface segregation). |

---

## 4. Riscos MÉDIOS

### R-7: MFA Enforcement Bloqueia Utilizadores Existentes

| Dimensão | Detalhe |
|----------|---------|
| **Probabilidade** | MÉDIA |
| **Impacto** | MÉDIO |
| **Descrição** | Activar enforcement de MFA sem período de transição, bloqueando utilizadores que não configuraram MFA. |
| **Mitigação** | Implementar período de graça de 14 dias. Notificação clara na UI. Bypass administrativo para emergências. |

### R-8: Migrações API Key Causa Downtime

| Dimensão | Detalhe |
|----------|---------|
| **Probabilidade** | BAIXA |
| **Impacto** | MÉDIO |
| **Descrição** | Migração de API keys de appsettings para BD falha ou causa downtime durante transição. |
| **Mitigação** | Dual-read temporário: ler de BD primeiro, fallback para appsettings. Migração gradual com rollback. |

### R-9: RowVersion Causa Breaking Changes em Handlers

| Dimensão | Detalhe |
|----------|---------|
| **Probabilidade** | MÉDIA |
| **Impacto** | MÉDIO |
| **Descrição** | Adicionar RowVersion/ConcurrencyToken em entidades existentes causa DbUpdateConcurrencyException em handlers que não tratam concurrency. |
| **Mitigação** | Adicionar progressivamente, módulo a módulo. Atualizar handlers para incluir RowVersion nos updates. Testes de concorrência por módulo. |

### R-10: AI Tools Runtime Causa Side-Effects Não Previstos

| Dimensão | Detalhe |
|----------|---------|
| **Probabilidade** | ALTA |
| **Impacto** | MÉDIO |
| **Descrição** | Quando tools AI passam de cosmético para funcional, podem executar ações que modificam dados reais sem controlo adequado. |
| **Mitigação** | Sandbox environment para AI tools. Dry-run mode obrigatório como default. Permission checks antes de cada tool execution. Audit logging de todas as execuções. |

### R-11: Traduções i18n Incorrectas em es e pt-BR

| Dimensão | Detalhe |
|----------|---------|
| **Probabilidade** | MÉDIA |
| **Impacto** | MÉDIO |
| **Descrição** | Traduções adicionadas para completar gaps podem ter erros linguísticos ou terminologia inconsistente. |
| **Mitigação** | Revisão por falante nativo antes de merge. Glossário de termos técnicos por locale. |

---

## 5. Riscos BAIXOS

### R-12: Testes E2E Flaky na Wave 5

| Dimensão | Detalhe |
|----------|---------|
| **Probabilidade** | ALTA |
| **Impacto** | BAIXO |
| **Descrição** | Testes E2E adicionados na consolidação final são instáveis devido a timing, estados de BD ou race conditions. |
| **Mitigação** | Retry logic, waits adequados, ambiente de teste dedicado, fixtures de dados determinísticos. |

### R-13: RAG Requer Infraestrutura Vector Database

| Dimensão | Detalhe |
|----------|---------|
| **Probabilidade** | ALTA |
| **Impacto** | BAIXO (diferível) |
| **Descrição** | Implementação de RAG necessita de vector database (pgvector ou serviço externo), adicionando complexidade de infraestrutura. |
| **Mitigação** | Usar pgvector (extensão PostgreSQL, já é o SGBD do projecto). Fallback: keyword search sem embeddings. |

### R-14: API Key dos Providers AI Tem Custos

| Dimensão | Detalhe |
|----------|---------|
| **Probabilidade** | ALTA |
| **Impacto** | BAIXO |
| **Descrição** | Activar providers OpenAI e Azure AI implica custos por token que precisam de governança. |
| **Mitigação** | Token budgets por tenant. Quotas por utilizador. Alertas ao atingir 80% do budget. Default para Ollama (local, gratuito). |

---

## 6. Matriz de Riscos

```
                         IMPACTO
              BAIXO    MÉDIO    ALTO    CRÍTICO
         ┌─────────┬─────────┬─────────┬─────────┐
ALTA     │ R-12    │ R-10    │ R-3     │ R-2     │
         │ R-13    │ R-11    │ R-5     │         │
         │ R-14    │         │         │         │
         ├─────────┼─────────┼─────────┼─────────┤
P  MÉDIA │         │ R-7     │ R-4     │ R-1     │
R        │         │ R-9     │ R-6     │         │
O        │         │         │         │         │
B  ├─────┼─────────┼─────────┼─────────┼─────────┤
A  BAIXA │         │ R-8     │         │         │
B        │         │         │         │         │
.        │         │         │         │         │
         └─────────┴─────────┴─────────┴─────────┘
```

**Zona vermelha (acção imediata):** R-1, R-2  
**Zona laranja (acção planeada):** R-3, R-4, R-5, R-6, R-10  
**Zona amarela (monitorizar):** R-7, R-8, R-9, R-11  
**Zona verde (aceitar):** R-12, R-13, R-14

---

## 7. Plano de Monitorização de Riscos

### Checkpoints de Risco por Wave

| Wave | Riscos a Monitorizar | Checkpoint |
|------|---------------------|-----------|
| Wave 0 | R-3 (menu antes de módulos) | Confirmar que apenas rotas foram corrigidas, sem reestruturação de menu |
| Wave 1 | R-1 (AI antes segurança), R-5 (docs antes código), R-7 (MFA bloqueia users) | Segurança fechada antes de qualquer AI. Docs reflectem estado real |
| Wave 2 | R-2 (frontend antes backend), R-4 (migrações), R-9 (RowVersion) | Backend estável antes de frontend. Migrações criadas |
| Wave 3 | R-2 (frontend), R-6 (módulos fora de ordem), R-11 (traduções) | Frontend conectado a endpoints reais. Traduções revistas |
| Wave 4 | R-1 (AI), R-10 (tools side-effects), R-14 (custos) | Tools com sandbox. Budgets configurados |
| Wave 5 | R-12 (E2E flaky), R-13 (RAG infra) | Testes estáveis. pgvector avaliado |

### Responsabilidades

| Papel | Responsabilidade |
|-------|-----------------|
| Tech Lead | Monitorizar R-1, R-2, R-3 (riscos arquitecturais) |
| Security Lead | Monitorizar R-1, R-7, R-8 (riscos de segurança) |
| DevOps | Monitorizar R-4, R-8, R-13 (riscos de infra) |
| QA Lead | Monitorizar R-12 (testes flaky) |
| Product Owner | Monitorizar R-6, R-14 (riscos de priorização e custos) |

---

## 8. Plano de Contingência

### Se Wave 1 (Segurança) Atrasa >1 Semana
- **Acção:** Bloquear início de Wave 4 (AI)
- **Acção:** Avançar com Wave 2 (Backend/DB) em paralelo
- **Acção:** Documentar gap de segurança explicitamente

### Se Wave 2 (Backend/DB) Atrasa >2 Semanas
- **Acção:** Frontend trabalha com mocks estáveis
- **Acção:** Definir contratos API e gerar stubs
- **Acção:** Não considerar frontend "fechado" até backend estável

### Se Wave 4 (AI) Atrasa >3 Semanas
- **Acção:** Lançar como MVP sem AI tools funcionais
- **Acção:** Chat básico com Ollama é suficiente para MVP
- **Acção:** Marcar AI tools como "Coming Soon" na UI

### Se Maturidade Global Não Atinge 80% após Todas as Waves
- **Acção:** Identificar os 2-3 módulos que puxam média para baixo
- **Acção:** Sprint focado nesses módulos
- **Acção:** Aceitar 75% como MVP se módulos fracos são Product Analytics e Integrations (baixa criticidade)

---

## 9. Lições das Auditorias (Riscos Já Materializados)

As auditorias revelaram que alguns riscos **já se materializaram**:

| Risco | Estado | Evidência |
|-------|--------|-----------|
| Frontend antes de backend | ✅ Materializado | 3 rotas partidas, 7 órfãs, 1 página 0 bytes |
| Docs antes de código | ✅ Materializado | AI docs 65% vs backend 25% |
| Módulos fora de ordem | ✅ Parcial | AI frontend 70% vs backend 25% |
| Segurança diferida | ✅ Parcial | MFA modelado mas não enforced |

**Implicação:** Estes riscos não são teóricos — já aconteceram neste projecto. A mitigação é mais urgente porque o padrão histórico confirma a probabilidade.

---

## 10. Conclusão

Os riscos identificados são **gerenciáveis** se as mitigações forem respeitadas. Os dois riscos críticos (R-1: AI antes de segurança, R-2: frontend antes de backend) já têm evidência histórica de materialização neste projecto, o que torna as mitigações obrigatórias e não opcionais.

**Regra fundamental:** Respeitar a sequência das waves é a melhor mitigação individual. A tentação de "avançar mais rápido" trabalhando em waves posteriores em paralelo é a principal ameaça ao sucesso do plano.

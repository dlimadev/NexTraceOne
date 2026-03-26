# Avaliação Página a Página — NexTraceOne Frontend

> **Data:** 2026-03-26
> **Escopo:** Páginas de alta importância por módulo
> **Legenda de avaliação:** ★★★★★ Excelente · ★★★★☆ Bom · ★★★☆☆ Adequado · ★★☆☆☆ Fraco · ★☆☆☆☆ Muito Fraco

---

## Módulo: Dashboard / Home

### DashboardPage (`/`)

| Dimensão | Avaliação | Detalhe |
|----------|-----------|---------|
| Propósito da página | ★★★★★ | Command center persona-aware — bem definido |
| Persona principal | Engineer / Tech Lead (configurável) | Adaptação por persona implementada |
| Contexto exibido | ★★★★☆ | Serviços, contratos, mudanças, incidentes — contextual e operacional |
| Ação principal | ★★★☆☆ | QuickActions presentes mas KPI cards não são clicáveis |
| Maturidade visual | ★★★★☆ | Boa hierarquia, paleta correta |
| Maturidade funcional | ★★★☆☆ | Dados reais mas drill-down limitado |
| Aderência ao produto | ★★★★☆ | Claramente NexTraceOne — persona label, contexto operacional |
| Aderência ao design system | ★★★★☆ | Usa PageContainer, tokens corretos (exceto `!grid-cols-5`) |
| Problemas de hierarquia visual | Os cards operacionais (serviços/contratos/mudanças/incidentes) têm o mesmo peso visual — sem destaque para situação crítica |
| Problemas de layout | Secção de widgets operacionais (grid 3-item counters) parece tabela de estado sem narrativa |
| Problemas de navegação | KPI cards não navegam para contexto; utilizador precisa usar sidebar para explorar |
| Melhorias sugeridas | Tornar StatCards clicáveis; transformar grid de 3 counters em componentes com tendência e ação |

---

## Módulo: Catálogo de Serviços

### ServiceCatalogListPage (`/services`)

| Dimensão | Avaliação | Detalhe |
|----------|-----------|---------|
| Propósito | ★★★★☆ | Lista de serviços com contexto de domínio, criticidade e equipa |
| Persona principal | Engineer, Tech Lead, Architect |
| Contexto exibido | Serviços, criticidade, equipa, domínio (estimado) |
| Maturidade visual | ★★★☆☆ | Não auditada diretamente — estimada como adequada |
| Problemas conhecidos | Sem split-view (lista + detalhe lateral); filtros podem estar incompletos |
| Melhorias | Adicionar split view; adicionar contagem de incidentes e mudanças por serviço na lista |

---

### ServiceDetailPage (`/services/:serviceId`)

| Dimensão | Avaliação | Detalhe |
|----------|-----------|---------|
| Propósito da página | ★★★★☆ | Detalhe completo de serviço: APIs, contratos, ownership, classificação |
| Persona principal | Engineer, Tech Lead, Architect |
| Contexto exibido | ★★★★☆ | APIs, contratos, ownership, links, incidentes relacionados, IA assistant |
| Ação principal | ★★★☆☆ | Links para source of truth e contratos — sem ações diretas (criar incidente, etc.) |
| Maturidade visual | ★★☆☆☆ | Cabeçalho manual sem EntityHeader; badges com cores hardcoded (IC-001); não usa PageContainer (IC-004) |
| Maturidade funcional | ★★★☆☆ | Funcional mas incompleto — "Recent Changes" mostra apenas link, não dados |
| Aderência ao produto | ★★★☆☆ | Conteúdo correto mas aparência diverge do resto do produto |
| Aderência ao design system | ★★☆☆☆ | 4 mapas de cor inline, sem PageContainer, tabelas sem TableWrapper |
| Problemas de hierarquia visual | Cabeçalho tem badges de tipo + lifecycle + criticidade no mesmo nível horizontal — sem priorização; coluna direita tem 6 cards pequenos empilhados |
| Problemas de layout | Sem tabs — todo o conteúdo em scroll único; a coluna direita com 6 micro-cards fragmenta a informação |
| Problemas de componentes | Ausência de EntityHeader, OwnershipPanel, TimelinePanel |
| Melhorias sugeridas | Usar PageContainer; usar Badge com variantes corretas; criar EntityHeader; organizar coluna direita em OwnershipPanel + ClassificationPanel; adicionar tab de "Mudanças" com timeline real |

---

## Módulo: Gestão de Contratos

### ContractWorkspacePage (`/contracts/studio/:id`)

| Dimensão | Avaliação | Detalhe |
|----------|-----------|---------|
| Propósito | ★★★★★ | Espaço de trabalho completo para edição e governança de contratos |
| Persona principal | Engineer, Tech Lead |
| Contexto exibido | Contrato, versão, protocolo, validação, dependências, aprovações (estimado) |
| Maturidade funcional | ★★★★☆ | 12+ seções de workspace com builders visuais |
| Maturidade visual | ★★★☆☆ | Não auditada diretamente — estrutura complexa |
| Problemas conhecidos | Sem DiffViewer para comparação de versões; sem ReviewPanel para aprovação |
| Melhorias | Adicionar DiffViewer; adicionar ReviewPanel unificado para workflow de aprovação |

---

## Módulo: Change Governance

### ReleasesPage / ChangeCatalogPage (`/releases`, `/changes`)

| Dimensão | Avaliação | Detalhe |
|----------|-----------|---------|
| Propósito | ★★★★☆ | Visão de mudanças/releases com contexto de confiança |
| Persona principal | Tech Lead, Engineer |
| Contexto exibido | Mudanças, confiança, risco, serviço associado |
| Maturidade visual | ★★★☆☆ | Não auditada diretamente |
| Problemas conhecidos | Sem ChangeDetail integrado com correlação de incidentes |
| Melhorias | Integrar ChangeDetail com IncidentDetail para correlação bidirecional |

---

## Módulo: Operações

### IncidentDetailPage (`/operations/incidents/:id`)

| Dimensão | Avaliação | Detalhe |
|----------|-----------|---------|
| Propósito da página | ★★★★★ | Investigação completa de incidente com correlação, evidência e mitigation |
| Persona principal | Engineer, Tech Lead, SRE |
| Contexto exibido | ★★★★★ | Severity, status, correlation, evidence, timeline, impacted services, runbooks, related contracts |
| Ação principal | ★★★★☆ | Refresh correlation disponível; links para serviços e contratos; IA assistant contextual |
| Maturidade visual | ★★★☆☆ | Funcional e informativa, mas cores fora do design system (IC-003) |
| Maturidade funcional | ★★★★★ | A página mais funcionalmente completa do produto |
| Aderência ao produto | ★★★★★ | Exemplifica o NexTraceOne — correlação, evidência, change context |
| Aderência ao design system | ★★★☆☆ | Usa PageContainer, Badge, Card — mas tem cores IC-003 e UUID exposto (IC-010) |
| Problemas de hierarquia visual | 2 colunas de igual peso — Timeline e Correlation têm mesma importância visual que Runbooks |
| Problemas de layout | Layout de 2 colunas iguais não prioriza visualmente o que é mais urgente |
| Problemas de componentes | Timeline inline (IC nas melhorias) em vez de TimelinePanel reutilizável |
| Melhorias sugeridas | Corrigir cores hardcoded; criar TimelinePanel; extrair UUID do display; adicionar EntityHeader com status de severidade em destaque |

---

## Módulo: Governança

### ExecutiveOverviewPage (`/governance/executive`)

| Dimensão | Avaliação | Detalhe |
|----------|-----------|---------|
| Propósito da página | ★★★★☆ | Visão holística executiva de risco, maturidade, mudanças e incidentes |
| Persona principal | Executive, Platform Admin |
| Contexto exibido | ★★★☆☆ | Risco, maturidade, tendência operacional, mudanças, incidentes — mas tudo como números |
| Ação principal | ★★☆☆☆ | Não há ações diretas — apenas leitura de dados |
| Maturidade visual | ★★☆☆☆ | 5 cards empilhados sem hierarquia; sem charts; cores fora do design system (IC-002) |
| Maturidade funcional | ★★★☆☆ | Dados corretos mas modo de fetch inadequado (IC-008) |
| Aderência ao produto | ★★★☆☆ | Conteúdo adequado para Executive, mas apresentação não transmite urgência |
| Aderência ao design system | ★★☆☆☆ | Cores hardcoded, useEffect em vez de useQuery, sem charts apesar de ECharts disponível |
| Problemas de hierarquia visual | Todos os 5 blocos têm o mesmo peso visual — o executivo não sabe o que é mais urgente |
| Problemas de layout | Empilhamento linear de cards sem variação de densidade ou destaque |
| Melhorias sugeridas | Adicionar charts ECharts para tendências; criar secção hero com 3 KPIs críticos em destaque; adicionar painel de "ação imediata"; corrigir cores; migrar para useQuery |

---

### CompliancePage (`/governance/compliance`)

| Dimensão | Avaliação | Detalhe |
|----------|-----------|---------|
| Propósito | ★★★★☆ | Dashboard de compliance com estado de controlos e políticas |
| Persona principal | Auditor, Platform Admin |
| Maturidade visual | ★★★☆☆ (estimada) |
| Maturidade funcional | ★★★☆☆ (estimada) |
| Problemas esperados | Sem ligação clara a evidências específicas; sem caminho de investigação para Auditor |
| Melhorias | Integrar link direto para EvidencePackagesPage; adicionar path de "investigar item não conforme" |

---

### RiskCenterPage (`/governance/risk`)

| Dimensão | Avaliação | Detalhe |
|----------|-----------|---------|
| Propósito | ★★★★☆ | Centro de gestão de risco |
| Persona principal | Tech Lead, Platform Admin, Executive |
| Maturidade visual | ★★★☆☆ (estimada) |
| Problemas esperados | RiskHeatmapPage separada poderia estar integrada como view dentro de RiskCenterPage |
| Melhorias | Integrar heatmap como tab; adicionar RiskBadge padronizado |

---

## Módulo: AI Hub

### AiAssistantPage (`/ai/assistant`)

| Dimensão | Avaliação | Detalhe |
|----------|-----------|---------|
| Propósito | ★★★★☆ | Assistente IA com contexto de governança |
| Persona principal | Engineer, Tech Lead |
| Contexto exibido | Contexto injetado via `AssistantPanel` pattern |
| Maturidade visual | ★★★☆☆ (estimada) |
| Problemas | Risco de parecer "chat genérico com LLM" sem diferenciação clara de contexto enterprise |
| Melhorias | O contexto operacional (serviço, incidente, ambiente) deve ser visível ao utilizador — não apenas injetado internamente |

---

## Módulo: Identidade e Acesso

### LoginPage (`/login`)

| Dimensão | Avaliação | Detalhe |
|----------|-----------|---------|
| Propósito | ★★★★★ | Autenticação com fluxo MFA e seleção de tenant |
| Maturidade visual | ★★★★☆ (estimada com AuthCard/AuthShell) |
| Maturidade funcional | ★★★★★ | Login, MFA, forgot password, activation, invitation — fluxo completo |
| Aderência ao produto | ★★★★☆ | AuthCard e AuthShell são específicos e profissionais |
| Melhorias | Verificar que o brand gradient e identidade NexTraceOne são visíveis na página de login |

---

## Módulo: Integrações

### IntegrationHubPage (`/integrations`)

| Dimensão | Avaliação | Detalhe |
|----------|-----------|---------|
| Propósito | ★★★★☆ | Hub central de conectores e ingestão |
| Persona principal | Platform Admin |
| Maturidade funcional | ★★★☆☆ (estimada) |
| Problemas esperados | IngestionFreshnessPage e IngestionExecutionsPage são operacionais mas podem parecer técnicas demais sem contexto de negócio |
| Melhorias | Adicionar indicador de saúde por integração na lista; mostrar último sucesso/falha |

---

## Resumo Executivo de Páginas

| Página | Maturidade Visual | Maturidade Funcional | Aderência DS | Prioridade de Melhoria |
|--------|------------------|---------------------|--------------|----------------------|
| DashboardPage | ★★★★☆ | ★★★☆☆ | ★★★★☆ | Média |
| ServiceDetailPage | ★★☆☆☆ | ★★★☆☆ | ★★☆☆☆ | **Alta** |
| IncidentDetailPage | ★★★☆☆ | ★★★★★ | ★★★☆☆ | Média |
| ExecutiveOverviewPage | ★★☆☆☆ | ★★★☆☆ | ★★☆☆☆ | **Alta** |
| ContractWorkspacePage | ★★★☆☆ | ★★★★☆ | ★★★☆☆ | Média |
| LoginPage | ★★★★☆ | ★★★★★ | ★★★★☆ | Baixa |
| AiAssistantPage | ★★★☆☆ | ★★★★☆ | ★★★☆☆ | Média |

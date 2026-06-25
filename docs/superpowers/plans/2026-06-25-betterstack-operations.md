# Operations — passe v5 (controlos->DS) — Plan

Fase 1 do roadmap (docs/.../2026-06-24-betterstack-rollout-roadmap.md). Padrão e convenções: ver roadmap
+ ciclos 14-16. Uma tarefa por página; gate por página = teste da página + lint/build + grep limpo.
EXCLUÍDAS (trace refactor, deferidas): TraceExplorerPage, RequestExplorerPage.

## Conversion Reference (resumo)
Controlos crus -> DS de `shared/ui` (Button/IconButton/TextField/TextArea/Select/SearchInput/Tabs/Toggle).
Cores legacy/`dark:` -> tokens (red->critical, yellow/amber->warning, green/emerald->success, blue->accent,
bg-blue-X text-white -> Button primary). Link-as-button -> useNavigate (nunca <Link><Button>). Button faz
disabled||loading interno. Select sem optgroup (achatar); opcao vazia selecionavel = {value:'',label} em options.
SearchInput -> role searchbox. IconButton/Toggle exigem label. required -> asterisco manual no label.
Taxonomias intencionais (severidade/tipo, sem token) ficam raw + comentario. Preservar queries/mutations/
onChange/disabled/i18n. Surgical: git add so o ficheiro.

## Vagas (26 paginas, todas com teste)
- W1 Incidents/SRE: Incidents, IncidentDetail, IncidentTimeline, PostIncident, SreDashboard, TeamReliability
- W2 SLO/Reliability: ReliabilitySloManagement, SloBurnRate, SloMarketplace, DependencyRisk, ApiRegression
- W3 Explorers/monitoring: LogExplorer, DbExplorer, ProfilingExplorer, SyntheticMonitoring, EnvironmentComparison
- W4 Testing/chaos/platform: ChaosEngineering, LoadTesting, PlatformOperations, AutomationWorkflows
- W5 Builders/AI: RunbookBuilder, CustomChartBuilder, PredictiveIntelligence, AiRunbookSuggester, AiIncidentSummarizer, AiAnomaly

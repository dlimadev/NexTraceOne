# 01 — O que fica no PostgreSQL

PostgreSQL é o **sistema de registo** (system of record) para todos os dados transaccionais
e de domínio. Tudo o que precisa de ACID, FK, Outbox ou consistência imediata fica aqui.

---

## Módulo: Identity & Access (prefix `iam_`)

| Tabela | Justificação |
|--------|-------------|
| `iam_tenants` | Core do sistema multi-tenant — FK em todos os módulos |
| `iam_users` | Identidade — lida em cada request autenticado |
| `iam_roles` / `iam_permissions` | RBAC — validado em cada endpoint |
| `iam_sessions` | Sessões activas — TTL gerido por expiração de negócio |
| `iam_tenant_memberships` | Relação Tenant↔User — FK crítica |
| `iam_external_identities` | SSO/OIDC — precisa de lookup rápido por subject |
| `iam_sso_group_mappings` | Mapeamento de grupos SSO para roles |
| `iam_break_glass_requests` | Auditoria de acesso de emergência — ACID obrigatório |
| `iam_jit_access_requests` | Just-in-time access — workflow com aprovação |
| `iam_delegations` | Delegações de permissão — estado transaccional |
| `iam_access_review_campaigns` | Campanhas de revisão de acesso |
| `iam_access_review_items` | Items de revisão — state machine |
| `iam_environments` | Definição de ambientes — referenciados em todo o domínio |
| `iam_platform_api_tokens` | Tokens de API — lookup por hash em cada request |
| `iam_policy_definitions` | Políticas de autorização — lidas no middleware |
| `iam_tenant_licenses` | Licenças de tenant — verificadas em operações billing |
| `iam_agent_registrations` | Registo de agentes externos — FK para execuções |
| `iam_module_access_policies` | Políticas de acesso por módulo |

**⚠️ Migrar para ClickHouse (ver 02):**
- `iam_security_events` — log de eventos de segurança (volume alto, append-only)
- `iam_agent_query_records` — log de queries de agente IA (append-only)
- `iam_alert_firing_records` — histórico de alertas disparados (série temporal)

---

## Módulo: Configuration (prefix `cfg_`)

| Tabela | Justificação |
|--------|-------------|
| `cfg_configuration_definitions` | Catálogo de 949 chaves — lido no arranque |
| `cfg_configuration_entries` | Valores por scope — lidos em cada feature |
| `cfg_configuration_audit_entries` | Auditoria de mudanças de config — FK para definitions |
| `cfg_modules` | Definição de módulos da plataforma |
| `cfg_feature_flag_definitions` | Definição de feature flags |
| `cfg_feature_flag_entries` | Estado de feature flags por scope |
| `cfg_user_saved_views` | Views personalizadas por utilizador |
| `cfg_user_bookmarks` | Bookmarks de utilizador |
| `cfg_user_watches` | Watch lists de utilizador |
| `cfg_user_alert_rules` | Regras de alerta configuradas pelo utilizador |
| `cfg_entity_tags` | Tags aplicadas a entidades |
| `cfg_service_custom_fields` | Campos custom por serviço |
| `cfg_taxonomy_categories` / `cfg_taxonomy_values` | Taxonomia — hierárquica, FK |
| `cfg_automation_rules` | Regras de automação |
| `cfg_change_checklists` | Checklists de mudança |
| `cfg_contract_templates` | Templates de contrato |
| `cfg_scheduled_reports` | Relatórios agendados |
| `cfg_saved_prompts` | Prompts guardados pelo utilizador |
| `cfg_webhook_templates` | Templates de webhook |
| `cfg_contract_compliance_policies` | Políticas de conformidade de contratos |

---

## Módulo: Catalog — Contracts (prefix `ctr_`)

| Tabela | Justificação |
|--------|-------------|
| `ctr_contract_versions` | Versão de contrato — estado e metadados (conteúdo vai para ES) |
| `ctr_contract_diffs` | Diffs entre versões — referenciam versões por FK |
| `ctr_contract_rule_violations` | Violações de regras — associadas a versões |
| `ctr_contract_artifacts` | Artefactos de contrato |
| `ctr_drafts` / `ctr_reviews` | State machine de revisão |
| `ctr_contract_lint_rulesets` | Rulesets de linting |
| `ctr_canonical_entities` | Entidades canónicas — referências de domínio |
| `ctr_contract_evidence_packs` | Evidence packs — ligados a releases por FK |
| `ctr_contract_compliance_gates` | Gates de conformidade |
| `ctr_contract_negotiations` | Negociações de contrato — state machine |
| `ctr_schema_evolution_advices` | Conselhos de evolução de schema |
| `ctr_breaking_change_proposals` | Propostas de breaking change — workflow |
| `ctr_contract_consumer_inventories` | Inventário de consumidores (master) |
| `ctr_graphql_schema_snapshots` | Snapshots GraphQL — referenciados por FK |
| `ctr_protobuf_schema_snapshots` | Snapshots Protobuf |
| `ctr_data_contract_schemas` | Schemas de data contract |

**⚠️ Dual-write (conteúdo para ES):**
- `ctr_contract_versions.spec_content` — conteúdo YAML/JSON do contrato → ES para search

---

## Módulo: Catalog — Graph (prefix `cat_`)

| Tabela | Justificação |
|--------|-------------|
| `cat_api_assets` | Activos de API — referência central do catálogo |
| `cat_service_assets` | Serviços — entidade central |
| `cat_consumer_relationships` | Relações consumidor↔produtor — FK crítica |
| `cat_consumer_assets` | Activos consumidores |
| `cat_discovery_sources` | Fontes de descoberta |
| `cat_saved_graph_views` | Views guardadas do grafo |
| `cat_linked_references` | Referências ligadas |
| `cat_service_links` | Links entre serviços |
| `cat_discovered_services` | Serviços descobertos (master) |
| `cat_discovery_runs` | Runs de descoberta — estado |
| `cat_discovery_match_rules` | Regras de matching |
| `cat_service_interfaces` | Interfaces de serviço |
| `cat_contract_bindings` | Ligações contrato↔serviço |
| `cat_dx_scores` | Scores DX por serviço — calculados e guardados |

**⚠️ Migrar para ClickHouse:**
- `cat_productivity_snapshots` — série temporal de produtividade por developer

**⚠️ Migrar para Elasticsearch:**
- `cat_discovered_services` (conteúdo) — search por nome/tecnologia/equipa

---

## Módulo: Change Governance (prefix `chg_`)

| Tabela | Justificação |
|--------|-------------|
| `chg_releases` | Releases — agregado principal, FK em tudo |
| `chg_blast_radius_reports` | Relatórios de blast radius — ligados a releases |
| `chg_change_scores` | Scores de mudança — calculados por release |
| `chg_change_events` | Eventos de mudança — estado transaccional |
| `chg_freeze_windows` | Janelas de freeze — verificadas em aprovações |
| `chg_release_baselines` | Baselines de release |
| `chg_observation_windows` | Janelas de observação |
| `chg_post_release_reviews` | Revisões pós-release — formal |
| `chg_rollback_assessments` | Avaliações de rollback |
| `chg_canary_rollouts` | Canary rollouts — state machine |
| `chg_release_notes` | Notas de release |
| `chg_promotion_gates` | Gates de promoção |
| `chg_approval_requests` / `chg_approval_policies` | Workflow de aprovação |
| `chg_external_change_requests` | Pedidos externos |
| `chg_benchmark_consents` | Consentimento para benchmarking cross-tenant |
| `chg_release_calendar_entries` | Calendário de releases |
| `chg_service_risk_profiles` | Perfis de risco por serviço |
| `chg_deployment_environments` | Ambientes de deploy — referência |
| `chg_promotion_requests` | Pedidos de promoção — workflow |

**⚠️ Migrar para ClickHouse:**
- `chg_benchmark_snapshots` — snapshots de DORA cross-tenant (série temporal)
- `chg_change_confidence_events` — log de eventos de confiança (append-only)

---

## Módulo: Operational Intelligence (prefix `ops_`)

| Tabela | Justificação |
|--------|-------------|
| `ops_incidents` | Incidentes — agregado principal com estado |
| `ops_mitigation_workflows` | Workflows de mitigação — state machine |
| `ops_runbooks` | Runbooks — conteúdo referenciado por FK |
| `ops_post_incident_reviews` | Post-mortems — formais, ligados a incidentes |
| `ops_slo_definitions` | Definições SLO — referência |
| `ops_sla_definitions` | Definições SLA |
| `ops_service_failure_predictions` | Predições — estado por serviço |
| `ops_capacity_forecasts` | Forecasts de capacidade |
| `ops_chaos_experiments` | Experimentos de chaos — state machine |
| `ops_operational_playbooks` | Playbooks operacionais |
| `ops_playbook_executions` | Execuções de playbook — estado |
| `ops_investigation_contexts` | Contextos de investigação |
| `ops_cost_snapshots` | Snapshots de custo por período |
| `ops_cost_attributions` | Atribuições de custo — FK para serviços |
| `ops_cost_trends` | Tendências de custo (calculadas) |
| `ops_service_cost_profiles` | Perfis de custo por serviço |
| `ops_budget_forecasts` | Forecasts de budget |
| `ops_efficiency_recommendations` | Recomendações de eficiência |

**⚠️ Migrar para ClickHouse (ver 02):**
- `ops_service_metrics_snapshots`
- `ops_dependency_metrics_snapshots`
- `ops_burn_rate_snapshots`
- `ops_error_budget_snapshots`
- `ops_reliability_snapshots`
- `ops_runtime_snapshots`
- `ops_cost_records`
- `ops_waste_signals`
- `ops_slo_observations`
- `ops_anomaly_snapshots`
- `ops_telemetry_references`
- `ops_release_runtime_correlations`

---

## Módulo: AIKnowledge (prefix `aik_`)

| Tabela | Justificação |
|--------|-------------|
| `aik_ai_models` | Registo de modelos de IA — referência |
| `aik_access_policies` | Políticas de acesso a IA |
| `aik_budgets` | Orçamentos de tokens por tenant |
| `aik_token_quota_policies` | Políticas de quota |
| `aik_conversations` | Conversas de assistente — estado |
| `aik_messages` | Mensagens — ligadas a conversas por FK |
| `aik_agents` | Agentes de IA — registo |
| `aik_agent_executions` | Execuções de agente — estado |
| `aik_agent_artifacts` | Artefactos produzidos por agentes |
| `aik_guardrails` | Guardrails de IA |
| `aik_evaluations` | Avaliações de modelos |
| `aik_evaluation_suites` | Suites de avaliação |
| `aik_external_data_sources` | Fontes de dados externas RAG |
| `aik_prompt_assets` | Registo de prompt assets |
| `aik_prompt_versions` | Versões de prompts |
| `aik_memory_nodes` | Nós de memória organizacional (+ pgvector para embeddings) |
| `aik_skills` | Skills registadas |
| `aik_routing_decisions` | Decisões de routing (master) |
| `aik_war_rooms` | War rooms — state machine |
| `aik_self_healing_actions` | Acções de self-healing — workflow |

**⚠️ Migrar para ClickHouse:**
- `aik_token_usage_ledger` — ledger de tokens (append-only, alto volume)
- `aik_external_inference_records` — log de inferências externas
- `aik_model_prediction_samples` — amostras de predição de modelos
- `aik_agent_performance_metrics` — métricas periódicas de agentes
- `aik_agent_trajectory_feedbacks` — feedbacks de trajectória

**⚠️ Migrar para Elasticsearch:**
- `aik_memory_nodes` (conteúdo/título) — search semântico (manter pgvector para embeddings)

---

## Módulo: Audit & Compliance (prefix `aud_`)

| Tabela | Justificação |
|--------|-------------|
| `aud_audit_events` | Eventos de auditoria — **FICA NO PG** (chain-of-custody, hashing) |
| `aud_audit_chain_links` | Links da cadeia de hash — integridade criptográfica |
| `aud_retention_policies` | Políticas de retenção |
| `aud_compliance_policies` | Políticas de conformidade |
| `aud_audit_campaigns` | Campanhas de auditoria |
| `aud_compliance_results` | Resultados de conformidade |

> **Nota crítica**: `aud_audit_events` e `aud_audit_chain_links` **nunca** devem ser movidas para
> ClickHouse ou Elasticsearch. A cadeia de hashes garante imutabilidade verificável — esta garantia
> só existe com PostgreSQL ACID. Uma cópia no Elasticsearch pode existir para search, mas o
> PostgreSQL é a única fonte de verdade para integridade.

---

## Módulo: Governance (prefix `gov_`)

| Tabela | Justificação |
|--------|-------------|
| `gov_teams` / `gov_domains` | Organização — FK em todo o sistema |
| `gov_packs` / `gov_pack_versions` | Governance packs — versionados |
| `gov_waivers` | Waivers de conformidade — state machine |
| `gov_delegated_administrations` | Delegações de admin |
| `gov_evidence_packages` / `gov_evidence_items` | Evidence packs — ligados a releases |
| `gov_compliance_gaps` | Gaps identificados |
| `gov_policy_as_code_definitions` | Políticas como código |
| `gov_security_scan_results` | Resultados de scans — por release |
| `gov_technical_debt_items` | Items de dívida técnica |
| `gov_service_maturity_assessments` | Avaliações de maturidade |
| `gov_custom_dashboards` | Dashboards personalizados |
| `gov_notebooks` | Notebooks de análise |
| `gov_scheduled_dashboard_reports` | Relatórios agendados |
| `gov_presence_sessions` | Sessões de presença para collaboration |
| `gov_dashboard_monitors` | Monitores de dashboard |
| `gov_saml_sso_configurations` | Configurações SSO |

**⚠️ Migrar para ClickHouse:**
- `gov_dashboard_usage_events` — stream de uso de dashboards (append-only)
- `gov_dashboard_revisions` — histórico de revisões (append-only)
- `gov_team_health_snapshots` — snapshots temporais de saúde de equipa

---

## Módulo: Knowledge (prefix `knw_`)

| Tabela | Justificação |
|--------|-------------|
| `knw_knowledge_documents` | Documentos — master (conteúdo vai para ES) |
| `knw_operational_notes` | Notas operacionais — ligadas a incidentes |
| `knw_knowledge_relations` | Relações entre documentos — grafo |
| `knw_knowledge_graph_snapshots` | Snapshots do grafo |
| `knw_proposed_runbooks` | Runbooks propostos — state machine |

---

## Módulo: Notifications, Integrations (prefix `ntf_`, `int_`)

| Tabela | Justificação |
|--------|-------------|
| `ntf_notifications` | Notificações — estado de entrega |
| `ntf_deliveries` | Entregas — rastreio por notificação |
| `ntf_preferences` | Preferências por utilizador |
| `ntf_templates` | Templates de notificação |
| `ntf_channel_configurations` | Configuração de canais |
| `int_integration_connectors` | Conectores — configuração |
| `int_ingestion_sources` | Fontes de ingestão |
| `int_ingestion_executions` | Execuções — estado |
| `int_webhook_subscriptions` | Subscrições de webhook |
| `int_tenant_pipeline_rules` | Regras de pipeline por tenant |

---

## Resumo: contagem por módulo

| Módulo | Tabelas em PG | Migrar CH | Migrar ES | Dual-write |
|--------|--------------|-----------|-----------|------------|
| IAM | 16 | 3 | 0 | 0 |
| Configuration | 19 | 0 | 0 | 0 |
| Catalog/Contracts | 16 | 0 | 1 (spec) | 1 |
| Catalog/Graph | 13 | 1 | 1 | 0 |
| Change Governance | 20 | 2 | 0 | 0 |
| Operational Intel. | 19 | 12 | 0 | 0 |
| AIKnowledge | 21 | 5 | 1 | 1 |
| Audit/Compliance | 6 | 0 | 0 | 0 |
| Governance | 17 | 3 | 0 | 0 |
| Knowledge | 5 | 0 | 1 | 1 |
| Notifications/Integr. | 10 | 0 | 0 | 0 |
| Product Analytics | 2 | 1 | 0 | 0 |
| **Total** | **164** | **27** | **4** | **3** |

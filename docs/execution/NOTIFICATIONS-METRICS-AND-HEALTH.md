# Notifications — Metrics & Health

## Métricas Implementadas

### Métricas Operacionais (Platform Metrics)
| Métrica | Descrição | Agrupamento |
|---------|-----------|-------------|
| TotalGenerated | Notificações geradas no período | Por tenant + período |
| ByCategory | Contagem por categoria funcional | Incident, Approval, Change, etc. |
| BySeverity | Contagem por severidade | Info, ActionRequired, Warning, Critical |
| BySourceModule | Contagem por módulo de origem | OperationalIntelligence, Catalog, etc. |
| DeliveriesByChannel | Deliveries por canal externo | Email, MicrosoftTeams |
| TotalDelivered | Entregas com sucesso | Total |
| TotalFailed | Entregas falhadas | Total |
| TotalPending | Entregas pendentes | Total |
| TotalSkipped | Entregas ignoradas | Total |

### Métricas de Interação (Interaction Metrics)
| Métrica | Descrição |
|---------|-----------|
| TotalRead | Notificações lidas (read + acknowledged + archived) |
| TotalUnread | Notificações não lidas |
| TotalAcknowledged | Acknowledged explicitamente |
| TotalSnoozed | Adiadas via snooze |
| TotalArchived | Arquivadas |
| TotalDismissed | Descartadas |
| TotalEscalated | Escaladas |
| ReadRate | read / total gerado |
| AcknowledgeRate | acknowledged / total que requer ação |

### Métricas de Qualidade (Quality Metrics)
| Métrica | Descrição |
|---------|-----------|
| AveragePerUserPerDay | Média diária por utilizador |
| TotalSuppressed | Notificações suprimidas |
| TotalGrouped | Com GroupId (agrupadas) |
| TotalCorrelatedWithIncidents | Vinculadas a incidentes |
| TopNoisyTypes | Top 5 tipos mais volumosos |
| LeastEngagedTypes | Top 5 tipos menos lidos |

## Health

### Componentes Verificados
| Componente | Verificação | Thresholds |
|-----------|-------------|------------|
| InAppStore | Conectividade da base de dados | Can connect = Healthy |
| DeliveryPipeline | Backlog de deliveries pendentes | > 100 pending = Degraded |
| EmailChannel | Falhas recentes (60 min) | ≥ 10 failures = Degraded |
| TeamsChannel | Falhas recentes (60 min) | ≥ 10 failures = Degraded |

### Status Possíveis
- **Healthy** — Componente operacional, sem problemas detectados
- **Degraded** — Operacional com degradação (backlog alto ou falhas recentes)
- **Unhealthy** — Indisponível ou com falha crítica

### Regras de Agregação
- Overall = Unhealthy se qualquer componente Unhealthy
- Overall = Degraded se qualquer componente Degraded (e nenhum Unhealthy)
- Overall = Healthy apenas se todos os componentes Healthy

## Troubleshooting

### Cenários Comuns
| Problema | Diagnóstico |
|----------|-------------|
| Email não entregue | Verificar EmailChannel health + delivery log |
| Teams não entregue | Verificar TeamsChannel health + webhook config |
| Backlog crescente | Verificar DeliveryPipeline pending count |
| Store indisponível | Verificar InAppStore connectivity |
| Notificações não geradas | Verificar métricas TotalGenerated = 0 |

## Limitações
- Métricas são calculadas em tempo real (consulta ao banco), não pré-agregadas
- Health verifica estado actual, não histórico
- Troubleshooting depende de connectivity com a base de dados
- Métricas são scoped por tenant — não há visão cross-tenant

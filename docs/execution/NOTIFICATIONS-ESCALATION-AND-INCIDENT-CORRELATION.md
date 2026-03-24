# Notifications — Escalation & Incident Correlation

## Escalação

### Critérios
| Severidade | Condição | Threshold | Resultado |
|-----------|----------|-----------|-----------|
| Critical | Não acknowledged | > 30 minutos | Escalável |
| ActionRequired | RequiresAction + não acknowledged | > 2 horas | Escalável |
| Warning | — | — | Não escalável |
| Info | — | — | Não escalável |

### Guards (Não Escalar Se)
| Condição | Resultado |
|----------|-----------|
| Já escalado | Não escalar (idempotente) |
| Acknowledged | Não escalar |
| Archived | Não escalar |
| Dismissed | Não escalar |
| Snoozed activo | Não escalar |

### Acções de Escalação
1. Marca notificação como `IsEscalated = true`
2. Regista `EscalatedAt` para auditoria
3. Log warning para rastreabilidade
4. Futuro: reenvio por canal mais forte, ampliação de destinatários

### Safeguards
- Sem loops de escalação (idempotente por design)
- Thresholds claros e previsíveis
- Escalação é rastreável e auditável
- Snooze bloqueia escalação (decisão consciente do utilizador)

## Correlação com Incidentes

### Modelo
| Campo | Tipo | Descrição |
|-------|------|-----------|
| CorrelatedIncidentId | Guid? | Id do incidente vinculado |

### Cenários de Correlação
| Cenário | Acção |
|---------|-------|
| Notificação crítica vinculada manualmente | `CorrelateWithIncident(incidentId)` |
| Health degradation persistente | Candidato a automação |
| Worker failure repetitivo | Candidato a automação |
| Integração crítica indisponível | Candidato a automação |

### Automação Controlada (Base)
A Fase 6 estabelece a base para automação controlada:
- Campo `CorrelatedIncidentId` permite vinculação explícita
- Lógica de verificação de duplicação de incidente (evita criar duplicados)
- Rastreabilidade completa

### Regras
- Não criar incidente para tudo
- Automação deve ser opt-in/configurada por regra
- Criação automática deve ser rastreável e auditável
- Cada correlação é explícita (sem inferência silenciosa)

### Futuro (Fase 7+)
- Worker de automação para criação de incidentes
- Regras configuráveis de auto-incident
- Enriquecimento de incidentes com contexto de notificação
- Dashboard de correlação notificação-incidente

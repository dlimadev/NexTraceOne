# Notifications — Acknowledge, Snooze & Digest

## Acknowledge

### Estados
O acknowledge é um estado operacional distinto de "lido":

| Transição | Efeito |
|-----------|--------|
| Unread → Acknowledged | Sets AcknowledgedAt, AcknowledgedBy, ReadAt (implícito) |
| Read → Acknowledged | Sets AcknowledgedAt, AcknowledgedBy |
| Archived → Acknowledged | Bloqueado (sem efeito) |
| Dismissed → Acknowledged | Bloqueado (sem efeito) |

### Campos
- `AcknowledgedAt` — data/hora UTC do acknowledge
- `AcknowledgedBy` — Guid do utilizador (opcional para retrocompatibilidade)
- `AcknowledgeComment` — nota textual opcional

### Regras Operacionais
- Acknowledge não é resolução; é reconhecimento
- Acknowledge não substitui leitura; é um nível acima
- Eventos obrigatórios podem exigir acknowledge explícito
- Acknowledge impede escalação automática

### Limitações
- Nesta fase, sem workflow de acknowledge em cadeia
- Sem notificação de acknowledge para terceiros (futuro)

## Snooze

### Campos
- `SnoozedUntil` — data/hora UTC até à qual a notificação está adiada
- `SnoozedBy` — Guid do utilizador que fez snooze

### Durações Recomendadas
| Opção | Duração |
|-------|---------|
| 1 hora | +1h |
| 4 horas | +4h |
| Amanhã | Próximo dia útil 09:00 UTC |
| Custom | Data/hora escolhida |

### Regras Operacionais
- Notificações archived ou dismissed NÃO podem ser snoozed
- Snooze previne escalação enquanto activo
- Snooze previne supressão de nova notificação do mesmo tipo
- `IsSnoozed()` verifica semanticamente se snooze está activo
- `Unsnooze()` limpa campos de snooze (reactivação)

### Limitações
- Nesta fase, sem auto-unsnooze por worker (futuro)
- Sem repetição de snooze automática

## Digest

### Elegibilidade
| Severidade | Elegível para Digest |
|-----------|---------------------|
| Info | ✅ Sim |
| ActionRequired | ✅ Sim |
| Warning | ❌ Não |
| Critical | ❌ Não |

### Comportamento
- Janela: últimas 24 horas
- Agrupamento: por categoria (Incident, Approval, Contract, etc.)
- Apenas notificações Unread e não suprimidas
- Gera resumo textual: "You have 15 pending notification(s): Incident: 5, Approval: 3, Contract: 2..."

### Regras
- Digest não substitui notificação imediata de alta severidade
- Digest é opt-in (futuro: configurável por utilizador)
- Eventos Critical/Warning são SEMPRE entregues imediatamente

### Limitações
- Nesta fase, sem entrega automática de digest por email (serviço gera, mas delivery é manual/API)
- Sem configuração de frequência por utilizador (futuro)

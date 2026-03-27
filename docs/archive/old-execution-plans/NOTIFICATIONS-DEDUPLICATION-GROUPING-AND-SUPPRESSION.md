# Notifications — Deduplication, Grouping & Suppression

## Deduplicação Avançada

### Chave de Deduplicação
A deduplicação básica (Fase 2) verifica: `tenant + recipient + eventType + sourceEntityId` numa janela de 5 minutos.

A deduplicação avançada (Fase 6) adiciona:
- **Chave de correlação**: `tenant|module|eventType|entityType|entityId`
- **Contagem de ocorrências**: Em vez de descartar duplicatas, incrementa `OccurrenceCount`
- **Timestamp actualizado**: `LastOccurrenceAt` reflecte a última ocorrência

### Comportamentos
| Cenário | Resultado |
|---------|-----------|
| Primeiro evento | Criar notificação nova |
| Evento duplicado na janela | Incrementar `OccurrenceCount` OU skip |
| Evento duplicado fora da janela | Criar notificação nova |
| Evento com entidade diferente | Criar notificação nova |
| Evento para destinatário diferente | Criar notificação nova |

### Regras
- Eventos semanticamente diferentes nunca são deduplicados
- A chave é determinística e reproduzível
- Multi-tenant: deduplicação é sempre scoped ao tenant

## Agrupamento e Correlação

### CorrelationKey
Chave gerada automaticamente para agrupar notificações relacionadas:
```
{tenantId}|{sourceModule}|{eventType}[|{sourceEntityType}][|{sourceEntityId}]
```

### GroupId
UUID partilhado entre notificações do mesmo grupo. Resolvido automaticamente:
1. Procura grupo existente com a mesma `CorrelationKey` na janela (60 min padrão)
2. Se existe: reutiliza o `GroupId`
3. Se não existe: gera novo `GroupId`

### Casos de Uso
- Múltiplos incidentes do mesmo serviço → mesmo grupo
- Múltiplas falhas da mesma integração → mesmo grupo
- Múltiplos budgets do mesmo domínio → mesmo grupo

### Regras
- Agrupamento não apaga gravidade
- Eventos críticos correlatos mantêm rastreabilidade individual
- Cada notificação tem seu próprio Id, mas partilha GroupId

## Supressão Básica

### Regras de Supressão
| # | Regra | Condição |
|---|-------|----------|
| 1 | Acknowledged recente | Mesmo tipo+entidade acknowledged nos últimos 30 min |
| 2 | Snooze activo | Mesmo tipo+entidade com snooze activo |

### Safeguards
- **Notificações Critical**: NUNCA suprimidas
- **Notificações obrigatórias**: NUNCA suprimidas (BreakGlass, Approval, Compliance)
- Supressão é explícita: `IsSuppressed` + `SuppressionReason` para auditabilidade
- Supressão coexiste com deduplicação (são complementares, não substitutas)

### Rationale
A supressão reduz ruído adicional sem perder eventos importantes. Cada supressão é rastreável e reversível por design.

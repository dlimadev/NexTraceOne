# Política de Canais de Notificação

## Visão Geral

A decisão de canal no NexTraceOne combina três fontes:

1. **Política obrigatória** (MandatoryNotificationPolicy)
2. **Regras de severidade** (padrão da plataforma)
3. **Preferências do utilizador** (NotificationPreferenceService)
4. **Disponibilidade de infraestrutura** (NotificationChannelOptions)

## Fluxo de Decisão

```
Notificação recebida
    │
    ├── 1. É obrigatória? → Canais obrigatórios SEMPRE incluídos
    │
    ├── 2. Regras de severidade → Canais elegíveis por padrão
    │
    ├── 3. Preferências do utilizador → Filtra canais não obrigatórios
    │
    └── 4. Infraestrutura disponível? → Remove canais desabilitados
    │
    ▼
Lista final de canais
```

## Regras por Severidade

| Severidade | InApp | Email | Teams |
|------------|:-----:|:-----:|:-----:|
| Info | ✅ | — | — |
| ActionRequired | ✅ | ✅ | — |
| Warning | ✅ | ✅ | ✅ |
| Critical | ✅ | ✅ | ✅ |

## Notificações Obrigatórias (Non-Opt-Out)

### Regras implementadas:

| Evento | Canais Obrigatórios | Razão |
|--------|-------------------|-------|
| BreakGlassActivated | InApp + Email + Teams | Segurança crítica — acesso emergencial |
| IncidentCreated (Critical) | InApp + Email + Teams | Incidente operacional urgente |
| IncidentEscalated (Critical) | InApp + Email + Teams | Escalação de severidade |
| ApprovalPending | InApp + Email | Ação obrigatória do aprovador |
| ComplianceCheckFailed | InApp + Email | Violação de governança |
| Qualquer evento Critical | InApp + Email | Mínimo para severidade máxima |

### Comportamento:
- Canais obrigatórios são incluídos INDEPENDENTEMENTE das preferências
- O utilizador NÃO pode desativar canais obrigatórios via API
- A tentativa de desativar retorna erro de validação
- Na UI, canais obrigatórios aparecem com ícone de cadeado

## Interação com Preferências

### Canal não obrigatório:
```
Severidade sugere Email? → Utilizador habilitou Email? → SIM → Entrega por Email
                                                       → NÃO → Não entrega
```

### Canal obrigatório:
```
Evento é obrigatório? → SIM → Entrega SEMPRE (ignora preferência)
```

### InApp:
```
Sempre incluído (não pode ser desativado)
```

## Configuração de Infraestrutura

Canais podem ser desabilitados globalmente via configuração:

```json
{
  "Notifications": {
    "Channels": {
      "Email": { "Enabled": true, "SmtpHost": "..." },
      "Teams": { "Enabled": true, "WebhookUrl": "..." }
    }
  }
}
```

Se um canal está desabilitado na infraestrutura, mesmo canais obrigatórios não são entregues por esse canal (não há como entregar sem infraestrutura).

## Auditabilidade

Cada decisão de roteamento é logada:
```
Routing resolved for user {UserId}, severity {Severity}, category {Category}: [{Channels}] (mandatory: [{MandatoryChannels}])
```

## Evolução Futura

- Regras por tenant (admins podem personalizar política)
- Quiet hours (supressão temporária de canais externos)
- Digest (agrupamento de notificações de baixa prioridade)
- Canais adicionais (Slack, SMS, Webhook genérico)

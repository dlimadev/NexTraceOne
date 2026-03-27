# P7.1 — Post-Change Gap Report

## O que foi resolvido nesta fase

| Item | Estado |
|---|---|
| `NotificationTemplate` entity persistida | ✅ Implementado |
| `DeliveryChannelConfiguration` entity persistida | ✅ Implementado |
| `SmtpConfiguration` entity persistida | ✅ Implementado |
| NotificationsDbContext expandido de 3 para 6 entidades | ✅ Implementado |
| Mappings EF Core para as 3 novas entidades | ✅ Implementado |
| Migration `P7_1_NotificationsExpansion` | ✅ Gerada |
| Repositórios: `NotificationTemplateRepository`, `DeliveryChannelConfigurationRepository`, `SmtpConfigurationRepository` | ✅ Implementados |
| Handlers CQRS: `ListTemplates`, `UpsertTemplate`, `ListChannels`, `UpsertChannel`, `GetSmtp`, `UpsertSmtp` | ✅ Implementados |
| 6 novos endpoints REST de configuração | ✅ Implementados |
| Tipos TypeScript para templates, canais e SMTP | ✅ Adicionados |
| Chamadas de API frontend para as 3 novas entidades | ✅ Adicionadas |
| Hook `useNotificationConfiguration` | ✅ Adicionado |
| 30 novos testes (domínio + aplicação) | ✅ Passando |
| Compilação do solution completo | ✅ Build succeeded. 0 Error(s) |

---

## O que ficou pendente após P7.1

### P7.2 — Integração da configuração persistida com o sistema de delivery

O `EmailNotificationDispatcher` ainda usa `NotificationChannelOptions` (appsettings) como fonte de configuração SMTP. A `SmtpConfiguration` persistida não é consumida pelo dispatcher nesta fase.

**Pending:**
- Modificar `EmailNotificationDispatcher` para consultar `ISmtpConfigurationStore` em vez de depender exclusivamente de `IOptions<NotificationChannelOptions>`.
- Estratégia: fallback gracioso (persistida → appsettings) para compatibilidade backward.

### P7.2 — Integração de templates persistidos com o sistema de resolução

O `NotificationTemplateResolver` (engine em memória) e o `NotificationOrchestrator` ainda não consultam a `INotificationTemplateStore` para resolver templates.

**Pending:**
- Atualizar `NotificationOrchestrator` para tentar resolver template persistido antes de usar o resolver em memória.
- Lógica: se existir template persistido ativo para o `EventType + Channel + Locale`, usar o persistido; caso contrário, fallback para o resolver em memória.

### P7.2 — Seeding inicial de templates built-in

Não foram semeados templates built-in nesta fase. O sistema parte de estado vazio para a tabela `ntf_templates`.

**Pending:**
- Criar `NotificationTemplateSeeder` com templates padrão (IncidentCreated, ApprovalPending, etc.) para pré-popular a tabela.

### P7.2 — Integração de configuração de canal com `NotificationRoutingEngine`

O `NotificationRoutingEngine` ainda toma decisões de routing baseado em `NotificationChannelOptions` (appsettings). Não consulta a `DeliveryChannelConfiguration` persistida.

**Pending:**
- Atualizar `NotificationRoutingEngine` para consultar `IDeliveryChannelConfigurationStore` e verificar se o canal está habilitado na configuração persistida.

### P7.2 — Criptografia de senha SMTP

A `SmtpConfiguration.EncryptedPassword` armazena o valor tal como é recebido. Não existe cifra real implementada.

**Pending:**
- Implementar `ISmtpPasswordEncryptor` (ex.: AES-256 com chave de environment variable).
- `UpsertSmtpConfiguration.Handler` deve chamar o encryptor antes de persistir.
- `EmailNotificationDispatcher` deve decifrar antes de usar.

### P7.2 — UI dedicada para templates e SMTP

A `NotificationConfigurationPage` existente usa o módulo Configuration (key-value) para settings de notificação. Não usa os novos endpoints de templates/canais/SMTP.

**Pending:**
- Criar/adaptar uma página ou painel dentro da `NotificationConfigurationPage` que consuma os hooks `useNotificationTemplates`, `useDeliveryChannels`, `useSmtpConfiguration`.
- Formulário de gestão de templates com editor de conteúdo.
- Formulário de teste de conexão SMTP.

### Fase futura — Delivery multi-canal completo

Esta fase apenas estruturou o modelo. A delivery end-to-end completa por todos os canais fica para fase posterior:
- Integração real com Slack
- Webhook genérico configurável
- SMS via provider externo
- Digest agendado

### Fase futura — Templates multilingues e motor de variáveis

O sistema de templates suporta `Locale`, mas não existe:
- Motor de substituição de variáveis `{{VariableName}}` em produção.
- Fallback automático de locale (pt → en → built-in).
- Validação de variáveis disponíveis por tipo de evento.

---

## Limitações residuais conhecidas

1. **Delivery ainda usa appsettings para SMTP** — a configuração persistida existe mas não é consumida pelo dispatcher. Funcionalmente, o email continua a usar `NotificationChannelOptions` até P7.2.

2. **Templates persistidos não são usados** — existem na base de dados mas o orquestrador ainda usa o resolver em memória. O sistema continua funcional com os templates built-in.

3. **Sem criptografia de senha SMTP** — a senha é armazenada em claro na coluna `EncryptedPassword`. Deve ser endereçado em P7.2.

4. **Sem validação de conectividade SMTP** — não existe endpoint de "test connection" para validar a configuração SMTP antes de habilitar.

---

## Próximos passos recomendados (P7.2)

1. Consumir `ISmtpConfigurationStore` em `EmailNotificationDispatcher` com fallback para appsettings.
2. Atualizar `NotificationOrchestrator` para consultar templates persistidos antes do resolver em memória.
3. Implementar `ISmtpPasswordEncryptor` com AES-256.
4. Criar `NotificationTemplateSeeder` com templates padrão.
5. Atualizar `NotificationRoutingEngine` para consultar `IDeliveryChannelConfigurationStore`.
6. Criar formulário de configuração de SMTP na `NotificationConfigurationPage`.
7. Criar formulário de gestão de templates na UI.

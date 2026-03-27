# Notifications — Catalog & Template Governance

## Catálogo de Tipos de Notificação

### Estado Actual
- **29 tipos registados** em `NotificationType.All`
- **11 tipos com template dedicado** no `NotificationTemplateResolver`
- **18 tipos com template genérico** (fallback)
- **Validação via** `NotificationType.IsValid(eventType)`

### Regras de Governança do Catálogo

#### 1. Registo Obrigatório
Todos os tipos de notificação devem estar registados em `NotificationType`.
- Não usar strings soltas no código
- Novos tipos devem ser adicionados ao catálogo estático
- A validação `IsValid()` deve ser usada nos pontos de entrada

#### 2. Cobertura de Templates
Cada tipo deve ter template no `NotificationTemplateResolver`:
- **Tipos com template dedicado**: título, mensagem, categoria, severidade e flag de ação específicos
- **Tipos sem template**: usam fallback genérico (aceitável para tipos informacionais)
- **Tipos obrigatórios sem template**: considerados gaps de governança

#### 3. Tipos Obrigatórios (Mandatory)
Definidos pelo `MandatoryNotificationPolicy`:
- `BreakGlassActivated` — obrigatório, todos os canais
- `IncidentCreated` / `IncidentEscalated` com Critical — obrigatório, todos os canais
- `ApprovalPending` — obrigatório, InApp + Email
- `ComplianceCheckFailed` — obrigatório, InApp + Email
- Qualquer severidade Critical — obrigatório, InApp + Email (mínimo)

## Governança de Templates

### Regras
| Regra | Descrição |
|-------|-----------|
| Naming | Templates seguem padrão `Build{EventType}(parameters)` |
| Parametrização | Parâmetros via `IReadOnlyDictionary<string, string>` com fallbacks |
| Categoria/Severidade | Definidos no template, não no chamador |
| i18n | Preparados para externalização futura (valores fixos actualmente) |

### Quem Pode Alterar
- **Novos tipos**: Equipa de produto + revisão de arquitectura
- **Templates**: Equipa de produto + revisão de UX
- **Categorias/Severidades**: Mudança controlada com revisão
- **Regras obrigatórias**: Aprovação de governance board

### Processo de Mudança
1. Proposta de novo tipo/template
2. Validação de que não duplica tipo existente
3. Definição de categoria, severidade e flag de ação
4. Implementação do template dedicado
5. Actualização do catálogo `NotificationType.All`
6. Testes unitários

## Governança de Canais

### Canais Permitidos
| Canal | Estado | Governança |
|-------|--------|-----------|
| InApp | Sempre activo | Não desligável |
| Email | Configurável | Via `Notifications:Channels:Email` |
| MicrosoftTeams | Configurável | Via `Notifications:Channels:Teams` |

### Regras de Canal
- InApp é canal base, sempre presente
- Email e Teams são opt-in por configuração
- Canais são atribuídos pelo `NotificationRoutingEngine` com base em severidade e preferências
- Mandatory policy pode forçar canais para tipos obrigatórios

## Prevenção de Drift

### Mecanismos
1. **Catálogo estático** — `NotificationType.All` é a fonte de verdade
2. **Validação em runtime** — `IsValid()` rejeita tipos não registados
3. **Governança summary** — `GetGovernanceSummaryAsync()` detecta gaps
4. **Validação individual** — `ValidateEventTypeAsync()` detalha problemas
5. **Testes** — `AllCatalogTypes_ShouldBeRegistered` cobre todos os tipos

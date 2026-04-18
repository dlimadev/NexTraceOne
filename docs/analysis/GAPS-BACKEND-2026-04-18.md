# NexTraceOne — Gaps e Problemas: Backend
**Data:** 2026-04-18  
**Modo:** Analysis realista — sem minimizar problemas  
**Referência:** [STATE-OF-PRODUCT-2026-04-18.md](./STATE-OF-PRODUCT-2026-04-18.md)

---

## 1. Resumo

O backend do NexTraceOne tem uma base sólida de arquitectura e uma implementação real em ~82% dos seus módulos. No entanto, existem problemas críticos não resolvidos que impedem produção, stubs que mascaram ausência de implementação, e padrões de código que comprometem robustez.

**Total de problemas identificados nesta análise:** 28  
(6 críticos, 7 altos, 10 médios, 5 baixos)

---

## 2. Problemas Críticos

### [C-02] Chave JWT de fallback hardcoded {#c-02}

**Ficheiro:** `src/building-blocks/NexTraceOne.BuildingBlocks.Security/DependencyInjection.cs`  
**Risco:** CRÍTICO — compromete todos os tokens em produção

```csharp
// Hardcoded no código-fonte — visível em binários compilados
const string devFallbackKey = "NexTrace-Dev-Only-Key-Change-In-Production-XXXXX";
```

**Problema real:** Esta constante está compilada nos binários. Qualquer pessoa com acesso ao DLL pode extraí-la via `strings` ou ILSpy. Se este key chegar a produção (por omissão de configuração de variável de ambiente), todos os tokens JWT do sistema são forjáveis.

**Remediação:** Remover `devFallbackKey` completamente. A aplicação deve falhar em startup (`throw new InvalidOperationException`) se `Jwt:Key` não estiver configurado. Nunca silenciar ausência de segredos críticos com fallback.

---

### [C-03] API Keys em memória sem encriptação e sem rotação {#c-03}

**Ficheiro:** `ApiKeyAuthenticationHandler.cs`  
**Risco:** CRÍTICO — exposure de chaves em dumps de processo

**Problema real:**
- Chaves lidas de `appsettings.json` em plaintext
- Armazenadas em memória como strings sem hash
- Sem suporte a rotação de chaves
- Sem auditoria de utilização por chave
- Sem expiração por chave

**Remediação:** Hash das chaves com SHA-256 no momento de configuração. Comparação por hash. Implementar expiração e log de auditoria por uso de chave.

---

### [C-04] 6 endpoints de autenticação em falta {#c-04}

**Módulo:** `IdentityAccess`  
**Risco:** CRÍTICO — fluxos de onboarding completamente quebrados

O frontend chama os seguintes endpoints que **não existem no backend**:

| Endpoint esperado | Método | Impacto |
|-------------------|--------|---------|
| `/api/v1/identity/auth/activate-account` | POST | Activação de conta após convite |
| `/api/v1/identity/auth/forgot-password` | POST | Recuperação de password |
| `/api/v1/identity/auth/reset-password` | POST | Reset de password com token |
| `/api/v1/identity/auth/resend-mfa-code` | POST | Reenvio de código MFA |
| `/api/v1/identity/auth/invitation/{id}` | GET | Detalhes do convite |
| `/api/v1/identity/auth/accept-invitation` | POST | Aceitação de convite |

**Consequência directa:**
- Qualquer utilizador convidado não consegue activar a sua conta
- Password recovery está completamente quebrado
- Fluxo MFA fica bloqueado em caso de expiração de código
- Sistema de convites é não-funcional

**Remediação:** Implementar 6 handlers MediatR + validadores + endpoints. Estes não são features avançadas — são fluxos básicos de autenticação que qualquer sistema enterprise precisa.

---

### [C-05] Export endpoint — stub hardcoded {#c-05}

**Ficheiro:** `src/modules/configuration/NexTraceOne.Configuration.API/ExportEndpointModule.cs`  
**Risco:** CRÍTICO — feature de exportação completamente não-funcional

```csharp
// Retorna sempre "queued" sem nenhum job real
return StatusCode(501, new { status = "queued", message = "Export scheduled" });
```

**Problema real:**
- Nenhum job Quartz registado para processar exports
- Nenhuma entidade `ExportJob` na base de dados
- Nenhum mecanismo de entrega (download link, email, webhook)
- O frontend mostra "a exportar..." sem nunca concluir

**Remediação:**
1. Criar entidade `ExportRequest` com estado (Queued, Processing, Ready, Failed)
2. Registar `ExportProcessorJob` no Quartz com intervalo configurável
3. Implementar geração real (CSV/Excel via ClosedXML, PDF via PdfSharpCore)
4. Endpoint de polling `/api/v1/export/{id}/status` + download link

---

### [C-06] OnCall Intelligence com dados pseudo-aleatórios {#c-06}

**Ficheiro:** `src/modules/operationalintelligence/...GetOnCallIntelligence.cs`  
**Risco:** CRÍTICO — métricas falsas apresentadas como reais

```csharp
// Geração pseudo-aleatória mascarada como métrica operacional
var fatigueSeed = engineer.Name.Length + DateTime.UtcNow.DayOfYear;
var fatigueMinutes = Math.Min(20m + (fatigueSeed % 30), 60m);
```

**Problema real:**
- Indicadores de fadiga de on-call são calculados com seed baseado no comprimento do nome + dia do ano
- Os números mudam ligeiramente a cada dia para parecer dinâmicos
- Não há integração com nenhuma fonte real de dados de on-call (PagerDuty, OpsGenie, etc.)
- Um engenheiro pode tomar decisões operacionais com base em números inventados

**Remediação:**
- Opção A (curto prazo): Remover completamente o ecrã ou mostrar explicitamente "Dados não disponíveis — configure integração on-call"
- Opção B (médio prazo): Integrar com PagerDuty/OpsGenie via Integrations module
- Não há opção C aceitável que mantenha dados pseudo-aleatórios

---

## 3. Problemas de Alta Prioridade

### [A-04] Silent exception handling

**Localização:** 3 handlers em módulos distintos  
**Padrão problemático:**

```csharp
catch (Exception ex)
{
    // Empty catch — exception swallowed completely
    _logger.LogWarning("..."); // apenas warning, não re-throw
    return Result.Success(fallback); // retorna sucesso silencioso
}
```

**Problema:** Falhas reais são ocultadas. Em produção, problemas sistemáticos podem acumular-se sem alertas durante dias.

**Remediação:** Logar como Error com stack trace completo, ou re-throw wrappado em `Result.Failure`. Nunca retornar `Result.Success` quando uma excepção ocorreu.

---

### [A-05] NullReferenceException em potencial no IncidentCorrelationService

**Ficheiro:** `IncidentCorrelationService.cs`  
**Problema:**

```csharp
var change = await _changeRepository.GetLatestByServiceAsync(serviceId);
// change pode ser null — próxima linha não verifica
var correlationScore = CalculateScore(change.Timestamp, incident.OccurredAt);
```

**Remediação:** Guard clause imediato após a chamada ao repositório.

---

### [A-06] DTO mismatch na correlação de incidentes

**Problema:** Endpoint de correlação recebe `changeId` como `string` no DTO mas o domínio espera `Guid`. A conversão implícita funciona em cenários normais mas falha com UUIDs não-standard, sem erro claro para o utilizador.

**Remediação:** Strongly typed ID no DTO. Validação FluentValidation com `Must(id => Guid.TryParse(id, out _))`.

---

### [A-08] Correlação incidente↔mudança demasiado superficial

**Localização:** `IncidentCorrelationService`  
**Problema actual:** A correlação usa apenas:
1. Correspondência de nome de serviço (string match)
2. Janela de tempo (±30 minutos)

Não considera:
- Dependências transitivas (serviço A depende de B que foi alterado)
- Blast radius calculado
- Severidade e tipo de mudança
- Histórico de correlações anteriores para o mesmo par serviço/mudança
- Rollout percentual (deploy gradual que afecta só X% dos pedidos)

**Impacto:** Feature de correlação apresentada como inteligente, mas detecta apenas casos triviais. Em ambientes com muitas mudanças simultâneas, produz ruído e falsos positivos.

---

### [A-09] Ausência de eventos de integração no módulo Configuration

**Problema:** O módulo Configuration altera feature flags, templates e automações que outros módulos dependem. Mas não publica `IntegrationEvents` quando esses valores mudam.

**Consequência:** Módulos dependentes não sabem que o estado mudou sem polling. O padrão de outbox existe mas não é usado aqui.

**Remediação:** Publicar `FeatureFlagChangedEvent`, `AutomationRuleUpdatedEvent`, `TemplateModifiedEvent` via outbox existente.

---

### [A-10] Geração de código com TODOs não funcionais

**Ficheiro:** `GenerateServerFromContract` (Catalog module)  
**Problema:**

```csharp
// Código gerado pelo GenerateServerFromContract:
public class OrderController : ControllerBase {
    // TODO: Inject services
    // TODO: Implement handler
    [HttpPost]
    public async Task<IActionResult> CreateOrder() {
        throw new NotImplementedException();
    }
}
```

O produto apresenta esta funcionalidade como "geração de código". O código gerado não compila de forma útil. Um engenheiro que use esta feature em contexto real vai obter um scaffolding não-funcional.

**Remediação:** Ou gerar código funcional real (com handlers MediatR básicos), ou rotular explicitamente como "scaffolding template — requires completion" na UI.

---

## 4. Problemas de Média Prioridade

### [M-01] Thresholds de correlação hardcoded

```csharp
// Em múltiplos handlers
const int CorrelationWindowMinutes = 30;
const double MinimumConfidenceScore = 0.65;
```

Estes valores devem ser configuráveis por ambiente/tenant no módulo Configuration.

---

### [M-02] Moeda EUR hardcoded como default

```csharp
var defaultCurrency = "EUR";
```

Em ambientes enterprise multi-região, a moeda deve vir de configuração de tenant.

---

### [M-03] GenerateMockServer sem validação de spec vazia

O handler `GenerateMockServer` não valida se a especificação OpenAPI passada está vazia ou inválida antes de tentar processá-la, gerando stack trace vazio sem mensagem útil.

---

### [M-04] AddBookmark sem validação de enum

```csharp
// Falta validação
// Deveria ter: .Must(v => IsInEnum<BookmarkType>(v))
```

---

### [M-05] Background workers sem circuit breaker

Os 54 jobs Quartz (outbox processors) não têm circuit breaker. Se a base de dados estiver temporariamente indisponível, todos os jobs vão entrar em retry loop simultâneo. Recomendado: jitter + backoff exponencial + circuit breaker por DbContext.

---

### [M-06] Ausência de idempotency keys em endpoints de mutação críticos

Endpoints como `PromoteToEnvironment`, `ApproveChange`, `AcceptInvitation` não têm mecanismo de idempotência. Double-click ou retry de rede pode causar duplicação de operações críticas.

---

### [M-07] Falta de paginação em alguns endpoints de listagem

Vários endpoints de listagem em módulos como Knowledge e Notifications não têm paginação obrigatória, expondo o sistema a consultas sem limite em ambientes com volume alto.

---

### [M-08] Ausência de validação de ambiente nas operações de Change Governance

Mudanças podem ser promovidas sem verificar se o ambiente de destino está em `freeze window`. A lógica de freeze window existe no domínio mas não é aplicada consistentemente em todos os pontos de entrada da API.

---

### [M-09] Missing rate limiting no endpoint de AI streaming

O endpoint SSE de AI streaming tem rate limiting definido na policy `"Ai"`, mas não valida orçamento de tokens antes de iniciar o stream. Um utilizador pode esgotar o budget de tokens de toda a organização num único request longo.

---

### [M-10] Falta de validação cross-module na promoção de contratos

O `ContractComplianceGate` verifica conformidade de contratos durante promoção, mas não correlaciona com `ContractDrift` — um contrato pode estar em drift (comportamento real diverge da spec) e ainda assim passar o gate de compliance.

---

## 5. Problemas de Baixa Prioridade

### [L-01] Comentários em inglês misturados com português

Convenção definida no CLAUDE.md: código em inglês, comentários XML em português. Encontrados comentários inline em português misturados com código em inglês em ~12 ficheiros.

### [L-02] Alguns handlers não usam CancellationToken propagation

Em 8 handlers identificados, o `CancellationToken` é recebido no método principal mas não propagado para chamadas internas ao repositório.

### [L-03] Uso de `DateTime.Now` em vez de UTC em 4 locais

Violação da regra explícita no CLAUDE.md: `nunca DateTime.Now; usar abstração correta ou UTC`. Encontrado em módulos Configuration e Notifications.

### [L-04] Logging inconsistente — mix de structured e string interpolation

```csharp
// Incorrecto (string interpolation perde structured logging)
_logger.LogInformation($"Service {serviceId} processed");

// Correcto (structured)
_logger.LogInformation("Service {ServiceId} processed", serviceId);
```

### [L-05] Dependências circulares latentes

Module X (Change) acede a contratos definidos em Module Y (Catalog) via interface pública. A direcção é correcta, mas não há testes automáticos que detectem introdução de dependências circulares.

---

## 6. Módulos com melhor qualidade de implementação

Para referência positiva — módulos que servem de referência de implementação correcta:

1. **Catalog** — 90 features, todas reais, 1179+ testes, bounded context limpo
2. **AI Knowledge** — LLM real, guardrails, audit trail, tools com permissões — referência de como IA deve ser implementada
3. **Audit Compliance** — SHA-256 hash chain, imutabilidade real, modelo de dados correcto
4. **Building Blocks** — CQRS, Result<T>, strongly typed IDs — todos production-grade

---

## 7. Priorização de remediação backend

```
SPRINT 1 (bloqueadores de produção):
  [C-02] Remover JWT hardcoded key          → 2h
  [C-03] Encriptar/hashar API keys          → 4h
  [C-04] Implementar 6 endpoints auth       → 3 dias
  [C-05] Implementar Export com Quartz      → 2 dias
  [C-06] Remover/substituir OnCall fake     → 1 dia
  [A-04] Corrigir silent exception handlers → 4h
  [A-05] Fix NullRef em CorrelationService  → 1h

SPRINT 2 (qualidade e integridade):
  [A-06] DTO mismatch correlação            → 2h
  [A-08] Melhorar correlação incidente      → 3 dias
  [A-09] Eventos de integração Configuration→ 2 dias
  [M-01] Externalizar thresholds            → 1 dia
  [M-05] Circuit breaker nos workers        → 2 dias
  [M-06] Idempotency keys                   → 1 dia
```

---

*Para análise do frontend ver [GAPS-FRONTEND-2026-04-18.md](./GAPS-FRONTEND-2026-04-18.md)*  
*Para análise da base de dados ver [GAPS-DATABASE-2026-04-18.md](./GAPS-DATABASE-2026-04-18.md)*

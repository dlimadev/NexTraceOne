# Phase 7 — AI Sources, Tools and Governance

## IExternalAIRoutingPort

Interface de porta de saída (hexagonal architecture) para comunicação com providers de IA:

```csharp
public interface IExternalAIRoutingPort
{
    Task<string> RouteQueryAsync(
        string groundingContext,
        string query,
        string? preferredProvider,
        CancellationToken cancellationToken);
}
```

A implementação concreta (adapter) resolve o provider a usar com base em:
- `preferredProvider` (se fornecido e disponível)
- política de routing configurada no tenant
- disponibilidade do provider

---

## Estratégia de grounding context

Cada feature constrói um grounding context explícito antes de chamar o provider:

```
Environment: QA (Profile: qa)
Tenant: tenant-acme-001
Analysis window: last 7 days
Analysis type: Non-production environment risk assessment
Services in scope: all accessible services in this environment
Goal: Identify signals that could represent regression, quality issues, or risk to production.
```

**Princípios:**
- Contexto passado explicitamente — não inferido pela IA
- TenantId sempre incluído para garantir isolamento semântico
- Goal statement estruturado para guiar a resposta da IA
- Sem dados sensíveis no grounding (apenas metadados descritivos)

---

## Provider routing e fallback

### Fluxo normal

```
Feature.Handler
  → externalAiRoutingPort.RouteQueryAsync(grounding, query, preferredProvider, ct)
  → Provider selecionado (OpenAI / Azure OpenAI / Ollama / etc.)
  → string com resposta estruturada
```

### Fallback

Se nenhum provider estiver disponível, a porta retorna:
```
[FALLBACK_PROVIDER_UNAVAILABLE] {mensagem}
```

O handler detecta:
```csharp
isFallback = aiContent.StartsWith("[FALLBACK_PROVIDER_UNAVAILABLE]", StringComparison.OrdinalIgnoreCase);
```

E inclui `IsFallback = true` na response — sem falhar.

### Exceção do provider

Se o provider lança exceção, o handler:
1. Loga com `LogWarning` incluindo `TenantId`, `CorrelationId` e exceção
2. Retorna `Result.Error` com código `AIKnowledge.Provider.Unavailable`

---

## CorrelationId para auditoria

Cada invocação gera um `CorrelationId = Guid.NewGuid().ToString()`.

Este ID:
- É retornado na response ao cliente
- É incluído em todos os logs do handler
- Permite rastrear uma análise específica nos logs do sistema
- É exibido na UI para referência do utilizador

---

## TenantId como garantia de isolamento

O `TenantId` é um pilar de segurança:
- Obrigatório em todos os commands de análise de ambiente
- Validado por `AbstractValidator` antes do handler
- Incluído no grounding enviado à IA (isolamento semântico)
- Incluído em todos os logs estruturados

**Regra**: Não é possível analisar ou comparar ambientes de tenants diferentes.
O backend garante este isolamento em todas as camadas.

---

## Governança de IA (Phase 7 scope)

A Phase 7 usa a porta `IExternalAIRoutingPort` existente, que já suporta:
- Model registry
- AI access policies
- Token budget governance
- Audit de uso

Estas capacidades foram implementadas nas fases anteriores e são reutilizadas
diretamente pelos novos features sem alterações.

**TODO Phase 8**: Auditoria granular por feature (registar `AnalyzeNonProdEnvironment`
como evento de auditoria específico, não apenas como chamada genérica ao provider).

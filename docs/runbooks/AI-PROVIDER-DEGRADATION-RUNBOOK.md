# Runbook — Degradação do AI Provider

> **NexTraceOne — Operação**  
> Versão: 1.0 | Data: 2026-03-22

---

## Contexto

O NexTraceOne utiliza AI providers externos (ex: OpenAI, Azure OpenAI, ou equivalente) através do módulo `ExternalAI`.  
O sistema é projetado para falhar de forma segura quando o provider está indisponível.

**Arquitetura de resiliência**:
- `IExternalAIRoutingPort` — abstração que isola o provider real
- Timeout configurável por request
- Circuit breaker (quando implementado)
- Safe failure: retorna erro tratado, nunca 500 não tratado

---

## Sintomas de Degradação

| Sintoma | Severidade | Indicador |
|---------|-----------|-----------|
| AI chat não responde | SEV-2 | `/api/v1/ai/assistant/chat` retorna erro |
| Latência AI > 30s | SEV-2 | Timeout na resposta |
| Erros 429 (rate limit) | SEV-3 | Rate limit excedido no provider |
| Erro de autenticação do provider | SEV-2 | 401/403 do provider |
| Provider completamente indisponível | SEV-2 | HTTP 5xx do provider |

---

## Diagnóstico

### Passo 1 — Verificar health do sistema

```bash
APIHOST="https://<prod-url>"
curl -sf "${APIHOST}/health" | jq '.checks | to_entries[] | select(.key | contains("ai"))'
```

### Passo 2 — Verificar logs de AI

```bash
docker logs nextraceone-apihost --tail 200 2>&1 | grep -E "(ExternalAI|AI|provider|OpenAI|azure)"
```

### Passo 3 — Testar provider diretamente

```bash
# Verificar status do provider (exemplo OpenAI)
curl -sf "https://status.openai.com/api/v2/status.json" | jq .status

# Verificar Azure OpenAI (se aplicável)
curl -sf "https://status.azure.com/en-us/status" | head -20
```

### Passo 4 — Verificar configuração

```bash
# Verificar se a API key está configurada (sem expor o valor)
docker exec nextraceone-apihost env | grep -E "(AI_|OPENAI|AZURE)" | sed 's/=.*/=***/'
```

---

## Ações de Mitigação

### Mitigação 1 — Provider temporariamente indisponível (< 30 min)

**Ação**: aguardar. O sistema continua funcionando para todos os outros módulos.

```bash
# Confirmar que o resto do sistema está saudável
curl -sf "${APIHOST}/live"
curl -sf "${APIHOST}/ready"

# Informar utilizadores
# "AI Assistant temporariamente indisponível. Demais funcionalidades normais."
```

### Mitigação 2 — Rate limit (429)

**Ação**: verificar quota utilizada e aguardar reset (geralmente 1 minuto ou 1 hora).

```bash
# Verificar logs de rate limit
docker logs nextraceone-apihost --tail 200 2>&1 | grep "429\|RateLimitExceeded\|rate_limit"
```

**Configuração**: ajustar `ExternalAI:RequestsPerMinute` no appsettings se necessário.

### Mitigação 3 — API Key inválida ou expirada

**Ação**: atualizar o secret no ambiente de produção.

1. Gerar nova API key no dashboard do provider
2. Atualizar o secret `AI_PROVIDER_API_KEY` no ambiente de produção
3. Reiniciar o ApiHost para recarregar configuração:
   ```bash
   docker compose restart apihost
   ```
4. Validar:
   ```bash
   sleep 30
   curl -sf "${APIHOST}/health" | jq .
   ```

### Mitigação 4 — Provider completamente fora (fallback)

**Ação**: habilitar modo degradado (se implementado).

O sistema está projetado para retornar `503 Service Unavailable` com mensagem estruturada:
```json
{
  "code": "AI_PROVIDER_UNAVAILABLE",
  "message": "AI Assistant temporariamente indisponível",
  "details": "O provider de IA está com problemas. Tente novamente em alguns minutos."
}
```

**Verificar**: que os endpoints de AI estão retornando erros tratados, não 500.
```bash
curl -X POST "${APIHOST}/api/v1/ai/assistant/chat" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{"message":"test","sessionId":"test"}' | jq .
# Deve retornar erro estruturado, não stack trace
```

---

## Comportamento Esperado Pós-Degradação

Quando o provider se recupera:
1. Novas requisições AI passam a funcionar automaticamente
2. Nenhum restart necessário (HTTP client se reconecta)
3. Sessões abertas de AI podem precisar ser reiniciadas pelo utilizador

---

## Checklist de Resolução

- [ ] Provider voltou a responder
- [ ] `/health` retorna `Healthy` para todos os checks
- [ ] Teste manual de chat AI funcionou
- [ ] Logs sem erros de provider nas últimas 5 min
- [ ] Utilizadores informados da resolução

---

## Prevenção

1. **Alertas de quota**: configurar alerta quando quota atingir 80%
2. **Multiple providers**: considerar fallback para segundo provider em fase futura
3. **Circuit breaker**: implementar `Polly.CircuitBreaker` no `ExternalAIRoutingAdapter`
4. **Timeout adequado**: garantir timeout < 30s para não bloquear threads do ApiHost

---

*Runbook mantido pelo Tech Lead responsável pelo módulo AIKnowledge.*

# P1.1 — CORS Base Cleanup Report

**Data de execução:** 2026-03-26  
**Classificação:** HIGH / P1 — Segurança de configuração  
**Estado:** CONCLUÍDO

---

## 1. Contexto

A auditoria do estado atual do NexTraceOne identificou que `appsettings.json` (configuração base)
continha origens CORS de desenvolvimento (`http://localhost:5173`, `http://localhost:3000`).

O risco identificado: se a ordem de carregamento de configuração falhar, ou se o ficheiro base
for usado num ambiente indevido, produção poderia aceitar pedidos vindos de localhost.

---

## 2. Ficheiros alterados

| Ficheiro | Tipo de alteração |
|---|---|
| `src/platform/NexTraceOne.ApiHost/appsettings.json` | Origens localhost removidas; `AllowedOrigins` definido como `[]` |
| `src/platform/NexTraceOne.ApiHost/appsettings.Development.json` | Secção `Cors:AllowedOrigins` adicionada com as origens de desenvolvimento |
| `src/platform/NexTraceOne.ApiHost/WebApplicationBuilderExtensions.cs` | Fallback de desenvolvimento atualizado para tratar array vazio além de null; comentário XML atualizado |

---

## 3. Origens removidas do config base

**Ficheiro:** `appsettings.json`

```json
// ANTES
"Cors": {
  "AllowedOrigins": [
    "http://localhost:5173",
    "http://localhost:3000"
  ]
}

// DEPOIS
"Cors": {
  "AllowedOrigins": []
}
```

---

## 4. Origens mantidas/adicionadas em Development

**Ficheiro:** `appsettings.Development.json`

```json
"Cors": {
  "AllowedOrigins": [
    "http://localhost:5173",
    "http://localhost:3000"
  ]
}
```

As origens de desenvolvimento existem **exclusivamente** neste ficheiro, que é carregado apenas
quando `ASPNETCORE_ENVIRONMENT=Development`.

---

## 5. Comportamento de resolução de CORS no startup

### Lógica em `WebApplicationBuilderExtensions.AddCorsConfiguration`

1. Lê `Cors:AllowedOrigins` da configuração efectiva (base + overlay por ambiente).
2. Se o ambiente **não é Development nem CI** e `AllowedOrigins` está vazio ou nulo → lança `InvalidOperationException` (startup bloqueado).
3. Em Development/CI, se `AllowedOrigins` está vazio ou nulo → aplica fallback de conveniência para `localhost:5173` e `localhost:3000`.
4. Valida que nenhuma origem contém wildcard (`*`) — proibido com `AllowCredentials`.
5. Regista a política CORS padrão com as origens resolvidas, headers e métodos permitidos.

### Fluxo por ambiente

| Ambiente | `appsettings.json` | Overlay | Origens efectivas |
|---|---|---|---|
| Development | `[]` | `appsettings.Development.json` → `[localhost:5173, localhost:3000]` | `[localhost:5173, localhost:3000]` ✓ |
| Staging / Production | `[]` | Nenhum overlay de dev | Lança exceção — exige configuração explícita ✓ |

---

## 6. Alteração ao fallback em WebApplicationBuilderExtensions.cs

O fallback foi actualizado de:
```csharp
var corsOrigins = configuredOrigins ?? ["http://localhost:5173", "http://localhost:3000"];
```
para:
```csharp
var corsOrigins = (configuredOrigins is { Length: > 0 })
    ? configuredOrigins
    : ["http://localhost:5173", "http://localhost:3000"];
```

**Motivo:** com o config base a usar `[]` (array vazio), `configuredOrigins` retorna `[]` em vez de `null`.
A expressão `?? [...]` não cobria arrays vazios. A nova expressão garante que o fallback de Development
continua funcional se o overlay de configuração estiver ausente (por exemplo, execução de testes sem
`appsettings.Development.json`). O fallback é seguro pois só é alcançável em `Development` ou `CI`
(ambientes não-desenvolvimento lançam exceção antes).

---

## 7. Validação funcional realizada

- Configuração base inspeccionada: `AllowedOrigins` está agora `[]`.
- Configuração de Development inspeccionada: `AllowedOrigins` contém as duas origens localhost.
- Lógica de resolução revisada: startup em Production/Staging com array vazio lança exceção (proteção activa).
- Fallback em Development cobre tanto `null` como `[]` — Development continua funcional.
- Nenhum wildcard introduzido.
- Nenhum outro ficheiro com origens localhost indevidas identificado no âmbito desta fase.

---

## 8. Critérios de aceite verificados

| Critério | Estado |
|---|---|
| `appsettings.json` não contém origens localhost | ✅ |
| `appsettings.Development.json` contém origens de desenvolvimento | ✅ |
| Configuração CORS continua funcional em Development | ✅ |
| Nenhum wildcard inseguro introduzido | ✅ |
| Relatório final gerado | ✅ |

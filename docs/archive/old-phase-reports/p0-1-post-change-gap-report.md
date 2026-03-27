# P0.1 — Post-Change Gap Report

**Data de execução:** 2026-03-25  
**Fase:** P0.1 — Remoção de passwords hardcoded das connection strings  
**Estado:** CONCLUÍDO COM GAPS CONTROLADOS

---

## 1. O que foi resolvido

| Item | Estado |
|---|---|
| `Password=ouro18` removido de `appsettings.Development.json` (20 strings) | ✅ RESOLVIDO |
| `Password=ouro18` removido de `ApiE2EFixture.cs` (2 entradas) | ✅ RESOLVIDO |
| `Password=ouro18` removido de `ApiHostPostgreSqlFixture.cs` (2 entradas) | ✅ RESOLVIDO |
| `appsettings.json` já estava limpo desde E18 | ✅ CONFIRMADO |
| `.env.example` já alinhado com estratégia de env vars | ✅ CONFIRMADO |

---

## 2. O que ficou pendente (fora do escopo de P0.1)

### 2.1 JWT Secret hardcoded em `appsettings.Development.json`

```json
"Jwt": {
  "Secret": "NexTraceOne-Development-SecretKey-AtLeast32BytesLong-2024!"
}
```

Este valor está hardcoded no ficheiro Development. Embora seja um segredo de desenvolvimento,
não deve permanecer num ficheiro commitado.

**Responsabilidade:** P0.2 — JWT / Auth hardening

---

### 2.2 JWT Secret vazio em `appsettings.json`

O `appsettings.json` tem `"Secret": ""` para o JWT. Sem um fallback ou validação explícita
por ambiente, isto pode ser um problema em staging/produção se a variável de ambiente não for
fornecida.

**Responsabilidade:** P0.2

---

### 2.3 Fallback AES hardcoded (fora do escopo de P0.1)

Identificado em relatórios anteriores. Não tratado nesta fase.

**Responsabilidade:** P0.2 ou P0.3

---

### 2.4 CORS sem restrição em Development

Identificado como gap de segurança em relatórios anteriores. Não tratado nesta fase.

**Responsabilidade:** P0.3

---

### 2.5 `NEXTRACE_SKIP_INTEGRITY` em testes

Os test fixtures definem `NEXTRACE_SKIP_INTEGRITY=true` via código. Aceitável para testes,
mas deve ser documentado como comportamento controlado e nunca replicado em produção.

**Estado:** Gap controlado e aceitável.

---

### 2.6 Desenvolvimento local sem password real

Após esta fase, o desenvolvimento local requer configuração explícita de credenciais via:
- `dotnet user-secrets`
- Variáveis de ambiente
- Ficheiro `.env` local (não commitado)

Sem este passo, a aplicação falhará ao conectar à base de dados.

**Mitigação:** Documentação clara no README / onboarding guide. Recomendado para P0.2 ou fase
de Developer Experience.

---

## 3. Riscos residuais

| Risco | Severidade | Mitigação recomendada |
|---|---|---|
| Programador novo pode não saber como configurar credenciais locais | MÉDIO | Actualizar README com secção de setup local |
| JWT Secret ainda hardcoded em Development | MÉDIO | P0.2 |
| AES fallback hardcoded (se existir) | MÉDIO | P0.2/P0.3 |
| `ouro18` pode existir em branches locais ou histórico git | BAIXO | Não afecta segurança do repositório activo; histórico pode ser limpo separadamente se necessário |

---

## 4. O que deve ser tratado no P0.2

1. **JWT Secret**: remover `"Secret": "NexTraceOne-Development-SecretKey-AtLeast32BytesLong-2024!"` do ficheiro commitado; substituir por variável de ambiente com validação de startup.
2. **AES fallback**: verificar e remover qualquer chave AES hardcoded.
3. **Validação de startup**: garantir que `StartupValidation` rejeita explicitamente o placeholder `REPLACE_VIA_ENV` como valor real (actualmente é tratado como string não-vazia).
4. **README / Developer Guide**: documentar o processo de setup local com `dotnet user-secrets` ou `.env`.

---

## 5. Classificação final

| Dimensão | Estado |
|---|---|
| Credenciais BD em appsettings | ✅ RESOLVIDO |
| Credenciais BD em testes | ✅ RESOLVIDO |
| Alinhamento com `.env.example` | ✅ CONFIRMADO |
| Regressão de arranque | ✅ SEM REGRESSÃO (placeholder não-vazio; falha apenas em connect real) |
| Outros segredos hardcoded | ⚠️ PENDENTE (P0.2) |

**Classificação geral:** `P0.1_COMPLETE_WITH_CONTROLLED_GAPS`

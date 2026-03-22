# Runbook — Análise de Drift e Comparação de Ambientes

> **NexTraceOne — Operação**  
> Versão: 1.0 | Data: 2026-03-22

---

## Contexto

O NexTraceOne possui funcionalidade de comparação entre ambientes (ex: Staging vs Produção) através do módulo `OperationalIntelligence.Runtime` e dos agentes AI de análise contextual.

**Objetivo**: identificar quando dois ambientes divergem de forma não esperada — contrato, configuração, schema ou comportamento — antes que isso cause incidente.

---

## Quando Usar Este Runbook

- Promoção de release de staging para produção está bloqueada
- Análise AI retornou `BLOCK_PROMOTION` ou risco `HIGH`
- Diferenças inesperadas de comportamento entre ambientes
- Investigação de incidente pós-promoção
- Auditoria de conformidade entre ambientes

---

## Fontes de Drift

| Tipo de Drift | Onde verificar | Risco |
|---------------|---------------|-------|
| Versão de contrato de API diferente | Contract module | Alto |
| Migration aplicada em staging mas não em produção | `__EFMigrationsHistory` | Alto |
| Variáveis de configuração diferentes | Environment variables | Médio |
| Imagem de container diferente | Docker registry | Alto |
| Dados de seed diferentes | Banco de dados | Baixo |
| Feature flags divergentes | Configuração | Médio |

---

## Diagnóstico

### Passo 1 — Verificar versões de imagem

```bash
# Qual versão está em staging?
docker inspect nextraceone-staging-apihost --format '{{.Config.Image}}'

# Qual versão está em produção?
docker inspect nextraceone-prod-apihost --format '{{.Config.Image}}'

# Comparar SHAs
# Se diferente, identificar commits entre as duas versões
git log --oneline <tag-prod>..<tag-staging>
```

### Passo 2 — Comparar migrations aplicadas

```bash
# Migrations em staging
psql "${STAGING_CONN_OPERATIONS}" -c "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY 1;" > /tmp/staging-migrations.txt

# Migrations em produção
psql "${PROD_CONN_OPERATIONS}" -c "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY 1;" > /tmp/prod-migrations.txt

# Diff
diff /tmp/staging-migrations.txt /tmp/prod-migrations.txt
```

### Passo 3 — Comparar versões de contratos

```bash
# Listar contratos em staging
curl -sf -H "Authorization: Bearer ${STAGING_TOKEN}" \
  "${STAGING_APIHOST}/api/v1/contracts/summary" | jq .

# Listar contratos em produção
curl -sf -H "Authorization: Bearer ${PROD_TOKEN}" \
  "${PROD_APIHOST}/api/v1/contracts/summary" | jq .
```

### Passo 4 — Usar análise AI para drift

Via interface do NexTraceOne (quando disponível):

1. Acessar **AI Assistant → Environment Analysis**
2. Selecionar Source: `Staging` e Target: `Production`
3. Executar **Compare Environments**
4. Revisar findings retornados

Via API:
```bash
curl -X POST "${STAGING_APIHOST}/api/v1/ai/analysis/compare-environments" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "sourceEnvironmentId": "<staging-env-id>",
    "targetEnvironmentId": "<prod-env-id>",
    "serviceId": "<service-id>",
    "scope": "contracts,schema,config"
  }' | jq .
```

---

## Cenários de Drift e Ações

### Cenário A — Contrato diferente entre ambientes

**Sintoma**: versão de API em staging (v3.0) diferente de produção (v2.5)

**Ação**:
1. Verificar se a mudança é intencional (nova feature em staging)
2. Verificar compatibilidade retroativa via `ValidateContractCompatibility`
3. Se breaking change: bloquear promoção até consumidores migrarem
4. Se backwards-compatible: promoção pode prosseguir com comunicado

### Cenário B — Migration em staging não em produção

**Sintoma**: staging tem 45 migrations, produção tem 44

**Ação**:
1. Identificar qual migration está pendente
2. Avaliar impacto da migration (additive vs breaking)
3. Planejar janela de deploy para aplicar migration + código
4. Ver `MIGRATION-FAILURE-RUNBOOK.md` se migration falhar em produção

### Cenário C — Comportamento diferente entre ambientes (sem mudança de código)

**Sintoma**: feature funciona em staging, quebra em produção

**Diagnóstico**:
```bash
# Comparar configurações (sem expor valores secretos)
docker exec nextraceone-staging-apihost env | grep -v "SECRET\|PASSWORD\|KEY\|CONN" | sort > /tmp/staging-config.txt
docker exec nextraceone-prod-apihost env | grep -v "SECRET\|PASSWORD\|KEY\|CONN" | sort > /tmp/prod-config.txt
diff /tmp/staging-config.txt /tmp/prod-config.txt
```

**Ação**: corrigir a configuração no ambiente com desvio.

### Cenário D — Análise AI retornou BLOCK_PROMOTION

**Sintoma**: feature `CompareEnvironments` retornou `recommendedDecision: BLOCK_PROMOTION`

**Ação**:
1. Revisar o campo `findings` da resposta AI
2. Para cada finding de risco `HIGH`:
   - Investigar causa
   - Corrigir antes da promoção
3. Para findings de risco `MEDIUM`:
   - Avaliar com Tech Lead
   - Documentar aceitação de risco se proceder
4. Re-executar análise após correções

---

## Resolução e Normalização

Após identificar e corrigir o drift:

1. **Documentar a diferença** e a ação tomada
2. **Verificar que ambos os ambientes estão sincronizados**:
   ```bash
   diff /tmp/staging-migrations.txt /tmp/prod-migrations.txt
   # Sem diff = ambientes sincronizados
   ```
3. **Re-executar análise de comparação** — deve retornar zero findings de risco HIGH
4. **Atualizar runbook** se um cenário novo foi encontrado

---

## Prevenção de Drift

1. **Pipeline de staging como gate de produção** — nunca deploy direto em produção
2. **Migrations aplicadas em staging primeiro** e validadas antes de produção
3. **Análise AI de comparação obrigatória** antes de toda promoção para produção
4. **Feature flags** para rollout gradual sem drift de código
5. **Configuration as Code** — toda configuração versionada, nunca manual

---

*Runbook mantido pelo Tech Lead. Atualizar quando novos tipos de drift forem identificados.*

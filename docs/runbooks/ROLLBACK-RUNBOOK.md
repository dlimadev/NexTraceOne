# Runbook — Rollback

## Quando executar rollback

Execute rollback quando:
- Smoke checks falharem após deploy
- Health check `/ready` retornar `Unhealthy` após deploy
- Aumento súbito de erros 5xx pós-deploy
- Degradação de performance não explicável
- Funcionalidade crítica quebrada em staging/produção

---

## Critérios de decisão — Rollback vs Hotfix

| Situação | Decisão |
|---|---|
| Build anterior estável e disponível no registry | Rollback imediato |
| Problema de migration aplicada (irreversível) | Hotfix + forward-only migration |
| Bug de lógica sem impacto de dados | Hotfix rápido preferível |
| Problema de configuração | Fix de config sem rollback de imagem |
| Indisponibilidade total | Rollback imediato |

---

## Rollback de imagens

### Via Docker Compose (staging local)

```bash
# 1. Identificar tag anterior estável
# As tags seguem o padrão: <8-char-SHA> e 'staging'
# Ver histórico no GitHub Packages:
# https://github.com/dlimadev/NexTraceOne/pkgs/container/nextraceone-apihost

# 2. Parar serviços atuais
docker compose down apihost workers ingestion frontend

# 3. Editar docker-compose.yml ou definir IMAGE_TAG
export IMAGE_TAG=<tag-anterior-estavel>

# 4. Subir com tag anterior
docker compose up -d apihost workers ingestion frontend

# 5. Validar
curl http://localhost:8080/live
```

### Via `workflow_dispatch` manual

1. Ir para: `Actions → Staging → Run workflow`
2. Não há job explícito de rollback — re-run de um commit anterior:
   - Ir para a lista de commits em `main`
   - Clicar no commit anterior estável
   - Copiar o SHA (8 chars)
   - No `staging.yml`, disparar manualmente e passar `IMAGE_TAG=<sha>`

---

## Rollback de migrations (atenção — risco de perda de dados)

⚠️ **Rollback de migrations implica perda de dados nas colunas/tabelas removidas.**
**Sempre ter backup antes de qualquer operação.**

### Rollback para migration específica

```bash
dotnet ef database update <MigrationName> \
  --project src/platform/NexTraceOne.ApiHost/NexTraceOne.ApiHost.csproj \
  --context <FullContextName> \
  --connection "<connection-string>"
```

Exemplo — IdentityDbContext:
```bash
dotnet ef database update InitialMigration \
  --project src/platform/NexTraceOne.ApiHost/NexTraceOne.ApiHost.csproj \
  --context NexTraceOne.IdentityAccess.Infrastructure.Persistence.IdentityDbContext \
  --connection "Host=pg;Database=nextraceone_identity;Username=app;Password=secret"
```

### Listar migrações aplicadas

```bash
dotnet ef migrations list \
  --project src/platform/NexTraceOne.ApiHost/NexTraceOne.ApiHost.csproj \
  --context <FullContextName> \
  --connection "<connection-string>"
```

---

## Rollback de configuração

Se o problema for de configuração (variável de ambiente errada):

```bash
# 1. Identificar variável problemática nos logs
docker compose logs apihost --tail=100 | grep -i error

# 2. Editar .env ou variável de ambiente
nano .env

# 3. Restart dos serviços afetados
docker compose up -d --force-recreate apihost

# 4. Validar
curl http://localhost:8080/ready
```

---

## Checklist de rollback

```
[ ] Identificado o motivo do rollback
[ ] Backup do banco feito (se migration envolvida)
[ ] Decidido: rollback de imagem / config / migration
[ ] Rollback executado
[ ] Health checks passando
[ ] Smoke checks passando (ver POST-DEPLOY-VALIDATION.md)
[ ] Incidente registrado com causa raiz
[ ] Equipa notificada
```

---

## Comunicação durante rollback

1. Notificar equipa imediatamente ao iniciar rollback
2. Registrar:
   - Hora do incidente
   - Sintoma observado
   - Decisão tomada (rollback vs hotfix)
   - Hora de resolução
3. Criar ticket de follow-up para investigação da causa raiz

# Wave 1 — Instalação & First-Run Experience

> **Prioridade:** Crítica — Bloqueante para adopção enterprise
> **Esforço estimado:** L (Large)
> **Módulos impactados:** `identityaccess`, `configuration`, `platform/ApiHost`
> **Referência:** [INDEX.md](./INDEX.md)

---

## Contexto

O bootstrap actual requer configuração manual de dezenas de variáveis de ambiente,
connection strings, JWT secrets e CORS origins. Para uma equipa de infra que instala
o produto pela primeira vez, esta é a maior barreira de adopção.

Benchmark de mercado: Plane, Coder e Replicated mostram que plataformas self-hosted
líderes investem fortemente em preflight checks e wizards de instalação guiados.
O Exchange 2007 já realizava preflight checks em 2007 — em 2026 é standard obrigatório.

---

## W1-01 — Preflight Check Engine

### Problema
O servidor pode ter PostgreSQL inacessível, versão incompatível, permissões incorrectas,
porta ocupada ou RAM insuficiente. Hoje, o produto falha em startup com mensagens de erro
técnicas difíceis de interpretar por equipas de infra.

### Solução
Endpoint público `GET /preflight` (sem autenticação) que executa antes do primeiro login:

```
┌─────────────────────────────────────────────────────┐
│              NexTraceOne Preflight Check             │
├─────────────────────────────────────────────────────┤
│  ✅  PostgreSQL 16.2 — acessível e compatível        │
│  ✅  Schema nextraceone — criado com sucesso         │
│  ✅  Permissões CREATE/ALTER/SELECT — confirmadas    │
│  ✅  .NET Runtime 10.0.1 — compatível               │
│  ✅  RAM disponível: 28 GB / 32 GB — adequada        │
│  ✅  Disco disponível: 420 GB — adequado             │
│  ✅  Porta 8080 — livre                              │
│  ⚠️  Ollama — não detectado (IA opcional)           │
│  ⚠️  SMTP — não configurado (notificações opcionais)│
│  ❌  OTel Collector — inacessível em :4317           │
│       → Solução: verificar se o collector está       │
│         a correr ou desactivar observabilidade       │
└─────────────────────────────────────────────────────┘
```

### Checks obrigatórios
- PostgreSQL acessível + versão ≥ 15
- Permissões `CREATE`, `ALTER TABLE`, `SELECT`, `INSERT` no schema alvo
- Espaço em disco ≥ 5 GB no path de dados
- RAM disponível ≥ 4 GB
- Portas 8080, 8090 livres
- .NET Runtime compatível
- Connection strings configuradas (todas as obrigatórias)
- JWT Secret configurado e com comprimento ≥ 32 chars
- CORS Origins configuradas

### Checks opcionais (avisos, não bloqueantes)
- Ollama acessível em :11434
- SMTP configurado
- OTel Collector acessível
- ClickHouse acessível

### Implementação sugerida
```csharp
// platform/NexTraceOne.ApiHost/Preflight/PreflightCheckService.cs
public sealed class PreflightCheckService
{
    public async Task<PreflightReport> RunAsync(CancellationToken ct);
}

// Endpoint sem autenticação — acessível antes do primeiro login
app.MapGet("/preflight", async (PreflightCheckService svc, CancellationToken ct) =>
    await svc.RunAsync(ct));
```

### Critério de aceite
- [ ] `/preflight` retorna JSON com lista de checks e estados
- [ ] Página HTML amigável em `/preflight/ui` antes do primeiro login
- [ ] Startup recusa-se a continuar se checks críticos falharem (com mensagem clara)
- [ ] Logs estruturados de cada check com contexto de diagnóstico

---

## W1-02 — Setup Wizard (First-Run)

### Problema
Após o preflight, o admin precisa de configurar: tenant principal, utilizador admin,
SMTP, URL pública, AI provider. Hoje isso exige edição de ficheiros de configuração
e reinício do serviço.

### Solução
Wizard web em `/setup` activo apenas quando `SetupCompleted = false` na base de dados:

```
Passo 1: Bem-vindo ao NexTraceOne
  → Verificar licença (online ou ficheiro)
  → Escolher idioma da plataforma

Passo 2: Configuração da Organização
  → Nome da organização
  → Domínio principal
  → Fuso horário padrão

Passo 3: Conta de Administrador
  → Nome, email, password do primeiro admin
  → (ou importar de LDAP/AD se disponível)

Passo 4: URL Pública
  → URL base da plataforma (para links em emails)
  → Teste de acessibilidade

Passo 5: Notificações (opcional)
  → Configuração SMTP
  → Envio de email de teste

Passo 6: AI Assistant (opcional)
  → Escolher: Ollama local | OpenAI | Nenhum
  → Testar conectividade e modelo

Passo 7: Resumo e Confirmação
  → Mostrar todas as escolhas
  → Aplicar e redirecionar para o dashboard
```

### Implementação sugerida
- Backend: `POST /api/v1/setup/complete` com DTO de todas as escolhas
- Frontend: rota `/setup` protegida por flag `SetupCompleted`
- Após conclusão: flag `SetupCompleted = true` na tabela `platform_settings`
- Redireccionar automaticamente para `/setup` quando `SetupCompleted = false`

### Critério de aceite
- [ ] Wizard inacessível após setup concluído
- [ ] Cada passo valida antes de avançar
- [ ] Skip disponível em passos opcionais
- [ ] Estado de wizard persistido (pode retomar se fechar o browser)
- [ ] i18n completo em todos os textos do wizard

---

## W1-03 — Configuration Validator

### Problema
Configurações inválidas são detectadas apenas em runtime, muitas vezes causando falhas
parciais difíceis de diagnosticar.

### Solução
Endpoint `GET /api/v1/admin/config-health` disponível para admins:

```json
{
  "status": "degraded",
  "checks": [
    {
      "key": "Smtp__Host",
      "status": "warning",
      "message": "SMTP não configurado — notificações desactivadas",
      "suggestion": "Configurar Smtp__Host, Smtp__Port e Smtp__From"
    },
    {
      "key": "Jwt__Secret",
      "status": "ok",
      "message": "JWT Secret configurado com 48 caracteres"
    },
    {
      "key": "ConnectionStrings__IdentityDatabase",
      "status": "ok",
      "message": "PostgreSQL acessível — latência 2ms"
    }
  ]
}
```

### Critério de aceite
- [ ] Endpoint disponível apenas para role `PlatformAdmin`
- [ ] Cobre todas as variáveis obrigatórias e opcionais
- [ ] Inclui sugestão de resolução para cada problema
- [ ] Widget visível no painel de admin com alerta se `status != ok`

---

## W1-04 — Seed Inteligente de Demonstração

### Problema
Após instalação, o produto está completamente vazio. O Time to First Value é alto porque
o utilizador não tem referência de como o produto funciona com dados reais.

### Solução
Opção no Setup Wizard de criar **tenant de demonstração** com:
- 5 serviços de exemplo com ownership, contratos e dependências reais
- 3 mudanças com blast radius, evidências e histórico
- 2 incidentes correlacionados com mudanças
- Runbooks de exemplo
- 1 SLO configurado

> Os dados de demonstração devem ser claramente marcados como `is_demo = true`
> e elimináveis com um único botão no painel de admin.

### Critério de aceite
- [ ] Seed executado em < 10 segundos
- [ ] Banner visível em todo o tenant demo indicando que são dados de exemplo
- [ ] Botão "Eliminar dados de demonstração" no painel de admin
- [ ] Seed não executa em tenants de produção existentes

---

## Dependências e Riscos

| Dependência | Notas |
|---|---|
| `ConfigurationDbContext` | Guardar flag `SetupCompleted` |
| `IdentityDbContext` | Criar primeiro utilizador admin via wizard |
| Frontend rota `/setup` e `/preflight` | Novas rotas sem autenticação |

| Risco | Mitigação |
|---|---|
| Wizard pode ser aberto por utilizador não autorizado | Bloquear após `SetupCompleted = true`; HTTPS obrigatório |
| Seed de demo pode confundir com dados reais | Banner persistente + flag `is_demo` em todas as entidades |

---

## Referências de Mercado

- Plane self-hosted: wizard de setup em 4 passos com skip de opcionais
- Replicated: preflight checks com bundle de suporte integrado
- Exchange 2007+: prerequisite checks antes de qualquer instalação
- Coder: `/health` público com checks categorizados por severidade

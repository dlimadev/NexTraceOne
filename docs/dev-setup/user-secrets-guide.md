# Configuração de Desenvolvimento Local — User Secrets

## Visão geral

O `appsettings.Development.json` contém valores de placeholder para desenvolvimento local.
**Não commitar passwords ou segredos reais** neste ficheiro.

Para sobrepor valores sensíveis localmente, usar o mecanismo `.NET User Secrets`.

---

## Configuração inicial (uma vez por máquina)

```bash
cd src/platform/NexTraceOne.ApiHost

# Inicializar user secrets para o projeto
dotnet user-secrets init

# Configurar JWT secret local
dotnet user-secrets set "Jwt:Secret" "local-dev-jwt-secret-minimum-32-chars-long-replace-me"

# Configurar passwords da base de dados (se diferente do appsettings.Development.json)
dotnet user-secrets set "ConnectionStrings:NexTraceOne" "Host=localhost;Port=5432;Database=nextraceone;Username=nextraceone;Password=SEU_PASSWORD;Maximum Pool Size=20"
dotnet user-secrets set "ConnectionStrings:IdentityDatabase" "Host=localhost;Port=5432;Database=nextraceone;Username=nextraceone;Password=SEU_PASSWORD;Maximum Pool Size=20"
# ... repetir para cada connection string necessária
```

---

## Verificar secrets configurados

```bash
dotnet user-secrets list --project src/platform/NexTraceOne.ApiHost
```

---

## Estrutura de prioridade de configuração

O .NET carrega configuração na seguinte ordem (último sobrepõe anterior):

1. `appsettings.json`
2. `appsettings.Development.json`
3. **User Secrets** (apenas em `Development` environment)
4. Variáveis de ambiente

---

## Para CI/CD e produção

Em CI e produção, usar **variáveis de ambiente** ou **secrets do pipeline** (GitHub Actions secrets, Azure Key Vault, etc.).

Nunca usar `appsettings.Production.json` com valores reais em source control.

---

## Ficheiro `.gitignore`

O seguinte padrão deve estar no `.gitignore` do projeto (já confirmado):

```
# User Secrets
**/secrets.json
```

Os User Secrets do .NET são armazenados fora do repositório em:
- **Windows:** `%APPDATA%\Microsoft\UserSecrets\<user-secrets-id>\secrets.json`
- **macOS/Linux:** `~/.microsoft/usersecrets/<user-secrets-id>/secrets.json`

---

## Referência

- [Safe storage of app secrets in development — .NET docs](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)

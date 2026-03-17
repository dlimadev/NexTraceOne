# Notas de Hardening On-Premise — NexTraceOne

> Documento de referência para distribuição e execução segura em ambiente on-premise.
> Atualizado em Março 2026.

---

## Verdade Arquitetural

Em ambiente on-premise, **não é possível prometer proteção absoluta** contra um operador
com controle total da infraestrutura (acesso root ao servidor, acesso ao banco de dados,
acesso ao filesystem).

O objetivo realista e obrigatório é:
1. **Nunca distribuir código-fonte** — apenas artefatos compilados/publicados
2. **Reduzir exposição** do código e facilitar detecção de adulteração
3. **Dificultar engenharia reversa** indevida (não prevenir)
4. **Proteger segredos e dados sensíveis** com externalização adequada
5. **Registrar e documentar** limites, riscos residuais e controles aplicados

---

## 1. Como Distribuir a Aplicação sem Código-Fonte

### Backend (.NET)

| Item | Regra |
|------|-------|
| Código-fonte (.cs, .csproj) | ❌ Nunca incluir no pacote de distribuição |
| Assemblies compilados (.dll) | ✅ Incluir — artefato publicado |
| Self-contained publish | ✅ Recomendado — inclui runtime .NET, elimina dependência de SDK |
| ReadyToRun (R2R) | ✅ Recomendado — pré-compilação AOT que dificulta decompilação |
| PDB files (debug symbols) | ❌ Não incluir em distribuição de produção |
| XML doc files | ❌ Não incluir em distribuição de produção |
| appsettings.Development.json | ❌ Não incluir em distribuição de produção |
| Source files, tests | ❌ Nunca incluir |

**Comando de publish recomendado:**
```bash
dotnet publish src/platform/NexTraceOne.ApiHost/NexTraceOne.ApiHost.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishReadyToRun=true \
  -p:DebugType=none \
  -p:DebugSymbols=false \
  -p:GenerateDocumentationFile=false \
  -o ./publish/apihost
```

### Frontend (React/Vite)

| Item | Regra |
|------|-------|
| Código-fonte (.tsx, .ts) | ❌ Nunca incluir no pacote de distribuição |
| Source maps (.map) | ❌ Desativados em produção (`sourcemap: false`) |
| Build de produção (dist/) | ✅ Incluir — JS/CSS minificados |
| node_modules | ❌ Nunca incluir |
| Arquivos de configuração de dev | ❌ Não incluir (.env.local, vite.config.ts, etc.) |
| console.log statements | ❌ Removidos via terser (`drop_console: true`) |

**Medidas implementadas no Vite:**
- `sourcemap: false` — sem source maps
- `minify: 'terser'` com `drop_console` e `drop_debugger`
- Nomes de assets com hash (sem estrutura interna exposta)

---

## 2. O Que Deve e Não Deve Ir nos Pacotes de Deploy

### ✅ Incluir

- Assemblies compilados (.dll) do backend
- Runtime .NET (self-contained)
- Frontend build (dist/ com HTML/JS/CSS minificados)
- `appsettings.json` (template com valores vazios para segredos)
- Scripts de instalação/configuração
- Documentação operacional
- Migration scripts do banco de dados
- Health check endpoints
- Checksums SHA-256 dos artefatos

### ❌ Não Incluir

- Código-fonte (.cs, .tsx, .ts, .csproj, .sln)
- Testes (unitários, integração, e2e)
- Source maps (.map)
- PDB/debug symbols
- appsettings.Development.json
- .env files com segredos
- node_modules
- Ferramentas de desenvolvimento (SDK, compilers)
- Postman collections
- Documentação interna de desenvolvimento (docs/ do repositório)
- CI/CD scripts
- Git history (.git/)

---

## 3. Política de Build de Produção

| Aspecto | Regra |
|---------|-------|
| Configuração | `Release` (nunca `Debug`) |
| Source maps | Desativados |
| Debug symbols | Não incluídos |
| Console.log | Removido via terser |
| Otimização | Minificação + tree shaking + ReadyToRun |
| Self-contained | Recomendado para independência de runtime |
| Trimming | Avaliar com cautela (pode quebrar reflection) |

---

## 4. Segredos e Configuração Externa

### Segredos que DEVEM ser externalizados

| Segredo | Onde Configurar |
|---------|----------------|
| Connection string do banco | Variável de ambiente ou secrets manager |
| JWT Secret | Variável de ambiente ou secrets manager |
| SMTP credentials | Variável de ambiente ou secrets manager |
| OpenTelemetry endpoint | Variável de ambiente |

### Regras

- **Nunca** incluir segredos em `appsettings.json` distribuído
- O template de configuração deve ter valores **vazios** para todos os segredos
- Documentar variáveis de ambiente esperadas no README operacional
- Suportar `DOTNET_ENVIRONMENT` ou `ASPNETCORE_ENVIRONMENT` para seleção de perfil
- Validar que segredos obrigatórios estão configurados no startup (fail fast)

### Variáveis de Ambiente Reconhecidas

```
NEXTRACE_SKIP_INTEGRITY=true|false   # Pular verificação de integridade (dev only)
NEXTRACE_AUTO_MIGRATE=true|false     # Auto-migrate banco (default: false em prod)
ConnectionStrings__NexTraceOne=...   # Connection string do PostgreSQL
Jwt__Secret=...                      # Segredo JWT (mín. 256 bits)
```

---

## 5. Riscos Residuais de Ambiente On-Premise

| Risco | Severidade | Mitigação |
|-------|------------|-----------|
| Operador pode inspecionar assemblies .NET | Alto (inerente) | ReadyToRun + sem PDB + obfuscator opcional |
| Operador pode inspecionar JS do frontend | Alto (inerente) | Minificação + sem source maps + drop_console |
| Operador pode acessar banco de dados | Alto (inerente) | Encryption at rest (responsabilidade infra) |
| Operador pode interceptar tráfego | Médio | HTTPS obrigatório + HSTS + certificate pinning opcional |
| Adulteração de assemblies | Médio | Verificação de integridade no boot + signing |
| Vazamento de connection string | Médio | Externalização de segredos + permissions de arquivo |

**Importante:** Estes riscos são inerentes a qualquer software on-premise. A mitigação
é de responsabilidade compartilhada entre o vendedor (NexTraceOne) e o operador (cliente).

---

## 6. Boas Práticas de Empacotamento, Assinatura e Integridade

### Empacotamento
- Gerar artefatos em CI/CD controlado (não na máquina de dev)
- Usar reproducible builds quando possível
- Incluir manifesto de versão no artefato (build number, commit SHA, data)

### Assinatura (Recomendado)
- Assinar assemblies .NET com strong name
- Assinar pacote de distribuição com código de certificado (Authenticode para Windows)
- Publicar checksums SHA-256 de cada artefato

### Integridade
- `AssemblyIntegrityChecker.VerifyOrThrow()` no boot da aplicação
- Health check endpoint para verificação de status
- Logging de versão e build number no startup
- Documentar procedimento de verificação de integridade para o cliente

### Verificação pelo Cliente
O cliente on-premise deve poder:
1. Verificar checksums dos artefatos recebidos
2. Validar assinaturas digitais (quando implementadas)
3. Comparar versão reportada pela aplicação com a versão distribuída
4. Consultar audit log para detectar comportamento anômalo

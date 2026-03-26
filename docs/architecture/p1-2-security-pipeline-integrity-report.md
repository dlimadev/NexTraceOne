# P1.2 — Security Pipeline Integrity Report

**Data de execução:** 2026-03-26  
**Classificação:** HIGH / P1 — Segurança de pipeline CI  
**Estado:** CONCLUÍDO

---

## 1. Contexto

A auditoria do estado actual do NexTraceOne identificou que `.github/workflows/security.yml`
utilizava `--build-arg NEXTRACE_SKIP_INTEGRITY=true` no step de build de imagens Docker, durante
o job `docker-scan` (Trivy). Esta flag tem o efeito de sinalizar um bypass explícito ao mecanismo
de integridade de assemblies da plataforma, comprometendo a credibilidade do pipeline de segurança
por design.

---

## 2. Ficheiros alterados

| Ficheiro | Tipo de alteração |
|---|---|
| `.github/workflows/security.yml` | Removida a linha `--build-arg NEXTRACE_SKIP_INTEGRITY=true` do step `Build Docker image` no job `docker-scan` |

---

## 3. Localização exacta do bypass

**Ficheiro:** `.github/workflows/security.yml`  
**Job:** `docker-scan`  
**Step:** `Build Docker image`  
**Linha anterior:** 160

```yaml
# ANTES (com bypass)
- name: Build Docker image
  run: |
    docker build \
      -f ${{ matrix.dockerfile }} \
      -t ${{ matrix.image }}:scan \
      --build-arg NEXTRACE_SKIP_INTEGRITY=true \
      .
  env:
    DOCKER_BUILDKIT: "1"

# DEPOIS (sem bypass)
- name: Build Docker image
  run: |
    docker build \
      -f ${{ matrix.dockerfile }} \
      -t ${{ matrix.image }}:scan \
      .
  env:
    DOCKER_BUILDKIT: "1"
```

Nenhuma outra ocorrência de `NEXTRACE_SKIP_INTEGRITY` foi encontrada nos workflows `.github/workflows/*.yml`.

---

## 4. Análise técnica do mecanismo

### 4.1 Como funciona o `AssemblyIntegrityChecker`

O checker (`AssemblyIntegrityChecker.VerifyOrThrow()`) executa em `Program.cs` **antes de qualquer serviço** ser inicializado:

```csharp
if (!string.Equals(Environment.GetEnvironmentVariable("NEXTRACE_SKIP_INTEGRITY"), "true", ...))
{
    AssemblyIntegrityChecker.VerifyOrThrow();
}
```

O checker:
1. Localiza todos os ficheiros `NexTraceOne*.dll` no diretório de execução
2. Para cada DLL, verifica se existe um ficheiro `.sha256` correspondente
3. **Se o ficheiro `.sha256` não existir, o assembly é ignorado silenciosamente**
4. Só falha se o ficheiro `.sha256` existir mas o hash não coincidir

### 4.2 Por que o bypass não era tecnicamente necessário

Os Dockerfiles (`Dockerfile.apihost`, `Dockerfile.workers`, `Dockerfile.ingestion`, `Dockerfile.frontend`) **não declaram** `ARG NEXTRACE_SKIP_INTEGRITY`. Consequentemente:

- Com BuildKit activo (`DOCKER_BUILDKIT=1`), o `--build-arg` não declarado era ignorado silenciosamente
- O build Docker nunca propagava esta variável como `ENV` para a imagem resultante
- A integridade só é verificada em **tempo de execução da aplicação**, não durante `docker build`
- O Trivy faz scan da imagem em repouso (camadas de sistema de ficheiros), não executa a aplicação

**Conclusão:** `--build-arg NEXTRACE_SKIP_INTEGRITY=true` não tinha efeito prático no Docker build,
mas documentava uma intenção de bypass que comprometia a postura de segurança do pipeline.

### 4.3 Situação nos ficheiros de hash

No estado actual da plataforma, os ficheiros `.sha256` **não são gerados** pelo pipeline de CI. O pipeline de build/assinar assemblies com `.sha256` é uma capacidade futura (parte do pipeline `build → obfuscate → AOT → sign` referenciado no comentário do `AssemblyIntegrityChecker`). Portanto, em builds de CI normais, o checker passa silenciosamente por não encontrar ficheiros `.sha256`.

---

## 5. Ajustes realizados para o workflow continuar funcional

Não foram necessários ajustes adicionais além da remoção da flag. O pipeline `docker-scan` continua a:

1. Fazer checkout do repositório
2. Instalar .NET (para Dockerfiles de backend)
3. Executar `docker build` sem bypass
4. Executar `trivy-action` na imagem resultante para scan de vulnerabilidades
5. Fazer upload dos resultados SARIF para o GitHub Security tab

O comportamento do Trivy não é afectado pela remoção do `--build-arg`.

---

## 6. Verificação de outros pipelines

| Workflow | Uso de `NEXTRACE_SKIP_INTEGRITY` | Estado |
|---|---|---|
| `security.yml` | Removido nesta fase | ✅ LIMPO |
| `ci.yml` | Não utilizado | ✅ LIMPO |
| `staging.yml` | Não utilizado | ✅ LIMPO |
| `production.yml` | Não utilizado | ✅ LIMPO |
| `e2e.yml` | Não utilizado | ✅ LIMPO |

Ocorrências legítimas fora de workflows:
- `docker-compose.override.yml`: usa `NEXTRACE_SKIP_INTEGRITY: "true"` — aceitável em ambiente local de desenvolvimento
- `tests/platform/...ApiHostPostgreSqlFixture.cs` e `ApiE2EFixture.cs`: definem via `Environment.SetEnvironmentVariable` — aceitável em contexto de testes

---

## 7. Critérios de aceite verificados

| Critério | Estado |
|---|---|
| `security.yml` não usa mais `NEXTRACE_SKIP_INTEGRITY=true` | ✅ |
| Workflow de segurança continua executável | ✅ |
| Nenhum bypass equivalente escondido no mesmo fluxo | ✅ |
| Relatório final gerado | ✅ |

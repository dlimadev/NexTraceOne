# Contract Conformance — Integração CI/CD

> Parte do plano: [01-OVERVIEW.md](01-OVERVIEW.md)

---

## 1. Abordagens de identificação do serviço no CI

O CI não deve ter de gerir GUIDs internos do NexTraceOne. Existem três abordagens, que podem ser usadas em combinação:

| Abordagem | Complexidade | Segurança | Recomendação |
|-----------|-------------|-----------|--------------|
| **A — CI Token com binding** | Baixa | Alta | Padrão para equipas |
| **B — `.nextraceone.yaml` no repo** | Muito baixa | Média | Onboarding inicial |
| **C — Variáveis de pipeline explícitas** | Média | Média | Casos especiais |

---

## 2. Abordagem A — CI Token com Binding (Recomendada)

O owner da equipa cria um token no NexTraceOne com binding ao serviço. O token é armazenado como secret no repositório CI.

### Criação do token (UI ou API)

Via NexTraceOne UI → Contracts → CI Tokens → New Token

Ou via API:
```bash
curl -X POST https://nextraceone.example.com/api/contracts/ci-tokens \
  -H "Authorization: Bearer $USER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "serviceId": "uuid-do-servico",
    "name": "payment-api-github-ci",
    "allowedEnvironments": ["development", "pre-production"],
    "expiresAt": "2027-04-10T00:00:00Z"
  }'
```

O `rawToken` retornado (ex: `ctr_ci_pXXXXXXXXXXXXXXXXXXXX`) é guardado como secret no repositório.

### Uso no GitHub Actions

```yaml
# .github/workflows/ci.yml
jobs:
  contract-conformance:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Start service for spec extraction
        run: docker compose up -d app

      - name: Wait for service health
        run: curl --retry 10 --retry-delay 2 http://localhost:5000/health

      - name: NexTraceOne — Contract Conformance Gate
        uses: nextraceone/contract-gate@v1
        with:
          nextraceone_url: ${{ vars.NEXTRACEONE_URL }}
          ci_token: ${{ secrets.NEXTRACEONE_CI_TOKEN }}
          spec_url: http://localhost:5000/swagger/v1/swagger.json
          environment: pre-production
          # fail_on: breaking (default)
          # score_threshold: 80 (default from NexTraceOne policy)

      - name: Stop service
        if: always()
        run: docker compose down
```

**O que o action faz internamente:**
1. Extrai a spec do serviço (`spec_url`)
2. Chama `POST /contracts/validate-implementation` com o CI token no header
3. O NexTraceOne resolve o serviço a partir do binding do token
4. Avalia a resposta e decide se falha o step (`fail_on`)
5. Publica o relatório de desvios como annotation no PR

---

## 3. Abordagem B — Ficheiro `.nextraceone.yaml`

Cada repositório de serviço tem um ficheiro de configuração na raiz. Não requer token com binding — usa um API key de tenant com scope `contracts:validate`.

### Estrutura do ficheiro

```yaml
# .nextraceone.yaml
# Ficheiro de configuração do NexTraceOne para este repositório.
# Gerado automaticamente ou criado manualmente pelo owner da equipa.

service:
  slug: payment-api                    # Slug do serviço no NexTraceOne (obrigatório)
  api-asset-id: null                   # Opcional — evita lookup por slug

conformance:
  spec-endpoint: /swagger/v1/swagger.json  # Path para extrair a spec do serviço
  spec-format: openapi-json                # openapi-json | openapi-yaml | asyncapi | wsdl
  ignored-paths:                           # Paths ignorados no diff
    - /health
    - /metrics
    - /swagger
    - /swagger/{**}
  fail-on: breaking                        # breaking | any-drift | score-below-threshold | never

environment-mapping:
  # branch → ambiente NexTraceOne
  main: production
  develop: pre-production
  release/*: pre-production
  feature/*: development
  hotfix/*: pre-production
```

### Uso no GitHub Actions com `.nextraceone.yaml`

```yaml
- name: NexTraceOne — Contract Conformance Gate
  uses: nextraceone/contract-gate@v1
  with:
    nextraceone_url: ${{ vars.NEXTRACEONE_URL }}
    api_key: ${{ secrets.NEXTRACEONE_API_KEY }}
    # sem ci_token — usa .nextraceone.yaml para resolver serviço
    # sem service_slug — lido do ficheiro
    # sem environment — mapeado do branch actual via environment-mapping
```

O action:
1. Lê `.nextraceone.yaml` da raiz do repo
2. Determina o ambiente pelo branch actual
3. Extrai a spec do serviço
4. Chama `POST /contracts/validate-implementation` com `serviceSlug` + `environmentName`

---

## 4. Abordagem C — Variáveis de pipeline explícitas

Para casos onde não é viável ter `.nextraceone.yaml` ou CI Token com binding.

```yaml
- name: NexTraceOne — Contract Conformance Gate
  uses: nextraceone/contract-gate@v1
  with:
    nextraceone_url: ${{ vars.NEXTRACEONE_URL }}
    api_key: ${{ secrets.NEXTRACEONE_API_KEY }}
    service_slug: ${{ vars.NEXTRACEONE_SERVICE_SLUG }}
    environment: ${{ vars.TARGET_ENVIRONMENT }}
    spec_url: http://localhost:5000/swagger/v1/swagger.json
    fail_on: breaking
```

---

## 5. Parâmetros do GitHub Action `nextraceone/contract-gate@v1`

| Parâmetro | Obrigatório | Padrão | Descrição |
|-----------|-------------|--------|-----------|
| `nextraceone_url` | Sim | — | URL base do NexTraceOne |
| `ci_token` | Não | — | Token CI com binding (Abordagem A) |
| `api_key` | Não | — | API key de utilizador/tenant (Abordagens B/C) |
| `service_slug` | Não | do `.nextraceone.yaml` | Slug do serviço |
| `environment` | Não | mapeado do branch | Ambiente alvo |
| `spec_url` | Não | do `.nextraceone.yaml` | URL da spec do serviço |
| `spec_file` | Não | — | Caminho para ficheiro de spec (alternativa a `spec_url`) |
| `fail_on` | Não | `breaking` | `breaking` \| `any-drift` \| `score-below-threshold` \| `never` |
| `score_threshold` | Não | política do NexTraceOne | Override local do threshold de score |
| `commit_sha` | Não | `$GITHUB_SHA` | SHA do commit (auto-detectado) |
| `branch_name` | Não | `$GITHUB_REF_NAME` | Nome do branch (auto-detectado) |
| `pipeline_run_id` | Não | `$GITHUB_RUN_ID` | ID do run do pipeline (auto-detectado) |

**Outputs do action:**

| Output | Descrição |
|--------|-----------|
| `conformance_status` | Compliant \| Drifted \| Breaking \| Error |
| `conformance_score` | Score numérico (0-100) |
| `deviation_count` | Número total de desvios |
| `breaking_count` | Número de desvios breaking |
| `check_id` | ID do `ConformanceCheck` registado no NexTraceOne |
| `recommendation` | Approve \| Warn \| Block |

---

## 6. Integração com outros sistemas CI/CD

### Jenkins (Declarative Pipeline)

```groovy
pipeline {
  stages {
    stage('Contract Conformance') {
      steps {
        sh '''
          curl -s -X POST ${NEXTRACEONE_URL}/api/contracts/validate-implementation \
            -H "Authorization: CiToken ${NEXTRACEONE_CI_TOKEN}" \
            -H "Content-Type: application/json" \
            -d '{
              "resolution": {
                "environmentName": "pre-production"
              },
              "implementedSpecContent": "'$(curl -s http://localhost:5000/swagger/v1/swagger.json | base64 -w 0)'",
              "implementedSpecFormat": "openapi-json",
              "sourceSystem": "jenkins",
              "pipelineRunId": "'${BUILD_NUMBER}'"
            }' \
            -o conformance-result.json

          STATUS=$(cat conformance-result.json | python3 -c "import sys,json; print(json.load(sys.stdin)['recommendation'])")
          if [ "$STATUS" = "Block" ]; then
            echo "Contract conformance check failed. See report:"
            cat conformance-result.json
            exit 1
          fi
        '''
      }
    }
  }
}
```

### GitLab CI

```yaml
contract-conformance:
  stage: test
  script:
    - |
      RESULT=$(curl -s -X POST "$NEXTRACEONE_URL/api/contracts/validate-implementation" \
        -H "Authorization: CiToken $NEXTRACEONE_CI_TOKEN" \
        -H "Content-Type: application/json" \
        -d "{
          \"resolution\": {\"environmentName\": \"pre-production\"},
          \"implementedSpecContent\": \"$(curl -s http://localhost:5000/swagger/v1/swagger.json | jq -Rs .)\",
          \"implementedSpecFormat\": \"openapi-json\",
          \"sourceSystem\": \"gitlab-ci\",
          \"pipelineRunId\": \"$CI_PIPELINE_ID\"
        }")
      RECOMMENDATION=$(echo $RESULT | jq -r '.recommendation')
      [ "$RECOMMENDATION" != "Block" ] || (echo "$RESULT" && exit 1)
  artifacts:
    reports:
      junit: conformance-result.xml
```

### Azure DevOps

```yaml
- task: Bash@3
  displayName: 'NexTraceOne Contract Conformance'
  inputs:
    targetType: inline
    script: |
      SPEC=$(curl -s http://localhost:5000/swagger/v1/swagger.json)
      RESULT=$(curl -s -X POST "$(NEXTRACEONE_URL)/api/contracts/validate-implementation" \
        -H "Authorization: CiToken $(NEXTRACEONE_CI_TOKEN)" \
        -H "Content-Type: application/json" \
        -d "{
          \"resolution\": {\"environmentName\": \"$(TARGET_ENVIRONMENT)\"},
          \"implementedSpecContent\": $(echo $SPEC | jq -Rs .),
          \"implementedSpecFormat\": \"openapi-json\",
          \"sourceSystem\": \"azure-devops\",
          \"pipelineRunId\": \"$(Build.BuildId)\"
        }")
      RECOMMENDATION=$(echo $RESULT | jq -r '.recommendation')
      echo "##vso[task.setvariable variable=ConformanceStatus]$(echo $RESULT | jq -r '.status')"
      [ "$RECOMMENDATION" != "Block" ] || (echo "$RESULT" && exit 1)
```

---

## 7. Alternativa: Spec estática gerada no build

Para serviços .NET que geram a spec no build (sem precisar arrancar o serviço):

```yaml
- name: Generate OpenAPI spec
  run: |
    dotnet tool install --global Microsoft.dotnet-openapi || true
    dotnet build src/MyService/MyService.csproj
    dotnet run --project src/MyService/MyService.csproj \
      --no-build -- \
      --urls "http://localhost:5000" &
    sleep 3
    curl -s http://localhost:5000/swagger/v1/swagger.json > spec.json
    kill %1

- name: NexTraceOne — Contract Conformance Gate
  uses: nextraceone/contract-gate@v1
  with:
    nextraceone_url: ${{ vars.NEXTRACEONE_URL }}
    ci_token: ${{ secrets.NEXTRACEONE_CI_TOKEN }}
    spec_file: spec.json           # usa ficheiro em vez de URL
    environment: pre-production
```

---

## 8. Fluxo completo no pipeline

```
git push → CI arranca
      │
      ├─ Build & Unit Tests
      ├─ [NOVO] Arrancar serviço em modo test / gerar spec
      ├─ [NOVO] Contract Conformance Gate
      │     ├─ Extrai spec (URL ou ficheiro)
      │     ├─ POST /contracts/validate-implementation
      │     ├─ NexTraceOne: resolve contrato → diff → policy → resultado
      │     ├─ Regista ConformanceCheck + entrada de Changelog
      │     ├─ Publica relatório no PR (annotations)
      │     └─ Exit 1 se recommendation = Block
      │
      ├─ Integration Tests
      ├─ Security Scan
      └─ Deploy para ambiente alvo

Deploy → RegisterContractDeployment (automático se auto_register=true)
       → Gate de promoção verifica ConformanceCheck válido
```

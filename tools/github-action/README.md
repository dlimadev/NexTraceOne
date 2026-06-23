# NexTraceOne GitHub Actions

Composite actions para integrar a governança do NexTraceOne em pipelines CI/CD.
Todas consomem a API REST da plataforma (`/api/v1/...`) via `curl` + `jq`, com retry/backoff em falhas transitórias.

| Action | Diretório | Gate | Endpoint |
|---|---|---|---|
| Change Confidence Gate | `nexone-change-confidence-gate` | Bloqueia release abaixo do confidence score | `GET /api/v1/changes/{releaseId}/confidence` |
| Contract Drift Gate | `nexone-contract-drift-gate` | Bloqueia breaking change de contrato | `GET /api/v1/contracts/diff` |
| SCA Gate | `nexone-sca-gate` | Bloqueia vulnerabilidades de dependências acima do limiar | `GET /api/v1/catalog/dependencies/{serviceId}/health` |

> Pré-requisito: `jq` disponível no runner (já presente nos runners GitHub-hosted `ubuntu-*`).
> Autenticação: passe sempre o token via `secrets.*` — nunca em texto plano.

---

## Change Confidence Gate

```yaml
- uses: dlimadev/NexTraceOne/tools/github-action/nexone-change-confidence-gate@main
  with:
    release-id: ${{ github.sha }}
    api-url: https://nextraceone.example.com
    api-token: ${{ secrets.NEXTRACE_TOKEN }}
    min-confidence-score: "70"   # opcional (default 70)
```

Outputs: `score`, `tier`.

## Contract Drift Gate

```yaml
- uses: dlimadev/NexTraceOne/tools/github-action/nexone-contract-drift-gate@main
  with:
    from-contract-id: ${{ vars.BASE_CONTRACT_ID }}
    to-contract-id: ${{ vars.PR_CONTRACT_ID }}
    api-url: https://nextraceone.example.com
    api-token: ${{ secrets.NEXTRACE_TOKEN }}
    fail-on-breaking: "true"     # opcional (default true)
```

Outputs: `has-breaking-changes`, `summary`.

## SCA Gate (dependency vulnerabilities)

```yaml
- uses: dlimadev/NexTraceOne/tools/github-action/nexone-sca-gate@main
  with:
    service-id: ${{ vars.SERVICE_ID }}
    api-url: https://nextraceone.example.com
    api-token: ${{ secrets.NEXTRACE_TOKEN }}
    fail-on: "high"              # low | medium | high | critical (default high)
    max-allowed: "0"             # nº máximo de vulns no/acima do limiar (default 0)
```

Outputs: `health-score`, `offending-count`.

---

## Equivalência com o CLI `nex`

Os mesmos gates podem ser executados localmente/num runner com o CLI:

```bash
nex confidence score <releaseId> --min-score 70
nex contract diff --from <id> --to <id>          # ver drift de contrato
nex security deps <serviceId> --fail-on high     # gate de SCA por serviço
nex security vulnerable --min-severity high --fail-on-found
```

## Notas

- As actions falham (exit 1) quando o gate é violado, bloqueando o job — coloque-as antes do passo de deploy/merge.
- Em ambientes air-gapped, garanta conectividade do runner à API NexTraceOne (as actions não acessam serviços externos além da própria plataforma).
- Para anotações nativas na aba **Security** do GitHub, combine com a publicação de SARIF dos scanners (Trivy/CodeQL) já presentes no workflow `security.yml`.
</content>

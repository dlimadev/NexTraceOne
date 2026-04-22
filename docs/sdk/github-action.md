# NexOne Change Confidence Gate — GitHub Action

Block deployments when the change confidence score is below your configured threshold.

## Usage

```yaml
jobs:
  confidence-gate:
    runs-on: ubuntu-latest
    steps:
      - name: Check change confidence
        uses: ./tools/github-action/nexone-change-confidence-gate
        with:
          release-id: ${{ github.ref_name }}
          api-url: ${{ vars.NEXONE_API_URL }}
          api-token: ${{ secrets.NEXONE_API_TOKEN }}
          min-confidence-score: "70"
```

## Inputs

| Input | Required | Default | Description |
|-------|----------|---------|-------------|
| `release-id` | ✅ | — | Release identifier to score |
| `api-url` | ✅ | — | NexTraceOne API base URL |
| `api-token` | ✅ | — | Bearer token for authentication |
| `min-confidence-score` | ❌ | `70` | Minimum acceptable score (0–100) |

## Outputs

| Output | Description |
|--------|-------------|
| `score` | Numeric confidence score returned by the API |
| `tier` | Confidence tier (e.g., `High`, `Medium`, `Low`) |

## Behaviour

- The action calls `GET /api/v1/changes/{release-id}/confidence`
- If the score is **below** `min-confidence-score`, the step fails with exit code 1
- If the API is unreachable, the step fails with exit code 1
- Annotates the workflow run with the score via `::notice::`

## Full Pipeline Example

```yaml
name: Release Gate

on:
  push:
    tags: ['v*']

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: NexOne confidence gate
        uses: ./tools/github-action/nexone-change-confidence-gate
        id: gate
        with:
          release-id: ${{ github.ref_name }}
          api-url: https://nextraceone.example.com
          api-token: ${{ secrets.NEXONE_TOKEN }}
          min-confidence-score: "80"

      - name: Deploy to production
        if: success()
        run: echo "Deploying ${{ github.ref_name }} with score ${{ steps.gate.outputs.score }}"
```

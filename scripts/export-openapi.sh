#!/usr/bin/env bash
# ═══════════════════════════════════════════════════════════════════════════════
# NexTraceOne — Exportar OpenAPI / Swagger JSON
#
# Exporta o documento OpenAPI da ApiHost para um ficheiro artefacto.
# Suporta dois modos:
#   1. Via instância em execução (curl contra API viva)
#   2. Via dotnet-openapi / Microsoft.AspNetCore.OpenApi CLI tool
#
# Uso:
#   bash scripts/export-openapi.sh
#   bash scripts/export-openapi.sh --url http://localhost:5000 --out openapi.json
#   bash scripts/export-openapi.sh --live --url https://staging.nextraceone.io
#   bash scripts/export-openapi.sh --help
#
# Variáveis de ambiente:
#   APIHOST_URL     URL base da ApiHost (default: http://localhost:5000)
#   OPENAPI_OUT     Caminho de saída do JSON (default: artifacts/openapi.json)
#
# Exit codes:
#   0   Exportação concluída com sucesso
#   1   Falha na exportação
# ═══════════════════════════════════════════════════════════════════════════════
set -euo pipefail

# ── Defaults ──────────────────────────────────────────────────────────────────
APIHOST_URL="${APIHOST_URL:-http://localhost:5000}"
OPENAPI_OUT="${OPENAPI_OUT:-artifacts/openapi.json}"
LIVE_MODE=false
HELP=false

# ── Parse args ────────────────────────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
  case "$1" in
    --url)       APIHOST_URL="$2"; shift 2 ;;
    --out)       OPENAPI_OUT="$2"; shift 2 ;;
    --live)      LIVE_MODE=true; shift ;;
    --help|-h)   HELP=true; shift ;;
    *)           echo "Argumento desconhecido: $1" >&2; exit 1 ;;
  esac
done

if [[ "$HELP" == true ]]; then
  head -24 "$0" | tail -22
  exit 0
fi

# ── Preparar pasta de saída ───────────────────────────────────────────────────
OUT_DIR="$(dirname "$OPENAPI_OUT")"
mkdir -p "$OUT_DIR"

echo "═══════════════════════════════════════"
echo " NexTraceOne — Export OpenAPI"
echo "═══════════════════════════════════════"
echo " Target URL : $APIHOST_URL"
echo " Output     : $OPENAPI_OUT"
echo ""

# ── Modo 1: Curl contra instância viva ───────────────────────────────────────
export_via_live_instance() {
  local url="$APIHOST_URL/openapi/v1.json"
  echo "→ A exportar via instância viva: $url"

  if ! command -v curl &>/dev/null; then
    echo "ERRO: curl não encontrado. Instalar curl ou usar modo offline." >&2
    exit 1
  fi

  local http_code
  http_code=$(curl -sf -w "%{http_code}" -o "$OPENAPI_OUT" "$url" 2>&1 || true)

  if [[ "$http_code" == "200" ]]; then
    echo "✓ OpenAPI exportado com sucesso → $OPENAPI_OUT"
    echo "  Tamanho: $(wc -c < "$OPENAPI_OUT") bytes"
  else
    echo "ERRO: A API respondeu com HTTP $http_code em $url" >&2
    echo "  Certifique-se de que a ApiHost está a correr em $APIHOST_URL" >&2
    exit 1
  fi
}

# ── Modo 2: Microsoft.AspNetCore.OpenApi dotnet tool ─────────────────────────
export_via_dotnet_tool() {
  local api_project
  api_project="$(dirname "$0")/../src/platform/NexTraceOne.ApiHost/NexTraceOne.ApiHost.csproj"
  api_project="$(realpath "$api_project" 2>/dev/null || echo "$api_project")"

  echo "→ A exportar via dotnet project: $api_project"

  if ! command -v dotnet &>/dev/null; then
    echo "ERRO: dotnet não encontrado. Instalar .NET 10 SDK." >&2
    exit 1
  fi

  if [[ ! -f "$api_project" ]]; then
    echo "ERRO: Projeto ApiHost não encontrado em $api_project" >&2
    exit 1
  fi

  # Usar dotnet build + launch openapi endpoint in background
  echo "→ A compilar ApiHost..."
  dotnet build "$api_project" -c Release --nologo -q 2>&1

  # Tentar via dotnet run com --urls e curl com retry
  local tmp_pid_file
  tmp_pid_file=$(mktemp)

  echo "→ A iniciar ApiHost em modo de exportação (porta 5099)..."
  ASPNETCORE_ENVIRONMENT=Development \
  ASPNETCORE_URLS="http://localhost:5099" \
  NEXTRACE_SKIP_MIGRATIONS=true \
    dotnet run --project "$api_project" -c Release --no-build --launch-profile "" \
    > /tmp/nextraceone-openapi-export.log 2>&1 &

  local api_pid=$!
  echo "$api_pid" > "$tmp_pid_file"

  # Aguardar a API ficar pronta (até 30 s)
  echo "→ A aguardar arranque da API..."
  local retries=0
  until curl -sf "http://localhost:5099/health" > /dev/null 2>&1; do
    retries=$((retries + 1))
    if [[ $retries -ge 30 ]]; then
      echo "ERRO: API não ficou pronta após 30 segundos." >&2
      kill "$api_pid" 2>/dev/null || true
      exit 1
    fi
    sleep 1
  done

  echo "→ API pronta. A exportar documento OpenAPI..."
  local http_code
  http_code=$(curl -sf -w "%{http_code}" -o "$OPENAPI_OUT" "http://localhost:5099/openapi/v1.json" 2>&1 || true)
  kill "$api_pid" 2>/dev/null || true

  if [[ "$http_code" == "200" ]]; then
    echo "✓ OpenAPI exportado com sucesso → $OPENAPI_OUT"
    echo "  Tamanho: $(wc -c < "$OPENAPI_OUT") bytes"
  else
    echo "ERRO: Falha na exportação (HTTP $http_code). Ver log: /tmp/nextraceone-openapi-export.log" >&2
    exit 1
  fi
}

# ── Executar ──────────────────────────────────────────────────────────────────
if [[ "$LIVE_MODE" == true ]]; then
  export_via_live_instance
else
  export_via_dotnet_tool
fi

echo ""
echo "═══════════════════════════════════════"
echo " Exportação concluída: $OPENAPI_OUT"
echo "═══════════════════════════════════════"

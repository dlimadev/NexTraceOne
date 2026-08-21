#!/usr/bin/env bash
# =============================================================================
# audit-baseline.sh
# NexTraceOne — Coletor de evidência do estado real
#
# Produz um retrato objetivo e reprodutível do repositório, cruzando os elos
# de cada fatia vertical (endpoint → handler → repositório → EF → migration →
# teste) e o estado dos gates executáveis.
#
# Uso:
#   bash scripts/quality/audit-baseline.sh              # inventário (rápido)
#   bash scripts/quality/audit-baseline.sh --with-gates # + build/test/lint
#
# Saída: Markdown em stdout. Redirecionar para ficheiro se necessário.
#
# Estratégia: docs/audit/ESTRATEGIA-AUDITORIA-ESTADO-REAL.md
# =============================================================================

set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT"

WITH_GATES=false
for arg in "$@"; do
  [[ "$arg" == "--with-gates" ]] && WITH_GATES=true
done

# Conta ocorrências de um padrão em ficheiros .cs sob um caminho (0 se ausente).
count_matches() {
  local ci=""
  if [[ "$1" == "-i" ]]; then ci="-i"; shift; fi
  local pattern="$1" path="$2"
  [[ -d "$path" ]] || { echo 0; return; }
  grep -rhoE $ci "$pattern" "$path" --include=*.cs 2>/dev/null | wc -l | tr -d ' '
}

# Conta ficheiros .cs que contêm um padrão (0 se ausente).
count_files() {
  local ci=""
  if [[ "$1" == "-i" ]]; then ci="-i"; shift; fi
  local pattern="$1" path="$2"
  [[ -d "$path" ]] || { echo 0; return; }
  grep -rlE $ci "$pattern" "$path" --include=*.cs 2>/dev/null | wc -l | tr -d ' '
}

echo "# NexTraceOne — Baseline de Auditoria"
echo
echo "> Gerado por \`scripts/quality/audit-baseline.sh\` em $(date -u +%Y-%m-%dT%H:%M:%SZ)"
echo "> Commit: \`$(git rev-parse --short HEAD)\` — branch \`$(git rev-parse --abbrev-ref HEAD)\`"
echo

# ── Dimensão do repositório ──────────────────────────────────────────────────
echo "## Dimensão"
echo
echo "| Métrica | Valor |"
echo "|---|---|"
echo "| Ficheiros C# (excl. bin/obj) | $(find src tools -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' 2>/dev/null | wc -l) |"
echo "| Ficheiros TS/TSX (excl. node_modules) | $(find src/frontend/src -name '*.ts' -o -name '*.tsx' 2>/dev/null | wc -l) |"
echo "| Projetos .csproj | $(find src tools tests -name '*.csproj' 2>/dev/null | wc -l) |"
echo "| Módulos (bounded contexts) | $(ls -d src/modules/*/ 2>/dev/null | wc -l) |"
echo

# ── Inventário por módulo ────────────────────────────────────────────────────
echo "## Inventário por módulo"
echo
echo "Cada coluna é um elo da fatia vertical. Colunas desequilibradas indicam"
echo "drift — não são, por si só, defeitos, mas marcam onde investigar."
echo
echo "| Módulo | Endpoints | Handlers | Repos (iface) | Repos (impl) | EF configs | Migrations | Null\* |"
echo "|---|---|---|---|---|---|---|---|"

for dir in src/modules/*/; do
  m=$(basename "$dir")
  api=$(ls -d "$dir"*.API 2>/dev/null | head -1)
  app=$(ls -d "$dir"*.Application 2>/dev/null | head -1)
  inf=$(ls -d "$dir"*.Infrastructure 2>/dev/null | head -1)

  endpoints=$(count_matches "\.Map(Get|Post|Put|Patch|Delete)\(" "${api:-/nonexistent}")
  handlers=$(count_files ": I(Command|Query)Handler<" "${app:-/nonexistent}")
  repo_iface=$(count_matches "interface I[A-Za-z]+Repository" "${app:-/nonexistent}")
  repo_impl=$(count_matches "class [A-Za-z]+Repository" "${inf:-/nonexistent}")
  ef_cfg=$(count_files "IEntityTypeConfiguration<" "$dir")
  migrations=$(find "$dir" -path '*Migrations*' -name '*.cs' \
      -not -name '*Designer*' -not -name '*ModelSnapshot*' 2>/dev/null | wc -l | tr -d ' ')
  nulls=$(count_matches "class Null[A-Za-z]+" "$dir")

  echo "| $m | $endpoints | $handlers | $repo_iface | $repo_impl | $ef_cfg | $migrations | $nulls |"
done
echo

# ── Padrão Null: Reader (legítimo) vs Repository (bug) ───────────────────────
echo "## Padrão Null — classificação"
echo
echo "Regra do projeto (CLAUDE.md §21): \`NullXxxReader\` é placeholder legítimo;"
echo "\`NullXxxRepository\` é bug — persistência precisa de implementação real."
echo
echo "| Categoria | Ocorrências | Veredito |"
echo "|---|---|---|"
echo "| \`Null*Reader\` | $(count_matches 'class Null[A-Za-z]+Reader' src) | legítimo (phase-gated) |"
echo "| \`Null*Provider\` / \`Null*Service\` | $(count_matches 'class Null[A-Za-z]+(Provider|Service)' src) | legítimo (provider externo opcional) |"
echo "| \`Null*Repository\` | $(count_matches 'class Null[A-Za-z]+Repository' src) | **investigar — candidato a bug** |"
echo
if grep -rhoE "class Null[A-Za-z]+Repository" src --include=*.cs 2>/dev/null | grep -q .; then
  echo "Ficheiros \`Null*Repository\`:"
  echo
  grep -rlE "class Null[A-Za-z]+Repository" src --include=*.cs 2>/dev/null \
    | sed 's|^|- `|;s|$|`|'
  echo
fi

# ── Marcadores de dados simulados ────────────────────────────────────────────
echo "## Marcadores de simulação"
echo
echo "| Marcador | Ficheiros | Ocorrências |"
echo "|---|---|---|"
echo "| \`IsSimulated\` (backend) | $(count_files 'IsSimulated' src) | $(count_matches 'IsSimulated' src) |"
echo "| \`simulatedNote\` (backend) | $(count_files -i 'simulatedNote' src) | $(count_matches -i 'simulatedNote' src) |"
sn_fe=$(grep -rli "simulatedNote" src/frontend/src --include=*.ts --include=*.tsx 2>/dev/null | wc -l | tr -d ' ')
stub_fe=$(find src/frontend/src/stubs -name '*.ts' 2>/dev/null | wc -l | tr -d ' ')
echo "| \`simulatedNote\` (frontend) | $sn_fe | — |"
echo "| Handlers de stub MSW (frontend) | $stub_fe | — |"
echo

# ── Cobertura de teste na solução ────────────────────────────────────────────
echo "## Projetos de teste fora da solução"
echo
echo "Testes fora de \`NexTraceOne.sln\` nunca correm em CI (\`dotnet test NexTraceOne.sln\`)."
echo
echo "| Projeto de teste | No .sln | [Fact]/[Theory] |"
echo "|---|---|---|"
orphan_total=0
while IFS= read -r csproj; do
  name=$(basename "$csproj" .csproj)
  in_sln=$(grep -c "$name.csproj" NexTraceOne.sln 2>/dev/null | tr -d ' ')
  in_sln=${in_sln:-0}
  n=$(grep -rhoE "\[Fact\]|\[Theory\]" "$(dirname "$csproj")" --include=*.cs 2>/dev/null | wc -l | tr -d ' ')
  if [[ "$in_sln" -eq 0 ]]; then
    echo "| $name | ❌ não | $n |"
    orphan_total=$((orphan_total + n))
  fi
done < <(find tests tools -name '*Tests.csproj' 2>/dev/null | sort)
echo
echo "**Testes invisíveis ao CI: $orphan_total**"
echo

# ── Gates executáveis ────────────────────────────────────────────────────────
echo "## Gates executáveis"
echo
if [[ "$WITH_GATES" == false ]]; then
  echo "_Não executados. Correr com \`--with-gates\` para incluir._"
  exit 0
fi

echo "| Gate | Comando | Resultado |"
echo "|---|---|---|"

run_gate() {
  local label="$1" cmd="$2"
  local out
  out=$(eval "$cmd" 2>&1)
  local code=$?
  if [[ $code -eq 0 ]]; then
    echo "| $label | \`$cmd\` | ✅ passou |"
  else
    echo "| $label | \`$cmd\` | ❌ falhou (exit $code) |"
  fi
}

run_gate "Anti-demo" "bash scripts/quality/check-no-demo-artifacts.sh"
run_gate "i18n" "node scripts/quality/check-i18n-coverage.sh"

if command -v dotnet >/dev/null 2>&1; then
  run_gate "Build backend" "dotnet build NexTraceOne.sln -v q --nologo"
  run_gate "Testes backend" "NEXTRACE_SKIP_INTEGRITY=true dotnet test NexTraceOne.sln --no-build --nologo --filter 'FullyQualifiedName!~E2E&FullyQualifiedName!~Selenium&FullyQualifiedName!~IntegrationTests'"
else
  echo "| Build backend | \`dotnet build\` | ⚠️ SDK .NET ausente |"
  echo "| Testes backend | \`dotnet test\` | ⚠️ SDK .NET ausente |"
fi

if [[ -d src/frontend/node_modules ]]; then
  run_gate "Typecheck frontend" "cd src/frontend && npx tsc -b --force"
  run_gate "Lint frontend" "cd src/frontend && npx eslint ."
  run_gate "Testes frontend" "cd src/frontend && npx vitest run"
else
  echo "| Frontend | — | ⚠️ node_modules ausente (correr \`npm install\`) |"
fi

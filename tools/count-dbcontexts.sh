#!/usr/bin/env bash
# Count DbContext classes across the solution (excluding DesignTime factories).
# Source of truth for documentation references to the number of DbContexts.
#
# Usage:
#   ./tools/count-dbcontexts.sh           # prints count + list
#   ./tools/count-dbcontexts.sh --count   # prints only the integer
#
# Avoids documentation drift (divergence D-02/D-05 in HONEST-GAPS.md).

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
SRC_DIR="${ROOT_DIR}/src"

# Find all files ending with DbContext.cs (not DesignTime factories, not in obj/bin).
mapfile -t CONTEXTS < <(find "${SRC_DIR}" -name "*DbContext.cs" \
  -not -name "*DesignTime*" \
  -not -path "*/obj/*" \
  -not -path "*/bin/*" \
  -type f \
  | sort)

COUNT="${#CONTEXTS[@]}"

if [[ "${1:-}" == "--count" ]]; then
  echo "${COUNT}"
  exit 0
fi

echo "DbContexts in solution: ${COUNT}"
echo ""
for f in "${CONTEXTS[@]}"; do
  # Extract class name from filename
  name="$(basename "${f}" .cs)"
  rel="${f#${ROOT_DIR}/}"
  echo "  - ${name}  (${rel})"
done

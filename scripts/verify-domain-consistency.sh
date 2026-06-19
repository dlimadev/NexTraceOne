#!/usr/bin/env bash
# Verifica consistência entre entidades de domínio, configurações EF, DbSets e migrations.
# Uso: bash scripts/verify-domain-consistency.sh [modulo]

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MODULE_FILTER="${1:-}"

echo "========================================="
echo "Verificação de consistência do domínio"
echo "ROOT: $ROOT_DIR"
echo "========================================="

find "$ROOT_DIR/src/modules" -maxdepth 2 -type d | while read -r module_dir; do
    module_name="$(basename "$module_dir")"
    
    if [[ -n "$MODULE_FILTER" && "$module_name" != "$MODULE_FILTER" ]]; then
        continue
    fi
    
    infra_dir="$module_dir/NexTraceOne.$(echo "$module_name" | sed 's/^./\u&/; s/-//g').Infrastructure"
    if [[ ! -d "$infra_dir" ]]; then
        continue
    fi
    
    domain_dir="$module_dir/NexTraceOne.$(echo "$module_name" | sed 's/^./\u&/; s/-//g').Domain"
    
    echo ""
    echo "--- Módulo: $module_name ---"
    
    # Entidades de domínio (classes que herdam Entity<> ou AggregateRoot<>)
    if [[ -d "$domain_dir" ]]; then
        entity_count=$(find "$domain_dir" -name "*.cs" -print0 | xargs -0 grep -lE "class.*:.*Entity<|class.*:.*AggregateRoot<|class.*:.*AuditableEntity<" 2>/dev/null | wc -l)
        echo "Entidades de domínio: $entity_count"
    else
        echo "Entidades de domínio: 0 (diretório não encontrado)"
    fi
    
    # Configurações EF
    config_dir="$infra_dir/Persistence/Configurations"
    if [[ -d "$config_dir" ]]; then
        config_count=$(find "$config_dir" -name "*Configuration.cs" | wc -l)
        echo "Configurações EF: $config_count"
    else
        echo "Configurações EF: 0"
    fi
    
    # DbContext e DbSets
    dbcontext_file=$(find "$infra_dir" -name "*DbContext.cs" | head -n 1)
    if [[ -n "$dbcontext_file" ]]; then
        dbset_count=$(grep -c "DbSet<" "$dbcontext_file" 2>/dev/null || echo 0)
        dbcontext_name=$(grep -oE "class[[:space:]]+[A-Za-z0-9_]+DbContext" "$dbcontext_file" | sed -E 's/class[[:space:]]+//' | head -n 1)
        echo "DbContext: $dbcontext_name"
        echo "DbSets: $dbset_count"
    else
        echo "DbContext: não encontrado"
    fi
    
    # Migrations
    migrations_dir="$infra_dir/Persistence/Migrations"
    if [[ -d "$migrations_dir" ]]; then
        migration_count=$(find "$migrations_dir" -maxdepth 1 -name "*.cs" ! -name "*ModelSnapshot*" | wc -l)
        echo "Migrations: $migration_count"
    else
        echo "Migrations: 0"
    fi
done

echo ""
echo "========================================="
echo "Verificação concluída."
echo "========================================="

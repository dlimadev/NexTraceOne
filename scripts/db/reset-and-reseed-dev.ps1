# ============================================================================
# Reset completo da base de dados + reseed para desenvolvimento
# ============================================================================
# Passo 1: Apaga TODAS as tabelas (todos os DbContexts)
# Passo 2: Compila e executa a aplicação (migrations + seeds automáticos)
# Passo 3: Aplica os scripts manuais de seed
# ============================================================================

$ErrorActionPreference = "Stop"

Write-Host "=== 1. Nuke database (apaga TODAS as tabelas) ===" -ForegroundColor Cyan
psql -U postgres -d nextraceone -f "$PSScriptRoot\nuke-database.sql"

Write-Host ""
Write-Host "=== 2. Build da aplicacao ===" -ForegroundColor Cyan
dotnet build src/platform/NexTraceOne.ApiHost/NexTraceOne.ApiHost.csproj

Write-Host ""
Write-Host "=== 3. Run (aplica migrations de todos os DbContexts + seeds automaticos) ===" -ForegroundColor Cyan
Write-Host "A app vai arrancar e aplicar automaticamente:"
Write-Host "  - Migrations dos 10 DbContexts"
Write-Host "  - Seed de autorizacao (role permissions + module access policies)"
Write-Host "  - Seed de desenvolvimento (ficheiros SeedData/*.sql)"
Write-Host ""
Write-Host "Pressiona CTRL+C quando vir 'Development seed data process completed.'"
Write-Host ""
dotnet run --project src/platform/NexTraceOne.ApiHost

Write-Host ""
Write-Host "=== 4. Seeds manuais (idempotentes — seguro re-executar) ===" -ForegroundColor Cyan
psql -U postgres -d nextraceone -f "db/seed/seed_production.sql"
psql -U postgres -d nextraceone -f "db/seed/seed_development.sql"

Write-Host ""
Write-Host "=== DONE ===" -ForegroundColor Green

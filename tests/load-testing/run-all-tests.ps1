# Script de automação para execução completa de load tests (Windows PowerShell)
# Uso: .\run-all-tests.ps1 [-TestType smoke|load|stress|spike|endurance|all]

param(
    [ValidateSet('smoke', 'load', 'stress', 'spike', 'endurance', 'all')]
    [string]$TestType = 'all'
)

$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ScenariosDir = Join-Path $ScriptDir "scenarios"
$ReportsDir = Join-Path $ScriptDir "reports"
$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"

# Verificar se k6 está instalado
try {
    $k6Version = & k6 version 2>&1
    Write-Host "✅ k6 encontrado: $k6Version" -ForegroundColor Green
} catch {
    Write-Host "❌ k6 não está instalado. Instale em: https://k6.io/docs/getting-started/installation/" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║         NexTraceOne Load Testing Framework               ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

function Run-Test {
    param(
        [string]$TestName,
        [string]$TestFile,
        [string]$Duration
    )

    Write-Host "▶ Executando: $TestName" -ForegroundColor Yellow
    Write-Host "   Duração estimada: $Duration"
    Write-Host "   Relatório: $ReportsDir\$($TestName)_$Timestamp.json"
    Write-Host ""

    # Criar diretório de relatórios se não existir
    if (-not (Test-Path $ReportsDir)) {
        New-Item -ItemType Directory -Force -Path $ReportsDir | Out-Null
    }

    # Executar teste com output JSON
    $reportFile = Join-Path $ReportsDir "$($TestName)_$Timestamp.json"

    try {
        & k6 run $TestFile --out "json=$reportFile"
        Write-Host "✅ $TestName concluído com sucesso!" -ForegroundColor Green
    } catch {
        Write-Host "❌ $TestName falhou! Verifique os logs acima." -ForegroundColor Red
        return $false
    }

    Write-Host ""
    Write-Host "─────────────────────────────────────────────────────────────"
    Write-Host ""
    return $true
}

switch ($TestType) {
    'smoke' {
        Run-Test "smoke-test" (Join-Path $ScenariosDir "smoke-test.js") "30 segundos"
    }
    'load' {
        Run-Test "load-test" (Join-Path $ScenariosDir "load-test.js") "9 minutos"
    }
    'stress' {
        Run-Test "stress-test" (Join-Path $ScenariosDir "stress-test.js") "24 minutos"
    }
    'spike' {
        Run-Test "spike-test" (Join-Path $ScenariosDir "spike-test.js") "8 minutos"
    }
    'endurance' {
        Run-Test "endurance-test" (Join-Path $ScenariosDir "endurance-test.js") "1 hora"
    }
    'all' {
        Write-Host "📋 Executando TODOS os testes em sequência..." -ForegroundColor Cyan
        Write-Host ""

        # Smoke test primeiro (validação rápida)
        $smokeResult = Run-Test "smoke-test" (Join-Path $ScenariosDir "smoke-test.js") "30 segundos"
        if (-not $smokeResult) {
            Write-Host "⚠️  Smoke test falhou. Abortando testes restantes." -ForegroundColor Red
            exit 1
        }

        # Load test
        Run-Test "load-test" (Join-Path $ScenariosDir "load-test.js") "9 minutos"

        # Stress test
        Run-Test "stress-test" (Join-Path $ScenariosDir "stress-test.js") "24 minutos"

        # Spike test
        Run-Test "spike-test" (Join-Path $ScenariosDir "spike-test.js") "8 minutos"

        Write-Host "⚠️  Endurance test (1h) não executado automaticamente." -ForegroundColor Yellow
        Write-Host "   Para executar: .\run-all-tests.ps1 -TestType endurance" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║                    Testes Concluídos!                     ║" -ForegroundColor Green
Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "Relatórios salvos em: $ReportsDir" -ForegroundColor Cyan
Write-Host ""
Write-Host "Para visualizar resultados:"
Write-Host "  k6 inspect $ReportsDir\*_$Timestamp.json"
Write-Host ""

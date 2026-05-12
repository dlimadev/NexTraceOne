# Script to replace [Fact] with [RequiresDockerFact] in integration tests
# Usage: .\scripts\add-requires-docker-attribute.ps1

$testFiles = Get-ChildItem -Path "tests/platform/NexTraceOne.IntegrationTests" -Filter "*Tests.cs" -Recurse

foreach ($file in $testFiles) {
    Write-Host "Processing: $($file.FullName)"
    
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    
    # Add using statement if not exists
    if ($content -notmatch "using NexTraceOne\.IntegrationTests\.Infrastructure;") {
        $content = $content -replace '(namespace\s+\S+;)', "`$1`nusing NexTraceOne.IntegrationTests.Infrastructure;"
    }
    
    # Replace [Fact, RequiresDocker] or [Fact][RequiresDocker] with [RequiresDockerFact]
    $content = $content -replace '\[Fact,\s*RequiresDocker\]', '[RequiresDockerFact]'
    $content = $content -replace '\[Fact\]\s*\[RequiresDocker\]', '[RequiresDockerFact]'
    $content = $content -replace '\[Fact\]', '[RequiresDockerFact]'
    
    # Remove standalone [RequiresDocker] lines
    $content = $content -replace '\s*\[RequiresDocker\]\s*', ''
    
    Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline
    
    Write-Host "  Updated: $($file.Name)"
}

Write-Host ""
Write-Host "Completed! All integration tests now use [RequiresDockerFact]."
Write-Host "Tests will be automatically skipped when Docker is not available."

$root = "C:\Users\dlima\Documents\NexTraceOne\NexTraceOne"

$files = @(
    "src\modules\aiknowledge\NexTraceOne.AIKnowledge.API\ExternalAI\Endpoints\Endpoints\ExternalAiEndpointModule.cs",
    "src\modules\aiknowledge\NexTraceOne.AIKnowledge.API\Orchestration\Endpoints\Endpoints\AiOrchestrationEndpointModule.cs",
    "src\modules\auditcompliance\NexTraceOne.AuditCompliance.API\Endpoints\Endpoints\AuditEndpointModule.cs",
    "src\modules\catalog\NexTraceOne.Catalog.API\Portal\Endpoints\Endpoints\DeveloperPortalEndpointModule.cs",
    "src\modules\catalog\NexTraceOne.Catalog.API\Portal\Endpoints\Endpoints\PublicationCenterEndpointModule.cs",
    "src\modules\changegovernance\NexTraceOne.ChangeGovernance.API\ChangeIntelligence\Endpoints\Endpoints\ChangeConfidenceEndpoints.cs",
    "src\modules\changegovernance\NexTraceOne.ChangeGovernance.API\ChangeIntelligence\Endpoints\Endpoints\ReleaseQueryEndpoints.cs",
    "src\modules\changegovernance\NexTraceOne.ChangeGovernance.API\Promotion\Endpoints\Endpoints\PromotionEndpointModule.cs",
    "src\modules\changegovernance\NexTraceOne.ChangeGovernance.API\RulesetGovernance\Endpoints\Endpoints\RulesetGovernanceEndpointModule.cs",
    "src\modules\changegovernance\NexTraceOne.ChangeGovernance.API\Workflow\Endpoints\Endpoints\StatusEndpoints.cs",
    "src\modules\identityaccess\NexTraceOne.IdentityAccess.API\Endpoints\Endpoints\UserEndpoints.cs",
    "src\modules\notifications\NexTraceOne.Notifications.API\Endpoints\NotificationCenterEndpointModule.cs",
    "src\modules\operationalintelligence\NexTraceOne.OperationalIntelligence.API\Cost\Endpoints\Endpoints\CostIntelligenceEndpointModule.cs",
    "src\modules\operationalintelligence\NexTraceOne.OperationalIntelligence.API\Reliability\Endpoints\Endpoints\ReliabilityEndpointModule.cs",
    "src\modules\operationalintelligence\NexTraceOne.OperationalIntelligence.API\Runtime\Endpoints\Endpoints\RuntimeIntelligenceEndpointModule.cs"
)

$totalFixed = 0

foreach ($rel in $files) {
    $path = Join-Path $root $rel
    $original = Get-Content $path -Raw -Encoding UTF8
    $updated = $original -replace '(?<![a-zA-Z_])int page(?!\s*=)(\s*,)', 'int page = 1$1'
    $updated = $updated -replace '(?<![a-zA-Z_])int pageSize(?!\s*=)(\s*,)', 'int pageSize = 20$1'
    if ($updated -ne $original) {
        [System.IO.File]::WriteAllText($path, $updated, [System.Text.Encoding]::UTF8)
        $pageCount   = ([regex]::Matches($original, '(?<![a-zA-Z_])int page(?!\s*=)\s*,')).Count
        $sizeCount   = ([regex]::Matches($original, '(?<![a-zA-Z_])int pageSize(?!\s*=)\s*,')).Count
        Write-Host "Fixed $($pageCount)x page + $($sizeCount)x pageSize  ->  $($rel.Split('\')[-1])"
        $totalFixed++
    }
}

Write-Host ""
Write-Host "Done. Files updated: $totalFixed"

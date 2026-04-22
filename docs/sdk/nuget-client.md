# NexTrace.Sdk — NuGet Client Usage

## Installation

```bash
dotnet add package NexTrace.Sdk
```

## Configuration

```csharp
var options = new NexTraceSdkOptions
{
    BaseUrl        = "https://nextraceone.example.com",
    ApiToken       = Environment.GetEnvironmentVariable("NEXONE_API_TOKEN")!,
    TimeoutSeconds = 30,
    RetryCount     = 2
};

var client = new NexTraceSdkClient(options);
```

## Service Catalog

```csharp
// Get a service by name
var svc = await client.Services.GetServiceAsync("payments", ct);

// List services by team
var teamServices = await client.Services.ListServicesAsync(team: "platform", ct);
```

## Contract Governance

```csharp
// Get contract
var contract = await client.Contracts.GetContractAsync("contract-id-here", ct);

// Semantic diff between two versions
var diff = await client.Contracts.DiffContractAsync("v1-id", "v2-id", ct);
if (diff?.HasBreakingChanges == true)
    Console.WriteLine("⚠ Breaking changes detected!");
```

## Change Intelligence

```csharp
// Confidence score for a release
var score = await client.Changes.GetConfidenceScoreAsync("v2.1.0-rc1", ct);
Console.WriteLine($"Score: {score?.Score} — Tier: {score?.Tier}");
Console.WriteLine($"Recommendation: {score?.Recommendation}");

// Change status by SHA
var status = await client.Changes.GetChangeStatusAsync("abc123def456", ct);
```

## Compliance

```csharp
var coverage = await client.Compliance.CheckCoverageAsync("GDPR", ct);
Console.WriteLine($"GDPR Coverage: {coverage?.CoveragePercent}%");
foreach (var gap in coverage?.Gaps ?? [])
    Console.WriteLine($"  Gap: {gap}");
```

## Disposing

`NexTraceSdkClient` implements `IDisposable`. Use `using` or inject as a singleton:

```csharp
using var client = new NexTraceSdkClient(options);
```

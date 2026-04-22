# NexTrace SDK — Overview

The **NexTrace.Sdk** is the official .NET client library for the NexTraceOne platform API.

## Quick Start

```csharp
using NexTrace.Sdk;

var client = new NexTraceSdkClient(new NexTraceSdkOptions
{
    BaseUrl  = "https://nextraceone.example.com",
    ApiToken = "YOUR_API_TOKEN"
});

// Query the Service Catalog
var service = await client.Services.GetServiceAsync("payments");

// Get change confidence score
var score = await client.Changes.GetConfidenceScoreAsync("v1.2.0-rc1");
Console.WriteLine($"Confidence: {score?.Score} ({score?.Tier})");
```

## Sub-clients

| Client | Purpose |
|--------|---------|
| `client.Services` | Service Catalog queries |
| `client.Contracts` | Contract Governance + diff |
| `client.Changes` | Change Intelligence + confidence |
| `client.Compliance` | Compliance coverage checks |

## CLI Tool

For CI/CD pipelines, use the **nex** CLI:

```bash
nex confidence score v1.2.0-rc1 --min-score 70
nex compliance check --standard GDPR
```

See [nuget-client.md](nuget-client.md) and [github-action.md](github-action.md) for details.

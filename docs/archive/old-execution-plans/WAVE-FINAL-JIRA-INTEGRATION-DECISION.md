# Wave Final — Jira Integration Decision

## Decision: Formal Deferral (PGLI)

The Jira work item synchronization capability (`SyncJiraWorkItems`) has been formally
deferred as a Post-Go-Live Item (PGLI).

## Rationale

1. **External dependency**: Jira integration requires OAuth/API token configuration,
   endpoint setup, and field mapping that is customer-specific
2. **Integration framework exists**: The Governance module already has `IntegrationConnector`
   entities and infrastructure for managing external integrations
3. **Not blocking core product**: The core change governance capabilities (release tracking,
   blast radius, change confidence, change score) function independently of Jira
4. **Honest error instead of placeholder**: The handler now returns an explicit
   `JIRA_INTEGRATION_DEFERRED` error instead of a misleading success with
   "not configured" message

## Previous State

```csharp
// Returned success with misleading message:
return new Response(release.Id.Value,
    "Jira sync not configured. Configure the Jira integration to enable this feature.");
```

## Current State

```csharp
// Returns explicit error — no ambiguity:
return Error.Validation("JIRA_INTEGRATION_DEFERRED",
    "Jira work item sync is formally deferred (PGLI). " +
    "Configure a Jira connector via Governance Integration Connectors to enable this capability.");
```

## Future Implementation Path

When Jira integration is prioritized:
1. Create a Jira `IntegrationConnector` via the Governance module
2. Implement `IJiraClient` in infrastructure with OAuth/API token support
3. Update `SyncJiraWorkItems.Handler` to use the connector
4. Add work item mapping configuration
5. Enable bidirectional sync with conflict resolution

## Key Principle

> "not configured" is not an acceptable state for an exposed capability.
> Either implement it or formally defer it with an explicit error.

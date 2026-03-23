# Service Catalog & Contracts

## Browsing the Service Catalog

Navigate to **Services** in the sidebar to access the full service catalog.

- **List View** — Browse all registered services with filters for type, criticality, lifecycle status, and team ownership.
- **Search** — Use the search bar to find services by name or identifier.
- **Filters** — Filter by service type (REST API, SOAP, Kafka Producer/Consumer, Background Service), criticality level, or lifecycle status.

## Viewing Service Details

Click on any service to see its detail page:

- **Overview** — Service metadata, ownership, criticality, and description
- **Contracts** — All contracts associated with this service
- **Dependencies** — Upstream and downstream service dependencies
- **Reliability** — SLO tracking, incident history, and operational health
- **Documentation** — Linked runbooks and operational notes

## Managing Contracts

### Contract Types

NexTraceOne supports contracts for:

- **REST APIs** (OpenAPI/Swagger)
- **SOAP Services** (WSDL)
- **Event Contracts** (AsyncAPI for Kafka)
- **Background Services**
- **Canonical Entities** (shared schemas/DTOs)

### Creating a Contract

1. Navigate to **Contracts** → **Contract Studio**.
2. Choose the contract type (REST, SOAP, Event, etc.).
3. Use the **Visual REST Builder** to define endpoints, parameters, request/response schemas, and security requirements without writing YAML manually.
4. Add metadata: tags, ownership, versioning information.
5. **Validate** the contract to check for errors.
6. **Save as Draft** to start the approval workflow.

### Contract Lifecycle

Contracts follow a governed lifecycle:

```
Draft → In Review → Approved → Locked → Deprecated → Retired
```

- **Draft** — Initial creation, editable by the team
- **In Review** — Submitted for approval
- **Approved** — Ready for use
- **Locked** — Immutable production version
- **Deprecated** — Marked for sunset with deprecation notes
- **Retired** — No longer active

### Source of Truth

The **Source of Truth** view provides a consolidated, authoritative view of any service or contract — including its full history, ownership chain, and current production state. Access it from the service detail page or the dedicated Source of Truth Explorer.

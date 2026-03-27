# Phase 7 — CLI Commands Reference

## Overview

The NexTraceOne CLI (`nex`) provides command-line access to platform operations. Phase 7 delivers two functional commands: `nex validate` and `nex catalog`.

## Installation

```bash
# Build from source
dotnet build tools/NexTraceOne.CLI/NexTraceOne.CLI.csproj -c Release

# The output binary is named 'nex'
# Located at: tools/NexTraceOne.CLI/bin/Release/net10.0/nex
```

## Commands

### `nex validate`

Validates a contract manifest JSON file against NexTraceOne rules.

**Usage:**
```
nex validate <file> [--format text|json] [--strict]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `<file>` | Path to the contract manifest JSON file to validate |

**Options:**
| Option | Description | Default |
|--------|-------------|---------|
| `--format` | Output format: `text` (table) or `json` | `text` |
| `--strict` | Treat warnings as errors (exit code 1) | `false` |

**Validation Rules:**

| Rule ID | Severity | Description |
|---------|----------|-------------|
| CLI001 | Error | `name` field is required |
| CLI002 | Error | `version` field is required |
| CLI003 | Error | `type` field is required |
| CLI004 | Error | `type` must be one of: `rest-api`, `soap`, `event-contract`, `background-service` |
| CLI005 | Error | `version` must follow semantic versioning (e.g., `1.0.0`) |
| CLI006 | Warning | `description` is recommended |
| CLI007 | Error | Each endpoint must have `method` field |
| CLI008 | Error | Each endpoint must have `path` field |
| CLI009 | Warning | `owner` is recommended |
| CLI010 | Warning | `team` is recommended |
| CLI011 | Warning | Schema should have `format` field |

**Contract Manifest Schema:**
```json
{
  "name": "payment-service",
  "version": "1.2.0",
  "type": "rest-api",
  "description": "Payment processing service",
  "owner": "payments-team",
  "team": "platform",
  "endpoints": [
    { "path": "/api/payments", "method": "POST" },
    { "path": "/api/payments/{id}", "method": "GET" }
  ],
  "schema": {
    "format": "openapi-3.1"
  }
}
```

**Exit Codes:**
| Code | Meaning |
|------|---------|
| 0 | Validation passed (no errors; no warnings in strict mode) |
| 1 | Validation failed (errors found, or warnings in strict mode) |
| 2 | File not found or JSON parse error |

**Examples:**
```bash
# Validate a contract manifest
nex validate my-service.json

# Validate with JSON output
nex validate my-service.json --format json

# Strict mode (warnings become errors)
nex validate my-service.json --strict
```

---

### `nex catalog`

Query the NexTraceOne service catalog.

**Subcommands:**

#### `nex catalog list`

List all services in the catalog.

```
nex catalog list [--url <api-url>] [--format text|json]
```

| Option | Description | Default |
|--------|-------------|---------|
| `--url` | NexTraceOne API base URL | `NEX_API_URL` env var or `http://localhost:8080` |
| `--format` | Output format: `text` (table) or `json` | `text` |

#### `nex catalog get`

Get details of a specific service.

```
nex catalog get <id> [--url <api-url>] [--format text|json]
```

| Argument | Description |
|----------|-------------|
| `<id>` | Service ID (GUID) |

| Option | Description | Default |
|--------|-------------|---------|
| `--url` | NexTraceOne API base URL | `NEX_API_URL` env var or `http://localhost:8080` |
| `--format` | Output format: `text` (table) or `json` | `text` |

**Exit Codes:**
| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | API error, connection failure, or service not found |

**Examples:**
```bash
# List all services (default URL)
nex catalog list

# List services from specific API
nex catalog list --url https://api.nextraceone.local

# Get service details
nex catalog get 550e8400-e29b-41d4-a716-446655440000

# JSON output
nex catalog list --format json

# Set API URL via environment variable
export NEX_API_URL=https://api.nextraceone.local
nex catalog list
```

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `NEX_API_URL` | Default API base URL for catalog commands | `http://localhost:8080` |

## Tests

CLI tests are located at `tests/platform/NexTraceOne.CLI.Tests/`:

```bash
# Run CLI tests
dotnet test tests/platform/NexTraceOne.CLI.Tests/

# Expected: 44 tests passing
```

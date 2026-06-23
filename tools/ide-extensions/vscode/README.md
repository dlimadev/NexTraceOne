# NexTraceOne Copilot — VS Code Extension

AI-assisted operations and contract governance directly in your IDE, powered by NexTraceOne.

## Features

- **AI Chat Panel** (sidebar): Copilot-like conversation with history, code-block rendering and "Insert at Cursor"/Copy actions.
- **Chat Participant `@nextraceone`** (VS Code ≥ 1.90): slash commands `/service`, `/change`, `/contract`, `/incident`, `/report`, `/blast-radius`, `/scaffold`, `/generate`, `/migrate`.
- **Language Model Tools** (VS Code ≥ 1.113): `get_service`, `get_contract`, `blast_radius`, `get_incident` — usable by Copilot agents.
- **Service Catalog Tree View**: browse services, drill into ownership/domain/type, open in dashboard or ask the AI about a service.
- **Scaffold New Service**: multi-step wizard from a governed template → writes files → optional catalog registration.
- **Ask AI / Ask about selection**: context-aware answers about services, contracts, changes and incidents.
- **Contract Migration Patch**: generate provider/consumer code hints for a contract change.
- **Configure MCP Server**: write `mcp.json` (global or workspace) pointing at the NexTraceOne MCP endpoint.
- **Open Dashboard** and a **status bar** indicator for the current service/environment.

## Installation

1. Package and install from source:
   ```bash
   cd tools/ide-extensions/vscode
   npm install
   npm run build
   # Install via VS Code: Extensions → ··· → Install from VSIX (after vsce package)
   ```

2. Or install directly in development mode:
   ```bash
   # Open this folder in VS Code and press F5 to launch the extension in a new window
   ```

## Configuration

| Setting | Default | Description |
|---|---|---|
| `nextraceone.serverUrl` | `http://localhost:5000` | URL of your NexTraceOne server |
| `nextraceone.apiKey` | _(empty)_ | NexTraceOne IDE API Key (generated in Platform Admin → IDE Extensions) |

## Usage

1. Configure your `nextraceone.serverUrl` and `nextraceone.apiKey` in VS Code settings.
2. Select text or a code snippet in the editor.
3. Open the Command Palette (`Ctrl+Shift+P` / `Cmd+Shift+P`) and run **NexTraceOne: Ask AI**.
4. The AI response appears in the NexTraceOne output channel and a notification.

## Requirements

- VS Code `^1.90.0` (chat participant); Language Model Tools require `≥ 1.113`
- A running NexTraceOne server with an IDE API key

## Notes

- All queries are routed via the NexTraceOne AI governance layer — audited, policy-controlled.
- No data is sent to external AI providers unless your NexTraceOne administrator has configured external models.

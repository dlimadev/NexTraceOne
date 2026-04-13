# NexTraceOne Copilot — VS Code Extension

AI-assisted operations and contract governance directly in your IDE, powered by NexTraceOne.

## Features

- **Ask AI** (`NexTraceOne: Ask AI`): Select code or text and ask the NexTraceOne AI Assistant for context-aware answers about services, contracts, changes and incidents.
- **Configure** (`NexTraceOne: Configure`): Open extension settings to configure the server URL and API key.

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

- VS Code `^1.85.0`
- A running NexTraceOne server with an IDE API key

## Notes

- All queries are routed via the NexTraceOne AI governance layer — audited, policy-controlled.
- No data is sent to external AI providers unless your NexTraceOne administrator has configured external models.

# NexTraceOne Visual Studio Extension

VSIX extension for Visual Studio 2022 that brings NexTraceOne AI Copilot capabilities directly into the IDE.

## Features

- **NexTraceOne AI Chat** — Tool Window with chat interface for AI-assisted operations and engineering
- **Ask AI about selection** — Right-click in the code editor to ask NexTraceOne AI about selected code
- **Options page** — Configure server URL, API key and persona via Tools → Options → NexTraceOne

## Requirements

- Visual Studio 2022 (17.x)
- .NET Framework 4.8
- VSIX SDK (installed with the "Visual Studio extension development" workload)

## Getting Started

### Build

1. Install the **Visual Studio extension development** workload in the VS Installer
2. Open `NexTraceOne.VisualStudio.csproj` in Visual Studio
3. Build the project — it will produce a `.vsix` file

### Configure

1. Install the `.vsix` file in Visual Studio
2. Open **Tools → Options → NexTraceOne → General**
3. Set **Server URL** and **API Key**
4. Open **View → Other Windows → NexTraceOne AI Chat** to start chatting

## Commands

| Command | Menu location | Description |
|---|---|---|
| `NexAiChatWindowCommand` | View → Other Windows → NexTraceOne AI Chat | Opens the AI Chat tool window |
| `NexAiAskAboutSelectionCommand` | Right-click in editor → Ask NexTraceOne AI | Sends selected code to AI for analysis |

## Architecture

```
NexTraceOne.VisualStudio/
├── NexTraceOnePackage.cs          — AsyncPackage root, registers all services
├── Commands/
│   ├── NexAiChatWindowCommand.cs  — Opens the AI chat tool window
│   └── NexAiAskAboutSelectionCommand.cs — Context menu command
├── Options/
│   └── NexTraceOneOptionsPage.cs  — Tool → Options dialog page
└── ToolWindows/
    ├── NexAiChatWindow.cs         — ToolWindowPane container
    └── NexAiChatControl.cs        — WPF UserControl with chat UI
```

## Backend

The extension communicates with `POST /api/v1/ai/ide/query` on the NexTraceOne server.
See the `AiIdeEndpointModule` in the backend for the contract.

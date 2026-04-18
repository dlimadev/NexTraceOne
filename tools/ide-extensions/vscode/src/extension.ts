import * as vscode from 'vscode';
import * as https from 'https';
import * as http from 'http';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { URL } from 'url';

let outputChannel: vscode.OutputChannel;

// ═══════════════════════════════════════════════════════════════════════════════
// Activation
// ═══════════════════════════════════════════════════════════════════════════════

export function activate(context: vscode.ExtensionContext): void {
  outputChannel = vscode.window.createOutputChannel('NexTraceOne');
  outputChannel.appendLine('NexTraceOne extension activated.');

  // Register sidebar Chat Panel
  const chatViewProvider = new NexChatViewProvider(context);
  context.subscriptions.push(
    vscode.window.registerWebviewViewProvider('nextraceone.chatView', chatViewProvider),
  );

  // Register Chat Participant (@nextraceone)
  registerChatParticipant(context);

  // Register commands
  context.subscriptions.push(
    vscode.commands.registerCommand('nextraceone.chat', () => handleChatCommand(context)),
    vscode.commands.registerCommand('nextraceone.configure', handleConfigureCommand),
    vscode.commands.registerCommand('nextraceone.inspectService', handleInspectServiceCommand),
    vscode.commands.registerCommand('nextraceone.askAboutSelection', handleAskAboutSelectionCommand),
    vscode.commands.registerCommand('nextraceone.mcpConfigure', handleMcpConfigureCommand),
    vscode.commands.registerCommand('nextraceone.openDashboard', handleOpenDashboardCommand),
    vscode.commands.registerCommand('nextraceone.openChatPanel', () =>
      vscode.commands.executeCommand('nextraceone.chatView.focus')),
  );
}

export function deactivate(): void {
  outputChannel?.dispose();
}

// ═══════════════════════════════════════════════════════════════════════════════
// Chat Panel — WebviewViewProvider (Sidebar)
// ═══════════════════════════════════════════════════════════════════════════════

interface ChatMessage {
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: string;
}

/**
 * NexChatViewProvider implementa um painel de chat Copilot-like na barra lateral do VS Code.
 * Mantém histórico de conversa por sessão e comunica com /api/v1/ai/ide/query.
 */
class NexChatViewProvider implements vscode.WebviewViewProvider {
  private _view?: vscode.WebviewView;
  private _history: ChatMessage[] = [];

  constructor(private readonly _context: vscode.ExtensionContext) {}

  resolveWebviewView(
    webviewView: vscode.WebviewView,
    _context: vscode.WebviewViewResolveContext,
    _token: vscode.CancellationToken,
  ): void {
    this._view = webviewView;

    webviewView.webview.options = {
      enableScripts: true,
      localResourceRoots: [this._context.extensionUri],
    };

    webviewView.webview.html = this._getHtmlContent(webviewView.webview);

    // Handle messages from the webview
    webviewView.webview.onDidReceiveMessage(async (message: { type: string; text?: string }) => {
      switch (message.type) {
        case 'sendMessage':
          if (message.text) {
            await this._handleUserMessage(message.text);
          }
          break;
        case 'clearHistory':
          this._history = [];
          this._postMessage({ type: 'clearHistory' });
          break;
        case 'ready':
          // Restore history on panel re-open
          if (this._history.length > 0) {
            this._postMessage({ type: 'restoreHistory', messages: this._history });
          }
          break;
      }
    });
  }

  /**
   * Envia uma mensagem ao painel de chat a partir do código da extensão
   * (usado por comandos como inspectService ou askAboutSelection).
   */
  async sendQuery(text: string): Promise<void> {
    if (this._view) {
      this._view.show(true);
    }
    await this._handleUserMessage(text);
  }

  private async _handleUserMessage(text: string): Promise<void> {
    const userMsg: ChatMessage = {
      role: 'user',
      content: text,
      timestamp: new Date().toISOString(),
    };
    this._history.push(userMsg);
    this._postMessage({ type: 'addMessage', message: userMsg });

    const config = getConfig();

    if (!config.apiKey) {
      const errMsg: ChatMessage = {
        role: 'system',
        content: '⚠️ API key not configured. Use **NexTraceOne: Configure** to set it.',
        timestamp: new Date().toISOString(),
      };
      this._history.push(errMsg);
      this._postMessage({ type: 'addMessage', message: errMsg });
      return;
    }

    this._postMessage({ type: 'setLoading', loading: true });

    try {
      const editor = vscode.window.activeTextEditor;
      const context = editor?.document.getText(editor.selection) ?? undefined;

      const response = await callIdeQueryApi(config.serverUrl, config.apiKey, {
        queryText: text,
        clientType: 'vscode',
        clientVersion: '0.2.0',
        queryType: 'GeneralQuery',
        context,
        persona: config.persona,
      });

      const assistantMsg: ChatMessage = {
        role: 'assistant',
        content: response,
        timestamp: new Date().toISOString(),
      };
      this._history.push(assistantMsg);
      this._postMessage({ type: 'addMessage', message: assistantMsg });
    } catch (err: unknown) {
      const errText = err instanceof Error ? err.message : String(err);
      const errMsg: ChatMessage = {
        role: 'system',
        content: `❌ Error: ${errText}`,
        timestamp: new Date().toISOString(),
      };
      this._history.push(errMsg);
      this._postMessage({ type: 'addMessage', message: errMsg });
    } finally {
      this._postMessage({ type: 'setLoading', loading: false });
    }
  }

  private _postMessage(message: unknown): void {
    if (this._view) {
      void this._view.webview.postMessage(message);
    }
  }

  private _getHtmlContent(_webview: vscode.Webview): string {
    return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>NexTraceOne AI Chat</title>
  <style>
    * { box-sizing: border-box; margin: 0; padding: 0; }
    body {
      font-family: var(--vscode-font-family);
      font-size: var(--vscode-font-size);
      color: var(--vscode-foreground);
      background: var(--vscode-sideBar-background);
      display: flex; flex-direction: column; height: 100vh;
      overflow: hidden;
    }
    #header {
      padding: 8px 12px;
      border-bottom: 1px solid var(--vscode-panel-border);
      display: flex; align-items: center; justify-content: space-between;
    }
    #header-title { font-weight: 600; font-size: 12px; opacity: 0.8; }
    #clear-btn {
      background: none; border: none; cursor: pointer;
      color: var(--vscode-descriptionForeground);
      font-size: 11px; padding: 2px 6px;
    }
    #clear-btn:hover { color: var(--vscode-foreground); }
    #messages {
      flex: 1; overflow-y: auto; padding: 12px;
      display: flex; flex-direction: column; gap: 10px;
    }
    .message { max-width: 100%; word-wrap: break-word; }
    .message.user {
      align-self: flex-end;
      background: var(--vscode-button-background);
      color: var(--vscode-button-foreground);
      border-radius: 12px 12px 2px 12px;
      padding: 8px 12px; max-width: 85%;
      font-size: 12px;
    }
    .message.assistant {
      align-self: flex-start;
      background: var(--vscode-editor-background);
      border: 1px solid var(--vscode-panel-border);
      border-radius: 2px 12px 12px 12px;
      padding: 8px 12px; max-width: 92%;
      font-size: 12px; line-height: 1.5;
      white-space: pre-wrap;
      position: relative;
    }
    .copy-btn {
      position: absolute; top: 4px; right: 4px;
      background: var(--vscode-button-secondaryBackground, rgba(128,128,128,0.15));
      color: var(--vscode-button-secondaryForeground, var(--vscode-foreground));
      border: none; border-radius: 3px;
      padding: 1px 6px; font-size: 10px; cursor: pointer;
      opacity: 0; transition: opacity 0.15s;
    }
    .message.assistant:hover .copy-btn { opacity: 1; }
    .copy-btn:hover { background: var(--vscode-button-secondaryHoverBackground, rgba(128,128,128,0.3)); }
    .message.system {
      align-self: center; text-align: center;
      color: var(--vscode-descriptionForeground);
      font-size: 11px; font-style: italic;
      padding: 4px 8px;
    }
    .message-time {
      font-size: 10px; opacity: 0.5; margin-top: 3px;
    }
    .user .message-time { text-align: right; }
    #loading { display: none; align-self: flex-start; padding: 4px 12px; }
    #loading.visible { display: flex; gap: 4px; align-items: center; }
    .dot {
      width: 6px; height: 6px; border-radius: 50%;
      background: var(--vscode-button-background);
      animation: bounce 1.2s infinite ease-in-out;
    }
    .dot:nth-child(2) { animation-delay: 0.2s; }
    .dot:nth-child(3) { animation-delay: 0.4s; }
    @keyframes bounce { 0%, 80%, 100% { transform: scale(0.6); } 40% { transform: scale(1); } }
    #input-area {
      display: flex; gap: 6px; padding: 8px 12px;
      border-top: 1px solid var(--vscode-panel-border);
      background: var(--vscode-sideBar-background);
    }
    #input {
      flex: 1; resize: none;
      background: var(--vscode-input-background);
      color: var(--vscode-input-foreground);
      border: 1px solid var(--vscode-input-border, transparent);
      border-radius: 4px; padding: 6px 8px; font-size: 12px;
      font-family: inherit; min-height: 32px; max-height: 120px;
    }
    #input:focus { outline: 1px solid var(--vscode-focusBorder); }
    #send-btn {
      background: var(--vscode-button-background);
      color: var(--vscode-button-foreground);
      border: none; border-radius: 4px;
      padding: 6px 12px; cursor: pointer; font-size: 12px;
      align-self: flex-end; white-space: nowrap;
    }
    #send-btn:hover { background: var(--vscode-button-hoverBackground); }
    #send-btn:disabled { opacity: 0.5; cursor: default; }
    .slash-hint {
      font-size: 10px; color: var(--vscode-descriptionForeground);
      padding: 0 12px 4px; font-style: italic;
    }
  </style>
</head>
<body>
  <div id="header">
    <span id="header-title">🚀 NexTraceOne AI</span>
    <button id="clear-btn" title="Clear conversation">Clear</button>
  </div>
  <div id="messages">
    <div class="message system">
      Ask about services, contracts, changes, incidents.<br>
      Try: <em>/service payments-api</em> or <em>/change list --env production</em>
    </div>
  </div>
  <div id="loading">
    <div class="dot"></div>
    <div class="dot"></div>
    <div class="dot"></div>
  </div>
  <div class="slash-hint">Tip: use /service, /change, /contract, /incident, /report, /blast-radius</div>
  <div id="input-area">
    <textarea id="input" rows="1" placeholder="Ask NexTraceOne AI…"></textarea>
    <button id="send-btn">Send</button>
  </div>

  <script>
    const vscode = acquireVsCodeApi();
    const messagesEl = document.getElementById('messages');
    const inputEl = document.getElementById('input');
    const sendBtn = document.getElementById('send-btn');
    const loadingEl = document.getElementById('loading');
    const clearBtn = document.getElementById('clear-btn');

    function appendMessage(msg) {
      const div = document.createElement('div');
      div.className = 'message ' + msg.role;

      const contentSpan = document.createElement('span');
      contentSpan.textContent = msg.content;
      div.appendChild(contentSpan);

      // Copy button for assistant messages
      if (msg.role === 'assistant') {
        const copyBtn = document.createElement('button');
        copyBtn.className = 'copy-btn';
        copyBtn.title = 'Copy to clipboard';
        copyBtn.textContent = 'Copy';
        copyBtn.addEventListener('click', () => {
          navigator.clipboard.writeText(msg.content).then(() => {
            copyBtn.textContent = '✓ Copied';
            setTimeout(() => { copyBtn.textContent = 'Copy'; }, 1500);
          }).catch(() => {
            copyBtn.textContent = 'Copy';
          });
        });
        div.appendChild(copyBtn);
      }

      const timeEl = document.createElement('div');
      timeEl.className = 'message-time';
      timeEl.textContent = new Date(msg.timestamp).toLocaleTimeString();
      div.appendChild(timeEl);
      messagesEl.appendChild(div);
      messagesEl.scrollTop = messagesEl.scrollHeight;
    }

    function sendMessage() {
      const text = inputEl.value.trim();
      if (!text) return;
      inputEl.value = '';
      inputEl.style.height = 'auto';
      vscode.postMessage({ type: 'sendMessage', text });
    }

    sendBtn.addEventListener('click', sendMessage);

    inputEl.addEventListener('keydown', (e) => {
      if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        sendMessage();
      }
    });

    inputEl.addEventListener('input', () => {
      inputEl.style.height = 'auto';
      inputEl.style.height = Math.min(inputEl.scrollHeight, 120) + 'px';
    });

    clearBtn.addEventListener('click', () => {
      vscode.postMessage({ type: 'clearHistory' });
    });

    window.addEventListener('message', (event) => {
      const msg = event.data;
      switch (msg.type) {
        case 'addMessage':
          appendMessage(msg.message);
          break;
        case 'setLoading':
          loadingEl.classList.toggle('visible', msg.loading);
          sendBtn.disabled = msg.loading;
          break;
        case 'clearHistory':
          messagesEl.innerHTML = '<div class="message system">Conversation cleared.</div>';
          break;
        case 'restoreHistory':
          msg.messages.forEach(appendMessage);
          break;
      }
    });

    // Signal ready to restore history
    vscode.postMessage({ type: 'ready' });
  </script>
</body>
</html>`;
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Chat Participants API — @nextraceone
// ═══════════════════════════════════════════════════════════════════════════════

function registerChatParticipant(context: vscode.ExtensionContext): void {
  // The Chat Participants API is available in VS Code ≥ 1.90
  // We guard with a check so the extension still loads on older versions
  if (!('chat' in vscode)) {
    outputChannel.appendLine('Chat Participants API not available in this VS Code version (requires ≥ 1.90).');
    return;
  }

  try {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const chatApi = (vscode as any).chat as {
      createChatParticipant: (
        id: string,
        handler: (
          request: { prompt: string; command?: string },
          _context: unknown,
          stream: { markdown: (text: string) => void; progress: (text: string) => void },
          token: vscode.CancellationToken,
        ) => Promise<void>,
      ) => { iconPath: unknown; followupProvider: unknown; dispose: () => void };
    };

    const participant = chatApi.createChatParticipant(
      'nextraceone',
      async (request, _ctx, stream, token) => {
        const config = getConfig();

        if (!config.apiKey) {
          stream.markdown(
            '⚠️ **NexTraceOne API key not configured.**\n\n' +
            'Run `NexTraceOne: Configure` from the command palette to set your API key.',
          );
          return;
        }

        // Map slash commands to query types
        let queryType = 'GeneralQuery';
        let prefix = '';
        switch (request.command) {
          case 'service':
            queryType = 'OwnershipLookup';
            prefix = `[Service lookup] `;
            break;
          case 'change':
            queryType = 'BreakingChangeAlert';
            prefix = `[Change context] `;
            break;
          case 'contract':
            queryType = 'ContractSuggestion';
            prefix = `[Contract query] `;
            break;
          case 'incident':
            queryType = 'GeneralQuery';
            prefix = `[Incident context] `;
            break;
          case 'report':
            queryType = 'GeneralQuery';
            prefix = `[DORA/report query] `;
            break;
          case 'blast-radius':
            queryType = 'BreakingChangeAlert';
            prefix = `[Blast radius analysis] `;
            break;
        }

        const queryText = prefix + request.prompt;

        stream.progress('Querying NexTraceOne AI…');

        if (token.isCancellationRequested) return;

        try {
          const response = await callIdeQueryApi(config.serverUrl, config.apiKey, {
            queryText,
            clientType: 'vscode-chat',
            clientVersion: '0.2.0',
            queryType,
            persona: config.persona,
          });

          stream.markdown(response);
        } catch (err: unknown) {
          const msg = err instanceof Error ? err.message : String(err);
          stream.markdown(`❌ **Error contacting NexTraceOne:** ${msg}`);
        }
      },
    );

    context.subscriptions.push({ dispose: () => participant.dispose() });
    outputChannel.appendLine('Chat participant @nextraceone registered.');
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err);
    outputChannel.appendLine(`Could not register chat participant: ${msg}`);
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Commands
// ═══════════════════════════════════════════════════════════════════════════════

/** Handles the "NexTraceOne: Ask AI" command — quick-input fallback. */
async function handleChatCommand(_context: vscode.ExtensionContext): Promise<void> {
  const config = getConfig();

  if (!config.apiKey) {
    const action = await vscode.window.showWarningMessage(
      'NexTraceOne: No API key configured.',
      'Configure',
    );
    if (action === 'Configure') {
      await handleConfigureCommand();
    }
    return;
  }

  const editor = vscode.window.activeTextEditor;
  const selectedText = editor?.document.getText(editor.selection) ?? '';

  const query = selectedText.trim()
    ? selectedText
    : await vscode.window.showInputBox({
        prompt: 'Ask NexTraceOne AI',
        placeHolder: 'e.g. What changed in the payment-service recently?',
      });

  if (!query) return;

  outputChannel.show(true);
  outputChannel.appendLine(`\n[Query]: ${query}`);

  try {
    const response = await callIdeQueryApi(config.serverUrl, config.apiKey, {
      queryText: query,
      clientType: 'vscode',
      clientVersion: '0.2.0',
      queryType: 'GeneralQuery',
      persona: config.persona,
    });
    outputChannel.appendLine(`[Response]: ${response}`);
    await vscode.window.showInformationMessage(
      `NexTraceOne AI: ${response.slice(0, 200)}${response.length > 200 ? '...' : ''}`,
    );
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : String(err);
    outputChannel.appendLine(`[Error]: ${message}`);
    await vscode.window.showErrorMessage(`NexTraceOne AI Error: ${message}`);
  }
}

/** Handles the "NexTraceOne: Configure" command. */
async function handleConfigureCommand(): Promise<void> {
  await vscode.commands.executeCommand('workbench.action.openSettings', 'nextraceone');
}

/** Handles right-click → "NexTraceOne: Inspect Service Context". */
async function handleInspectServiceCommand(): Promise<void> {
  const config = getConfig();

  const editor = vscode.window.activeTextEditor;
  const fileName = editor?.document.fileName ?? '';
  const workspaceRoot = vscode.workspace.workspaceFolders?.[0]?.uri.fsPath ?? '';

  // Infer service name from workspace folder or file path
  const inferredService = path.basename(workspaceRoot) || path.basename(path.dirname(fileName));
  const service = await vscode.window.showInputBox({
    prompt: 'Service name in NexTraceOne',
    value: inferredService,
    placeHolder: 'e.g. payments-service',
  });

  if (!service || !config.apiKey) return;

  outputChannel.show(true);
  outputChannel.appendLine(`\n[Inspect Service]: ${service}`);

  try {
    const response = await callIdeQueryApi(config.serverUrl, config.apiKey, {
      queryText: `Show service context, recent changes, contracts and reliability for: ${service}`,
      clientType: 'vscode',
      clientVersion: '0.2.0',
      queryType: 'OwnershipLookup',
      serviceContext: service,
      persona: config.persona,
    });
    outputChannel.appendLine(`[Service Context]: ${response}`);

    const panel = vscode.window.createWebviewPanel(
      'nextraceoneServiceContext',
      `NexTraceOne: ${service}`,
      vscode.ViewColumn.Beside,
      { enableScripts: false },
    );
    panel.webview.html = `<html><body style="font-family:sans-serif;padding:16px;white-space:pre-wrap">${escapeHtml(response)}</body></html>`;
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : String(err);
    await vscode.window.showErrorMessage(`NexTraceOne: ${message}`);
  }
}

/** Handles right-click on selection → "NexTraceOne: Ask AI about this". */
async function handleAskAboutSelectionCommand(): Promise<void> {
  const config = getConfig();
  const editor = vscode.window.activeTextEditor;

  if (!editor) {
    await vscode.window.showWarningMessage('No active editor.');
    return;
  }

  const selectedText = editor.document.getText(editor.selection);
  if (!selectedText.trim()) {
    await vscode.window.showWarningMessage('No text selected.');
    return;
  }

  if (!config.apiKey) {
    await vscode.window.showWarningMessage(
      'NexTraceOne: No API key configured. Run NexTraceOne: Configure.',
    );
    return;
  }

  outputChannel.show(true);
  outputChannel.appendLine(`\n[Ask about selection]: ${selectedText.slice(0, 100)}...`);

  try {
    const response = await callIdeQueryApi(config.serverUrl, config.apiKey, {
      queryText: `Analyse and explain this code in the context of NexTraceOne services and contracts:\n\n${selectedText}`,
      clientType: 'vscode',
      clientVersion: '0.2.0',
      queryType: 'CodeGeneration',
      context: selectedText,
      persona: config.persona,
    });
    outputChannel.appendLine(`[Response]: ${response}`);

    const panel = vscode.window.createWebviewPanel(
      'nextraceoneAskSelection',
      'NexTraceOne AI Analysis',
      vscode.ViewColumn.Beside,
      { enableScripts: false },
    );
    panel.webview.html = `<html><body style="font-family:sans-serif;padding:16px;white-space:pre-wrap">${escapeHtml(response)}</body></html>`;
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : String(err);
    await vscode.window.showErrorMessage(`NexTraceOne AI Error: ${message}`);
  }
}

/** Handles "NexTraceOne: Configure MCP Server" command. */
async function handleMcpConfigureCommand(): Promise<void> {
  const config = getConfig();
  const mcpServerUrl = config.serverUrl.replace(/\/$/, '') + '/api/v1/ai/mcp';

  // Ask user whether to configure globally (~/.vscode/mcp.json) or at workspace level (.vscode/mcp.json)
  const scope = await vscode.window.showQuickPick(
    [
      { label: '$(home) Global', description: 'Write to ~/.vscode/mcp.json (all workspaces)', value: 'global' },
      { label: '$(folder) Workspace', description: 'Write to .vscode/mcp.json in current workspace', value: 'workspace' },
    ],
    { placeHolder: 'Choose MCP configuration scope', title: 'NexTraceOne MCP Configuration' },
  );

  if (!scope) return;

  const mcpConfig = {
    servers: {
      nextraceone: {
        url: mcpServerUrl,
        type: 'http',
        ...(config.apiKey ? { headers: { Authorization: `Bearer ${config.apiKey}` } } : {}),
      },
    },
  };

  let mcpConfigPath: string;

  if (scope.value === 'workspace') {
    const workspaceRoot = vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
    if (!workspaceRoot) {
      await vscode.window.showWarningMessage('NexTraceOne: No workspace folder open. Using global configuration.');
      const vscodeDir = path.join(os.homedir(), '.vscode');
      mcpConfigPath = path.join(vscodeDir, 'mcp.json');
    } else {
      mcpConfigPath = path.join(workspaceRoot, '.vscode', 'mcp.json');
    }
  } else {
    const vscodeDir = path.join(os.homedir(), '.vscode');
    mcpConfigPath = path.join(vscodeDir, 'mcp.json');
  }

  try {
    const dir = path.dirname(mcpConfigPath);
    if (!fs.existsSync(dir)) {
      fs.mkdirSync(dir, { recursive: true });
    }
    fs.writeFileSync(mcpConfigPath, JSON.stringify(mcpConfig, null, 2), 'utf-8');
    await vscode.window.showInformationMessage(
      `✓ MCP config written to ${mcpConfigPath}. Restart VS Code to apply.`,
      'Open File',
    ).then(async (action: string | undefined) => {
      if (action === 'Open File') {
        await vscode.workspace.openTextDocument(mcpConfigPath).then((doc: vscode.TextDocument) =>
          vscode.window.showTextDocument(doc));
      }
    });
    outputChannel.appendLine(`MCP configured at: ${mcpConfigPath}`);
    outputChannel.appendLine(`MCP server URL: ${mcpServerUrl}`);
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : String(err);
    await vscode.window.showErrorMessage(`NexTraceOne: Failed to write MCP config: ${message}`);
  }
}

/** Handles "NexTraceOne: Open Dashboard" command — opens the web UI in the default browser. */
async function handleOpenDashboardCommand(): Promise<void> {
  const config = getConfig();
  const dashboardUrl = config.serverUrl.replace(/\/$/, '');

  try {
    await vscode.env.openExternal(vscode.Uri.parse(dashboardUrl));
    outputChannel.appendLine(`Opened NexTraceOne dashboard: ${dashboardUrl}`);
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : String(err);
    await vscode.window.showErrorMessage(`NexTraceOne: Failed to open dashboard: ${message}`);
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// API Client
// ═══════════════════════════════════════════════════════════════════════════════

interface IdeQueryPayload {
  queryText: string;
  clientType: string;
  clientVersion: string;
  queryType: string;
  context?: string;
  persona?: string;
  serviceContext?: string;
}

/**
 * Calls POST /api/v1/ai/ide/query and returns the AI response text.
 * Handles both direct text responses and JSON envelopes with content/message fields.
 */
async function callIdeQueryApi(
  serverUrl: string,
  apiKey: string,
  payload: IdeQueryPayload,
): Promise<string> {
  const parsedUrl = new URL('/api/v1/ai/ide/query', serverUrl);
  const bodyStr = JSON.stringify(payload);

  return new Promise<string>((resolve, reject) => {
    const transport = parsedUrl.protocol === 'https:' ? https : http;

    const req = transport.request(
      {
        hostname: parsedUrl.hostname,
        port: parsedUrl.port ?? (parsedUrl.protocol === 'https:' ? '443' : '80'),
        path: parsedUrl.pathname + parsedUrl.search,
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Content-Length': Buffer.byteLength(bodyStr),
          'Authorization': `Bearer ${apiKey}`,
          'X-Client-Type': payload.clientType,
          'X-Client-Version': payload.clientVersion,
        },
        timeout: 30000,
      },
      (res) => {
        const chunks: Buffer[] = [];
        res.on('data', (chunk: Buffer) => chunks.push(chunk));
        res.on('end', () => {
          const body = Buffer.concat(chunks).toString('utf-8');
          if (res.statusCode && res.statusCode >= 400) {
            reject(new Error(`Server returned ${res.statusCode}: ${body}`));
            return;
          }
          try {
            const parsed = JSON.parse(body) as Record<string, unknown>;
            // Try common response shapes
            const answer =
              parsed['content'] ??
              parsed['output'] ??
              parsed['message'] ??
              parsed['response'] ??
              parsed['result'] ??
              body;
            resolve(String(answer));
          } catch {
            resolve(body);
          }
        });
      },
    );

    req.on('error', (err: Error) => reject(err));
    req.on('timeout', () => {
      req.destroy();
      reject(new Error('Request timed out after 30 seconds'));
    });

    req.write(bodyStr);
    req.end();
  });
}

// ═══════════════════════════════════════════════════════════════════════════════
// Helpers
// ═══════════════════════════════════════════════════════════════════════════════

interface NexConfig {
  serverUrl: string;
  apiKey: string;
  persona: string;
  defaultEnvironment: string;
}

function getConfig(): NexConfig {
  const cfg = vscode.workspace.getConfiguration('nextraceone');
  return {
    serverUrl: cfg.get<string>('serverUrl') ?? 'http://localhost:5000',
    apiKey: cfg.get<string>('apiKey') ?? '',
    persona: cfg.get<string>('persona') ?? 'Engineer',
    defaultEnvironment: cfg.get<string>('defaultEnvironment') ?? 'production',
  };
}

function escapeHtml(text: string): string {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}


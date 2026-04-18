"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
exports.activate = activate;
exports.deactivate = deactivate;
const vscode = __importStar(require("vscode"));
const https = __importStar(require("https"));
const http = __importStar(require("http"));
const fs = __importStar(require("fs"));
const path = __importStar(require("path"));
const os = __importStar(require("os"));
const url_1 = require("url");
let outputChannel;
// ═══════════════════════════════════════════════════════════════════════════════
// Activation
// ═══════════════════════════════════════════════════════════════════════════════
function activate(context) {
    outputChannel = vscode.window.createOutputChannel('NexTraceOne');
    outputChannel.appendLine('NexTraceOne extension activated.');
    // Register sidebar Chat Panel
    const chatViewProvider = new NexChatViewProvider(context);
    context.subscriptions.push(vscode.window.registerWebviewViewProvider('nextraceone.chatView', chatViewProvider));
    // Register Service Catalog Tree View
    const catalogProvider = new ServiceCatalogTreeProvider();
    context.subscriptions.push(vscode.window.registerTreeDataProvider('nextraceone.catalogView', catalogProvider));
    // Auto-load catalog on activation when API key is set
    const initialConfig = getConfig();
    if (initialConfig.apiKey) {
        void catalogProvider.load(initialConfig.serverUrl, initialConfig.apiKey);
    }
    // Register Chat Participant (@nextraceone)
    registerChatParticipant(context);
    // Register LanguageModel tools (VS Code ≥ 1.113)
    registerLanguageModelTools(context);
    // Status bar
    createStatusBar(context);
    // Register commands
    context.subscriptions.push(vscode.commands.registerCommand('nextraceone.chat', () => handleChatCommand(context)), vscode.commands.registerCommand('nextraceone.configure', handleConfigureCommand), vscode.commands.registerCommand('nextraceone.inspectService', handleInspectServiceCommand), vscode.commands.registerCommand('nextraceone.askAboutSelection', handleAskAboutSelectionCommand), vscode.commands.registerCommand('nextraceone.mcpConfigure', handleMcpConfigureCommand), vscode.commands.registerCommand('nextraceone.openDashboard', handleOpenDashboardCommand), vscode.commands.registerCommand('nextraceone.scaffold', () => handleScaffoldCommand()), vscode.commands.registerCommand('nextraceone.applyCode', (code, filename) => handleApplyCodeCommand(code, filename)), vscode.commands.registerCommand('nextraceone.openChatPanel', () => vscode.commands.executeCommand('nextraceone.chatView.focus')), vscode.commands.registerCommand('nextraceone.refreshCatalog', () => {
        const cfg = getConfig();
        if (cfg.apiKey) {
            void catalogProvider.load(cfg.serverUrl, cfg.apiKey);
        }
        else {
            void vscode.window.showWarningMessage('NexTraceOne: Configure your API key first.');
        }
    }), vscode.commands.registerCommand('nextraceone.openServiceDashboard', (serviceName) => handleOpenServiceDashboardCommand(serviceName)), vscode.commands.registerCommand('nextraceone.askAboutService', (serviceName) => {
        void chatViewProvider.sendQuery(`Show service context, ownership, contracts and recent changes for: ${serviceName}`);
        void vscode.commands.executeCommand('nextraceone.chatView.focus');
    }));
    // Reload catalog when settings change
    context.subscriptions.push(vscode.workspace.onDidChangeConfiguration((e) => {
        if (e.affectsConfiguration('nextraceone')) {
            updateStatusBar();
            const cfg = getConfig();
            if (cfg.apiKey) {
                void catalogProvider.load(cfg.serverUrl, cfg.apiKey);
            }
        }
    }));
}
function deactivate() {
    outputChannel?.dispose();
}
/**
 * NexChatViewProvider implementa um painel de chat Copilot-like na barra lateral do VS Code.
 * Mantém histórico de conversa por sessão e comunica com /api/v1/ai/ide/query.
 */
class NexChatViewProvider {
    constructor(_context) {
        this._context = _context;
        this._history = [];
    }
    resolveWebviewView(webviewView, _context, _token) {
        this._view = webviewView;
        webviewView.webview.options = {
            enableScripts: true,
            localResourceRoots: [this._context.extensionUri],
        };
        webviewView.webview.html = this._getHtmlContent(webviewView.webview);
        // Handle messages from the webview
        webviewView.webview.onDidReceiveMessage(async (message) => {
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
                case 'applyCode':
                    if (message.code) {
                        await vscode.commands.executeCommand('nextraceone.applyCode', message.code, message.filename);
                    }
                    break;
            }
        });
    }
    /**
     * Envia uma mensagem ao painel de chat a partir do código da extensão
     * (usado por comandos como inspectService ou askAboutSelection).
     */
    async sendQuery(text) {
        if (this._view) {
            this._view.show(true);
        }
        await this._handleUserMessage(text);
    }
    async _handleUserMessage(text) {
        const userMsg = {
            role: 'user',
            content: text,
            timestamp: new Date().toISOString(),
        };
        this._history.push(userMsg);
        this._postMessage({ type: 'addMessage', message: userMsg });
        const config = getConfig();
        if (!config.apiKey) {
            const errMsg = {
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
            const assistantMsg = {
                role: 'assistant',
                content: response,
                timestamp: new Date().toISOString(),
            };
            this._history.push(assistantMsg);
            this._postMessage({ type: 'addMessage', message: assistantMsg });
        }
        catch (err) {
            const errText = err instanceof Error ? err.message : String(err);
            const errMsg = {
                role: 'system',
                content: `❌ Error: ${errText}`,
                timestamp: new Date().toISOString(),
            };
            this._history.push(errMsg);
            this._postMessage({ type: 'addMessage', message: errMsg });
        }
        finally {
            this._postMessage({ type: 'setLoading', loading: false });
        }
    }
    _postMessage(message) {
        if (this._view) {
            void this._view.webview.postMessage(message);
        }
    }
    _getHtmlContent(_webview) {
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
    .msg-text { white-space: pre-wrap; word-wrap: break-word; }
    .code-block {
      margin: 6px 0;
      border: 1px solid var(--vscode-panel-border);
      border-radius: 4px;
      overflow: hidden;
    }
    .code-block-header {
      display: flex; align-items: center; justify-content: space-between;
      background: var(--vscode-titleBar-activeBackground, rgba(60,60,60,0.5));
      padding: 2px 8px; font-size: 10px;
      color: var(--vscode-descriptionForeground);
    }
    .code-block-lang { font-family: monospace; }
    .code-block-actions { display: flex; gap: 4px; }
    .code-action-btn {
      background: var(--vscode-button-background);
      color: var(--vscode-button-foreground);
      border: none; border-radius: 3px;
      padding: 2px 7px; font-size: 10px; cursor: pointer;
      white-space: nowrap;
    }
    .code-action-btn:hover { background: var(--vscode-button-hoverBackground); }
    .code-action-btn.secondary {
      background: var(--vscode-button-secondaryBackground, rgba(128,128,128,0.2));
      color: var(--vscode-button-secondaryForeground, var(--vscode-foreground));
    }
    .code-action-btn.secondary:hover { background: var(--vscode-button-secondaryHoverBackground, rgba(128,128,128,0.35)); }
    .code-block pre {
      margin: 0; padding: 8px 10px; overflow-x: auto;
      background: var(--vscode-editor-background);
      font-family: var(--vscode-editor-font-family, monospace);
      font-size: 11px; line-height: 1.4;
      white-space: pre; tab-size: 2;
    }
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

    function escapeHtmlStr(s) {
      return s.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
    }

    /**
     * Parses message content into segments: plain text and code blocks.
     * Returns array of {type:'text',content} | {type:'code',lang,content}
     */
    function parseContent(raw) {
      const segments = [];
      const codeBlockRe = /\x60\x60\x60([a-zA-Z0-9_\-]*)\n?([\s\S]*?)\x60\x60\x60/g;
      let last = 0;
      let match;
      while ((match = codeBlockRe.exec(raw)) !== null) {
        if (match.index > last) {
          segments.push({ type: 'text', content: raw.slice(last, match.index) });
        }
        segments.push({ type: 'code', lang: match[1] || 'text', content: match[2] || '' });
        last = match.index + match[0].length;
      }
      if (last < raw.length) segments.push({ type: 'text', content: raw.slice(last) });
      return segments;
    }

    function appendMessage(msg) {
      const div = document.createElement('div');
      div.className = 'message ' + msg.role;

      if (msg.role === 'assistant') {
        // Parse content for code blocks
        const segments = parseContent(msg.content);
        segments.forEach(seg => {
          if (seg.type === 'text') {
            if (seg.content.trim()) {
              const span = document.createElement('span');
              span.className = 'msg-text';
              span.textContent = seg.content;
              div.appendChild(span);
            }
          } else {
            // Code block with action buttons
            const block = document.createElement('div');
            block.className = 'code-block';

            const header = document.createElement('div');
            header.className = 'code-block-header';

            const langSpan = document.createElement('span');
            langSpan.className = 'code-block-lang';
            langSpan.textContent = seg.lang || 'text';
            header.appendChild(langSpan);

            const actions = document.createElement('div');
            actions.className = 'code-block-actions';

            // Insert at cursor button
            const insertBtn = document.createElement('button');
            insertBtn.className = 'code-action-btn';
            insertBtn.title = 'Insert code at cursor position in the active editor';
            insertBtn.textContent = '↓ Insert at Cursor';
            const capturedCode = seg.content;
            const capturedLang = seg.lang;
            insertBtn.addEventListener('click', () => {
              vscode.postMessage({ type: 'applyCode', code: capturedCode, filename: null });
              insertBtn.textContent = '✓ Applied';
              setTimeout(() => { insertBtn.textContent = '↓ Insert at Cursor'; }, 2000);
            });
            actions.appendChild(insertBtn);

            // Copy code button
            const copyCodeBtn = document.createElement('button');
            copyCodeBtn.className = 'code-action-btn secondary';
            copyCodeBtn.title = 'Copy code to clipboard';
            copyCodeBtn.textContent = 'Copy';
            copyCodeBtn.addEventListener('click', () => {
              navigator.clipboard.writeText(capturedCode).then(() => {
                copyCodeBtn.textContent = '✓';
                setTimeout(() => { copyCodeBtn.textContent = 'Copy'; }, 1500);
              });
            });
            actions.appendChild(copyCodeBtn);

            header.appendChild(actions);
            block.appendChild(header);

            const pre = document.createElement('pre');
            pre.innerHTML = escapeHtmlStr(capturedCode);
            block.appendChild(pre);

            div.appendChild(block);
          }
        });

        // Overall copy button
        const copyBtn = document.createElement('button');
        copyBtn.className = 'copy-btn';
        copyBtn.title = 'Copy full response to clipboard';
        copyBtn.textContent = 'Copy';
        copyBtn.addEventListener('click', () => {
          navigator.clipboard.writeText(msg.content).then(() => {
            copyBtn.textContent = '✓ Copied';
            setTimeout(() => { copyBtn.textContent = 'Copy'; }, 1500);
          }).catch(() => { copyBtn.textContent = 'Copy'; });
        });
        div.appendChild(copyBtn);
      } else {
        const contentSpan = document.createElement('span');
        contentSpan.textContent = msg.content;
        div.appendChild(contentSpan);
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
function registerChatParticipant(context) {
    // The Chat Participants API is available in VS Code ≥ 1.90
    // We guard with a check so the extension still loads on older versions
    if (!('chat' in vscode)) {
        outputChannel.appendLine('Chat Participants API not available in this VS Code version (requires ≥ 1.90).');
        return;
    }
    try {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const chatApi = vscode.chat;
        const participant = chatApi.createChatParticipant('nextraceone', async (request, _ctx, stream, token) => {
            const config = getConfig();
            if (!config.apiKey) {
                stream.markdown('⚠️ **NexTraceOne API key not configured.**\n\n' +
                    'Run `NexTraceOne: Configure` from the command palette to set your API key.');
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
                case 'scaffold':
                    queryType = 'CodeGeneration';
                    prefix = `[Scaffold/code generation] `;
                    break;
                case 'generate':
                    queryType = 'CodeGeneration';
                    prefix = `[Code generation] `;
                    break;
            }
            const queryText = prefix + request.prompt;
            stream.progress('Querying NexTraceOne AI…');
            if (token.isCancellationRequested)
                return;
            try {
                const response = await callIdeQueryApi(config.serverUrl, config.apiKey, {
                    queryText,
                    clientType: 'vscode-chat',
                    clientVersion: '0.2.0',
                    queryType,
                    persona: config.persona,
                });
                stream.markdown(response);
            }
            catch (err) {
                const msg = err instanceof Error ? err.message : String(err);
                stream.markdown(`❌ **Error contacting NexTraceOne:** ${msg}`);
            }
        });
        context.subscriptions.push({ dispose: () => participant.dispose() });
        outputChannel.appendLine('Chat participant @nextraceone registered.');
    }
    catch (err) {
        const msg = err instanceof Error ? err.message : String(err);
        outputChannel.appendLine(`Could not register chat participant: ${msg}`);
    }
}
// ═══════════════════════════════════════════════════════════════════════════════
// Status Bar
// ═══════════════════════════════════════════════════════════════════════════════
let _statusBarItem;
function createStatusBar(context) {
    _statusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 100);
    updateStatusBar();
    _statusBarItem.show();
    context.subscriptions.push(_statusBarItem);
}
function updateStatusBar() {
    if (!_statusBarItem)
        return;
    const config = getConfig();
    const env = config.defaultEnvironment || 'production';
    const workspaceService = path.basename(vscode.workspace.workspaceFolders?.[0]?.uri.fsPath ?? '');
    if (!config.apiKey) {
        _statusBarItem.text = '$(warning) NexTraceOne';
        _statusBarItem.tooltip = 'NexTraceOne: API key not configured. Click to configure.';
        _statusBarItem.command = 'nextraceone.configure';
        _statusBarItem.backgroundColor = new vscode.ThemeColor('statusBarItem.warningBackground');
    }
    else if (workspaceService) {
        _statusBarItem.text = `$(rocket) ${workspaceService} [${env}]`;
        _statusBarItem.tooltip = `NexTraceOne: ${workspaceService} · ${env}\nClick to open AI Chat`;
        _statusBarItem.command = 'nextraceone.openChatPanel';
        _statusBarItem.backgroundColor = undefined;
    }
    else {
        _statusBarItem.text = `$(rocket) NexTraceOne [${env}]`;
        _statusBarItem.tooltip = `NexTraceOne · ${env}\nClick to open AI Chat`;
        _statusBarItem.command = 'nextraceone.openChatPanel';
        _statusBarItem.backgroundColor = undefined;
    }
}
class CatalogNode extends vscode.TreeItem {
    constructor(label, collapsibleState, contextValue, serviceItem) {
        super(label, collapsibleState);
        this.contextValue = contextValue;
        this.serviceItem = serviceItem;
        this.contextValue = contextValue;
    }
}
class ServiceCatalogTreeProvider {
    constructor() {
        this._onDidChangeTreeData = new vscode.EventEmitter();
        this.onDidChangeTreeData = this._onDidChangeTreeData.event;
        this._services = [];
        this._loading = false;
    }
    refresh() {
        this._onDidChangeTreeData.fire();
    }
    async load(serverUrl, apiKey) {
        this._loading = true;
        this._error = undefined;
        this._onDidChangeTreeData.fire();
        try {
            this._services = await fetchCatalogServices(serverUrl, apiKey);
        }
        catch (err) {
            this._error = err instanceof Error ? err.message : String(err);
            this._services = [];
            outputChannel.appendLine(`[Catalog] Failed to load: ${this._error}`);
        }
        finally {
            this._loading = false;
            this._onDidChangeTreeData.fire();
        }
    }
    getTreeItem(element) {
        return element;
    }
    getChildren(element) {
        if (!element) {
            if (this._loading) {
                const n = new CatalogNode('Loading catalog…', vscode.TreeItemCollapsibleState.None, 'loading');
                n.iconPath = new vscode.ThemeIcon('loading~spin');
                return [n];
            }
            if (this._error) {
                const n = new CatalogNode(`Error: ${this._error}`, vscode.TreeItemCollapsibleState.None, 'error');
                n.iconPath = new vscode.ThemeIcon('error');
                return [n];
            }
            if (this._services.length === 0) {
                const n = new CatalogNode('No services found — check API key', vscode.TreeItemCollapsibleState.None, 'empty');
                n.iconPath = new vscode.ThemeIcon('info');
                return [n];
            }
            return this._services.map((s) => {
                const node = new CatalogNode(s.name, vscode.TreeItemCollapsibleState.Collapsed, 'service', s);
                node.description = s.teamName ?? '';
                node.tooltip = [s.name, s.domain, s.type, s.language].filter(Boolean).join(' · ');
                node.iconPath = new vscode.ThemeIcon('server');
                return node;
            });
        }
        if (element.contextValue === 'service' && element.serviceItem) {
            const s = element.serviceItem;
            const items = [];
            const addInfo = (label, icon) => {
                const n = new CatalogNode(label, vscode.TreeItemCollapsibleState.None, 'info');
                n.iconPath = new vscode.ThemeIcon(icon);
                items.push(n);
            };
            if (s.description)
                addInfo(s.description, 'quote');
            if (s.teamName)
                addInfo(`Team: ${s.teamName}`, 'organization');
            if (s.domain)
                addInfo(`Domain: ${s.domain}`, 'symbol-namespace');
            if (s.type)
                addInfo(`Type: ${s.type}`, 'symbol-class');
            if (s.language)
                addInfo(`Language: ${s.language}`, 'code');
            if (s.status)
                addInfo(`Status: ${s.status}`, 'circle-filled');
            const dashNode = new CatalogNode('Open in Dashboard', vscode.TreeItemCollapsibleState.None, 'action');
            dashNode.iconPath = new vscode.ThemeIcon('link-external');
            dashNode.command = { command: 'nextraceone.openServiceDashboard', title: 'Open in Dashboard', arguments: [s.name] };
            items.push(dashNode);
            const aiNode = new CatalogNode('Ask AI about this service', vscode.TreeItemCollapsibleState.None, 'action');
            aiNode.iconPath = new vscode.ThemeIcon('comment-discussion');
            aiNode.command = { command: 'nextraceone.askAboutService', title: 'Ask AI', arguments: [s.name] };
            items.push(aiNode);
            return items;
        }
        return [];
    }
}
function fetchCatalogServices(serverUrl, apiKey) {
    return new Promise((resolve, reject) => {
        const parsedUrl = new url_1.URL('/api/v1/catalog/services?pageSize=100', serverUrl);
        const transport = parsedUrl.protocol === 'https:' ? https : http;
        const req = transport.request({
            hostname: parsedUrl.hostname,
            port: parsedUrl.port,
            path: parsedUrl.pathname + parsedUrl.search,
            method: 'GET',
            headers: { 'Authorization': `Bearer ${apiKey}`, 'Accept': 'application/json' },
            timeout: 15000,
        }, (res) => {
            const chunks = [];
            res.on('data', (c) => chunks.push(c));
            res.on('end', () => {
                const body = Buffer.concat(chunks).toString('utf-8');
                if (res.statusCode && res.statusCode >= 400) {
                    reject(new Error(`Server ${res.statusCode}: ${body.slice(0, 200)}`));
                    return;
                }
                try {
                    const parsed = JSON.parse(body);
                    resolve(Array.isArray(parsed) ? parsed : (parsed.items ?? []));
                }
                catch {
                    reject(new Error('Invalid catalog response'));
                }
            });
        });
        req.on('error', reject);
        req.on('timeout', () => { req.destroy(); reject(new Error('Request timed out')); });
        req.end();
    });
}
// ═══════════════════════════════════════════════════════════════════════════════
// LanguageModel Tools (VS Code ≥ 1.113)
// ═══════════════════════════════════════════════════════════════════════════════
function registerLanguageModelTools(context) {
    // vscode.lm.registerTool is available in VS Code ≥ 1.113; guard for older versions
    const lmApi = vscode['lm'];
    if (typeof lmApi?.registerTool !== 'function') {
        outputChannel.appendLine('LanguageModelTool API not available (requires VS Code ≥ 1.113).');
        return;
    }
    const registerTool = lmApi.registerTool.bind(lmApi);
    function makeResult(text) {
        try {
            // Use the proper LanguageModelToolResult API when available
            const vscAny = vscode;
            const TextPart = vscAny['LanguageModelTextPart'];
            const ToolResult = vscAny['LanguageModelToolResult'];
            if (TextPart && ToolResult) {
                return new ToolResult([new TextPart(text)]);
            }
        }
        catch { /* fall through */ }
        return { content: [{ value: text }] };
    }
    const tools = [
        {
            name: 'nextraceone_get_service',
            invoke: async (input) => {
                const config = getConfig();
                if (!config.apiKey)
                    return 'NexTraceOne API key not configured.';
                return callIdeQueryApi(config.serverUrl, config.apiKey, {
                    queryText: `Show service context, ownership, contracts and reliability for: ${input['serviceName']}`,
                    clientType: 'vscode-tool', clientVersion: '0.5.0',
                    queryType: 'OwnershipLookup', serviceContext: input['serviceName'], persona: config.persona,
                });
            },
        },
        {
            name: 'nextraceone_get_contract',
            invoke: async (input) => {
                const config = getConfig();
                if (!config.apiKey)
                    return 'NexTraceOne API key not configured.';
                const q = input['version']
                    ? `Show contract details for: ${input['contractName']} version ${input['version']}`
                    : `Show contract details, schema and examples for: ${input['contractName']}`;
                return callIdeQueryApi(config.serverUrl, config.apiKey, {
                    queryText: q,
                    clientType: 'vscode-tool', clientVersion: '0.5.0',
                    queryType: 'ContractSuggestion', persona: config.persona,
                });
            },
        },
        {
            name: 'nextraceone_blast_radius',
            invoke: async (input) => {
                const config = getConfig();
                if (!config.apiKey)
                    return 'NexTraceOne API key not configured.';
                const env = input['environment'] ?? config.defaultEnvironment ?? 'production';
                return callIdeQueryApi(config.serverUrl, config.apiKey, {
                    queryText: `Analyse blast radius for service: ${input['serviceName']} in environment: ${env}. List dependent services and potential impact.`,
                    clientType: 'vscode-tool', clientVersion: '0.5.0',
                    queryType: 'BreakingChangeAlert', serviceContext: input['serviceName'], persona: config.persona,
                });
            },
        },
        {
            name: 'nextraceone_get_incident',
            invoke: async (input) => {
                const config = getConfig();
                if (!config.apiKey)
                    return 'NexTraceOne API key not configured.';
                const q = input['serviceName']
                    ? `[Incident context] ${input['query']} for service: ${input['serviceName']}`
                    : `[Incident context] ${input['query']}`;
                return callIdeQueryApi(config.serverUrl, config.apiKey, {
                    queryText: q,
                    clientType: 'vscode-tool', clientVersion: '0.5.0',
                    queryType: 'GeneralQuery', serviceContext: input['serviceName'], persona: config.persona,
                });
            },
        },
    ];
    for (const tool of tools) {
        try {
            const disposable = registerTool(tool.name, {
                invoke: async (opts, _token) => {
                    try {
                        const text = await tool.invoke(opts.input);
                        return makeResult(text);
                    }
                    catch (err) {
                        return makeResult(`Error: ${err instanceof Error ? err.message : String(err)}`);
                    }
                },
            });
            context.subscriptions.push(disposable);
            outputChannel.appendLine(`Registered LanguageModelTool: ${tool.name}`);
        }
        catch (err) {
            outputChannel.appendLine(`Could not register tool ${tool.name}: ${err instanceof Error ? err.message : String(err)}`);
        }
    }
}
// ═══════════════════════════════════════════════════════════════════════════════
// Commands
// ═══════════════════════════════════════════════════════════════════════════════
/** Handles the "NexTraceOne: Ask AI" command — quick-input fallback. */
async function handleChatCommand(_context) {
    const config = getConfig();
    if (!config.apiKey) {
        const action = await vscode.window.showWarningMessage('NexTraceOne: No API key configured.', 'Configure');
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
    if (!query)
        return;
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
        await vscode.window.showInformationMessage(`NexTraceOne AI: ${response.slice(0, 200)}${response.length > 200 ? '...' : ''}`);
    }
    catch (err) {
        const message = err instanceof Error ? err.message : String(err);
        outputChannel.appendLine(`[Error]: ${message}`);
        await vscode.window.showErrorMessage(`NexTraceOne AI Error: ${message}`);
    }
}
/** Handles the "NexTraceOne: Configure" command. */
async function handleConfigureCommand() {
    await vscode.commands.executeCommand('workbench.action.openSettings', 'nextraceone');
}
/** Handles right-click → "NexTraceOne: Inspect Service Context". */
async function handleInspectServiceCommand() {
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
    if (!service || !config.apiKey)
        return;
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
        const panel = vscode.window.createWebviewPanel('nextraceoneServiceContext', `NexTraceOne: ${service}`, vscode.ViewColumn.Beside, { enableScripts: false });
        panel.webview.html = `<html><body style="font-family:sans-serif;padding:16px;white-space:pre-wrap">${escapeHtml(response)}</body></html>`;
    }
    catch (err) {
        const message = err instanceof Error ? err.message : String(err);
        await vscode.window.showErrorMessage(`NexTraceOne: ${message}`);
    }
}
/** Handles right-click on selection → "NexTraceOne: Ask AI about this". */
async function handleAskAboutSelectionCommand() {
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
        await vscode.window.showWarningMessage('NexTraceOne: No API key configured. Run NexTraceOne: Configure.');
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
        const panel = vscode.window.createWebviewPanel('nextraceoneAskSelection', 'NexTraceOne AI Analysis', vscode.ViewColumn.Beside, { enableScripts: false });
        panel.webview.html = `<html><body style="font-family:sans-serif;padding:16px;white-space:pre-wrap">${escapeHtml(response)}</body></html>`;
    }
    catch (err) {
        const message = err instanceof Error ? err.message : String(err);
        await vscode.window.showErrorMessage(`NexTraceOne AI Error: ${message}`);
    }
}
/** Handles "NexTraceOne: Configure MCP Server" command. */
async function handleMcpConfigureCommand() {
    const config = getConfig();
    const mcpServerUrl = config.serverUrl.replace(/\/$/, '') + '/api/v1/ai/mcp';
    // Ask user whether to configure globally (~/.vscode/mcp.json) or at workspace level (.vscode/mcp.json)
    const scope = await vscode.window.showQuickPick([
        { label: '$(home) Global', description: 'Write to ~/.vscode/mcp.json (all workspaces)', value: 'global' },
        { label: '$(folder) Workspace', description: 'Write to .vscode/mcp.json in current workspace', value: 'workspace' },
    ], { placeHolder: 'Choose MCP configuration scope', title: 'NexTraceOne MCP Configuration' });
    if (!scope)
        return;
    const mcpConfig = {
        servers: {
            nextraceone: {
                url: mcpServerUrl,
                type: 'http',
                ...(config.apiKey ? { headers: { Authorization: `Bearer ${config.apiKey}` } } : {}),
            },
        },
    };
    let mcpConfigPath;
    if (scope.value === 'workspace') {
        const workspaceRoot = vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
        if (!workspaceRoot) {
            await vscode.window.showWarningMessage('NexTraceOne: No workspace folder open. Using global configuration.');
            const vscodeDir = path.join(os.homedir(), '.vscode');
            mcpConfigPath = path.join(vscodeDir, 'mcp.json');
        }
        else {
            mcpConfigPath = path.join(workspaceRoot, '.vscode', 'mcp.json');
        }
    }
    else {
        const vscodeDir = path.join(os.homedir(), '.vscode');
        mcpConfigPath = path.join(vscodeDir, 'mcp.json');
    }
    try {
        const dir = path.dirname(mcpConfigPath);
        if (!fs.existsSync(dir)) {
            fs.mkdirSync(dir, { recursive: true });
        }
        fs.writeFileSync(mcpConfigPath, JSON.stringify(mcpConfig, null, 2), 'utf-8');
        await vscode.window.showInformationMessage(`✓ MCP config written to ${mcpConfigPath}. Restart VS Code to apply.`, 'Open File').then(async (action) => {
            if (action === 'Open File') {
                await vscode.workspace.openTextDocument(mcpConfigPath).then((doc) => vscode.window.showTextDocument(doc));
            }
        });
        outputChannel.appendLine(`MCP configured at: ${mcpConfigPath}`);
        outputChannel.appendLine(`MCP server URL: ${mcpServerUrl}`);
    }
    catch (err) {
        const message = err instanceof Error ? err.message : String(err);
        await vscode.window.showErrorMessage(`NexTraceOne: Failed to write MCP config: ${message}`);
    }
}
/** Handles "NexTraceOne: Open Dashboard" command — opens the web UI in the default browser. */
async function handleOpenDashboardCommand() {
    const config = getConfig();
    const dashboardUrl = config.serverUrl.replace(/\/$/, '');
    try {
        await vscode.env.openExternal(vscode.Uri.parse(dashboardUrl));
        outputChannel.appendLine(`Opened NexTraceOne dashboard: ${dashboardUrl}`);
    }
    catch (err) {
        const message = err instanceof Error ? err.message : String(err);
        await vscode.window.showErrorMessage(`NexTraceOne: Failed to open dashboard: ${message}`);
    }
}
/** Handles opening a specific service page in the NexTraceOne dashboard. */
async function handleOpenServiceDashboardCommand(serviceName) {
    const config = getConfig();
    const serviceUrl = `${config.serverUrl.replace(/\/$/, '')}/services/${encodeURIComponent(serviceName)}`;
    try {
        await vscode.env.openExternal(vscode.Uri.parse(serviceUrl));
        outputChannel.appendLine(`Opened service dashboard: ${serviceUrl}`);
    }
    catch (err) {
        const message = err instanceof Error ? err.message : String(err);
        await vscode.window.showErrorMessage(`NexTraceOne: Failed to open service dashboard: ${message}`);
    }
}
/**
 * Handles "NexTraceOne: Scaffold New Service" command.
 * Multi-step wizard: pick template → enter service name → configure → generate files.
 */
async function handleScaffoldCommand() {
    const config = getConfig();
    if (!config.apiKey) {
        const action = await vscode.window.showWarningMessage('NexTraceOne: No API key configured.', 'Configure');
        if (action === 'Configure')
            await handleConfigureCommand();
        return;
    }
    // Step 1: Fetch available templates
    let templates = [];
    await vscode.window.withProgress({ location: vscode.ProgressLocation.Notification, title: 'NexTraceOne: Loading templates…' }, async () => {
        try {
            templates = await fetchTemplates(config.serverUrl, config.apiKey);
        }
        catch (err) {
            const msg = err instanceof Error ? err.message : String(err);
            await vscode.window.showErrorMessage(`NexTraceOne: Could not fetch templates: ${msg}`);
        }
    });
    if (templates.length === 0) {
        await vscode.window.showWarningMessage('NexTraceOne: No active service templates found. Configure templates in the NexTraceOne platform.');
        return;
    }
    // Step 2: Pick template
    const templateItems = templates.map((t) => ({
        label: `$(symbol-class) ${t.displayName}`,
        description: `${t.serviceType} · ${t.language} · v${t.version}`,
        detail: `${t.description}${t.hasBaseContract ? '  $(file-code) Contract' : ''}${t.hasScaffoldingManifest ? '  $(list-tree) Manifest' : ''}`,
        template: t,
    }));
    const picked = await vscode.window.showQuickPick(templateItems, {
        placeHolder: 'Select a service template',
        title: 'NexTraceOne: Scaffold New Service (1/4)',
        matchOnDescription: true,
        matchOnDetail: true,
    });
    if (!picked)
        return;
    const selectedTemplate = picked.template;
    // Step 3: Enter service name
    const serviceName = await vscode.window.showInputBox({
        prompt: 'Service name (lowercase kebab-case)',
        placeHolder: 'e.g. payment-service',
        title: 'NexTraceOne: Scaffold New Service (2/4)',
        validateInput: (v) => {
            if (!v)
                return 'Service name is required';
            if (!/^[a-z0-9][a-z0-9\-]{0,62}[a-z0-9]$/.test(v))
                return 'Must be lowercase kebab-case (e.g. payment-service)';
            return null;
        },
    });
    if (!serviceName)
        return;
    // Step 4: Configure team / domain
    const team = await vscode.window.showInputBox({
        prompt: 'Team name (optional)',
        placeHolder: selectedTemplate.defaultTeam || 'e.g. platform-team',
        title: 'NexTraceOne: Scaffold New Service (3/4)',
        value: selectedTemplate.defaultTeam,
    });
    const domain = await vscode.window.showInputBox({
        prompt: 'Domain (optional)',
        placeHolder: selectedTemplate.defaultDomain || 'e.g. payments',
        title: 'NexTraceOne: Scaffold New Service (4/4)',
        value: selectedTemplate.defaultDomain,
    });
    // Step 5: Pick output directory
    const workspaceRoot = vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
    const defaultOutputDir = workspaceRoot ? path.join(workspaceRoot, serviceName) : serviceName;
    const outputDirResult = await vscode.window.showInputBox({
        prompt: 'Output directory',
        value: defaultOutputDir,
        title: 'NexTraceOne: Output Directory',
    });
    if (!outputDirResult)
        return;
    // Step 6: Call scaffold API and write files
    await vscode.window.withProgress({
        location: vscode.ProgressLocation.Notification,
        title: `NexTraceOne: Scaffolding ${serviceName}…`,
        cancellable: false,
    }, async (progress) => {
        progress.report({ increment: 20, message: 'Calling scaffold API…' });
        let plan;
        try {
            plan = await callScaffoldApi(config.serverUrl, config.apiKey, selectedTemplate.slug, serviceName, team || undefined, domain || undefined);
        }
        catch (err) {
            const msg = err instanceof Error ? err.message : String(err);
            await vscode.window.showErrorMessage(`NexTraceOne Scaffold: ${msg}`);
            return;
        }
        progress.report({ increment: 40, message: 'Writing files…' });
        let filesWritten = 0;
        const errorFiles = [];
        for (const file of plan.files) {
            if (!file.path)
                continue;
            try {
                const fullPath = path.join(outputDirResult, file.path.replace(/\//g, path.sep));
                const fileDir = path.dirname(fullPath);
                await fs.promises.mkdir(fileDir, { recursive: true });
                await fs.promises.writeFile(fullPath, file.content ?? '', 'utf-8');
                filesWritten++;
            }
            catch {
                errorFiles.push(file.path);
            }
        }
        // Write base contract spec if available
        if (plan.baseContractSpec) {
            const ext = plan.serviceType?.toLowerCase().includes('rest') ? 'openapi.json'
                : plan.serviceType?.toLowerCase().includes('kafka') ? 'asyncapi.yaml'
                    : plan.serviceType?.toLowerCase().includes('soap') ? 'service.wsdl'
                        : 'contract.json';
            const contractPath = path.join(outputDirResult, `${serviceName}-${ext}`);
            try {
                await fs.promises.writeFile(contractPath, plan.baseContractSpec, 'utf-8');
                filesWritten++;
            }
            catch { /* non-fatal */ }
        }
        progress.report({ increment: 90, message: 'Opening project…' });
        // Open the output folder in a new workspace window or add to workspace
        const outputUri = vscode.Uri.file(outputDirResult);
        const action = await vscode.window.showInformationMessage(`✓ ${serviceName} scaffolded! ${filesWritten} file(s) created.`, 'Open Folder', 'Add to Workspace', 'Register in Catalog');
        if (action === 'Open Folder') {
            await vscode.commands.executeCommand('vscode.openFolder', outputUri, { forceNewWindow: true });
        }
        else if (action === 'Add to Workspace') {
            vscode.workspace.updateWorkspaceFolders(vscode.workspace.workspaceFolders?.length ?? 0, 0, { uri: outputUri, name: serviceName });
        }
        else if (action === 'Register in Catalog') {
            await registerScaffoldedService(config.serverUrl, config.apiKey, plan);
            await vscode.window.showInformationMessage(`✓ ${serviceName} registered in NexTraceOne catalog.`);
        }
    });
}
/** Fetches active service templates from the NexTraceOne catalog API. */
function fetchTemplates(serverUrl, apiKey) {
    return new Promise((resolve, reject) => {
        const parsedUrl = new url_1.URL('/api/v1/catalog/templates?isActive=true', serverUrl);
        const transport = parsedUrl.protocol === 'https:' ? https : http;
        const req = transport.request({
            hostname: parsedUrl.hostname,
            port: parsedUrl.port,
            path: parsedUrl.pathname + parsedUrl.search,
            method: 'GET',
            headers: { 'Authorization': `Bearer ${apiKey}`, 'Accept': 'application/json' },
            timeout: 15000,
        }, (res) => {
            const chunks = [];
            res.on('data', (c) => chunks.push(c));
            res.on('end', () => {
                const body = Buffer.concat(chunks).toString('utf-8');
                if (res.statusCode && res.statusCode >= 400) {
                    reject(new Error(`Server ${res.statusCode}: ${body}`));
                    return;
                }
                try {
                    const parsed = JSON.parse(body);
                    resolve(parsed.items ?? []);
                }
                catch {
                    reject(new Error('Invalid response from templates API'));
                }
            });
        });
        req.on('error', reject);
        req.on('timeout', () => { req.destroy(); reject(new Error('Request timed out')); });
        req.end();
    });
}
/** Calls the scaffold API and returns the scaffolding plan. */
function callScaffoldApi(serverUrl, apiKey, templateSlug, serviceName, team, domain) {
    return new Promise((resolve, reject) => {
        const bodyStr = JSON.stringify({ serviceName, teamName: team, domain });
        const parsedUrl = new url_1.URL(`/api/v1/catalog/templates/slug/${encodeURIComponent(templateSlug)}/scaffold`, serverUrl);
        const transport = parsedUrl.protocol === 'https:' ? https : http;
        const req = transport.request({
            hostname: parsedUrl.hostname,
            port: parsedUrl.port,
            path: parsedUrl.pathname,
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Content-Length': Buffer.byteLength(bodyStr),
                'Authorization': `Bearer ${apiKey}`,
            },
            timeout: 30000,
        }, (res) => {
            const chunks = [];
            res.on('data', (c) => chunks.push(c));
            res.on('end', () => {
                const body = Buffer.concat(chunks).toString('utf-8');
                if (res.statusCode === 404) {
                    reject(new Error(`Template '${templateSlug}' not found`));
                    return;
                }
                if (res.statusCode && res.statusCode >= 400) {
                    reject(new Error(`Server ${res.statusCode}: ${body}`));
                    return;
                }
                try {
                    resolve(JSON.parse(body));
                }
                catch {
                    reject(new Error('Invalid scaffold response'));
                }
            });
        });
        req.on('error', reject);
        req.on('timeout', () => { req.destroy(); reject(new Error('Scaffold request timed out')); });
        req.write(bodyStr);
        req.end();
    });
}
/** Registers a scaffolded service in the NexTraceOne catalog. */
function registerScaffoldedService(serverUrl, apiKey, plan) {
    return new Promise((resolve, reject) => {
        const bodyStr = JSON.stringify({
            serviceName: plan.serviceName,
            domain: plan.domain,
            teamName: plan.teamName,
            templateSlug: plan.templateSlug,
            scaffoldingId: plan.scaffoldingId,
        });
        const parsedUrl = new url_1.URL('/api/v1/catalog/scaffold/register', serverUrl);
        const transport = parsedUrl.protocol === 'https:' ? https : http;
        const req = transport.request({
            hostname: parsedUrl.hostname,
            port: parsedUrl.port,
            path: parsedUrl.pathname,
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Content-Length': Buffer.byteLength(bodyStr),
                'Authorization': `Bearer ${apiKey}`,
            },
            timeout: 15000,
        }, (res) => {
            const chunks = [];
            res.on('data', (c) => chunks.push(c));
            res.on('end', () => {
                if (res.statusCode && res.statusCode >= 400) {
                    const body = Buffer.concat(chunks).toString('utf-8');
                    reject(new Error(`Registration failed ${res.statusCode}: ${body}`));
                }
                else {
                    resolve();
                }
            });
        });
        req.on('error', reject);
        req.on('timeout', () => { req.destroy(); reject(new Error('Registration request timed out')); });
        req.write(bodyStr);
        req.end();
    });
}
// ═══════════════════════════════════════════════════════════════════════════════
// Code Apply
// ═══════════════════════════════════════════════════════════════════════════════
/**
 * Handles "NexTraceOne: Apply Code" command.
 * Inserts the given code at the active editor cursor position.
 * If no editor is active, offers to create a new file.
 */
async function handleApplyCodeCommand(code, filename) {
    const editor = vscode.window.activeTextEditor;
    if (editor) {
        // Insert code at current cursor position
        const selection = editor.selection;
        await editor.edit((editBuilder) => {
            if (!selection.isEmpty) {
                editBuilder.replace(selection, code);
            }
            else {
                editBuilder.insert(selection.active, code);
            }
        });
        await vscode.window.showInformationMessage('✓ Code applied to editor.');
        return;
    }
    // No active editor — offer to create a new file
    const targetFilename = filename
        ?? await vscode.window.showInputBox({
            prompt: 'Enter filename to create',
            placeHolder: 'e.g. MyService.cs or index.ts',
        });
    if (!targetFilename)
        return;
    const workspaceRoot = vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
    const filePath = workspaceRoot
        ? path.join(workspaceRoot, targetFilename)
        : targetFilename;
    try {
        await fs.promises.writeFile(filePath, code, 'utf-8');
        const doc = await vscode.workspace.openTextDocument(filePath);
        await vscode.window.showTextDocument(doc);
        await vscode.window.showInformationMessage(`✓ Code written to ${path.basename(filePath)}.`);
    }
    catch (err) {
        const msg = err instanceof Error ? err.message : String(err);
        await vscode.window.showErrorMessage(`NexTraceOne: Could not write file: ${msg}`);
    }
}
/**
 * Calls POST /api/v1/ai/ide/query and returns the AI response text.
 * Handles both direct text responses and JSON envelopes with content/message fields.
 */
async function callIdeQueryApi(serverUrl, apiKey, payload) {
    const parsedUrl = new url_1.URL('/api/v1/ai/ide/query', serverUrl);
    const bodyStr = JSON.stringify(payload);
    return new Promise((resolve, reject) => {
        const transport = parsedUrl.protocol === 'https:' ? https : http;
        const req = transport.request({
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
        }, (res) => {
            const chunks = [];
            res.on('data', (chunk) => chunks.push(chunk));
            res.on('end', () => {
                const body = Buffer.concat(chunks).toString('utf-8');
                if (res.statusCode && res.statusCode >= 400) {
                    reject(new Error(`Server returned ${res.statusCode}: ${body}`));
                    return;
                }
                try {
                    const parsed = JSON.parse(body);
                    // Try common response shapes
                    const answer = parsed['content'] ??
                        parsed['output'] ??
                        parsed['message'] ??
                        parsed['response'] ??
                        parsed['result'] ??
                        body;
                    resolve(String(answer));
                }
                catch {
                    resolve(body);
                }
            });
        });
        req.on('error', (err) => reject(err));
        req.on('timeout', () => {
            req.destroy();
            reject(new Error('Request timed out after 30 seconds'));
        });
        req.write(bodyStr);
        req.end();
    });
}
function getConfig() {
    const cfg = vscode.workspace.getConfiguration('nextraceone');
    return {
        serverUrl: cfg.get('serverUrl') ?? 'http://localhost:5000',
        apiKey: cfg.get('apiKey') ?? '',
        persona: cfg.get('persona') ?? 'Engineer',
        defaultEnvironment: cfg.get('defaultEnvironment') ?? 'production',
    };
}
function escapeHtml(text) {
    return text
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}
//# sourceMappingURL=extension.js.map
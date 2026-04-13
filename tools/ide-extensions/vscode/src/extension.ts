import * as vscode from 'vscode';
import * as https from 'https';
import * as http from 'http';
import { URL } from 'url';

let outputChannel: vscode.OutputChannel;

export function activate(context: vscode.ExtensionContext): void {
  outputChannel = vscode.window.createOutputChannel('NexTraceOne');
  outputChannel.appendLine('NexTraceOne Copilot extension activated.');

  context.subscriptions.push(
    vscode.commands.registerCommand('nextraceone.chat', handleChatCommand),
    vscode.commands.registerCommand('nextraceone.configure', handleConfigureCommand),
  );
}

export function deactivate(): void {
  outputChannel?.dispose();
}

/** Handles the "NexTraceOne: Ask AI" command. */
async function handleChatCommand(): Promise<void> {
  const config = vscode.workspace.getConfiguration('nextraceone');
  const serverUrl = config.get<string>('serverUrl') ?? 'http://localhost:5000';
  const apiKey = config.get<string>('apiKey') ?? '';

  if (!apiKey) {
    const action = await vscode.window.showWarningMessage(
      'NexTraceOne: No API key configured. Please set nextraceone.apiKey in settings.',
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
    const responseBody = await callNexTraceOneApi(serverUrl, apiKey, query);
    outputChannel.appendLine(`[Response]: ${responseBody}`);
    await vscode.window.showInformationMessage(
      `NexTraceOne AI: ${responseBody.slice(0, 200)}${responseBody.length > 200 ? '...' : ''}`,
    );
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : String(err);
    outputChannel.appendLine(`[Error]: ${message}`);
    await vscode.window.showErrorMessage(`NexTraceOne AI Error: ${message}`);
  }
}

/** Handles the "NexTraceOne: Configure" command. */
async function handleConfigureCommand(): Promise<void> {
  await vscode.commands.executeCommand(
    'workbench.action.openSettings',
    'nextraceone',
  );
}

/** Calls POST {serverUrl}/api/v1/ai/ide/query and returns the response text. */
async function callNexTraceOneApi(
  serverUrl: string,
  apiKey: string,
  query: string,
): Promise<string> {
  const parsedUrl = new URL('/api/v1/ai/ide/query', serverUrl);
  const payload = JSON.stringify({
    query,
    clientType: 'vscode',
    persona: 'Engineer',
  });

  return new Promise<string>((resolve, reject) => {
    const transport = parsedUrl.protocol === 'https:' ? https : http;

    const req = transport.request(
      {
        hostname: parsedUrl.hostname,
        port: parsedUrl.port ?? (parsedUrl.protocol === 'https:' ? 443 : 80),
        path: parsedUrl.pathname + parsedUrl.search,
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Content-Length': Buffer.byteLength(payload),
          'Authorization': `Bearer ${apiKey}`,
          'X-Client-Type': 'vscode',
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
            const answer = (parsed['content'] ?? parsed['output'] ?? parsed['message'] ?? body) as string;
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

    req.write(payload);
    req.end();
  });
}

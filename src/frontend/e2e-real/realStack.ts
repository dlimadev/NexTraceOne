import { execFile, spawn } from 'node:child_process';
import { open, readFile, rm, writeFile } from 'node:fs/promises';
import * as path from 'node:path';
import { promisify } from 'node:util';

const execFileAsync = promisify(execFile);

export const repoRoot = path.resolve(import.meta.dirname, '..');
export const backendProjectPath = path.resolve(repoRoot, '../platform/NexTraceOne.ApiHost/NexTraceOne.ApiHost.csproj');
export const stateFilePath = '/tmp/nextraceone-playwright-real-state.json';
export const backendLogPath = '/tmp/nextraceone-playwright-real-backend.log';
export const postgresContainerName = 'nextraceone-rh6-playwright-postgres';
export const backendBaseUrl = 'http://127.0.0.1:5187';
export const postgresPort = 15432;

const databaseMap = {
  catalog: 'nextrace_real_catalog',
  changeGovernance: 'nextrace_real_change_governance',
  identity: 'nextrace_real_identity',
  incidents: 'nextrace_real_incidents',
  runtime: 'nextrace_real_runtime',
  cost: 'nextrace_real_cost',
  aiGovernance: 'nextrace_real_aigovernance',
  externalAi: 'nextrace_real_externalai',
  aiOrchestration: 'nextrace_real_aiorchestration',
  governance: 'nextrace_real_governance',
  audit: 'nextrace_real_audit',
} as const;

function buildConnectionString(database: string): string {
  return `Host=127.0.0.1;Port=${postgresPort};Database=${database};Username=postgres;Password=postgres;Include Error Detail=true`;
}

export function buildBackendEnvironment(): NodeJS.ProcessEnv {
  return {
    ...process.env,
    ASPNETCORE_ENVIRONMENT: 'Development',
    ASPNETCORE_URLS: backendBaseUrl,
    NEXTRACE_SKIP_INTEGRITY: 'true',
    NEXTRACE_IGNORE_PENDING_MODEL_CHANGES: 'true',
    ConnectionStrings__NexTraceOne: buildConnectionString(databaseMap.catalog),
    ConnectionStrings__CatalogDatabase: buildConnectionString(databaseMap.catalog),
    ConnectionStrings__ContractsDatabase: buildConnectionString(databaseMap.catalog),
    ConnectionStrings__DeveloperPortalDatabase: buildConnectionString(databaseMap.catalog),
    ConnectionStrings__ChangeIntelligenceDatabase: buildConnectionString(databaseMap.changeGovernance),
    ConnectionStrings__WorkflowDatabase: buildConnectionString(databaseMap.changeGovernance),
    ConnectionStrings__RulesetGovernanceDatabase: buildConnectionString(databaseMap.changeGovernance),
    ConnectionStrings__PromotionDatabase: buildConnectionString(databaseMap.changeGovernance),
    ConnectionStrings__IdentityDatabase: buildConnectionString(databaseMap.identity),
    ConnectionStrings__IncidentDatabase: buildConnectionString(databaseMap.incidents),
    ConnectionStrings__RuntimeIntelligenceDatabase: buildConnectionString(databaseMap.runtime),
    ConnectionStrings__CostIntelligenceDatabase: buildConnectionString(databaseMap.cost),
    ConnectionStrings__AiGovernanceDatabase: buildConnectionString(databaseMap.aiGovernance),
    ConnectionStrings__ExternalAiDatabase: buildConnectionString(databaseMap.externalAi),
    ConnectionStrings__AiOrchestrationDatabase: buildConnectionString(databaseMap.aiOrchestration),
    ConnectionStrings__GovernanceDatabase: buildConnectionString(databaseMap.governance),
    ConnectionStrings__AuditDatabase: buildConnectionString(databaseMap.audit),
  };
}

export async function execCommand(command: string, args: string[]): Promise<string> {
  const { stdout, stderr } = await execFileAsync(command, args, { cwd: repoRoot });
  return `${stdout}${stderr}`;
}

export async function removeContainerIfPresent(): Promise<void> {
  try {
    await execCommand('docker', ['rm', '-f', postgresContainerName]);
  } catch {
    // Container may not exist from previous run.
  }
}

export async function startPostgresContainer(): Promise<void> {
  await removeContainerIfPresent();

  await execCommand('docker', [
    'run',
    '--detach',
    '--name', postgresContainerName,
    '--publish', `${postgresPort}:5432`,
    '--env', 'POSTGRES_USER=postgres',
    '--env', 'POSTGRES_PASSWORD=postgres',
    '--env', 'POSTGRES_DB=postgres',
    'postgres:16-alpine',
  ]);

  await waitFor(async () => {
    try {
      await execCommand('docker', ['exec', postgresContainerName, 'pg_isready', '-U', 'postgres']);
      return true;
    } catch {
      return false;
    }
  }, 60_000, 'PostgreSQL container did not become ready in time.');
}

export async function createDatabases(): Promise<void> {
  for (const database of Object.values(databaseMap)) {
    await execCommand('docker', [
      'exec',
      postgresContainerName,
      'psql',
      '-U',
      'postgres',
      '-d',
      'postgres',
      '-c',
      `CREATE DATABASE "${database}"`,
    ]);
  }
}

export async function startBackendProcess(): Promise<number> {
  const logHandle = await open(backendLogPath, 'a');
  const child = spawn(
    'dotnet',
    ['run', '--project', backendProjectPath, '--no-launch-profile'],
    {
      cwd: repoRoot,
      env: buildBackendEnvironment(),
      detached: true,
      stdio: ['ignore', logHandle.fd, logHandle.fd],
    },
  );

  child.unref();
  await logHandle.close();

  await waitForHttp(`${backendBaseUrl}/health`, 180_000);

  if (!child.pid) {
    throw new Error('Backend process did not expose a PID.');
  }

  return child.pid;
}

export async function stopBackendProcess(pid: number | undefined): Promise<void> {
  if (!pid) return;

  try {
    process.kill(-pid, 'SIGTERM');
  } catch {
    try {
      process.kill(pid, 'SIGTERM');
    } catch {
      // Process already exited.
    }
  }
}

export async function stopPostgresContainer(): Promise<void> {
  await removeContainerIfPresent();
}

export async function writeState(state: { backendPid?: number }): Promise<void> {
  await writeFile(stateFilePath, JSON.stringify(state), 'utf-8');
}

export async function readState(): Promise<{ backendPid?: number }> {
  const content = await readFile(stateFilePath, 'utf-8');
  return JSON.parse(content) as { backendPid?: number };
}

export async function cleanupStateFile(): Promise<void> {
  await rm(stateFilePath, { force: true });
}

export async function waitForHttp(url: string, timeoutMs: number): Promise<void> {
  await waitFor(async () => {
    try {
      const response = await fetch(url);
      return response.ok;
    } catch {
      return false;
    }
  }, timeoutMs, `Timed out waiting for HTTP endpoint ${url}. See ${backendLogPath}.`);
}

async function waitFor(
  predicate: () => Promise<boolean>,
  timeoutMs: number,
  timeoutMessage: string,
): Promise<void> {
  const startedAt = Date.now();

  while (Date.now() - startedAt < timeoutMs) {
    if (await predicate()) {
      return;
    }

    await new Promise((resolve) => setTimeout(resolve, 1_000));
  }

  throw new Error(timeoutMessage);
}

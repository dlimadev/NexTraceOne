import {
  cleanupStateFile,
  readState,
  stopBackendProcess,
  stopPostgresContainer,
} from './realStack';

export default async function globalTeardown(): Promise<void> {
  try {
    const state = await readState();
    await stopBackendProcess(state.backendPid);
  } catch {
    // State file may be absent if setup failed before persisting state.
  } finally {
    await stopPostgresContainer();
    await cleanupStateFile();
  }
}


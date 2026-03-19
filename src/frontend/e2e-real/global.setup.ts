import {
  cleanupStateFile,
  createDatabases,
  startBackendProcess,
  startPostgresContainer,
  writeState,
} from './realStack';

export default async function globalSetup(): Promise<void> {
  await cleanupStateFile();
  await startPostgresContainer();
  await createDatabases();
  const backendPid = await startBackendProcess();
  await writeState({ backendPid });
}


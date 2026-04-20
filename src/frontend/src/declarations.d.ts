// Module declarations for packages without TypeScript type definitions.
declare module '@fontsource-variable/instrument-sans';

// Augments Vite's ImportMeta.env with project-specific environment variables
// so consumers can read `import.meta.env.VITE_*` without unsafe type assertions.
interface ImportMetaEnv {
  readonly VITE_DOCS_BASE_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

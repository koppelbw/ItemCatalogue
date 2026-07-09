/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Absolute API origin baked in at build time; unset locally so the dev proxy serves /api. */
  readonly VITE_API_BASE_URL?: string;
  /** "true" forces demo-only mode: the app never contacts the API. */
  readonly VITE_FORCE_DEMO?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

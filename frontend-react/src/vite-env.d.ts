/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_DOCSERVER_URL?: string;
  readonly VITE_API_FOR_DOCSERVER?: string;
  readonly VITE_OO_PLUGIN_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

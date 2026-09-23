export interface RuntimeConfig {
  readonly hubUrl: string;
  readonly mlApiUrl: string;
}

export const DEFAULT_EXCHANGE_HUB_URL = 'http://localhost:5000/hubs/exchange';
export const DEFAULT_ML_API_URL = 'http://localhost:5005/api/v1/';

const DEFAULT_RUNTIME_CONFIG: RuntimeConfig = {
  hubUrl: DEFAULT_EXCHANGE_HUB_URL,
  mlApiUrl: DEFAULT_ML_API_URL,
};

export async function loadRuntimeConfig(
  fetcher: typeof fetch = globalThis.fetch.bind(globalThis),
): Promise<RuntimeConfig> {
  try {
    const response = await fetcher('/runtime-config.json', { cache: 'no-store' });
    if (!response.ok) {
      return DEFAULT_RUNTIME_CONFIG;
    }

    const payload: unknown = await response.json();
    return normalizeRuntimeConfig(payload);
  } catch {
    return DEFAULT_RUNTIME_CONFIG;
  }
}

export function normalizeRuntimeConfig(payload: unknown): RuntimeConfig {
  if (!isRecord(payload)) {
    return DEFAULT_RUNTIME_CONFIG;
  }

  const hubUrl = normalizePublicHttpUrl(payload['hubUrl'], DEFAULT_EXCHANGE_HUB_URL);
  const mlApiUrl = ensureTrailingSlash(
    normalizePublicHttpUrl(payload['mlApiUrl'], DEFAULT_ML_API_URL),
  );

  return { hubUrl, mlApiUrl };
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function normalizePublicHttpUrl(value: unknown, fallback: string): string {
  if (typeof value !== 'string') {
    return fallback;
  }

  const normalized = value.trim();
  return isSupportedPublicHttpUrl(normalized) ? normalized : fallback;
}

function isSupportedPublicHttpUrl(value: string): boolean {
  if (value.startsWith('/')) {
    return !value.startsWith('//');
  }

  try {
    const url = new URL(value);
    return url.protocol === 'http:' || url.protocol === 'https:';
  } catch {
    return false;
  }
}

function ensureTrailingSlash(value: string): string {
  return value.endsWith('/') ? value : `${value}/`;
}

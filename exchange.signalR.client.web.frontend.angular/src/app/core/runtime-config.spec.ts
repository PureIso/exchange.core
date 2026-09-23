import {
  DEFAULT_EXCHANGE_HUB_URL,
  DEFAULT_ML_API_URL,
  loadRuntimeConfig,
  normalizeRuntimeConfig,
} from './runtime-config';

describe('runtime configuration', () => {
  it('loads an absolute public hub URL without caching it', async () => {
    const fetcher = vi.fn(() =>
      Promise.resolve(
        new Response(
          JSON.stringify({
            hubUrl: 'https://exchange.example/hubs/exchange',
            mlApiUrl: 'https://ml.example/api/v1',
          }),
          {
            status: 200,
            headers: { 'Content-Type': 'application/json' },
          },
        ),
      ),
    );

    await expect(loadRuntimeConfig(fetcher)).resolves.toEqual({
      hubUrl: 'https://exchange.example/hubs/exchange',
      mlApiUrl: 'https://ml.example/api/v1/',
    });
    expect(fetcher).toHaveBeenCalledWith('/runtime-config.json', { cache: 'no-store' });
  });

  it('accepts a same-origin hub path', () => {
    expect(normalizeRuntimeConfig({ hubUrl: ' /hubs/exchange ' })).toEqual({
      hubUrl: '/hubs/exchange',
      mlApiUrl: DEFAULT_ML_API_URL,
    });
  });

  it.each([
    undefined,
    {},
    { hubUrl: '' },
    { hubUrl: '//unexpected.example/hubs/exchange' },
    { hubUrl: 'javascript:alert(1)' },
  ])('uses the safe development default for invalid input %#', (payload) => {
    expect(normalizeRuntimeConfig(payload)).toEqual({
      hubUrl: DEFAULT_EXCHANGE_HUB_URL,
      mlApiUrl: DEFAULT_ML_API_URL,
    });
  });

  it('uses the development default when the runtime file is unavailable', async () => {
    const fetcher = vi.fn(() => Promise.reject(new TypeError('network unavailable')));

    await expect(loadRuntimeConfig(fetcher)).resolves.toEqual({
      hubUrl: DEFAULT_EXCHANGE_HUB_URL,
      mlApiUrl: DEFAULT_ML_API_URL,
    });
  });

  it('accepts a same-origin ML API path and rejects executable schemes independently', () => {
    expect(
      normalizeRuntimeConfig({
        hubUrl: 'javascript:alert(1)',
        mlApiUrl: ' /api/v1 ',
      }),
    ).toEqual({
      hubUrl: DEFAULT_EXCHANGE_HUB_URL,
      mlApiUrl: '/api/v1/',
    });
  });
});

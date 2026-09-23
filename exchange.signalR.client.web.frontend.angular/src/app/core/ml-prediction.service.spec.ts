import { TestBed } from '@angular/core/testing';
import { ML_API_URL, ML_FETCH, MlPredictionService } from './ml-prediction.service';

describe('MlPredictionService', () => {
  const fetcher = vi.fn<typeof fetch>();
  let service: MlPredictionService;

  beforeEach(() => {
    fetcher.mockReset();
    TestBed.configureTestingModule({
      providers: [
        MlPredictionService,
        { provide: ML_API_URL, useValue: '/ml-api/' },
        { provide: ML_FETCH, useValue: fetcher },
      ],
    });
    service = TestBed.inject(MlPredictionService);
  });

  it('loads, deduplicates, and sorts indicator filenames', async () => {
    fetcher.mockResolvedValueOnce(
      jsonResponse({ api_version: '1.0.0', indicator_files: ['eth.csv', 'btc.csv', 'btc.csv'] }),
    );

    await expect(service.getIndicators()).resolves.toEqual(['btc.csv', 'eth.csv']);
    expect(fetcher).toHaveBeenCalledWith(
      '/ml-api/rnn/',
      expect.objectContaining({ method: 'GET' }),
    );
  });

  it('dispatches only the prediction endpoint', async () => {
    fetcher.mockResolvedValueOnce(
      jsonResponse({ task_id: 'prediction-1', status: 'PENDING' }, 202),
    );

    await expect(service.startPrediction('btc.csv')).resolves.toEqual({
      taskId: 'prediction-1',
      state: 'PENDING',
    });
    expect(fetcher).toHaveBeenCalledWith(
      '/ml-api/predict/',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ indicator_file: 'btc.csv' }),
      }),
    );
  });

  it('normalizes successful task output for charting', async () => {
    fetcher.mockResolvedValueOnce(
      jsonResponse({
        state: 'SUCCESS',
        status: {
          current_progress: 100,
          status: 'Task completed!',
          close_price: { name: 'close_price', series: [{ name: '2026-01-01', value: 101 }] },
          close_price_prediction: {
            name: 'close_price_prediction',
            series: [{ name: '2026-01-02', value: '102.5' }],
          },
        },
      }),
    );

    await expect(service.getTaskResult('prediction-1')).resolves.toEqual({
      state: 'SUCCESS',
      progress: 100,
      message: 'Task completed!',
      actual: { name: 'close_price', series: [{ name: '2026-01-01', value: 101 }] },
      predicted: {
        name: 'close_price_prediction',
        series: [{ name: '2026-01-02', value: 102.5 }],
      },
    });
  });

  it('returns a safe error without exposing an upstream response body', async () => {
    fetcher.mockResolvedValueOnce(new Response('secret traceback', { status: 500 }));

    await expect(service.getIndicators()).rejects.toThrow('request failed (500)');
  });
});

function jsonResponse(payload: unknown, status = 200): Response {
  return new Response(JSON.stringify(payload), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

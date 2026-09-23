import { inject, Injectable, InjectionToken } from '@angular/core';
import { DEFAULT_ML_API_URL } from './runtime-config';

export interface PredictionPoint {
  readonly name: string;
  readonly value: number;
}

export interface PredictionSeries {
  readonly name: string;
  readonly series: readonly PredictionPoint[];
}

export interface PredictionTask {
  readonly taskId: string;
  readonly state: string;
}

export interface PredictionTaskResult {
  readonly state: string;
  readonly progress: number | null;
  readonly message: string | null;
  readonly actual: PredictionSeries | null;
  readonly predicted: PredictionSeries | null;
}

export const ML_API_URL = new InjectionToken<string>('ML_API_URL', {
  providedIn: 'root',
  factory: () => DEFAULT_ML_API_URL,
});

export const ML_FETCH = new InjectionToken<typeof fetch>('ML_FETCH', {
  providedIn: 'root',
  factory: () => globalThis.fetch.bind(globalThis),
});

@Injectable({ providedIn: 'root' })
export class MlPredictionService {
  private readonly apiUrl = inject(ML_API_URL);
  private readonly fetcher = inject(ML_FETCH);

  async getIndicators(): Promise<readonly string[]> {
    const payload = await this.request('rnn/', { method: 'GET' });
    if (!isRecord(payload) || !Array.isArray(payload['indicator_files'])) {
      throw new Error('The machine-learning service returned an invalid indicator list.');
    }

    return [...new Set(payload['indicator_files'].filter(isNonEmptyString))].sort((left, right) =>
      left.localeCompare(right),
    );
  }

  async startPrediction(indicatorFile: string): Promise<PredictionTask> {
    if (!indicatorFile.trim()) {
      throw new Error('Select an indicator before running a prediction.');
    }

    const payload = await this.request('predict/', {
      method: 'POST',
      body: JSON.stringify({ indicator_file: indicatorFile }),
    });
    if (
      !isRecord(payload) ||
      !isNonEmptyString(payload['task_id']) ||
      !isNonEmptyString(payload['status'])
    ) {
      throw new Error('The machine-learning service returned an invalid prediction task.');
    }

    return { taskId: payload['task_id'], state: payload['status'] };
  }

  async getTaskResult(taskId: string): Promise<PredictionTaskResult> {
    if (!taskId.trim()) {
      throw new Error('A prediction task ID is required.');
    }

    const payload = await this.request('taskstatus/', {
      method: 'POST',
      body: JSON.stringify({ task_id: taskId }),
    });
    if (!isRecord(payload) || !isNonEmptyString(payload['state'])) {
      throw new Error('The machine-learning service returned an invalid task status.');
    }

    const status = isRecord(payload['status']) ? payload['status'] : {};
    return {
      state: payload['state'],
      progress: finiteNumber(status['current_progress']),
      message: isNonEmptyString(status['status']) ? status['status'] : null,
      actual: predictionSeries(status['close_price']),
      predicted: predictionSeries(status['close_price_prediction']),
    };
  }

  private async request(path: string, init: RequestInit): Promise<unknown> {
    let response: Response;
    try {
      response = await this.fetcher(`${this.apiUrl}${path}`, {
        ...init,
        headers: { Accept: 'application/json', 'Content-Type': 'application/json' },
      });
    } catch {
      throw new Error('The machine-learning service is unavailable.');
    }

    if (!response.ok) {
      throw new Error(`The machine-learning request failed (${response.status}).`);
    }

    try {
      return await response.json();
    } catch {
      throw new Error('The machine-learning service returned invalid JSON.');
    }
  }
}

function predictionSeries(value: unknown): PredictionSeries | null {
  if (!isRecord(value) || !isNonEmptyString(value['name']) || !Array.isArray(value['series'])) {
    return null;
  }

  const series = value['series']
    .map((point): PredictionPoint | null => {
      if (!isRecord(point)) {
        return null;
      }
      const numericValue = finiteNumber(point['value']);
      if (
        (typeof point['name'] !== 'string' && typeof point['name'] !== 'number') ||
        numericValue === null
      ) {
        return null;
      }
      return { name: String(point['name']), value: numericValue };
    })
    .filter((point): point is PredictionPoint => point !== null);

  return series.length > 0 ? { name: value['name'], series } : null;
}

function finiteNumber(value: unknown): number | null {
  if (typeof value !== 'number' && (typeof value !== 'string' || value.trim() === '')) {
    return null;
  }
  const numericValue = typeof value === 'number' ? value : Number(value);
  return Number.isFinite(numericValue) ? numericValue : null;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function isNonEmptyString(value: unknown): value is string {
  return typeof value === 'string' && value.trim().length > 0;
}

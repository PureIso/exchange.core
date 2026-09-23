import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import {
  MlPredictionService,
  PredictionSeries,
  PredictionTaskResult,
} from '../../core/ml-prediction.service';

const POLL_INTERVAL_MS = 2_000;
const MAX_POLL_ATTEMPTS = 30;

@Component({
  selector: 'app-machine-learning',
  templateUrl: './machine-learning.html',
  styleUrl: './machine-learning.scss',
})
export class MachineLearning implements OnInit, OnDestroy {
  private readonly client = inject(MlPredictionService);
  private pollTimer: ReturnType<typeof setTimeout> | null = null;
  private pollAttempts = 0;

  protected readonly indicators = signal<readonly string[]>([]);
  protected readonly selectedIndicator = signal('');
  protected readonly taskId = signal<string | null>(null);
  protected readonly taskState = signal<string | null>(null);
  protected readonly taskMessage = signal<string | null>(null);
  protected readonly progress = signal<number | null>(null);
  protected readonly actual = signal<PredictionSeries | null>(null);
  protected readonly predicted = signal<PredictionSeries | null>(null);
  protected readonly loadingIndicators = signal(false);
  protected readonly dispatching = signal(false);
  protected readonly checkingTask = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly chart = computed(() => buildChart(this.actual(), this.predicted()));

  ngOnInit(): void {
    void this.loadIndicators();
  }

  ngOnDestroy(): void {
    this.cancelPolling();
  }

  protected selectIndicator(event: Event): void {
    this.selectedIndicator.set((event.target as HTMLSelectElement).value);
  }

  protected async loadIndicators(): Promise<void> {
    this.loadingIndicators.set(true);
    this.error.set(null);
    try {
      const indicators = await this.client.getIndicators();
      this.indicators.set(indicators);
      if (!indicators.includes(this.selectedIndicator())) {
        this.selectedIndicator.set(indicators[0] ?? '');
      }
    } catch (error: unknown) {
      this.error.set(describeError(error));
    } finally {
      this.loadingIndicators.set(false);
    }
  }

  protected async runPrediction(): Promise<void> {
    this.cancelPolling();
    this.pollAttempts = 0;
    this.dispatching.set(true);
    this.error.set(null);
    this.taskId.set(null);
    this.taskState.set(null);
    this.taskMessage.set(null);
    this.progress.set(null);
    this.actual.set(null);
    this.predicted.set(null);

    try {
      const task = await this.client.startPrediction(this.selectedIndicator());
      this.taskId.set(task.taskId);
      this.taskState.set(task.state);
      await this.refreshTask();
    } catch (error: unknown) {
      this.error.set(describeError(error));
    } finally {
      this.dispatching.set(false);
    }
  }

  protected async refreshTask(): Promise<void> {
    const taskId = this.taskId();
    if (!taskId || this.checkingTask()) {
      return;
    }

    this.cancelPolling();
    this.checkingTask.set(true);
    this.error.set(null);
    try {
      const result = await this.client.getTaskResult(taskId);
      this.applyResult(result);
      if (!isTerminalState(result.state) && this.pollAttempts < MAX_POLL_ATTEMPTS) {
        this.pollAttempts += 1;
        this.pollTimer = setTimeout(() => void this.refreshTask(), POLL_INTERVAL_MS);
      }
    } catch (error: unknown) {
      this.error.set(describeError(error));
    } finally {
      this.checkingTask.set(false);
    }
  }

  private applyResult(result: PredictionTaskResult): void {
    this.taskState.set(result.state);
    this.taskMessage.set(result.message);
    this.progress.set(result.progress);
    this.actual.set(result.actual);
    this.predicted.set(result.predicted);
  }

  private cancelPolling(): void {
    if (this.pollTimer !== null) {
      clearTimeout(this.pollTimer);
      this.pollTimer = null;
    }
  }
}

interface ChartLine {
  readonly name: string;
  readonly points: string;
  readonly color: string;
}

interface PredictionChart {
  readonly lines: readonly ChartLine[];
  readonly minimum: number;
  readonly maximum: number;
}

function buildChart(
  actual: PredictionSeries | null,
  predicted: PredictionSeries | null,
): PredictionChart | null {
  const available = [actual, predicted].filter(
    (series): series is PredictionSeries => series !== null,
  );
  const values = available.flatMap((series) => series.series.map((point) => point.value));
  if (values.length === 0) {
    return null;
  }

  const minimum = Math.min(...values);
  const maximum = Math.max(...values);
  const range = maximum - minimum || 1;
  const lines = available.map((series, index) => ({
    name: series.name,
    color: index === 0 ? '#08734a' : '#d55b2a',
    points: series.series
      .map((point, pointIndex) => {
        const denominator = Math.max(series.series.length - 1, 1);
        const x = 40 + (pointIndex / denominator) * 640;
        const y = 250 - ((point.value - minimum) / range) * 210;
        return `${x.toFixed(1)},${y.toFixed(1)}`;
      })
      .join(' '),
  }));

  return { lines, minimum, maximum };
}

function isTerminalState(state: string): boolean {
  return ['SUCCESS', 'FAILURE', 'REVOKED'].includes(state.toUpperCase());
}

function describeError(error: unknown): string {
  return error instanceof Error ? error.message : 'The machine-learning request failed.';
}

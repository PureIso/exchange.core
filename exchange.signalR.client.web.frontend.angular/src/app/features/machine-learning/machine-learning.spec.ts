import { TestBed } from '@angular/core/testing';
import { MlPredictionService, PredictionTaskResult } from '../../core/ml-prediction.service';
import { MachineLearning } from './machine-learning';

describe('MachineLearning', () => {
  const client = {
    getIndicators: vi.fn(() => Promise.resolve(['btc-eur.csv', 'eth-eur.csv'])),
    startPrediction: vi.fn(() => Promise.resolve({ taskId: 'prediction-1', state: 'PENDING' })),
    getTaskResult: vi.fn<() => Promise<PredictionTaskResult>>(() =>
      Promise.resolve({
        state: 'SUCCESS',
        progress: 100,
        message: 'Task completed!',
        actual: {
          name: 'close_price',
          series: [
            { name: '2026-01-01', value: 101 },
            { name: '2026-01-02', value: 102 },
          ],
        },
        predicted: {
          name: 'close_price_prediction',
          series: [
            { name: '2026-01-01', value: 101 },
            { name: '2026-01-02', value: 103 },
          ],
        },
      }),
    ),
  };

  beforeEach(async () => {
    Object.values(client).forEach((mock) => mock.mockClear());
    await TestBed.configureTestingModule({
      imports: [MachineLearning],
      providers: [{ provide: MlPredictionService, useValue: client }],
    }).compileComponents();
  });

  it('loads indicator choices without exposing training controls', async () => {
    const fixture = TestBed.createComponent(MachineLearning);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const options = fixture.nativeElement.querySelectorAll(
      'option',
    ) as NodeListOf<HTMLOptionElement>;
    expect([...options].map((option) => option.value)).toEqual(['btc-eur.csv', 'eth-eur.csv']);
    expect(fixture.nativeElement.textContent).not.toContain('Run Training');
  });

  it('dispatches a prediction, refreshes its task, and renders both chart lines', async () => {
    const fixture = TestBed.createComponent(MachineLearning);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    (
      fixture.nativeElement.querySelector('[data-testid="predict-button"]') as HTMLButtonElement
    ).click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(client.startPrediction).toHaveBeenCalledWith('btc-eur.csv');
    expect(client.getTaskResult).toHaveBeenCalledWith('prediction-1');
    expect(
      fixture.nativeElement.querySelector('[data-testid="prediction-status"]').textContent,
    ).toContain('SUCCESS');
    expect(fixture.nativeElement.querySelectorAll('polyline')).toHaveLength(2);
  });

  it('renders a safe service failure', async () => {
    client.getIndicators.mockRejectedValueOnce(
      new Error('The machine-learning service is unavailable.'),
    );
    const fixture = TestBed.createComponent(MachineLearning);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain(
      'The machine-learning service is unavailable.',
    );
  });
});

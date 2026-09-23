import { expect, test } from '@playwright/test';
import { SignalRHubFixture } from './signalr-hub.fixture';

test.beforeEach(async ({ page }) => {
  await page.route('**/runtime-config.json', (route) =>
    route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify({ hubUrl: '/hubs/exchange', mlApiUrl: '/ml-api/' }),
    }),
  );
});

test('redirects to the dashboard and exposes a recoverable connection failure', async ({
  page,
}) => {
  await page.route('**/hubs/exchange/**', (route) => route.abort('connectionfailed'));
  await page.goto('/');

  await expect(page).toHaveURL(/\/dashboard$/);
  await expect(page.getByRole('heading', { name: 'Market workspace' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Dashboard' })).toHaveClass(/active/);
  await expect(page.getByTestId('feed-control')).toContainText('subscription stopped');
  await expect(page.getByRole('alert')).toContainText('Exchange request unavailable');
  await expect(page.getByRole('button', { name: 'Retry' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Apply feed selection' })).toBeDisabled();
});

test('navigates between lazy-loaded dashboard and history routes', async ({ page }) => {
  await page.route('**/hubs/exchange/**', (route) => route.abort('connectionfailed'));
  await page.goto('/dashboard');

  await page.getByRole('link', { name: 'History & statistics' }).click();
  await expect(page).toHaveURL(/\/history$/);
  await expect(page.getByRole('link', { name: 'History & statistics' })).toHaveClass(/active/);
  await expect(page.getByRole('heading', { name: 'Orders and fills' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Load history' })).toBeDisabled();
  await expect(page.getByRole('button', { name: 'Load statistics' })).toBeDisabled();

  await page.getByRole('link', { name: 'Dashboard' }).click();
  await expect(page).toHaveURL(/\/dashboard$/);
  await expect(page.getByTestId('current-prices')).toBeVisible();
});

test('runs the prediction-only machine-learning workflow with a deterministic API', async ({
  page,
}) => {
  await page.route('**/hubs/exchange/**', (route) => route.abort('connectionfailed'));
  await page.route('**/ml-api/rnn/', (route) =>
    route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify({ api_version: '1.0.0', indicator_files: ['btc-eur.csv'] }),
    }),
  );
  await page.route('**/ml-api/predict/', async (route) => {
    expect(route.request().postDataJSON()).toEqual({ indicator_file: 'btc-eur.csv' });
    await route.fulfill({
      status: 202,
      contentType: 'application/json',
      body: JSON.stringify({ task_id: 'prediction-1', status: 'PENDING' }),
    });
  });
  await page.route('**/ml-api/taskstatus/', async (route) => {
    expect(route.request().postDataJSON()).toEqual({ task_id: 'prediction-1' });
    await route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify({
        state: 'SUCCESS',
        status: {
          current_progress: 100,
          status: 'Task completed!',
          close_price: {
            name: 'close_price',
            series: [
              { name: '2026-08-15', value: 65000 },
              { name: '2026-08-16', value: 65200 },
            ],
          },
          close_price_prediction: {
            name: 'close_price_prediction',
            series: [
              { name: '2026-08-15', value: 65000 },
              { name: '2026-08-16', value: 65400 },
            ],
          },
        },
      }),
    });
  });

  await page.goto('/machine-learning');
  await expect(page.getByRole('link', { name: 'Machine learning' })).toHaveClass(/active/);
  await expect(page.getByTestId('indicator-file')).toHaveValue('btc-eur.csv');
  await expect(page.getByText('Model training is not exposed')).toBeVisible();

  await page.getByTestId('predict-button').click();

  await expect(page.getByTestId('prediction-status')).toContainText('SUCCESS');
  await expect(page.getByTestId('prediction-status')).toContainText('prediction-1');
  await expect(
    page.getByRole('img', { name: 'Actual and predicted close-price chart' }),
  ).toBeVisible();
  await expect(page.locator('polyline')).toHaveCount(2);
  await expect(page.getByRole('button', { name: 'Run Training' })).toHaveCount(0);
});

test('delivers SignalR state and refreshes it after an automatic reconnect', async ({ page }) => {
  const hub = new SignalRHubFixture();
  await hub.register(page);
  await page.goto('/dashboard');

  await expect(page.locator('.status')).toHaveText('connected');
  await expect(page.locator('#exchange option')).toHaveCount(2);
  await page.locator('#exchange').selectOption('Coinbase');
  await expect(page.getByTestId('current-prices')).toContainText('65000.25');
  await expect(page.getByTestId('account-balances')).toContainText('1.25');
  await expect(page.getByRole('cell', { name: 'BTC-EUR', exact: true })).toBeVisible();

  await page.getByRole('checkbox', { name: 'Subscribe to BTC-EUR' }).check();
  await page.getByTestId('subscribe-button').click();
  await expect
    .poll(() => hub.hasInvocation('RequestedSubscription', ['Coinbase', ['BTC-EUR']]))
    .toBe(true);
  await expect(page.getByTestId('feed-control')).toContainText('subscription running');
  await expect(page.getByTestId('asset-information')).toContainText('64999');

  await page.getByRole('link', { name: 'History & statistics' }).click();
  await page.getByTestId('history-button').click();
  await expect(page.getByTestId('order-history')).toContainText('order-1');
  await expect(page.getByTestId('fill-history')).toContainText(
    '8b8ce449-b211-4ade-9349-f86a928b28c8',
  );
  await page.getByTestId('statistics-button').click();
  await expect(page.getByTestId('fill-statistics')).toContainText('66000');

  await page.getByRole('link', { name: 'Dashboard' }).click();
  hub.pauseNextHandshake();
  await hub.disconnectLatest();
  await expect(page.locator('.status')).toHaveText('reconnecting');
  await expect.poll(() => hub.connectionCount).toBe(2);
  hub.resumeHandshake();

  await expect(page.locator('.status')).toHaveText('connected');
  await expect.poll(() => hub.hasInvocation('RequestedApplications')).toBe(true);
  await expect(page.getByTestId('current-prices')).toContainText('66000.5');
});

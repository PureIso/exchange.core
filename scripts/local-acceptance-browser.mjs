import { chromium } from '../exchange.signalR.client.web.frontend.angular/node_modules/@playwright/test/index.mjs';

const gatewayUrl = process.env.LOCAL_ACCEPTANCE_GATEWAY_URL ?? 'http://127.0.0.1:8088';
const productId = process.env.LOCAL_ACCEPTANCE_PRODUCT_ID ?? 'BTC-USD';
const indicatorFile = process.env.LOCAL_ACCEPTANCE_INDICATOR_FILE ?? 'coinbase-btc-usd.csv';
const browser = await chromium.launch({ headless: true });
try {
  const page = await browser.newPage();
  const negotiate = await page.request.post(
    `${gatewayUrl}/backend/hubs/exchange/negotiate?negotiateVersion=1`,
  );
  if (!negotiate.ok()) {
    throw new Error(`SignalR negotiate returned HTTP ${negotiate.status()}.`);
  }

  await page.goto(`${gatewayUrl}/dashboard`, { waitUntil: 'domcontentloaded' });
  const exchange = page.locator('#exchange');
  await exchange.waitFor({ state: 'visible', timeout: 20_000 });
  const advancedTradeOption = exchange.locator('option', {
    hasText: 'Coinbase Advanced Trade',
  });
  await advancedTradeOption.waitFor({ state: 'attached', timeout: 20_000 });
  if ((await advancedTradeOption.count()) !== 1) {
    throw new Error('Angular exchange selector did not render Coinbase Advanced Trade.');
  }
  await exchange.selectOption({ label: 'Coinbase Advanced Trade' });
  const productCheckbox = page.getByRole('checkbox', { name: `Subscribe to ${productId}`, exact: true });
  await productCheckbox.waitFor({ state: 'visible', timeout: 20_000 });
  await productCheckbox.check();
  await page.getByTestId('subscribe-button').click();
  await page.getByTestId('feed-control').getByText('subscription running').waitFor({
    timeout: 20_000,
  });
  await page
    .getByTestId('current-prices')
    .locator('dt', { hasText: productId })
    .waitFor({ timeout: 30_000 });

  await page.goto(`${gatewayUrl}/machine-learning`, { waitUntil: 'domcontentloaded' });
  const indicator = page.getByTestId('indicator-file');
  await indicator.waitFor({ state: 'visible', timeout: 20_000 });
  await indicator.selectOption(indicatorFile);
  if ((await indicator.inputValue()) !== indicatorFile) {
    throw new Error('Angular did not select the captured Coinbase candle CSV.');
  }
  await page.getByTestId('predict-button').click();
  await page.getByTestId('prediction-status').getByText('SUCCESS').waitFor({ timeout: 60_000 });
  await page.getByRole('img', { name: 'Actual and predicted close-price chart' }).waitFor({
    timeout: 20_000,
  });
  const lineCount = await page.locator('polyline').count();
  if (lineCount < 2) {
    throw new Error(`Angular prediction chart rendered ${lineCount} line(s), expected two.`);
  }
  const chartPoints = await page.locator('polyline').evaluateAll((lines) =>
    lines.map((line) => line.getAttribute('points') ?? ''),
  );
  if (chartPoints.some((points) => points.trim().length === 0)) {
    throw new Error('Angular prediction chart rendered an empty series.');
  }
  console.log('Angular SignalR selection and replayed prediction chart checks passed.');
} finally {
  await browser.close();
}

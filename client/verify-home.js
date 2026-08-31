const { chromium } = require('playwright');

const BASE = 'http://localhost:8090';

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  const errors = [];
  page.on('pageerror', (e) => errors.push(`pageerror: ${e.message}`));
  page.on('console', (msg) => {
    if (msg.type() === 'error') errors.push(`console.error: ${msg.text()}`);
  });

  console.log('=== Home page, Requests menu currently active — widget should show ===');
  await page.goto(`${BASE}/`, { waitUntil: 'networkidle' });
  await page.waitForSelector('text=Pending Requisitions by Department', { timeout: 10000 });
  const rowCount = await page.locator('table tbody tr').count();
  console.log(`Widget shown with ${rowCount} department rows.`);
  const firstRowText = await page.locator('table tbody tr').first().textContent();
  console.log('Top department row:', firstRowText);

  console.log('=== Toggle Requests menu item off in Menu Admin ===');
  await page.goto(`${BASE}/menu-admin`, { waitUntil: 'networkidle' });
  await page.waitForSelector('table tbody tr', { timeout: 10000 });
  const requestsRow = page.locator('tr', { hasText: 'Requests' }).filter({ hasText: '/requests' });
  const toggleSwitch = requestsRow.locator('input[type="checkbox"]');
  await toggleSwitch.waitFor({ timeout: 10000 });
  const wasChecked = await toggleSwitch.isChecked();
  console.log('Requests menu item active before toggle:', wasChecked);
  await toggleSwitch.click();
  await page.waitForTimeout(500);

  console.log('=== Home page again — widget should now be hidden, info message shown ===');
  await page.goto(`${BASE}/`, { waitUntil: 'networkidle' });
  await page.waitForSelector('text=Toggle it on in Menu Admin', { timeout: 10000 });
  const widgetGone = (await page.locator('text=Pending Requisitions by Department').count()) === 0;
  console.log('Widget hidden after toggling menu item off:', widgetGone);

  console.log('=== Toggle Requests menu item back on (cleanup) ===');
  await page.goto(`${BASE}/menu-admin`, { waitUntil: 'networkidle' });
  await page.waitForSelector('table tbody tr', { timeout: 10000 });
  const requestsRow2 = page.locator('tr', { hasText: 'Requests' }).filter({ hasText: '/requests' });
  const toggleSwitch2 = requestsRow2.locator('input[type="checkbox"]');
  await toggleSwitch2.waitFor({ timeout: 10000 });
  await toggleSwitch2.click();
  await page.waitForTimeout(500);

  console.log('=== Home page once more — widget should be back ===');
  await page.goto(`${BASE}/`, { waitUntil: 'networkidle' });
  await page.waitForSelector('text=Pending Requisitions by Department', { timeout: 10000 });
  console.log('Widget restored after toggling menu item back on.');

  console.log('=== Console/page errors ===');
  console.log(errors.length ? errors : 'none');
  if (errors.length) process.exitCode = 1;

  await browser.close();
})().catch((e) => {
  console.error('VERIFY FAILED:', e);
  process.exit(1);
});

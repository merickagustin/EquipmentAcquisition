const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  const errors = [];
  page.on('pageerror', (e) => errors.push(`pageerror: ${e.message}`));
  page.on('console', (msg) => {
    if (msg.type() === 'error') errors.push(`console.error: ${msg.text()}`);
  });

  console.log('=== Navigating to /menu-admin ===');
  await page.goto('http://localhost:5248/menu-admin', { waitUntil: 'networkidle' });
  await page.waitForSelector('text=Menu Admin', { timeout: 10000 });

  const rowTexts = await page.$$eval('table tbody tr', (rows) =>
    rows.map((r) => r.querySelector('td')?.textContent?.trim()),
  );
  console.log('Table rows (Label column):', rowTexts);

  console.log('=== Checking nav sidebar (same layout on this page) ===');
  const navText = await page.$eval('#nav-root', (el) => el.textContent);
  console.log('Nav sidebar text:', navText);

  console.log('=== Testing Create flow ===');
  await page.getByRole('button', { name: 'New Menu Item' }).click();
  await page.getByLabel('Label').waitFor({ timeout: 10000 });
  await page.getByLabel('Label').fill('Playwright Test Item');
  await page.getByLabel('Display Order').fill('99');
  await page.getByRole('button', { name: 'Save' }).click();
  await page.waitForSelector('text=Playwright Test Item', { timeout: 10000 });
  console.log('Create succeeded — "Playwright Test Item" appeared in the table.');

  console.log('=== Testing Delete flow (cleanup) ===');
  const row = page.locator('tr', { hasText: 'Playwright Test Item' });
  await row.getByRole('button').last().click(); // delete icon is the last button in the row
  await page.getByRole('button', { name: 'Delete' }).click();
  await page.waitForSelector('text=Playwright Test Item', { state: 'detached', timeout: 10000 });
  console.log('Delete succeeded — row removed.');

  await page.screenshot({ path: 'verify-screenshot.png', fullPage: true });
  console.log('Screenshot saved to client/verify-screenshot.png');

  console.log('=== Browser console/page errors ===');
  console.log(errors.length ? errors : 'none');

  await browser.close();
})().catch((e) => {
  console.error('VERIFY FAILED:', e);
  process.exit(1);
});

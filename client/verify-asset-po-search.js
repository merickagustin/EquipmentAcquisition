const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  const errors = [];
  page.on('pageerror', (e) => errors.push(`pageerror: ${e.message}`));
  page.on('console', (msg) => {
    if (msg.type() === 'error') errors.push(`console.error: ${msg.text()}`);
  });

  await page.goto('http://localhost:8090/assets', { waitUntil: 'networkidle' });
  await page.waitForSelector('table tbody tr', { timeout: 10000 });

  console.log('=== Open New Asset, search for a PO by number fragment ===');
  await page.getByRole('button', { name: 'New Asset' }).click();
  const poField = page.getByRole('dialog').getByLabel('Purchase Order');
  await poField.waitFor({ timeout: 10000 });
  await poField.fill('PO-2026-0001');
  await page.waitForTimeout(600); // past the 350ms debounce
  await page.waitForSelector('li[role="option"]', { timeout: 10000 });
  const optionCount = await page.locator('li[role="option"]').count();
  console.log(`Options returned for "PO-2026-0001": ${optionCount}`);
  const firstOptionText = await page.locator('li[role="option"]').first().textContent();
  console.log('First option text:', firstOptionText);
  await page.locator('li[role="option"]').first().click();

  console.log('=== Fill the rest and create the asset ===');
  await page.getByLabel('Asset Tag').fill('PLAYWRIGHT-PO-SEARCH-001');
  await page.getByRole('dialog').getByLabel('Department').click();
  await page.getByRole('option').first().click();
  await page.getByRole('button', { name: 'Save' }).click();
  await page.waitForSelector('text=PLAYWRIGHT-PO-SEARCH-001', { timeout: 10000 });
  console.log('Create succeeded.');

  console.log('=== Clean up ===');
  const row = page.locator('tr', { hasText: 'PLAYWRIGHT-PO-SEARCH-001' });
  await row.getByRole('button').last().click();
  await page.getByRole('button', { name: 'Delete' }).click();
  await page.waitForSelector('text=PLAYWRIGHT-PO-SEARCH-001', { state: 'detached', timeout: 10000 });
  console.log('Delete succeeded.');

  console.log('=== Console/page errors ===');
  console.log(errors.length ? errors : 'none');
  if (errors.length) process.exitCode = 1;

  await browser.close();
})().catch((e) => {
  console.error('VERIFY FAILED:', e);
  process.exit(1);
});

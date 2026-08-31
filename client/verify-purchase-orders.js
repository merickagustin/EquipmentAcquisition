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

  console.log('=== Navigating to /purchase-orders ===');
  await page.goto(`${BASE}/purchase-orders`, { waitUntil: 'networkidle' });
  await page.waitForSelector('text=Purchase Orders', { timeout: 10000 });
  await page.waitForSelector('table tbody tr', { timeout: 10000 });
  const rowCount = await page.locator('table tbody tr').count();
  console.log(`Rows loaded: ${rowCount}`);

  console.log('=== Filter by an existing PO (id=1) via Acquisition Request Id ===');
  await page.getByLabel('Acquisition Request Id').first().fill('1');
  await page.waitForTimeout(500);
  const filteredRows = await page.locator('table tbody tr').count();
  console.log(`Rows after filtering to request #1: ${filteredRows}`);
  await page.getByLabel('Acquisition Request Id').first().fill('');
  await page.waitForTimeout(500);

  console.log('=== Create and approve a fresh request to attach a PO to ===');
  // Every seeded Approved request already has a PO (the seeder gives one to each) — a
  // fresh request is the only reliable way to get an Approved-but-PO-less target.
  const target = await page.evaluate(async () => {
    const base = window.__API_BASE_URL__;
    const created = await (
      await fetch(`${base}/api/acquisition-requests`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          departmentId: 1,
          equipmentCategoryId: 1,
          requestedByEmployeeId: 1,
          itemDescription: 'Playwright PO target',
          justification: null,
          quantity: 1,
          estimatedCost: 500,
        }),
      })
    ).json();
    await fetch(`${base}/api/acquisition-requests/${created.id}/approve`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ approvedByEmployeeId: 1 }),
    });
    return created.id;
  });
  console.log('Created and approved request:', target);

  console.log('=== Create a purchase order against it, via the request picker ===');
  await page.getByRole('button', { name: 'New Purchase Order' }).click();
  const requestPicker = page.getByRole('dialog').getByLabel('Acquisition Request');
  await requestPicker.waitFor({ timeout: 10000 });
  await requestPicker.click();
  const targetOption = page.getByRole('option', { name: new RegExp(`^#${target} `) });
  await targetOption.waitFor({ timeout: 10000 });
  const optionText = await targetOption.textContent();
  console.log('Picker option text:', optionText);
  await targetOption.click();
  console.log('Selected the fresh request from the dropdown.');
  await page.getByLabel('PO Number').fill('PO-PLAYWRIGHT-STANDALONE-001');
  await page.getByRole('dialog').getByLabel('Vendor').click();
  await page.getByRole('option').first().click();
  await page.getByLabel('Unit Cost').fill('99.50');
  await page.getByRole('button', { name: 'Save' }).click();
  await page.waitForSelector('text=PO-PLAYWRIGHT-STANDALONE-001', { timeout: 10000 });
  console.log('Create succeeded.');

  console.log('=== Edit it ===');
  const row = page.locator('tr', { hasText: 'PO-PLAYWRIGHT-STANDALONE-001' });
  await row.getByRole('button').first().click();
  const unitCostField = page.getByRole('dialog').getByLabel('Unit Cost');
  await unitCostField.waitFor({ timeout: 10000 });
  await unitCostField.fill('150');
  await page.getByRole('button', { name: 'Save' }).click();
  await page.waitForTimeout(1000);
  const totalCostCell = await row.locator('td').nth(5).textContent();
  console.log('Total cost after edit:', totalCostCell);

  console.log('=== Delete it ===');
  await row.getByRole('button').last().click();
  await page.getByRole('button', { name: 'Delete' }).click();
  await page.waitForSelector('text=PO-PLAYWRIGHT-STANDALONE-001', { state: 'detached', timeout: 10000 });
  console.log('Delete succeeded.');

  console.log('=== Clean up the test request ===');
  await page.evaluate(async (id) => {
    const base = window.__API_BASE_URL__;
    await fetch(`${base}/api/acquisition-requests/${id}`, { method: 'DELETE' });
  }, target);
  console.log('Request cleanup done.');

  console.log('=== Console/page errors ===');
  console.log(errors.length ? errors : 'none');
  if (errors.length) process.exitCode = 1;

  await browser.close();
})().catch((e) => {
  console.error('VERIFY FAILED:', e);
  process.exit(1);
});

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

  console.log('=== Navigating to /requests ===');
  await page.goto(`${BASE}/requests`, { waitUntil: 'networkidle' });
  await page.waitForSelector('text=Acquisition Requests', { timeout: 10000 });
  await page.waitForSelector('table tbody tr', { timeout: 10000 });
  const rowCount = await page.locator('table tbody tr').count();
  console.log(`Pending rows loaded for default department: ${rowCount}`);

  console.log('=== Create a new request ===');
  await page.getByRole('button', { name: 'New Request' }).click();
  await page.getByLabel('Item Description').waitFor({ timeout: 10000 });
  await page.getByLabel('Equipment Category').click();
  await page.getByRole('option').first().click();
  await page.getByLabel('Requested By').click();
  await page.getByRole('option').first().click();
  await page.getByLabel('Item Description').fill('Playwright Test Laptop');
  await page.getByLabel('Quantity').fill('2');
  await page.getByLabel('Estimated Cost').fill('1500');
  await page.getByRole('button', { name: 'Save' }).click();
  await page.waitForSelector('text=Playwright Test Laptop', { timeout: 10000 });
  console.log('Create succeeded — "Playwright Test Laptop" appeared in the table.');

  console.log('=== Approve it ===');
  const newRow = page.locator('tr', { hasText: 'Playwright Test Laptop' });
  await newRow.getByRole('button').nth(1).click(); // Edit, Approve, Reject, Delete -> Approve is index 1
  await page.getByLabel('Approved By').waitFor({ timeout: 10000 });
  await page.getByLabel('Approved By').click();
  await page.getByRole('option').first().click();
  await page.getByRole('button', { name: 'Approve' }).click();
  await page.waitForSelector('text=Playwright Test Laptop', { state: 'detached', timeout: 10000 });
  console.log('Approve succeeded — row left the Pending filter.');

  console.log('=== Switch filter to Approved, create a PO ===');
  await page.getByLabel('Status').click();
  await page.getByRole('option', { name: 'Approved' }).click();
  await page.waitForSelector('text=Playwright Test Laptop', { timeout: 10000 });
  const approvedRow = page.locator('tr', { hasText: 'Playwright Test Laptop' });
  await approvedRow.getByRole('button').first().click(); // shopping cart icon
  await page.getByLabel('Vendor').waitFor({ timeout: 10000 });
  await page.getByLabel('Vendor').click();
  await page.getByRole('option').first().click();
  await page.getByLabel('PO Number').fill('PO-PLAYWRIGHT-001');
  await page.getByLabel('Unit Cost').fill('750');
  await page.getByRole('button', { name: 'Save' }).click();
  await page.waitForSelector('text=PO-PLAYWRIGHT-001', { state: 'hidden', timeout: 10000 }).catch(() => {});
  console.log('PO create submitted.');
  await page.waitForTimeout(1000);
  const vendorCellText = await approvedRow.locator('td').nth(7).textContent();
  console.log('Vendor column now shows:', vendorCellText);

  console.log('=== Edit the PO, then remove it ===');
  await approvedRow.getByRole('button').first().click();
  await page.getByLabel('Unit Cost').waitFor({ timeout: 10000 });
  const unitCostValue = await page.getByLabel('Unit Cost').inputValue();
  console.log('Loaded existing PO unit cost:', unitCostValue);
  await page.getByRole('button', { name: 'Remove Purchase Order' }).click();
  await page.getByRole('button', { name: 'Delete' }).click();
  await page.waitForTimeout(1000);
  console.log('PO removed.');

  console.log('=== Clean up: delete the test request ===');
  await page.getByLabel('Status').click();
  await page.getByRole('option', { name: 'Approved' }).click();
  await page.waitForSelector('text=Playwright Test Laptop', { timeout: 10000 });
  const cleanupRow = page.locator('tr', { hasText: 'Playwright Test Laptop' });
  await cleanupRow.getByRole('button').last().click();
  await page.getByRole('button', { name: 'Delete' }).click();
  await page.waitForSelector('text=Playwright Test Laptop', { state: 'detached', timeout: 10000 });
  console.log('Cleanup delete succeeded.');

  console.log('=== Console/page errors ===');
  console.log(errors.length ? errors : 'none');
  if (errors.length) process.exitCode = 1;

  await browser.close();
})().catch((e) => {
  console.error('VERIFY FAILED:', e);
  process.exit(1);
});

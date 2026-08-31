const { chromium } = require('playwright');

const BASE = 'http://localhost:8090';

(async () => {
  const browser = await chromium.launch();
  const errors = [];

  const trackErrors = (page) => {
    page.on('pageerror', (e) => errors.push(`pageerror: ${e.message}`));
    page.on('console', (msg) => {
      if (msg.type() === 'error') errors.push(`console.error: ${msg.text()}`);
    });
  };

  console.log('=== Asset Registry ===');
  const assetsPage = await browser.newPage();
  trackErrors(assetsPage);
  await assetsPage.goto(`${BASE}/assets`, { waitUntil: 'networkidle' });
  await assetsPage.waitForSelector('text=Asset Registry', { timeout: 10000 });
  await assetsPage.waitForSelector('table tbody tr', { timeout: 10000 });
  const rowCount = await assetsPage.locator('table tbody tr').count();
  console.log(`Rows loaded: ${rowCount}`);

  console.log('=== Create an asset (against PO #1) ===');
  await assetsPage.getByRole('button', { name: 'New Asset' }).click();
  await assetsPage.getByLabel('Purchase Order Id').waitFor({ timeout: 10000 });
  await assetsPage.getByLabel('Purchase Order Id').fill('1');
  await assetsPage.getByLabel('Purchase Order Id').blur();
  await assetsPage.waitForSelector('text=PO-', { timeout: 10000 });
  console.log('PO lookup confirmation appeared.');
  await assetsPage.getByLabel('Asset Tag').fill('PLAYWRIGHT-ASSET-001');
  await assetsPage.getByRole('dialog').getByLabel('Department').click();
  await assetsPage.getByRole('option').first().click();
  await assetsPage.getByRole('button', { name: 'Save' }).click();
  await assetsPage.waitForSelector('text=PLAYWRIGHT-ASSET-001', { timeout: 10000 });
  console.log('Create succeeded.');

  console.log('=== Edit it ===');
  const row = assetsPage.locator('tr', { hasText: 'PLAYWRIGHT-ASSET-001' });
  await row.getByRole('button').first().click();
  const editStatus = assetsPage.getByRole('dialog').getByLabel('Status');
  await editStatus.waitFor({ timeout: 10000 });
  await editStatus.click();
  await assetsPage.getByRole('option', { name: 'Assigned' }).click();
  await assetsPage.getByRole('button', { name: 'Save' }).click();
  await assetsPage.waitForTimeout(500);
  console.log('Edit succeeded.');

  console.log('=== Delete it ===');
  await row.getByRole('button').last().click();
  await assetsPage.getByRole('button', { name: 'Delete' }).click();
  await assetsPage.waitForSelector('text=PLAYWRIGHT-ASSET-001', { state: 'detached', timeout: 10000 });
  console.log('Delete succeeded.');
  await assetsPage.close();

  console.log('\n=== Department Spend Report ===');
  const reportPage = await browser.newPage();
  trackErrors(reportPage);
  await reportPage.goto(`${BASE}/reports/department-spend`, { waitUntil: 'networkidle' });
  await reportPage.waitForSelector('text=Department Spend Report', { timeout: 10000 });
  await Promise.race([
    reportPage.waitForSelector('table tbody tr', { timeout: 10000 }),
    reportPage.waitForSelector('text=No spend in this range.', { timeout: 10000 }),
  ]);
  const reportRows = await reportPage.locator('table tbody tr').count();
  console.log(`Report rows: ${reportRows}`);
  const totalRow = await reportPage.locator('table tfoot').textContent().catch(() => null);
  console.log('Footer total row:', totalRow);
  await reportPage.close();

  console.log('\n=== Console/page errors across both pages ===');
  console.log(errors.length ? errors : 'none');
  if (errors.length) process.exitCode = 1;

  await browser.close();
})().catch((e) => {
  console.error('VERIFY FAILED:', e);
  process.exit(1);
});

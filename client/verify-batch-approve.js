const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  const errors = [];
  page.on('pageerror', (e) => errors.push(`pageerror: ${e.message}`));
  page.on('console', (msg) => {
    if (msg.type() === 'error') errors.push(`console.error: ${msg.text()}`);
  });

  await page.goto('http://localhost:8090/requests', { waitUntil: 'networkidle' });

  console.log('=== Filter to a department with many Pending rows ===');
  await page.getByLabel('Department').click();
  await page.getByRole('option', { name: 'Engineering' }).click();
  await page.waitForSelector('table tbody tr', { timeout: 10000 });
  await page.waitForTimeout(500);

  const rowCount = await page.locator('table tbody tr').count();
  console.log(`Pending rows on page 1: ${rowCount}`);
  if (rowCount < 3) throw new Error('Expected at least 3 Pending rows for this test');

  // Row count alone won't shrink (328 Pending rows, pageSize 10 — page 1 stays full) —
  // track the total count from TablePagination's "1-10 of N" caption instead.
  const totalBefore = await page.locator('.MuiTablePagination-displayedRows').textContent();
  const countBefore = Number(totalBefore.match(/of (\d+)/)[1]);
  console.log(`Total Pending count before: ${countBefore}`);

  console.log('=== Select the first 3 rows via their checkboxes, capture their ids ===');
  const checkboxes = page.locator('table tbody tr input[type="checkbox"]');
  const selectedDescriptions = [];
  for (let i = 0; i < 3; i++) {
    await checkboxes.nth(i).check();
    selectedDescriptions.push(await page.locator('table tbody tr').nth(i).locator('td').nth(1).textContent());
  }
  console.log('Selected items:', selectedDescriptions);

  const approveButton = page.getByRole('button', { name: /Approve Selected \(3\)/ });
  await approveButton.waitFor({ timeout: 5000 });
  console.log('Bulk action button shows count 3 — correct.');

  console.log('=== Open batch approve dialog, pick approver, submit ===');
  await approveButton.click();
  const dialog = page.getByRole('dialog');
  await dialog.getByLabel('Approved By').click();
  await page.getByRole('option').first().click();
  await dialog.getByRole('button', { name: 'Approve All' }).click();

  console.log('=== Confirm the snackbar notice mentions 3 requests ===');
  await page.waitForSelector('text=/3 requests approved/', { timeout: 10000 });
  console.log('Snackbar confirmed.');

  console.log('=== Refresh and confirm the total Pending count dropped by 3 ===');
  await page.waitForTimeout(3000); // give the CacheRefreshQueue worker a cycle
  await page.getByRole('alert').getByRole('button', { name: 'Refresh' }).click();
  await page.waitForTimeout(1000);
  const totalAfter = await page.locator('.MuiTablePagination-displayedRows').textContent();
  const countAfter = Number(totalAfter.match(/of (\d+)/)[1]);
  console.log(`Total Pending count after: ${countAfter} (was ${countBefore})`);
  if (countAfter !== countBefore - 3) throw new Error(`Expected total to drop by exactly 3, went ${countBefore} -> ${countAfter}`);

  console.log('=== Console/page errors ===');
  console.log(errors.length ? errors : 'none');
  if (errors.length) process.exitCode = 1;

  await browser.close();
})().catch((e) => {
  console.error('VERIFY FAILED:', e);
  process.exit(1);
});

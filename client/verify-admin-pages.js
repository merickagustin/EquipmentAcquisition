const { chromium } = require('playwright');

const BASE = 'http://localhost:8090';

const cases = [
  {
    path: '/vendors',
    heading: 'Vendors',
    newButton: 'New Vendor',
    fill: async (page) => {
      await page.getByLabel('Name').fill('Playwright Test Vendor');
    },
    rowText: 'Playwright Test Vendor',
  },
  {
    path: '/departments',
    heading: 'Departments',
    newButton: 'New Department',
    fill: async (page) => {
      await page.getByLabel('Code').fill('PWT');
      await page.getByLabel('Name').fill('Playwright Test Department');
    },
    rowText: 'Playwright Test Department',
  },
  {
    path: '/equipment-categories',
    heading: 'Equipment Categories',
    newButton: 'New Category',
    fill: async (page) => {
      await page.getByLabel('Name').fill('Playwright Test Category');
    },
    rowText: 'Playwright Test Category',
  },
];

(async () => {
  const browser = await chromium.launch();

  for (const c of cases) {
    const page = await browser.newPage();
    const errors = [];
    page.on('pageerror', (e) => errors.push(`pageerror: ${e.message}`));
    page.on('console', (msg) => {
      if (msg.type() === 'error') errors.push(`console.error: ${msg.text()}`);
    });

    console.log(`\n=== ${c.path} ===`);
    await page.goto(`${BASE}${c.path}`, { waitUntil: 'networkidle' });
    await page.waitForSelector(`text=${c.heading}`, { timeout: 10000 });

    const rowCountBefore = await page.locator('table tbody tr').count();
    console.log(`Rows before create: ${rowCountBefore}`);

    await page.getByRole('button', { name: c.newButton }).click();
    await page.getByLabel(/Name|Code/).first().waitFor({ timeout: 10000 });
    await c.fill(page);
    await page.getByRole('button', { name: 'Save' }).click();
    await page.waitForSelector(`text=${c.rowText}`, { timeout: 10000 });
    console.log(`Create succeeded — "${c.rowText}" appeared in the table.`);

    const row = page.locator('tr', { hasText: c.rowText });
    await row.getByRole('button').last().click(); // delete icon is the last button in the row
    await page.getByRole('button', { name: 'Delete' }).click();
    await page.waitForSelector(`text=${c.rowText}`, { state: 'detached', timeout: 10000 });
    console.log('Delete succeeded — row removed.');

    console.log('Console/page errors:', errors.length ? errors : 'none');
    if (errors.length) process.exitCode = 1;

    await page.close();
  }

  await browser.close();
})().catch((e) => {
  console.error('VERIFY FAILED:', e);
  process.exit(1);
});

const { chromium } = require('playwright-core');
(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await (await browser.newContext()).newPage();
  const errors = [];
  page.on('pageerror', e => errors.push(`PE: ${e.message}\n${(e.stack || '').split('\n').slice(0, 8).join('\n')}`));
  page.on('console', m => { if (m.type() === 'error') errors.push(`CE: ${m.text().slice(0, 800)}`); });
  await page.route(/\/hubs\//, r => r.abort());
  await page.route(/notifications\/unread/, r => r.fulfill({ status: 200, body: '0' }));

  // Login
  await page.goto('http://localhost:4200/login');
  await page.waitForTimeout(1500);
  await page.locator('input').nth(0).fill('admin');
  await page.locator('input').nth(1).fill('Admin@123');
  await page.locator('button[type="submit"]').click();
  await page.waitForTimeout(2000);

  // Issue detail
  errors.length = 0;
  await page.goto('http://localhost:4200/issues/DEMO-13', { timeout: 8000 }).catch(() => {});
  await page.waitForTimeout(4000);
  console.log('--- Issue detail critical errors ---');
  errors.filter(e => !e.includes('hubs') && !e.includes('SignalR') && !e.includes('FailedToNegotiate') && !e.includes('Failed to fetch') && !e.includes('Failed to load resource')).slice(0, 10).forEach(e => console.log(e));

  // Create dialog
  errors.length = 0;
  await page.goto('http://localhost:4200/projects/DEMO/backlog', { timeout: 8000 }).catch(() => {});
  await page.waitForTimeout(2500);
  const createBtn = page.getByRole('button', { name: /Tạo issue/i });
  if (await createBtn.count() > 0) {
    await createBtn.first().click().catch(() => {});
    await page.waitForTimeout(2500);
  }
  console.log('--- Create dialog critical errors ---');
  errors.filter(e => !e.includes('hubs') && !e.includes('SignalR') && !e.includes('FailedToNegotiate') && !e.includes('Failed to fetch') && !e.includes('Failed to load resource')).slice(0, 10).forEach(e => console.log(e));

  await browser.close();
})();

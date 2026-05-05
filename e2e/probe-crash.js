const { chromium } = require('playwright-core');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  const errors = [];
  page.on('console', (msg) => {
    if (msg.type() === 'error' || msg.type() === 'warning') {
      errors.push(`[${msg.type()}] ${msg.text()}`);
    }
  });
  page.on('pageerror', (e) => errors.push(`[pageerror] ${e.message}\n${e.stack || ''}`));

  await page.goto('http://localhost:4200/login', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(800);
  const inputs = await page.locator('input').all();
  await inputs[0].fill('admin');
  await inputs[1].fill('Admin@123');
  await page.click('button[type="submit"]');
  await page.waitForURL(/\/workspaces$|\/$/);
  await page.waitForTimeout(800);

  console.log('\n=== 1. Open issue detail DEMO-13 ===');
  errors.length = 0;
  try {
    await page.goto('http://localhost:4200/issues/DEMO-13', { waitUntil: 'domcontentloaded', timeout: 10000 });
  } catch (e) { console.log('goto timeout (expected if crash):', e.message.slice(0, 100)); }
  await page.waitForTimeout(3000);
  try { await page.screenshot({ path: 'screenshots/crash-detail.png', fullPage: false, timeout: 5000 }); } catch (e) { console.log('screenshot timeout'); }
  const detailErrors = errors.filter(e =>
    !e.includes('SignalR') && !e.includes('hubs/workspace') &&
    !e.includes('Status code') && !e.includes('FailedToNegotiate'));
  console.log(`Issue-detail errors (excl. SignalR): ${detailErrors.length}`);
  detailErrors.slice(0, 15).forEach(e => console.log(' ', e.slice(0, 400)));

  console.log('\n=== 2. Open Create Issue dialog from topbar ===');
  errors.length = 0;
  try {
    await page.goto('http://localhost:4200/projects/DEMO/backlog', { waitUntil: 'domcontentloaded', timeout: 10000 });
  } catch {}
  await page.waitForTimeout(2500);
  // Click "Tạo issue" button on backlog page
  const createBtns = await page.getByRole('button', { name: /Tạo issue|Create issue/i }).all();
  if (createBtns.length > 0) {
    await createBtns[0].click();
    await page.waitForTimeout(1500);
    await page.screenshot({ path: 'screenshots/crash-create.png', fullPage: true });
  } else {
    console.log('No Tạo issue button found');
  }
  const createErrors = errors.filter(e =>
    !e.includes('SignalR') && !e.includes('hubs/workspace') &&
    !e.includes('Status code') && !e.includes('FailedToNegotiate'));
  console.log(`Create dialog errors: ${createErrors.length}`);
  createErrors.slice(0, 15).forEach(e => console.log(' ', e.slice(0, 300)));

  await browser.close();
})();

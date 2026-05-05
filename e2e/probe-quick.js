const { chromium } = require('playwright-core');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, ignoreHTTPSErrors: true });
  const page = await ctx.newPage();
  const errors = [];
  page.on('console', m => { if (m.type() === 'error') errors.push(`[c] ${m.text()}`); });
  page.on('pageerror', e => errors.push(`[p] ${e.message}\n${e.stack?.split('\n').slice(0, 5).join('\n')}`));

  // Disable SignalR-related routes to skip noise
  await page.route(/\/hubs\//, r => r.abort());

  await page.goto('http://localhost:4200/login', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(1500);
  const inputs = await page.locator('input').all();
  if (inputs.length >= 2) {
    await inputs[0].fill('admin');
    await inputs[1].fill('Admin@123');
    await page.click('button[type="submit"]').catch(() => {});
    await page.waitForTimeout(2000);
  }

  console.log('=== Issue detail DEMO-13 ===');
  errors.length = 0;
  await page.goto('http://localhost:4200/issues/DEMO-13', { waitUntil: 'domcontentloaded', timeout: 8000 }).catch(e => console.log('goto err:', e.message.slice(0, 80)));
  await page.waitForTimeout(4000);
  const filtered = errors.filter(e => !e.includes('SignalR') && !e.includes('hubs/workspace'));
  filtered.slice(0, 8).forEach(e => console.log(e.slice(0, 600)));
  console.log(`Total: ${filtered.length}`);

  console.log('\n=== Backlog page + create dialog ===');
  errors.length = 0;
  await page.goto('http://localhost:4200/projects/DEMO/backlog', { waitUntil: 'domcontentloaded', timeout: 8000 }).catch(() => {});
  await page.waitForTimeout(3000);
  const backlogErrs = errors.filter(e => !e.includes('SignalR') && !e.includes('hubs'));
  backlogErrs.slice(0, 8).forEach(e => console.log(e.slice(0, 600)));
  console.log(`Backlog total: ${backlogErrs.length}`);

  errors.length = 0;
  const btn = page.getByRole('button', { name: /Tạo issue/i });
  if (await btn.count() > 0) {
    await btn.first().click();
    await page.waitForTimeout(2500);
    const dlgErrs = errors.filter(e => !e.includes('SignalR') && !e.includes('hubs'));
    console.log('Create dialog errs:', dlgErrs.length);
    dlgErrs.slice(0, 8).forEach(e => console.log(e.slice(0, 600)));
  } else {
    console.log('No "Tạo issue" button found');
  }

  await browser.close();
})();

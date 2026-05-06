const { chromium } = require('playwright-core');
(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await (await browser.newContext()).newPage();
  const errs = [];
  page.on('pageerror', e => errs.push(`PE: ${e.message.slice(0, 200)}`));
  page.on('console', m => { if (m.type() === 'error') errs.push(`CE: ${m.text().slice(0, 300)}`); });
  page.on('response', r => {
    if (r.status() === 504) errs.push(`504: ${r.url().slice(-80)}`);
  });
  await page.route(/\/hubs\//, r => r.abort());

  await page.goto('http://localhost:4200/login');
  await page.waitForTimeout(1500);
  await page.locator('input').nth(0).fill('admin');
  await page.locator('input').nth(1).fill('Admin@123');
  await page.locator('button[type="submit"]').click();
  await page.waitForTimeout(2500);

  for (const url of [
    '/issues',
    '/issues/DEMO-13',
    '/projects/DEMO/backlog',
  ]) {
    errs.length = 0;
    console.log(`\n=== ${url} ===`);
    try {
      await page.goto(`http://localhost:4200${url}`, { timeout: 12000 });
    } catch (e) { console.log('goto:', e.message.slice(0, 100)); }
    await page.waitForTimeout(4000);
    const title = await page.title().catch(() => '?');
    console.log(`  title: ${title}`);
    const bodyText = await page.locator('body').textContent().catch(() => '');
    console.log(`  body length: ${bodyText.length}`);
    const filt = errs.filter(e => !e.includes('hubs') && !e.includes('SignalR') && !e.includes('FailedToNegotiate') && !e.includes('Failed to fetch') && !e.includes('Failed to load resource'));
    filt.slice(0, 6).forEach(e => console.log(`  ${e}`));
  }

  await browser.close();
})();

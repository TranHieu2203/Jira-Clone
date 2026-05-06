const { chromium } = require('playwright-core');
(async () => {
  const browser = await chromium.launch({ headless: true, args: ['--enable-logging', '--v=1'] });
  const ctx = await browser.newContext();
  const page = await ctx.newPage();
  const errs = [];

  // Capture every event
  page.on('console', m => errs.push(`${m.type().toUpperCase()}: ${m.text().slice(0, 500)}`));
  page.on('pageerror', e => errs.push(`PAGE ERROR: ${e.message}`));
  page.on('crash', () => errs.push('CRASH EVENT'));

  // CDP — listen to runtime exceptions + memory pressure
  const cdp = await ctx.newCDPSession(page);
  await cdp.send('Runtime.enable');
  await cdp.send('Log.enable');
  await cdp.send('Performance.enable');
  cdp.on('Runtime.exceptionThrown', (p) => {
    errs.push(`CDP EXC: ${p.exceptionDetails.text} ${p.exceptionDetails.exception?.description?.slice(0, 300) || ''}`);
  });
  cdp.on('Log.entryAdded', (p) => {
    if (p.entry.level === 'error') errs.push(`CDP LOG: ${p.entry.text.slice(0, 300)}`);
  });

  await page.route(/\/hubs\//, r => r.abort());

  // Login
  await page.goto('http://localhost:4200/login');
  await page.waitForTimeout(2500);
  await page.locator('input').nth(0).fill('admin');
  await page.locator('input').nth(1).fill('Admin@123');
  await page.locator('button[type="submit"]').click();
  await page.waitForTimeout(3000);

  errs.length = 0;
  console.log('=== goto /issues/DEMO-13 ===');
  await page.goto('http://localhost:4200/issues/DEMO-13', { waitUntil: 'commit' }).catch(e => console.log('goto:', e.message.slice(0, 80)));

  await page.waitForTimeout(4000);

  console.log(`\nFinal URL: ${page.url()}`);
  try {
    const sz = await page.evaluate(() => document.querySelector('app-root')?.innerHTML.length ?? -1);
    console.log(`app-root size: ${sz}`);
  } catch { console.log('eval failed (page died)'); }

  console.log('\n=== Events ===');
  errs.filter(e => !e.includes('hubs') && !e.includes('SignalR') && !e.includes('FailedToNegotiate') && !e.includes('Failed to fetch') && !e.includes('vite]')).forEach(e => console.log(e));

  await browser.close();
})();

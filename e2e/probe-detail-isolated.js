const { chromium } = require('playwright-core');
(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext();
  const page = await ctx.newPage();
  const errs = [];
  page.on('console', m => errs.push(`${m.type()}: ${m.text()}`));
  page.on('pageerror', e => errs.push(`PE: ${e.message}\n${e.stack?.split('\n').slice(0,5).join('|') || ''}`));
  page.on('crash', () => errs.push('PAGE CRASHED'));
  page.on('requestfailed', r => errs.push(`FAIL: ${r.url()} - ${r.failure()?.errorText}`));
  // No route abort — let hub connect normally to :5000

  // Login
  await page.goto('http://localhost:4200/login');
  await page.waitForTimeout(2000);
  await page.locator('input').nth(0).fill('admin');
  await page.locator('input').nth(1).fill('Admin@123');
  await page.locator('button[type="submit"]').click();
  await page.waitForTimeout(3000);

  // Direct goto detail
  errs.length = 0;
  console.log('=== Navigating to /issues/DEMO-13 ===');
  await page.goto('http://localhost:4200/issues/DEMO-13', { waitUntil: 'commit' }).catch(e => console.log('goto:', e.message.slice(0, 100)));

  // Watch DOM growth + crash
  for (let i = 0; i < 10; i++) {
    await new Promise(r => setTimeout(r, 1000));
    let snapshot;
    try {
      snapshot = await page.evaluate(() => {
        const root = document.querySelector('app-root');
        const detail = document.querySelector('app-issue-detail-page');
        const head = document.querySelector('.head');
        return {
          rootSize: root?.innerHTML.length ?? -1,
          detailPresent: !!detail,
          detailSize: detail?.innerHTML.length ?? 0,
          headPresent: !!head,
          headText: head?.textContent?.slice(0, 80) ?? null
        };
      });
    } catch (e) {
      console.log(`t=${i+1}s: page died — ${e.message.slice(0, 80)}`);
      break;
    }
    console.log(`t=${i+1}s root=${snapshot.rootSize} detail=${snapshot.detailPresent}/${snapshot.detailSize} head=${snapshot.headText}`);
  }

  console.log('\n=== Errors ===');
  errs.filter(e => !e.includes('hubs') && !e.includes('SignalR')).forEach(e => console.log(e.slice(0, 400)));

  await browser.close();
})();

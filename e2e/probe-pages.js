const { chromium } = require('playwright-core');
(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await (await browser.newContext()).newPage();
  const errs = [];
  page.on('pageerror', e => errs.push(`PE: ${e.message.slice(0, 200)}`));
  page.on('console', m => { if (m.type() === 'error') errs.push(`CE: ${m.text().slice(0, 300)}`); });
  page.on('response', r => {
    if (r.status() >= 400 && !r.url().includes('hubs')) errs.push(`${r.status()}: ${r.url()}`);
  });
  page.on('requestfailed', r => errs.push(`REQ-FAIL: ${r.url()} - ${r.failure()?.errorText}`));
  page.on('crash', () => errs.push('PAGE CRASHED'));
  page.on('close', () => errs.push('PAGE CLOSED'));
  await page.route(/\/hubs\//, r => r.abort());

  await page.goto('http://localhost:4200/login');
  await page.waitForTimeout(2500);
  const inputCount = await page.locator('input').count();
  console.log(`Login page input count: ${inputCount}`);
  console.log(`Body sample: ${(await page.locator('body').textContent()).slice(0, 100)}`);
  if (inputCount < 2) {
    console.log('=== boot errors ===');
    errs.slice(0, 20).forEach(e => console.log(' ', e));
    await browser.close();
    return;
  }
  await page.locator('input').nth(0).fill('admin');
  await page.locator('input').nth(1).fill('Admin@123');
  await page.locator('button[type="submit"]').click();
  await page.waitForTimeout(3500);
  const afterLoginUrl = page.url();
  console.log(`After login URL: ${afterLoginUrl}`);
  console.log(`Body now: ${(await page.locator('body').textContent()).slice(0, 200)}`);
  console.log('=== login errs ===');
  errs.slice(0, 8).forEach(e => console.log(' ', e));

  // Capture every error/log without filter for issue-detail
  errs.length = 0;
  page.removeAllListeners('console');
  page.on('console', m => errs.push(`${m.type()}: ${m.text().slice(0, 400)}`));
  page.removeAllListeners('pageerror');
  page.on('pageerror', e => errs.push(`PE: ${e.message}\n${(e.stack || '').split('\n').slice(0, 8).join('\n')}`));

  console.log('\n=== /issues/DEMO-13 (verbose) ===');
  try {
    await page.goto('http://localhost:4200/issues/DEMO-13', { timeout: 15000, waitUntil: 'commit' });
  } catch (e) { console.log('goto:', e.message.slice(0, 80)); }
  // Capture state every 1s to see when it crashes
  for (let i = 0; i < 8; i++) {
    await page.waitForTimeout(1000);
    try {
      const sz = await page.evaluate(() => document.querySelector('app-root')?.innerHTML.length ?? -1);
      const visible = await page.evaluate(() => {
        const detail = document.querySelector('app-issue-detail-page');
        return detail ? `present, hasContent=${detail.innerHTML.length > 100}` : 'no app-issue-detail-page';
      });
      console.log(`t=${i+1}s: app-root=${sz} chars, ${visible}`);
    } catch (e) { console.log(`t=${i+1}s: eval err: ${e.message.slice(0, 80)}`); break; }
  }
  console.log(`URL: ${page.url()}`);
  console.log(`title: ${await page.title().catch(() => '?')}`);
  const bt = await page.locator('body').textContent().catch(() => '');
  console.log(`body length: ${bt.length}`);
  console.log(`body sample: ${bt.slice(0, 200)}`);
  const html = await page.content().catch(() => '');
  console.log(`HTML head: ${html.slice(0, 500)}`);
  const rootInner = await page.evaluate(() => {
    const root = document.querySelector('app-root');
    return root ? root.innerHTML.slice(0, 800) : 'no app-root';
  }).catch(e => `eval err: ${e.message}`);
  console.log(`app-root innerHTML: ${rootInner}`);
  console.log('=== ALL events ===');
  errs.filter(e => !e.includes('SignalR') && !e.includes('hubs') && !e.includes('FailedToNegotiate') && !e.includes('Failed to fetch')).slice(0, 20).forEach(e => console.log(e.slice(0, 600)));

  await browser.close();
})();

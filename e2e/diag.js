const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ locale: 'vi-VN' });
  const page = await ctx.newPage();

  page.on('console', (msg) => console.log(`CONSOLE[${msg.type()}]:`, msg.text()));
  page.on('pageerror', (err) => console.log(`PAGEERROR:`, err.stack || err.message));
  page.on('requestfailed', (req) => console.log(`REQ FAIL: ${req.url()} ${req.failure()?.errorText}`));
  page.on('response', (resp) => {
    const url = resp.url();
    if (url.includes('/api/') || url.includes('/i18n/')) {
      console.log(`HTTP ${resp.status()} ${resp.request().method()} ${url}`);
    }
  });

  await page.goto('http://localhost:4200/login', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1500);

  // Click login as visible
  await page.locator('button[type="submit"]').click();
  await page.waitForTimeout(3000);
  console.log('Final URL:', page.url());
  await browser.close();
})();

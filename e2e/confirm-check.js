// Verify the delete-confirm dialog renders translated text.
const { chromium } = require('playwright');
const path = require('path');

(async () => {
  const browser = await chromium.launch({
    headless: false,
    slowMo: 200,
    args: ['--window-size=1280,820', '--window-position=80,40']
  });
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 }, locale: 'vi-VN' });
  const page = await ctx.newPage();

  await page.goto('http://localhost:4200/login', { waitUntil: 'networkidle' });
  await page.locator('button[type="submit"]').click();
  await page.waitForURL('**/products');
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(500);

  // The page has a row from previous test (SKU-TOAST-1 / Sản phẩm demo). Click Xóa.
  await page.getByRole('button', { name: /Xóa|Delete/i }).first().click();
  await page.waitForSelector('.p-confirmdialog', { timeout: 5000 });
  await page.waitForTimeout(400);
  await page.screenshot({ path: path.join(__dirname, 'screenshots/12-confirm-translated.png') });
  console.log('📸 12-confirm-translated.png');

  await page.waitForTimeout(1500);
  await browser.close();
})().catch((e) => { console.error('FAIL:', e); process.exit(1); });

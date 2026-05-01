// Verify the success toast renders translated text after the fix.
const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');

const SHOTS = path.join(__dirname, 'screenshots');
fs.mkdirSync(SHOTS, { recursive: true });

(async () => {
  const browser = await chromium.launch({
    headless: false,
    slowMo: 250,
    args: ['--window-size=1280,820', '--window-position=80,40']
  });
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 }, locale: 'vi-VN' });
  const page = await ctx.newPage();
  page.on('console', (m) => console.log(`[browser ${m.type()}] ${m.text()}`));

  await page.goto('http://localhost:4200/login', { waitUntil: 'networkidle' });
  await page.locator('button[type="submit"]').click();
  await page.waitForURL('**/products');
  await page.waitForLoadState('networkidle');

  // Open create dialog
  await page.getByRole('button', { name: /Tạo mới|Tạo sản phẩm|Create/i }).first().click();
  await page.waitForTimeout(400);

  // Fill form
  await page.locator('input[name="name"]').fill('Sản phẩm demo');
  await page.locator('input[name="sku"]').fill('SKU-TOAST-1');
  await page.locator('p-inputnumber[name="price"] input').first().fill('150');
  await page.locator('input[name="description"]').fill('Kiểm tra toast');

  // Submit
  await page.locator('p-dialog button[type="submit"]').click();

  // Capture toast as soon as it appears
  await page.waitForSelector('.p-toast .p-toast-message', { timeout: 5000 });
  await page.waitForTimeout(300); // animation settle
  await page.screenshot({ path: path.join(SHOTS, '11-toast-translated.png') });
  console.log('📸 11-toast-translated.png');

  await page.waitForTimeout(1500);
  await browser.close();
})().catch((e) => { console.error('FAIL:', e); process.exit(1); });

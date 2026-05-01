// Playwright E2E driving real Chrome (headed) so the user can watch.
// Screenshots saved to ./screenshots/<step>.png
const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');

const SHOTS = path.join(__dirname, 'screenshots');
fs.mkdirSync(SHOTS, { recursive: true });

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

(async () => {
  const browser = await chromium.launch({
    headless: false,            // headed so user can watch
    slowMo: 350,                // slow down each action so it's visible
    args: ['--window-size=1280,820', '--window-position=80,40']
  });
  const ctx = await browser.newContext({
    viewport: { width: 1280, height: 800 },
    locale: 'vi-VN'
  });
  const page = await ctx.newPage();

  page.on('console', (msg) => console.log(`  [browser ${msg.type()}] ${msg.text()}`));
  page.on('pageerror', (err) => console.log(`  [browser ERROR] ${err.message}`));

  const shot = async (name) => {
    const file = path.join(SHOTS, `${name}.png`);
    await page.screenshot({ path: file, fullPage: false });
    console.log(`  📸 ${name}.png`);
  };

  console.log('Step 1: open login page');
  await page.goto('http://localhost:4200/login', { waitUntil: 'networkidle' });
  await sleep(500);
  await shot('01-login-empty');

  console.log('Step 2: clear pre-filled fields, type creds');
  // The form is pre-filled (admin / Admin@123). Clear and re-type to make typing visible.
  const userInput = page.locator('input[name="userName"]');
  const passInput = page.locator('input[name="password"]');
  await userInput.click();
  await userInput.fill('');
  await userInput.type('admin', { delay: 60 });
  await passInput.click();
  await passInput.fill('');
  await passInput.type('Admin@123', { delay: 60 });
  await shot('02-login-typed');

  console.log('Step 3: submit login → expect redirect to /products');
  await page.locator('button[type="submit"]').click();
  await page.waitForURL('**/products', { timeout: 10000 });
  await page.waitForLoadState('networkidle');
  await sleep(800);
  await shot('03-products-list-empty');

  console.log('Step 4: open create dialog');
  await page.getByRole('button', { name: /Tạo mới|Tạo sản phẩm|Create/i }).first().click();
  await sleep(600);
  await shot('04-create-dialog-open');

  console.log('Step 5: fill product form');
  await page.locator('input[name="name"]').type('Sản phẩm demo', { delay: 50 });
  await page.locator('input[name="sku"]').type('SKU-DEMO-1', { delay: 50 });
  // p-inputNumber wraps the input in a different element; locate by label/parent
  const priceInput = page.locator('p-inputnumber[name="price"] input').first();
  await priceInput.click();
  await priceInput.type('199.5', { delay: 50 });
  await page.locator('input[name="description"]').type('Một sản phẩm tạo từ Playwright', { delay: 30 });
  await shot('05-create-form-filled');

  console.log('Step 6: submit create → expect toast + dialog closed + row added');
  await page.locator('p-dialog button[type="submit"]').click();
  await sleep(1200);
  await shot('06-after-create-toast');

  console.log('Step 7: trigger validation error to see ErrorDialog (TraceId)');
  // Re-open dialog with an invalid SKU (duplicate of seeded one) won't work as we just created it.
  // Use empty values to trip the FluentValidation backend errors.
  await page.getByRole('button', { name: /Tạo mới|Tạo sản phẩm|Create/i }).first().click();
  await sleep(500);
  // Leave the form blank then submit
  await page.locator('p-dialog button[type="submit"]').click();
  await sleep(1500);
  await shot('07-error-dialog-traceid');

  console.log('Step 8: dismiss error dialog & cancel form');
  // Close ErrorDialog (the OK button)
  const okBtn = page.locator('p-dialog').filter({ hasText: /Trace|Mã truy/i }).getByRole('button', { name: /OK|Đồng ý/i });
  if (await okBtn.count()) {
    await okBtn.first().click();
    await sleep(400);
  }
  // Cancel the create dialog if it's still open
  const cancelBtn = page.getByRole('button', { name: /Hủy|Cancel/i }).first();
  if (await cancelBtn.isVisible().catch(() => false)) {
    await cancelBtn.click();
    await sleep(400);
  }

  console.log('Step 9: switch language to EN, capture re-translated UI');
  await page.locator('.lang-switch button', { hasText: 'EN' }).click();
  await sleep(800);
  await shot('08-language-en');

  console.log('Step 10: switch back to VI');
  await page.locator('.lang-switch button', { hasText: 'VI' }).click();
  await sleep(600);
  await shot('09-language-vi');

  console.log('Step 11: logout');
  await page.getByRole('button', { name: /Đăng xuất|Logout/i }).click();
  await page.waitForURL('**/login', { timeout: 10000 });
  await sleep(500);
  await shot('10-logged-out');

  console.log('All done. Closing browser in 2s.');
  await sleep(2000);
  await browser.close();
})().catch(async (err) => {
  console.error('FAILED:', err);
  process.exit(1);
});

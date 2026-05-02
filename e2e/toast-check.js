// Verify success toast after creating a workspace (Sample /products removed).
const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');

const BASE = process.env.E2E_BASE_URL || 'http://localhost:4200';
const SHOTS = path.join(__dirname, 'screenshots');
fs.mkdirSync(SHOTS, { recursive: true });

(async () => {
  const ts = Date.now();
  const browser = await chromium.launch({
    headless: false,
    slowMo: 250,
    args: ['--window-size=1280,820', '--window-position=80,40']
  });
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 }, locale: 'vi-VN' });
  const page = await ctx.newPage();
  page.on('console', (m) => console.log(`[browser ${m.type()}] ${m.text()}`));

  await page.goto(`${BASE}/login`, { waitUntil: 'networkidle' });
  await page.locator('input[name="userName"]').fill('admin');
  await page.locator('input[name="password"]').fill('Admin@123');
  await page.locator('button[type="submit"]').click();
  await page.waitForURL('**/workspaces', { timeout: 30000 });
  await page.waitForLoadState('networkidle');

  await page.getByRole('button', { name: /Tạo workspace/i }).click();
  await page.waitForTimeout(400);
  await page.locator('input[name="name"]').fill(`Toast WS ${ts}`);
  await page.locator('input[name="slug"]').fill(`toast-${ts}`);
  await page.locator('p-dialog').getByRole('button', { name: /^Lưu$/i }).click();

  await page.waitForSelector('.p-toast .p-toast-message', { timeout: 8000 });
  await page.waitForTimeout(300);
  await page.screenshot({ path: path.join(SHOTS, '11-toast-translated.png') });
  console.log('📸 11-toast-translated.png');

  await page.waitForTimeout(800);
  await browser.close();
})().catch((e) => {
  console.error('FAIL:', e);
  process.exit(1);
});

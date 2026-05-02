// Verify delete-comment ConfirmDialog uses translated strings (requires seeded issue DEMO-1 + ≥1 comment).
const { chromium } = require('playwright');
const path = require('path');

const BASE = process.env.E2E_BASE_URL || 'http://localhost:4200';
const ISSUE_KEY = process.env.E2E_ISSUE_KEY || 'DEMO-1';

(async () => {
  const browser = await chromium.launch({
    headless: false,
    slowMo: 200,
    args: ['--window-size=1280,820', '--window-position=80,40']
  });
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 }, locale: 'vi-VN' });
  const page = await ctx.newPage();

  await page.goto(`${BASE}/login`, { waitUntil: 'networkidle' });
  await page.locator('input[name="userName"]').fill('admin');
  await page.locator('input[name="password"]').fill('Admin@123');
  await page.locator('button[type="submit"]').click();
  await page.waitForURL('**/workspaces', { timeout: 30000 });
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(400);

  await page.goto(`${BASE}/issues/${ISSUE_KEY}`, { waitUntil: 'networkidle' });
  await page.waitForTimeout(600);

  const deleteBtn = page.locator('app-comments-thread').getByRole('button', { name: /^Xóa$/i }).first();
  if (!(await deleteBtn.count())) {
    console.warn(`No comment delete button on ${ISSUE_KEY}; add a comment first or set E2E_ISSUE_KEY.`);
    await browser.close();
    process.exit(0);
  }

  await deleteBtn.click();
  await page.waitForSelector('.p-confirmdialog', { timeout: 8000 });
  await page.waitForTimeout(400);
  await page.screenshot({ path: path.join(__dirname, 'screenshots/12-confirm-translated.png') });
  console.log('📸 12-confirm-translated.png');

  await page.waitForTimeout(800);
  await browser.close();
})().catch((e) => {
  console.error('FAIL:', e);
  process.exit(1);
});

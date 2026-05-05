/**
 * Audit script for: workflow editor, custom fields admin, issue detail page UI/UX.
 * - Login admin, navigate qua các surface, screenshot từng step.
 * - Capture console errors + 4xx/5xx network responses.
 * - Output: e2e/screenshots/audit-*.png + console.log report cuối.
 *
 * Run: node audit-jira-ui.js
 */
const { chromium } = require('playwright-core');
const path = require('path');
const fs = require('fs');

const BASE = process.env.AUDIT_BASE || 'http://localhost:4200';
const SHOTS = path.join(__dirname, 'screenshots');
fs.mkdirSync(SHOTS, { recursive: true });

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const errors = [];
const networkErrors = [];

async function shot(page, name) {
  const file = path.join(SHOTS, `audit-${name}.png`);
  await page.screenshot({ path: file, fullPage: true });
  console.log(`[shot] ${name} → ${file}`);
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  page.on('console', (msg) => {
    const t = msg.type();
    if (t === 'error' || t === 'warning') {
      errors.push(`[${t}] ${msg.text()}`);
    }
  });
  page.on('pageerror', (e) => errors.push(`[pageerror] ${e.message}`));
  page.on('response', (r) => {
    const url = r.url();
    if (url.includes('/api/') && r.status() >= 400) {
      networkErrors.push(`${r.status()} ${r.request().method()} ${url}`);
    }
  });

  try {
    // ─── 1. Login ─────────────────────────────────────────────
    console.log(`\n=== 1. Login ===`);
    await page.goto(`${BASE}/login`, { waitUntil: 'networkidle' });
    await page.fill('input[name="userName"], input[type="text"]', 'admin');
    await page.fill('input[type="password"]', 'Admin@123');
    await shot(page, '01-login-form');
    await page.click('button[type="submit"]');
    await page.waitForURL(/\/workspaces$|\/$/, { timeout: 10000 });
    await sleep(500);
    await shot(page, '02-after-login');

    // ─── 2. Navigate to DEMO project ───────────────────────────
    console.log(`\n=== 2. Open DEMO project ===`);
    await page.goto(`${BASE}/projects/DEMO`, { waitUntil: 'networkidle' });
    await sleep(800);
    await shot(page, '03-project-overview');

    // ─── 3. Backlog page ───────────────────────────────────────
    console.log(`\n=== 3. Backlog page ===`);
    await page.goto(`${BASE}/projects/DEMO/backlog`, { waitUntil: 'networkidle' });
    await sleep(1200);
    await shot(page, '04-backlog');

    // ─── 4. Board page ─────────────────────────────────────────
    console.log(`\n=== 4. Board page ===`);
    await page.goto(`${BASE}/projects/DEMO/board`, { waitUntil: 'networkidle' });
    await sleep(1200);
    await shot(page, '05-board');

    // ─── 5. Issues list ────────────────────────────────────────
    console.log(`\n=== 5. Issues list ===`);
    await page.goto(`${BASE}/projects/DEMO/issues`, { waitUntil: 'networkidle' });
    await sleep(1000);
    await shot(page, '06-issues-list');

    // ─── 6. Issue detail ───────────────────────────────────────
    console.log(`\n=== 6. Issue detail (DEMO-13 = F11 Permission scheme) ===`);
    await page.goto(`${BASE}/issues/DEMO-13`, { waitUntil: 'networkidle' });
    await sleep(1000);
    await shot(page, '07-issue-detail');

    // ─── 7. Settings → Workflow editor ─────────────────────────
    console.log(`\n=== 7. Workflow editor ===`);
    await page.goto(`${BASE}/projects/DEMO/settings/workflow`, { waitUntil: 'networkidle' });
    await sleep(1500);
    await shot(page, '08-workflow-editor');

    // ─── 8. Settings → Custom fields admin ─────────────────────
    console.log(`\n=== 8. Custom fields admin ===`);
    await page.goto(`${BASE}/projects/DEMO/settings/fields`, { waitUntil: 'networkidle' });
    await sleep(1500);
    await shot(page, '09-fields-admin');

    // ─── 9. Settings overview ──────────────────────────────────
    console.log(`\n=== 9. Settings overview ===`);
    await page.goto(`${BASE}/projects/DEMO/settings`, { waitUntil: 'networkidle' });
    await sleep(800);
    await shot(page, '10-settings-overview');
  } catch (e) {
    console.error('FATAL:', e.message);
    await shot(page, '99-error-state').catch(() => {});
  }

  // ─── Report ────────────────────────────────────────────────
  console.log(`\n${'─'.repeat(60)}`);
  console.log(`Console errors: ${errors.length}`);
  for (const e of errors.slice(0, 30)) console.log('  ' + e);
  if (errors.length > 30) console.log(`  ... +${errors.length - 30} more`);
  console.log(`\nNetwork 4xx/5xx: ${networkErrors.length}`);
  for (const e of networkErrors.slice(0, 30)) console.log('  ' + e);
  if (networkErrors.length > 30) console.log(`  ... +${networkErrors.length - 30} more`);

  await browser.close();
})();

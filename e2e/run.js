/**
 * Jira-Clone — Playwright headed smoke (một lượt các tính năng hiện có).
 *
 * Tiêu chí: login + ErrorDialog (401), workspace/project/issue, board + swimlane,
 * comment + attachment + activity (hiển thị), UserPicker (workspace member dialog),
 * project settings (/projects/:key/settings), workspace validation ErrorDialog, i18n VI/EN, logout.
 *
 * Yêu cầu: stack Docker dev chạy (nginx http://localhost:4200) hoặc FE+BE tương đương.
 *
 * Chạy:  cd e2e && node run.js
 * Hoặc: npm run jira-flow --prefix e2e
 */
const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');

const BASE = process.env.E2E_BASE_URL || 'http://localhost:4200';
const SHOTS = path.join(__dirname, 'screenshots');
const UPLOAD_FILE = path.join(__dirname, 'fixtures', 'e2e-upload.txt');

fs.mkdirSync(SHOTS, { recursive: true });

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

/** PrimeNG đưa overlay ra body — đừng chờ visibility trên host `app-error-dialog`. */
async function dismissErrorDialog(page) {
  const dlg = page.getByRole('dialog').filter({ hasText: /Trace|Mã truy vết|Chi tiết|Lỗi|Error/i });
  if (await dlg.count() && await dlg.first().isVisible().catch(() => false)) {
    await dlg.first().getByRole('button', { name: /Đồng ý|OK/i }).click();
    await sleep(400);
  }
}

async function shot(page, name) {
  const file = path.join(SHOTS, `${name}.png`);
  await page.screenshot({ path: file, fullPage: false });
  console.log(`  📸 ${name}.png`);
}

/** PrimeNG p-select: mở dropdown (tránh label che pointer). */
async function pickPrimeSelectOption(page, selectName, optionTextRegex) {
  const sel = page.locator(`p-dialog p-select[name="${selectName}"]`);
  const trigger = sel.locator('.p-select-dropdown');
  await trigger.click({ force: true });
  await sleep(400);
  await page.getByRole('option', { name: optionTextRegex }).click();
  await sleep(250);
}

(async () => {
  const ts = Date.now();
  const wsSlug = `e2e${ts}`;
  const wsName = `E2E WS ${ts}`;
  const projKey = `E${String(ts).slice(-6)}`;
  const projName = `E2E Kanban ${ts}`;

  const browser = await chromium.launch({
    headless: false,
    slowMo: 280,
    args: ['--window-size=1360,880', '--window-position=60,32']
  });
  const ctx = await browser.newContext({
    viewport: { width: 1280, height: 820 },
    locale: 'vi-VN'
  });
  const page = await ctx.newPage();

  page.on('console', (msg) => {
    if (msg.type() === 'error') console.log(`  [browser ${msg.type()}] ${msg.text()}`);
  });
  page.on('pageerror', (err) => console.log(`  [browser PAGEERROR] ${err.message}`));

  console.log(`Base URL: ${BASE}`);

  // --- Login: sai mật khẩu → ErrorDialog + TraceId ---
  console.log('\nStep 1: Login sai → ErrorDialog');
  await page.goto(`${BASE}/login`, { waitUntil: 'networkidle', timeout: 60000 });
  await sleep(400);
  await shot(page, '01-login');

  await page.locator('input[name="password"]').fill('');
  await page.locator('input[name="password"]').type('WrongPassword!', { delay: 40 });
  await page.locator('button[type="submit"]').click();
  await page.getByText(/Tài khoản hoặc mật khẩu không đúng|invalid credentials|Unauthorized/i).waitFor({
    state: 'visible',
    timeout: 20000
  });
  await shot(page, '02-login-error-dialog');
  await dismissErrorDialog(page);

  // --- Login đúng → Workspaces ---
  console.log('\nStep 2: Login admin → /workspaces');
  await page.locator('input[name="userName"]').fill('');
  await page.locator('input[name="userName"]').type('admin', { delay: 35 });
  await page.locator('input[name="password"]').fill('');
  await page.locator('input[name="password"]').type('Admin@123', { delay: 35 });
  await page.locator('button[type="submit"]').click();
  await page.waitForURL('**/workspaces', { timeout: 30000 });
  await page.waitForLoadState('networkidle');
  await sleep(600);
  await shot(page, '03-workspaces');

  // --- Tạo workspace ---
  console.log('\nStep 3: Tạo workspace');
  await page.getByRole('button', { name: /Tạo workspace/i }).click();
  await sleep(500);
  await page.locator('input[name="name"]').fill(wsName);
  await page.locator('input[name="slug"]').fill(wsSlug);
  await page.locator('p-dialog').getByRole('button', { name: /^Lưu$/i }).click();
  await sleep(1200);
  await shot(page, '04-workspace-created');

  // --- Vào workspace theo slug ---
  console.log('\nStep 4: Mở workspace detail');
  await page.locator(`a.card`).filter({ hasText: `@${wsSlug}` }).click();
  await page.waitForURL(`**/workspaces/${wsSlug}`, { timeout: 15000 });
  await sleep(700);
  await shot(page, '05-workspace-detail');

  // --- Tạo project Kanban ---
  console.log('\nStep 5: Tạo dự án Kanban');
  await page.getByRole('button', { name: /Tạo dự án/i }).click();
  await sleep(500);
  await page.locator('p-dialog input[name="name"]').fill(projName);
  await page.locator('p-dialog input[name="key"]').fill(projKey);
  await pickPrimeSelectOption(page, 'type', /Kanban/i);
  await page.locator('p-dialog').getByRole('button', { name: /^Lưu$/i }).click();
  await sleep(1500);
  await shot(page, '06-project-created');

  // --- Project detail → Board ---
  console.log('\nStep 6: Board Kanban');
  await page.locator('a.proj').filter({ hasText: projKey }).click();
  await page.waitForURL(`**/projects/${projKey}`, { timeout: 15000 });
  await sleep(600);
  // Trang dự án: workflow (hoặc hint) + custom fields theo issue type (seed global).
  await page.getByText(/Acceptance criteria/i).first().waitFor({ state: 'visible', timeout: 25000 });
  await shot(page, '06b-project-jira-overview');
  await page.getByRole('link', { name: /Bảng/i }).click();
  await page.waitForURL(`**/projects/${projKey}/board`, { timeout: 15000 });
  await page.waitForLoadState('networkidle');
  await sleep(1200);
  await shot(page, '07-board-flat');

  // Swimlane theo assignee (native select value)
  const layoutSel = page.locator('select.layout-native');
  if (await layoutSel.count()) {
    await layoutSel.selectOption('assignee');
    await sleep(600);
    await shot(page, '08-board-swimlane-assignee');
  }

  // --- Create issue (topbar) — project đã context từ board ---
  console.log('\nStep 7: Tạo issue từ topbar');
  await page.locator('button[aria-label="Create"]').click();
  await sleep(400);
  await page.locator('p-dialog input[name="summary"]').waitFor({ state: 'visible', timeout: 15000 });
  await page.waitForFunction(
    () => {
      const el = document.querySelector('p-dialog p-select[name="type"]');
      return el && !el.classList.contains('p-disabled');
    },
    null,
    { timeout: 25000 }
  );
  await page.locator('[data-testid="cf-acceptance_criteria"]').waitFor({ state: 'visible', timeout: 20000 });
  await page.locator('[data-testid="cf-acceptance_criteria"]').fill(`E2E AC ${ts}`);
  await pickPrimeSelectOption(page, 'cf-risk_level', /Medium/i);
  await page.locator('p-dialog input[name="summary"]').fill(`Playwright issue ${ts}`);
  await page.locator('p-dialog button[type="submit"]').click({ timeout: 15000 });
  await page.waitForURL(/\/issues\/.+/, { timeout: 20000 });
  await page.locator('[data-testid="cf-acceptance_criteria"]').waitFor({ state: 'visible', timeout: 20000 });
  const acNeedle = `E2E AC ${ts}`;
  await page.waitForFunction(
    (needle) => {
      const el = document.querySelector('[data-testid="cf-acceptance_criteria"]');
      return el && 'value' in el && String(el.value).includes(needle);
    },
    acNeedle,
    { timeout: 25000 }
  );
  await sleep(400);
  await shot(page, '09-issue-detail');

  // --- Bình luận ---
  console.log('\nStep 8: Comment');
  await page.locator('textarea[name="newBody"]').fill(`Bình luận E2E ${ts}`);
  await page.getByRole('button', { name: /^Bình luận$/i }).click();
  await sleep(1200);
  await shot(page, '10-after-comment');

  console.log('\nStep 8b: Theme dark / light (data-theme)');
  await page.locator('button.theme-btn').click();
  await sleep(500);
  {
    const th = await page.evaluate(() => document.documentElement.getAttribute('data-theme'));
    if (th !== 'dark') throw new Error(`expected data-theme=dark, got ${th}`);
  }
  await shot(page, '10b-theme-dark');
  await page.locator('button.theme-btn').click();
  await sleep(400);
  {
    const th = await page.evaluate(() => document.documentElement.getAttribute('data-theme'));
    if (th !== 'light') throw new Error(`expected data-theme=light, got ${th}`);
  }
  await shot(page, '10c-theme-light');

  console.log('\nStep 8c: Xóa comment — PrimeNG ConfirmDialog');
  await page.locator('app-comments-thread').getByRole('button', { name: /^Xóa$/i }).first().click();
  await sleep(500);
  await page.getByRole('button', { name: /^Có$/i }).click();
  await sleep(1000);
  await shot(page, '10d-comment-deleted');

  // --- Attachment ---
  console.log('\nStep 9: Upload attachment');
  const fileInput = page.locator('app-attachment-panel input[type="file"]');
  await fileInput.setInputFiles(UPLOAD_FILE);
  await sleep(2000);
  await shot(page, '11-attachment-listed');

  // --- Workspace: Add member dialog + UserPicker (chỉ thử tìm user, Hủy) ---
  console.log('\nStep 10: Workspace — UserPicker (không submit member)');
  await page.goto(`${BASE}/workspaces/${wsSlug}`, { waitUntil: 'networkidle' });
  await sleep(600);
  await page.getByRole('button', { name: /Thêm thành viên/i }).click();
  await sleep(400);
  const pickerInput = page.locator('.user-picker-input, app-user-picker input').first();
  await pickerInput.click();
  await pickerInput.fill('admin');
  await sleep(800);
  await page.locator('.p-autocomplete-panel li').first().click({ timeout: 8000 }).catch(() => {});
  await sleep(400);
  await shot(page, '12-add-member-picker');
  await page.getByRole('button', { name: /^Hủy$/i }).click();
  await sleep(400);

  // --- Project settings (workflow + custom fields read-only hub) ---
  console.log('\nStep 11a: Project settings');
  await page.goto(`${BASE}/projects/${projKey}/settings`, { waitUntil: 'networkidle' });
  await sleep(700);
  await page.getByText(/Cấu hình dự án|Project settings/i).waitFor({ state: 'visible', timeout: 20000 });
  await shot(page, '13-project-settings');

  // --- Workspace create empty submit → validation ErrorDialog ---
  console.log('\nStep 11b: Workspace — validation ErrorDialog');
  await page.goto(`${BASE}/workspaces`, { waitUntil: 'networkidle' });
  await sleep(500);
  await page.getByRole('button', { name: /Tạo workspace/i }).click();
  await sleep(400);
  await page.locator('p-dialog').getByRole('button', { name: /^Lưu$/i }).click();
  await sleep(900);
  await page.getByRole('dialog').filter({ hasText: /Mã truy vết|Trace|Lỗi|Error/i }).waitFor({
    state: 'visible',
    timeout: 20000
  });
  await shot(page, '14-workspace-validation-error');
  await dismissErrorDialog(page);
  await page.getByRole('button', { name: /^Hủy$/i }).click();
  await sleep(300);

  // --- i18n EN → VI ---
  console.log('\nStep 12: Ngôn ngữ EN / VI');
  await page.locator('.lang-switch button').filter({ hasText: 'EN' }).click();
  await sleep(700);
  await shot(page, '15-lang-en');
  await page.locator('.lang-switch button').filter({ hasText: 'VI' }).click();
  await sleep(500);
  await shot(page, '16-lang-vi');

  // --- Logout ---
  console.log('\nStep 13: Logout');
  await page.getByRole('button', { name: /Profile/i }).click();
  await sleep(400);
  await page.getByRole('menuitem', { name: /Logout/i }).click();
  await page.waitForURL('**/login', { timeout: 15000 });
  await shot(page, '17-logged-out');

  console.log('\n✅ Jira-Clone E2E flow done. Closing browser in 2s…');
  await sleep(2000);
  await browser.close();
})().catch(async (err) => {
  console.error('FAILED:', err);
  process.exit(1);
});

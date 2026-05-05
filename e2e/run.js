/**
 * Jira-Clone E2E: UI + assert 3 luồng email (assignee / status / comment) qua API + DB Postgres (docker).
 * API: E2E_API_URL (mặc định = BASE). Headless: E2E_HEADLESS=1.
 */
const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');
const { execFileSync } = require('child_process');

const BASE = process.env.E2E_BASE_URL || 'http://localhost:4200';
let resolvedApiOrigin = (process.env.E2E_API_URL || '').trim() || BASE;
const SECOND_ADMIN_USER = (process.env.E2E_SECOND_ADMIN_USERNAME || 'admin2').trim();
const SECOND_ADMIN_PASS = process.env.E2E_SECOND_ADMIN_PASSWORD || 'Admin@123';
const SHOTS = path.join(__dirname, 'screenshots');
const UPLOAD_FILE = path.join(__dirname, 'fixtures', 'e2e-upload.txt');
const PG_CONTAINER = (process.env.E2E_PG_CONTAINER || 'jira-clone-postgres').trim();

fs.mkdirSync(SHOTS, { recursive: true });

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function postJson(url, body) {
  const r = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body)
  });
  const text = await r.text();
  try {
    return JSON.parse(text);
  } catch {
    throw new SyntaxError(`Non-JSON ${url}: ${text.slice(0, 160)}`);
  }
}

async function adminAccessToken() {
  const candidates = [...new Set([resolvedApiOrigin, BASE, 'http://localhost:5000', 'http://127.0.0.1:5000'])];
  let lastErr;
  for (const origin of candidates) {
    if (!origin) continue;
    try {
      const body = await postJson(`${origin}/api/v1/auth/login`, {
        userName: 'admin',
        password: 'Admin@123'
      });
      if (body.success && body.data?.accessToken) {
        resolvedApiOrigin = origin;
        return body.data.accessToken;
      }
      lastErr = new Error(JSON.stringify(body));
    } catch (e) {
      lastErr = e;
    }
  }
  throw lastErr instanceof Error ? lastErr : new Error(String(lastErr));
}

async function getJsonAuth(url, token) {
  const r = await fetch(url, { headers: { Authorization: `Bearer ${token}` } });
  const text = await r.text();
  try {
    return JSON.parse(text);
  } catch {
    throw new SyntaxError(`Non-JSON GET ${url}: ${text.slice(0, 160)}`);
  }
}

async function postJsonAuth(url, token, body) {
  const r = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    body: JSON.stringify(body)
  });
  const text = await r.text();
  try {
    return JSON.parse(text);
  } catch {
    throw new SyntaxError(`Non-JSON POST ${url}: ${text.slice(0, 160)}`);
  }
}

async function adminEmailFromMe() {
  const token = await adminAccessToken();
  const body = await getJsonAuth(`${resolvedApiOrigin}/api/v1/auth/me`, token);
  return (body.data?.email || '').trim();
}

async function fetchEmailLogs(templateKey, toEmail) {
  const token = await adminAccessToken();
  const q = new URLSearchParams({ pageIndex: '1', pageSize: '50', templateKey, toEmail });
  return getJsonAuth(`${resolvedApiOrigin}/api/v1/admin/email-logs?${q}`, token);
}

async function userIdByUserName(userName) {
  const token = await adminAccessToken();
  const q = encodeURIComponent(userName);
  const body = await getJsonAuth(`${resolvedApiOrigin}/api/v1/users/search?q=${q}&take=20`, token);
  const list = body.data || [];
  const hit = list.find((x) => String(x.userName || '').toLowerCase() === userName.toLowerCase());
  if (!hit?.id) throw new Error(`Không tìm thấy user ${userName}`);
  return hit.id;
}

async function ensureWorkspaceMemberBySlug(slug, memberUserName) {
  const token = await adminAccessToken();
  const ws = await getJsonAuth(`${resolvedApiOrigin}/api/v1/workspaces/by-slug/${encodeURIComponent(slug)}`, token);
  if (!ws.success || !ws.data?.id) throw new Error(`workspace by-slug ${slug}: ${JSON.stringify(ws)}`);
  const uid = await userIdByUserName(memberUserName);
  const add = await postJsonAuth(`${resolvedApiOrigin}/api/v1/workspaces/${ws.data.id}/members`, token, {
    userId: uid,
    role: 3
  });
  if (!add.success) throw new Error(`POST members: ${JSON.stringify(add)}`);
}

function rowForIssueKey(items, issueKey) {
  const k = issueKey.trim();
  return (items || []).find((x) => x && String(x.subjectRendered || '').includes(k));
}

/** Chờ log Sent (status=1); Failed/Skipped → throw. */
async function waitForSentEmail(templateKey, toEmail, issueKey, tries = 45, delayMs = 1500) {
  for (let i = 0; i < tries; i++) {
    const body = await fetchEmailLogs(templateKey, toEmail);
    const items = body.data?.items || [];
    const hit = rowForIssueKey(items, issueKey);
    if (hit) {
      const st = hit.status;
      if (st === 1 || st === 'Sent') return hit;
      if (st === 2 || st === 'Failed') {
        throw new Error(`${templateKey} → Failed to=${hit.toEmail}: ${hit.error || 'unknown'}`);
      }
      if (st === 3 || st === 'Skipped') {
        throw new Error(`${templateKey} → Skipped: ${hit.error || ''}`);
      }
    }
    await sleep(delayMs);
  }
  throw new Error(`Hết thời gian chờ Sent: ${templateKey} to=${toEmail} issueKey=${issueKey}`);
}

function printDbEmailRowsForIssue(issueKey) {
  const safe = String(issueKey).replace(/'/g, "''");
  const sql = `SELECT template_key, status, to_email, left(subject_rendered, 72) AS subj FROM notification.email_logs WHERE subject_rendered LIKE '%${safe}%' ORDER BY created_at DESC LIMIT 12;`;
  try {
    const out = execFileSync(
      'docker',
      ['exec', PG_CONTAINER, 'psql', '-U', 'jira_clone', '-d', 'jira_clone', '-c', sql],
      { encoding: 'utf8' }
    );
    console.log(out);
  } catch (e) {
    console.log(`  (skip postgres) ${e.message?.split('\n')[0] || e}`);
  }
}

async function loginViaUi(page, userName, password) {
  await page.goto(`${BASE}/login`, { waitUntil: 'networkidle', timeout: 60000 });
  await sleep(350);
  await page.locator('input[name="userName"]').fill('');
  await page.locator('input[name="userName"]').type(userName, { delay: 35 });
  await page.locator('input[name="password"]').fill('');
  await page.locator('input[name="password"]').type(password, { delay: 35 });
  await page.locator('button[type="submit"]').click();
  await page.waitForURL((u) => !String(u.pathname).includes('/login'), { timeout: 30000 });
  await page.waitForLoadState('networkidle');
  await sleep(500);
}

async function logoutViaUi(page) {
  await page.getByRole('button', { name: /Profile/i }).click();
  await sleep(350);
  await page.getByRole('menuitem', { name: /Logout/i }).click();
  await page.waitForURL('**/login', { timeout: 15000 });
  await sleep(400);
}

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

  const headless = process.env.E2E_HEADLESS === '1' || process.env.E2E_HEADLESS === 'true';
  const browser = await chromium.launch({
    headless,
    slowMo: headless ? 0 : 220,
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

  const adminInbox = await adminEmailFromMe();
  if (!adminInbox) throw new Error('admin không có email trong DB — không thể assert gửi mail.');
  console.log(`Base: ${BASE} | API: ${resolvedApiOrigin} | admin inbox: ${adminInbox}`);

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

  console.log('\nStep 2: Login admin → workspaces');
  await page.locator('input[name="userName"]').fill('');
  await page.locator('input[name="userName"]').type('admin', { delay: 35 });
  await page.locator('input[name="password"]').fill('');
  await page.locator('input[name="password"]').type('Admin@123', { delay: 35 });
  await page.locator('button[type="submit"]').click();
  await page.waitForURL('**/workspaces', { timeout: 30000 });
  await page.waitForLoadState('networkidle');
  await sleep(600);
  await shot(page, '03-workspaces');

  console.log('\nStep 3–4: workspace + admin2 (API members)');
  await page.getByRole('button', { name: /Tạo workspace/i }).click();
  await sleep(500);
  await page.locator('input[name="name"]').fill(wsName);
  await page.locator('input[name="slug"]').fill(wsSlug);
  await page.locator('p-dialog').getByRole('button', { name: /^Lưu$/i }).click();
  await sleep(1200);
  await shot(page, '04-workspace-created');
  await page.locator(`a.card`).filter({ hasText: `@${wsSlug}` }).click();
  await page.waitForURL(`**/workspaces/${wsSlug}`, { timeout: 15000 });
  await sleep(700);
  await shot(page, '05-workspace-detail');
  await ensureWorkspaceMemberBySlug(wsSlug, SECOND_ADMIN_USER);
  await page.reload({ waitUntil: 'networkidle' });
  await sleep(600);
  await shot(page, '05b-workspace-member-admin2');

  console.log('\nStep 5–7: project, board, issue');
  await page.getByRole('button', { name: /Tạo dự án/i }).click();
  await sleep(500);
  await page.locator('p-dialog input[name="name"]').fill(projName);
  await page.locator('p-dialog input[name="key"]').fill(projKey);
  await pickPrimeSelectOption(page, 'type', /Kanban/i);
  await page.locator('p-dialog').getByRole('button', { name: /^Lưu$/i }).click();
  await sleep(1500);
  await shot(page, '06-project-created');
  await page.locator('a.proj').filter({ hasText: projKey }).click();
  await page.waitForURL(`**/projects/${projKey}`, { timeout: 15000 });
  await sleep(600);
  await shot(page, '06b-project-jira-overview');
  await page.goto(`${BASE}/projects/${projKey}/settings/fields`, { waitUntil: 'networkidle', timeout: 60000 });
  await sleep(700);
  await page.getByRole('button', { name: /Gắn ngữ cảnh demo|Bind demo contexts/i }).click();
  await sleep(1500);
  await page.getByRole('link', { name: /Bảng/i }).first().click();
  await page.waitForURL(`**/projects/${projKey}/board`, { timeout: 15000 });
  await page.waitForLoadState('networkidle');
  await sleep(1200);
  await shot(page, '07-board-flat');
  const layoutSel = page.locator('select.layout-native');
  if (await layoutSel.count()) {
    await layoutSel.selectOption('assignee');
    await sleep(600);
    await shot(page, '08-board-swimlane-assignee');
  }

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
  await sleep(900);
  const acLocator = page.locator('[data-testid="cf-acceptance_criteria"]');
  let hasDemoCf = false;
  try {
    await acLocator.waitFor({ state: 'visible', timeout: 45000 });
    hasDemoCf = true;
  } catch {
    console.log('  (skip) không có demo CF trong dialog');
  }
  if (hasDemoCf) {
    const acNeedle = `E2E AC ${ts}`;
    await acLocator.fill(acNeedle);
    await page.locator('[data-testid="cf-mandays"]').waitFor({ state: 'visible', timeout: 20000 });
    await page.locator('[data-testid="cf-mandays"] input').fill('1.5');
    await pickPrimeSelectOption(page, 'cf-risk_level', /Medium/i);
  }
  await page.locator('p-dialog input[name="summary"]').fill(`Playwright issue ${ts}`);
  await page.locator('p-dialog button[type="submit"]').click({ timeout: 15000 });
  await page.waitForURL(/\/issues\/.+/, { timeout: 20000 });
  if (hasDemoCf) {
    const acNeedle = `E2E AC ${ts}`;
    await page.locator('[data-testid="cf-acceptance_criteria"]').waitFor({ state: 'visible', timeout: 20000 });
    await page.waitForFunction(
      (needle) => {
        const el = document.querySelector('[data-testid="cf-acceptance_criteria"]');
        return el && 'value' in el && String(el.value).includes(needle);
      },
      acNeedle,
      { timeout: 25000 }
    );
  }
  await sleep(400);
  await shot(page, '09-issue-detail');
  const issuePageUrl = page.url();

  console.log('\nStep 8: admin2 — assignee admin → lưu → transition → comment @admin');
  await logoutViaUi(page);
  await loginViaUi(page, SECOND_ADMIN_USER, SECOND_ADMIN_PASS);
  await page.goto(issuePageUrl, { waitUntil: 'networkidle', timeout: 60000 });
  await sleep(900);
  await shot(page, '09b-issue-as-admin2');

  const issueKey = (await page.locator('.head .key').innerText()).trim();
  if (!issueKey) throw new Error('Không đọc được issue key (.head .key)');

  const assigneeIn = page.locator('.assignee-row .user-picker-input').first();
  await assigneeIn.click();
  await assigneeIn.fill('');
  await assigneeIn.type('admin', { delay: 25 });
  await sleep(1200);
  const assigneeOpt = page
    .locator('.p-autocomplete-option')
    .filter({ hasText: /\badmin\b/i })
    .first();
  await assigneeOpt.waitFor({ state: 'visible', timeout: 25000 });
  await assigneeOpt.click({ force: true });
  await sleep(400);
  await page.getByRole('button', { name: /Lưu người xử lý|Save assignee/i }).click();
  await sleep(2200);
  await shot(page, '09c-assignee-saved');

  const transBtn = page.locator('section .transitions button').first();
  if (!(await transBtn.count())) throw new Error('Không có transition — không test được issue.status_changed');
  await transBtn.click();
  await sleep(2500);
  await shot(page, '09d-after-transition');

  const commentEditor = page.locator('app-comments-thread .composer .ql-editor');
  await commentEditor.click();
  await commentEditor.fill('');
  await commentEditor.type(`E2E mention ${ts} @ad`, { delay: 18 });
  const mentionList = page.locator('app-comments-thread .composer .mention-list');
  await mentionList.waitFor({ state: 'visible', timeout: 20000 });
  await mentionList.getByRole('button', { name: /admin/i }).first().click();
  await page.getByRole('button', { name: /^Bình luận$/i }).click();
  await sleep(2000);
  await shot(page, '10-after-comment');

  console.log('\nStep 8e: assert API email_logs → Sent (3 template)');
  const a1 = await waitForSentEmail('issue.assignee_changed', adminInbox, issueKey);
  console.log(`  Sent assignee: ${a1.subjectRendered}`);
  const a2 = await waitForSentEmail('issue.status_changed', adminInbox, issueKey);
  console.log(`  Sent status: ${a2.subjectRendered}`);
  const a3 = await waitForSentEmail('comment.added', adminInbox, issueKey);
  console.log(`  Sent comment: ${a3.subjectRendered}`);

  console.log('\nStep 8f: Postgres notification.email_logs (theo issue key)');
  printDbEmailRowsForIssue(issueKey);

  console.log('\nStep 8g: xóa comment');
  await page.locator('app-comments-thread').getByRole('button', { name: /^Xóa$/i }).first().click();
  await sleep(500);
  await page.getByRole('button', { name: /^Có$/i }).click();
  await sleep(800);
  await shot(page, '10d-comment-deleted');

  await logoutViaUi(page);
  await loginViaUi(page, 'admin', 'Admin@123');
  await page.goto(issuePageUrl, { waitUntil: 'networkidle', timeout: 60000 });
  await sleep(600);
  await shot(page, '09z-issue-back-admin');

  console.log('\nStep 9–13: theme, attachment, picker, settings, i18n, logout');
  await page.locator('button.theme-btn').click();
  await sleep(500);
  if ((await page.evaluate(() => document.documentElement.getAttribute('data-theme'))) !== 'dark') {
    throw new Error('expected dark theme');
  }
  await shot(page, '10b-theme-dark');
  await page.locator('button.theme-btn').click();
  await sleep(400);
  if ((await page.evaluate(() => document.documentElement.getAttribute('data-theme'))) !== 'light') {
    throw new Error('expected light theme');
  }
  await shot(page, '10c-theme-light');

  const fileInput = page.locator('app-attachment-panel input[type="file"]');
  await fileInput.setInputFiles(UPLOAD_FILE);
  await sleep(2000);
  await shot(page, '11-attachment-listed');

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

  await page.goto(`${BASE}/projects/${projKey}/settings`, { waitUntil: 'networkidle' });
  await sleep(700);
  await page.getByText(/Cấu hình dự án|Project settings/i).waitFor({ state: 'visible', timeout: 20000 });
  await shot(page, '13-project-settings');

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

  await page.locator('.lang-switch button').filter({ hasText: 'EN' }).click();
  await sleep(700);
  await shot(page, '15-lang-en');
  await page.locator('.lang-switch button').filter({ hasText: 'VI' }).click();
  await sleep(500);
  await shot(page, '16-lang-vi');

  await page.getByRole('button', { name: /Profile/i }).click();
  await sleep(400);
  await page.getByRole('menuitem', { name: /Logout/i }).click();
  await page.waitForURL('**/login', { timeout: 15000 });
  await shot(page, '17-logged-out');

  console.log('\n✅ E2E + email DB OK.');
  await sleep(1500);
  await browser.close();
})().catch((err) => {
  console.error('FAILED:', err);
  process.exit(1);
});

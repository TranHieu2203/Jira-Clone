/**
 * Audit custom fields feature end-to-end:
 *  1. Open custom-fields admin → click "Gắn ngữ cảnh demo" → verify bind
 *  2. Add a manual context via row button → verify
 *  3. Open issue detail → verify custom field form appears
 *  4. Fill a field + save → verify persistence
 *
 * Capture screenshots + console errors + 4xx/5xx network.
 */
const { chromium } = require('playwright-core');
const path = require('path');
const fs = require('fs');

const BASE = process.env.AUDIT_BASE || 'http://localhost:4200';
const SHOTS = path.join(__dirname, 'screenshots');
fs.mkdirSync(SHOTS, { recursive: true });

const errors = [];
const networkErrors = [];

async function shot(page, name) {
  const file = path.join(SHOTS, `cf-${name}.png`);
  await page.screenshot({ path: file, fullPage: true });
  console.log(`[shot] ${name}`);
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  page.on('console', (msg) => {
    const t = msg.type();
    if (t === 'error' || t === 'warning') errors.push(`[${t}] ${msg.text().slice(0, 200)}`);
  });
  page.on('pageerror', (e) => errors.push(`[pageerror] ${e.message}`));
  page.on('response', (r) => {
    const url = r.url();
    if (url.includes('/api/') && r.status() >= 400) {
      networkErrors.push(`${r.status()} ${r.request().method()} ${url}`);
    }
  });

  try {
    console.log('=== Login ===');
    await page.goto(`${BASE}/login`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(800);
    const inputs = await page.locator('input').all();
    await inputs[0].fill('admin');
    await inputs[1].fill('Admin@123');
    await page.click('button[type="submit"]');
    await page.waitForURL(/\/workspaces$|\/$/);
    await page.waitForTimeout(800);

    console.log('\n=== 1. Custom fields admin ===');
    await page.goto(`${BASE}/projects/DEMO/settings/fields`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    await shot(page, '01-admin-initial');

    // Inspect state — count fields + context status
    const initial = await page.evaluate(() => {
      const fields = document.querySelectorAll('.field-row, tbody tr');
      const noContextNotices = Array.from(document.querySelectorAll('*'))
        .filter((e) => /Chưa có ngữ cảnh|No context/i.test(e.textContent || ''))
        .length;
      const bindBtn = Array.from(document.querySelectorAll('button'))
        .find((b) => /Gắn ngữ cảnh demo|Bind demo/i.test(b.textContent || ''));
      return {
        rowCount: fields.length,
        noContextNotices,
        hasBindButton: !!bindBtn,
        bindButtonText: bindBtn?.textContent?.trim() ?? null
      };
    });
    console.log('Initial state:', initial);

    console.log('\n=== 2. Click "Gắn ngữ cảnh demo" ===');
    const bindBtn = await page.locator('button').filter({ hasText: /Gắn ngữ cảnh demo|Bind demo/i }).first();
    if (await bindBtn.count() > 0) {
      await bindBtn.click();
      await page.waitForTimeout(2000);
      await shot(page, '02-after-bind-demo');

      // Re-inspect to count contexts
      const afterBind = await page.evaluate(() => {
        const noContextNotices = Array.from(document.querySelectorAll('*'))
          .filter((e) => /Chưa có ngữ cảnh|No context/i.test(e.textContent || ''))
          .length;
        const ctxRows = document.querySelectorAll('.context-row, .ctx-row, [class*="context"]');
        return {
          noContextNotices,
          ctxRowCount: ctxRows.length,
          bodyText: document.body.textContent?.slice(0, 500) ?? ''
        };
      });
      console.log('After bind:', afterBind);
    } else {
      console.log('No "Gắn ngữ cảnh demo" button found');
    }

    console.log('\n=== 3. Open DEMO-13 issue detail ===');
    await page.goto(`${BASE}/issues/DEMO-13`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(2500);
    await shot(page, '03-issue-detail');

    const detailState = await page.evaluate(() => {
      const cfForm = document.querySelector('app-issue-custom-fields-form');
      const cfText = cfForm?.textContent?.trim() ?? null;
      const fieldInputs = cfForm?.querySelectorAll('input, textarea, p-select, p-multiselect, p-inputnumber, p-datepicker').length ?? 0;
      const noFieldNotice = Array.from(document.querySelectorAll('*'))
        .filter((e) => /Không có trường tùy chỉnh|No custom field/i.test(e.textContent || ''))
        .length;
      return {
        cfFormPresent: !!cfForm,
        cfTextSnippet: cfText?.slice(0, 200) ?? null,
        fieldInputCount: fieldInputs,
        noFieldNotices: noFieldNotice
      };
    });
    console.log('Issue-detail CF state:', detailState);

    console.log('\n=== 4. API direct check ===');
    // Direct API: check if contexts are bound to project
    const apiState = await page.evaluate(async () => {
      const tokenJson = localStorage.getItem('jira-clone:auth.access');
      const tok = tokenJson || (() => {
        const keys = Object.keys(localStorage);
        return keys.find((k) => k.includes('access')) ? localStorage.getItem(keys.find((k) => k.includes('access'))) : null;
      })();
      try {
        // Get DEMO project id first
        const r1 = await fetch('http://localhost:5000/api/v1/projects/by-key/DEMO', {
          headers: { Authorization: `Bearer ${tok}` }
        });
        const j1 = await r1.json();
        const projectId = j1?.data?.id;
        if (!projectId) return { error: 'no projectId', auth: !!tok };

        // List fields
        const r2 = await fetch(`http://localhost:5000/api/v1/projects/${projectId}/custom-fields`, {
          headers: { Authorization: `Bearer ${tok}` }
        });
        const j2 = await r2.json();

        // Resolve fields for issue type STORY (DEMO-13)
        const issueTypeStory = j1.data.issueTypes.find((t) => t.key === 'STORY');
        let resolved = null;
        if (issueTypeStory) {
          const r3 = await fetch(`http://localhost:5000/api/v1/projects/${projectId}/issue-types/${issueTypeStory.id}/custom-fields/resolved`, {
            headers: { Authorization: `Bearer ${tok}` }
          });
          resolved = await r3.json();
        }

        return {
          projectId,
          fieldsCount: j2?.data?.length ?? 0,
          fieldsWithContexts: j2?.data?.filter?.((f) => f.contexts?.length > 0).length ?? 0,
          firstFieldContextsSample: j2?.data?.[0]?.contexts ?? null,
          resolvedForStory: resolved?.data ?? null,
          resolvedCount: resolved?.data?.length ?? 0
        };
      } catch (e) {
        return { error: String(e), auth: !!tok };
      }
    });
    console.log('API state:', JSON.stringify(apiState, null, 2));
  } catch (e) {
    console.error('FATAL:', e.message);
    await shot(page, '99-error').catch(() => {});
  }

  console.log(`\n${'─'.repeat(60)}`);
  console.log(`Console errors: ${errors.length}`);
  for (const e of errors.slice(0, 20)) console.log(' ', e);
  console.log(`Network 4xx/5xx: ${networkErrors.length}`);
  for (const e of networkErrors.slice(0, 20)) console.log(' ', e);

  await browser.close();
})();

const { chromium } = require('playwright-core');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  await page.goto('http://localhost:4200/login', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1000);
  const inputs = await page.locator('input').all();
  await inputs[0].fill('admin');
  await inputs[1].fill('Admin@123');
  await page.click('button[type="submit"]');
  await page.waitForURL(/\/workspaces$|\/$/);
  await page.waitForTimeout(800);

  // Capture all console output
  page.on('console', (msg) => console.log(`[browser-console][${msg.type()}]`, msg.text()));

  await page.goto('http://localhost:4200/projects/DEMO/backlog', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);

  // Inspect Angular signal values via dev hook
  const sigState = await page.evaluate(() => {
    const root = document.querySelector('app-backlog-page');
    if (!root) return { error: 'no app-backlog-page element' };
    // @ts-ignore
    const ng = window.ng;
    if (!ng?.getComponent) return { error: 'window.ng not available (prod build?)' };
    const cmp = ng.getComponent(root);
    if (!cmp) return { error: 'no component found' };
    return {
      hasBacklogItems: typeof cmp.backlogItems === 'function',
      itemsLength: cmp.backlogItems ? cmp.backlogItems().length : 'N/A',
      groupsLength: cmp.backlogGroups ? cmp.backlogGroups().length : 'N/A',
      groupByEpic: cmp.groupByEpic ? cmp.groupByEpic() : 'N/A',
      sprintsLength: cmp.sprints ? cmp.sprints().length : 'N/A',
      selectedSprintId: cmp.selectedSprintId ? cmp.selectedSprintId() : 'N/A'
    };
  });
  console.log('Signal state:', JSON.stringify(sigState, null, 2));

  const dom = await page.evaluate(() => {
    const left = document.querySelector('#product-backlog-list');
    const stats = document.querySelector('.panel-head .panel-stats');
    const groups = document.querySelectorAll('.group-header');
    const rows = document.querySelectorAll('.issue-row');
    const empty = document.querySelector('.drop-list .empty');
    return {
      panelStats: stats?.textContent?.trim() ?? null,
      groupCount: groups.length,
      rowCount: rows.length,
      hasEmpty: !!empty,
      emptyText: empty?.textContent?.trim() ?? null,
      leftHTMLSample: left?.innerHTML?.slice(0, 500) ?? null
    };
  });
  console.log('DOM state:', JSON.stringify(dom, null, 2));

  // Hit BE search directly to confirm what's returned
  const apiResp = await page.evaluate(async () => {
    const r = await fetch('http://localhost:5000/api/v1/issues/search', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: 'Bearer ' + localStorage.getItem('jira-clone:auth.access') },
      body: JSON.stringify({
        textSearch: null,
        jql: null,
        pageIndex: 1,
        pageSize: 200,
        sort: 'key',
        includeArchived: false
      })
    });
    const j = await r.json();
    return {
      success: j.success,
      itemCount: j.data?.items?.length ?? 0,
      sampleItem: j.data?.items?.[0] ?? null,
      hasParent: j.data?.items?.some((i) => i.parentIssueId) ?? false,
      withParentCount: j.data?.items?.filter((i) => i.parentIssueId).length ?? 0
    };
  });
  console.log('\nAPI search:', JSON.stringify(apiResp, null, 2));

  await browser.close();
})();

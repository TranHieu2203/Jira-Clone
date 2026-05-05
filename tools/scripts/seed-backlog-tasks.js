/**
 * Seed các task ⬜ còn lại trong docs/BACKLOG.md vào app Jira-Clone đang chạy.
 *
 * - Idempotent: skip nếu summary đã tồn tại (search theo prefix [ID]).
 * - Auth: admin / Admin@123 (mặc định seed của Identity).
 * - Workspace: ENV `SEED_WORKSPACE_SLUG` (default `acme`) — tự tạo nếu thiếu.
 * - Project: ENV `SEED_PROJECT_KEY` (default `DEMO`) — tự tạo nếu thiếu (Scrum, lead = admin).
 * - Tạo 3 Epic (Phase B / C / D), từng Story/Task trỏ ParentIssueId về Epic tương ứng.
 *
 * Run:
 *   node tools/scripts/seed-backlog-tasks.js
 *
 * ENV optional:
 *   SEED_API_URL=http://localhost:5000
 *   SEED_USERNAME=admin
 *   SEED_PASSWORD=Admin@123
 */

'use strict';

const API = (process.env.SEED_API_URL || 'http://localhost:5000').replace(/\/$/, '');
const USER = process.env.SEED_USERNAME || 'admin';
const PASS = process.env.SEED_PASSWORD || 'Admin@123';
const WORKSPACE_SLUG = process.env.SEED_WORKSPACE_SLUG || 'acme';
const WORKSPACE_NAME = process.env.SEED_WORKSPACE_NAME || 'Acme';
const PROJECT_KEY = process.env.SEED_PROJECT_KEY || 'DEMO';
const PROJECT_NAME = process.env.SEED_PROJECT_NAME || 'Jira-Clone Dev Backlog';

let token = '';
let log = (...a) => console.log('[seed]', ...a);
let warn = (...a) => console.warn('[seed][warn]', ...a);
let die = (msg, extra) => {
  console.error('[seed][error]', msg, extra ? JSON.stringify(extra, null, 2) : '');
  process.exit(1);
};

async function http(method, path, body) {
  const r = await fetch(`${API}${path}`, {
    method,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {})
    },
    body: body !== undefined ? JSON.stringify(body) : undefined
  });
  const text = await r.text();
  let json;
  try {
    json = text ? JSON.parse(text) : {};
  } catch {
    die(`Non-JSON response from ${method} ${path}`, { status: r.status, body: text.slice(0, 200) });
  }
  if (!r.ok || (json && json.success === false)) {
    return { ok: false, status: r.status, json };
  }
  return { ok: true, status: r.status, json };
}

async function login() {
  const res = await http('POST', '/api/v1/auth/login', { userName: USER, password: PASS });
  if (!res.ok) die('Login failed', res.json);
  token = res.json.data.accessToken;
  log(`Logged in as ${USER}`);
}

async function ensureWorkspace() {
  const mine = await http('GET', '/api/v1/workspaces/mine');
  if (!mine.ok) die('GET /workspaces/mine failed', mine.json);
  const list = mine.json.data || [];
  let ws = list.find((w) => w.slug === WORKSPACE_SLUG);
  if (ws) {
    log(`Workspace '${WORKSPACE_SLUG}' đã có (${ws.id})`);
    return ws;
  }
  log(`Tạo workspace '${WORKSPACE_SLUG}'...`);
  const create = await http('POST', '/api/v1/workspaces', {
    name: WORKSPACE_NAME,
    slug: WORKSPACE_SLUG,
    description: 'Workspace dùng cho dev backlog',
    avatarUrl: null
  });
  if (!create.ok) die('Create workspace failed', create.json);
  return create.json.data;
}

async function getMe() {
  const me = await http('GET', '/api/v1/auth/me');
  if (!me.ok) die('GET /auth/me failed', me.json);
  return me.json.data;
}

async function ensureProject(workspaceId, leadUserId) {
  const byKey = await http('GET', `/api/v1/projects/by-key/${encodeURIComponent(PROJECT_KEY)}`);
  if (byKey.ok && byKey.json.data) {
    log(`Project '${PROJECT_KEY}' đã có (${byKey.json.data.id})`);
    return byKey.json.data;
  }
  log(`Tạo project '${PROJECT_KEY}' (Scrum)...`);
  const create = await http('POST', '/api/v1/projects', {
    workspaceId,
    name: PROJECT_NAME,
    key: PROJECT_KEY,
    leadId: leadUserId,
    type: 1, // Scrum
    description: 'Tự seed từ docs/BACKLOG.md để theo dõi việc còn lại của Jira-Clone.'
  });
  if (!create.ok) die('Create project failed', create.json);
  return create.json.data;
}

async function getProjectDetail(projectId) {
  const res = await http('GET', `/api/v1/projects/${projectId}`);
  if (!res.ok) die('GET project detail failed', res.json);
  return res.json.data;
}

function pickIssueType(detail, key) {
  const it = (detail.issueTypes || []).find((t) => t.key === key);
  if (!it) die(`Không tìm thấy issue type '${key}' trong project ${detail.key}`);
  return it;
}

async function findIssueByPrefix(projectId, prefix) {
  // Search bằng TextSearch trên summary; cap 200 để tránh sót.
  const res = await http('POST', '/api/v1/issues/search', {
    projectId,
    textSearch: prefix,
    pageIndex: 1,
    pageSize: 200,
    includeArchived: true
  });
  if (!res.ok) {
    warn('Search failed (sẽ tạo mới)', res.json);
    return null;
  }
  const items = res.json.data?.items || [];
  return items.find((i) => i.summary && i.summary.startsWith(prefix)) || null;
}

async function upsertIssue(projectId, issueTypeId, parentId, t) {
  const prefix = `[${t.id}]`;
  const summary = `${prefix} ${t.title}`;
  const existing = await findIssueByPrefix(projectId, prefix);
  if (existing) {
    log(`SKIP ${prefix} (đã tồn tại: ${existing.key})`);
    return existing;
  }
  const body = {
    projectId,
    issueTypeId,
    summary,
    description: t.description,
    priority: t.priority,
    assigneeId: null,
    parentIssueId: parentId || null,
    dueDate: null,
    storyPoints: t.storyPoints || null,
    labels: t.labels,
    customFieldValues: null
  };
  const res = await http('POST', '/api/v1/issues', body);
  if (!res.ok) {
    warn(`Tạo ${prefix} thất bại`, res.json);
    return null;
  }
  log(`CREATE ${prefix} → ${res.json.data.key}`);
  return res.json.data;
}

// ─── Backlog dataset (chốt từ docs/BACKLOG.md ngày 2026-05-05) ─────────────────

const PHASE_B = {
  id: 'PHASE-B',
  title: 'Phase B — Test coverage + Pipeline polish',
  priority: 2,
  labels: ['phase-b', 'epic'],
  description:
    'Bổ sung test coverage cho các module domain logic (Comment / ActivityLog / Attachment / Notification), refactor tech debt FE board, đóng L4 qua R9.\n\nNguồn: docs/BACKLOG.md §7 Phase B.'
};
const PHASE_C = {
  id: 'PHASE-C',
  title: 'Phase C — Jira feature parity (deferred)',
  priority: 3,
  labels: ['phase-c', 'epic'],
  description: 'Các tính năng Jira parity còn lại sau khi đã đóng F1-F5, F7, F8a, F12, F15.\n\nNguồn: docs/BACKLOG.md §7 Phase C.'
};
const PHASE_D = {
  id: 'PHASE-D',
  title: 'Phase D — Enterprise & Ops',
  priority: 2,
  labels: ['phase-d', 'epic'],
  description:
    'Permission scheme custom, public API, automation, workflow/field editor, audit, deploy/CI ops.\n\nNguồn: docs/BACKLOG.md §7 Phase D.'
};

const TASKS = [
  // ─── Phase B ─────────────────────────────────────────────
  {
    epic: 'PHASE-B', id: 'T1', type: 'TASK', priority: 3, storyPoints: 3,
    labels: ['test', 'phase-b', 'deferred-tests'],
    title: 'Unit test Comment module (10–12 test)',
    description:
      'Domain: mention regex, edit-only-by-author, soft delete.\nService: paging + mention extract.\n\nGhi chú: user đã yêu cầu tạm bỏ tests — task giữ làm placeholder để bật lại khi cần.'
  },
  {
    epic: 'PHASE-B', id: 'T2', type: 'TASK', priority: 4, storyPoints: 2,
    labels: ['test', 'phase-b', 'deferred-tests'],
    title: 'Unit test ActivityLog module (8–10 test)',
    description: 'Handler ghi đúng entry cho mỗi event type.'
  },
  {
    epic: 'PHASE-B', id: 'T3', type: 'TASK', priority: 4, storyPoints: 2,
    labels: ['test', 'phase-b', 'deferred-tests'],
    title: 'Unit test Attachment module (8 test)',
    description: 'Domain: max size, content type whitelist (nếu có). Service: storage call mock.'
  },
  {
    epic: 'PHASE-B', id: 'T4', type: 'TASK', priority: 3, storyPoints: 3,
    labels: ['test', 'phase-b', 'deferred-tests'],
    title: 'Unit test Notification module (10 test)',
    description: 'Handler dispatch đúng template + email body interpolation.'
  },
  {
    epic: 'PHASE-B', id: 'T5', type: 'TASK', priority: 2, storyPoints: 8,
    labels: ['test', 'phase-b', 'deferred-tests', 'integration'],
    title: 'Integration test (Testcontainers Postgres) cho Issue + Workflow flow',
    description: 'Cover migration apply + transition end-to-end với DB thật.'
  },
  {
    epic: 'PHASE-B', id: 'R3', type: 'TASK', priority: 3, storyPoints: 5,
    labels: ['refactor', 'phase-b', 'frontend', 'tech-debt'],
    title: 'Refactor frontend/src/app/features/project/board.page.ts (668 dòng → 4 file)',
    description: 'Tách `BoardPollingService`, `BoardFiltersComponent`, `BoardSwimlaneLayout` ra riêng. Pure cleanup, low risk.'
  },
  {
    epic: 'PHASE-B', id: 'R9', type: 'TASK', priority: 2, storyPoints: 8,
    labels: ['refactor', 'phase-b', 'backend', 'reliability'],
    title: 'Domain dispatcher → outbox (idempotent handlers)',
    description:
      '`OutboxingDomainEventDispatcher` để domain events đi qua outbox như integration events. ActivityLog handlers cần idempotent (de-dupe theo event id).\n\nFix L4 — domain event handler khác transaction với producer → có thể loss khi handler fail.'
  },

  // ─── Phase C (deferred) ──────────────────────────────────
  {
    epic: 'PHASE-C', id: 'F6', type: 'STORY', priority: 3, storyPoints: 8,
    labels: ['feature', 'phase-c', 'frontend'],
    title: 'Roadmap (Epic timeline Gantt)',
    description: 'View timeline epic theo `dueDate`. Drag để chỉnh start/end.'
  },
  {
    epic: 'PHASE-C', id: 'F8b', type: 'STORY', priority: 3, storyPoints: 5,
    labels: ['feature', 'phase-c'],
    title: 'CSV Import',
    description:
      'Phức tạp hơn export: validate type key, summary required, parent key resolution, idempotency. Cần dialog upload + preview rows + per-row error report.'
  },

  // ─── Phase D ─────────────────────────────────────────────
  {
    epic: 'PHASE-D', id: 'F11', type: 'STORY', priority: 1, storyPoints: 13,
    labels: ['feature', 'phase-d', 'security', 'enterprise'],
    title: 'Permission scheme custom (replace 4-role fixed)',
    description:
      'Mở khóa multi-tenant/enterprise sale. Hiện đang fixed 4 role qua D6 — cần permission scheme assignable theo project + per-action permission map.\n\nĐề xuất ưu tiên #1 trong BACKLOG.md.'
  },
  {
    epic: 'PHASE-D', id: 'F14', type: 'STORY', priority: 2, storyPoints: 13,
    labels: ['feature', 'phase-d', 'integration', 'api'],
    title: 'Webhook + Public REST API + API token',
    description: 'Integration với external tools. API token CRUD + scope; webhook subscribe events; public REST API có rate-limit + version.'
  },
  {
    epic: 'PHASE-D', id: 'F16', type: 'STORY', priority: 3, storyPoints: 5,
    labels: ['feature', 'phase-d'],
    title: 'Version + Component (Jira features ngách)',
    description: 'CRUD Version (release version) + Component (sub-area trong project). Filter issue theo version/component.'
  },
  {
    epic: 'PHASE-D', id: 'F9', type: 'STORY', priority: 2, storyPoints: 13,
    labels: ['feature', 'phase-d', 'frontend', 'p10'],
    title: 'Workflow Editor UI (drag-drop graph)',
    description: 'Drag-drop graph editor cho workflow. Xem template SOFTWARE_SIMPLE để chuẩn hóa node/edge schema.'
  },
  {
    epic: 'PHASE-D', id: 'F10', type: 'STORY', priority: 2, storyPoints: 13,
    labels: ['feature', 'phase-d', 'frontend', 'p10'],
    title: 'Field Editor UI + Screen / ScreenScheme / IssueTypeScreenScheme',
    description: 'CRUD field / context / order trong UI admin (không chỉ seed + API AddContext). Screen layout đầy đủ như Jira.'
  },
  {
    epic: 'PHASE-D', id: 'F13', type: 'STORY', priority: 2, storyPoints: 13,
    labels: ['feature', 'phase-d', 'security', 'p11'],
    title: '2FA + Password reset + SSO/OAuth',
    description: 'Identity hoàn thiện. 2FA TOTP, password reset flow qua email link, SSO/OAuth (Google + Microsoft tối thiểu).'
  },
  {
    epic: 'PHASE-D', id: 'F17', type: 'STORY', priority: 3, storyPoints: 13,
    labels: ['feature', 'phase-d'],
    title: 'Automation rule (trigger → action engine)',
    description: 'Rule engine: WHEN issue.statusChanged THEN assignee=X / addLabel / sendWebhook ...'
  },
  {
    epic: 'PHASE-D', id: 'O1', type: 'TASK', priority: 3, storyPoints: 5,
    labels: ['ops', 'phase-d', 'docker', 'deferred'],
    title: 'Production Dockerfile multi-stage + secrets management',
    description: 'Defer theo user request 2026-05-05 — không ưu tiên, tập trung feature/quality trước. Mở lại khi sang production-deploy stage.'
  },
  {
    epic: 'PHASE-D', id: 'O2', type: 'TASK', priority: 3, storyPoints: 5,
    labels: ['ops', 'phase-d', 'ci', 'integration'],
    title: 'CI matrix Postgres + Oracle integration test',
    description: 'GitHub Actions matrix Postgres + Oracle với Testcontainers. Phụ thuộc T5 nếu chưa có integration test framework.'
  },
  {
    epic: 'PHASE-D', id: 'O3', type: 'TASK', priority: 4, storyPoints: 3,
    labels: ['ops', 'phase-d', 'docs'],
    title: 'README deploy guide + HTTPS + backup/restore',
    description: 'Hướng dẫn deploy production: reverse proxy (nginx/traefik), HTTPS via Let\'s Encrypt, backup/restore Postgres + MinIO.'
  }
];

// ─── Limitations còn open (3) ─────────────────────────────────────────────────
// Đưa thành Bug để tách khỏi feature/task pipeline.
const LIMITATIONS = [
  {
    epic: 'PHASE-B', id: 'L2', type: 'BUG', priority: 4, storyPoints: 1,
    labels: ['limitation', 'phase-b'],
    title: 'WorkflowProvisioner chỉ chạy lazy khi tạo issue đầu tiên',
    description: 'Cleaner: subscribe `ProjectCreated` event và provision eager. Cần shared events project hoặc cross-module reference. Acceptable MVP.'
  },
  {
    epic: 'PHASE-B', id: 'L7', type: 'BUG', priority: 5, storyPoints: 1,
    labels: ['limitation', 'phase-b', 'frontend', 'wontfix'],
    title: 'Angular template @ ký tự cần escape `{{ \'@\' }}`',
    description: 'Angular 18 control flow conflict với @if/@for. Đã ghi nhớ trong PATTERNS.md. Wontfix.'
  },
  {
    epic: 'PHASE-B', id: 'L13', type: 'BUG', priority: 3, storyPoints: 2,
    labels: ['limitation', 'phase-b', 'i18n'],
    title: 'i18n key vi/en có thể chưa cover hết error từ BE',
    description: 'BE trả `messageKey` mà FE chưa có sẽ fallback show key thô. Audit cần thiết: dump tất cả `messageKey` trả về từ Result, đối chiếu assets/i18n/{vi,en}.json.'
  }
];

// ─── Main ─────────────────────────────────────────────────────────────────────
async function main() {
  log(`API = ${API}`);
  await login();
  const me = await getMe();
  const ws = await ensureWorkspace();
  const project = await ensureProject(ws.id, me.id);
  const detail = await getProjectDetail(project.id);

  const epicType = pickIssueType(detail, 'EPIC');
  const storyType = pickIssueType(detail, 'STORY');
  const taskType = pickIssueType(detail, 'TASK');
  const bugType = pickIssueType(detail, 'BUG');
  const typeMap = { EPIC: epicType.id, STORY: storyType.id, TASK: taskType.id, BUG: bugType.id };

  // Tạo Epics trước
  const epics = {};
  for (const e of [PHASE_B, PHASE_C, PHASE_D]) {
    const created = await upsertIssue(project.id, epicType.id, null, e);
    if (created) epics[e.id] = created;
  }

  // Tạo child issues
  let createdCount = 0;
  let skippedCount = 0;
  for (const t of [...TASKS, ...LIMITATIONS]) {
    const parent = epics[t.epic];
    if (!parent) {
      warn(`Bỏ qua ${t.id} — Epic ${t.epic} chưa có`);
      continue;
    }
    const result = await upsertIssue(project.id, typeMap[t.type], parent.id, t);
    if (result) {
      // Phân biệt skip vs create dựa vào log line — đơn giản đủ dùng
      if (result.summary && result.summary.startsWith(`[${t.id}]`)) {
        // already-existed path also returns the issue, but log đã in SKIP/CREATE rồi
      }
    }
  }

  log('─'.repeat(60));
  log(`Done. Mở http://localhost:4200/projects/${project.key}/backlog để xem.`);
  log(`Hoặc: http://localhost:4200/issues?jql=${encodeURIComponent('project = "' + project.key + '"')}`);
}

main().catch((err) => {
  console.error('[seed][fatal]', err);
  process.exit(1);
});

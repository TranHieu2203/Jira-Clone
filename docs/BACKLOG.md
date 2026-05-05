# Backlog & Session Handoff

> **Mục đích**: file này tập hợp **những việc dang dở + context cần biết** để session sau (hoặc người khác) tiếp tục được mà không phải đọc lại toàn bộ.
>
> **Quan hệ với các file khác**:
> - `docs/PROGRESS.md` = lịch sử + roadmap đầy đủ (past + future).
> - `docs/BACKLOG.md` = **just the unfinished**, prioritized, actionable.
> - `CLAUDE.md` = quy tắc kiến trúc, không đổi.
>
> **Cập nhật lần cuối**: 2026-05-04 (My Issues JQL, email pipeline, workspace add member, E2E email + DB)

---

## 0. Quick start (cho session sau)

### Repo
- Branch chính: `main` (đã merge 9 commit từ `claude/clever-lamport-6c57e9`)
- Worktree đang dùng: `D:\slw\git-project\Jira-Clone\.claude\worktrees\clever-lamport-6c57e9`
- Để chạy code mới nhất: `cd D:\slw\git-project\Jira-Clone && git checkout main` (hoặc dùng worktree hiện tại)

### Docker stack
```bash
cd D:\slw\git-project\Jira-Clone
docker compose -f docker-compose.dev.yml up -d --build
# Services:
#   - postgres (5432, db=jira_clone, user/pass=jira_clone/jira_clone)
#   - api      (5000 → swagger /swagger, health /health/live)
#   - web      (4200, nginx proxy /api/* → api:8080)
```

### Test credentials
- **Admin**: `admin` / `Admin@123` (seeded tự động)
- **Sample data đã có** sau smoke test: workspace `acme`, project `DEMO`, issue `DEMO-1`

### Smoke test (verify stack OK)
```bash
TOKEN=$(curl -s -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"Admin@123"}' \
  | python -c "import sys, json; print(json.load(sys.stdin)['data']['accessToken'])")

curl -s http://localhost:5000/api/v1/workspaces/mine \
  -H "Authorization: Bearer $TOKEN" | python -m json.tool
```

### Build commands
```bash
# Backend
dotnet build Jira-Clone.sln                                # full sln
dotnet test                                                # all unit tests (69 PASS hiện tại)

# Frontend
cd frontend && npx ng build --configuration=development
```

---

## 1. Trạng thái hiện tại (snapshot)

### Modules BE (10+ modules đã có)
| Module | Status |
|---|---|
| BB.* (BuildingBlocks) | ✅ + **BB.Storage** (`IFileStorage` Local/S3); migration runner Oracle |
| Identity | ✅ + `GET /api/v1/users/search`, `GET /api/v1/users/{id}` (picker) + **`IUserEmailLookup`** (cho notification gửi mail) |
| ~~Sample (Product demo)~~ | Đã gỡ khỏi solution + FE |
| **Project** (Workspace + Project + IssueType + IPermissionChecker) | ✅ 19 tests + **add workspace member** insert-only Postgres (tránh concurrency) |
| **Workflow** (Engine + 9 built-in steps + Provisioner) | ✅ 15 tests |
| **CustomField** (EAV + 13 type handlers) | ✅ 20 tests |
| **Issue** (tích hợp 4 module) | ✅ 15 tests + SignalR notify hook (`IIssueRealtimeNotifier`) |
| **Sprint** | ✅ schema `sprint`, API, FE backlog/board/reports (Postgres migration) |
| **Comment** (mention + edit/delete) | ✅ chưa có test |
| **ActivityLog** (domain handlers → `activity_entries`, GET by issue) | ✅ chưa có test |
| **Attachment** (`issue_attachments`, multipart upload, download, delete) | ✅ chưa có test |
| **Notification** | ✅ in-app + **email**: `email_templates`, `email_logs`, Resend, event handlers (assign/status/comment + mention), admin controllers |

### Frontend
- Layout hybrid: top bar 48px + sidebar contextual + breadcrumb
- **Router (đã khớp sidebar / menu)** — trong shell có auth:
  - `/workspaces`, `/workspaces/:slug`
  - `/projects`, `/projects/:projectKey` (overview), `/board`, `/backlog`, `/issues`, `/reports`, `/settings`
  - `/issues` (**My issues** — mặc định JQL `assignee = currentUser()`), `/issues/:issueKey`
  - `/profile` (menu user), `/settings` (cài đặt app: ngôn ngữ + theme)
  - `login` ngoài shell
- Create dialogs: project (**lead** qua `UserPicker`), issue (**assignee** optional + topbar `+`)
- Issue detail: assignee chỉnh qua `UserPicker` + nút lưu
- Comment thread + **Activity** timeline inline trong issue detail
- Board Kanban drag-drop + filter assignee/issue type + **swimlane theo assignee** + dialog chọn transition khi có nhiều transition cùng đích
- Issue detail: **Attachments** (upload / download / xóa file của chính user)
- **Custom field**: context **theo từng project** (không seed global); layout **display_order** trên `CustomFieldContext`; resolve sort theo order + name; domain handler `ProjectCreated` + startup **backfill** cho project cũ; FE create/detail hỗ trợ **Text, Number, Date, Select, Multi-select** (demo seed 5 field)
- Workspace detail: **Add member** qua `UserPicker` + chọn role (BE: insert SQL trên Postgres để tránh lỗi concurrency graph EF)
- Global **My issues** (`/issues`): chỉ issue assign cho user đăng nhập trừ khi user xóa/sửa ô JQL
- Admin (role `Admin`): **Email templates** + **Email logs** (`/admin/...`)
- StatusCacheService resolve status name + category color

### End-to-end đã chạy
- Login → Workspace → Project → Issue (auto-provision workflow) → Transition (qua engine + qua board drag-drop) → Comment
- **Mở rộng** (`e2e/run.js`): login sai (ErrorDialog), tạo workspace, **thêm member admin2 qua API**, board/issue, **admin2**: assignee → save → transition → comment `@admin`; **assert API** email sent + **Postgres** `notification.email_logs` (3 template: assignee, status, comment); theme, attachment, settings validation, i18n, logout. **Playwright**: selector assignee PrimeNG v18 `.p-autocomplete-option`.

---

## 2. Backlog priority — **Ưu tiên cao** (làm trước)

### ✅ P6.polish — Board (MVP)
**Đã có**: filter; transition dialog; polling 30s; swimlane theo assignee. **Defer**: SignalR (P11).

### ✅ BB#12 — Oracle migrations + runner theo provider
**Đã có**: `ProviderAwareMigrationsAssembly` + suffix `_Postgres` / `_Oracle`; migration Oracle trong `Migrations/Oracle/` cho Workflow, Project, CustomField, Issue, Comment, ActivityLog, Attachment; `docker-compose.dev.yml` service **minio** (optional). Script: `tools/scripts/regenerate-oracle-migrations.ps1`.

### ✅ P7.attachment (MVP)
**Đã có**: `BB.Storage` (Local + S3/MinIO), module Attachment, API multipart, FE panel trên issue detail.

### ✅ CustomField — context theo project + “screen” layout + FE kiểu field (slice Jira-like)
**Đã có**:
- `CustomFieldContext.DisplayOrder` + migration Postgres/Oracle; global context cũ (nếu còn) được set `display_order = 1000` để không chen layout project.
- Seed: 5 field định nghĩa **không** gắn context global — `acceptance_criteria`, `risk_level`, `cf_story_points`, `cf_target_date`, `cf_components`.
- `IDemoCustomFieldProjectBinder` + `ProjectCustomFieldProvisioningHandler` (`ProjectCreated`) + `CustomFieldDemoProjectBinderBackfill` (Api.Host) cho mọi project hiện có.
- `ResolveForAsync` sort theo min `DisplayOrder` context khớp + `Name`.
- FE: `issue-custom-fields-form` — Number (`p-inputNumber`), Date (`input type=date`), Multi-select (`p-multiSelect`), giữ Text/Select.

**Chưa làm (vẫn defer đúng P10 đầy đủ)**: Screen / ScreenScheme / IssueTypeScreenScheme trong DB; drag-drop editor layout; CRUD field trong UI admin.

---

## 3. Backlog priority — **Trung bình**

### 🟡 P9 — Search & Filter & Notification (slice ✅)
**Scope search**:
- ~~BB#8 + spec composition~~ ✅
- ~~JQL-lite~~ ✅ `JqlLiteParser`: `assignee = currentUser()`, `assignee = empty`, `status = "<guid>"`, `text ~ "..."`, nối `AND`
- ~~CFV qua indexed columns~~ ✅ `IIssueFieldValueIssueFilter` + `FieldFilters` trên `POST /issues/search`
- **Chưa làm**: JQL `status = "In Progress"` theo tên (cần resolve workflow); JQL `cf[key] = …`

**Scope notification**:
- ~~Module Notification~~ ✅ `in_app_notifications`, API list/unread/mark read + handlers integration events
- ~~IssueAssigneeChanged / IssueStatusChanged / CommentAdded~~ ✅ publish qua `IEventBus` (outbox)
- ~~FE bell~~ ✅ dropdown + badge + mark all read (polling 45s)
- ✅ **Email (MVP)**: Resend + `email_templates` / `email_logs` + admin API + **`IUserEmailLookup`** + auto-send **assignee / status / comment** (mention trong body comment → notify users được parse)
- **Chưa làm / polish**: suppress duplicates; user prefs “email off”; Oracle seed migration parity cho bảng email nếu cần; FE không proxy SignalR khi `ng serve` (chỉ noise, không chặn flow)

### ✅ P8 — Sprint + Backlog (slice ✅)
**Đã có**:
- Module `Sprint` (schema `sprint`): `sprints`, `sprint_issues`, `sprint_commit_lines`; migration Postgres `InitSprint_Postgres`
- API `GET/POST .../projects/{projectId}/sprints`, `active`, `burndown`, `start`, `complete`, add/remove issue, reorder
- Issue search: `issueIds`, `excludeIssueIds`
- FE: route backlog (`backlog.page`) kéo backlog ↔ sprint; board filter **Active sprint**; Reports burndown (SVG) — active sprint hoặc sprint completed gần nhất
**Chưa làm / polish**: Oracle migration Sprint (dùng Migrate Postgres); velocity report; sprint permission riêng

### 🟢 BB#4 — Outbox processor ✅
**Đã có**: `OutboxDbContext` (schema `outbox` / bảng `outbox_messages`), `EfOutboxStore`, `OutboxingEventBus` (`IEventBus` → enqueue), `OutboxProcessorHostedService` (~5s, batch, retry + dead-letter), đăng ký + `EnsureSchema` trong `Api.Host`.

### 🟢 BB#8 — `Specification<T>` pattern ✅
**Đã có**: `ISpecification<T>`, `Specification<T>`, `And()` (parameter replace, không `Invoke`). `IssueRepository.SearchAsync` dùng `IssueSpecifications.From(criteria)`.

---

## 4. Backlog priority — **Thấp** (polish / nice-to-have)

### 🟢 P10 — Workflow Editor UI + Field Editor UI (partial)
**Đã có (slice)**: layout thứ tự field qua `display_order` + bind context theo project + FE nhập nhiều kiểu (xem mục CustomField ✅ ở trên).

**Còn lại**:
- Drag-drop graph editor cho workflow (xem template SOFTWARE_SIMPLE)
- CRUD field / context / order trong UI admin (không chỉ seed + API `AddContext`)
- Screen / ScreenScheme / IssueTypeScreenScheme đầy đủ như Jira (đã defer ở P3)

### 🟢 P11 — Identity hoàn thiện
- Permission scheme configurable (hiện đang fixed 4 role qua D6)
- SignalR realtime cho board + comment
- 2FA, password reset flow

### 🟢 P12 — Production-ready
- ~~CI entry~~ ✅ `.github/workflows/dotnet.yml` (build + test Release trên push/PR `main`/`develop`)
- Production Dockerfile multi-stage build (đã có dev OK)
- CI: matrix Postgres + Oracle **integration test** (Testcontainers — chưa)
- README hướng dẫn deploy
- Secrets management (đang dev mode hardcode JWT signing key)
- Rate limiting tuning
- HTTPS / reverse proxy guide
- Backup / restore guide

### 🟢 Cleanup
- ~~Xóa Sample module (Product) + route `/products`~~ ✅ (2026-05-02)
- ~~Refactor: `ProjectDetailPage` + `GET /projects/by-key/{key}` (member-scoped, 409 nếu key trùng nhiều workspace)~~ ✅
- ~~`IIssueTypeReader.GetAsync` return null~~ ✅ (`GetIssueTypeByIdAsync` + map DTO)
- ~~Status name Issue list khi search cross-project~~ ✅ (`IssueSummaryDto.ProjectId` + FE `StatusCacheService.ensureProjectLoaded` theo từng project trong trang)
- ~~Router thiếu (`/profile`, `/settings`, backlog/reports project)~~ ✅
- ~~Refactor `confirm()` native trong CommentsThread → dùng PrimeNG ConfirmDialog~~ ✅

---

## 5. Limitations / Known issues

| # | Issue | Workaround / Note |
|---|---|---|
| L1 | ~~`Project.NextIssueNumber` race condition~~ | ✅ **C5 done** — `IsConcurrencyToken()` + retry loop 10 lần trong `IssueNumberAllocator`. Multi-DB. |
| L2 | WorkflowProvisioner lazy (chỉ chạy khi tạo issue đầu tiên) | Cleaner: subscribe `ProjectCreated` event và provision eager. Cần shared events project hoặc cross-module reference |
| L3 | Permission check chưa enforce trên mọi endpoint | JWT auth có (chỉ check authenticated). Per-action permission check qua `IPermissionChecker` chưa wire vào controller. Dùng `[Authorize(Policy=...)]` hoặc service-level guard |
| L4 | Domain event handler khác transaction với producer | ActivityLog có thể loss nếu handler fail. BB#4 ✅ cho **integration** events (`IEventBus`); domain dispatch (`IDomainEventDispatcher`) chưa ghi outbox |
| L5 | Issue search không filter được custom field | ✅ slice: `FieldFilters` + `IIssueFieldValueIssueFilter`; JQL `cf[key]` chưa |
| L6 | ~~Status name không resolve cho cross-project search~~ | ✅ Đã preload workflow theo `projectId` trên mỗi dòng list + giữ cờ fixed project |
| L7 | FE template `@` ký tự cần escape `{{ '@' }}` | Angular 18 control flow conflict. Đã ghi nhớ |
| L8 | Default workflow `SOFTWARE_SIMPLE` cứng | OK MVP. P10 cho user tự design workflow |
| L9 | ~~Attachment chưa có~~ | ✅ Đã có BB.Storage + module Attachment (MinIO optional) |
| L10 | ~~Email auto-send~~ | ✅ Đã có lookup email + gửi theo event (assign/status/comment + mention). Cần polish: prefs người dùng, retry DLQ, template versioning |
| L11 | ~~Sample module (Product)~~ | ✅ Đã gỡ |
| L12 | ~~Dark mode toggle không có UI~~ | ✅ Topbar nút ☾/☀ + `ThemeService` + localStorage |
| L13 | i18n message keys vi/en có thể chưa cover hết error từ BE | Audit cần thiết — BE trả `messageKey` mà FE chưa có sẽ fallback show key thô |
| L14 | Test coverage: chỉ 69 unit tests, 0 integration test với DB thật | Defer cùng BB#12 (cần Testcontainers) |
| L15 | ~~Issue API không scope theo `project_members`~~ | ✅ **Đã fix hoàn toàn**: (1) Issue endpoints — `IIssueProjectAccess` + `IssueSearchCriteria.AccessibleProjectIds`; search chỉ trong project user là member; `projectId` lạ → 403 `issue.search.project_access_denied`; GET by id/key, children, create/update/delete/archive/transition/watchers đều check membership (403/404 thống nhất). (2) **Comment/Attachment/Activity** — `IIssueAccessGuard` chặn cross-project lookup qua `issueId`; trả 404 thống nhất khi issue not exists OR user not member (chống enumeration). |

---

## 6. Quyết định kiến trúc đã chốt (D1–D6, không đổi)

Xem `docs/PROGRESS.md §8`. Quan trọng nhớ:
- **D1** CFV storage: EAV + JSON + indexed columns ✅ đã impl
- **D2** Workflow scope: project-scoped + IsTemplate flag ✅
- **D3** Transition step: strategy pattern + registry ✅
- **D4** Realtime: polling MVP, SignalR P11 ⏳
- **D5** File storage: AWSSDK.S3 + IFileStorage ✅ (`BB.Storage`)
- **D6** Permission: 4 role cố định MVP, scheme P11 ✅ phần MVP

---

## 7. Roadmap đề xuất theo phase (consolidated — 2026-05-05)

> **Mục tiêu**: gộp các backlog ở §2–§4 + L15 + gap phát hiện qua audit thành **4 phase** thực thi tuần tự theo giá trị + dependency. Mỗi task có **ID** (`R*` = refactor/security, `F*` = feature, `T*` = test, `O*` = ops).
>
> Mỗi task có cột:
> - **Effort**: S (≤ 1 buổi) / M (1–2 ngày) / L (3+ ngày)
> - **Status**: ⬜ chưa làm — 🟡 đang làm — ✅ xong
> - **Owner**: ai đang nhận (để trống = chưa assign)

### Phase A — Bảo mật + Production hardening (3–5 ngày) 🔴

| ID | Task | Effort | Status | Ghi chú |
|---|---|---|---|---|
| **R1** | Issue API scope theo `project_members` | S | ✅ | Done — `IIssueProjectAccess` + `IssueSearchCriteria.AccessibleProjectIds` (L15 phần 1) |
| **R1.5** | Mở rộng access guard sang **Comment / Attachment / Activity** controllers | M | ✅ | Done — `IIssueAccessGuard` (Issue.Application) resolve `issueId → IssueAccessSnapshot(IssueId, ProjectId)`, internally uses `IIssueProjectAccess.CanAccessProjectAsync`. Wire vào CommentService (4 method), AttachmentService (4 method), ActivityLogService. Trả 404 thống nhất khi issue not exists OR user not member để chống enumeration. Build + 79 test PASS. |
| **R2** | Wire `IPermissionChecker` per-action | M | ✅ | Done — `PermissionCheckerExtensions.RequireProjectAsync/RequireOrgAsync` + `Result.Failure<T>(Result)` propagate. Wire vào: IssueService (Create/Update/Delete/Archive/Unarchive/Transition/Watcher), CommentService.Create, AttachmentService (Upload/Delete), ProjectService (Create/Update/Archive/Delete/Member×3/IssueType×3), WorkspaceService (Update/Delete/Member×3), WorkflowService (8 method via `EnsureWorkflowAdminAsync` helper), CustomFieldService (AddContext/RemoveContext via `EnsureCanBindContextAsync`). SprintService đã có check sẵn. Email Templates/Logs admin đã có `[Authorize(Roles="Admin")]`. Build + 79 test PASS. |
| **C3** | Rotate Resend API key + chuyển qua env/secret | S | ✅ | Done — `appsettings.Development.json` Resend.ApiKey reset về `""`. Dev set qua `dotnet user-secrets` hoặc env `Resend__ApiKey`. EventEmailDispatcher catch exception → mark log "failed" gracefully. **⚠️ Old key vẫn trong git history — bạn phải rotate tại resend.com.** Xem `docs/SECRETS.md`. |
| **C4** | JWT signing key qua env (không hardcode) | S | ✅ | Done — `appsettings.json` `Jwt.SigningKey = ""`; dev override qua `appsettings.Development.json` (placeholder ≥ 32 bytes). **`SecurityExtensions.AddBbSecurity` fail-fast** nếu key empty hoặc < 32 bytes UTF-8 (RFC 7518). Prod set qua env `Jwt__SigningKey`. Xem `docs/SECRETS.md`. |
| **C5** | Concurrency cho `Project.NextIssueNumber` | S | ✅ | Done — `ProjectDbContext` mark `NextIssueNumber.IsConcurrencyToken()` (multi-DB, Postgres + Oracle). `IssueNumberAllocator` retry loop tối đa 10 lần khi `DbUpdateConcurrencyException`, dùng `IUnitOfWork.DiscardChanges()` (mới thêm vào BB.Persistence) để clear tracker giữa các iteration. Trả `Conflict` nếu vượt retry. **L1 đã đóng.** Build + 79 test PASS. *Note: integration test concurrency thật defer Phase B T5 (cần Testcontainers).* |
| **R4** | Sprint + Notification Oracle migrations | S | ✅ | Done — generated `Migrations/Oracle/InitSprint_Oracle.cs` + `Migrations/Oracle/InitNotification_Oracle.cs` qua `regenerate-oracle-migrations.ps1` (đã fix encoding em-dash bug). Build PASS. *Note: chưa apply thật trên Oracle Free 23 (cần Testcontainers Phase B T5).* |

### Phase B — Test coverage + Pipeline polish (1 tuần) 🟡

| ID | Task | Effort | Status | Ghi chú |
|---|---|---|---|---|
| **T1** | Unit test Comment module (10–12 test) | M | ⬜ | Domain: mention regex, edit-only-by-author, soft delete. Service: paging + mention extract. |
| **T2** | Unit test ActivityLog module (8–10 test) | S | ⬜ | Handler ghi đúng entry cho mỗi event type. |
| **T3** | Unit test Attachment module (8 test) | S | ⬜ | Domain: max size, content type whitelist (nếu có). Service: storage call mock. |
| **T4** | Unit test Notification module (10 test) | M | ⬜ | Handler dispatch đúng template + email body interpolation. |
| **T5** | Integration test (Testcontainers Postgres) cho Issue + Workflow flow | L | ⬜ | Cover migration apply + transition end-to-end với DB thật. |
| **R3** | Refactor `board.page.ts` (668 dòng → 4 file) | M | ⬜ | Tách `BoardPollingService`, `BoardFiltersComponent`, `BoardSwimlaneLayout`. |
| **R6** | Email pipeline: dedupe + opt-out + DLQ admin page | M | ⬜ | `EmailUserPreferences` table; throttle 5min sliding window cùng issue+user; admin page xem failed emails. |
| **R9** | Domain dispatcher → outbox (idempotent handlers) | L | ⬜ | `OutboxingDomainEventDispatcher`; ActivityLog handlers idempotent. Fix L4. |

### Phase C — Jira feature parity (2–3 tuần) 🟢

| ID | Task | Effort | Status | Ghi chú |
|---|---|---|---|---|
| **F1** | JQL-lite mở rộng | M | ✅ | Done — extended `JqlLiteParser` thêm 3 clause: `priority = High` (hoặc số 1-5), `type = "BUG"` (key, resolve sang IssueTypeId qua `IIssueTypeReader.ListByProjectAsync`), `label = "x"` + `label in ("a","b","c")` (AND across labels). `IssueSearchCriteria.RequiredLabels` mới + `IssueSpecifications` filter `i.Labels.Contains(lbl)` (Postgres jsonb-array translate; Oracle CLOB chưa support — known limit). 15 new test PASS (40/40 Issue tests). i18n keys vi/en cho 5 error mới (`duplicate_priority`, `duplicate_type`, `type_requires_project`, `label_in_empty`, `unrecognized_clause`). Status name → workflow resolve đã có sẵn từ trước. |
| **F2** | Saved filter | S | ✅ | Done — `SavedFilter` aggregate trong `Issue.Domain` (đặt cùng module Issue vì gắn JQL). Bảng `saved_filters` schema `issue` (Postgres incremental migration) + Oracle migration consolidated. CRUD API `/saved-filters/{mine,id}` với owner-only modify (404 cho non-owner non-shared để chống enumeration); shared filter cho phép user khác xem + apply. FE: `SavedFilterApiService` + `SavedFilterPickerComponent` (Select dropdown + Dialog "Save current/Edit" + ConfirmDialog xoá) mounted trong `IssuesPage` trên ô JQL. i18n vi/en đầy đủ (`saved_filter.*`). Build BE + FE PASS, 94 test PASS. |
| **F3** | **Issue Link module** (relates/blocks/duplicates/clones) | M | ✅ | Done — `Modules/IssueLink/` 4 layer (Domain/Application/Infrastructure/Api). 5 link type: RelatesTo (đối xứng), Blocks, Duplicates, Clones, Causes (asymmetric pairs với inverse label). Domain events `IssueLinkAdded/Removed`. Wire `IIssueAccessGuard` (cả source + target) + `IPermissionChecker.IssueEdit`. Idempotent unique index `(source, target, type)`. Postgres + Oracle migration. FE: `IssueLinkApiService` + `LinkedIssuesPanelComponent` (PrimeNG AutoComplete search issue + Select link type, list outgoing/incoming với forward/inverse label). Mount trong issue-detail.page. i18n vi/en đầy đủ. Build BE + FE PASS, 79 test PASS. |
| **F4** | Rich text description (Quill editor + mention) | M | ✅ | Already done — `RichTextEditorComponent` (Quill, monochrome theme override) wired vào: `create-issue.dialog`, `issue-detail.page` (edit mode), `comments-thread`. Hỗ trợ `@user` mention với `UserApiService` autocomplete. BE lưu HTML, FE render qua `[innerHTML]` + `isRichHtml()` detector. |
| **F5** | Bulk edit (multi-select issues + batch update) | M | ✅ | Done — `IssueService.BulkUpdateAsync(BulkUpdateRequest)` partial-success: applies assignee (set/clear), priority, labels add/remove, archive/unarchive cho ≤100 issue per batch. Per-issue check `IIssueProjectAccess` + `IPermissionChecker.IssueEdit` (cached per project). Trả `{succeeded, failed[{id, messageKey}]}`. Endpoint `POST /api/v1/issues/bulk-update`. FE: checkbox column + select-all + selection signal trong `IssuesPage`; sticky `BulkEditToolbarComponent` hiện khi ≥1 issue selected → mở dialog (UserPicker assignee + clear flag, priority Select, add/remove labels comma-separated, archive yes/no/noop) → ConfirmDialog → BE call → toast partial nếu có lỗi. i18n vi/en đầy đủ (`issue.bulk.*`). Status transition cố tình defer vì cần per-issue workflow validation. Build BE 0/0, ng build OK, 94 test PASS. |
| **F6** | Roadmap (Epic timeline Gantt) | L | ⬜ | View timeline epic theo `dueDate`. |
| **F7** | Velocity report | S | ✅ | Done — `SprintService.GetVelocityAsync(projectId, count)` + `VelocityReportDto` + endpoint `GET /projects/{projectId}/sprints/velocity?count=6`. Per-sprint: committed = sum `SprintCommitLine.BurndownPoints`; completed = sum points của issue reach Done category trước EndDate (qua `IActivityEntryRepository.ListIssueStatusChangesForIssuesAsync` + workflow status category). Average = mean completed across sprints có data. FE: `SprintApiService.velocity()` + bar chart SVG (overlay committed mờ + completed đậm) trong `project-reports.page` cạnh Burndown, scale chart width theo số sprint. i18n vi/en (`reports.velocity_*`). Build BE 0/0, ng build OK. |
| **F8a** | CSV Export | S | ✅ | Done — `IssueService.ExportSearchAsCsvAsync(SearchIssuesRequest)` reuses SearchAsync với `pageSize=5000` (cap), enrich type name + status name per-project (1 lookup mỗi project, no N+1). RFC 4180 quote escape, UTF-8 BOM cho Excel. Endpoint `POST /issues/export.csv` trả `text/csv` file. FE: nút "Export CSV" trên IssuesPage → blob download via `URL.createObjectURL`. Cùng filter (textSearch, jql) với search hiện tại. i18n vi/en (`issue.export.button`). |
| **F8b** | CSV Import | M | ⬜ | Defer — phức tạp hơn (validate type key, summary required, parent key resolution, idempotency). Cần dialog upload + preview rows + per-row error report. |

### Phase D — Enterprise (3–4 tuần) 🟢

| ID | Task | Effort | Status | Ghi chú |
|---|---|---|---|---|
| **F9** | Workflow Editor UI (drag-drop graph) — **P10** | L | ⬜ | |
| **F10** | Field Editor UI + Screen/ScreenScheme/IssueTypeScreenScheme — **P10** | L | ⬜ | |
| **F11** | Permission scheme custom (replace 4-role fixed) — **P11** | L | ⬜ | |
| **F12** | SignalR realtime (board + comment + issue + attachment + link) | M | ✅ | Done — BE: `WorkspaceHub` (JoinProject/JoinIssue groups) + `SignalRIssueRealtimeNotifier` đã có sẵn. Bổ sung emit `IssueThreadRealtimeEvent`/`IssueBoardRealtimeEvent` cho 3 service mới: `IssueService.UpdateAsync` ("updated"), `AttachmentService.Upload/Delete` ("attachment"), `IssueLinkService.Create/Delete` ("link", emit cho cả source+target). FE: `WorkspaceHubService` đã có connect/reconnect/JWT-via-query (`accessTokenFactory`). Wire listener vào: `IssueDetailPage` (status/assignee/updated → reload issue), `ActivityTimelineComponent` (any event → reload list), `AttachmentPanelComponent` (action="attachment" → reload), `LinkedIssuesPanelComponent` (action="link" → reload). BoardPage + CommentsThread đã listen sẵn từ trước. **D4 đã đạt** — multi-user collab feel real-time, polling 30s vẫn giữ làm fallback. Build BE 0/0, ng build OK, 94 test PASS. |
| **F13** | 2FA + Password reset + SSO/OAuth — **P11** | L | ⬜ | |
| **F14** | Webhook + Public REST API + API token | L | ⬜ | |
| **F15** | Audit log (admin actions, khác Activity log) | M | ⬜ | |
| **F16** | Version + Component | M | ⬜ | |
| **F17** | Automation rule (trigger → action engine) | L | ⬜ | |
| **O1** | Production Dockerfile multi-stage + secrets management | M | ⬜ | |
| **O2** | CI matrix Postgres + Oracle integration test | M | ⬜ | |
| **O3** | README deploy guide + HTTPS + backup/restore | M | ⬜ | |

### Process — Quy tắc khi làm task

1. **Mỗi task = 1 commit (hoặc PR)** — tiêu đề `feat(R1.5): scope comment/attachment/activity by project`.
2. **Cập nhật bảng phase này** — đổi `⬜ → 🟡 → ✅` mỗi khi task chuyển trạng thái. Dán commit hash vào cột "Ghi chú" khi xong.
3. **Khi xong toàn phase** → ghi vào `PROGRESS.md §9 History` 1 dòng.
4. **Nếu phát hiện task mới** trong khi làm → thêm vào phase tương ứng + ID tiếp theo (vd. `F18`).
5. **Test trước, code sau (TDD nhẹ)** với task ID `T*` hoặc bất cứ task nào touch domain logic.
6. **Build phải PASS** trước khi commit (`dotnet build` + `npx ng build` cho FE thay đổi).
7. **Update BACKLOG.md** trong cùng commit khi đổi status.

### Next action (ưu tiên #1)

> ✅ Phase A đóng (7 task). Phase B test (T1-T5) tạm bỏ.
>
> ✅ Phase C: F1 + F2 + F3 + F4 + F5 + F7 + F8a xong (7/9 — F6 + F8b defer).
>
> ✅ Phase D bắt đầu: **F12 (SignalR realtime)** xong (1/12).
>
> Tiếp theo theo độ ưu tiên:
> - **F15** Audit log admin (M) — track admin actions (delete project, change role, edit workflow…). Khác ActivityLog (per-issue).
> - **R6** Phase B Email pipeline polish (M) — dedupe + opt-out + DLQ admin page.
> - **O1** Production Dockerfile multi-stage (M) — chuẩn bị deploy.
> - **F11** Permission scheme custom (L) — replace 4-role fixed.
> - **F13** 2FA + Password reset + SSO/OAuth (L).
>
> Đề xuất **F15 (Audit log)** — value cao cho admin/security review, effort vừa, leverage outbox sẵn có.
>
> Hoặc **R6 (Email pipeline polish)** — đóng L10 hoàn toàn, ROI cao.

---

## 8. Cập nhật file này khi nào?

- ✅ Khi xong 1 phase / task lớn → đánh dấu trong PROGRESS.md, **xóa khỏi BACKLOG.md**
- ✅ Khi phát hiện limitation/gap mới → thêm vào §5
- ✅ Khi defer 1 sub-task → ghi vào BACKLOG.md với lý do
- ✅ Khi bắt đầu phase mới → cập nhật §7 với recommendation kế tiếp

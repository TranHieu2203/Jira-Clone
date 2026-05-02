# Backlog & Session Handoff

> **Mục đích**: file này tập hợp **những việc dang dở + context cần biết** để session sau (hoặc người khác) tiếp tục được mà không phải đọc lại toàn bộ.
>
> **Quan hệ với các file khác**:
> - `docs/PROGRESS.md` = lịch sử + roadmap đầy đủ (past + future).
> - `docs/BACKLOG.md` = **just the unfinished**, prioritized, actionable.
> - `CLAUDE.md` = quy tắc kiến trúc, không đổi.
>
> **Cập nhật lần cuối**: 2026-05-02 (router đầy đủ + IssueSummary có `projectId` + preload status đa project)

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
| Identity | ✅ + `GET /api/v1/users/search`, `GET /api/v1/users/{id}` (picker) |
| ~~Sample (Product demo)~~ | Đã gỡ khỏi solution + FE |
| **Project** (Workspace + Project + IssueType + IPermissionChecker) | ✅ 19 tests |
| **Workflow** (Engine + 9 built-in steps + Provisioner) | ✅ 15 tests |
| **CustomField** (EAV + 13 type handlers) | ✅ 20 tests |
| **Issue** (tích hợp 4 module) | ✅ 15 tests |
| **Comment** (mention + edit/delete) | ✅ chưa có test |
| **ActivityLog** (domain handlers → `activity_entries`, GET by issue) | ✅ chưa có test |
| **Attachment** (`issue_attachments`, multipart upload, download, delete) | ✅ chưa có test |

### Frontend
- Layout hybrid: top bar 48px + sidebar contextual + breadcrumb
- **Router (đã khớp sidebar / menu)** — trong shell có auth:
  - `/workspaces`, `/workspaces/:slug`
  - `/projects`, `/projects/:projectKey` (overview), `/board`, `/backlog`, `/issues`, `/reports`, `/settings`
  - `/issues`, `/issues/:issueKey`
  - `/profile` (menu user), `/settings` (cài đặt app: ngôn ngữ + theme)
  - `login` ngoài shell
- Create dialogs: project (**lead** qua `UserPicker`), issue (**assignee** optional + topbar `+`)
- Issue detail: assignee chỉnh qua `UserPicker` + nút lưu
- Comment thread + **Activity** timeline inline trong issue detail
- Board Kanban drag-drop + filter assignee/issue type + **swimlane theo assignee** + dialog chọn transition khi có nhiều transition cùng đích
- Issue detail: **Attachments** (upload / download / xóa file của chính user)
- **Custom field**: context **theo từng project** (không seed global); layout **display_order** trên `CustomFieldContext`; resolve sort theo order + name; domain handler `ProjectCreated` + startup **backfill** cho project cũ; FE create/detail hỗ trợ **Text, Number, Date, Select, Multi-select** (demo seed 5 field)
- Workspace detail: **Add member** qua `UserPicker` + chọn role
- StatusCacheService resolve status name + category color

### End-to-end đã chạy
- Login → Workspace → Project → Issue (auto-provision workflow) → Transition (qua engine + qua board drag-drop) → Comment

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

### 🟡 P8 — Sprint + Backlog
**Scope**:
- Module `Sprint`: domain (Sprint entity với name, goal, startDate, endDate, status: Future/Active/Completed; SprintIssue join)
- Service: createSprint, startSprint, completeSprint, addIssue, removeIssue
- FE: backlog page với drag issues vào sprint, sprint board view (giống Kanban nhưng filter theo activeSprint)
- Burndown chart (số story points / số issue done theo ngày)

### 🟡 P9 — Search & Filter & Notification
**Scope search**:
- ~~BB#8: `Specification<T>` pattern~~ ✅ (`BB.Persistence.Specification`, `IssueSpecifications.From` + `IssueRepository.SearchAsync`)
- IssueRepository.SearchAsync mở rộng filter qua spec composition (custom field / JQL-lite — tiếp)
- JQL-lite parser (đơn giản): `assignee = currentUser() AND status = "In Progress"`
- IssueFieldValue lookup bằng IndexedString/Number/Date columns đã có

**Scope notification**:
- Module `Notification`: domain (Notification entity, recipientId, type, payload, isRead)
- Subscribe events: IssueAssigneeChanged (notify new assignee), CommentAdded (notify watchers + mentions), IssueStatusChanged (notify watchers)
- FE: notification bell trên topbar (đã có icon, chưa có dropdown), badge count, mark-as-read

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
- Production Dockerfile multi-stage build (đã có dev OK)
- CI: GitHub Actions matrix (Postgres + Oracle integration test)
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
| L1 | `Project.NextIssueNumber` race condition khi 2 user tạo issue đồng thời | Optimistic concurrency chưa add. Low traffic OK. Sửa: thêm `[ConcurrencyCheck]` hoặc dùng PostgreSQL sequence |
| L2 | WorkflowProvisioner lazy (chỉ chạy khi tạo issue đầu tiên) | Cleaner: subscribe `ProjectCreated` event và provision eager. Cần shared events project hoặc cross-module reference |
| L3 | Permission check chưa enforce trên mọi endpoint | JWT auth có (chỉ check authenticated). Per-action permission check qua `IPermissionChecker` chưa wire vào controller. Dùng `[Authorize(Policy=...)]` hoặc service-level guard |
| L4 | Domain event handler khác transaction với producer | ActivityLog có thể loss nếu handler fail. BB#4 ✅ cho **integration** events (`IEventBus`); domain dispatch (`IDomainEventDispatcher`) chưa ghi outbox |
| L5 | Issue search không filter được custom field | Index columns đã có; BB#8 ✅ — vẫn cần compose spec với CFV |
| L6 | ~~Status name không resolve cho cross-project search~~ | ✅ Đã preload workflow theo `projectId` trên mỗi dòng list + giữ cờ fixed project |
| L7 | FE template `@` ký tự cần escape `{{ '@' }}` | Angular 18 control flow conflict. Đã ghi nhớ |
| L8 | Default workflow `SOFTWARE_SIMPLE` cứng | OK MVP. P10 cho user tự design workflow |
| L9 | ~~Attachment chưa có~~ | ✅ Đã có BB.Storage + module Attachment (MinIO optional) |
| L10 | Email notification chưa có | Cần SMTP config + template engine. Phase P11+ |
| L11 | ~~Sample module (Product)~~ | ✅ Đã gỡ |
| L12 | ~~Dark mode toggle không có UI~~ | ✅ Topbar nút ☾/☀ + `ThemeService` + localStorage |
| L13 | i18n message keys vi/en có thể chưa cover hết error từ BE | Audit cần thiết — BE trả `messageKey` mà FE chưa có sẽ fallback show key thô |
| L14 | Test coverage: chỉ 69 unit tests, 0 integration test với DB thật | Defer cùng BB#12 (cần Testcontainers) |

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

## 7. Recommended next session start

**Open prompt**:
> **P8 Sprint/Backlog**, **BB#4 Outbox processor**, hoặc **P9 search/notifications**.

Optional polish:
> Attachment: preview ảnh/PDF; virus scan; giới hạn loại file. MinIO: tạo bucket `jira-clone` tự động (init container).

---

## 8. Cập nhật file này khi nào?

- ✅ Khi xong 1 phase / task lớn → đánh dấu trong PROGRESS.md, **xóa khỏi BACKLOG.md**
- ✅ Khi phát hiện limitation/gap mới → thêm vào §5
- ✅ Khi defer 1 sub-task → ghi vào BACKLOG.md với lý do
- ✅ Khi bắt đầu phase mới → cập nhật §7 với recommendation kế tiếp

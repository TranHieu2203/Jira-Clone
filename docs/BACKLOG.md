# Backlog & Session Handoff

> **Mục đích**: file này tập hợp **những việc dang dở + context cần biết** để session sau (hoặc người khác) tiếp tục được mà không phải đọc lại toàn bộ.
>
> **Quan hệ với các file khác**:
> - `docs/PROGRESS.md` = lịch sử + roadmap đầy đủ (past + future).
> - `docs/BACKLOG.md` = **just the unfinished**, prioritized, actionable.
> - `CLAUDE.md` = quy tắc kiến trúc, không đổi.
>
> **Cập nhật lần cuối**: 2026-05-02

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
dotnet test                                                # all unit tests (54 PASS hiện tại)

# Frontend
cd frontend && npx ng build --configuration=development
```

---

## 1. Trạng thái hiện tại (snapshot)

### Modules BE (9 modules đã có)
| Module | Status |
|---|---|
| BB.* (BuildingBlocks) | ✅ 9/12 items, defer 3 |
| Identity | ✅ + `GET /api/v1/users/search`, `GET /api/v1/users/{id}` (picker) |
| Sample (demo, sẽ xóa sau) | ✅ |
| **Project** (Workspace + Project + IssueType + IPermissionChecker) | ✅ 19 tests |
| **Workflow** (Engine + 9 built-in steps + Provisioner) | ✅ 15 tests |
| **CustomField** (EAV + 13 type handlers) | ✅ 20 tests |
| **Issue** (tích hợp 4 module) | ✅ 15 tests |
| **Comment** (mention + edit/delete) | ✅ chưa có test |
| **ActivityLog** (domain handlers → `activity_entries`, GET by issue) | ✅ chưa có test |

### Frontend
- Layout hybrid: top bar 48px + sidebar contextual + breadcrumb
- 8 feature pages: workspaces (list/detail), projects (list/detail), board, issues (search), issue detail, login
- Create dialogs: project (**lead** qua `UserPicker`), issue (**assignee** optional + topbar `+`)
- Issue detail: assignee chỉnh qua `UserPicker` + nút lưu
- Comment thread + **Activity** timeline inline trong issue detail
- Board Kanban drag-drop + filter assignee/issue type + dialog chọn transition khi có nhiều transition cùng đích
- StatusCacheService resolve status name + category color

### End-to-end đã chạy
- Login → Workspace → Project → Issue (auto-provision workflow) → Transition (qua engine + qua board drag-drop) → Comment

---

## 2. Backlog priority — **Ưu tiên cao** (làm trước)

### 🟡 P6.polish — Board realtime (còn lại)
**Đã có**: filter assignee / issue type trên board; dialog chọn transition khi có nhiều transition cùng target status; **polling 30s** làm mới `issueApi.search` (tạm thời; defer SignalR đến P11).
**Còn làm**:
- Swimlanes (optional)

### 🟡 BB#12 — Oracle migration + per-provider runner
**Hiện trạng**: tất cả module có Postgres migration; Oracle chưa có.

**Scope**:
- Sửa `IDesignTimeDbContextFactory` để filter migration theo provider via `[DbContext(typeof(...))]` + custom `IMigrationsAssembly`
- Hoặc đơn giản hơn: tách 2 folder `Migrations/Postgres/` và `Migrations/Oracle/`, chỉ pickup theo provider runtime
- Generate Oracle migration cho mỗi module

---

## 3. Backlog priority — **Trung bình**

### 🟡 P7.attachment — Attachment module (cần file storage)
**Pre-requisite**: BB#5 (đã chốt D5 — `AWSSDK.S3` + `IFileStorage` abstraction)

**Scope**:
1. `BB.Storage` (mới): `IFileStorage` interface + `LocalFileStorage` (dev) + `S3FileStorage` (prod, MinIO compatible)
2. Add MinIO container vào `docker-compose.dev.yml`
3. `Modules/Attachment/`: domain (Attachment entity với issueId, fileName, contentType, sizeBytes, storageKey), service upload/download/delete
4. FE: drag-drop upload zone trong issue detail + thumbnail preview

### 🟡 P8 — Sprint + Backlog
**Scope**:
- Module `Sprint`: domain (Sprint entity với name, goal, startDate, endDate, status: Future/Active/Completed; SprintIssue join)
- Service: createSprint, startSprint, completeSprint, addIssue, removeIssue
- FE: backlog page với drag issues vào sprint, sprint board view (giống Kanban nhưng filter theo activeSprint)
- Burndown chart (số story points / số issue done theo ngày)

### 🟡 P9 — Search & Filter & Notification
**Scope search**:
- BB#8: `Specification<T>` pattern
- IssueRepository.SearchAsync mở rộng filter qua spec composition
- JQL-lite parser (đơn giản): `assignee = currentUser() AND status = "In Progress"`
- IssueFieldValue lookup bằng IndexedString/Number/Date columns đã có

**Scope notification**:
- Module `Notification`: domain (Notification entity, recipientId, type, payload, isRead)
- Subscribe events: IssueAssigneeChanged (notify new assignee), CommentAdded (notify watchers + mentions), IssueStatusChanged (notify watchers)
- FE: notification bell trên topbar (đã có icon, chưa có dropdown), badge count, mark-as-read

### 🟡 BB#4 — Outbox processor
**Hiện trạng**: `OutboxMessage` entity đã có, processor chưa.

**Scope**: BackgroundService scan outbox table mỗi 5s, publish event qua `IEventBus`, mark processed. Cần khi có integration event cross-module thật (P9 Notification).

### 🟡 BB#8 — Specification<T> pattern
**Scope**: chuẩn cho composable filter trên repos. Cần khi P9 search.

---

## 4. Backlog priority — **Thấp** (polish / nice-to-have)

### 🟢 P10 — Workflow Editor UI + Field Editor UI
- Drag-drop graph editor cho workflow (xem template SOFTWARE_SIMPLE)
- CRUD field type với form config theo từng type
- Screen / ScreenScheme / IssueTypeScreenScheme (đã defer ở P3)

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
- Xóa Sample module (Product) — chỉ là demo từ ngày đầu
- Xóa các route `/products` khỏi FE
- Refactor: hiện `ProjectDetailPage` dùng `listMine()` + filter — nên thêm endpoint `GET /projects/by-key/{key}` (không cần workspaceId) để load trực tiếp
- `IIssueTypeReader.GetAsync` đang return null — implement đầy đủ
- Status name trong IssueList chỉ resolve khi `fixedProjectId` — cross-project search vẫn show UUID
- Refactor `confirm()` native trong CommentsThread → dùng PrimeNG ConfirmDialog

---

## 5. Limitations / Known issues

| # | Issue | Workaround / Note |
|---|---|---|
| L1 | `Project.NextIssueNumber` race condition khi 2 user tạo issue đồng thời | Optimistic concurrency chưa add. Low traffic OK. Sửa: thêm `[ConcurrencyCheck]` hoặc dùng PostgreSQL sequence |
| L2 | WorkflowProvisioner lazy (chỉ chạy khi tạo issue đầu tiên) | Cleaner: subscribe `ProjectCreated` event và provision eager. Cần shared events project hoặc cross-module reference |
| L3 | Permission check chưa enforce trên mọi endpoint | JWT auth có (chỉ check authenticated). Per-action permission check qua `IPermissionChecker` chưa wire vào controller. Dùng `[Authorize(Policy=...)]` hoặc service-level guard |
| L4 | Domain event handler khác transaction với producer | ActivityLog có thể loss nếu handler fail. Outbox cần thiết để fix |
| L5 | Issue search không filter được custom field | Cần `Specification<T>` (BB#8) + index columns đã có |
| L6 | Status name không resolve cho cross-project search | StatusCache hiện chỉ load 1 project. Cần multi-project preload hoặc BE include status name trong DTO |
| L7 | FE template `@` ký tự cần escape `{{ '@' }}` | Angular 18 control flow conflict. Đã ghi nhớ |
| L8 | Default workflow `SOFTWARE_SIMPLE` cứng | OK MVP. P10 cho user tự design workflow |
| L9 | Attachment chưa có | Cần BB.Storage + MinIO container |
| L10 | Email notification chưa có | Cần SMTP config + template engine. Phase P11+ |
| L11 | Sample module (Product) còn trong repo | Xóa khi cleanup |
| L12 | Dark mode toggle không có UI | CSS đã sẵn `[data-theme="dark"]`, chỉ thiếu nút toggle |
| L13 | i18n message keys vi/en có thể chưa cover hết error từ BE | Audit cần thiết — BE trả `messageKey` mà FE chưa có sẽ fallback show key thô |
| L14 | Test coverage: chỉ 69 unit tests, 0 integration test với DB thật | Defer cùng BB#12 (cần Testcontainers) |

---

## 6. Quyết định kiến trúc đã chốt (D1–D6, không đổi)

Xem `docs/PROGRESS.md §8`. Quan trọng nhớ:
- **D1** CFV storage: EAV + JSON + indexed columns ✅ đã impl
- **D2** Workflow scope: project-scoped + IsTemplate flag ✅
- **D3** Transition step: strategy pattern + registry ✅
- **D4** Realtime: polling MVP, SignalR P11 ⏳
- **D5** File storage: AWSSDK.S3 + IFileStorage ⏳ chưa impl
- **D6** Permission: 4 role cố định MVP, scheme P11 ✅ phần MVP

---

## 7. Recommended next session start

**Open prompt**:
> Tiếp tục **BB#12 (Oracle migrations)** hoặc **P7.attachment** (sau khi có BB.Storage). Hoặc P6 swimlanes (optional).

Optional UX:
> Workspace **Add member** dialog — hiện vẫn nhập raw `userId`; có thể tái sử dụng `UserPicker` + API search.

---

## 8. Cập nhật file này khi nào?

- ✅ Khi xong 1 phase / task lớn → đánh dấu trong PROGRESS.md, **xóa khỏi BACKLOG.md**
- ✅ Khi phát hiện limitation/gap mới → thêm vào §5
- ✅ Khi defer 1 sub-task → ghi vào BACKLOG.md với lý do
- ✅ Khi bắt đầu phase mới → cập nhật §7 với recommendation kế tiếp

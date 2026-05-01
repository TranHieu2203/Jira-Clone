# Jira-Clone — .NET 8 + Angular 18 (Modular Monolith)

> Stack: .NET 8 BE (Layered, không CQRS) + Angular 18 FE + PostgreSQL/Oracle (chọn qua config) + PrimeNG monochrome.
> Mọi response có `traceId`. FE: Toast cho success, Dialog cho error (kèm TraceId + nút copy).

📄 **Tham chiếu kiến trúc đầy đủ**: [CLAUDE.md](CLAUDE.md)

---

## 1. Cấu trúc

```
.
├── CLAUDE.md                       # Spec & rule cho AI/tooling
├── Jira-Clone.sln                  # .NET solution
├── Directory.Build.props
├── global.json                     # SDK 8.x
├── src/
│   ├── BuildingBlocks/             # BB.Common, BB.Persistence, BB.Web, BB.Localization,
│   │                               # BB.Logging, BB.Security, BB.EventBus
│   ├── Modules/
│   │   ├── Sample/                 # Domain / Application / Infrastructure / Api
│   │   └── Identity/               # User, Role, RefreshToken, JWT
│   └── Bootstrapper/Api.Host/      # Program.cs, appsettings, Swagger
├── frontend/                       # Angular 18 standalone + signals
├── docker/                         # Dockerfile.api, Dockerfile.web, nginx.conf
├── docker-compose.postgres.yml
├── docker-compose.oracle.yml
└── docker-compose.dev.yml          # api + web + postgres
```

---

## 2. Chạy nhanh (Docker)

### 2.1. Postgres (mặc định) + Api + Web

```bash
docker compose -f docker-compose.dev.yml up -d --build
```

| Service        | URL                          | Ghi chú                           |
| -------------- | ---------------------------- | --------------------------------- |
| Web (FE)       | http://localhost:4200        | Angular qua nginx                 |
| API + Swagger  | http://localhost:5000/swagger| `/api/v1/...`                     |
| Postgres       | localhost:5432               | user/pass/db = `jira_clone`       |
| Adminer (DB UI)| http://localhost:8081        | Khởi tạo qua compose.postgres.yml |

Tài khoản seed mặc định: **admin / Admin@123**

### 2.2. Chỉ DB (dùng dev local)

```bash
docker compose -f docker-compose.postgres.yml up -d
# hoặc
docker compose -f docker-compose.oracle.yml up -d
```

---

## 3. Chạy thủ công (không Docker)

### 3.1. Backend

Cài .NET SDK 8.x.

```bash
dotnet restore
dotnet build
dotnet run --project src/Bootstrapper/Api.Host -- --migrate
```

API: https://localhost:5001 (HTTPS) hoặc http://localhost:5000 (HTTP).

### 3.2. Frontend

Cài Node 20+.

```bash
cd frontend
npm install --legacy-peer-deps
npm start
```

Mở http://localhost:4200.

---

## 4. Switch DB Postgres ↔ Oracle

Sửa `src/Bootstrapper/Api.Host/appsettings.json`:

```json
{
  "Database": {
    "Provider": "Postgres",
    "ConnectionString": "Host=localhost;Port=5432;Database=jira_clone;Username=jira_clone;Password=jira_clone"
  }
}
```

Đổi sang Oracle:

```json
{
  "Database": {
    "Provider": "Oracle",
    "ConnectionString": "User Id=jira_clone;Password=jira_clone;Data Source=localhost:1521/FREEPDB1"
  }
}
```

Hoặc qua biến môi trường:

```bash
export Database__Provider=Oracle
export Database__ConnectionString="User Id=...;Password=...;Data Source=..."
```

> ⚠️ Migration được sinh riêng từng provider. Để bootstrap migration mới:
> `dotnet ef migrations add Init --project src/Modules/Sample/Sample.Infrastructure --context SampleDbContext --output-dir Migrations/Postgres -- Postgres`
> Tương tự cho Oracle: thay `Postgres` bằng `Oracle` cuối câu.

---

## 5. Convention TraceId — debug khi user báo lỗi

1. User báo lỗi → copy **TraceId** từ ErrorDialog (nút Copy).
2. Search log:
   ```bash
   grep <traceId> logs/app-*.log
   ```
3. Mỗi log line đã có `[TraceId]` enricher. `Activity.Current.TraceId` cũng được set, sẵn sàng tích hợp OpenTelemetry.
4. Header `X-Trace-Id` được forward giữa các service (DelegatingHandler có thể bổ sung khi tách microservice).

---

## 6. Thêm module mới — checklist 10 bước

1. `mkdir src/Modules/<Name>/<Name>.{Domain,Application,Infrastructure,Api}`.
2. Tạo 4 csproj (xem `Sample` làm template), khớp `ProjectReference` lên `BuildingBlocks`.
3. Domain: thêm Entity (kế thừa `AuditableEntity`), domain exception nếu cần.
4. Application: DTO, Validator (FluentValidation), Service interface + impl trả `Result<T>`.
5. Infrastructure: `DbContext` riêng (schema riêng), Repository, `IDesignTimeDbContextFactory` cho ef migrations.
6. Api: Controller mỏng kế thừa `BaseController`, gọi service và `ToResponse(...)`.
7. Module DI: extension method `Add<Name>Module(IServiceCollection, IConfiguration)`.
8. Add vào `Jira-Clone.sln`, add `ProjectReference` cho `Api.Host`.
9. `Program.cs`: `builder.Services.Add<Name>Module(builder.Configuration);`.
10. Migration: `dotnet ef migrations add Init --project ... -- Postgres` (và Oracle).

---

## 7. Thêm base UI component

`frontend/src/app/shared/ui/app-<name>.component.ts` — wrap PrimeNG, dùng `input()` signal, theme qua CSS variables ở `styles/theme.scss`. Demo trong `features/sample/products.page.ts`.

> **Trạng thái hiện tại**: scaffold sử dụng PrimeNG component trực tiếp trong feature pages (theming áp qua SCSS overrides). Việc gói thành 25 wrappers chuẩn (button, input, table, ...) là việc tăng dần — tham khảo Section 3.5 của [CLAUDE.md](CLAUDE.md).

---

## 8. Thêm ngôn ngữ mới

1. Thêm `frontend/src/assets/i18n/<lang>.json` với cùng structure key.
2. Thêm `src/BuildingBlocks/BB.Localization/Resources/<lang>.json` cho backend.
3. `environment.ts`: thêm vào `supportedLangs`.
4. Cập nhật language switcher trong `app-shell.component.ts`.

---

## 9. Response format

**Success:**
```json
{
  "success": true,
  "data": { "id": "...", "name": "..." },
  "messageKey": "product.created.success",
  "messageArgs": { "name": "Sản phẩm A" },
  "errors": null,
  "traceId": "0HMV...",
  "timestamp": "2026-05-01T08:00:00Z"
}
```

**Error:**
```json
{
  "success": false,
  "data": null,
  "messageKey": "product.create.failed",
  "errors": [{ "code": "PRODUCT_NAME_REQUIRED", "messageKey": "validation.required", "field": "name" }],
  "traceId": "0HMV...",
  "timestamp": "2026-05-01T08:00:00Z"
}
```

---

## 10. Ghi chú lộ trình

- ✅ BuildingBlocks (Common, Persistence, Web, Localization, Logging, Security, EventBus skeleton).
- ✅ Sample module CRUD end-to-end (Postgres ready; Oracle migration cần generate).
- ✅ Identity module + JWT + seed admin.
- ✅ FE: shell, interceptors (TraceId / Auth / ApiResponse / Error), i18n vi/en, ErrorDialog với TraceId, login + product CRUD.
- ✅ Docker: API image (238MB) + Web image (50MB), compose Postgres / Oracle / dev.
- ⬜ Tạo migration `dotnet ef migrations add Init` cho Postgres + Oracle (chạy thủ công khi DB sẵn sàng).
- ⬜ Test BE: xUnit + Testcontainers Postgres/Oracle (matrix CI).
- ⬜ Test FE: Vitest + Playwright E2E.
- ⬜ 25 base UI wrappers (`app-input`, `app-table`, ...).
- ⬜ Outbox processor (Hangfire), Cache abstraction, Rate-limit policies tinh chỉnh.
- ⬜ CI GitHub Actions.

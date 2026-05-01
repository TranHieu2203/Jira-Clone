# Jira-Clone — AI Build Rules

> **Stack**: .NET 8 + Angular 18 (Modular Monolith → Microservices-ready)
> **DB**: PostgreSQL **hoặc** Oracle (chọn qua config), không CQRS — dùng Service Layer thuần.
> Mọi request có `TraceId`. FE hiển thị success bằng Toast, error bằng Dialog (kèm TraceId + message). Multi-language vi/en.

AI làm việc trong repo này PHẢI tuân thủ các quy tắc dưới đây. Khi sinh code mới hoặc sửa code cũ, bám sát các convention, kiến trúc, response format, và yêu cầu cross-cutting đã định nghĩa.

---

## 1. Mục tiêu & ràng buộc

- **Modular Monolith** ngay từ đầu, mỗi module độc lập domain, dễ tách microservice sau này (Outbox + Event Bus abstraction sẵn).
- Backend hỗ trợ **PostgreSQL** và **Oracle** — switch qua `appsettings.json`, không sửa domain/business code.
- Frontend **Angular 18+** (standalone, signals, control flow mới) + **PrimeNG v18**, theme **trắng/đen** monochrome.
- **Không dùng CQRS/MediatR** — kiến trúc layered cổ điển: `Controller → Service → Repository`. Đơn giản, dễ debug, dễ onboard team.
- **TraceId** xuyên suốt: HTTP header → log → response → FE hiển thị khi lỗi.
- **i18n** chuẩn ngay từ nền: backend trả `messageKey`, FE translate theo locale hiện tại.

---

## 2. Backend — .NET 8 (Layered Architecture, KHÔNG CQRS)

### 2.1. Triết lý kiến trúc

**Layered + Clean Architecture nhẹ**, mỗi module có 4 layer:

```
Domain        ← Entity, Value Object, Domain Event, Domain Exception (no dependency)
Application   ← Service, DTO, Validator, Interface (chỉ phụ thuộc Domain)
Infrastructure← EF Core, Repository impl, External clients (phụ thuộc Application + Domain)
Api           ← Controller, Endpoint, Module DI registration (phụ thuộc Application)
```

**Service Layer** thay cho CQRS:
- Mỗi entity nghiệp vụ có 1 `XxxService` interface (Application) + impl (Application).
- Service trả về `Result<T>` (success/failure + error code + message key).
- Validator (FluentValidation) gọi **bên trong** Service trước khi xử lý — không cần MediatR pipeline.
- Transaction quản lý ở Service qua `IUnitOfWork`.
- Service KHÔNG biết HTTP — Controller mỏng, chỉ map request/response và gọi service.

### 2.2. Solution structure

```
src/
├── BuildingBlocks/
│   ├── BB.Common/                 # Result<T>, PagedList<T>, BaseEntity, IAuditable, IEntityWithTrace
│   ├── BB.Persistence/            # IUnitOfWork, IRepository<T>, BaseDbContext, ValueConverters
│   ├── BB.EventBus/               # IIntegrationEvent, IEventBus (in-memory), Outbox abstractions
│   ├── BB.Web/                    # ApiResponse<T>, GlobalExceptionHandler, TraceIdMiddleware,
│   │                              #   CorrelationContext, BaseController, ProblemDetailsFactory
│   ├── BB.Localization/           # ILocalizer, resource loaders (JSON-based, vi.json/en.json)
│   ├── BB.Logging/                # Serilog config, TraceId enricher
│   └── BB.Security/               # JWT helpers, CurrentUser accessor
├── Modules/
│   ├── Identity/
│   │   ├── Identity.Domain/        # User, Role, RefreshToken
│   │   ├── Identity.Application/   # IAuthService, AuthService, LoginDto, validators
│   │   ├── Identity.Infrastructure/# IdentityDbContext, UserRepository, JwtTokenGenerator
│   │   └── Identity.Api/           # AuthController, IdentityModule.cs (extension method DI)
│   └── Sample/
│       ├── Sample.Domain/          # Product entity
│       ├── Sample.Application/     # IProductService, ProductService, ProductDto, validator
│       ├── Sample.Infrastructure/  # SampleDbContext, ProductRepository, Migrations/{Postgres,Oracle}
│       └── Sample.Api/             # ProductController, SampleModule.cs
├── Bootstrapper/
│   └── Api.Host/                   # Program.cs, appsettings.{Development,Production}.json
tests/
├── UnitTests/                      # Service, validator, domain logic
└── IntegrationTests/               # Testcontainers Postgres + Oracle
```

### 2.3. Request flow chuẩn

```
HTTP Request
   │ (header X-Trace-Id nếu có, không thì sinh mới)
   ▼
TraceIdMiddleware            → set Activity.Current, push vào CorrelationContext
   ▼
Authentication / Authorization
   ▼
Controller (mỏng, ~5-10 dòng)
   │ map request → DTO
   ▼
Application Service
   │ 1. Validate (FluentValidation .ValidateAndThrowAsync)
   │ 2. Business logic
   │ 3. Repository / UnitOfWork
   │ 4. Publish domain/integration event (nếu có) → Outbox
   │ 5. Return Result<T>
   ▼
Controller map Result<T> → ApiResponse<T> (HTTP 200/400/...)
   ▼
GlobalExceptionHandler (nếu exception thoát ra) → ApiResponse error + TraceId
```

### 2.4. Database — chỉ Postgres & Oracle

**Provider switching:**
```json
"Database": {
  "Provider": "Postgres",          // "Postgres" | "Oracle"
  "ConnectionString": "...",
  "CommandTimeout": 30,
  "EnableSensitiveDataLogging": false
}
```

- `BB.Persistence` cung cấp `DbContextOptionsBuilder.UseConfiguredDatabase(IConfiguration)`.
- Mỗi module có 1 `DbContext` riêng — schema riêng để chuẩn bị tách microservice.
- Migrations riêng cho từng provider, đặt trong `Infrastructure/Migrations/Postgres/` và `Infrastructure/Migrations/Oracle/`.
- Dùng `EFCore.NamingConventions` → `snake_case` cho cả 2 DB.
- `ValueConverter` cho `DateTimeOffset`, `Guid`, `enum`, `bool` (Oracle: `NUMBER(1)`).
- **CẤM** trong domain: `JSONB`, `tsvector`, Oracle `XMLTYPE`, raw `MERGE`. Nếu cần → tạo interface `IFullTextSearch` với 2 impl.
- PK mặc định `Guid` (UUID v7 nếu được).
- Pagination: `OFFSET ... FETCH NEXT`.

### 2.5. TraceId / Correlation

1. `TraceIdMiddleware`:
   - Đọc header `X-Trace-Id`; nếu không có → sinh mới.
   - Set `Activity.Current.SetTag("trace_id", ...)` + `HttpContext.TraceIdentifier`.
   - Push vào `ICorrelationContext` (scoped).
   - Add response header `X-Trace-Id`.
2. Serilog enricher: mọi log line có `TraceId`.
3. `ApiResponse<T>` luôn chứa `traceId`.
4. Outgoing HTTP: `DelegatingHandler` forward `X-Trace-Id`.
5. Background jobs / Outbox: lấy TraceId từ message metadata.

### 2.6. Response format chuẩn

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
  "messageArgs": null,
  "errors": [
    { "code": "PRODUCT_NAME_REQUIRED", "messageKey": "validation.required", "field": "name", "args": null }
  ],
  "traceId": "0HMV...",
  "timestamp": "2026-05-01T08:00:00Z"
}
```

**HTTP status mapping**:
- Success → 200/201
- Validation → 400
- Unauthorized → 401, Forbidden → 403, NotFound → 404, Conflict → 409
- Unexpected → 500 kèm full TraceId.

### 2.7. Cross-cutting

- **Validation**: FluentValidation, gọi trong Service. Validator có thể inject `IStringLocalizer`.
- **Logging**: Serilog, Console (dev) + File rolling (prod).
- **Auth**: JWT Bearer + Refresh Token rotation. `ICurrentUser` scoped.
- **Authorization**: Policy-based + permission claims.
- **Caching**: `IDistributedCache` (memory dev, Redis prod-ready) wrap qua `ICacheService`.
- **Background jobs**: `IJobScheduler` abstraction, default Hangfire.
- **Outbox**: `IOutboxStore`, `OutboxProcessor`.
- **Health checks**: `/health/live`, `/health/ready`.
- **API versioning**: `Asp.Versioning.Mvc` URL segment.
- **OpenAPI**: Swashbuckle.
- **Rate limiting**: `Microsoft.AspNetCore.RateLimiting`.
- **CORS**: configurable.
- **i18n backend**: JSON `BB.Localization/Resources/{vi,en}.json`. Service trả `messageKey` — backend KHÔNG translate (FE làm).

### 2.8. Module registration

Mỗi module expose extension method `Add{Name}Module` để Bootstrapper gọi.

---

## 3. Frontend — Angular 18+ + PrimeNG v18

### 3.1. Project structure

```
src/app/
├── core/
│   ├── auth/                       # AuthService, AuthGuard, AuthInterceptor
│   ├── http/                       # interceptors (trace-id, api-response, error, loading)
│   ├── i18n/                       # TranslateService config, language switcher
│   ├── notification/               # NotificationService, ErrorDialog
│   ├── config/
│   └── layout/                     # AppShell, Sidebar, Topbar
├── shared/
│   ├── ui/                         # Base components
│   ├── directives/
│   ├── pipes/
│   └── models/                     # ApiResponse, ApiError types
├── features/
│   ├── identity/                   # login, profile (lazy)
│   └── sample/                     # CRUD sản phẩm (lazy)
├── styles/
└── assets/i18n/{vi,en}.json
```

### 3.2. HTTP layer — success/error

- **TraceIdInterceptor** → forward `X-Trace-Id`.
- **AuthInterceptor** → Bearer token.
- **LoadingInterceptor** → spinner counter.
- **ApiResponseInterceptor**:
  - `success=true` + có `messageKey` → `NotificationService.success(key, args)` + return data.
  - `success=false` → throw `ApiException(errors, traceId, messageKey)`.
- **ErrorInterceptor** → bắt mọi error → `NotificationService.error({ messageKey, traceId, errors })` → mở `ErrorDialog`.

ErrorDialog là **modal blocking** (`p-dialog`), hiển thị title (i18n), message, danh sách errors, **TraceId monospace + nút copy**, nút OK.

Component **KHÔNG** try-catch lỗi chung — interceptor xử trung tâm.
Toast success **chỉ** khi BE có `messageKey`.

### 3.3. i18n

- `@ngx-translate/core` + `@ngx-translate/http-loader`.
- Languages: `vi` (default), `en`.
- File: `assets/i18n/{lang}.json` namespace theo module.
- Language switcher persist `localStorage`, sync `Accept-Language` header.
- Date/number: `Intl` API + Angular `DatePipe` locale động.

### 3.4. State

- **Signals** mặc định.
- `@ngrx/signals` cho feature state phức tạp.
- KHÔNG NgRx classic.

### 3.5. Base components (wrap PrimeNG, monochrome)

`shared/ui/`: app-button, app-input, app-input-number, app-textarea, app-select, app-multi-select, app-tree-select, app-date-picker, app-checkbox, app-radio, app-switch, app-form-field, app-table, app-dialog, app-error-dialog, app-confirm, app-toast, app-card, app-tabs, app-badge, app-chip, app-tag, app-skeleton, app-progress, app-spinner, app-empty-state, app-page-header, app-language-switcher.

API gọn — `input()` signal + `model()` two-way. Không expose toàn bộ PrimeNG props.

### 3.6. Theme monochrome

CSS variables `--c-bg, --c-surface, --c-border, --c-text, --c-primary, --c-on-primary, --c-danger, --c-success`, light + dark.

PrimeNG theming: `@primeng/themes` preset Aura, override semantic tokens về monochrome.
**CẤM**: gradient, drop-shadow màu, accent ngoài đen/trắng/xám (riêng `--c-accent-danger` đỏ `#dc2626` cho action delete/error icon được phép).

Phân biệt trạng thái bằng icon, border weight, typography weight.
Dark mode: attribute `data-theme="dark"` trên `<html>`.

---

## 4. DevEx & Quality

- **.NET**: `Directory.Build.props` bật `TreatWarningsAsErrors`, `Nullable enable`, `ImplicitUsings`. `.editorconfig`, `dotnet format`.
- **Angular**: ESLint + Prettier + Stylelint, `husky` + `lint-staged`, commitlint Conventional Commits.
- **Tests BE**: xUnit + FluentAssertions + Testcontainers (matrix Postgres/Oracle).
- **Tests FE**: Vitest + Angular Testing Library, Playwright E2E.
- **CI**: GitHub Actions — build, lint, test (matrix), docker build, scan CVE.
- **Docker**: `docker-compose.postgres.yml`, `docker-compose.oracle.yml`, `docker-compose.dev.yml`.
- **Taskfile**: `make up-postgres`, `make migrate`, `make test`, `make seed`.

---

## 5. Convention

- **Comment**: tiếng Việt cho business logic, tiếng Anh cho code/identifier/log/exception message.
- **Commit**: Conventional Commits.
- **Naming**: `PascalCase` (C# class/interface), `camelCase` (TS), `kebab-case` (file FE, URL, HTML attr), `SNAKE_CASE` (env var, constants), `snake_case` (DB column).
- **Branch**: `feat/*`, `fix/*`, `chore/*`. Base `develop`, release `main`.

---

## 6. Thứ tự generate

1. Solution + BuildingBlocks.
2. Module `Sample` BE đầy đủ.
3. `Api.Host` + Program.cs + Swagger + integration test.
4. FE: app shell + theme + `NotificationService` + `ErrorDialog` + interceptors + i18n.
5. FE base components + preview route.
6. Feature `Sample` FE end-to-end.
7. Module `Identity` BE + FE.
8. Docker Compose + CI.
9. README + dev guides.

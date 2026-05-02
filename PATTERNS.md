# Patterns & Lessons — Jira-Clone

> Tài liệu để khi code chức năng mới **không bị lệch khỏi rule đã có**.
> Đọc kèm: [CLAUDE.md](CLAUDE.md) (kiến trúc tổng) + [README.md](README.md) (quickstart).
>
> File này tập hợp:
> 1. **Convention đã chốt** (cách viết đúng, có ví dụ thực tế trong repo).
> 2. **Bẫy đã đạp phải** (bug đã fix — không lặp lại).
> 3. **Checklist** khi thêm module / feature page / i18n key / test mới.

---

## 1. Backend — Convention

### 1.1. Cấu trúc module 4 layer

Mỗi module = 4 csproj, dependency một chiều:

```
{Module}.Domain         → BB.Common
{Module}.Application    → {Module}.Domain, BB.Common, BB.Persistence (interfaces only)
{Module}.Infrastructure → {Module}.Application, BB.Persistence, BB.Web
{Module}.Api            → {Module}.Application, {Module}.Infrastructure, BB.Web
                          (Microsoft.NET.Sdk + FrameworkReference Microsoft.AspNetCore.App,
                           KHÔNG dùng Microsoft.NET.Sdk.Web)
```

**Reference làm template**: [src/Modules/Project/](src/Modules/Project). Module Sample/Product demo đã gỡ — copy cấu trúc 4 layer từ Project (hoặc Identity), đổi tên.

### 1.2. Per-module typed UnitOfWork — BẮT BUỘC

**Bug đã đạp**: 2 module cùng register `IUnitOfWork` (open interface) → DI last-wins → `ProductService` thực ra gọi `IdentityDbContext.SaveChanges()` → product không persist mặc dù response trả 200.

**Đúng**: mỗi module định nghĩa **typed UoW** riêng:

```csharp
// Project.Application/IProjectUnitOfWork.cs
public interface IProjectUnitOfWork : IUnitOfWork { }

// Project.Infrastructure/ProjectUnitOfWork.cs
public sealed class ProjectUnitOfWork : UnitOfWork<ProjectDbContext>, IProjectUnitOfWork
{
    public ProjectUnitOfWork(ProjectDbContext ctx) : base(ctx) { }
}

// Project.Api/ProjectModule.cs
services.AddScoped<IProjectUnitOfWork, ProjectUnitOfWork>();
```

Service inject `IProjectUnitOfWork` (typed theo module), **không bao giờ** inject `IUnitOfWork` raw.

Reference: [IProjectUnitOfWork](src/Modules/Project/Project.Application/Repositories/IRepositories.cs), [IIdentityUnitOfWork.cs](src/Modules/Identity/Identity.Application/IIdentityUnitOfWork.cs).

### 1.3. Service trả `Result<T>`, validator gọi bên trong

```csharp
public async Task<Result<ProductDto>> CreateAsync(CreateProductRequest req, CancellationToken ct)
{
    await _createValidator.ValidateAndThrowAsync(req, ct);   // ValidationException → 400 trong handler

    if (await _repo.SkuExistsAsync(req.Sku, null, ct))
    {
        return Result.Failure<ProductDto>(
            ErrorType.Conflict,
            "product.sku_duplicated",
            new[] { new ResultError("PRODUCT_SKU_DUPLICATED", "product.sku_duplicated", "sku") });
    }

    var entity = new Product(req.Name, req.Sku, req.Price, req.Description);
    await _repo.AddAsync(entity, ct);
    await _uow.SaveChangesAsync(ct);

    return Result.Success(
        ToDto(entity),
        messageKey: "product.created.success",
        messageArgs: new { name = entity.Name });
}
```

**Bẫy**: `Result.Success("string")` ambiguous giữa `Success(messageKey)` và `Success<string>(data)`. Luôn **named arg** khi chỉ trả message:
```csharp
return Result.Success(messageKey: "product.deleted.success");
```

### 1.4. Controller mỏng — chỉ map `Result<T>` → `ApiResponse<T>`

```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateProductRequest req, CancellationToken ct) =>
    Created(await _service.CreateAsync(req, ct));   // helper trên BaseController
```

**Không bao giờ** đặt business logic, validation, hoặc try/catch trong controller. `BaseController.ToResponse(...)` map sang ApiResponse + đúng HTTP status.

### 1.5. Schema bootstrap (chưa có migration)

**Bug đã đạp**: `EnsureCreatedAsync()` short-circuit nếu **database** đã tồn tại — context/module tạo schema trước, Identity gọi sau thấy DB tồn tại → skip → seed crash vì `identity.roles` không có.

**Đúng**: dùng `IRelationalDatabaseCreator.CreateTablesAsync()` per-context. Nếu module đã có migration, gọi `MigrateAsync()`. Code mẫu: [Program.cs `EnsureSchemaAsync`](src/Bootstrapper/Api.Host/Program.cs).

> 📝 Khi module sẵn sàng cho prod: `dotnet ef migrations add Init` → từ đó `Migrate` chạy thay vì `CreateTables`.

### 1.6. Repository pattern

- Generic `Repository<T>` ở [BB.Persistence](src/BuildingBlocks/BB.Persistence/Repository.cs) lo CRUD chung.
- Module repo extend nó cho query đặc thù (`SearchAsync`, `FindByXxxAsync`):

  ```csharp
  public sealed class ProductRepository : Repository<Product>, IProductRepository { ... }
  ```

- Repo trả entity, **không** trả DTO. Mapping ở Service.

### 1.7. DbContext per module

- Mỗi module 1 `DbContext`, schema riêng (Postgres `HasDefaultSchema(...)`).
- `BaseDbContext` lo audit + traceId + value converter (DateTimeOffset, bool↔NUMBER cho Oracle).
- **Cấm**: type Postgres-only (`JSONB`, `tsvector`) hoặc Oracle-only (`XMLTYPE`) trong domain. Cần thì viết interface `IFullTextSearch` 2 impl.

### 1.8. Migration

- Đặt trong `{Module}.Infrastructure/Migrations/` (Postgres ở root) và `Migrations/Oracle/` cho chuỗi Oracle.
- **Runtime**: `UseConfiguredDatabase` gắn `DatabaseProviderOptionsExtension` + `ReplaceService<IMigrationsAssembly, ProviderAwareMigrationsAssembly>` — chỉ apply migration có **suffix tên class** `_Postgres` hoặc `_Oracle` (hai history song song).
- Generate riêng theo provider (args cuối `Postgres` / `Oracle` cho factory):
  ```bash
  dotnet ef migrations add InitSomething_Postgres \
    --project src/Modules/Project/Project.Infrastructure \
    --context ProjectDbContext \
    --startup-project src/Bootstrapper/Api.Host -- Postgres
  ```
- Oracle initial: tạm xóa/sao lưu file migration Postgres + snapshot → `dotnet ef migrations add InitSomething_Oracle ... -- Oracle` → chuyển file `*_Oracle*.cs` vào `Migrations/Oracle/` → xóa snapshot sinh ra → khôi phục snapshot Postgres. Script mẫu: `tools/scripts/regenerate-oracle-migrations.ps1`.
- `IDesignTimeDbContextFactory` đọc args / env `DB_PROVIDER` để chọn provider.
- EF Core 8: `IDiagnosticsLogger<DbLoggerCategory.Migrations>` — **`DbLoggerCategory` nằm namespace `Microsoft.EntityFrameworkCore`**, không phải `Diagnostics` (xem implementation `ProviderAwareMigrationsAssembly`).

### 1.9. ApiResponse contract — không đổi

```json
{
  "success": true|false,
  "data": <T>|null,
  "messageKey": "namespace.event.success" | null,
  "messageArgs": { "key": "value" } | null,
  "errors": [{ "code", "messageKey", "field", "args" }] | null,
  "traceId": "...",
  "timestamp": "..."
}
```

- BE **không bao giờ** translate. Chỉ trả `messageKey`. FE translate.
- Mọi response **phải** có `traceId` (kể cả 5xx) — middleware [TraceIdMiddleware](src/BuildingBlocks/BB.Web/TraceIdMiddleware.cs) lo việc này.

---

## 2. Frontend — Convention

### 2.1. Standalone components + signals only

- KHÔNG `NgModule`.
- KHÔNG `BehaviorSubject` cho local state — dùng `signal()`/`computed()`.
- KHÔNG NgRx classic. Nếu cần global state phức tạp → `@ngrx/signals` SignalStore.

### 2.2. HTTP interceptor chain — thứ tự cố định

```typescript
provideHttpClient(withInterceptors([
  traceIdInterceptor,        // gắn Accept-Language
  authInterceptor,           // gắn Bearer
  apiResponseInterceptor,    // unwrap ApiResponse → emit data; throw ApiException nếu fail
  errorInterceptor           // catch ApiException + HttpErrorResponse → mở ErrorDialog
]))
```

**Đừng** thay đổi thứ tự.

### 2.3. apiResponseInterceptor UNWRAP body.data — service nhận raw data

**Bug đã đạp**: `AuthService.login()` ban đầu khai báo `Observable<ApiResponse<AuthResponse>>` rồi check `res.success`. Nhưng interceptor đã unwrap nên `res` là `AuthResponse`, không có field `success` → check luôn falsy → không navigate.

**Đúng**:
```typescript
// Service trả raw data type, KHÔNG phải ApiResponse<T>
login(u: string, p: string): Observable<AuthResponse> {
  return this.http.post<AuthResponse>(`${api}/v1/auth/login`, { userName: u, password: p })
    .pipe(tap((data) => this.persist(data)));
}

// Component subscribe — không check res.success, không try/catch error
this.auth.login(...).subscribe({
  next: () => this.router.navigate(['/workspaces']),
  error: () => this.loading.set(false)   // chỉ reset UI state, ErrorDialog tự hiện
});
```

Tham chiếu: [auth.service.ts](frontend/src/app/core/auth/auth.service.ts), [login.page.ts](frontend/src/app/features/identity/login.page.ts).

### 2.4. Notification flow — KHÔNG handle lỗi trong component

- **Success**: BE trả `messageKey` → interceptor gọi `notif.success(key, args)` → toast tự hiện.
- **Error**: bất kỳ failure → interceptor mở `ErrorDialog` (modal blocking + TraceId + Copy).
- Component **chỉ** override khi cần inline error UX (form field highlight). Mặc định: không try/catch, không subscribe error toàn cục.

### 2.5. i18n — luôn `| translate`

**Trong template**: 100% string user-visible đều qua pipe.

```html
<button [label]="'common.save' | translate"></button>
<th>{{ 'product.sku' | translate }}</th>
<p-dialog [header]="'product.create' | translate">...</p-dialog>
```

**Trong TS code (imperative)**: dùng `TranslateService.get(keys, args).subscribe(t => ...)`. **KHÔNG dùng `.instant()`** — bị tree-shaking strip ở prod build, fail runtime với `instant is not a function`.

```typescript
// ✅ Đúng
this.translate.get(['product.delete_confirm', 'common.delete', 'common.yes', 'common.cancel'],
                   { name: p.name })
  .subscribe((t) => {
    this.confirm.confirm({
      header: t['common.delete'],
      message: t['product.delete_confirm'],
      acceptLabel: t['common.yes'],
      rejectLabel: t['common.cancel'],
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => { ... }
    });
  });

// ❌ Sai — fail ở prod
const msg = this.translate.instant('...', args);
```

Tham chiếu: [comments-thread.component.ts](frontend/src/app/features/issue/comments-thread.component.ts) (xóa comment + `translate.get` cho ConfirmDialog).

### 2.6. Toast — pass i18n key qua `data`, KHÔNG pre-translate summary

**Bug đã đạp**: ban đầu gọi `translate.instant(messageKey)` rồi pass vào `MessageService.add({ summary })`. Prod build → `instant` tree-shaken → toast hiện raw key như `product.created.success`.

**Đúng**:

```typescript
// NotificationService
success(messageKey: string, args?: Record<string, unknown>): void {
  this.toast.add({
    severity: 'success',
    data: { messageKey, args },   // ← pass key + args qua data
    life: 3500
  });
}
```

```html
<!-- AppComponent — override toast template, dùng pipe -->
<p-toast position="top-right">
  <ng-template let-message pTemplate="message">
    <div class="toast-row">
      <i [class]="iconFor(message.severity)"></i>
      <span>{{ message.data.messageKey | translate: (message.data.args ?? null) }}</span>
    </div>
  </ng-template>
</p-toast>
```

Tham chiếu: [notification.service.ts](frontend/src/app/core/notification/notification.service.ts), [app.component.ts](frontend/src/app/app.component.ts).

### 2.7. Inject service trong HTTP interceptor — cẩn thận property name

**Bug đã đạp**: viết `LanguageService` với signal `lang = signal()`, rồi `inject(LanguageService)` trong interceptor để gọi `langSvc.lang()`. Prod build → `e.lang is not a function` (đổi sang `getLang()` cũng fail). Nguyên nhân: optimizer/tree-shaker xử lý property mangling không nhất quán khi gọi method trên service inject ngay trong interceptor function context.

**Workaround đã chốt**: trong interceptor, **inject thẳng `TranslateService`** (lib core, được preserve):

```typescript
export const traceIdInterceptor: HttpInterceptorFn = (req, next) => {
  const translate = inject(TranslateService);
  const lang = translate.currentLang || translate.defaultLang || 'vi';
  return next(req.clone({ setHeaders: { 'Accept-Language': lang } }));
};
```

LanguageService vẫn tồn tại để bind UI state (`lang.use()`), nhưng interceptor đọc từ TranslateService trực tiếp. Mọi service "wrapper" custom có signal property → **đừng** inject vào interceptor — luôn fallback service core của lib.

### 2.8. Theme monochrome + 1 danger accent

CSS variables ở [styles/theme.scss](frontend/src/styles/theme.scss). PrimeNG override qua semantic tokens. Phân biệt trạng thái bằng:
- Icon (PrimeIcons)
- Border weight
- Typography weight
- **Chỉ** `--c-accent-danger: #dc2626` cho destructive (Delete button, Error icon, accept button của confirmDelete).

KHÔNG thêm màu khác. KHÔNG gradient. KHÔNG drop-shadow màu.

### 2.9. Base UI component (shared/ui)

Khi feature lặp pattern UI, wrap PrimeNG thành component standalone trong [shared/ui/](frontend/src/app/shared/ui):
- API gọn — `input()` signal + `model()` two-way.
- KHÔNG expose toàn bộ PrimeNG props — chỉ những gì team thực dùng.
- Reference: [app-page-header.component.ts](frontend/src/app/shared/ui/app-page-header.component.ts).

### 2.10. Service per feature

- File: `features/{name}/{name}.service.ts`.
- Inject `HttpClient` + `APP_CONFIG`.
- Endpoint URL: `${cfg.apiBaseUrl}/v1/{resource}`.
- Method trả `Observable<TheData>`, KHÔNG `Observable<ApiResponse<TheData>>`.
- Reference: [project.service.ts](frontend/src/app/core/api/project.service.ts).

---

## 3. Test conventions

### 3.1. Smoke test BE qua nginx (cùng path browser dùng)

```bash
TOKEN=$(curl -s -X POST http://localhost:4200/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"Admin@123"}' \
  | python -c "import sys,json; print(json.load(sys.stdin)['data']['accessToken'])")

curl -s http://localhost:4200/api/v1/projects/mine -H "Authorization: Bearer $TOKEN"
```

Test qua port 4200 (nginx) thay vì 5000 (api raw) → verify cả nginx proxy + CORS + interceptor unwrap thực tế.

### 3.2. Inspect DB

```bash
docker exec jira-clone-postgres psql -U jira_clone -d jira_clone -c "SELECT id, key, name FROM project.projects LIMIT 5;"
```

Khi test FE và data không như kỳ vọng, **luôn check DB trực tiếp** trước khi đi tìm bug ở interceptor/state.

### 3.3. Playwright headed E2E

Project: [e2e/](e2e). Pattern:

```javascript
const browser = await chromium.launch({
  headless: false,    // user phải nhìn được
  slowMo: 250,        // chậm để follow flow
  args: ['--window-size=1280,820']
});
```

**Mỗi step có ý nghĩa → screenshot** (`page.screenshot({ path: 'screenshots/NN-step.png' })`). Screenshot là evidence, không phải optional.

Nếu Chrome (channel) launch fail (do user đang chạy Chrome thật), dùng bundled Chromium:
```bash
npx playwright install chromium
```

### 3.4. Console + network listener khi debug

```javascript
page.on('console', (m) => console.log(`[browser ${m.type()}] ${m.text()}`));
page.on('pageerror', (e) => console.log('PAGEERROR:', e.stack));
page.on('response', (r) => {
  if (r.url().includes('/api/')) console.log(`HTTP ${r.status()} ${r.request().method()} ${r.url()}`);
});
```

Khi test fail vì FE silent error, **luôn** add 3 listener trên trước khi đoán bug.

### 3.5. Tear-down giữa runs

Test e2e không tự cleanup → có thể fail run sau vì duplicate SKU/email. Khi cần state sạch:
```bash
docker exec jira-clone-postgres psql -U jira_clone -d jira_clone -c "DELETE FROM issue.issues WHERE key LIKE 'E2E-%';"
```

### 3.6. Khi nào "done"

Trước khi nói feature xong, **bắt buộc** chạy đủ 4:
1. `dotnet build` — 0 warning, 0 error.
2. Frontend build (qua docker rebuild web) — không ERROR trong output.
3. Smoke test API qua curl — happy path + 1 validation error path.
4. Playwright headed run — screenshot ít nhất login + main interaction + error case.

Nếu UI/feature không test được (vd background job), **nói rõ** "chưa test E2E vì X" thay vì im lặng.

---

## 4. Checklist — thêm module mới

1. **Tạo csproj structure**: copy `src/Modules/Project/` (hoặc module nhỏ hơn như Attachment), đổi tên namespace/schema cho `{Module}`.
2. **Domain entity**: extend `AuditableEntity` (có sẵn audit + traceId). Throw `DomainException` cho rule vi phạm.
3. **Application layer**:
   - DTO record cho request/response.
   - Validator `AbstractValidator<TRequest>` với `WithErrorCode("...")` + `WithMessage("validation.xxx")`.
   - `IXxxRepository : IRepository<TEntity>` cho query đặc thù.
   - `IXxxService` trả `Result<T>` cho mọi method.
   - `IXxxModuleUnitOfWork : IUnitOfWork` (typed UoW).
4. **Infrastructure layer**:
   - `XxxDbContext : BaseDbContext`, `HasDefaultSchema("xxx")` cho Postgres.
   - Repository impl extend `Repository<T>`.
   - `XxxModuleUnitOfWork : UnitOfWork<XxxDbContext>, IXxxModuleUnitOfWork`.
   - `IDesignTimeDbContextFactory` cho ef migrations.
5. **Api layer**:
   - Controller extend `BaseController`, dùng `[Authorize]` mặc định, `[AllowAnonymous]` cho endpoint public.
   - `XxxModule.cs` extension method `AddXxxModule(...)` đăng ký DbContext + UoW + Repo + Service + Validators.
6. **Solution**: add 4 csproj vào [Jira-Clone.sln](Jira-Clone.sln).
7. **Bootstrapper**: thêm `ProjectReference` ở [Api.Host.csproj](src/Bootstrapper/Api.Host/Api.Host.csproj). `Program.cs`: `builder.Services.AddXxxModule(builder.Configuration);`.
8. **Schema bootstrap**: thêm `var xxxDb = scope.ServiceProvider.GetRequiredService<XxxDbContext>(); await EnsureSchemaAsync(xxxDb, logger);`.
9. **i18n key**: thêm namespace `xxx.*` vào cả `vi.json` (BE + FE) và `en.json` (BE + FE) — 4 file.
10. **Test**: 1 unit test service (validator + business rule), 1 smoke test (curl) cho create + list. Build clean.

---

## 5. Checklist — thêm feature page FE

1. **Service**: `features/{name}/{name}.service.ts`. Method trả `Observable<RawData>`.
2. **Page component**: `features/{name}/{name}.page.ts`. Standalone, OnPush, signals. Inject service.
3. **Template** dùng base UI component nếu có; PrimeNG trực tiếp nếu chưa wrap.
4. **Tất cả** label/header/button/column → `| translate`.
5. **Imperative i18n** (confirm, alert, etc.) → `translate.get([keys], args).subscribe(t => ...)`.
6. **KHÔNG** subscribe error globally — interceptor + ErrorDialog lo.
7. **i18n keys** thêm cho cả `vi.json` và `en.json` cùng lúc.
8. **Route**: thêm vào [app.routes.ts](frontend/src/app/app.routes.ts), lazy `loadComponent`, có `authGuard` nếu protected.
9. **Test**: 1 step trong [e2e/run.js](e2e/run.js) cover golden path + 1 error path. Screenshot.

---

## 6. Checklist — thêm i18n key

1. Thêm key vào **4 file** cùng lúc — KHÔNG bỏ sót:
   - [src/BuildingBlocks/BB.Localization/Resources/vi.json](src/BuildingBlocks/BB.Localization/Resources/vi.json) (BE — chỉ những key BE dùng để format email/log)
   - [src/BuildingBlocks/BB.Localization/Resources/en.json](src/BuildingBlocks/BB.Localization/Resources/en.json)
   - [frontend/src/assets/i18n/vi.json](frontend/src/assets/i18n/vi.json) (FE — tất cả key user-visible)
   - [frontend/src/assets/i18n/en.json](frontend/src/assets/i18n/en.json)
2. Naming convention: `<domain>.<event_or_field>[.modifier]`. Vd: `product.created.success`, `validation.required`, `auth.invalid_credentials`.
3. ICU param: `{{name}}`, `{{count}}`. BE pass qua `messageArgs`, FE pass qua `translate.get(key, { name: '...' })` hoặc trong template `'key' | translate: { name: ... }`.
4. KHÔNG hardcode message tiếng Việt/Anh trong domain exception, validator, controller. Chỉ pass `messageKey`.

---

## 7. Catalog các bug đã đạp — đừng lặp lại

| # | Triệu chứng | Nguyên nhân | Fix | File |
|---|---|---|---|---|
| 1 | API container crash khi seed: `relation "identity.roles" does not exist` | `EnsureCreatedAsync` short-circuit khi DB tồn tại — context thứ 2 skip | Dùng `IRelationalDatabaseCreator.CreateTablesAsync` per-context | [Program.cs](src/Bootstrapper/Api.Host/Program.cs) `EnsureSchemaAsync` |
| 2 | Login API trả 200 nhưng entity không persist sau Create | `IUnitOfWork` đăng ký 2 lần → DI last-wins → service gọi nhầm DbContext module khác | Typed UoW per module | [ProjectUnitOfWork](src/Modules/Project/Project.Infrastructure/Repositories.cs), [IdentityUnitOfWork.cs](src/Modules/Identity/Identity.Infrastructure/IdentityUnitOfWork.cs) |
| 3 | Login HTTP 200 nhưng FE không redirect | `apiResponseInterceptor` unwrap body.data nhưng AuthService check `res.success` | Service trả raw data, không phải ApiResponse | [auth.service.ts](frontend/src/app/core/auth/auth.service.ts) |
| 4 | Console error `e.current is not a function` ở traceIdInterceptor | `inject(LanguageService)` trong interceptor + signal property bị optimizer mangle | Inject `TranslateService` core | [interceptors.ts](frontend/src/app/core/http/interceptors.ts) |
| 5 | Toast hiển thị raw key `product.created.success` | `translate.instant()` bị tree-shaken ở prod | Pass `{messageKey, args}` qua MessageService.data + override `<p-toast>` template với pipe | [notification.service.ts](frontend/src/app/core/notification/notification.service.ts), [app.component.ts](frontend/src/app/app.component.ts) |
| 6 | Confirm dialog xóa hiện text Anh "Delete X?" | Hardcode template string | `translate.get([keys], args)` rồi pass header/message/labels | [comments-thread.component.ts](frontend/src/app/features/issue/comments-thread.component.ts) |
| 7 | Build FE fail `Could not resolve "@shared/..."` | tsconfig thiếu `baseUrl` | Add `"baseUrl": "./"` cạnh `paths` | [tsconfig.json](frontend/tsconfig.json) |
| 8 | Build BE fail `BB.Web does not contain a static Main` | `Microsoft.NET.Sdk.Web` cho project library | Đổi sang `Microsoft.NET.Sdk` + `<FrameworkReference Include="Microsoft.AspNetCore.App" />` | [BB.Web.csproj](src/BuildingBlocks/BB.Web/BB.Web.csproj) |
| 9 | `provideAppInitializer is not exported` | API là Angular 19+ | Angular 18 dùng `APP_INITIALIZER` token + factory | [app.config.ts](frontend/src/app/app.config.ts) |
| 10 | `provideTranslateService is not exported` | API là ngx-translate v16+ | v15 dùng `importProvidersFrom(TranslateModule.forRoot(...))` | [app.config.ts](frontend/src/app/app.config.ts) |
| 11 | `Result.Success("...")` ambiguous | 2 overload Success(messageKey) và Success<T>(T) | Dùng named arg `messageKey:` | mọi service trả `Result` |
| 12 | Playwright Chrome channel launch fail | User có Chrome thật đang chạy → pipe transport conflict | Bỏ `channel: 'chrome'`, dùng bundled Chromium (`npx playwright install chromium`) | [e2e/run.js](e2e/run.js) |

---

## 8. Quick reference — các lệnh hay dùng

```bash
# Up full stack
docker compose -f docker-compose.dev.yml up -d --build

# Rebuild riêng api / web sau khi sửa code
docker compose -f docker-compose.dev.yml up -d --build api
docker compose -f docker-compose.dev.yml up -d --build web

# Inspect DB
docker exec jira-clone-postgres psql -U jira_clone -d jira_clone -c "\dt project.*"
docker exec jira-clone-postgres psql -U jira_clone -d jira_clone -c "SELECT id, key, name FROM project.projects LIMIT 5;"

# API logs
docker logs jira-clone-api --tail 50

# Ví dụ cleanup issue test E2E (tuỳ prefix)
docker exec jira-clone-postgres psql -U jira_clone -d jira_clone -c "DELETE FROM issue.issues WHERE key LIKE 'E2E-%';"

# Build BE local
dotnet build

# Run e2e
cd e2e && node run.js
```

---

## 9. Khi không biết viết thế nào

1. Tìm pattern tương tự trong module **Project** (hoặc module domain gần nhất trong repo).
2. Tra cứu file này (PATTERNS.md) — các bug đã đạp + giải pháp.
3. Đọc [CLAUDE.md](CLAUDE.md) cho kiến trúc tổng.
4. Nếu phát sinh bug mới chưa có trong catalog §7 → **thêm vào** sau khi fix, để lần sau không lặp.

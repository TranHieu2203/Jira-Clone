# Secrets Management

> Các giá trị nhạy cảm KHÔNG commit vào repo. Khi clone về, bạn cần tự cấu hình theo hướng dẫn dưới đây.

## Production

Bắt buộc set qua **environment variables** trước khi chạy `Api.Host`:

| Env var | Mục đích |
|---|---|
| `Jwt__SigningKey` | HMAC-SHA256 key (≥ 32 bytes UTF-8). Sinh bằng `openssl rand -base64 48`. |
| `Resend__ApiKey` | Resend API key (nếu bật email). |
| `ConnectionStrings__Database` hoặc `Database__ConnectionString` | DB connection string. |
| `Storage__S3__AccessKey` / `Storage__S3__SecretKey` | Credentials MinIO/S3 (nếu Storage.Provider=S3). |

⚠️ **Fail-fast**: app sẽ throw ngay khi startup nếu `Jwt:SigningKey` rỗng hoặc < 32 bytes.

## Development

Chọn 1 trong 2 cách:

### Cách 1: User Secrets (recommended, không leak vào git)

```bash
cd src/Bootstrapper/Api.Host
dotnet user-secrets init
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)"
dotnet user-secrets set "Resend:ApiKey" "re_YOUR_DEV_KEY"
```

Secrets lưu ở:
- Windows: `%APPDATA%\Microsoft\UserSecrets\<id>\secrets.json`
- macOS/Linux: `~/.microsoft/usersecrets/<id>/secrets.json`

### Cách 2: Environment variables (terminal hiện tại)

PowerShell:
```powershell
$env:Jwt__SigningKey = "DEV_KEY_AT_LEAST_32_CHARS_LONG_xxxxxxxxxxxxxxxxxxxx"
$env:Resend__ApiKey = "re_YOUR_DEV_KEY"
dotnet run
```

Bash:
```bash
export Jwt__SigningKey="DEV_KEY_AT_LEAST_32_CHARS_LONG_xxxxxxxxxxxxxxxxxxxx"
export Resend__ApiKey="re_YOUR_DEV_KEY"
dotnet run
```

> **Lưu ý**: trong `appsettings.Development.json` đã có sẵn dev placeholder `Jwt.SigningKey` để FE/BE chạy ngay khi clone về. **KHÔNG dùng key này cho prod.**

## Rotation policy

- **Resend API key**: từ commit `1f53363` đã từng bị commit thật vào git → **đã invalidate trong commit `cc85891` reset về empty**. Nếu bạn fork/clone trước đó, hãy rotate ngay tại https://resend.com/api-keys.
- **JWT signing key**: rotate khi nghi ngờ leak. Sau rotate, all access tokens cũ sẽ invalid (refresh token cũng cần re-issue). Nên dùng key versioning trong `JwtOptions` (TODO P11+).

## Docker compose

`docker-compose.dev.yml` cần thêm env vars vào service `api`:

```yaml
services:
  api:
    environment:
      - Jwt__SigningKey=${JWT_SIGNING_KEY:-DEV_ONLY_DO_NOT_USE_IN_PROD__JIRA_CLONE_DEV_KEY_32B}
      - Resend__ApiKey=${RESEND_API_KEY:-}
```

Tạo `.env` (không commit, đã có trong `.gitignore`):
```
JWT_SIGNING_KEY=...
RESEND_API_KEY=re_...
```

## CI/CD

GitHub Actions: dùng repository secrets — set `JWT_SIGNING_KEY` + `RESEND_API_KEY` qua Settings → Secrets and variables → Actions, sau đó pass vào job:

```yaml
env:
  Jwt__SigningKey: ${{ secrets.JWT_SIGNING_KEY }}
  Resend__ApiKey: ${{ secrets.RESEND_API_KEY }}
```

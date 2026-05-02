# Agent instructions — Jira-Clone

Repo này có rule cố định cho mọi AI agent (Claude Code, Cursor, Aider, etc.). **Đọc 3 file dưới đây theo thứ tự trước khi sửa hoặc viết bất kỳ dòng code nào**:

1. **[PATTERNS.md](PATTERNS.md)** — convention + 12 bug đã đạp + checklist. **Bắt buộc đọc §7 (catalog bug)** trước khi đoán nguyên nhân bất kỳ triệu chứng nào.
2. **[CLAUDE.md](CLAUDE.md)** — kiến trúc tổng, response format, ràng buộc.
3. **[README.md](README.md)** — quickstart, lệnh chạy.

## Tôn chỉ

- **Convention đã có > sáng tạo riêng**. Đi ngược pattern phải nói rõ lý do với user trước.
- **Module `Sample` là reference implementation** — khi không biết viết thế nào, copy pattern từ đó.
- **"Done" = 4 bước** ở [PATTERNS.md §3.6](PATTERNS.md): build BE clean + build FE clean + curl smoke + Playwright + screenshot.
- **Bug mới phát hiện** → fix xong **bắt buộc** thêm vào [PATTERNS.md §7](PATTERNS.md). Catalog không được stale.

## Build / run / test

```bash
# Up full stack
docker compose -f docker-compose.dev.yml up -d --build

# Build BE
dotnet build

# Smoke test (qua nginx — cùng path browser dùng)
curl -s -X POST http://localhost:4200/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"Admin@123"}'

# E2E headed (xem AI thao tác trên browser)
cd e2e && node run.js
```

Tài khoản seed: **admin / Admin@123**.

## Commit

Conventional Commits: `feat(sample):`, `fix(identity):`, `chore:`, `refactor:`. Branch base `develop`, release `main`.

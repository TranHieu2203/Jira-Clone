// Tiny SPA-fallback static server. Serves the FE dist bundle on port 4201 so
// we can audit issue-detail / issues / backlog pages WITHOUT Vite dev cache.
const http = require('http');
const fs = require('fs');
const path = require('path');

const ROOT = process.argv[2] || path.join(__dirname, '..', 'frontend', 'dist', 'jira-clone', 'browser');
const PORT = Number(process.argv[3] || 4201);

const MIME = {
  '.html': 'text/html', '.js': 'application/javascript', '.css': 'text/css',
  '.json': 'application/json', '.svg': 'image/svg+xml', '.png': 'image/png',
  '.jpg': 'image/jpeg', '.ico': 'image/x-icon', '.woff2': 'font/woff2'
};

http.createServer((req, res) => {
  const u = decodeURIComponent(req.url.split('?')[0]);
  let f = path.join(ROOT, u);
  // SPA fallback: any path không có extension → index.html
  if (!fs.existsSync(f) || fs.statSync(f).isDirectory()) {
    if (path.extname(u) === '') f = path.join(ROOT, 'index.html');
  }
  fs.readFile(f, (err, data) => {
    if (err) { res.writeHead(404); res.end('not found'); return; }
    res.writeHead(200, { 'Content-Type': MIME[path.extname(f).toLowerCase()] || 'text/plain' });
    res.end(data);
  });
}).listen(PORT, () => console.log(`static-spa on :${PORT} root=${ROOT}`));

"""Local Game Design Atlas.

Stdlib only. Serves the workspace for reading.
Does not modify data/ or canonical Markdown documents.

    python server.py
"""

import json
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import unquote, urlparse

import validate

ROOT = Path(__file__).resolve().parent
REPO = ROOT.parent
DATA = ROOT / "data"
APP = ROOT / "app"
HOST = "127.0.0.1"
PORT = 8765
MEDIA_PREFIXES = ("References/",)
MEDIA_SUFFIXES = (".webp", ".png", ".jpg", ".jpeg", ".gif")


def read_atlas():
    return validate.load_atlas(ROOT)


def media_file(url_path):
    relative = unquote(url_path[len("/media/"):]).replace("\\", "/")
    if relative.startswith("/") or ".." in relative.split("/"):
        return None
    if not relative.startswith(MEDIA_PREFIXES):
        return None
    if not relative.lower().endswith(MEDIA_SUFFIXES):
        return None
    candidate = (REPO / relative).resolve()
    try:
        candidate.relative_to(REPO.resolve())
    except ValueError:
        return None
    if not candidate.is_file():
        return None
    return candidate


class Handler(BaseHTTPRequestHandler):
    def log_message(self, fmt, *args):
        print("[%s] %s" % (self.log_date_time_string(), fmt % args))

    def _send(self, code, body, content_type, extra_headers=None):
        data = body if isinstance(body, bytes) else body.encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(data)))
        self.send_header("Cache-Control", "no-store")
        if extra_headers:
            for key, value in extra_headers:
                self.send_header(key, value)
        self.end_headers()
        self.wfile.write(data)

    def _json(self, code, payload):
        self._send(code, json.dumps(payload, ensure_ascii=False), "application/json; charset=utf-8")

    def do_GET(self):
        path = urlparse(self.path).path
        if path == "/api/atlas":
            self._json(200, read_atlas())
            return
        if path.startswith("/media/"):
            target = media_file(path)
            if target is None:
                self._send(404, "Not found", "text/plain; charset=utf-8")
                return
            kind = "image/webp" if target.suffix.lower() == ".webp" else "application/octet-stream"
            self._send(200, target.read_bytes(), kind)
            return
        if path == "/":
            path = "/index.html"
        relative = path.lstrip("/")
        if relative.startswith(".."):
            self._send(404, "Not found", "text/plain; charset=utf-8")
            return
        file_path = (APP / relative).resolve()
        try:
            file_path.relative_to(APP.resolve())
        except ValueError:
            self._send(404, "Not found", "text/plain; charset=utf-8")
            return
        if not file_path.is_file():
            self._send(404, "Not found", "text/plain; charset=utf-8")
            return
        types = {
            ".html": "text/html; charset=utf-8",
            ".css": "text/css; charset=utf-8",
            ".js": "text/javascript; charset=utf-8",
            ".json": "application/json; charset=utf-8",
        }
        self._send(200, file_path.read_bytes(), types.get(file_path.suffix, "application/octet-stream"))

def main():
    errors = validate.validate(read_atlas(), REPO)
    if errors:
        print("Atlas data failed validation. Fix data/ before serving.")
        for error in errors:
            print(" - " + error)
        raise SystemExit(1)
    server = ThreadingHTTPServer((HOST, PORT), Handler)
    print("Game Design Atlas: http://%s:%s/" % (HOST, PORT), flush=True)
    print("Data directory: %s" % DATA, flush=True)
    print("This server only reads data/. It does not write JSON or Markdown.", flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("Stopped.")


if __name__ == "__main__":
    main()

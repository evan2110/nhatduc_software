"""
Cap nhat NHATDUC_GMAIL_REFRESH_TOKEN tren Render qua API.

Usage:
  set RENDER_API_KEY=rnd_xxxxxxxx
  python scripts/update-render-gmail-env.py

Hoac:
  python scripts/update-render-gmail-env.py rnd_xxxxxxxx
"""
from __future__ import annotations

import json
import os
import sys
import urllib.error
import urllib.request
from pathlib import Path

SERVICE_ID = "srv-d8k2b9eq1p3s7fchag"
ENV_KEY = "NHATDUC_GMAIL_REFRESH_TOKEN"
ROOT = Path(__file__).resolve().parents[1]
SECRETS_PATH = ROOT / "NhatDucSoftware.Web" / "appsettings.Secrets.json"
TOKEN_PATH = Path(__file__).resolve().parent / "google-gmail-token.json"


def load_gmail_refresh_token() -> str:
    if SECRETS_PATH.exists():
        data = json.loads(SECRETS_PATH.read_text(encoding="utf-8"))
        token = (data.get("GoogleDrive") or {}).get("GmailRefreshToken", "").strip()
        if token:
            return token

    if TOKEN_PATH.exists():
        token = json.loads(TOKEN_PATH.read_text(encoding="utf-8")).get("refresh_token", "").strip()
        if token:
            return token

    raise SystemExit("Khong tim thay Gmail refresh token. Chay scripts/generate-gmail-token.py truoc.")


def render_request(method: str, url: str, api_key: str, payload: dict | None = None) -> dict:
    data = None if payload is None else json.dumps(payload).encode("utf-8")
    request = urllib.request.Request(
        url,
        data=data,
        method=method,
        headers={
            "Authorization": f"Bearer {api_key}",
            "Accept": "application/json",
            "Content-Type": "application/json",
        },
    )
    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            body = response.read().decode("utf-8")
            return json.loads(body) if body else {}
    except urllib.error.HTTPError as exc:
        detail = exc.read().decode("utf-8", errors="replace")
        raise SystemExit(f"Render API loi {exc.code}: {detail}") from exc


def upsert_env_var(api_key: str, value: str) -> None:
    url = f"https://api.render.com/v1/services/{SERVICE_ID}/env-vars/{ENV_KEY}"
    render_request("PUT", url, api_key, {"value": value})


def trigger_deploy(api_key: str) -> None:
    url = f"https://api.render.com/v1/services/{SERVICE_ID}/deploys"
    render_request("POST", url, api_key, {"clearCache": "do_not_clear"})


def main() -> None:
    api_key = (sys.argv[1] if len(sys.argv) > 1 else os.environ.get("RENDER_API_KEY", "")).strip()
    if not api_key:
        raise SystemExit(
            "Thieu RENDER_API_KEY. Lay tai Render Dashboard -> Account Settings -> API Keys.\n"
            "Chay: set RENDER_API_KEY=rnd_xxx && python scripts/update-render-gmail-env.py"
        )

    token = load_gmail_refresh_token()
    upsert_env_var(api_key, token)
    trigger_deploy(api_key)
    print(f"Da cap nhat {ENV_KEY} tren Render va kich hoat deploy.")


if __name__ == "__main__":
    main()

"""
Tao refresh token chi de GUI MAIL (scope: gmail.send).

Usage:
  python scripts/generate-gmail-token.py
  python scripts/generate-gmail-token.py path/to/client_secret.json

Tu dong doc OAuth tu:
  1) appsettings.Secrets.json (GoogleDrive section)
  2) bien moi truong GOOGLE_DRIVE_CLIENT_ID / GOOGLE_DRIVE_CLIENT_SECRET
"""
from __future__ import annotations

import json
import sys
import tempfile
from pathlib import Path

from google_auth_oauthlib.flow import InstalledAppFlow

SCOPES = ["https://www.googleapis.com/auth/gmail.send"]
ROOT = Path(__file__).resolve().parents[1]
SECRETS_PATH = ROOT / "NhatDucSoftware.Web" / "appsettings.Secrets.json"


def load_oauth_from_secrets() -> tuple[str, str] | None:
    if not SECRETS_PATH.exists():
        return None

    data = json.loads(SECRETS_PATH.read_text(encoding="utf-8"))
    drive = data.get("GoogleDrive") or {}
    client_id = (drive.get("ClientId") or "").strip()
    client_secret = (drive.get("ClientSecret") or "").strip()
    if client_id and client_secret:
        return client_id, client_secret
    return None


def build_client_secret_file(client_id: str, client_secret: str) -> Path:
    payload = {
        "installed": {
            "client_id": client_id,
            "client_secret": client_secret,
            "auth_uri": "https://accounts.google.com/o/oauth2/auth",
            "token_uri": "https://oauth2.googleapis.com/token",
            "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
            "redirect_uris": ["http://localhost"],
        }
    }
    temp = Path(tempfile.gettempdir()) / "nhatduc-gmail-client-secret.json"
    temp.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    return temp


def resolve_client_secret_path() -> Path:
    if len(sys.argv) >= 2 and sys.argv[1].strip():
        secret_path = Path(sys.argv[1])
        if not secret_path.exists():
            print(f"Khong tim thay: {secret_path}")
            sys.exit(1)
        return secret_path

    from_env = (
        __import__("os").environ.get("GOOGLE_DRIVE_CLIENT_ID", "").strip(),
        __import__("os").environ.get("GOOGLE_DRIVE_CLIENT_SECRET", "").strip(),
    )
    if from_env[0] and from_env[1]:
        print("Dung OAuth tu bien moi truong GOOGLE_DRIVE_CLIENT_ID/SECRET.")
        return build_client_secret_file(from_env[0], from_env[1])

    from_secrets = load_oauth_from_secrets()
    if from_secrets:
        print(f"Dung OAuth tu {SECRETS_PATH}.")
        return build_client_secret_file(from_secrets[0], from_secrets[1])

    print("Khong tim thay OAuth credentials.")
    print("Cach 1: dat ClientId/ClientSecret trong NhatDucSoftware.Web/appsettings.Secrets.json")
    print("Cach 2: python scripts/generate-gmail-token.py path/to/client_secret.json")
    sys.exit(1)


def update_secrets_file(refresh_token: str) -> None:
    if not SECRETS_PATH.exists():
        return

    data = json.loads(SECRETS_PATH.read_text(encoding="utf-8"))
    data.setdefault("GoogleDrive", {})["GmailRefreshToken"] = refresh_token
    SECRETS_PATH.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")
    print(f"Da luu GmailRefreshToken vao {SECRETS_PATH}")


def main() -> None:
    secret_path = resolve_client_secret_path()

    print("\n=== HUONG DAN ===")
    print("1. Trinh duyet se mo - dang nhap: ctytnhhgiaoducnhatduc@gmail.com")
    print("2. Chon 'Tiep tuc' va cap quyen gui email")
    print("3. Neu khong thay man hinh cap quyen, thu hoi quyen cu tai:")
    print("   https://myaccount.google.com/permissions")
    print()

    flow = InstalledAppFlow.from_client_secrets_file(str(secret_path), SCOPES)
    creds = flow.run_local_server(port=0, prompt="consent", access_type="offline")

    if not creds.refresh_token:
        print("\nLOI: Khong nhan duoc refresh_token.")
        print("Hay thu hoi quyen app tai https://myaccount.google.com/permissions roi chay lai.")
        sys.exit(1)

    granted = " ".join(creds.scopes or SCOPES)
    if "gmail.send" not in granted:
        print(f"\nLOI: Token khong co gmail.send. Scope nhan duoc: {granted}")
        sys.exit(1)

    output = {
        "client_id": creds.client_id,
        "client_secret": creds.client_secret,
        "refresh_token": creds.refresh_token,
        "scope": granted,
    }

    out_path = Path(__file__).resolve().parent / "google-gmail-token.json"
    out_path.write_text(json.dumps(output, indent=2), encoding="utf-8")
    update_secrets_file(creds.refresh_token)

    print("\n=== SUCCESS ===")
    print(f"Saved: {out_path}")
    print(f"\nNHATDUC_GMAIL_REFRESH_TOKEN={creds.refresh_token}")
    print("\nDat gia tri tren vao Render -> Environment -> NHATDUC_GMAIL_REFRESH_TOKEN")


if __name__ == "__main__":
    main()

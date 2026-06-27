"""
Tao refresh token chi de GUI MAIL (scope: gmail.send).
Dung khi GOOGLE_DRIVE_REFRESH_TOKEN cu chua co quyen gui mail.

Usage:
  pip install google-auth google-auth-oauthlib google-api-python-client
  python scripts/generate-gmail-token.py path/to/client_secret.json

Sau do dat tren Render:
  NHATDUC_GMAIL_REFRESH_TOKEN=<refresh_token>
(Giu nguyen GOOGLE_DRIVE_REFRESH_TOKEN cu neu Drive van hoat dong tot)
"""
import json
import sys
from pathlib import Path

from google_auth_oauthlib.flow import InstalledAppFlow

SCOPES = ["https://www.googleapis.com/auth/gmail.send"]


def main() -> None:
    if len(sys.argv) < 2:
        print("Usage: python generate-gmail-token.py <client_secret.json>")
        sys.exit(1)

    secret_path = Path(sys.argv[1])
    if not secret_path.exists():
        print(f"Khong tim thay: {secret_path}")
        sys.exit(1)

    print("\n=== HUONG DAN TRUOC KHI CHAY ===")
    print("1. Bat Gmail API: Google Cloud Console -> APIs & Services -> Library -> Gmail API -> Enable")
    print("2. Thu hoi quyen cu (neu da tung cap quyen app):")
    print("   https://myaccount.google.com/permissions")
    print("3. Dang nhap bang: ctytnhhgiaoducnhatduc@gmail.com khi trinh duyet mo")
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

    print("\n=== SUCCESS ===")
    print(f"Saved: {out_path}")
    print("\nDat tren Render Environment:")
    print(f"NHATDUC_GMAIL_REFRESH_TOKEN={creds.refresh_token}")
    print("\n(Co the dung chung client id/secret voi Drive, khong can doi GOOGLE_DRIVE_CLIENT_ID/SECRET)")
    print("Sau khi Save -> Render se redeploy. Thu lai nut 'Gui mail cong'.")


if __name__ == "__main__":
    main()

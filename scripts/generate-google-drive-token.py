"""
Tạo refresh token Google với quyền Drive + Gmail send.
Chạy 1 lần trên máy local, sau đó copy refresh_token lên Render.

Usage:
  pip install google-auth google-auth-oauthlib google-api-python-client
  python scripts/generate-google-drive-token.py path/to/client_secret.json
"""
import json
import sys
from pathlib import Path

from google_auth_oauthlib.flow import InstalledAppFlow

# drive: upload tài liệu vào folder Drive có sẵn
# gmail.send: gửi phiếu lương qua Gmail API (Render chặn SMTP)
SCOPES = [
    "https://www.googleapis.com/auth/drive",
    "https://www.googleapis.com/auth/gmail.send",
]


def main() -> None:
    if len(sys.argv) < 2:
        print("Usage: python generate-google-drive-token.py <client_secret.json>")
        sys.exit(1)

    secret_path = Path(sys.argv[1])
    if not secret_path.exists():
        print(f"Không tìm thấy: {secret_path}")
        sys.exit(1)

    flow = InstalledAppFlow.from_client_secrets_file(str(secret_path), SCOPES)
    creds = flow.run_local_server(port=0)

    output = {
        "client_id": creds.client_id,
        "client_secret": creds.client_secret,
        "refresh_token": creds.refresh_token,
        "scope": "drive gmail.send",
    }

    out_path = Path(__file__).resolve().parent / "google-drive-token.json"
    out_path.write_text(json.dumps(output, indent=2), encoding="utf-8")

    print("\n=== SUCCESS ===")
    print(f"Saved: {out_path}")
    print("\nSet these on Render Environment:")
    print(f"GOOGLE_DRIVE_CLIENT_ID={creds.client_id}")
    print(f"GOOGLE_DRIVE_CLIENT_SECRET={creds.client_secret}")
    print(f"GOOGLE_DRIVE_REFRESH_TOKEN={creds.refresh_token}")
    print("GOOGLE_DRIVE_ROOT_FOLDER_ID=1g1sl-pKk1d3sixMkpXbSWmiFDXb55u-n")
    print("\nLuu y: Can bat Gmail API trong Google Cloud Console cho project nay.")
    print("Sau khi deploy, tren Render se tu dong gui mail qua Gmail API (HTTPS), khong dung SMTP.")


if __name__ == "__main__":
    main()

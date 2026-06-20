import json
from google.oauth2.credentials import Credentials
from google.auth.transport.requests import Request
from googleapiclient.discovery import build

data = json.load(open(r"D:\Tool\NhatDucSoftware\scripts\google-drive-token.json"))
creds = Credentials(
    token=None,
    refresh_token=data["refresh_token"],
    token_uri="https://oauth2.googleapis.com/token",
    client_id=data["client_id"],
    client_secret=data["client_secret"],
    scopes=[data["scope"]],
)
creds.refresh(Request())
service = build("drive", "v3", credentials=creds)

about = service.about().get(fields="user").execute()
user = about.get("user", {})
print("email:", user.get("emailAddress", ""))

folder_id = "1g1sl-pKk1d3sixMkpXbSWmiFDXb55u-n"
try:
    f = service.files().get(fileId=folder_id, fields="id,name").execute()
    print("target_ok:", f.get("id"), f.get("name", "").encode("unicode_escape").decode())
except Exception as e:
    print("target_fail:", str(e)[:200])

resp = service.files().list(
    q="mimeType='application/vnd.google-apps.folder' and trashed=false",
    fields="files(id,name)",
    pageSize=30,
    orderBy="modifiedTime desc",
).execute()
print("recent_folders:")
for f in resp.get("files", []):
    name = f.get("name", "").encode("unicode_escape").decode()
    print(f"  {f['id']} | {name}")

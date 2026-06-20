import json
from google.oauth2.credentials import Credentials
from google.auth.transport.requests import Request
from googleapiclient.discovery import build
from googleapiclient.http import MediaInMemoryUpload

CORRECT_FOLDER = "1g1sl-pKk1d3sixMkpXbSWmiFDXb55u-n"

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

folder = service.files().get(fileId=CORRECT_FOLDER, fields="id,name").execute()
print("folder_ok:", folder["id"])

sub = service.files().create(
    body={"name": "TEST_UPLOAD_DELETE_ME", "mimeType": "application/vnd.google-apps.folder", "parents": [CORRECT_FOLDER]},
    fields="id",
).execute()

uploaded = service.files().create(
    body={"name": "probe.txt", "parents": [sub["id"]]},
    media_body=MediaInMemoryUpload(b"ok", mimetype="text/plain"),
    fields="id,webViewLink",
).execute()

service.files().delete(fileId=uploaded["id"]).execute()
service.files().delete(fileId=sub["id"]).execute()
print("upload_ok:", uploaded["webViewLink"])

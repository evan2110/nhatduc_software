using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Http;
using Google.Apis.Services;
using Google.Apis.Upload;
using GoogleFile = Google.Apis.Drive.v3.Data.File;

namespace NhatDucSoftware.Core.Services;

public class GoogleDriveUploadResult
{
    public string FileId { get; set; } = string.Empty;
    public string? WebViewLink { get; set; }
}

public class GoogleDriveService
{
    private static readonly string[] Scopes = [DriveService.Scope.DriveFile];

    private readonly string _rootFolderId;
    private readonly string? _serviceAccountJson;
    private readonly string? _clientId;
    private readonly string? _clientSecret;
    private readonly string? _refreshToken;
    private DriveService? _driveService;

    public GoogleDriveService()
    {
        _rootFolderId = Environment.GetEnvironmentVariable("GOOGLE_DRIVE_ROOT_FOLDER_ID")
            ?? "1q1sl-pKk1d3sixMkpXbSWmiFDXb55u-n";
        _serviceAccountJson = Environment.GetEnvironmentVariable("GOOGLE_DRIVE_SERVICE_ACCOUNT_JSON");
        _clientId = Environment.GetEnvironmentVariable("GOOGLE_DRIVE_CLIENT_ID");
        _clientSecret = Environment.GetEnvironmentVariable("GOOGLE_DRIVE_CLIENT_SECRET");
        _refreshToken = Environment.GetEnvironmentVariable("GOOGLE_DRIVE_REFRESH_TOKEN");
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_serviceAccountJson)
        || (!string.IsNullOrWhiteSpace(_clientId)
            && !string.IsNullOrWhiteSpace(_clientSecret)
            && !string.IsNullOrWhiteSpace(_refreshToken));

    public string ConfigurationHint =>
        "Cấu hình biến môi trường GOOGLE_DRIVE_SERVICE_ACCOUNT_JSON (service account) "
        + "hoặc GOOGLE_DRIVE_CLIENT_ID + GOOGLE_DRIVE_CLIENT_SECRET + GOOGLE_DRIVE_REFRESH_TOKEN (OAuth Gmail). "
        + "Thư mục gốc phải được chia sẻ với tài khoản Google Drive tương ứng.";

    public async Task<GoogleDriveUploadResult> UploadToSubjectFolderAsync(
        string subjectName,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException($"Google Drive chưa được cấu hình. {ConfigurationHint}");
        }

        if (string.IsNullOrWhiteSpace(subjectName))
        {
            throw new InvalidOperationException("Tên môn học không được để trống.");
        }

        var drive = GetDriveService();
        var folderId = await GetOrCreateSubjectFolderAsync(drive, subjectName.Trim(), cancellationToken);

        var fileMetadata = new GoogleFile
        {
            Name = fileName,
            Parents = [folderId]
        };

        var request = drive.Files.Create(fileMetadata, fileStream, contentType);
        request.Fields = "id, webViewLink";
        request.SupportsAllDrives = true;

        var uploadResult = await request.UploadAsync(cancellationToken);
        if (uploadResult.Status != UploadStatus.Completed)
        {
            throw new InvalidOperationException(
                uploadResult.Exception?.Message ?? "Upload lên Google Drive thất bại.");
        }

        var uploaded = request.ResponseBody
            ?? throw new InvalidOperationException("Google Drive không trả về thông tin file.");

        return new GoogleDriveUploadResult
        {
            FileId = uploaded.Id,
            WebViewLink = uploaded.WebViewLink
        };
    }

    private DriveService GetDriveService()
    {
        if (_driveService is not null)
        {
            return _driveService;
        }

        _driveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = CreateCredential(),
            ApplicationName = "NhatDucSoftware"
        });
        return _driveService;
    }

    private IConfigurableHttpClientInitializer CreateCredential()
    {
        if (!string.IsNullOrWhiteSpace(_serviceAccountJson))
        {
            return GoogleCredential.FromJson(_serviceAccountJson).CreateScoped(Scopes);
        }

        if (string.IsNullOrWhiteSpace(_clientId)
            || string.IsNullOrWhiteSpace(_clientSecret)
            || string.IsNullOrWhiteSpace(_refreshToken))
        {
            throw new InvalidOperationException($"Google Drive chưa được cấu hình. {ConfigurationHint}");
        }

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = _clientId,
                ClientSecret = _clientSecret
            },
            Scopes = Scopes
        });

        return new UserCredential(flow, "nhatduc-drive", new TokenResponse { RefreshToken = _refreshToken });
    }

    private async Task<string> GetOrCreateSubjectFolderAsync(
        DriveService drive,
        string subjectName,
        CancellationToken cancellationToken)
    {
        var escapedName = subjectName.Replace("'", "\\'");
        var query =
            $"mimeType='application/vnd.google-apps.folder' and '{_rootFolderId}' in parents and name='{escapedName}' and trashed=false";

        var listRequest = drive.Files.List();
        listRequest.Q = query;
        listRequest.Fields = "files(id, name)";
        listRequest.SupportsAllDrives = true;
        listRequest.IncludeItemsFromAllDrives = true;
        listRequest.PageSize = 1;

        var existing = await listRequest.ExecuteAsync(cancellationToken);
        if (existing.Files is { Count: > 0 })
        {
            return existing.Files[0].Id;
        }

        var folderMetadata = new GoogleFile
        {
            Name = subjectName,
            MimeType = "application/vnd.google-apps.folder",
            Parents = [_rootFolderId]
        };

        var createRequest = drive.Files.Create(folderMetadata);
        createRequest.Fields = "id";
        createRequest.SupportsAllDrives = true;
        var folder = await createRequest.ExecuteAsync(cancellationToken);
        return folder.Id;
    }
}

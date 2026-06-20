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

    private readonly GoogleDriveSettings _settings;
    private DriveService? _driveService;

    public GoogleDriveService(GoogleDriveSettings settings)
    {
        _settings = settings;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.ServiceAccountJson)
        || (!string.IsNullOrWhiteSpace(_settings.ClientId)
            && !string.IsNullOrWhiteSpace(_settings.ClientSecret)
            && !string.IsNullOrWhiteSpace(_settings.RefreshToken));

    public string ConfigurationHint =>
        "Cấu hình GoogleDrive trong appsettings hoặc biến môi trường "
        + "GOOGLE_DRIVE_CLIENT_ID, GOOGLE_DRIVE_CLIENT_SECRET, GOOGLE_DRIVE_REFRESH_TOKEN.";

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
        if (!string.IsNullOrWhiteSpace(_settings.ServiceAccountJson))
        {
            return GoogleCredential.FromJson(_settings.ServiceAccountJson).CreateScoped(Scopes);
        }

        if (string.IsNullOrWhiteSpace(_settings.ClientId)
            || string.IsNullOrWhiteSpace(_settings.ClientSecret)
            || string.IsNullOrWhiteSpace(_settings.RefreshToken))
        {
            throw new InvalidOperationException($"Google Drive chưa được cấu hình. {ConfigurationHint}");
        }

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = _settings.ClientId,
                ClientSecret = _settings.ClientSecret
            },
            Scopes = Scopes
        });

        return new UserCredential(flow, "nhatduc-drive", new TokenResponse
        {
            RefreshToken = _settings.RefreshToken
        });
    }

    private async Task<string> GetOrCreateSubjectFolderAsync(
        DriveService drive,
        string subjectName,
        CancellationToken cancellationToken)
    {
        var escapedName = subjectName.Replace("'", "\\'");
        var query =
            $"mimeType='application/vnd.google-apps.folder' and '{_settings.RootFolderId}' in parents and name='{escapedName}' and trashed=false";

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
            Parents = [_settings.RootFolderId]
        };

        var createRequest = drive.Files.Create(folderMetadata);
        createRequest.Fields = "id";
        createRequest.SupportsAllDrives = true;
        var folder = await createRequest.ExecuteAsync(cancellationToken);
        return folder.Id;
    }
}

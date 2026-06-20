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
    // drive.file KHÔNG truy cập được folder tạo thủ công trên Drive UI.
    // Cần scope drive để upload vào folder "Tài Liệu Giảng Dạy" có sẵn.
    private static readonly string[] Scopes = [DriveService.Scope.Drive];

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
        "Chạy scripts/generate-google-drive-token.py để tạo refresh token mới (scope drive), "
        + "rồi cập nhật GOOGLE_DRIVE_CLIENT_ID, GOOGLE_DRIVE_CLIENT_SECRET, GOOGLE_DRIVE_REFRESH_TOKEN trên Render.";

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
            throw new InvalidOperationException("Tên môn/lớp không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException("Tên file không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(_settings.RootFolderId))
        {
            throw new InvalidOperationException("Chưa cấu hình GOOGLE_DRIVE_ROOT_FOLDER_ID.");
        }

        var drive = await GetDriveServiceAsync(cancellationToken);
        var folderId = await GetOrCreateSubjectFolderAsync(drive, subjectName.Trim(), cancellationToken);

        var fileMetadata = new GoogleFile
        {
            Name = fileName.Trim(),
            Parents = [folderId]
        };

        var request = drive.Files.Create(fileMetadata, fileStream, contentType);
        request.Fields = "id, webViewLink";

        var uploadResult = await request.UploadAsync(cancellationToken);
        if (uploadResult.Status != UploadStatus.Completed)
        {
            var detail = uploadResult.Exception?.Message ?? "Upload lên Google Drive thất bại.";
            throw new InvalidOperationException(TranslateDriveError(detail));
        }

        var uploaded = request.ResponseBody
            ?? throw new InvalidOperationException("Google Drive không trả về thông tin file.");

        return new GoogleDriveUploadResult
        {
            FileId = uploaded.Id,
            WebViewLink = uploaded.WebViewLink
        };
    }

    private async Task<DriveService> GetDriveServiceAsync(CancellationToken cancellationToken)
    {
        if (_driveService is not null)
        {
            return _driveService;
        }

        var credential = CreateCredential();
        if (credential is UserCredential userCredential)
        {
            await userCredential.RefreshTokenAsync(cancellationToken);
        }

        _driveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
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
        var escapedName = EscapeDriveQueryValue(subjectName);
        var query =
            $"mimeType='application/vnd.google-apps.folder' and '{_settings.RootFolderId}' in parents and name='{escapedName}' and trashed=false";

        try
        {
            var listRequest = drive.Files.List();
            listRequest.Q = query;
            listRequest.Fields = "files(id, name)";
            listRequest.PageSize = 1;
            listRequest.Spaces = "drive";

            var existing = await listRequest.ExecuteAsync(cancellationToken);
            if (existing.Files is { Count: > 0 } && !string.IsNullOrEmpty(existing.Files[0].Id))
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
            var folder = await createRequest.ExecuteAsync(cancellationToken);

            if (!string.IsNullOrEmpty(folder.Id))
            {
                return folder.Id;
            }
        }
        catch (Google.GoogleApiException ex)
        {
            throw new InvalidOperationException(TranslateDriveError(ex.Message));
        }

        throw new InvalidOperationException($"Không thể tạo folder môn học \"{subjectName}\" trên Google Drive.");
    }

    private static string TranslateDriveError(string message)
    {
        if (message.Contains("File not found", StringComparison.OrdinalIgnoreCase)
            || message.Contains("notFound", StringComparison.OrdinalIgnoreCase))
        {
            return "Không truy cập được folder Google Drive. "
                + "Refresh token hiện tại có thể chỉ có quyền drive.file — "
                + "hãy chạy scripts/generate-google-drive-token.py để tạo token mới với quyền drive đầy đủ, "
                + "rồi cập nhật biến môi trường trên Render.";
        }

        if (message.Contains("invalid_scope", StringComparison.OrdinalIgnoreCase))
        {
            return "Refresh token không khớp quyền drive. "
                + "Chạy scripts/generate-google-drive-token.py và cập nhật GOOGLE_DRIVE_REFRESH_TOKEN trên Render.";
        }

        return message;
    }

    private static string EscapeDriveQueryValue(string value) =>
        value.Replace("\\", "\\\\").Replace("'", "\\'");
}

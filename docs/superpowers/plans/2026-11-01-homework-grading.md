# Homework grading Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Giáo viên tạo bài tập, sao chép tin sang Zalo bằng tay, tải bài học viên để Gemini chấm, lưu phiếu, và tự chọn bài nào ghi vào đánh giá tháng.

**Architecture:** Luật thuần (loại file, điểm, thư mục Drive, đáp án, file chờ, phiếu Gemini) nằm trong `NhatDucSoftware.Core/Helpers` và được test bằng xUnit, không gọi mạng. `HomeworkService` ghi Postgres. `GoogleDriveService` lưu ảnh, PDF và đáp án. `GeminiHomeworkClient` gửi file cho Gemini rồi bỏ video và ghi âm khỏi bộ nhớ. Trang Blazor `/teacher/homework` chỉ ghép các hàm đó. Bản nháp chưa lưu chỉ sống trên trang.

**Tech Stack:** .NET 8, Blazor Server InteractiveServer, xUnit, Npgsql, Google.Apis.Drive.v3, Gemini `generateContent` qua `HttpClient`.

## Global Constraints

- Không gọi Zalo và không thêm thư viện Zalo không chính thức.
- Học viên không đăng nhập. Chỉ vai trò `Teacher`. Giáo viên chỉ thấy lớp từ `ClassService.GetClassesByTeacher`.
- Không trang quản trị, không màn hình desktop, không sửa `NhatDucSoftware/Form1.cs`.
- Không lưu video hoặc ghi âm. Không tự chấm lại khi đổi đáp án. Không tính điểm trung bình tháng.
- Điểm thang 10, tối đa hai chữ số thập phân, từ 0 đến 10. Nhận `8.67` và `8,67`.
- File tối đa 50MB. Bài viết: `jpg`, `jpeg`, `png`, `webp`, `pdf`. Bài nói: `mp3`, `m4a`, `wav`, `mp4`, `webm`, `mov`.
- `INSERT`, `UPDATE`, `DELETE`, `SELECT` viết một dòng. `CREATE TABLE` được nhiều dòng, giống `Announcements`.
- Khóa Gemini chỉ ở `GEMINI_API_KEY`. Model mặc định `gemini-2.5-flash`, cấu hình `Gemini:Model`, ghi đè bằng `GEMINI_MODEL`.
- Thư mục gốc Drive `1i9Y9XjhW2emcYJzC5F3Ho1HzFzDLLIiU`, biến `GOOGLE_DRIVE_HOMEWORK_ROOT_FOLDER_ID`.
- Không chạy `dotnet run` cho web. Khởi động web sẽ mở cơ sở dữ liệu thật. Chỉ `dotnet test` và `dotnet build`.
- Commit bằng PowerShell here-string. Không dùng `&&` và không dùng bash heredoc.
- Không commit file chứa khóa API.

---

## File structure

- `NhatDucSoftware.Core/Helpers/HomeworkFileRules.cs` — loại file và 50MB.
- `NhatDucSoftware.Core/Helpers/HomeworkScoreParser.cs` — đọc điểm.
- `NhatDucSoftware.Core/Helpers/HomeworkDrivePath.cs` — tên môn và tên thư mục.
- `NhatDucSoftware.Core/Helpers/HomeworkAnnouncement.cs` — tin giao bài, không có đáp án.
- `NhatDucSoftware.Core/Helpers/HomeworkGradingRules.cs` — tạo bài, đáp án, quyền lớp, học viên.
- `NhatDucSoftware.Core/Helpers/HomeworkSubmissionState.cs` — chuyển file chờ thành file đã lưu.
- `NhatDucSoftware.Core/Helpers/HomeworkMonthlyPush.cs` — tham số ghi đánh giá tháng.
- `NhatDucSoftware.Core/Helpers/GeminiGradeParser.cs` — prompt và phân tích JSON.
- `NhatDucSoftware.Core/Models/HomeworkModels.cs` — hằng loại bài và dòng dữ liệu.
- `NhatDucSoftware.Core/Data/HomeworkSchema.cs` — `CREATE TABLE`.
- `NhatDucSoftware.Core/Services/HomeworkSql.cs` — câu lệnh một dòng.
- `NhatDucSoftware.Core/Services/HomeworkService.cs` — đọc ghi Postgres và gọi Drive.
- `NhatDucSoftware.Core/Services/GeminiHomeworkClient.cs` — gọi Gemini.
- `NhatDucSoftware.Web/Components/Pages/Teacher/Homework.razor` — trang giáo viên.
- Sửa `DatabaseInitializer.cs`, `GoogleDriveSettings.cs`, `GoogleDriveService.cs`, `Program.cs`, `MainLayout.razor`, `appsettings.json`.
- Test tương ứng trong `NhatDucSoftware.Core.Tests/`.

### Task 1: Loại file

**Files:**
- Create: `NhatDucSoftware.Core/Helpers/HomeworkFileRules.cs`
- Test: `NhatDucSoftware.Core.Tests/HomeworkFileRulesTests.cs`

**Interfaces:**
- Consumes: không có.
- Produces: `HomeworkFileRules.MaxBytes`, `HomeworkFileRules.IsAccepted(string kind, string fileName, long size, out string error)`. `kind` là `Written` hoặc `Speaking`.

- [ ] **Step 1: Write the failing test**

```csharp
using NhatDucSoftware.Core.Helpers;

namespace NhatDucSoftware.Core.Tests;

public class HomeworkFileRulesTests
{
    [Theory]
    [InlineData("Written", "bai.jpg")]
    [InlineData("Written", "bai.JPEG")]
    [InlineData("Written", "bai.png")]
    [InlineData("Written", "bai.webp")]
    [InlineData("Written", "bai.pdf")]
    [InlineData("Speaking", "noi.mp3")]
    [InlineData("Speaking", "noi.m4a")]
    [InlineData("Speaking", "noi.wav")]
    [InlineData("Speaking", "noi.mp4")]
    [InlineData("Speaking", "noi.webm")]
    [InlineData("Speaking", "noi.mov")]
    public void IsAccepted_AllowsListedTypes(string kind, string fileName)
    {
        Assert.True(HomeworkFileRules.IsAccepted(kind, fileName, 1024, out _));
    }

    [Theory]
    [InlineData("Written", "noi.mp4")]
    [InlineData("Speaking", "bai.pdf")]
    [InlineData("Written", "bai.docx")]
    public void IsAccepted_RejectsTheOtherKind(string kind, string fileName)
    {
        Assert.False(HomeworkFileRules.IsAccepted(kind, fileName, 1024, out var error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void IsAccepted_RejectsOver50MbWithoutChangingMeaningOfTheLimit()
    {
        Assert.False(HomeworkFileRules.IsAccepted("Written", "bai.jpg", HomeworkFileRules.MaxBytes + 1, out var error));
        Assert.Contains("50MB", error);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test NhatDucSoftware.Core.Tests/NhatDucSoftware.Core.Tests.csproj --filter HomeworkFileRulesTests -v q`

Expected: FAIL because `HomeworkFileRules` does not exist.

- [ ] **Step 3: Write minimal implementation**

```csharp
namespace NhatDucSoftware.Core.Helpers;

public static class HomeworkFileRules
{
    public const long MaxBytes = 50L * 1024L * 1024L;

    private static readonly string[] Written = ["jpg", "jpeg", "png", "webp", "pdf"];
    private static readonly string[] Speaking = ["mp3", "m4a", "wav", "mp4", "webm", "mov"];

    public static bool IsAccepted(string kind, string fileName, long size, out string error)
    {
        error = "";
        if (size > MaxBytes)
        {
            error = "File quá lớn. Giới hạn 50MB.";
            return false;
        }

        var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        var allowed = kind == "Speaking" ? Speaking : Written;
        if (!allowed.Contains(ext))
        {
            error = kind == "Speaking"
                ? "Bài nói chỉ nhận video hoặc ghi âm."
                : "Bài viết chỉ nhận ảnh hoặc PDF.";
            return false;
        }

        return true;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test NhatDucSoftware.Core.Tests/NhatDucSoftware.Core.Tests.csproj --filter HomeworkFileRulesTests -v q`

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add -- NhatDucSoftware.Core/Helpers/HomeworkFileRules.cs NhatDucSoftware.Core.Tests/HomeworkFileRulesTests.cs
git commit -m @"
Reject homework uploads that are the wrong type or over 50MB.

"@
```

### Task 2: Đọc điểm

**Files:**
- Create: `NhatDucSoftware.Core/Helpers/HomeworkScoreParser.cs`
- Test: `NhatDucSoftware.Core.Tests/HomeworkScoreParserTests.cs`

**Interfaces:**
- Consumes: không có.
- Produces: `HomeworkScoreParser.TryParse(string? text, out decimal score)`.

- [ ] **Step 1: Write the failing test**

```csharp
using NhatDucSoftware.Core.Helpers;

namespace NhatDucSoftware.Core.Tests;

public class HomeworkScoreParserTests
{
    [Theory]
    [InlineData("8,67", 8.67)]
    [InlineData("8.67", 8.67)]
    [InlineData("7,5", 7.5)]
    [InlineData("0", 0)]
    [InlineData("10", 10)]
    public void TryParse_AcceptsScaleOfTen(string text, decimal expected)
    {
        Assert.True(HomeworkScoreParser.TryParse(text, out var score));
        Assert.Equal(expected, score);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("tám")]
    [InlineData("-1")]
    [InlineData("10.01")]
    public void TryParse_RejectsMissingOrOutOfRange(string? text)
    {
        Assert.False(HomeworkScoreParser.TryParse(text, out _));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test NhatDucSoftware.Core.Tests/NhatDucSoftware.Core.Tests.csproj --filter HomeworkScoreParserTests -v q`

Expected: FAIL because the type does not exist.

- [ ] **Step 3: Write minimal implementation**

```csharp
using System.Globalization;

namespace NhatDucSoftware.Core.Helpers;

public static class HomeworkScoreParser
{
    public static bool TryParse(string? text, out decimal score)
    {
        score = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim().Replace(',', '.');
        if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return false;
        }

        if (parsed < 0 || parsed > 10)
        {
            return false;
        }

        score = Math.Round(parsed, 2, MidpointRounding.AwayFromZero);
        return true;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test NhatDucSoftware.Core.Tests/NhatDucSoftware.Core.Tests.csproj --filter HomeworkScoreParserTests -v q`

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add -- NhatDucSoftware.Core/Helpers/HomeworkScoreParser.cs NhatDucSoftware.Core.Tests/HomeworkScoreParserTests.cs
git commit -m @"
Parse homework scores on a 0 to 10 scale.

"@
```

### Task 3: Thư mục Drive và tin giao bài

**Files:**
- Create: `NhatDucSoftware.Core/Helpers/HomeworkDrivePath.cs`
- Create: `NhatDucSoftware.Core/Helpers/HomeworkAnnouncement.cs`
- Test: `NhatDucSoftware.Core.Tests/HomeworkDrivePathTests.cs`

**Interfaces:**
- Consumes: không có.
- Produces: `HomeworkDrivePath.SubjectFolder(string? courseName, string? courseCode, string className)`, `HomeworkDrivePath.AssignmentFolder(string className, string title)`, `HomeworkAnnouncement.Build(string className, string title, DateTime assignedOn, string prompt)`.

- [ ] **Step 1: Write the failing test**

```csharp
using NhatDucSoftware.Core.Helpers;

namespace NhatDucSoftware.Core.Tests;

public class HomeworkDrivePathTests
{
    [Fact]
    public void SubjectFolder_UsesCourseName()
    {
        Assert.Equal("Tiếng Anh", HomeworkDrivePath.SubjectFolder("Tiếng Anh", "ENG", "Grade 2A"));
    }

    [Fact]
    public void SubjectFolder_FallsBackToCourseCodeThenClassName()
    {
        Assert.Equal("ENG", HomeworkDrivePath.SubjectFolder("  ", "ENG", "Grade 2A"));
        Assert.Equal("Grade 2A", HomeworkDrivePath.SubjectFolder(null, " ", "Grade 2A"));
    }

    [Fact]
    public void AssignmentFolder_JoinsClassAndTitle()
    {
        Assert.Equal("Grade 2A - Unit 3", HomeworkDrivePath.AssignmentFolder("Grade 2A", "Unit 3"));
    }

    [Fact]
    public void Announcement_OmitsAnswerKey()
    {
        var text = HomeworkAnnouncement.Build("Grade 2A", "Unit 3", new DateTime(2026, 11, 1), "Viết 5 câu.");
        Assert.Contains("Grade 2A", text);
        Assert.Contains("Unit 3", text);
        Assert.Contains("01/11/2026", text);
        Assert.Contains("Viết 5 câu.", text);
        Assert.DoesNotContain("đáp án", text, StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test NhatDucSoftware.Core.Tests/NhatDucSoftware.Core.Tests.csproj --filter HomeworkDrivePathTests -v q`

Expected: FAIL because the types do not exist.

- [ ] **Step 3: Write minimal implementation**

`HomeworkDrivePath.cs`:

```csharp
namespace NhatDucSoftware.Core.Helpers;

public static class HomeworkDrivePath
{
    public static string SubjectFolder(string? courseName, string? courseCode, string className)
    {
        if (!string.IsNullOrWhiteSpace(courseName))
        {
            return courseName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(courseCode))
        {
            return courseCode.Trim();
        }

        return className.Trim();
    }

    public static string AssignmentFolder(string className, string title)
    {
        return $"{className.Trim()} - {title.Trim()}";
    }
}
```

`HomeworkAnnouncement.cs`:

```csharp
namespace NhatDucSoftware.Core.Helpers;

public static class HomeworkAnnouncement
{
    public static string Build(string className, string title, DateTime assignedOn, string prompt)
    {
        return $"BÀI TẬP{Environment.NewLine}Lớp: {className.Trim()}{Environment.NewLine}Bài: {title.Trim()}{Environment.NewLine}Ngày giao: {assignedOn:dd/MM/yyyy}{Environment.NewLine}Đề:{Environment.NewLine}{prompt.Trim()}";
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test NhatDucSoftware.Core.Tests/NhatDucSoftware.Core.Tests.csproj --filter HomeworkDrivePathTests -v q`

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add -- NhatDucSoftware.Core/Helpers/HomeworkDrivePath.cs NhatDucSoftware.Core/Helpers/HomeworkAnnouncement.cs NhatDucSoftware.Core.Tests/HomeworkDrivePathTests.cs
git commit -m @"
Build homework Drive folder names and the Zalo assignment text.

"@
```

### Task 4: Điều kiện chấm, file chờ, điểm tháng

**Files:**
- Create: `NhatDucSoftware.Core/Helpers/HomeworkGradingRules.cs`
- Create: `NhatDucSoftware.Core/Helpers/HomeworkSubmissionState.cs`
- Create: `NhatDucSoftware.Core/Helpers/HomeworkMonthlyPush.cs`
- Test: `NhatDucSoftware.Core.Tests/HomeworkGradingRulesTests.cs`

**Interfaces:**
- Consumes: `HomeworkScoreParser.TryParse`.
- Produces:
  - `HomeworkGradingRules.TryValidateCreate(string? title, string? prompt, string? kind, out string error)`
  - `HomeworkGradingRules.CanGrade(string kind, string? answerKeyText, string? answerKeyDriveFileId, out string error)`
  - `HomeworkGradingRules.CanAccess(bool teachesClass, bool studentInClass, out string error)`
  - `HomeworkSubmissionState.Commit(string? savedFileId, string? pendingFileId, string? pendingName, string? pendingLink, string? savedName, string? savedLink)` trả `HomeworkFileCommit`
  - `HomeworkMonthlyPush.TryBuild(decimal? score, string? shortComment, DateTime assignedOn, out HomeworkMonthlyWrite write, out string error)`

`HomeworkFileCommit` có `SavedFileName`, `SavedDriveFileId`, `SavedWebViewLink`, `DriveFileIdToDelete`. `HomeworkMonthlyWrite` có `Score`, `ShortComment`, `Year`, `Month` và không có phiếu.

- [ ] **Step 1: Write the failing test**

```csharp
using NhatDucSoftware.Core.Helpers;

namespace NhatDucSoftware.Core.Tests;

public class HomeworkGradingRulesTests
{
    [Fact]
    public void CanGrade_BlocksWrittenWorkWithoutAnAnswerKey()
    {
        Assert.False(HomeworkGradingRules.CanGrade("Written", "  ", null, out var error));
        Assert.Contains("đáp án", error, StringComparison.OrdinalIgnoreCase);
        Assert.True(HomeworkGradingRules.CanGrade("Written", "A", null, out _));
        Assert.True(HomeworkGradingRules.CanGrade("Written", null, "drive-1", out _));
        Assert.True(HomeworkGradingRules.CanGrade("Speaking", null, null, out _));
    }

    [Fact]
    public void CanAccess_RejectsAnotherClassOrStudent()
    {
        Assert.False(HomeworkGradingRules.CanAccess(false, true, out var classError));
        Assert.Contains("không dạy", classError);
        Assert.False(HomeworkGradingRules.CanAccess(true, false, out var studentError));
        Assert.Contains("không thuộc lớp", studentError);
    }

    [Fact]
    public void Commit_KeepsSavedFileUntilPendingIsPromoted()
    {
        var unchanged = HomeworkSubmissionState.Commit("saved", null, null, null, "old.pdf", "http://old");
        Assert.Equal("saved", unchanged.SavedDriveFileId);
        Assert.Null(unchanged.DriveFileIdToDelete);

        var promoted = HomeworkSubmissionState.Commit("saved", "pending", "new.pdf", "http://new", "old.pdf", "http://old");
        Assert.Equal("pending", promoted.SavedDriveFileId);
        Assert.Equal("saved", promoted.DriveFileIdToDelete);
    }

    [Fact]
    public void TryBuild_SendsScoreAndShortCommentForTheAssignmentMonth()
    {
        Assert.True(HomeworkMonthlyPush.TryBuild(8.67m, "Tốt", new DateTime(2026, 11, 3), out var write, out _));
        Assert.Equal(8.67m, write.Score);
        Assert.Equal("Tốt", write.ShortComment);
        Assert.Equal(2026, write.Year);
        Assert.Equal(11, write.Month);
        Assert.False(HomeworkMonthlyPush.TryBuild(null, "Tốt", new DateTime(2026, 11, 3), out _, out var error));
        Assert.Contains("lưu phiếu", error, StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test NhatDucSoftware.Core.Tests/NhatDucSoftware.Core.Tests.csproj --filter HomeworkGradingRulesTests -v q`

Expected: FAIL because the types do not exist.

- [ ] **Step 3: Write minimal implementation**

`HomeworkGradingRules.cs`:

```csharp
namespace NhatDucSoftware.Core.Helpers;

public static class HomeworkGradingRules
{
    public static bool TryValidateCreate(string? title, string? prompt, string? kind, out string error)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(prompt))
        {
            error = "Nhập tên bài và đề bài.";
            return false;
        }

        if (kind is not ("Written" or "Speaking"))
        {
            error = "Chọn loại bài viết hoặc bài nói.";
            return false;
        }

        error = "";
        return true;
    }

    public static bool CanGrade(string kind, string? answerKeyText, string? answerKeyDriveFileId, out string error)
    {
        if (kind == "Written"
            && string.IsNullOrWhiteSpace(answerKeyText)
            && string.IsNullOrWhiteSpace(answerKeyDriveFileId))
        {
            error = "Bài viết cần đáp án trước khi chấm.";
            return false;
        }

        error = "";
        return true;
    }

    public static bool CanAccess(bool teachesClass, bool studentInClass, out string error)
    {
        if (!teachesClass)
        {
            error = "Bạn không dạy lớp này.";
            return false;
        }

        if (!studentInClass)
        {
            error = "Học viên không thuộc lớp này.";
            return false;
        }

        error = "";
        return true;
    }
}
```

`HomeworkSubmissionState.cs`:

```csharp
namespace NhatDucSoftware.Core.Helpers;

public readonly record struct HomeworkFileCommit(
    string? SavedFileName,
    string? SavedDriveFileId,
    string? SavedWebViewLink,
    string? DriveFileIdToDelete);

public static class HomeworkSubmissionState
{
    public static HomeworkFileCommit Commit(
        string? savedFileId,
        string? pendingFileId,
        string? pendingName,
        string? pendingLink,
        string? savedName,
        string? savedLink)
    {
        if (string.IsNullOrWhiteSpace(pendingFileId))
        {
            return new HomeworkFileCommit(savedName, savedFileId, savedLink, null);
        }

        var delete = string.Equals(savedFileId, pendingFileId, StringComparison.Ordinal)
            ? null
            : savedFileId;
        if (string.IsNullOrWhiteSpace(delete))
        {
            delete = null;
        }

        return new HomeworkFileCommit(pendingName, pendingFileId, pendingLink, delete);
    }
}
```

`HomeworkMonthlyPush.cs`:

```csharp
namespace NhatDucSoftware.Core.Helpers;

public readonly record struct HomeworkMonthlyWrite(decimal Score, string ShortComment, int Year, int Month);

public static class HomeworkMonthlyPush
{
    public static bool TryBuild(decimal? score, string? shortComment, DateTime assignedOn, out HomeworkMonthlyWrite write, out string error)
    {
        write = default;
        if (score is null)
        {
            error = "Hãy lưu phiếu trước khi đưa vào đánh giá tháng.";
            return false;
        }

        if (score < 0 || score > 10)
        {
            error = "Điểm phải từ 0 đến 10.";
            return false;
        }

        write = new HomeworkMonthlyWrite(score.Value, shortComment?.Trim() ?? "", assignedOn.Year, assignedOn.Month);
        error = "";
        return true;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test NhatDucSoftware.Core.Tests/NhatDucSoftware.Core.Tests.csproj --filter HomeworkGradingRulesTests -v q`

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add -- NhatDucSoftware.Core/Helpers/HomeworkGradingRules.cs NhatDucSoftware.Core/Helpers/HomeworkSubmissionState.cs NhatDucSoftware.Core/Helpers/HomeworkMonthlyPush.cs NhatDucSoftware.Core.Tests/HomeworkGradingRulesTests.cs
git commit -m @"
Decide when a homework score can be graded, saved, and pushed.

"@
```

### Task 5: Prompt và phiếu Gemini

**Files:**
- Create: `NhatDucSoftware.Core/Helpers/GeminiGradeParser.cs`
- Test: `NhatDucSoftware.Core.Tests/GeminiGradeParserTests.cs`

**Interfaces:**
- Consumes: `HomeworkScoreParser.TryParse`.
- Produces: `GeminiGradePrompt.Build(...)` và `GeminiGradeParser.Parse(string responseText)` trả `GeminiGradeDraft` với `decimal? Score`, `string ShortComment`, `string ReportText`, `bool ScoreMissing`.

- [ ] **Step 1: Write the failing test**

```csharp
using NhatDucSoftware.Core.Helpers;

namespace NhatDucSoftware.Core.Tests;

public class GeminiGradeParserTests
{
    [Fact]
    public void Build_RequiresWrittenSlipHeaderAndSpeakingCriteria()
    {
        var written = GeminiGradePrompt.Build("Written", "Grade 2A", "Unit 3", "An", "Đề", "Đáp án A");
        Assert.Contains("PHIẾU CHẤM & SỬA BÀI", written);
        Assert.Contains("Đáp án A", written);

        var speaking = GeminiGradePrompt.Build("Speaking", "Grade 2A", "Unit 3", "An", "Đề", null);
        Assert.Contains("phát âm", speaking);
        Assert.Contains("độ trôi chảy", speaking);
        Assert.Contains("ngữ pháp", speaking);
        Assert.Contains("từ vựng", speaking);
        Assert.Contains("nội dung", speaking);
    }

    [Fact]
    public void Parse_ReadsScoreAndLeavesInvalidScoreForTheTeacher()
    {
        var ok = GeminiGradeParser.Parse("{\"score\":\"8,67\",\"shortComment\":\"Tốt\",\"report\":\"PHIẾU CHẤM & SỬA BÀI\"}");
        Assert.Equal(8.67m, ok.Score);
        Assert.Equal("Tốt", ok.ShortComment);
        Assert.False(ok.ScoreMissing);

        var bad = GeminiGradeParser.Parse("```json\n{\"score\":\"12\",\"shortComment\":\"Xem lại\",\"report\":\"Phiếu\"}\n```");
        Assert.Null(bad.Score);
        Assert.True(bad.ScoreMissing);
        Assert.Equal("Xem lại", bad.ShortComment);
        Assert.Equal("Phiếu", bad.ReportText);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test NhatDucSoftware.Core.Tests/NhatDucSoftware.Core.Tests.csproj --filter GeminiGradeParserTests -v q`

Expected: FAIL because the types do not exist.

- [ ] **Step 3: Write minimal implementation**

```csharp
using System.Text.Json;

namespace NhatDucSoftware.Core.Helpers;

public sealed class GeminiGradeDraft
{
    public decimal? Score { get; init; }
    public string ShortComment { get; init; } = "";
    public string ReportText { get; init; } = "";
    public bool ScoreMissing { get; init; }
}

public static class GeminiGradePrompt
{
    public static string Build(string kind, string className, string title, string studentName, string prompt, string? answerKeyText)
    {
        var shared = $"Trả về JSON thuần với các khóa score, shortComment, report. score là điểm trên thang 10 hoặc chuỗi rỗng nếu không chấm được. Học viên: {studentName}. Lớp: {className}. Bài: {title}. Đề: {prompt}.";
        if (kind == "Speaking")
        {
            return shared + " Đây là bài nói. report gồm điểm và nhận xét phát âm, độ trôi chảy, ngữ pháp, từ vựng, nội dung.";
        }

        return shared + " Đây là bài viết. Đối chiếu với đáp án: " + (answerKeyText ?? "") + ". report bắt đầu bằng dòng PHIẾU CHẤM & SỬA BÀI, rồi tên học viên, lớp, tên bài, điểm, từng chỗ đúng hoặc sai kèm bài làm, đáp án đúng và câu sửa, cuối là nhận xét chung.";
    }
}

public static class GeminiGradeParser
{
    public static GeminiGradeDraft Parse(string responseText)
    {
        var json = responseText.Trim();
        if (json.StartsWith("```", StringComparison.Ordinal))
        {
            json = json.Trim('`');
            if (json.StartsWith("json", StringComparison.OrdinalIgnoreCase))
            {
                json = json[4..];
            }

            json = json.Trim();
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var scoreText = ReadJsonText(root, "score");
        var parsed = HomeworkScoreParser.TryParse(scoreText, out var score);
        return new GeminiGradeDraft
        {
            Score = parsed ? score : null,
            ScoreMissing = !parsed,
            ShortComment = ReadJsonText(root, "shortComment"),
            ReportText = ReadJsonText(root, "report")
        };
    }

    private static string ReadJsonText(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var node) || node.ValueKind == JsonValueKind.Null)
        {
            return "";
        }

        return node.ValueKind == JsonValueKind.String ? node.GetString() ?? "" : node.ToString();
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test NhatDucSoftware.Core.Tests/NhatDucSoftware.Core.Tests.csproj --filter GeminiGradeParserTests -v q`

Expected: PASS. `score` dạng số JSON vẫn qua `ToString()` rồi `TryParse`.

- [ ] **Step 5: Commit**

```powershell
git add -- NhatDucSoftware.Core/Helpers/GeminiGradeParser.cs NhatDucSoftware.Core.Tests/GeminiGradeParserTests.cs
git commit -m @"
Parse Gemini homework slips and require the written report header.

"@
```

### Task 6: Bảng và cấu hình

**Files:**
- Create: `NhatDucSoftware.Core/Data/HomeworkSchema.cs`
- Create: `NhatDucSoftware.Core/Services/HomeworkSql.cs`
- Modify: `NhatDucSoftware.Core/Data/DatabaseInitializer.cs` sau dòng `MigratePlaintextPasswords(connection);`
- Modify: `NhatDucSoftware.Core/Services/GoogleDriveSettings.cs`
- Modify: `NhatDucSoftware.Web/Program.cs` trong khối tạo `GoogleDriveSettings`
- Modify: `NhatDucSoftware.Web/appsettings.json` chỉ thêm khóa, không đổi khóa khác
- Test: `NhatDucSoftware.Core.Tests/HomeworkSqlTests.cs`

**Interfaces:**
- Consumes: không có.
- Produces: `HomeworkSchema.CreateTables`, `HomeworkSql.InsertAssignment`, `HomeworkSql.UpsertPendingFile`, `HomeworkSql.SaveWrittenGrade`, `HomeworkSql.SaveSpeakingGrade`, `GoogleDriveSettings.HomeworkRootFolderId`.

- [ ] **Step 1: Write the failing test**

```csharp
using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Core.Services;

namespace NhatDucSoftware.Core.Tests;

public class HomeworkSqlTests
{
    [Fact]
    public void Statements_AreSingleLineAndKeepPendingSeparateFromSaved()
    {
        Assert.Contains("UNIQUE (AssignmentId, StudentId)", HomeworkSchema.CreateTables);
        Assert.DoesNotContain("\n", HomeworkSql.InsertAssignment);
        Assert.DoesNotContain("\n", HomeworkSql.UpsertPendingFile);
        Assert.DoesNotContain("\n", HomeworkSql.SaveWrittenGrade);
        Assert.DoesNotContain("\n", HomeworkSql.SaveSpeakingGrade);
        Assert.Contains("PendingDriveFileId", HomeworkSql.UpsertPendingFile);
        Assert.Contains("COALESCE(PendingDriveFileId, DriveFileId)", HomeworkSql.SaveWrittenGrade);
        Assert.DoesNotContain("DriveFileId", HomeworkSql.SaveSpeakingGrade);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test NhatDucSoftware.Core.Tests/NhatDucSoftware.Core.Tests.csproj --filter HomeworkSqlTests -v q`

Expected: FAIL because the types do not exist.

- [ ] **Step 3: Write minimal implementation**

`HomeworkSchema.cs`:

```csharp
namespace NhatDucSoftware.Core.Data;

public static class HomeworkSchema
{
    public const string CreateTables = @"
CREATE TABLE IF NOT EXISTS HomeworkAssignments (
    Id BIGSERIAL PRIMARY KEY,
    ClassId BIGINT NOT NULL,
    TeacherId BIGINT NOT NULL,
    Title TEXT NOT NULL,
    AssignedOn TEXT NOT NULL,
    Kind TEXT NOT NULL,
    Prompt TEXT NOT NULL,
    AnswerKeyText TEXT NULL,
    AnswerKeyFileName TEXT NULL,
    AnswerKeyDriveFileId TEXT NULL,
    AnswerKeyDriveWebViewLink TEXT NULL,
    DriveFolderId TEXT NULL,
    CreatedAt TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS HomeworkSubmissions (
    Id BIGSERIAL PRIMARY KEY,
    AssignmentId BIGINT NOT NULL,
    StudentId BIGINT NOT NULL,
    Score NUMERIC(5,2) NULL,
    ShortComment TEXT NULL,
    ReportText TEXT NULL,
    FileName TEXT NULL,
    DriveFileId TEXT NULL,
    DriveWebViewLink TEXT NULL,
    PendingFileName TEXT NULL,
    PendingDriveFileId TEXT NULL,
    PendingDriveWebViewLink TEXT NULL,
    GradedAt TEXT NULL,
    CONSTRAINT UQ_HomeworkSubmissions_Assignment_Student UNIQUE (AssignmentId, StudentId)
);";
}
```

`HomeworkSql.cs`:

```csharp
namespace NhatDucSoftware.Core.Services;

public static class HomeworkSql
{
    public const string InsertAssignment = "INSERT INTO HomeworkAssignments(ClassId, TeacherId, Title, AssignedOn, Kind, Prompt, AnswerKeyText, AnswerKeyFileName, AnswerKeyDriveFileId, AnswerKeyDriveWebViewLink, DriveFolderId, CreatedAt) VALUES(@classId, @teacherId, @title, @assignedOn, @kind, @prompt, @answerKeyText, @answerKeyFileName, @answerKeyDriveFileId, @answerKeyDriveWebViewLink, @driveFolderId, @createdAt) RETURNING Id;";
    public const string UpsertPendingFile = "INSERT INTO HomeworkSubmissions(AssignmentId, StudentId, PendingFileName, PendingDriveFileId, PendingDriveWebViewLink) VALUES(@assignmentId, @studentId, @pendingFileName, @pendingDriveFileId, @pendingDriveWebViewLink) ON CONFLICT (AssignmentId, StudentId) DO UPDATE SET PendingFileName = EXCLUDED.PendingFileName, PendingDriveFileId = EXCLUDED.PendingDriveFileId, PendingDriveWebViewLink = EXCLUDED.PendingDriveWebViewLink;";
    public const string SaveWrittenGrade = "UPDATE HomeworkSubmissions SET Score = @score, ShortComment = @shortComment, ReportText = @reportText, FileName = COALESCE(PendingFileName, FileName), DriveFileId = COALESCE(PendingDriveFileId, DriveFileId), DriveWebViewLink = COALESCE(PendingDriveWebViewLink, DriveWebViewLink), PendingFileName = NULL, PendingDriveFileId = NULL, PendingDriveWebViewLink = NULL, GradedAt = @gradedAt WHERE AssignmentId = @assignmentId AND StudentId = @studentId;";
    public const string SaveSpeakingGrade = "INSERT INTO HomeworkSubmissions(AssignmentId, StudentId, Score, ShortComment, ReportText, GradedAt) VALUES(@assignmentId, @studentId, @score, @shortComment, @reportText, @gradedAt) ON CONFLICT (AssignmentId, StudentId) DO UPDATE SET Score = EXCLUDED.Score, ShortComment = EXCLUDED.ShortComment, ReportText = EXCLUDED.ReportText, GradedAt = EXCLUDED.GradedAt;";
}
```

Trong `DatabaseInitializer.Initialize`, gọi `MigrateHomeworkTables(connection);` ngay sau `MigratePlaintextPasswords(connection);`. Thêm phương thức:

```csharp
private static void MigrateHomeworkTables(System.Data.Common.DbConnection connection)
{
    foreach (var statement in HomeworkSchema.CreateTables.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        using var command = connection.CreateCommand();
        command.CommandText = statement;
        command.ExecuteNonQuery();
    }
}
```

Thêm `using` nếu file chưa import namespace chứa `HomeworkSchema` (cùng namespace `NhatDucSoftware.Core.Data` thì không cần).

Thêm vào `GoogleDriveSettings`:

```csharp
public string HomeworkRootFolderId { get; set; } = "1i9Y9XjhW2emcYJzC5F3Ho1HzFzDLLIiU";
```

Trong `Program.cs`, khi tạo `GoogleDriveSettings`, thêm:

```csharp
HomeworkRootFolderId = Environment.GetEnvironmentVariable("GOOGLE_DRIVE_HOMEWORK_ROOT_FOLDER_ID")
    ?? config["GoogleDrive:HomeworkRootFolderId"]
    ?? "1i9Y9XjhW2emcYJzC5F3Ho1HzFzDLLIiU",
```

Trong `appsettings.json`, thêm vào object `GoogleDrive` khóa `"HomeworkRootFolderId": "1i9Y9XjhW2emcYJzC5F3Ho1HzFzDLLIiU"` và thêm object `"Gemini": { "Model": "gemini-2.5-flash" }`. Không ghi khóa API vào file.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test NhatDucSoftware.Core.Tests/NhatDucSoftware.Core.Tests.csproj --filter HomeworkSqlTests -v q`

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add -- NhatDucSoftware.Core/Data/HomeworkSchema.cs NhatDucSoftware.Core/Services/HomeworkSql.cs NhatDucSoftware.Core/Data/DatabaseInitializer.cs NhatDucSoftware.Core/Services/GoogleDriveSettings.cs NhatDucSoftware.Web/Program.cs NhatDucSoftware.Web/appsettings.json NhatDucSoftware.Core.Tests/HomeworkSqlTests.cs
git commit -m @"
Add homework tables and the Drive folder used for assignment files.

"@
```

### Task 7: Upload Drive cho bài tập

**Files:**
- Modify: `NhatDucSoftware.Core/Services/GoogleDriveService.cs`
- Test: `NhatDucSoftware.Core.Tests/HomeworkDriveFolderChoiceTests.cs`

**Interfaces:**
- Consumes: `GetOrCreateChildFolderAsync`, `HomeworkRootFolderId`.
- Produces: `GoogleDriveHomeworkUpload` với `FileId`, `WebViewLink`, `AssignmentFolderId`. `UploadHomeworkFileAsync(string? existingFolderId, string teacherName, string subjectName, string assignmentFolderName, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)`. `DeleteFileAsync(string? fileId, CancellationToken cancellationToken = default)`. `DownloadFileAsync(string fileId, CancellationToken cancellationToken = default)` trả `byte[]`. `HomeworkDriveFolderChoice.UseExisting(string? existingFolderId)`.

- [ ] **Step 1: Write the failing test**

```csharp
using NhatDucSoftware.Core.Services;

namespace NhatDucSoftware.Core.Tests;

public class HomeworkDriveFolderChoiceTests
{
    [Fact]
    public void UseExisting_KeepsTheStoredFolderAfterRename()
    {
        Assert.True(HomeworkDriveFolderChoice.UseExisting("folder-1"));
        Assert.False(HomeworkDriveFolderChoice.UseExisting("  "));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test NhatDucSoftware.Core.Tests/NhatDucSoftware.Core.Tests.csproj --filter HomeworkDriveFolderChoiceTests -v q`

Expected: FAIL because the type does not exist.

- [ ] **Step 3: Write minimal implementation**

Thêm vào cuối file `GoogleDriveService.cs`, trước các phương thức private, class kết quả và hàm chọn thư mục:

```csharp
public sealed class GoogleDriveHomeworkUpload
{
    public string FileId { get; set; } = "";
    public string? WebViewLink { get; set; }
    public string AssignmentFolderId { get; set; } = "";
}

public static class HomeworkDriveFolderChoice
{
    public static bool UseExisting(string? existingFolderId) => !string.IsNullOrWhiteSpace(existingFolderId);
}
```

Đặt class kết quả cạnh `GoogleDriveUploadResult` ở đầu file thay vì bên trong class. `HomeworkDriveFolderChoice` cũng để ngoài class.

Thêm phương thức public trong `GoogleDriveService`:

```csharp
public async Task<GoogleDriveHomeworkUpload> UploadHomeworkFileAsync(
    string? existingFolderId,
    string teacherName,
    string subjectName,
    string assignmentFolderName,
    Stream fileStream,
    string fileName,
    string contentType,
    CancellationToken cancellationToken = default)
{
    if (!IsConfigured)
    {
        throw new InvalidOperationException($"Google Drive chưa được cấu hình. {ConfigurationHint}");
    }

    if (string.IsNullOrWhiteSpace(_settings.HomeworkRootFolderId))
    {
        throw new InvalidOperationException("Chưa cấu hình GOOGLE_DRIVE_HOMEWORK_ROOT_FOLDER_ID.");
    }

    var drive = await GetDriveServiceAsync(cancellationToken);
    string folderId;
    if (HomeworkDriveFolderChoice.UseExisting(existingFolderId))
    {
        folderId = existingFolderId!.Trim();
    }
    else
    {
        var teacherId = await GetOrCreateChildFolderAsync(drive, _settings.HomeworkRootFolderId, teacherName.Trim(), cancellationToken);
        var subjectId = await GetOrCreateChildFolderAsync(drive, teacherId, subjectName.Trim(), cancellationToken);
        folderId = await GetOrCreateChildFolderAsync(drive, subjectId, assignmentFolderName.Trim(), cancellationToken);
    }

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
    return new GoogleDriveHomeworkUpload
    {
        FileId = uploaded.Id,
        WebViewLink = uploaded.WebViewLink,
        AssignmentFolderId = folderId
    };
}

public async Task DeleteFileAsync(string? fileId, CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(fileId) || !IsConfigured)
    {
        return;
    }

    var drive = await GetDriveServiceAsync(cancellationToken);
    try
    {
        await drive.Files.Delete(fileId).ExecuteAsync(cancellationToken);
    }
    catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
    {
    }
}

public async Task<byte[]> DownloadFileAsync(string fileId, CancellationToken cancellationToken = default)
{
    if (!IsConfigured)
    {
        throw new InvalidOperationException($"Google Drive chưa được cấu hình. {ConfigurationHint}");
    }

    var drive = await GetDriveServiceAsync(cancellationToken);
    using var stream = new MemoryStream();
    await drive.Files.Get(fileId).DownloadAsync(stream, cancellationToken);
    return stream.ToArray();
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test NhatDucSoftware.Core.Tests/NhatDucSoftware.Core.Tests.csproj --filter HomeworkDriveFolderChoiceTests -v q`

Expected: PASS. Không gọi Drive thật.

- [ ] **Step 5: Commit**

```powershell
git add -- NhatDucSoftware.Core/Services/GoogleDriveService.cs NhatDucSoftware.Core.Tests/HomeworkDriveFolderChoiceTests.cs
git commit -m @"
Upload homework files into the teacher and subject Drive folders.

"@
```

### Task 8: HomeworkService

**Files:**
- Create: `NhatDucSoftware.Core/Models/HomeworkModels.cs`
- Create: `NhatDucSoftware.Core/Services/HomeworkService.cs`
- Modify: `NhatDucSoftware.Web/Program.cs` thêm `builder.Services.AddScoped<HomeworkService>();` cạnh `EvaluationService`

**Interfaces:**
- Consumes: `HomeworkSql`, `HomeworkGradingRules`, `HomeworkSubmissionState`, `HomeworkMonthlyPush`, `HomeworkDrivePath`, `GoogleDriveService`, `EvaluationService.Save(int studentId, int classId, int teacherId, decimal? score, string? comment, int year, int month)`, `ClassService.GetClassesByTeacher`, `AttendanceService.GetStudentsByClass`, `CourseService.GetAll`, `TeacherProfileService.GetProfile`.
- Produces:
  - `HomeworkKind.Written`, `HomeworkKind.Speaking`
  - `HomeworkAssignment`, `HomeworkSubmissionRow`
  - `HomeworkService.CreateAssignment(int teacherId, int classId, string title, DateTime assignedOn, string kind, string prompt, string? answerKeyText)`
  - `HomeworkService.ReplaceAnswerKeyFile(int teacherId, int assignmentId, Stream stream, string fileName, string contentType)`
  - `HomeworkService.StoreWrittenFile(int teacherId, int assignmentId, int studentId, Stream stream, string fileName, string contentType)`
  - `HomeworkService.SaveGrade(int teacherId, int assignmentId, int studentId, decimal? score, string? shortComment, string reportText)`
  - `HomeworkService.PushToMonth(int teacherId, int assignmentId, int studentId)`
  - `HomeworkService.UpdateAnswerKeyText(int teacherId, int assignmentId, string? answerKeyText)`
  - `ListForTeacher(int teacherId, int classId)`, `GetAssignment(int teacherId, int assignmentId)`, `ListRows(int teacherId, int assignmentId)`

Đổi đáp án chữ hoặc file không cập nhật `Score` hay `ReportText`. `PushToMonth` gọi `EvaluationService.Save` với `write.Score`, `write.ShortComment`, `write.Year`, `write.Month`. Không truyền `ReportText`.

- [ ] **Step 1: Write the models and service**

`HomeworkModels.cs`:

```csharp
namespace NhatDucSoftware.Core.Models;

public static class HomeworkKind
{
    public const string Written = "Written";
    public const string Speaking = "Speaking";
}

public sealed class HomeworkAssignment
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = "";
    public string CourseName { get; set; } = "";
    public string CourseCode { get; set; } = "";
    public int TeacherId { get; set; }
    public string Title { get; set; } = "";
    public DateTime AssignedOn { get; set; }
    public string Kind { get; set; } = HomeworkKind.Written;
    public string Prompt { get; set; } = "";
    public string? AnswerKeyText { get; set; }
    public string? AnswerKeyFileName { get; set; }
    public string? AnswerKeyDriveFileId { get; set; }
    public string? AnswerKeyDriveWebViewLink { get; set; }
    public string? DriveFolderId { get; set; }
}

public sealed class HomeworkSubmissionRow
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public decimal? Score { get; set; }
    public string? ShortComment { get; set; }
    public string? ReportText { get; set; }
    public string? FileName { get; set; }
    public string? DriveWebViewLink { get; set; }
    public string? PendingFileName { get; set; }
    public string? SavedDriveFileId { get; set; }
    public string? PendingDriveFileId { get; set; }
}
```

Thêm vào `HomeworkSql` các hằng một dòng này:

```csharp
public const string ListForTeacher = "SELECT a.Id, a.ClassId, COALESCE(c.ClassName, ''), COALESCE(co.Name, ''), COALESCE(c.CourseCode, ''), a.TeacherId, a.Title, a.AssignedOn, a.Kind, a.Prompt, a.AnswerKeyText, a.AnswerKeyFileName, a.AnswerKeyDriveFileId, a.AnswerKeyDriveWebViewLink, a.DriveFolderId FROM HomeworkAssignments a INNER JOIN Classes c ON c.Id = a.ClassId LEFT JOIN Courses co ON co.Id = c.CourseId WHERE a.TeacherId = @teacherId AND a.ClassId = @classId ORDER BY a.AssignedOn DESC, a.Id DESC;";
public const string GetAssignment = "SELECT a.Id, a.ClassId, COALESCE(c.ClassName, ''), COALESCE(co.Name, ''), COALESCE(c.CourseCode, ''), a.TeacherId, a.Title, a.AssignedOn, a.Kind, a.Prompt, a.AnswerKeyText, a.AnswerKeyFileName, a.AnswerKeyDriveFileId, a.AnswerKeyDriveWebViewLink, a.DriveFolderId FROM HomeworkAssignments a INNER JOIN Classes c ON c.Id = a.ClassId LEFT JOIN Courses co ON co.Id = c.CourseId WHERE a.Id = @id AND a.TeacherId = @teacherId;";
public const string ListRows = "SELECT s.Id, s.FullName, sub.Score, sub.ShortComment, sub.ReportText, sub.FileName, sub.DriveWebViewLink, sub.PendingFileName, sub.DriveFileId, sub.PendingDriveFileId FROM ClassStudents cs INNER JOIN Students s ON s.Id = cs.StudentId LEFT JOIN HomeworkSubmissions sub ON sub.AssignmentId = @assignmentId AND sub.StudentId = s.Id WHERE cs.ClassId = @classId ORDER BY s.FullName ASC, s.Id ASC;";
public const string StudentInClass = "SELECT COUNT(1) FROM ClassStudents WHERE ClassId = @classId AND StudentId = @studentId;";
public const string SetDriveFolder = "UPDATE HomeworkAssignments SET DriveFolderId = @driveFolderId WHERE Id = @id AND DriveFolderId IS NULL;";
public const string ReplaceAnswerKeyFile = "UPDATE HomeworkAssignments SET AnswerKeyFileName = @fileName, AnswerKeyDriveFileId = @driveFileId, AnswerKeyDriveWebViewLink = @webViewLink WHERE Id = @id AND TeacherId = @teacherId;";
public const string UpdateAnswerKeyText = "UPDATE HomeworkAssignments SET AnswerKeyText = @answerKeyText WHERE Id = @id AND TeacherId = @teacherId;";
```

Thêm vào `HomeworkSqlTests` một fact `AddedStatements_AreSingleLine` gọi `Assert.DoesNotContain("\n", ...)` với bảy hằng trên.

`HomeworkService.cs` dùng `DbContext`, map `Id` bằng `Convert.ToInt32(reader.GetValue(...))`, và các phương thức public đúng chữ ký ở trên cộng `ListForTeacher(int teacherId, int classId)`, `GetAssignment(int teacherId, int assignmentId)`, `ListRows(int teacherId, int assignmentId)`, `UpdateAnswerKeyText(int teacherId, int assignmentId, string? answerKeyText)`.

Quy tắc trong thân phương thức:

- `EnsureTeaches` lấy `ClassService.GetClassesByTeacher(teacherId)` và gọi `HomeworkGradingRules.CanAccess(teaches, true, out error)`. Sai thì `throw new InvalidOperationException(error)`.
- `CreateAssignment` gọi `TryValidateCreate` rồi `EnsureTeaches`. `AssignedOn` ghi `yyyy-MM-dd`. `CreatedAt` ghi `DateTime.UtcNow.ToString("o")`. Chạy `HomeworkSql.InsertAssignment`. Cột file đáp án để NULL.
- `StoreWrittenFile` chỉ khi `assignment.Kind == HomeworkKind.Written` và `HomeworkFileRules.IsAccepted` đúng. `CanAccess(true, studentInClass, ...)`. Upload bằng `UploadHomeworkFileAsync(assignment.DriveFolderId, teacher.FullName, HomeworkDrivePath.SubjectFolder(...), HomeworkDrivePath.AssignmentFolder(...), ...)`. Chạy `SetDriveFolder` rồi `UpsertPendingFile`. Nếu `PendingDriveFileId` cũ khác file mới thì `DeleteFileAsync` id cũ. Không sửa `Score`, `ReportText`, `DriveFileId`.
- `ReplaceAnswerKeyFile` upload cùng cách, chạy `ReplaceAnswerKeyFile`, rồi `DeleteFileAsync` id đáp án cũ nếu khác file mới. Không sửa phiếu.
- `UpdateAnswerKeyText` chỉ chạy `UpdateAnswerKeyText`. Không sửa phiếu.
- `SaveGrade`: bài viết đọc `SavedDriveFileId` và `PendingDriveFileId` trước khi update, gọi `HomeworkSubmissionState.Commit`, chạy `SaveWrittenGrade`. `ExecuteNonQuery()` bằng 0 thì ném `InvalidOperationException("Hãy tải bài làm trước khi lưu.")`. Sau update thành công mới `DeleteFileAsync(commit.DriveFileIdToDelete)`. Bài nói chạy `SaveSpeakingGrade` và không đụng cột file. `score` null được ghi NULL.
- `PushToMonth` đọc điểm đã lưu, gọi `HomeworkMonthlyPush.TryBuild(score, shortComment, assignedOn, out write, out error)`. Sai thì ném `InvalidOperationException(error)`. Đúng thì `_evaluations.Save(studentId, assignment.ClassId, teacherId, write.Score, write.ShortComment, write.Year, write.Month)`. Không truyền `ReportText`.

Đăng ký trong `Program.cs` ngay sau `AddScoped<EvaluationService>()`:

```csharp
builder.Services.AddScoped<HomeworkService>();
```

- [ ] **Step 2: Build**

Run: `dotnet build NhatDucSoftware.Web/NhatDucSoftware.Web.csproj -v q`

Expected: build thành công. Không chạy ứng dụng.

- [ ] **Step 3: Commit**

```powershell
git add -- NhatDucSoftware.Core/Models/HomeworkModels.cs NhatDucSoftware.Core/Services/HomeworkService.cs NhatDucSoftware.Core/Services/HomeworkSql.cs NhatDucSoftware.Web/Program.cs
git commit -m @"
Store homework assignments and keep saved slips when a new upload is pending.

"@
```

### Task 9: Gemini client

**Files:**
- Create: `NhatDucSoftware.Core/Services/GeminiHomeworkClient.cs`
- Modify: `NhatDucSoftware.Web/Program.cs`
- Test: `NhatDucSoftware.Core.Tests/GeminiHomeworkRequestTests.cs`

**Interfaces:**
- Consumes: `GeminiGradePrompt.Build`, `GeminiGradeParser.Parse`.
- Produces: `GeminiHomeworkRequest.Endpoint(string model)`, `GeminiHomeworkClient.GradeAsync(...)` trả `GeminiGradeDraft`.

- [ ] **Step 1: Write the failing test**

```csharp
using NhatDucSoftware.Core.Services;

namespace NhatDucSoftware.Core.Tests;

public class GeminiHomeworkRequestTests
{
    [Fact]
    public void Endpoint_UsesTheConfiguredModel()
    {
        Assert.Equal(
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent",
            GeminiHomeworkRequest.Endpoint("gemini-2.5-flash"));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test NhatDucSoftware.Core.Tests/NhatDucSoftware.Core.Tests.csproj --filter GeminiHomeworkRequestTests -v q`

Expected: FAIL because the type does not exist.

- [ ] **Step 3: Write minimal implementation**

```csharp
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NhatDucSoftware.Core.Helpers;

namespace NhatDucSoftware.Core.Services;

public static class GeminiHomeworkRequest
{
    public static string Endpoint(string model) =>
        $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
}

public sealed class GeminiHomeworkClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiHomeworkClient(HttpClient http, string apiKey, string model)
    {
        _http = http;
        _apiKey = apiKey;
        _model = string.IsNullOrWhiteSpace(model) ? "gemini-2.5-flash" : model;
    }

    public async Task<GeminiGradeDraft> GradeAsync(
        string kind,
        string className,
        string title,
        string studentName,
        string prompt,
        string? answerKeyText,
        byte[]? answerKeyFile,
        string? answerKeyMime,
        byte[] studentFile,
        string studentMime,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("Chưa cấu hình GEMINI_API_KEY.");
        }

        var text = GeminiGradePrompt.Build(kind, className, title, studentName, prompt, answerKeyText);
        var parts = new List<object> { new { text } };
        if (answerKeyFile is { Length: > 0 })
        {
            parts.Add(new { inline_data = new { mime_type = answerKeyMime, data = Convert.ToBase64String(answerKeyFile) } });
        }

        parts.Add(new { inline_data = new { mime_type = studentMime, data = Convert.ToBase64String(studentFile) } });
        var body = JsonSerializer.Serialize(new { contents = new[] { new { parts } } });
        using var request = new HttpRequestMessage(HttpMethod.Post, GeminiHomeworkRequest.Endpoint(_model));
        request.Headers.Add("x-goog-api-key", _apiKey);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await _http.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Gemini không chấm được bài. Hãy thử lại.");
        }

        using var document = JsonDocument.Parse(payload);
        var raw = document.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
        return GeminiGradeParser.Parse(raw);
    }
}
```

Trong `Program.cs`, `builder.Services.AddHttpClient();` chưa có thì thêm một dòng đó. Rồi đăng ký đúng một lần:

```csharp
builder.Services.AddScoped(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(GeminiHomeworkClient));
    var key = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";
    var model = Environment.GetEnvironmentVariable("GEMINI_MODEL")
        ?? config["Gemini:Model"]
        ?? "gemini-2.5-flash";
    return new GeminiHomeworkClient(http, key, model);
});
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test NhatDucSoftware.Core.Tests/NhatDucSoftware.Core.Tests.csproj --filter GeminiHomeworkRequestTests -v q`

Expected: PASS. Không gọi Gemini.

- [ ] **Step 5: Commit**

```powershell
git add -- NhatDucSoftware.Core/Services/GeminiHomeworkClient.cs NhatDucSoftware.Core.Tests/GeminiHomeworkRequestTests.cs NhatDucSoftware.Web/Program.cs
git commit -m @"
Send homework files to Gemini and parse the returned slip.

"@
```

### Task 10: Trang Bài tập

**Files:**
- Create: `NhatDucSoftware.Web/Components/Pages/Teacher/Homework.razor`
- Modify: `NhatDucSoftware.Web/Components/Layout/MainLayout.razor` thêm link ngay sau link Nhận xét / Điểm

**Interfaces:**
- Consumes: toàn bộ API của Task 8 và Task 9, `HomeworkAnnouncement.Build`, `HomeworkFileRules.IsAccepted`, `HomeworkScoreParser.TryParse`.
- Produces: trang `/teacher/homework`.

- [ ] **Step 1: Add the nav link**

Trong khối `Session.IsTeacher`, sau link `/teacher/evaluation`, thêm:

```razor
<NavLink href="/teacher/homework">Bài tập</NavLink>
```

- [ ] **Step 2: Write the page**

Tạo `Homework.razor` với `@page "/teacher/homework"`, `@attribute [Authorize(Roles = "Teacher")]`, `@rendermode InteractiveServer`. Inject `UserSession`, `ClassService`, `HomeworkService`, `GeminiHomeworkClient`, `IJSRuntime`.

Hành vi bắt buộc:

- Không có `TeacherId` thì báo và dừng.
- Dropdown lớp lấy `ClassService.GetClassesByTeacher(teacherId)`.
- Form tạo bài: tên, ngày giao, loại `Written`/`Speaking` hiện chữ **Bài viết** và **Bài nói**, đề, ô đáp án. Bài nói ẩn đáp án. Nút tạo gọi `CreateAssignment`.
- Nút **Sao chép tin giao bài** gọi `HomeworkAnnouncement.Build`. Không đưa đáp án vào chuỗi. `navigator.clipboard.writeText` lỗi thì gán chuỗi vào một `textarea` để giáo viên tự chép.
- Chọn bài thì `ListRows`. Mỗi em có input file. Kiểm tra `HomeworkFileRules` trước khi gọi service. Quá giới hạn hoặc sai loại thì báo và không gọi service.
- Bài viết: `StoreWrittenFile`. File đã lưu và file chờ hiện tên. Link Drive mở file đã lưu.
- Nút **Chấm** gọi `CanGrade`. Bài nói đọc file vào `byte[]`, gọi `GradeAsync`, rồi gán mảng đó bằng `Array.Empty<byte>()`. Bài viết gọi `GoogleDriveService.DownloadFileAsync` từ Task 7 với id file chờ nếu có, không thì id file đã lưu. Byte tải về chỉ đưa vào `GradeAsync`, không ghi cột mới.
- Kết quả `GeminiGradeDraft` điền vào ô điểm, nhận xét ngắn, phiếu trên trang. `ScoreMissing` thì để trống điểm và báo giáo viên tự điền. Chưa bấm **Lưu** thì không gọi `SaveGrade`.
- **Lưu** gọi `SaveGrade`. **Sao chép phiếu** chỉ hiện khi `ReportText` đã lưu. **Đưa vào đánh giá tháng** gọi `PushToMonth`.
- Lỗi `InvalidOperationException` hiện `ex.Message` trên trang. Không xóa phiếu đã lưu khi chấm lỗi.
- Rời học viên đang chọn thì bản nháp trên form mất. Dòng đã lưu trong `ListRows` vẫn hiện lại.

- [ ] **Step 3: Build without starting the site**

Run: `dotnet test NhatDucSoftware.Core.Tests/NhatDucSoftware.Core.Tests.csproj -v q`

Expected: PASS.

Run: `dotnet build NhatDucSoftware.Web/NhatDucSoftware.Web.csproj -v q`

Expected: build thành công.

Run: `dotnet build NhatDucSoftware/NhatDucSoftware.csproj -v q`

Expected: ứng dụng desktop vẫn build. Không có màn hình bài tập mới.

- [ ] **Step 4: Commit**

```powershell
git add -- NhatDucSoftware.Web/Components/Pages/Teacher/Homework.razor NhatDucSoftware.Web/Components/Layout/MainLayout.razor NhatDucSoftware.Core/Services/GoogleDriveService.cs
git commit -m @"
Let teachers create homework, grade it with Gemini, and copy the slip.

"@
```

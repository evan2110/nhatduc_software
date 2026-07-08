using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Core.Services;
using NhatDucSoftware.Web.Components;
using NhatDucSoftware.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true);

static void SetEnvIfEmpty(string key, string? value)
{
    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key))
        && !string.IsNullOrWhiteSpace(value))
    {
        Environment.SetEnvironmentVariable(key, value.Trim());
    }
}

SetEnvIfEmpty("GOOGLE_DRIVE_CLIENT_ID", builder.Configuration["GoogleDrive:ClientId"]);
SetEnvIfEmpty("GOOGLE_DRIVE_CLIENT_SECRET", builder.Configuration["GoogleDrive:ClientSecret"]);
SetEnvIfEmpty("GOOGLE_DRIVE_REFRESH_TOKEN", builder.Configuration["GoogleDrive:RefreshToken"]);
SetEnvIfEmpty("NHATDUC_GMAIL_REFRESH_TOKEN", builder.Configuration["GoogleDrive:GmailRefreshToken"]);

var connectionString = builder.Configuration.GetConnectionString("Default");
var dbPassword = builder.Configuration["Database:Password"]
    ?? Environment.GetEnvironmentVariable("SUPABASE_DB_PASSWORD");
DbContext.Configure(
    string.IsNullOrWhiteSpace(connectionString) ? null : connectionString,
    dbPassword);

DatabaseInitializer.Initialize();

SetEnvIfEmpty("NHATDUC_GMAIL_REFRESH_TOKEN", AppSettingsService.Get("NHATDUC_GMAIL_REFRESH_TOKEN"));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 52 * 1024 * 1024;
    });

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UserSession>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<StudentService>();
builder.Services.AddScoped<CourseService>();
builder.Services.AddScoped<ClassService>();
builder.Services.AddScoped<TeacherService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<AttendanceService>();
builder.Services.AddScoped<EvaluationService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<TeacherTimesheetService>();
builder.Services.AddScoped<TeacherTimesheetNotificationService>();
builder.Services.AddScoped<ClassScheduleService>();
builder.Services.AddScoped<ExcelExportService>();
builder.Services.AddScoped<TeacherProfileService>();
builder.Services.AddScoped<GoogleDriveService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var settings = new GoogleDriveSettings
    {
        RootFolderId = Environment.GetEnvironmentVariable("GOOGLE_DRIVE_ROOT_FOLDER_ID")
            ?? config["GoogleDrive:RootFolderId"]
            ?? "1g1sl-pKk1d3sixMkpXbSWmiFDXb55u-n",
        TeacherProfileRootFolderId = Environment.GetEnvironmentVariable("GOOGLE_DRIVE_TEACHER_PROFILE_ROOT_FOLDER_ID")
            ?? config["GoogleDrive:TeacherProfileRootFolderId"]
            ?? "1yq8ByWsZv5-AQiteWVbETVKpcplQBcbq",
        ServiceAccountJson = Environment.GetEnvironmentVariable("GOOGLE_DRIVE_SERVICE_ACCOUNT_JSON")
            ?? config["GoogleDrive:ServiceAccountJson"],
        ClientId = Environment.GetEnvironmentVariable("GOOGLE_DRIVE_CLIENT_ID")
            ?? config["GoogleDrive:ClientId"],
        ClientSecret = Environment.GetEnvironmentVariable("GOOGLE_DRIVE_CLIENT_SECRET")
            ?? config["GoogleDrive:ClientSecret"],
        RefreshToken = Environment.GetEnvironmentVariable("GOOGLE_DRIVE_REFRESH_TOKEN")
            ?? config["GoogleDrive:RefreshToken"]
    };
    return new GoogleDriveService(settings);
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/account/login", async (HttpContext context, AuthService auth) =>
{
    var form = await context.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();

    var user = auth.Login(username.Trim(), password);
    if (user is null)
    {
        return Results.Redirect("/login?error=1");
    }

    var claims = new List<System.Security.Claims.Claim>
    {
        new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(System.Security.Claims.ClaimTypes.Name, user.Username),
        new(System.Security.Claims.ClaimTypes.Role, user.Role)
    };

    if (user.TeacherId.HasValue)
    {
        claims.Add(new System.Security.Claims.Claim("TeacherId", user.TeacherId.Value.ToString()));
    }

    var identity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new System.Security.Claims.ClaimsPrincipal(identity),
        new Microsoft.AspNetCore.Authentication.AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });

    var redirectUrl = user.Role == "Admin" ? "/admin/students" : "/teacher/schedule";
    return Results.LocalRedirect(redirectUrl);
}).DisableAntiforgery();

app.MapGet("/account/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.LocalRedirect("/login");
});

app.MapGet("/api/export/students", (ExcelExportService excel, StudentService students) =>
{
    var path = Path.Combine(Path.GetTempPath(), $"students_{Guid.NewGuid():N}.xlsx");
    excel.ExportStudentsToExcel(students.GetAll(), path);
    var bytes = File.ReadAllBytes(path);
    File.Delete(path);
    return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DanhSachHocVien.xlsx");
});

app.MapGet("/api/export/payments/{month:int}/{year:int}/{classId:int}", (int month, int year, int classId, ExcelExportService excel, PaymentService payments) =>
{
    var list = payments.GetPaymentListByClassMonthYear(classId, month, year);
    var path = Path.Combine(Path.GetTempPath(), $"payments_{Guid.NewGuid():N}.xlsx");
    excel.ExportPaymentListToExcel(list, month, year, path);
    var bytes = File.ReadAllBytes(path);
    File.Delete(path);
    return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"HocPhi_{month:D2}_{year}.xlsx");
});

app.MapGet("/api/export/revenue-month/{year:int}", (int year, ExcelExportService excel, ReportService reports) =>
{
    var data = reports.GetRevenueByMonth(year);
    var path = Path.Combine(Path.GetTempPath(), $"revenue_month_{Guid.NewGuid():N}.xlsx");
    excel.ExportRevenueByMonthToExcel(year, data, path);
    var bytes = File.ReadAllBytes(path);
    File.Delete(path);
    return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BaoCaoDoanhthuThang_{year}.xlsx");
});

app.MapGet("/api/export/revenue-year", (ExcelExportService excel, ReportService reports) =>
{
    var data = reports.GetRevenueByYear();
    var path = Path.Combine(Path.GetTempPath(), $"revenue_year_{Guid.NewGuid():N}.xlsx");
    excel.ExportRevenueByYearToExcel(data, path);
    var bytes = File.ReadAllBytes(path);
    File.Delete(path);
    return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BaoCaoDoanhthuNam.xlsx");
});

app.MapGet("/api/export/expense-month/{year:int}", (int year, ExcelExportService excel, ReportService reports) =>
{
    var monthly = reports.GetExpenseByMonth(year);
    var details = reports.GetTeacherExpenseDetail(year);
    var path = Path.Combine(Path.GetTempPath(), $"expense_{Guid.NewGuid():N}.xlsx");
    excel.ExportExpenseByMonthToExcel(year, monthly, details, path);
    var bytes = File.ReadAllBytes(path);
    File.Delete(path);
    return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BaoCaoChi_{year}.xlsx");
});

app.MapGet("/api/export/tuition-earned/{year:int}", (int year, ExcelExportService excel, ReportService reports) =>
{
    var monthly = reports.GetTuitionEarnedByMonth(year);
    var details = reports.GetClassTuitionDetail(year);
    var path = Path.Combine(Path.GetTempPath(), $"tuition_earned_{Guid.NewGuid():N}.xlsx");
    excel.ExportTuitionEarnedByMonthToExcel(year, monthly, details, path);
    var bytes = File.ReadAllBytes(path);
    File.Delete(path);
    return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BaoCaoThu_{year}.xlsx");
});

app.MapGet("/api/export/enrollment-month/{year:int}", (int year, ExcelExportService excel, ReportService reports) =>
{
    var data = reports.GetEnrollmentByMonth(year);
    var path = Path.Combine(Path.GetTempPath(), $"enrollment_{Guid.NewGuid():N}.xlsx");
    excel.ExportEnrollmentByMonthToExcel(year, data, path);
    var bytes = File.ReadAllBytes(path);
    File.Delete(path);
    return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BaoCaoHocVienLop_{year}.xlsx");
});

app.MapGet("/api/export/evaluations/{studentId:int}/{year:int}/{month:int}", (int studentId, int year, int month, ExcelExportService excel, EvaluationService evaluations, StudentService students) =>
{
    var student = students.GetAll().FirstOrDefault(s => s.Id == studentId);
    var name = student?.FullName ?? "HocVien";
    var data = evaluations.GetByStudentInMonth(studentId, year, month);
    var path = Path.Combine(Path.GetTempPath(), $"eval_{Guid.NewGuid():N}.xlsx");
    excel.ExportStudentEvaluationsByMonthToExcel(name, year, month, data, path);
    var bytes = File.ReadAllBytes(path);
    File.Delete(path);
    return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"DiemNhanXet_{name.Replace(" ", "")}_{month:D2}_{year}.xlsx");
});

app.MapGet("/api/export/center-activity", (string from, string to, ExcelExportService excel, AttendanceService attendance, TeacherTimesheetService timesheets) =>
{
    if (!DateTime.TryParse(from, out var fromDate) || !DateTime.TryParse(to, out var toDate))
    {
        return Results.BadRequest("Ngày không hợp lệ. Dùng định dạng yyyy-MM-dd.");
    }

    fromDate = fromDate.Date;
    toDate = toDate.Date;
    if (fromDate > toDate)
    {
        return Results.BadRequest("Từ ngày phải nhỏ hơn hoặc bằng đến ngày.");
    }

    var attendanceRows = attendance.GetAttendanceByDateRange(fromDate, toDate);
    var timesheetRows = timesheets.GetTimesheetsByDateRange(fromDate, toDate);
    var path = Path.Combine(Path.GetTempPath(), $"center_activity_{Guid.NewGuid():N}.xlsx");
    excel.ExportCenterActivityByDateRange(fromDate, toDate, attendanceRows, timesheetRows, path);
    var bytes = File.ReadAllBytes(path);
    File.Delete(path);

    var fileName = fromDate == toDate
        ? $"DiemDanh_ChamCong_{fromDate:yyyyMMdd}.xlsx"
        : $"DiemDanh_ChamCong_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx";
    return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
});

app.MapGet("/api/export/teacher-payroll-attendance", (
    int teacherId,
    int year,
    int month,
    TeacherService teachers,
    TeacherTimesheetNotificationService payrollEmail) =>
{
    if (teacherId <= 0 || month is < 1 or > 12 || year < 2000)
    {
        return Results.BadRequest("Tham số không hợp lệ.");
    }

    var teacher = teachers.GetAll().FirstOrDefault(t => t.Id == teacherId);
    if (teacher is null)
    {
        return Results.NotFound("Không tìm thấy giáo viên.");
    }

    var attachment = payrollEmail.BuildPayrollAttachment(teacher, year, month);
    return Results.File(
        attachment.Bytes,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        attachment.FileName);
});

app.Run();

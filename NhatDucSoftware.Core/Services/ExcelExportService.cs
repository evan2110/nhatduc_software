using System.IO;
using ClosedXML.Excel;
using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Services;

public class ExcelExportService
{
    public void ExportRevenueByMonthToExcel(int year, List<RevenueByMonthStat> data, string filePath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add($"Doanh thu {year}");

        worksheet.Cell(1, 1).Value = "Thống kê doanh thu theo tháng";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;
        worksheet.Range(1, 1, 1, 2).Merge();

        worksheet.Cell(2, 1).Value = $"Năm: {year}";
        worksheet.Cell(2, 1).Style.Font.Bold = true;

        worksheet.Cell(4, 1).Value = "Tháng";
        worksheet.Cell(4, 2).Value = "Doanh thu";
        worksheet.Range(4, 1, 4, 2).Style.Font.Bold = true;
        worksheet.Range(4, 1, 4, 2).Style.Fill.BackgroundColor = XLColor.LightGray;

        var row = 5;
        decimal totalRevenue = 0;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.MonthName;
            worksheet.Cell(row, 2).Value = item.TotalRevenue;
            worksheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
            totalRevenue += item.TotalRevenue;
            row++;
        }

        worksheet.Cell(row, 1).Value = "Tổng cộng";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 2).Value = totalRevenue;
        worksheet.Cell(row, 2).Style.Font.Bold = true;
        worksheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
        worksheet.Range(row, 1, row, 2).Style.Fill.BackgroundColor = XLColor.LightYellow;

        worksheet.Column(1).Width = 20;
        worksheet.Column(2).Width = 20;

        workbook.SaveAs(filePath);
    }

    public void ExportRevenueByYearToExcel(List<RevenueByYearStat> data, string filePath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Doanh thu theo năm");

        worksheet.Cell(1, 1).Value = "Thống kê doanh thu theo năm";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;
        worksheet.Range(1, 1, 1, 2).Merge();

        worksheet.Cell(3, 1).Value = "Năm";
        worksheet.Cell(3, 2).Value = "Doanh thu";
        worksheet.Range(3, 1, 3, 2).Style.Font.Bold = true;
        worksheet.Range(3, 1, 3, 2).Style.Fill.BackgroundColor = XLColor.LightGray;

        var row = 4;
        decimal totalRevenue = 0;
        foreach (var item in data.OrderBy(x => x.Year))
        {
            worksheet.Cell(row, 1).Value = item.Year;
            worksheet.Cell(row, 2).Value = item.TotalRevenue;
            worksheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
            totalRevenue += item.TotalRevenue;
            row++;
        }

        worksheet.Cell(row, 1).Value = "Tổng cộng";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 2).Value = totalRevenue;
        worksheet.Cell(row, 2).Style.Font.Bold = true;
        worksheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
        worksheet.Range(row, 1, row, 2).Style.Fill.BackgroundColor = XLColor.LightYellow;

        worksheet.Column(1).Width = 20;
        worksheet.Column(2).Width = 20;

        workbook.SaveAs(filePath);
    }

    public void ExportExpenseByMonthToExcel(int year, List<MonthlyAmountStat> monthly, List<TeacherExpenseDetailStat> details, string filePath)
    {
        using var workbook = new XLWorkbook();
        var monthlySheet = workbook.Worksheets.Add($"Chi tháng {year}");
        WriteMonthlyAmountSheet(monthlySheet, $"Tổng chi lương giáo viên năm {year}", "Tổng chi", monthly);

        var detailSheet = workbook.Worksheets.Add("Chi tiết giáo viên");
        detailSheet.Cell(1, 1).Value = $"Chi tiết chi lương giáo viên năm {year}";
        detailSheet.Cell(1, 1).Style.Font.Bold = true;
        detailSheet.Range(1, 1, 1, 3).Merge();
        detailSheet.Cell(3, 1).Value = "TT";
        detailSheet.Cell(3, 2).Value = "Giáo viên";
        detailSheet.Cell(3, 3).Value = "Tổng chi";
        detailSheet.Range(3, 1, 3, 3).Style.Font.Bold = true;
        detailSheet.Range(3, 1, 3, 3).Style.Fill.BackgroundColor = XLColor.LightGray;

        var row = 4;
        var index = 1;
        foreach (var item in details)
        {
            detailSheet.Cell(row, 1).Value = index++;
            detailSheet.Cell(row, 2).Value = item.TeacherName;
            detailSheet.Cell(row, 3).Value = item.TotalAmount;
            detailSheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
            row++;
        }

        detailSheet.Column(1).Width = 8;
        detailSheet.Column(2).Width = 30;
        detailSheet.Column(3).Width = 18;
        workbook.SaveAs(filePath);
    }

    public void ExportTuitionEarnedByMonthToExcel(int year, List<MonthlyAmountStat> monthly, List<ClassTuitionDetailStat> details, string filePath)
    {
        using var workbook = new XLWorkbook();
        var monthlySheet = workbook.Worksheets.Add($"Thu tháng {year}");
        WriteMonthlyAmountSheet(monthlySheet, $"Tổng thu học phí điểm danh năm {year}", "Tổng thu", monthly);

        var detailSheet = workbook.Worksheets.Add("Chi tiết lớp");
        detailSheet.Cell(1, 1).Value = $"Chi tiết thu học phí theo lớp năm {year}";
        detailSheet.Cell(1, 1).Style.Font.Bold = true;
        detailSheet.Range(1, 1, 1, 3).Merge();
        detailSheet.Cell(3, 1).Value = "TT";
        detailSheet.Cell(3, 2).Value = "Lớp";
        detailSheet.Cell(3, 3).Value = "Tổng thu";
        detailSheet.Range(3, 1, 3, 3).Style.Font.Bold = true;
        detailSheet.Range(3, 1, 3, 3).Style.Fill.BackgroundColor = XLColor.LightGray;

        var row = 4;
        var index = 1;
        foreach (var item in details)
        {
            detailSheet.Cell(row, 1).Value = index++;
            detailSheet.Cell(row, 2).Value = item.ClassName;
            detailSheet.Cell(row, 3).Value = item.TotalAmount;
            detailSheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
            row++;
        }

        detailSheet.Column(1).Width = 8;
        detailSheet.Column(2).Width = 30;
        detailSheet.Column(3).Width = 18;
        workbook.SaveAs(filePath);
    }

    public void ExportEnrollmentByMonthToExcel(int year, List<MonthlyEnrollmentStat> data, string filePath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add($"HocVienLop {year}");

        worksheet.Cell(1, 1).Value = "Thống kê học viên và lớp theo tháng";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Range(1, 1, 1, 3).Merge();
        worksheet.Cell(2, 1).Value = $"Năm: {year}";
        worksheet.Cell(2, 1).Style.Font.Bold = true;

        worksheet.Cell(4, 1).Value = "Tháng";
        worksheet.Cell(4, 2).Value = "Tổng học viên";
        worksheet.Cell(4, 3).Value = "Tổng lớp hoạt động";
        worksheet.Range(4, 1, 4, 3).Style.Font.Bold = true;
        worksheet.Range(4, 1, 4, 3).Style.Fill.BackgroundColor = XLColor.LightGray;

        var row = 5;
        foreach (var item in data.OrderBy(x => x.Month))
        {
            worksheet.Cell(row, 1).Value = item.MonthName;
            worksheet.Cell(row, 2).Value = item.StudentCount;
            worksheet.Cell(row, 3).Value = item.ClassCount;
            row++;
        }

        worksheet.Column(1).Width = 20;
        worksheet.Column(2).Width = 18;
        worksheet.Column(3).Width = 22;
        workbook.SaveAs(filePath);
    }

    private static void WriteMonthlyAmountSheet(IXLWorksheet worksheet, string title, string amountHeader, List<MonthlyAmountStat> data)
    {
        worksheet.Cell(1, 1).Value = title;
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Range(1, 1, 1, 2).Merge();

        worksheet.Cell(3, 1).Value = "Tháng";
        worksheet.Cell(3, 2).Value = amountHeader;
        worksheet.Range(3, 1, 3, 2).Style.Font.Bold = true;
        worksheet.Range(3, 1, 3, 2).Style.Fill.BackgroundColor = XLColor.LightGray;

        var row = 4;
        decimal total = 0;
        foreach (var item in data.OrderBy(x => x.Month))
        {
            worksheet.Cell(row, 1).Value = item.MonthName;
            worksheet.Cell(row, 2).Value = item.Amount;
            worksheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
            total += item.Amount;
            row++;
        }

        worksheet.Cell(row, 1).Value = "Tổng cộng";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 2).Value = total;
        worksheet.Cell(row, 2).Style.Font.Bold = true;
        worksheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
        worksheet.Range(row, 1, row, 2).Style.Fill.BackgroundColor = XLColor.LightYellow;

        worksheet.Column(1).Width = 20;
        worksheet.Column(2).Width = 20;
    }

    public void ExportStudentEvaluationsByMonthToExcel(string studentName, int year, int month, List<StudentEvaluationRow> data, string filePath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add($"DanhGia_{month:D2}_{year}");

        worksheet.Cell(1, 1).Value = "Bảng điểm / nhận xét theo tháng";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;
        worksheet.Range(1, 1, 1, 5).Merge();

        worksheet.Cell(2, 1).Value = $"Học viên: {studentName}";
        worksheet.Cell(3, 1).Value = $"Tháng/Năm: {month:D2}/{year}";
        worksheet.Cell(2, 1).Style.Font.Bold = true;
        worksheet.Cell(3, 1).Style.Font.Bold = true;

        worksheet.Cell(5, 1).Value = "Lớp";
        worksheet.Cell(5, 2).Value = "Giáo viên";
        worksheet.Cell(5, 3).Value = "Điểm";
        worksheet.Cell(5, 4).Value = "Nhận xét";
        worksheet.Cell(5, 5).Value = "Ngày";
        worksheet.Range(5, 1, 5, 5).Style.Font.Bold = true;
        worksheet.Range(5, 1, 5, 5).Style.Fill.BackgroundColor = XLColor.LightGray;

        var row = 6;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.Lop;
            worksheet.Cell(row, 2).Value = item.GiaoVien;
            worksheet.Cell(row, 3).Value = item.Diem;
            worksheet.Cell(row, 4).Value = item.NhanXet;
            worksheet.Cell(row, 5).Value = item.Ngay;
            row++;
        }

        worksheet.Column(1).Width = 24;
        worksheet.Column(2).Width = 24;
        worksheet.Column(3).Width = 10;
        worksheet.Column(4).Width = 50;
        worksheet.Column(5).Width = 14;

        workbook.SaveAs(filePath);
    }

    public void ExportStudentsToExcel(List<Student> data, string filePath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("HocVien");

        worksheet.Cell(1, 1).Value = "Danh sách học viên";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;
        worksheet.Range(1, 1, 1, 9).Merge();

        worksheet.Cell(3, 1).Value = "Mã học viên";
        worksheet.Cell(3, 2).Value = "Họ và tên";
        worksheet.Cell(3, 3).Value = "Lớp";
        worksheet.Cell(3, 4).Value = "Số điện thoại";
        worksheet.Cell(3, 5).Value = "Email";
        worksheet.Cell(3, 6).Value = "Năm sinh";
        worksheet.Cell(3, 7).Value = "Địa chỉ";
        worksheet.Cell(3, 8).Value = "Trạng thái";
        worksheet.Cell(3, 9).Value = "Số dư";
        worksheet.Range(3, 1, 3, 9).Style.Font.Bold = true;
        worksheet.Range(3, 1, 3, 9).Style.Fill.BackgroundColor = XLColor.LightGray;

        var row = 4;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.Id;
            worksheet.Cell(row, 2).Value = item.FullName;
            worksheet.Cell(row, 3).Value = item.ClassName;
            worksheet.Cell(row, 4).Value = item.Phone;
            worksheet.Cell(row, 5).Value = item.Email ?? string.Empty;
            worksheet.Cell(row, 6).Value = item.BirthYear?.ToString() ?? string.Empty;
            worksheet.Cell(row, 7).Value = item.Address ?? string.Empty;
            worksheet.Cell(row, 8).Value = item.Status;
            worksheet.Cell(row, 9).Value = item.Balance;
            row++;
        }

        worksheet.Column(1).Width = 12;
        worksheet.Column(2).Width = 24;
        worksheet.Column(3).Width = 22;
        worksheet.Column(4).Width = 18;
        worksheet.Column(5).Width = 24;
        worksheet.Column(6).Width = 12;
        worksheet.Column(7).Width = 28;
        worksheet.Column(8).Width = 14;

        workbook.SaveAs(filePath);
    }

    public void ExportPaymentListToExcel(List<PaymentClassListRow> data, int month, int year, string filePath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("HocPhi");

        worksheet.Cell(1, 1).Value = "Danh sách học phí";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;
        worksheet.Range(1, 1, 1, 6).Merge();

        worksheet.Cell(2, 1).Value = $"Tháng: {month:D2}/{year}";
        worksheet.Cell(2, 1).Style.Font.Bold = true;

        worksheet.Cell(4, 1).Value = "Thứ tự";
        worksheet.Cell(4, 2).Value = "Họ và tên";
        worksheet.Cell(4, 3).Value = "Lớp";
        worksheet.Cell(4, 4).Value = "Ngày thu";
        worksheet.Cell(4, 5).Value = "Số tiền";
        worksheet.Cell(4, 6).Value = "Người thu";
        worksheet.Range(4, 1, 4, 6).Style.Font.Bold = true;
        worksheet.Range(4, 1, 4, 6).Style.Fill.BackgroundColor = XLColor.LightGray;

        var row = 5;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.ThuTu;
            worksheet.Cell(row, 2).Value = item.HoVaTen;
            worksheet.Cell(row, 3).Value = item.Lop;
            worksheet.Cell(row, 4).Value = item.NgayThu;
            worksheet.Cell(row, 5).Value = item.SoTien;
            worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(row, 6).Value = item.NguoiThu;
            row++;
        }

        worksheet.Column(1).Width = 10;
        worksheet.Column(2).Width = 28;
        worksheet.Column(3).Width = 18;
        worksheet.Column(4).Width = 20;
        worksheet.Column(5).Width = 18;
        worksheet.Column(6).Width = 18;

        workbook.SaveAs(filePath);
    }

    public void ExportCenterActivityByDateRange(
        DateTime fromDate,
        DateTime toDate,
        List<CenterAttendanceExportRow> attendance,
        List<CenterTimesheetExportRow> timesheets,
        string filePath)
    {
        using var workbook = new XLWorkbook();
        var fromText = fromDate.ToString("dd/MM/yyyy");
        var toText = toDate.ToString("dd/MM/yyyy");
        var rangeText = fromDate.Date == toDate.Date ? fromText : $"{fromText} - {toText}";

        var attendanceSheet = workbook.Worksheets.Add("Diem danh HS");
        attendanceSheet.Cell(1, 1).Value = "Điểm danh học viên";
        attendanceSheet.Cell(1, 1).Style.Font.Bold = true;
        attendanceSheet.Cell(1, 1).Style.Font.FontSize = 14;
        attendanceSheet.Range(1, 1, 1, 6).Merge();
        attendanceSheet.Cell(2, 1).Value = $"Khoảng ngày: {rangeText}";
        attendanceSheet.Cell(2, 1).Style.Font.Bold = true;

        attendanceSheet.Cell(4, 1).Value = "Ngày";
        attendanceSheet.Cell(4, 2).Value = "Lớp";
        attendanceSheet.Cell(4, 3).Value = "Ca";
        attendanceSheet.Cell(4, 4).Value = "Học viên";
        attendanceSheet.Cell(4, 5).Value = "Trạng thái";
        attendanceSheet.Cell(4, 6).Value = "Giáo viên";
        attendanceSheet.Range(4, 1, 4, 6).Style.Font.Bold = true;
        attendanceSheet.Range(4, 1, 4, 6).Style.Fill.BackgroundColor = XLColor.LightGray;

        var row = 5;
        foreach (var item in attendance)
        {
            attendanceSheet.Cell(row, 1).Value = item.SessionDate.ToString("dd/MM/yyyy");
            attendanceSheet.Cell(row, 2).Value = item.ClassName;
            attendanceSheet.Cell(row, 3).Value = item.ShiftNumber;
            attendanceSheet.Cell(row, 4).Value = item.StudentName;
            attendanceSheet.Cell(row, 5).Value = item.Status;
            attendanceSheet.Cell(row, 6).Value = item.TeacherName;
            row++;
        }

        attendanceSheet.Column(1).Width = 14;
        attendanceSheet.Column(2).Width = 22;
        attendanceSheet.Column(3).Width = 8;
        attendanceSheet.Column(4).Width = 28;
        attendanceSheet.Column(5).Width = 14;
        attendanceSheet.Column(6).Width = 24;

        var timesheetSheet = workbook.Worksheets.Add("Cham cong GV");
        timesheetSheet.Cell(1, 1).Value = "Chấm công giáo viên";
        timesheetSheet.Cell(1, 1).Style.Font.Bold = true;
        timesheetSheet.Cell(1, 1).Style.Font.FontSize = 14;
        timesheetSheet.Range(1, 1, 1, 6).Merge();
        timesheetSheet.Cell(2, 1).Value = $"Khoảng ngày: {rangeText}";
        timesheetSheet.Cell(2, 1).Style.Font.Bold = true;

        timesheetSheet.Cell(4, 1).Value = "Ngày";
        timesheetSheet.Cell(4, 2).Value = "Giáo viên";
        timesheetSheet.Cell(4, 3).Value = "Ca";
        timesheetSheet.Cell(4, 4).Value = "Trạng thái";
        timesheetSheet.Cell(4, 5).Value = "Lương ca";
        timesheetSheet.Cell(4, 6).Value = "Ghi chú";
        timesheetSheet.Range(4, 1, 4, 6).Style.Font.Bold = true;
        timesheetSheet.Range(4, 1, 4, 6).Style.Fill.BackgroundColor = XLColor.LightGray;

        row = 5;
        foreach (var item in timesheets)
        {
            timesheetSheet.Cell(row, 1).Value = item.WorkDate.ToString("dd/MM/yyyy");
            timesheetSheet.Cell(row, 2).Value = item.TeacherName;
            timesheetSheet.Cell(row, 3).Value = item.ShiftNumber;
            timesheetSheet.Cell(row, 4).Value = item.IsPresent ? "Có mặt" : "Vắng";
            timesheetSheet.Cell(row, 5).Value = item.ShiftPay;
            timesheetSheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
            timesheetSheet.Cell(row, 6).Value = item.Note ?? "";
            row++;
        }

        timesheetSheet.Column(1).Width = 14;
        timesheetSheet.Column(2).Width = 24;
        timesheetSheet.Column(3).Width = 8;
        timesheetSheet.Column(4).Width = 14;
        timesheetSheet.Column(5).Width = 14;
        timesheetSheet.Column(6).Width = 28;

        workbook.SaveAs(filePath);
    }

    public byte[] BuildTeacherClassAttendanceWorkbook(
        int year,
        int month,
        IReadOnlyList<ClassInfo> classes,
        IReadOnlyList<CenterAttendanceExportRow> attendanceRows)
    {
        using var workbook = new XLWorkbook();
        var monthText = $"Tháng {month:D2}/{year}";
        var usedSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (classes.Count == 0)
        {
            var emptySheet = workbook.Worksheets.Add("Diem danh");
            emptySheet.Cell(1, 1).Value = "Không có lớp học trong tháng này.";
            emptySheet.Cell(1, 1).Style.Font.Bold = true;
        }
        else
        {
            foreach (var classInfo in classes.OrderBy(c => c.ClassName))
            {
                var sheetName = GetUniqueSheetName(classInfo.ClassName, usedSheetNames);
                var worksheet = workbook.Worksheets.Add(sheetName);
                WriteClassAttendanceSheet(worksheet, classInfo.ClassName, monthText, attendanceRows.Where(r => r.ClassId == classInfo.Id));
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public void ExportTeacherClassAttendanceForMonth(
        int year,
        int month,
        IReadOnlyList<ClassInfo> classes,
        IReadOnlyList<CenterAttendanceExportRow> attendanceRows,
        string filePath)
    {
        var bytes = BuildTeacherClassAttendanceWorkbook(year, month, classes, attendanceRows);
        File.WriteAllBytes(filePath, bytes);
    }

    private static void WriteClassAttendanceSheet(
        IXLWorksheet worksheet,
        string className,
        string monthText,
        IEnumerable<CenterAttendanceExportRow> rows)
    {
        worksheet.Cell(1, 1).Value = $"Điểm danh - {className}";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;
        worksheet.Range(1, 1, 1, 4).Merge();
        worksheet.Cell(2, 1).Value = monthText;
        worksheet.Cell(2, 1).Style.Font.Bold = true;

        worksheet.Cell(4, 1).Value = "Ngày";
        worksheet.Cell(4, 2).Value = "Ca";
        worksheet.Cell(4, 3).Value = "Học viên";
        worksheet.Cell(4, 4).Value = "Trạng thái";
        worksheet.Range(4, 1, 4, 4).Style.Font.Bold = true;
        worksheet.Range(4, 1, 4, 4).Style.Fill.BackgroundColor = XLColor.LightGray;

        var row = 5;
        foreach (var item in rows)
        {
            worksheet.Cell(row, 1).Value = item.SessionDate.ToString("dd/MM/yyyy");
            worksheet.Cell(row, 2).Value = item.ShiftNumber;
            worksheet.Cell(row, 3).Value = item.StudentName;
            worksheet.Cell(row, 4).Value = item.Status;
            row++;
        }

        worksheet.Column(1).Width = 14;
        worksheet.Column(2).Width = 8;
        worksheet.Column(3).Width = 28;
        worksheet.Column(4).Width = 14;
    }

    private static string GetUniqueSheetName(string className, ISet<string> usedSheetNames)
    {
        var baseName = SanitizeSheetName(className);
        if (usedSheetNames.Add(baseName))
        {
            return baseName;
        }

        for (var index = 2; index < 100; index++)
        {
            var suffix = $"_{index}";
            var trimmedBase = baseName;
            if (trimmedBase.Length + suffix.Length > 31)
            {
                trimmedBase = trimmedBase[..(31 - suffix.Length)];
            }

            var candidate = $"{trimmedBase}{suffix}";
            if (usedSheetNames.Add(candidate))
            {
                return candidate;
            }
        }

        var fallback = $"Lop_{usedSheetNames.Count + 1}";
        usedSheetNames.Add(fallback);
        return fallback;
    }

    private static string SanitizeSheetName(string name)
    {
        var sanitized = name
            .Replace('\\', '_')
            .Replace('/', '_')
            .Replace('*', '_')
            .Replace('?', '_')
            .Replace(':', '_')
            .Replace('[', '_')
            .Replace(']', '_')
            .Trim();

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "Lop";
        }

        return sanitized.Length > 31 ? sanitized[..31] : sanitized;
    }
}

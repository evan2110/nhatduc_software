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
}

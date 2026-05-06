using ClosedXML.Excel;
using NhatDucSoftware.Models;

namespace NhatDucSoftware.Services;

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
}

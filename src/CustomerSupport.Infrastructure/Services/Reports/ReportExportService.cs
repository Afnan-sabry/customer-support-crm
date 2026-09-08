using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CustomerSupport.Domain.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CustomerSupport.Infrastructure.Services.Reports;

public class ReportExportService : IReportExportService
{
    public Task<byte[]> ExportCsvAsync(ReportExportData data)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", data.ColumnHeaders.Select(EscapeCsvField)));
        foreach (var row in data.Rows)
            sb.AppendLine(string.Join(",", row.Select(EscapeCsvField)));
        return Task.FromResult(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray());
    }

    public Task<byte[]> ExportExcelAsync(ReportExportData data)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(Truncate(data.Title, 31));

        for (var col = 0; col < data.ColumnHeaders.Length; col++)
        {
            var cell = worksheet.Cell(1, col + 1);
            cell.Value = data.ColumnHeaders[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        for (var row = 0; row < data.Rows.Count; row++)
        {
            for (var col = 0; col < data.Rows[row].Length; col++)
                worksheet.Cell(row + 2, col + 1).Value = data.Rows[row][col];
        }

        worksheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
    }

    public Task<byte[]> ExportPdfAsync(ReportExportData data)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);

                page.Header().Column(col =>
                {
                    col.Item().Text(data.Title).FontSize(18).Bold();
                    if (data.DateRange is not null)
                        col.Item().Text($"Period: {data.DateRange}").FontSize(10).Italic();
                    col.Item().Text($"Generated: {data.GeneratedAt:yyyy-MM-dd HH:mm} UTC").FontSize(9);
                    col.Item().PaddingBottom(10);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        for (var i = 0; i < data.ColumnHeaders.Length; i++)
                            columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        foreach (var h in data.ColumnHeaders)
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5)
                                .Text(h).Bold().FontSize(9);
                        }
                    });

                    foreach (var row in data.Rows)
                    {
                        foreach (var cell in row)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                .Padding(4).Text(cell ?? "").FontSize(9);
                        }
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        });

        using var ms = new MemoryStream();
        document.GeneratePdf(ms);
        return Task.FromResult(ms.ToArray());
    }

    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            return $"\"{field.Replace("\"", "\"\"")}\"";
        return field;
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}

namespace CustomerSupport.Domain.Interfaces;

public interface IReportExportService
{
    Task<byte[]> ExportCsvAsync(ReportExportData data);
    Task<byte[]> ExportExcelAsync(ReportExportData data);
    Task<byte[]> ExportPdfAsync(ReportExportData data);
}

public record ReportExportData(
    string Title,
    string TitleAr,
    string[] ColumnHeaders,
    string[] ColumnHeadersAr,
    List<string[]> Rows,
    DateTime GeneratedAt,
    string? DateRange = null);

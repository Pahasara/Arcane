namespace Arcane.Core.Services.Interfaces;

public interface IExportService
{
    Task ExportEntryAsPdfAsync(Guid entryId, string outputPath, byte[] masterKey);
    Task ExportAllAsPdfAsync(string outputPath, byte[] masterKey);
    Task ExportEntryAsMarkdownAsync(Guid entryId, string outputPath, byte[] masterKey);
    Task ExportAllAsMarkdownZipAsync(string outputPath, byte[] masterKey);
}

using Microsoft.Extensions.Logging;

namespace BillingSuite.Infrastructure.Services.Html;

/// <summary>
/// Converts HTML string to PDF byte array
/// Note: Requires wkhtmltopdf to be installed on the system
/// Download from: https://wkhtmltopdf.org/
/// </summary>
public class HtmlToPdfService
{
    private readonly ILogger<HtmlToPdfService> _logger;

    public HtmlToPdfService(ILogger<HtmlToPdfService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Converts HTML content to PDF bytes using wkhtmltopdf command line tool
    /// </summary>
    public byte[] ConvertHtmlToPdf(string htmlContent, string? fileName = null)
    {
        try
        {
            _logger.LogDebug("Starting HTML to PDF conversion{FileName}", 
                !string.IsNullOrEmpty(fileName) ? $" for {fileName}" : "");

            // Create temporary HTML file
            var tempHtmlPath = Path.Combine(Path.GetTempPath(), $"invoice-{Guid.NewGuid()}.html");
            var tempPdfPath = Path.Combine(Path.GetTempPath(), $"invoice-{Guid.NewGuid()}.pdf");

            try
            {
                // Write HTML to temporary file
                File.WriteAllText(tempHtmlPath, htmlContent);
                _logger.LogDebug("Temporary HTML file created at {Path}", tempHtmlPath);

                // Call wkhtmltopdf to convert HTML to PDF
                ConvertUsingWkhtmltopdf(tempHtmlPath, tempPdfPath);

                // Read PDF file into byte array
                if (!File.Exists(tempPdfPath))
                {
                    throw new FileNotFoundException($"PDF file was not created at {tempPdfPath}. " +
                        "Make sure wkhtmltopdf is installed on your system. " +
                        "Download from: https://wkhtmltopdf.org/");
                }

                var pdfBytes = File.ReadAllBytes(tempPdfPath);

                _logger.LogInformation("HTML to PDF conversion successful, PDF size: {SizeKB}KB", 
                    pdfBytes.Length / 1024);

                return pdfBytes;
            }
            finally
            {
                // Clean up temporary files
                try
                {
                    if (File.Exists(tempHtmlPath))
                        File.Delete(tempHtmlPath);
                    if (File.Exists(tempPdfPath))
                        File.Delete(tempPdfPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary files");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting HTML to PDF{FileName}", 
                !string.IsNullOrEmpty(fileName) ? $" for {fileName}" : "");
            throw;
        }
    }

    /// <summary>
    /// Calls wkhtmltopdf command line tool to convert HTML to PDF
    /// </summary>
    private void ConvertUsingWkhtmltopdf(string htmlPath, string pdfPath)
    {
        try
        {
            // Try to find wkhtmltopdf in common installation paths
            string wkhtmltopdfPath = FindWkhtmltopdf();

            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = wkhtmltopdfPath,
                Arguments = $"--quiet --enable-local-file-access \"{htmlPath}\" \"{pdfPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = System.Diagnostics.Process.Start(processInfo))
            {
                if (process == null)
                {
                    throw new InvalidOperationException(
                        "Failed to start wkhtmltopdf process. " +
                        "Make sure wkhtmltopdf is installed and in your PATH. " +
                        "Download from: https://wkhtmltopdf.org/");
                }

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();
                    _logger.LogError("wkhtmltopdf error: {Error}", error);
                    throw new InvalidOperationException($"wkhtmltopdf failed: {error}");
                }
            }
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            _logger.LogError(ex, "wkhtmltopdf not found in PATH");
            throw new InvalidOperationException(
                "wkhtmltopdf is not installed or not in your PATH. " +
                "Please install it using one of these methods:\n" +
                "1. Download installer from: https://wkhtmltopdf.org/\n" +
                "2. On Windows: Download wkhtmltox-X.X.X-mxe-w64.zip from https://github.com/wkhtmltopdf/packaging/releases\n" +
                "3. Add the installation directory to your system PATH environment variable\n" +
                "4. Or restart your application after installation",
                ex);
        }
    }

    /// <summary>
    /// Attempts to find wkhtmltopdf executable in common locations
    /// </summary>
    private string FindWkhtmltopdf()
    {
        // Common installation paths for wkhtmltopdf on Windows
        string[] commonPaths = new[]
        {
            "wkhtmltopdf",  // Try PATH first
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "wkhtmltopdf", "bin", "wkhtmltopdf.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "wkhtmltopdf", "bin", "wkhtmltopdf.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "wkhtmltopdf", "bin", "wkhtmltopdf.exe"),
            // Portable/alternative installation paths
            @"C:\Program Files\wkhtmltopdf\bin\wkhtmltopdf.exe",
            @"C:\Program Files (x86)\wkhtmltopdf\bin\wkhtmltopdf.exe",
            @"C:\wkhtmltopdf\bin\wkhtmltopdf.exe",
        };

        foreach (var path in commonPaths)
        {
            try
            {
                if (File.Exists(path))
                {
                    _logger.LogInformation("Found wkhtmltopdf at: {Path}", path);
                    return path;
                }
            }
            catch
            {
                // Continue to next path
            }
        }

        // If not found in common paths, return the base command and let it fail with better error message
        _logger.LogWarning("wkhtmltopdf not found in common installation paths");
        return "wkhtmltopdf";
    }
}


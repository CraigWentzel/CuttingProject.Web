using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CuttingProject.Web.Services
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public interface IPdfValidatorService
    {
        ValidationResult Validate(IFormFile file, string[] lines);
    }

    public class PdfValidatorService : IPdfValidatorService
    {
        public ValidationResult Validate(IFormFile file, string[] lines)
        {
            ValidationResult result = new ValidationResult { IsValid = true };

            // File size check — 10MB max
            if (file.Length > 10 * 1024 * 1024)
            {
                result.IsValid = false;
                result.Errors.Add("File exceeds the 10MB maximum size.");
            }

            // Must contain expected header keywords
            bool hasHeader = lines.Any(l =>
                l.Contains("BI PROJECTS", StringComparison.OrdinalIgnoreCase) ||
                l.Contains("CRAIG RYAN", StringComparison.OrdinalIgnoreCase));

            if (!hasHeader)
            {
                result.IsValid = false;
                result.Errors.Add("This does not appear to be a valid cutting list PDF.");
            }

            // Must have at least one parseable data row
            bool hasData = lines.Any(l =>
            {
                string[] cols = l.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return cols.Length >= 7
                    && double.TryParse(cols[2], out double _)
                    && int.TryParse(cols[3], out int _);
            });

            if (!hasData)
            {
                result.IsValid = false;
                result.Errors.Add("No valid parts data found. Check the PDF format.");
            }

            return result;
        }
    }
}

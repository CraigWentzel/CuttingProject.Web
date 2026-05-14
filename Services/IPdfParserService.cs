using CuttingProject.Web.Models;

namespace CuttingProject.Web.Services
{
    
        public interface IPdfParserService
        {
            string[] ReadPdfLines(Stream fileStream);
            List<PartsRecord> ParsePartsFile(string[] lines);
            string ExtractProjectName(string[] lines);
        }
}


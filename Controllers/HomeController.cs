using Microsoft.AspNetCore.Mvc;
using CuttingProject.Web.Services;
using CuttingProject.Web.Models;

namespace CuttingProject.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IPdfParserService _parser;
        private readonly IBinAllocatorService _allocator;
        private readonly IPdfExportService _exporter;
        private readonly IPdfValidatorService _validator;

        public HomeController(
            IPdfParserService parser,
            IBinAllocatorService allocator,
            IPdfExportService exporter,
            IPdfValidatorService validator)
        {
            _parser = parser;
            _allocator = allocator;
            _exporter = exporter;
            _validator = validator;
        }

        public IActionResult Index() => View();
        public IActionResult About() => View();

        public IActionResult NotFoundPage()
        {
            Response.StatusCode = 404;
            return View("NotFound");
        }

        [HttpPost]
        public IActionResult Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Please upload a PDF file.");
                return View("Index");
            }

            // Step 1: Parse lines from PDF stream
            string[] lines;
            using (Stream stream = file.OpenReadStream())
            {
                lines = _parser.ReadPdfLines(stream);
            }

            // Step 2: Validate BEFORE doing anything else
            ValidationResult validation = _validator.Validate(file, lines);
            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors)
                    ModelState.AddModelError("", error);
                return View("Index");
            }

            // Step 3: Parse into parts records and group by profile
            List<PartsRecord> parts = _parser.ParsePartsFile(lines);
            string projectName = _parser.ExtractProjectName(lines);

            var partGroups = parts.GroupBy(p => p.Profile);

            // Step 4: Allocate to bins
            List<StockBin> bins = _allocator.AllocateToBins(partGroups);
            BinSummary summary = _allocator.BuildSummary(bins);

            // Step 5: Store in session and redirect
            HttpContext.Session.SetString("CuttingBins",
                System.Text.Json.JsonSerializer.Serialize(bins));
            HttpContext.Session.SetString("CuttingSummary",
                System.Text.Json.JsonSerializer.Serialize(summary));
            HttpContext.Session.SetString("ProjectName", projectName);

            return RedirectToAction("Results");
        }

        public IActionResult Results()
        {
            var binsJson = HttpContext.Session.GetString("CuttingBins");
            var summaryJson = HttpContext.Session.GetString("CuttingSummary");

            if (string.IsNullOrEmpty(binsJson) || string.IsNullOrEmpty(summaryJson))
                return RedirectToAction("Index");

            var bins = System.Text.Json.JsonSerializer
                .Deserialize<List<StockBin>>(binsJson);
            var summary = System.Text.Json.JsonSerializer
                .Deserialize<BinSummary>(summaryJson);

            ViewBag.ProjectName = HttpContext.Session.GetString("ProjectName");
            ViewBag.Summary = summary;

            return View(bins);
        }
    }
}
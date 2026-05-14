using ClosedXML.Excel;
using CuttingProject.Web.Models;
using CuttingProject.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CuttingProject.Web.Controllers
{
    public class CuttingController : Controller
    {
        private readonly IPdfParserService _pdfParser;
        private readonly IBinAllocatorService _binAllocator;
        private readonly IPdfExportService _pdfExport;
        private readonly IPdfValidatorService _pdfValidator;

        public CuttingController(
            IPdfParserService pdfParser,
            IBinAllocatorService binAllocator,
            IPdfExportService pdfExport,
            IPdfValidatorService pdfValidator)
        {
            _pdfParser = pdfParser;
            _binAllocator = binAllocator;
            _pdfExport = pdfExport;
            _pdfValidator = pdfValidator;
        }

        // ── GET /Cutting/Upload ────────────────────────────────────
        public IActionResult Upload() => View();

        // ── POST /Cutting/Upload ───────────────────────────────────
        [HttpPost]
        public IActionResult Upload(IFormFile pdfFile)
        {
            if (pdfFile == null || pdfFile.Length == 0)
            {
                ModelState.AddModelError("", "Please select a PDF file.");
                return View();
            }

            try
            {
                using Stream stream = pdfFile.OpenReadStream();
                string[] lines = _pdfParser.ReadPdfLines(stream);
                List<PartsRecord> parts = _pdfParser.ParsePartsFile(lines);
                string projectName = _pdfParser.ExtractProjectName(lines);

                if (parts.Count == 0)
                {
                    ModelState.AddModelError("",
                        "No parts found in this PDF. " +
                        "Please check the file is a valid cutting list.");
                    return View();
                }

                List<PartsRecord> sorted = parts
                    .OrderBy(p => new string(
                        p.Profile.TakeWhile(c => !char.IsDigit(c))
                                 .ToArray()).ToUpper())
                    .ThenBy(p =>
                    {
                        string digits = new string(
                            p.Profile.SkipWhile(c => !char.IsDigit(c))
                                     .TakeWhile(c => char.IsDigit(c)).ToArray());
                        return int.TryParse(digits, out int n) ? n : 0;
                    })
                    .ThenBy(p => p.Profile)
                    .ThenBy(p => new string(
                        p.Part.TakeWhile(c => !char.IsDigit(c))
                              .ToArray()).ToUpper())
                    .ThenBy(p =>
                    {
                        string digits = new string(
                            p.Part.SkipWhile(c => !char.IsDigit(c)).ToArray());
                        return int.TryParse(digits, out int n) ? n : 0;
                    })
                    .ToList();

                CuttingSession session = new CuttingSession
                {
                    ProjectName = projectName,
                    Parts = sorted
                };

                HttpContext.Session.SetString("CuttingSession",
                    JsonSerializer.Serialize(session));

                return RedirectToAction("Parts");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("",
                    "Failed to process PDF: " + ex.Message);
                return View();
            }
        }

        // ── GET /Cutting/Parts ─────────────────────────────────────
        public IActionResult Parts()
        {
            CuttingSession? session = GetSession();

            if (session == null || session.Parts == null || session.Parts.Count == 0)
            {
                TempData["Warning"] =
                    "Your session expired. Please upload your PDF again.";
                return RedirectToAction("Upload");
            }

            return View(session);
        }

        // ── POST /Cutting/Allocate ─────────────────────────────────
        [HttpPost]
        public IActionResult Allocate()
        {
            CuttingSession? session = GetSession();

            if (session == null || session.Parts == null || session.Parts.Count == 0)
            {
                TempData["Warning"] =
                    "Your session expired. Please upload your PDF again.";
                return RedirectToAction("Upload");
            }

            IEnumerable<IGrouping<string, PartsRecord>> groups =
              session.Parts.GroupBy(p => p.Profile);

            List<StockBin> bins = _binAllocator.AllocateToBins(groups);
            BinSummary summary = _binAllocator.BuildSummary(bins);

            session.Bins = bins;
            session.Summary = summary;

            HttpContext.Session.SetString("CuttingSession",
                JsonSerializer.Serialize(session));

            return RedirectToAction("Bins");
        }

        // ── GET /Cutting/Bins ──────────────────────────────────────
        public IActionResult Bins()
        {
            CuttingSession? session = GetSession();

            if (session == null || session.Bins == null || session.Bins.Count == 0)
            {
                TempData["Warning"] =
                    "Your session expired. Please upload your PDF again.";
                return RedirectToAction("Upload");
            }

            return View(session);
        }

        // ── POST /Cutting/ExportExcel ──────────────────────────────
        [HttpPost]
        public IActionResult ExportExcel()
        {
            CuttingSession? session = GetSession();

            if (session == null || session.Bins == null || session.Bins.Count == 0)
                return RedirectToAction("Upload");

            using XLWorkbook workbook = new XLWorkbook();

            List<string> profileNames = session.Bins
                .Select(b => b.Profile)
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            foreach (string profileName in profileNames)
            {
                List<StockBin> profileBins = session.Bins
                    .Where(b => b.Profile == profileName)
                    .OrderBy(b => b.BinNumber)
                    .ToList();

                if (profileBins.Count == 0) continue;

                string safeName = profileName
                    .Replace("/", "-").Replace("\\", "-")
                    .Replace("?", "").Replace("*", "")
                    .Replace("[", "(").Replace("]", ")")
                    .Replace(":", "-").Replace("'", "")
                    .Replace(".", "").Replace(" ", "");

                if (safeName.Length > 31)
                    safeName = safeName.Substring(0, 31);

                string finalName = safeName;
                int sheetCounter = 1;
                while (workbook.Worksheets.Any(w => w.Name == finalName))
                {
                    string trimmed = safeName.Length > 29
                        ? safeName.Substring(0, 29) : safeName;
                    finalName = trimmed + sheetCounter;
                    sheetCounter++;
                }

                IXLWorksheet ws = workbook.Worksheets.Add(finalName);

                ws.Cell(1, 1).Value = "CRAIG RYAN CONSULTING — CUTTING LIST";
                ws.Range(1, 1, 1, 9).Merge();
                ws.Cell(1, 1).Style.Font.Bold = true;
                ws.Cell(1, 1).Style.Font.Italic = true;
                ws.Cell(1, 1).Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;
                ws.Cell(1, 1).Style.Fill.BackgroundColor =
                    XLColor.FromArgb(15, 76, 129);
                ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;

                ws.Cell(2, 1).Value = "PROJECT:";
                ws.Cell(2, 1).Style.Font.Bold = true;
                ws.Cell(2, 2).Value = session.ProjectName;
                ws.Cell(2, 5).Value = "PROFILE:";
                ws.Cell(2, 5).Style.Font.Bold = true;
                ws.Cell(2, 6).Value = profileName;

                ws.Cell(3, 2).Value = "QTY BOUGHT:";
                ws.Cell(3, 2).Style.Font.Bold = true;
                ws.Cell(3, 5).Value = "QTY REQUIRED:";
                ws.Cell(3, 5).Style.Font.Bold = true;
                ws.Cell(3, 8).Value = "QTY SHORT:";
                ws.Cell(3, 8).Style.Font.Bold = true;

                string[] headers =
                {
                    "NO", "QTY", "CUT LENGTH", "Z JOIN 300mm",
                    "TOTAL METERS", "QTY", "LENGTHS", "OFF CUTS", "NOTE"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    IXLCell headerCell = ws.Cell(4, i + 1);
                    headerCell.Value = headers[i];
                    headerCell.Style.Font.Bold = true;
                    headerCell.Style.Fill.BackgroundColor =
                        XLColor.FromArgb(15, 76, 129);
                    headerCell.Style.Font.FontColor = XLColor.White;
                    headerCell.Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;
                    headerCell.Style.Border.OutsideBorder =
                        XLBorderStyleValues.Thin;
                }

                int currentRow = 5;

                IEnumerable<IGrouping<string, StockBin>> binGroups =
                    profileBins.GroupBy(b => b.BinNumber).OrderBy(g => g.Key);

                foreach (IGrouping<string, StockBin> binGroup in binGroups)
                {
                    foreach (StockBin bin in binGroup)
                    {
                        ws.Cell(currentRow, 1).Value = bin.Part;
                        ws.Cell(currentRow, 2).Value = bin.Quantity;
                        ws.Cell(currentRow, 3).Value = bin.CutLength;
                        ws.Cell(currentRow, 4).Value = "";
                        ws.Cell(currentRow, 5).Value = bin.TotalMeters;
                        ws.Cell(currentRow, 6).Value = "";
                        ws.Cell(currentRow, 7).Value = "";
                        ws.Cell(currentRow, 8).Value = "";
                        ws.Cell(currentRow, 9).Value = "";

                        ws.Range(currentRow, 1, currentRow, 9)
                            .Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                        currentRow++;
                    }

                    double binTotalMeters = Math.Round(
                        binGroup.Sum(b => b.TotalMeters), 3);
                    double binLength = binGroup.Max(b => b.BinLength);
                    double binOffCut = binGroup.Max(b => b.OffCut);

                    IXLRange summaryRange = ws.Range(currentRow, 1, currentRow, 9);
                    summaryRange.Style.Fill.BackgroundColor =
                        XLColor.FromArgb(255, 243, 205);
                    summaryRange.Style.Font.Bold = true;
                    summaryRange.Style.Border.OutsideBorder =
                        XLBorderStyleValues.Thin;

                    ws.Cell(currentRow, 5).Value = binTotalMeters;
                    ws.Cell(currentRow, 6).Value = 1;
                    ws.Cell(currentRow, 7).Value = binLength;
                    ws.Cell(currentRow, 8).Value = binOffCut;

                    currentRow++;
                }

                double profileTotal = Math.Round(
                    profileBins.Sum(b => b.TotalMeters), 3);
                double profileBinTotal = profileBins
                    .Where(b => b.BinLength > 0).Sum(b => b.BinLength);
                double profileOffCut = Math.Round(
                    profileBins.Where(b => b.OffCut > 0).Sum(b => b.OffCut), 3);

                ws.Cell(currentRow, 1).Value = "TOTAL";
                ws.Cell(currentRow, 5).Value = profileTotal;
                ws.Cell(currentRow, 7).Value = profileBinTotal;
                ws.Cell(currentRow, 8).Value = profileOffCut;

                IXLRange totalRange = ws.Range(currentRow, 1, currentRow, 9);
                totalRange.Style.Font.Bold = true;
                totalRange.Style.Fill.BackgroundColor =
                    XLColor.FromArgb(15, 76, 129);
                totalRange.Style.Font.FontColor = XLColor.White;
                totalRange.Style.Border.OutsideBorder =
                    XLBorderStyleValues.Thin;

                ws.Columns().AdjustToContents();
            }

            using MemoryStream stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            string downloadName = $"CuttingList_{session.ProjectName}_" +
                                  $"{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                downloadName);
        }

        // ── POST /Cutting/ExportPdf ────────────────────────────────
        [HttpPost]
        public IActionResult ExportPdf()
        {
            CuttingSession? session = GetSession();

            if (session == null || session.Bins == null || session.Bins.Count == 0)
                return RedirectToAction("Upload");

            byte[] pdf = _pdfExport.GenerateBinAllocationPdf(session);

            string downloadName = $"CuttingList_{session.ProjectName}_" +
                                  $"{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            return File(pdf, "application/pdf", downloadName);
        }

        // ── Helper ─────────────────────────────────────────────────
        private CuttingSession? GetSession()
        {
            string? json = HttpContext.Session.GetString("CuttingSession");

            if (string.IsNullOrEmpty(json))
                return null;

            return JsonSerializer.Deserialize<CuttingSession>(json);
        }
    }
}
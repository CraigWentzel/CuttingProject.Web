using CuttingProject.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CuttingProject.Web.Services
{
    public interface IPdfExportService
    {
        byte[] GenerateBinAllocationPdf(CuttingSession session);
    }

    public class PdfExportService : IPdfExportService
    {
        public byte[] GenerateBinAllocationPdf(CuttingSession session)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            IEnumerable<IGrouping<string, StockBin>> grouped =
                session.Bins.GroupBy(b => b.BinNumber);

            string[] headers =
            {
                "Bin No", "Profile", "Part", "Qty",
                "Cut Length", "Total Meters", "Bin Length", "Off Cut"
            };

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    // ── Page Setup ────────────────────────────────
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(8));

                    // ── Header ────────────────────────────────────
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem()
                                .Text("CRAIG RYAN CONSULTING — CUTTING LIST BIN ALLOCATION")
                                .FontSize(11).Bold().FontColor("#0f4c81");

                            row.ConstantItem(200).AlignRight()
                                .Text($"Date: {DateTime.Now:dd MMM yyyy}")
                                .FontSize(8).FontColor("#6c757d");
                        });

                        col.Item().PaddingTop(4)
                            .Text($"Project: {session.ProjectName}")
                            .FontSize(9).Bold();

                        col.Item().PaddingTop(4)
                            .Text(
                                $"Total Bins: {session.Summary.TotalBins}   " +
                                $"6m Bins: {session.Summary.TotalSixMeterBins}   " +
                                $"13m Bins: {session.Summary.TotalThirteenMeterBins}   " +
                                $"Total Meters: {session.Summary.TotalMetersUsed}m   " +
                                $"Total Off Cut: {session.Summary.TotalOffCut}m")
                            .FontSize(8).FontColor("#6c757d");

                        col.Item().PaddingTop(6)
                            .LineHorizontal(1).LineColor("#0f4c81");
                    });

                    // ── Content ───────────────────────────────────
                    page.Content().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(55);   // Bin No
                            cols.RelativeColumn(2);    // Profile
                            cols.RelativeColumn(1.5f); // Part
                            cols.ConstantColumn(35);   // Qty
                            cols.ConstantColumn(60);   // Cut Length
                            cols.ConstantColumn(65);   // Total Meters
                            cols.ConstantColumn(60);   // Bin Length
                            cols.ConstantColumn(55);   // Off Cut
                        });

                        // Table Header
                        table.Header(h =>
                        {
                            foreach (string hdr in headers)
                            {
                                h.Cell().Background("#0f4c81").Padding(5)
                                    .Text(hdr).FontColor("#ffffff")
                                    .Bold().FontSize(7.5f);
                            }
                        });

                        bool shadeBin = false;
                        string lastBin = string.Empty;

                        foreach (IGrouping<string, StockBin> group in grouped)
                        {
                            double usedTotal = Math.Round(
                                group.Where(b => b.Part != "SUMMARY")
                                     .Sum(b => b.TotalMeters), 3);

                            double binLength = Math.Round(group.Sum(b => b.BinLength), 3);
                            double offCut = Math.Round(group.Sum(b => b.OffCut), 3);

                            if (group.Key != lastBin)
                            {
                                lastBin = group.Key;
                                shadeBin = !shadeBin;
                            }

                            string rowBg = shadeBin ? "#f0f6ff" : "#ffffff";

                            foreach (StockBin bin in group)
                            {
                                string bg = bin.BinLength > 0 ? "#fff3cd" : rowBg;

                                table.Cell().Background(bg).Padding(4)
                                    .Text(bin.BinNumber).FontSize(7.5f);

                                table.Cell().Background(bg).Padding(4)
                                    .Text(bin.Profile).FontSize(7.5f);

                                table.Cell().Background(bg).Padding(4)
                                    .Text(bin.Part).FontSize(7.5f);

                                table.Cell().Background(bg).Padding(4).AlignCenter()
                                    .Text(bin.Quantity.ToString()).FontSize(7.5f);

                                table.Cell().Background(bg).Padding(4).AlignCenter()
                                    .Text(bin.CutLength.ToString("F3")).FontSize(7.5f);

                                table.Cell().Background(bg).Padding(4).AlignCenter()
                                    .Text(bin.TotalMeters.ToString("F3")).FontSize(7.5f);

                                table.Cell().Background(bg).Padding(4).AlignCenter()
                                    .Text(bin.BinLength > 0 ? bin.BinLength.ToString("F3") : "")
                                    .FontSize(7.5f);

                                table.Cell().Background(bg).Padding(4).AlignCenter()
                                    .Text(bin.OffCut > 0 ? bin.OffCut.ToString("F3") : "")
                                    .FontSize(7.5f);
                            }

                            // Summary row per bin
                            table.Cell().ColumnSpan(8).Background("#e9ecef").Padding(4)
                                .Text($"✔  Used: {usedTotal:F3}  +  OffCut: {offCut:F3}  =  {binLength:F3}")
                                .Italic().FontColor("#6c757d").FontSize(7.5f);
                        }
                    });

                    // ── Footer ────────────────────────────────────
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.DefaultTextStyle(t => t.FontSize(7.5f).FontColor("#6c757d"));
                        text.Span("Craig Ryan Consulting — Cutting List   |   Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();
        }
    }
}

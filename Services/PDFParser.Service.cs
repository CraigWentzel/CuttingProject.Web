
    using CuttingProject.Web.Models;
    using UglyToad.PdfPig;
    using UglyToad.PdfPig.Content;

    namespace CuttingProject.Web.Services
    {
        public class PdfParserService : IPdfParserService
        {
            public string[] ReadPdfLines(Stream fileStream)
            {
                List<string> lines = new List<string>();

                using (PdfDocument document = PdfDocument.Open(fileStream))
                {
                    foreach (Page page in document.GetPages())
                    {
                        IEnumerable<IGrouping<double, Word>> lineGroups = page.GetWords()
                            .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 0))
                            .OrderByDescending(g => g.Key);

                        foreach (IGrouping<double, Word> lineGroup in lineGroups)
                        {
                            IEnumerable<string> words = lineGroup
                                .OrderBy(w => w.BoundingBox.Left)
                                .Select(w => w.Text);

                            lines.Add(string.Join(" ", words));
                        }
                    }
                }

                return lines.ToArray();
            }

            public List<PartsRecord> ParsePartsFile(string[] allLines)
            {
                List<PartsRecord> parts = new List<PartsRecord>();

                HashSet<string> skipStartsWith = new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase)
            {
                "BI PROJECTS", "TO:", "ATT:", "ALL PARTS", "REV",
                "Total of", "Profile total", "---", "Part"
            };

                foreach (string line in allLines)
                {
                    string trimmed = line.Trim();

                    if (string.IsNullOrWhiteSpace(trimmed)) continue;
                    if (trimmed.Contains("Page")) continue;
                    if (trimmed.Contains("Date")) continue;

                    bool skip = false;
                    foreach (string prefix in skipStartsWith)
                    {
                        if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            skip = true;
                            break;
                        }
                    }
                    if (skip) continue;

                    string[] columns = trimmed.Split(
                        new char[] { ' ' },
                        StringSplitOptions.RemoveEmptyEntries);

                    if (columns.Length < 7) continue;
                    if (!double.TryParse(columns[2], out double lengthRaw)) continue;
                    if (!int.TryParse(columns[3], out int quantity)) continue;

                    parts.Add(new PartsRecord
                    {
                        Profile = columns[0].Replace("*", "X"),
                        Part = columns[1],
                        Length = Math.Round(lengthRaw / 1000, 3),
                        Quantity = quantity
                    });
                }

                return parts;
            }

            public string ExtractProjectName(string[] lines)
            {
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();

                    if (trimmed.Contains("Page")
                        && !trimmed.StartsWith("Part")
                        && !trimmed.StartsWith("BI PROJECTS"))
                    {
                        string candidate = trimmed
                            .Substring(0, trimmed.LastIndexOf("Page"))
                            .Trim();

                        if (!string.IsNullOrWhiteSpace(candidate))
                            return candidate;
                    }
                }

                return string.Empty;
            }
        }
    }
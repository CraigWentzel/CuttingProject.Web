using CuttingProject.Web.Models;

namespace CuttingProject.Web.Services
{
    public class BinAllocatorService : IBinAllocatorService
    {
        public List<StockBin> AllocateToBins(
            IEnumerable<IGrouping<string, PartsRecord>> partGroups)
        {
            List<StockBin> bins = new List<StockBin>();
            int binCounter = 1;

            foreach (IGrouping<string, PartsRecord> partGroup in
                     partGroups.OrderBy(g => g.Key))
            {
                List<PartsRecord> profiles = partGroup
                    .OrderByDescending(r => r.TotalMeters)
                    .ToList();

                while (profiles.Count > 0)
                {
                    double totalRemaining = Math.Round(
                        profiles.Sum(r => r.TotalMeters), 3);

                    double binLength = totalRemaining <= 6.0 ? 6.0 : 13.0;
                    double binUsed = 0;

                    List<PartsRecord> binProfiles = new List<PartsRecord>();

                    foreach (PartsRecord record in profiles.ToList())
                    {
                        double projected = Math.Round(
                            binUsed + record.TotalMeters, 3);

                        if (projected <= binLength)
                        {
                            binUsed = projected;
                            binProfiles.Add(record);
                            profiles.Remove(record);
                        }
                    }

                    // ── Force fit — try 13m first ──────────────
                    if (binProfiles.Count == 0 && profiles.Count > 0)
                    {
                        PartsRecord forced = profiles[0];

                        if (forced.TotalMeters <= 13.0)
                        {
                            binLength = 13.0;
                            binUsed = Math.Round(forced.TotalMeters, 3);
                            binProfiles.Add(forced);
                            profiles.Remove(forced);
                        }
                        else
                        {
                            binLength = Math.Ceiling(forced.TotalMeters);
                            binUsed = Math.Round(forced.TotalMeters, 3);
                            binProfiles.Add(forced);
                            profiles.Remove(forced);
                        }
                    }

                    double offCut = Math.Round(binLength - binUsed, 3);

                    // Add detail rows
                    foreach (PartsRecord record in binProfiles)
                    {
                        bins.Add(new StockBin
                        {
                            BinNumber = "Bin" + binCounter.ToString("D3"),
                            Part = record.Part,
                            Profile = record.Profile,
                            Quantity = record.Quantity,
                            CutLength = record.Length,
                            TotalMeters = record.TotalMeters,
                            BinLength = 0,   // leave 0 for detail rows
                            OffCut = 0,      // leave 0 for detail rows
                            IsPlaced = true
                        });
                    }

                    // Add summary row tied to the same profile
                    bins.Add(new StockBin
                    {
                        BinNumber = "Bin" + binCounter.ToString("D3"),
                        Part = "SUMMARY",
                        Profile = binProfiles.First().Profile,
                        Quantity = 0,
                        CutLength = 0,
                        TotalMeters = Math.Round(binProfiles.Sum(r => r.TotalMeters), 3),
                        BinLength = binLength,
                        OffCut = offCut,
                        IsPlaced = true
                    });

                    binCounter++;
                }
            }

            return bins;
        }

        public BinSummary BuildSummary(List<StockBin> bins)
        {
            return new BinSummary
            {
                TotalBins = bins
                    .Select(b => b.BinNumber)
                    .Distinct()
                    .Count(),

                // Exclude SUMMARY rows to avoid double-counting detail rows
                TotalMetersUsed = Math.Round(
                    bins.Where(b => b.Part != "SUMMARY")
                        .Sum(b => b.TotalMeters), 3),

                TotalOffCut = Math.Round(
                    bins.Where(b => b.OffCut > 0)
                        .Sum(b => b.OffCut), 3),

                TotalSixMeterBins = bins
                    .Where(b => b.BinLength == 6.0)
                    .Select(b => b.BinNumber)
                    .Distinct()
                    .Count(),

                TotalThirteenMeterBins = bins
                    .Where(b => b.BinLength == 13.0)
                    .Select(b => b.BinNumber)
                    .Distinct()
                    .Count()
            };
        }
    }
}
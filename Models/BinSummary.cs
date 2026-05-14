namespace CuttingProject.Web.Models
{
    public class BinSummary
    {
        public int TotalBins { get; set; }
        public double TotalMetersUsed { get; set; }
        public double TotalOffCut { get; set; }
        public int TotalSixMeterBins { get; set; }
        public int TotalThirteenMeterBins { get; set; }
        public double EfficiencyPercent =>
        TotalMetersUsed + TotalOffCut > 0
        ? Math.Round(TotalMetersUsed /
            (TotalMetersUsed + TotalOffCut) * 100, 1)
        : 0;
    }
}

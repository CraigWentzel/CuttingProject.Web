namespace CuttingProject.Web.Models
{
    public class StockBin
    {
        public string BinNumber { get; set; } = string.Empty;
        public string Profile { get; set; } = string.Empty;
        public string Part { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public double CutLength { get; set; }
        public double TotalMeters { get; set; }
        public double BinLength { get; set; }
        public double OffCut { get; set; }
        public bool IsPlaced { get; set; }
    }
}

namespace CuttingProject.Web.Models
{
    public class CuttingSession
    {
        public string ProjectName { get; set; } = string.Empty;
        public List<PartsRecord> Parts { get; set; } = new();
        public List<StockBin> Bins { get; set; } = new();
        public BinSummary Summary { get; set; } = new();
    }
}

namespace CuttingProject.Web.Models
{
    public class PartsRecord
    {

        public string Profile { get; set; } = string.Empty;
        public string Part { get; set; } = string.Empty;
        public double Length { get; set; }
        public int Quantity { get; set; }
        public double TotalMeters => Math.Round(Length * Quantity, 3);
    }
}

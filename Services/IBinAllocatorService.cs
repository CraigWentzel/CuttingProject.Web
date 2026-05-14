using CuttingProject.Web.Models;

namespace CuttingProject.Web.Services
{
    public interface IBinAllocatorService
    {
        List<StockBin> AllocateToBins(
            IEnumerable<IGrouping<string, PartsRecord>> partGroups);

        BinSummary BuildSummary(List<StockBin> bins);
    }
}
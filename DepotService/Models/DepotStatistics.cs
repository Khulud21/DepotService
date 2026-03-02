namespace DepotService.Models
{
    public class DepotStatistics
    {
        public int TotalDepots { get; set; }
        public int OnlineCount { get; set; }
        public int OfflineCount { get; set; }
        public int WarningCount { get; set; }
        public int OutdatedCount { get; set; }

        public double OnlinePercentage => TotalDepots > 0
            ? (double)OnlineCount / TotalDepots * 100
            : 0;

        public double OfflinePercentage => TotalDepots > 0
            ? (double)OfflineCount / TotalDepots * 100
            : 0;

        public double WarningPercentage => TotalDepots > 0
            ? (double)WarningCount / TotalDepots * 100
            : 0;

        public double HealthScore => TotalDepots > 0
            ? (double)(OnlineCount * 100 + WarningCount * 50) / TotalDepots
            : 0;
    }
}

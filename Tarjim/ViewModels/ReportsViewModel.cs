namespace Tarjim.ViewModels
{
    public class ReportsViewModel
    {
        public ProjectStatisticsViewModel ProjectStatistics { get; set; }
        public FinancialStatisticsViewModel FinancialStatistics { get; set; }
        public UserStatisticsViewModel UserStatistics { get; set; }
        public List<CategoryStatisticsViewModel> CategoryStatistics { get; set; }
        public List<LanguageStatisticsViewModel> LanguageStatistics { get; set; }
    }

    public class ProjectStatisticsViewModel
    {
        public int Total { get; set; }
        public int Open { get; set; }
        public int InProgress { get; set; }
        public int Completed { get; set; }
        public int Canceled { get; set; }
    }

    public class FinancialStatisticsViewModel
    {
        public decimal TotalRevenue { get; set; }
        public decimal PlatformProfit { get; set; }
        public decimal PendingPayments { get; set; }
        public decimal CompletedPayments { get; set; }
    }

    public class UserStatisticsViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalClients { get; set; }
        public int TotalTranslators { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
    }

    public class CategoryStatisticsViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int ProjectCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class LanguageStatisticsViewModel
    {
        public int LanguageId { get; set; }
        public string LanguageName { get; set; }
        public int AsSourceCount { get; set; }
        public int AsTargetCount { get; set; }
    }
}

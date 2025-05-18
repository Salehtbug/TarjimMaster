using System.Collections.Generic;
using Tarjim.Models;

namespace Tarjim.ViewModels
{
    public class AdminDashboardViewModel
    {
        // ملخص النظام
        public int TotalUsers { get; set; }
        public int TotalClients { get; set; }
        public int TotalTranslators { get; set; }
        public int TotalProjects { get; set; }

        // إحصائيات المشاريع
        public int OpenProjects { get; set; }
        public int InProgressProjects { get; set; }
        public int CompletedProjects { get; set; }
        public int CanceledProjects { get; set; }

        // إحصائيات مالية
        public decimal TotalRevenue { get; set; }
        public decimal PlatformProfit { get; set; }
        public decimal PendingPayments { get; set; }
        public decimal CompletedPayments { get; set; }

        // إحصائيات المترجمين
        public int ActiveTranslators { get; set; }
        public double AverageTranslatorRating { get; set; }
        public List<User> TopTranslators { get; set; }

        // إحصائيات العملاء
        public int ActiveClients { get; set; }
        public List<User> TopClients { get; set; }

        // إحصائيات فئات الترجمة
        public List<CategoryStatistics> TopCategories { get; set; }
        public List<LanguageStatistics> TopLanguages { get; set; }

        // النشاط الحديث
        public List<ActivityLog> RecentActivities { get; set; }
        public List<Project> RecentProjects { get; set; }
        public List<User> RecentUsers { get; set; }

        // طلبات الترجمة الفورية
        public int PendingInstantRequests { get; set; }
        public List<InstantTranslatorRequestViewModel> RecentInstantRequests { get; set; }
    }

    public class CategoryStatistics
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int ProjectCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class LanguageStatistics
    {
        public int LanguageId { get; set; }
        public string LanguageName { get; set; }
        public int AsSourceCount { get; set; }
        public int AsTargetCount { get; set; }
    }
}
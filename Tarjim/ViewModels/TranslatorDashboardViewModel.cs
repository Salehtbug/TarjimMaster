using System.Collections.Generic;
using Tarjim.Models;

namespace Tarjim.ViewModels
{
    public class TranslatorDashboardViewModel
    {
        public List<Project> AvailableProjects { get; set; } // أضف هذه الخاصية
        public List<Project> ActiveProjects { get; set; }
        public List<Project> CompletedProjects { get; set; }
        public List<ProjectOffer> PendingOffers { get; set; }
        public List<Notification> Unreadnotifications { get; set; }
        public User Translator { get; set; }
        public List<TranslatorCv> TranslatorCv { get; set; }
        public double TotalRating { get; set; }
        public int TotalReviews { get; set; }
    }
}

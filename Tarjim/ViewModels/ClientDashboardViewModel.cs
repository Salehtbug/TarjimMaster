using System.Collections.Generic;
using Tarjim.Models;

namespace Tarjim.ViewModels
{
    public class ClientDashboardViewModel
    {
        public List<Project> OpenProjects { get; set; }
        public List<Project> ActiveProjects { get; set; }
        public List<Project> CompletedProjects { get; set; }
        public int NewOffersCount { get; set; }
        public List<Notification> UnreadNotifications { get; set; }
        public User Client { get; set; }
    }
}
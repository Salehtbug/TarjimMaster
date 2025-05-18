using System.Collections.Generic;
using Tarjim.Models;

namespace Tarjim.ViewModels
{
    public class AllProjectsViewModel
    {
        public List<Project> OpenProjects { get; set; }
        public List<Project> ActiveProjects { get; set; }
        public List<Project> CompletedProjects { get; set; }
    }
}
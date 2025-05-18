using Tarjim.Models;

namespace Tarjim.ViewModels
{
    public class ProjectDetailsViewModel
    {
        public Project? Project { get; set; }
        public List<ProjectFile>? ProjectFiles { get; set; }
        public List<ProjectOffer>? ProjectOffers { get; set; }
        public List<ProjectRequirement>? ProjectRequirements { get; set; }
    }
}

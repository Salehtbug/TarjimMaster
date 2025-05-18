using System.Collections.Generic;
using Tarjim.Models;

namespace Tarjim.ViewModels
{
    public class ProjectOffersViewModel
    {
        public Project Project { get; set; }
        public List<ProjectOffer> Offers { get; set; }
    }
}

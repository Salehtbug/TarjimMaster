using Tarjim.Models;

namespace Tarjim.ViewModels
{
    public class WorkOnProjectViewModel
    {
        public Project Project { get; set; }
        public List<ProjectFile> OriginalFiles { get; set; }
        public List<ProjectFile> TranslatedFiles { get; set; }
        public List<ProjectRequirement> Requirements { get; set; }
    }
}

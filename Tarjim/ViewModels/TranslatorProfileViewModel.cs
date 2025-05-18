using Tarjim.Models;

namespace Tarjim.ViewModels
{
    public class TranslatorProfileViewModel
    {
        public User Translator { get; set; }
        public List<TranslatorCv> CvItems { get; set; }
        public List<Skill> AllSkills { get; set; }
        public List<int> SelectedSkillIds { get; set; }
        public List<Review> Reviews { get; set; }
    }
}

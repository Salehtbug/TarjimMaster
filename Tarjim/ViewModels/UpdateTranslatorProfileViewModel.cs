using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Tarjim.ViewModels
{
    public class UpdateTranslatorProfileViewModel
    {
        [Required(ErrorMessage = "الاسم مطلوب")]
        [Display(Name = "الاسم")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "نبذة عني")]
        public string Bio { get; set; } = string.Empty;

        [Display(Name = "الموقع")]
        public string Location { get; set; } = string.Empty;

        [Display(Name = "الصورة الشخصية")]
        public IFormFile? ProfileImage { get; set; }

        [Display(Name = "المهارات")]
        public List<int>? SelectedSkillIds { get; set; }

        [Display(Name = "المؤهلات التعليمية")]
        public string Education { get; set; } = string.Empty;

        [Display(Name = "الخبرات السابقة")]
        public string Experience { get; set; } = string.Empty;

        [Display(Name = "الشهادات")]
        public string Certifications { get; set; } = string.Empty;
    }
}

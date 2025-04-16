using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Tarjim.ViewModels
{
    public class TranslatorRegisterStep1ViewModel
    {
        [Display(Name = "الصورة الشخصية")]
        public IFormFile? Avatar { get; set; }

        [Required(ErrorMessage = "الاسم مطلوب")]
        [Display(Name = "الاسم الكامل")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "وصف الخدمة مطلوب")]
        [Display(Name = "وصف الخدمة")]
        public string? Bio { get; set; }

        [Required(ErrorMessage = "البلد مطلوب")]
        [Display(Name = "الدولة")]
        public string? Country { get; set; }

        [Required(ErrorMessage = "المدينة مطلوبة")]
        [Display(Name = "المدينة")]
        public string? City { get; set; }
    }
}
